namespace RigGen;

using ClangSharp;
using ClangSharp.Interop;
using JsonData;

public class CppGen
{
    public required IEnumerable<string> directories { get; set; }
    private readonly List<JsonData.Base> parsedData = new List<JsonData.Base>();
    private bool IsPathUser(string path)
    {
        return directories.Any(whitelistedDir =>
            path.StartsWith(whitelistedDir, StringComparison.OrdinalIgnoreCase));
    }
    private string NormalizedPath(string path)
    {
        return Path.GetFullPath(new Uri(path).LocalPath);
    }
    private void FillLocation(JsonData.Base node, CXCursor cursor)
    {
        cursor.Location.GetFileLocation(out var file, out uint line, out uint column, out uint _);
        node.location = new JsonData.Location
        {
            file = NormalizedPath(file.Name.ToString()),
            line = (int)line,
            column = (int)column
        };
    }
    private void AddNode(JsonData.Base node)
    {
        node.index = parsedData.Count;
        parsedData.Add(node);
    }
    private string FullyQualifiedName(CXCursor cursor)
    {
        var names = new Stack<string>();
        var current = cursor;
        while (current.Kind != CXCursorKind.CXCursor_TranslationUnit)
        {
            if (!string.IsNullOrEmpty(current.Spelling.ToString()))
            {
                names.Push(current.Spelling.ToString());
            }
            current = current.SemanticParent;
        }
        return string.Join("::", names);
    }
    List<string> CollectHeaderFiles()
    {
        //First collect all header files in the different directories
        //Do this by recursively walking each directory in the list
        var headerFiles = new List<string>();
        foreach (var directory in directories)
        {
            if (Directory.Exists(directory))
            {
                headerFiles.AddRange(
                    Directory.EnumerateFiles(directory, "*.h", SearchOption.AllDirectories)
                );
                headerFiles.AddRange(
                    Directory.EnumerateFiles(directory, "*.hpp", SearchOption.AllDirectories)
                );
            }
        }
        return headerFiles;
    }

    internal List<JsonData.Base> Generate(string outputDir, IEnumerable<string> additionalArgs)
    {
        var files = CollectHeaderFiles();
        //Write files to a temporary output cpp file, which will then be parsed by libclang
        var tempFile = Path.Combine(outputDir, "temp.cpp");
        using (var writer = new StreamWriter(tempFile))
        {
            writer.WriteLine("#define RIG_GEN");
            foreach (var file in files)
            {
                writer.WriteLine($"#include \"{file.Replace("\\", "/")}\"");
            }
        }

        //Pull the files into libclang
        unsafe
        {
            var index = CXIndex.Create();
            var args = new ReadOnlySpan<string>(additionalArgs.ToArray());
            var unit = CXTranslationUnit.CreateFromSourceFile(index, tempFile, args, null);
            if (unit == null)
            {
                throw new Exception("Failed to create translation unit");
            }
            var location = unit.GetLocation(unit.GetFile(tempFile), 0, 0);
            unit.Cursor.VisitChildren((cursor, _, _) =>
            {
                switch (cursor.Kind)
                {
                    case CXCursorKind.CXCursor_UsingDeclaration:
                        //Using declarations are a means of introducing annotations
                        ParseUsingDeclaration(cursor);
                        break;
                    case CXCursorKind.CXCursor_TypedefDecl:
                        //Typedefs are a means of introducing annotations
                        ParseTypedefDeclaration(cursor);
                        break;
                    case CXCursorKind.CXCursor_ClassDecl:
                    case CXCursorKind.CXCursor_StructDecl:
                        //If a class or struct is encountered here, only parse it if annotation requires it
                        ParseClassDeclaration(cursor);
                        break;
                    case CXCursorKind.CXCursor_FunctionDecl:
                        //Function declarations are the most important, they tell us what types are needed
                        ParseFunctionDeclaration(cursor);
                        break;
                    case CXCursorKind.CXCursor_Namespace:
                        //Need to recurse into the namespace to find more declarations
                        return CXChildVisitResult.CXChildVisit_Recurse;
                    default:
                        break;
                }
                return CXChildVisitResult.CXChildVisit_Continue;
            }, new CXClientData(IntPtr.Zero));
        }
        return parsedData;
    }
    void ParseUsingDeclaration(CXCursor cursor)
    {
    }
    void ParseTypedefDeclaration(CXCursor cursor)
    {
    }
    void ParseClassDeclaration(CXCursor cursor)
    {
    }
    void ParseFunctionDeclaration(CXCursor cursor)
    {
        var functionNode = new Function();
        FillLocation(functionNode, cursor);
        if (!IsPathUser(functionNode.location.file))
            return;
        functionNode.name = FullyQualifiedName(cursor);
        functionNode.access = Access.Public;   //Functions in the user path will be assumed public
        functionNode.modifiers = new List<Modifier>(); //Function declarations do not have modifiers
        functionNode.annotations = new List<Annotation>(); //TODO: Determine annotations
        functionNode.comments = new List<string>(); //TODO: Extract comments
        unsafe
        {
            cursor.VisitChildren((childCursor, _, _) =>
            {
                switch (childCursor.Kind)
                {
                    case CXCursorKind.CXCursor_ParmDecl:
                        //ParmDecl is a parameter declaration
                        functionNode.parameters.Add(new Parameter
                        {
                            name = childCursor.Spelling.ToString(),
                            qualifiedType = ParseTypeKind(childCursor.Type)
                        });
                        break;
                    case CXCursorKind.CXCursor_TypeRef:
                        //This is the return type
                        functionNode.returnQualifiedType = ParseTypeKind(childCursor.Type);
                        break;
                    default:
                        break;
                }
                return CXChildVisitResult.CXChildVisit_Continue;
            }, new CXClientData(IntPtr.Zero));
        }
        AddNode(functionNode);
    }
    int ParseUnqualifiedType(CXType type)
    {
        var typeNode = new DataType
        {
            name = type.Spelling.ToString(),
        };
        AddNode(typeNode);
        return typeNode.index;
    }
    List<Qualifier> GetQualifiers(CXType type, out CXType unqualifiedType)
    {
        unqualifiedType = type;
        var qualifiers = new List<Qualifier>();
        if (unqualifiedType.kind == CXTypeKind.CXType_Pointer)
        {
            throw new NotSupportedException("Pointer types are not supported");
        }
        if (unqualifiedType.kind == CXTypeKind.CXType_LValueReference || unqualifiedType.kind == CXTypeKind.CXType_RValueReference)
        {
            qualifiers.Add(Qualifier.Reference);
            unqualifiedType = unqualifiedType.NonReferenceType;
        }
        if (unqualifiedType.IsConstQualified)
        {
            qualifiers.Add(Qualifier.Const);
            unqualifiedType = unqualifiedType.UnqualifiedType;
        }
        return qualifiers;
    }
    int ParseTypeKind(CXType typeKind)
    {
        var qualifiers = GetQualifiers(typeKind, out var unqualifiedType);
        var typeNode = new QualifiedType
        {
            name = typeKind.Spelling.ToString(),
            dataType = ParseUnqualifiedType(unqualifiedType),
            qualifiers = qualifiers,
        };
        AddNode(typeNode);
        return typeNode.index;
    }
}