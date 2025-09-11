namespace BigRig.RigGen;

using ClangSharp;
using ClangSharp.Interop;
using JsonData;

public class CppGen
{
    public CppGen(IEnumerable<string> dirs)
    {
        directories = (from dir in dirs select NormalizedPath(dir)).ToList();
    }
    private List<string> directories;
    private readonly List<Base> parsedData = new List<Base>();
    private bool IsPathUser(string path)
    {
        return directories.Any(whitelistedDir =>
            path.StartsWith(whitelistedDir, StringComparison.OrdinalIgnoreCase));
    }
    private string NormalizedPath(string path)
    {
        if (path == string.Empty) { return string.Empty; }
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
    private delegate CXChildVisitResult VisitCursor(CXCursor cursor);
    private void WalkCursor(CXCursor cursor, VisitCursor action)
    {
        unsafe
        {
            cursor.VisitChildren((childCursor, _, _) =>
            {
                return action(childCursor);
            }, new CXClientData(IntPtr.Zero));
        }
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
        var numErrors =unit.NumDiagnostics;
        for (uint i = 0; i < numErrors; i++)
        {
            var diag = unit.GetDiagnostic(i);
            diag.Location.GetFileLocation(out var file, out uint line, out uint column, out uint _);
            var message = diag.Format(CXDiagnosticDisplayOptions.CXDiagnostic_DisplaySourceLocation).ToString();
            var severity = diag.Severity switch
            {
                CXDiagnosticSeverity.CXDiagnostic_Ignored => DiagnosticSeverity.Ignored,
                CXDiagnosticSeverity.CXDiagnostic_Note => DiagnosticSeverity.Note,
                CXDiagnosticSeverity.CXDiagnostic_Warning => DiagnosticSeverity.Warning,
                CXDiagnosticSeverity.CXDiagnostic_Error => DiagnosticSeverity.Error,
                CXDiagnosticSeverity.CXDiagnostic_Fatal => DiagnosticSeverity.Fatal,
                _ => DiagnosticSeverity.Ignored,
            };
            var messageNode = new Message
            {
                location = new Location
                {
                    file = NormalizedPath(file.Name.ToString()),
                    line = (int)line,
                    column = (int)column
                },
                name = "Diagnostic",
                message = message,
                severity = severity,
                category = diag.CategoryText.ToString(),
            };
            AddNode(messageNode);
            Console.WriteLine(message);
        }
        //Collect the important cursors
        var decls = new List<CXCursor>();
        var records = new List<CXCursor>();
        var functions = new List<CXCursor>();
        WalkCursor(unit.Cursor, (cursor) =>
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
        });
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
        //Add node here in case there is recursion, we want to have a node to refer to
        AddNode(recordNode);

        recordNode.isAnonymous = cursor.IsAnonymous;
        //Collect the different cursors
        var baseClasses = new List<CXCursor>();
        var fields = new List<CXCursor>();
        var methods = new List<CXCursor>();
        WalkCursor(cursor, (childCursor) =>
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
        });
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
        var paramNodes = new List<CXCursor>();
        WalkCursor(cursor, (childCursor) =>
        {
            if (childCursor.kind == CXCursorKind.CXCursor_ParmDecl)
            {
                paramNodes.Add(childCursor);
            }
            return CXChildVisitResult.CXChildVisit_Continue;
        });
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
    DataType ParseBuiltinType(CXType type)
    {
        var kind = type.kind switch
        {
            CXTypeKind.CXType_Void => BuiltinDataType.Kind.Void,
            CXTypeKind.CXType_Bool => BuiltinDataType.Kind.Bool,
            CXTypeKind.CXType_Char_U or CXTypeKind.CXType_UChar => BuiltinDataType.Kind.UInt8,
            CXTypeKind.CXType_Char_S or CXTypeKind.CXType_SChar => BuiltinDataType.Kind.Int8,
            CXTypeKind.CXType_UShort => BuiltinDataType.Kind.UInt16,
            CXTypeKind.CXType_Short or CXTypeKind.CXType_Char16 or CXTypeKind.CXType_WChar => BuiltinDataType.Kind.Int16,
            CXTypeKind.CXType_UInt => BuiltinDataType.Kind.UInt32,
            CXTypeKind.CXType_Int or CXTypeKind.CXType_Char32 => BuiltinDataType.Kind.Int32,
            CXTypeKind.CXType_ULong or CXTypeKind.CXType_ULongLong => BuiltinDataType.Kind.UInt64,
            CXTypeKind.CXType_Long or CXTypeKind.CXType_LongLong => BuiltinDataType.Kind.Int64,
            CXTypeKind.CXType_UInt128 => BuiltinDataType.Kind.UInt128,
            CXTypeKind.CXType_Int128 => BuiltinDataType.Kind.Int128,
            CXTypeKind.CXType_Float16 => BuiltinDataType.Kind.Float16,
            CXTypeKind.CXType_BFloat16 => BuiltinDataType.Kind.BFloat16,
            CXTypeKind.CXType_Float => BuiltinDataType.Kind.Float,
            CXTypeKind.CXType_Double => BuiltinDataType.Kind.Double,
            CXTypeKind.CXType_LongDouble => BuiltinDataType.Kind.LongDouble,
            CXTypeKind.CXType_Float128 => BuiltinDataType.Kind.Float128,
            CXTypeKind.CXType_NullPtr => BuiltinDataType.Kind.Nullptr,
            _ => BuiltinDataType.Kind.Unsupported,
        };
        return new BuiltinDataType { builtinKind = kind };
    }
    DataType ParseRecordType(CXCursor typeDecl)
    {
        //Collect template arguments by walking the cursor
        var templateArgs = new List<JsonData.TemplateArgument>();
        var templateArgsCount = clang.Cursor_getNumTemplateArguments(typeDecl);
        if (templateArgsCount > 0)
        {
            for (uint i = 0; i < templateArgsCount; i++)
            {
                var argKind = clang.Cursor_getTemplateArgumentKind(typeDecl, i);
                if (argKind == CXTemplateArgumentKind.CXTemplateArgumentKind_Type)
                {
                    var argType = clang.Cursor_getTemplateArgumentType(typeDecl, i);
                    templateArgs.Add(new JsonData.TemplateArgument
                    {
                        kind = JsonData.TemplateArgument.Kind.Type,
                        value = ParseUnqualifiedType(argType)
                    });
                }
                else if (argKind == CXTemplateArgumentKind.CXTemplateArgumentKind_Integral)
                {
                    var argValue = clang.Cursor_getTemplateArgumentValue(typeDecl, i);
                    templateArgs.Add(new JsonData.TemplateArgument
                    {
                        kind = JsonData.TemplateArgument.Kind.Integral,
                        value = argValue
                    });
                }
                else if (argKind == CXTemplateArgumentKind.CXTemplateArgumentKind_Pack)
                {
                    //It isn't clear how to tease apart a template argument pack, so just mark it as unknown
                    Console.WriteLine("Warning: Template argument packs are not fully supported, marking as unknown");
                    templateArgs.Add(new JsonData.TemplateArgument
                    {
                        kind = JsonData.TemplateArgument.Kind.Unknown,
                        value = -1
                    });
                }
                else
                {
                    templateArgs.Add(new JsonData.TemplateArgument
                    {
                        kind = JsonData.TemplateArgument.Kind.Unknown,
                        value = -1
                    });
                }
            }
        }
        var recordIndex = ParseClassDeclaration(typeDecl);
        return new RecordDataType
        {
            recordType = recordIndex,
            templateArgs = templateArgs
        };
    }
    DataType ParseEnumType(CXCursor typeDecl)
    {
        var enumNodes = new List<CXCursor>();
        WalkCursor(typeDecl, (childCursor) =>
        {
            if (childCursor.kind == CXCursorKind.CXCursor_EnumConstantDecl)
            {
                enumNodes.Add(childCursor);
            }
            return CXChildVisitResult.CXChildVisit_Continue;
        });
        return new EnumDataType
        {
            underlyingType = ParseUnqualifiedType(clang.getEnumDeclIntegerType(typeDecl)),
            enumerators = (from node in enumNodes select new EnumeratorField
            {
                name = node.Spelling.ToString(),
                value = node.EnumConstantDeclValue
            }).ToList()
        };
    }
    DataType ParseFunctionType(CXType type)
    {
        var argCount = clang.getNumArgTypes(type);
        var retType = clang.getResultType(type);
        var isVariadic = clang.isFunctionTypeVariadic(type);
        return new FunctionDataType
        {
            returnType = ParseUnqualifiedType(retType),
            argumentTypes = (from i in Enumerable.Range(0, argCount)
                             select ParseUnqualifiedType(clang.getArgType(type, (uint)i))).ToList(),
            isVariadic = isVariadic != 0
        };
    }
    int ParseUnqualifiedType(CXType type)
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
        var typeDecl = clang.getTypeDeclaration(type);
        var typeNode = type.kind switch
        {
            >=CXTypeKind.CXType_FirstBuiltin and <= CXTypeKind.CXType_LastBuiltin => ParseBuiltinType(type),
            CXTypeKind.CXType_Record => ParseRecordType(typeDecl),
            CXTypeKind.CXType_Enum => ParseEnumType(typeDecl),
            CXTypeKind.CXType_FunctionNoProto or CXTypeKind.CXType_FunctionProto => ParseFunctionType(type),
            _ => new UnknownDataType(),
        };
        typeNode.name = name;
        typeNode.location = GetLocation(typeDecl);

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
        var canonicalType = clang.getCanonicalType(typeKind);
        var qualifiers = GetQualifiers(canonicalType, out var unqualifiedType);
        var typeNode = new QualifiedType
        {
            name = typeKind.Spelling.ToString(),
            location = location,
            dataType = ParseUnqualifiedType(unqualifiedType),
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