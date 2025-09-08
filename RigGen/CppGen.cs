namespace RigGen;

using ClangSharp.Interop;
using JsonData;

public class CppGen
{
    public required IEnumerable<string> directories { get; set; }
    private readonly List<Base> parsedData = new List<Base>();
    private bool IsPathUser(string path)
    {
        return directories.Any(whitelistedDir =>
            path.StartsWith(whitelistedDir, StringComparison.OrdinalIgnoreCase));
    }
    private string NormalizedPath(string path)
    {
        return Path.GetFullPath(new Uri(path).LocalPath);
    }
    private Location GetLocation(CXCursor cursor)
    {
        cursor.Location.GetFileLocation(out var file, out uint line, out uint column, out uint _);
        return new Location
        {
            file = NormalizedPath(file.Name.ToString()),
            line = (int)line,
            column = (int)column
        };
    }
    private void AddNode(Base node)
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
    public List<Base> Generate(string outputDir, IEnumerable<string> additionalArgs)
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
        var index = CXIndex.Create();
        var args = new ReadOnlySpan<string>(additionalArgs.ToArray());
        var unit = CXTranslationUnit.CreateFromSourceFile(index, tempFile, args, null);
        if (unit == null)
        {
            throw new Exception("Failed to create translation unit");
        }
        var decls = new List<CXCursor>();
        var records = new List<CXCursor>();
        var functions = new List<CXCursor>();
        unsafe
        {
            unit.Cursor.VisitChildren((cursor, _, _) =>
            {
                switch (cursor.Kind)
                {
                    case CXCursorKind.CXCursor_UsingDeclaration:
                    case CXCursorKind.CXCursor_TypedefDecl:
                        decls.Add(cursor);
                        break;
                    case CXCursorKind.CXCursor_ClassDecl:
                    case CXCursorKind.CXCursor_StructDecl:
                        records.Add(cursor);
                        break;
                    case CXCursorKind.CXCursor_FunctionDecl:
                        functions.Add(cursor);
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
        //While it shouldn't matter the order of traversal, we'll traverse records first, followed by functions, and lastly annotations
        foreach (var node in records)
        {
            ParseClassDeclaration(node);
        }
        foreach (var node in functions)
        {
            ParseFunctionDeclaration(node, false);
        }
        foreach (var node in decls)
        {
            ParseTypedefDeclaration(node);
        }
        return parsedData;
    }
    void ParseTypedefDeclaration(CXCursor cursor)
    {
    }
    Access GetAccessFromSpecifier(CX_CXXAccessSpecifier specifier)
    {
        switch (specifier)
        {
            case CX_CXXAccessSpecifier.CX_CXXPublic:
                return Access.Public;
            case CX_CXXAccessSpecifier.CX_CXXProtected:
                return Access.Protected;
            case CX_CXXAccessSpecifier.CX_CXXPrivate:
                return Access.Private;
            default:
                return Access.Public;
        }
    }
    int ParseClassDeclaration(CXCursor cursor)
    {
        var recordNode = new Record();
        recordNode.location = GetLocation(cursor);
        if (!IsPathUser(recordNode.location.file))
            return -1;
        recordNode.name = FullyQualifiedName(cursor);
        //It is possible while parsing types to encounter the same class multiple times
        //So return the existing index if this class has already been parsed
        var existingNode = parsedData.OfType<Record>().FirstOrDefault(r => r.name == recordNode.name);
        if (existingNode != null)
        {
            return existingNode.index;
        }

        recordNode.isAnonymous = cursor.IsAnonymous;
        //Collect the different cursors in an unsafe context
        //Then parse them not in an unsafe context
        var baseClasses = new List<CXCursor>();
        var fields = new List<CXCursor>();
        var methods = new List<CXCursor>();
        unsafe
        {
            cursor.VisitChildren((childCursor, _, _) =>
            {
                switch (childCursor.Kind)
                {
                    case CXCursorKind.CXCursor_CXXBaseSpecifier:
                        baseClasses.Add(childCursor);
                        break;
                    case CXCursorKind.CXCursor_FieldDecl:
                        fields.Add(childCursor);
                        break;
                    case CXCursorKind.CXCursor_CXXMethod:
                        methods.Add(childCursor);
                        break;
                    default:
                        break;
                }
                return CXChildVisitResult.CXChildVisit_Continue;
            }, new CXClientData(IntPtr.Zero));
        }
        foreach (var node in baseClasses)
        {
            var baseRecord = new RecordBase();
            baseRecord.isVirtual = node.IsVirtualBase;
            baseRecord.access = GetAccessFromSpecifier(node.CXXAccessSpecifier);
            baseRecord.baseRecord = ParseClassDeclaration(clang.getCursorReferenced(node));
            recordNode.bases.Add(baseRecord);
        }
        foreach (var node in fields)
        {
            var fieldNode = new Field();
            fieldNode.name = node.Spelling.ToString();
            fieldNode.access = Access.Public;
            fieldNode.qualifiedType = ParseTypeKind(node.Type, GetLocation(node));
            fieldNode.offset = (uint)node.OffsetOfField;
            recordNode.fields.Add(fieldNode);
        }
        foreach (var node in methods)
        {
            recordNode.functions.Add(ParseFunctionDeclaration(node, true));
        }
        AddNode(recordNode);
        return recordNode.index;
    }
    int ParseFunctionDeclaration(CXCursor cursor, bool isMethod)
    {
        var functionNode = new Function();
        functionNode.location = GetLocation(cursor);
        //Methods have already been decided to be parsed, so always parse them
        if (!isMethod && !IsPathUser(functionNode.location.file))
        {
            return -1;
        }

        functionNode.name = FullyQualifiedName(cursor);
        //It is possible for global functions to have multiple declarations, so only parse the first one
        if (!isMethod && parsedData.OfType<Function>().Any(f => f.name == functionNode.name))
        {
            return -1;
        }

        functionNode.access = Access.Public;   //Functions in the user path will be assumed public
        if (isMethod)
        {
            functionNode.access = GetAccessFromSpecifier(cursor.CXXAccessSpecifier);
            var prettyPrinting = cursor.GetPrettyPrinted(new CXPrintingPolicy()).ToString();
            if (prettyPrinting.StartsWith("static "))   //Static methods have static at the start of the declaration
            {
                functionNode.modifiers.Add(Modifier.Static);
            }
            else
            {
                //Non-static methods can be const, virtual, or pure virtual
                if (prettyPrinting.Contains(") const")) //Want the const at the end of the parameter list
                {
                    functionNode.modifiers.Add(Modifier.Const);
                }
                if (prettyPrinting.StartsWith("virtual "))
                {
                    if (prettyPrinting.EndsWith("= 0")) //Pure virtual functions have = 0 at the end
                    {
                        functionNode.modifiers.Add(Modifier.PureVirtual);
                    }
                    else
                    {
                        functionNode.modifiers.Add(Modifier.Virtual);
                    }
                }
            }
        }
        functionNode.annotations = new List<Annotation>(); //TODO: Determine annotations
        functionNode.comments = new List<string>(); //TODO: Extract comments
        functionNode.returnQualifiedType = ParseTypeKind(cursor.ResultType, functionNode.location);
        //Getting both the parameter name and the parameter type requires walking further down the AST
        //Get a list of the cursors that point to the parameters, and then walk them in a safe context
        var paramNodes = new List<CXCursor>();
        unsafe
        {
            cursor.VisitChildren((childCursor, _, _) =>
            {
                if (childCursor.kind == CXCursorKind.CXCursor_ParmDecl)
                {
                    paramNodes.Add(childCursor);
                }
                return CXChildVisitResult.CXChildVisit_Continue;
            }, new CXClientData(IntPtr.Zero));
        }
        foreach (var node in paramNodes)
        {
            functionNode.parameters.Add(new Parameter
            {
                name = node.Spelling.ToString(),
                qualifiedType = ParseTypeKind(node.Type, GetLocation(node))
            });
        }
        AddNode(functionNode);
        return functionNode.index;
    }
    int ParseUnqualifiedType(CXType type, Location location)
    {
        var name = type.Spelling.ToString();
        //See if this type node already exists, if so, return that index
        //Match is determined by name only because all other decoration should be removed by this point
        var existingNode = parsedData.OfType<DataType>().FirstOrDefault(dt => dt.name == name);
        if (existingNode != null)
        {
            return existingNode.index;
        }
        //Otherwise, create a new type node
        var typeNode = new DataType
        {
            name = name,
            location = location
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
            qualifiers.Add(Qualifier.Pointer);
            unqualifiedType = clang.getPointeeType(unqualifiedType);
        }
        if (unqualifiedType.kind == CXTypeKind.CXType_LValueReference || unqualifiedType.kind == CXTypeKind.CXType_RValueReference)
        {
            qualifiers.Add(Qualifier.Reference);
            unqualifiedType = unqualifiedType.NonReferenceType;
        }
        if (unqualifiedType.IsConstQualified)
        {
            //Technically we lose a const pointer and pointer to const differentiation, but for interopability, that distinction isn't important
            qualifiers.Add(Qualifier.Const);
            unqualifiedType = unqualifiedType.UnqualifiedType;
        }
        return qualifiers;
    }
    int ParseTypeKind(CXType typeKind, Location location)
    {
        var canonincalType = typeKind.CanonicalType;
        var qualifiers = GetQualifiers(canonincalType, out var unqualifiedType);
        var typeNode = new QualifiedType
        {
            name = canonincalType.Spelling.ToString(),
            location = location,
            dataType = ParseUnqualifiedType(unqualifiedType, location),
            qualifiers = qualifiers,
        };
        //See if this qualified type node already exists, if so, return that index
        //Equality is determined by name, qualifiers, annotations, and dataType
        var existingNode = parsedData.OfType<QualifiedType>().FirstOrDefault(
            dt => dt.name == typeNode.name
            && dt.dataType == typeNode.dataType
            && dt.qualifiers.SequenceEqual(typeNode.qualifiers)
            && dt.annotations.SequenceEqual(typeNode.annotations)
        );
        if (existingNode != null)
        {
            return existingNode.index;
        }
        
        AddNode(typeNode);
        return typeNode.index;
    }
}