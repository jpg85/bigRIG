using JsonData;

namespace RigGenTests;

public class Tests
{
    string _clangHeaderPath = string.Empty;
    List<string> GetClangArgs()
    {
        var args = new List<string> { "-x", "c++", "-std=c++20" };
        if (!string.IsNullOrEmpty(_clangHeaderPath) && Directory.Exists(_clangHeaderPath))
        {
            args.Add("-isystem");
            args.Add(_clangHeaderPath);
        }
        return args;
    }
    [OneTimeSetUp]
    public void Setup()
    {
        //Locate clang executable and relative header files
        var overridePath = Environment.GetEnvironmentVariable("CLANG_PATH");
        if (!string.IsNullOrEmpty(overridePath) && Directory.Exists(overridePath))
        {
            _clangHeaderPath = overridePath;
            return;
        }
        var possiblePaths = new List<string>
        {
            // Common installation paths for Clang on different platforms
            "/usr/lib/clang",               // Linux
            "/usr/local/lib/clang",         // macOS
            "C:\\Program Files\\LLVM\\lib\\clang" // Windows
        };
        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                var versionDirs = Directory.GetDirectories(path);
                if (versionDirs.Length > 0)
                {
                    // Use the highest version available
                    Array.Sort(versionDirs);
                    _clangHeaderPath = Path.Combine(versionDirs[^1], "include"); // Get the last (highest) version
                    return;
                }
            }
        }
    }
    //Parse a simple function declaration
    [Test]
    public void TestFunctionDeclaration()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var simpleFunctionDirectory = Path.Combine(testDirectory, "TestCollateral", "CppGen", "SimpleFunction");
        var cppGen = new RigGen.CppGen(new List<string> { simpleFunctionDirectory });
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var json = cppGen.Generate(tempDir, GetClangArgs());
        Assert.That(json, Is.Not.Null);
        Assert.That(json, Has.Count.EqualTo(3));    // Function, ReturnType qualified type, Void type
        var voidType = json[0] as JsonData.DataType;
        var returnHolder = json[1] as JsonData.QualifiedType;
        var function = json[2] as JsonData.Function;
        Assert.That(voidType, Is.Not.Null);
        Assert.That(returnHolder, Is.Not.Null);
        Assert.That(function, Is.Not.Null);
    }
    //Parse a function with parameters
    [Test]
    public void TestFunctionWithParameters()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var parameterFunctionDirectory = Path.Combine(testDirectory, "TestCollateral", "CppGen", "ParameterFunction");
        var cppGen = new RigGen.CppGen(new List<string> { parameterFunctionDirectory });
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var json = cppGen.Generate(tempDir, GetClangArgs());
        Assert.That(json, Is.Not.Null);
        Assert.That(json, Has.Count.EqualTo(5));    // Function, ReturnType and Parameter qualified type, Int type, Float type, 1 Parameter qualified types
        var intType = json.OfType<JsonData.DataType>().FirstOrDefault(dt => dt.name == "int");
        var floatType = json.OfType<JsonData.DataType>().FirstOrDefault(dt => dt.name == "float");
        var returnHolder = json.OfType<JsonData.QualifiedType>().FirstOrDefault(qt => qt.name == "int");
        var param1Holder = json.OfType<JsonData.QualifiedType>().FirstOrDefault(qt => qt.name == "int");
        var param2Holder = json.OfType<JsonData.QualifiedType>().FirstOrDefault(qt => qt.name == "float");
        var function = json.OfType<JsonData.Function>().FirstOrDefault(f => f.name == "Foo");
        Assert.That(intType, Is.Not.Null);
        Assert.That(floatType, Is.Not.Null);
        Assert.That(returnHolder, Is.Not.Null);
        Assert.That(param1Holder, Is.Not.Null);
        Assert.That(param2Holder, Is.Not.Null);
        Assert.That(function, Is.Not.Null);
        //Verify the function is correctly linked to its return type and parameters
        Assert.That(function.returnQualifiedType, Is.EqualTo(returnHolder.index));
        Assert.That(function.parameters, Has.Count.EqualTo(2));
        Assert.That(function.parameters[0].name, Is.EqualTo("a"));
        Assert.That(function.parameters[0].qualifiedType, Is.EqualTo(param1Holder.index));
        Assert.That(function.parameters[1].name, Is.EqualTo("b"));
        Assert.That(function.parameters[1].qualifiedType, Is.EqualTo(param2Holder.index));
    }
    //Parse a function with sugared parameters (const, reference)
    [Test]
    public void TestFunctionWithSugaredParameters()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var sugaredParametersDirectory = Path.Combine(testDirectory, "TestCollateral", "CppGen", "SugaredParameters");
        var cppGen = new RigGen.CppGen(new List<string> { sugaredParametersDirectory });
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var json = cppGen.Generate(tempDir, GetClangArgs());
        Assert.That(json, Is.Not.Null);
        Assert.That(json, Has.Count.EqualTo(6));    // Function, ReturnType qualified type, Int type, Float type, 2 Parameter qualified types
        var intType = json.OfType<JsonData.DataType>().FirstOrDefault(dt => dt.name == "int");
        var floatType = json.OfType<JsonData.DataType>().FirstOrDefault(dt => dt.name == "float");
        var returnHolder = json.OfType<JsonData.QualifiedType>().FirstOrDefault(qt => qt.name == "const int");
        var param1Holder = json.OfType<JsonData.QualifiedType>().FirstOrDefault(qt => qt.name == "int &");
        var param2Holder = json.OfType<JsonData.QualifiedType>().FirstOrDefault(qt => qt.name == "const float &");
        var function = json.OfType<JsonData.Function>().FirstOrDefault(f => f.name == "Foo");
        Assert.That(intType, Is.Not.Null);
        Assert.That(floatType, Is.Not.Null);
        Assert.That(returnHolder, Is.Not.Null);
        Assert.That(param1Holder, Is.Not.Null);
        Assert.That(param2Holder, Is.Not.Null);
        Assert.That(function, Is.Not.Null);
        //Verify the function is correctly linked to its return type and parameters
        Assert.That(function.returnQualifiedType, Is.EqualTo(returnHolder.index));
        Assert.That(function.parameters, Has.Count.EqualTo(2));
        Assert.That(function.parameters[0].name, Is.EqualTo("a"));
        Assert.That(function.parameters[0].qualifiedType, Is.EqualTo(param1Holder.index));
        Assert.That(function.parameters[1].name, Is.EqualTo("b"));
        Assert.That(function.parameters[1].qualifiedType, Is.EqualTo(param2Holder.index));
    }
    //Parse a simple class/struct
    [Test]
    public void TestSimpleClass()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var simpleClassDirectory = Path.Combine(testDirectory, "TestCollateral", "CppGen", "SimpleClass");
        var cppGen = new RigGen.CppGen(new List<string> { simpleClassDirectory });
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var json = cppGen.Generate(tempDir, GetClangArgs());
        Assert.That(json, Is.Not.Null);
        Assert.That(json, Has.Count.EqualTo(11));    // Class, Field qualified type, Int type, void qualified type, void type, and 6 functions
        var intType = json.OfType<JsonData.DataType>().FirstOrDefault(dt => dt.name == "int");
        var fieldHolder = json.OfType<JsonData.QualifiedType>().FirstOrDefault(qt => qt.name == "int");
        var record = json.OfType<JsonData.Record>().FirstOrDefault(r => r.name == "Foo");
        Assert.That(intType, Is.Not.Null);
        Assert.That(fieldHolder, Is.Not.Null);
        Assert.That(record, Is.Not.Null);
        //Verify the class is correctly linked to its field
        Assert.That(record.fields, Has.Count.EqualTo(1));
        Assert.That(record.fields[0].name, Is.EqualTo("member"));
        Assert.That(record.fields[0].qualifiedType, Is.EqualTo(fieldHolder.index));
        //Verify the class is correctly linked to its functions
        Assert.That(record.functions, Has.Count.EqualTo(6));
        var functionNames = new List<string> { "Foo::Method", "Foo::ConstMethod", "Foo::VirtualMethod", "Foo::PureVirtualMethod", "Foo::ConstPureVirtualMethod", "Foo::StaticMethod" };
        foreach (var functionIndex in record.functions)
        {
            var function = json.OfType<JsonData.Function>().FirstOrDefault(f => f.index == functionIndex);
            Assert.That(function, Is.Not.Null);
            Assert.That(functionNames, Does.Contain(function.name));
            functionNames.Remove(function.name);
            //Verify the function is linked back to the class
            Assert.That(function.name, Is.Not.Null);
            Assert.That(function.location.file, Is.Not.Null);
            Assert.That(function.location.line, Is.GreaterThan(0));
            Assert.That(function.location.column, Is.GreaterThan(0));
            if (function.name == "Foo::Method")
            {
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.Const));
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.Static));
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.Virtual));
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.PureVirtual));
            }
            else if (function.name == "Foo::ConstMethod")
            {
                Assert.That(function.modifiers, Does.Contain(JsonData.Modifier.Const));
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.Static));
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.Virtual));
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.PureVirtual));
            }
            else if (function.name == "Foo::VirtualMethod")
            {
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.Const));
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.Static));
                Assert.That(function.modifiers, Does.Contain(JsonData.Modifier.Virtual));
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.PureVirtual));
            }
            else if (function.name == "Foo::PureVirtualMethod")
            {
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.Const));
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.Static));
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.Virtual));
                Assert.That(function.modifiers, Does.Contain(JsonData.Modifier.PureVirtual));
            }
            else if (function.name == "Foo::ConstPureVirtualMethod")
            {
                Assert.That(function.modifiers, Does.Contain(JsonData.Modifier.Const));
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.Static));
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.Virtual));
                Assert.That(function.modifiers, Does.Contain(JsonData.Modifier.PureVirtual));
            }
            else if (function.name == "Foo::StaticMethod")
            {
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.Const));
                Assert.That(function.modifiers, Does.Contain(JsonData.Modifier.Static));
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.Virtual));
                Assert.That(function.modifiers, Does.Not.Contain(JsonData.Modifier.PureVirtual));
            }
        }
    }
    //Parse a class with base classes
    [Test]
    public void TestClassWithBaseClasses()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var baseClassDirectory = Path.Combine(testDirectory, "TestCollateral", "CppGen", "BaseClass");
        var cppGen = new RigGen.CppGen(new List<string> { baseClassDirectory });
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var json = cppGen.Generate(tempDir, GetClangArgs());
        Assert.That(json, Is.Not.Null);
        Assert.That(json, Has.Count.EqualTo(3));    // 3 Classes
        var baseA = json.OfType<JsonData.Record>().FirstOrDefault(r => r.name == "BaseA");
        var baseB = json.OfType<JsonData.Record>().FirstOrDefault(r => r.name == "BaseB");
        var derived = json.OfType<JsonData.Record>().FirstOrDefault(r => r.name == "Derived");
        Assert.That(baseA, Is.Not.Null);
        Assert.That(baseB, Is.Not.Null);
        Assert.That(derived, Is.Not.Null);
        //Verify the derived class is correctly linked to its base classes
        Assert.That(derived.bases, Has.Count.EqualTo(2));
        var baseARef = derived.bases.FirstOrDefault(b => b.access == JsonData.Access.Public);
        var baseBRef = derived.bases.FirstOrDefault(b => b.access == JsonData.Access.Protected);
        Assert.That(baseARef, Is.Not.Null);
        Assert.That(baseARef.baseRecord, Is.EqualTo(baseA.index));
        Assert.That(baseARef.isVirtual, Is.False);
        Assert.That(baseBRef, Is.Not.Null);
        Assert.That(baseBRef.baseRecord, Is.EqualTo(baseB.index));
        Assert.That(baseBRef.isVirtual, Is.True);
    }
    //Parse a structure with a number of different types
    [Test]
    public void TestTypeParsing()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var typeParsingDirectory = Path.Combine(testDirectory, "TestCollateral", "CppGen", "TypeParsing");
        var cppGen = new RigGen.CppGen(new List<string> { typeParsingDirectory });
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var json = cppGen.Generate(tempDir, GetClangArgs());
        Assert.That(json, Is.Not.Null);
        var intType = json.OfType<BuiltinDataType>().FirstOrDefault(dt => dt.builtinKind == BuiltinDataType.Kind.Int32);
        var floatType = json.OfType<BuiltinDataType>().FirstOrDefault(dt => dt.builtinKind == BuiltinDataType.Kind.Float);
        var doubleType = json.OfType<BuiltinDataType>().FirstOrDefault(dt => dt.builtinKind == BuiltinDataType.Kind.Double);
        var charType = json.OfType<BuiltinDataType>().FirstOrDefault(dt => dt.builtinKind == BuiltinDataType.Kind.Int8);
        var boolType = json.OfType<BuiltinDataType>().FirstOrDefault(dt => dt.builtinKind == BuiltinDataType.Kind.Bool);
        var longType = json.OfType<BuiltinDataType>().FirstOrDefault(dt => dt.builtinKind == BuiltinDataType.Kind.Int64);
        var structNode = json.OfType<Record>().FirstOrDefault(r => r.name == "VariousTypes");
        Assert.That(intType, Is.Not.Null);
        Assert.That(floatType, Is.Not.Null);
        Assert.That(doubleType, Is.Not.Null);
        Assert.That(charType, Is.Not.Null);
        Assert.That(boolType, Is.Not.Null);
        Assert.That(longType, Is.Not.Null);
        Assert.That(structNode, Is.Not.Null);
        //Verify the struct is correctly linked to its fields
        Assert.That(structNode.fields, Has.Count.EqualTo(11));
        var fieldInt = structNode.fields.FirstOrDefault(f => f.name == "myInt");
        var fieldFloat = structNode.fields.FirstOrDefault(f => f.name == "myFloat");
        var fieldDouble = structNode.fields.FirstOrDefault(f => f.name == "myDouble");
        var fieldChar = structNode.fields.FirstOrDefault(f => f.name == "myChar");
        var fieldBool = structNode.fields.FirstOrDefault(f => f.name == "myBool");
        var fieldLong = structNode.fields.FirstOrDefault(f => f.name == "myLong");
        Assert.That(fieldInt, Is.Not.Null);
        var qualifiedTypeInt = json[fieldInt.qualifiedType] as QualifiedType;
        Assert.That(qualifiedTypeInt, Is.Not.Null);
        Assert.That(qualifiedTypeInt.dataType, Is.EqualTo(intType.index));
        Assert.That(fieldFloat, Is.Not.Null);
        var qualifiedTypeFloat = json[fieldFloat.qualifiedType] as QualifiedType;
        Assert.That(qualifiedTypeFloat, Is.Not.Null);
        Assert.That(qualifiedTypeFloat.dataType, Is.EqualTo(floatType.index));
        Assert.That(fieldDouble, Is.Not.Null);
        var qualifiedTypeDouble = json[fieldDouble.qualifiedType] as QualifiedType;
        Assert.That(qualifiedTypeDouble, Is.Not.Null);
        Assert.That(qualifiedTypeDouble.dataType, Is.EqualTo(doubleType.index));
        Assert.That(fieldChar, Is.Not.Null);
        var qualifiedTypeChar = json[fieldChar.qualifiedType] as QualifiedType;
        Assert.That(qualifiedTypeChar, Is.Not.Null);
        Assert.That(qualifiedTypeChar.dataType, Is.EqualTo(charType.index));
        Assert.That(fieldBool, Is.Not.Null);
        var qualifiedTypeBool = json[fieldBool.qualifiedType] as QualifiedType;
        Assert.That(qualifiedTypeBool, Is.Not.Null);
        Assert.That(qualifiedTypeBool.dataType, Is.EqualTo(boolType.index));
        Assert.That(fieldLong, Is.Not.Null);
        var qualifiedTypeLong = json[fieldLong.qualifiedType] as QualifiedType;
        Assert.That(qualifiedTypeLong, Is.Not.Null);
        Assert.That(qualifiedTypeLong.dataType, Is.EqualTo(longType.index));

        //Now verify a few more types AI wasn't able to cover
        var variantType = json.OfType<RecordDataType>().FirstOrDefault(r => r.name.StartsWith("std::variant"));
        Assert.That(variantType, Is.Not.Null);
        var fieldVariant = structNode.fields.FirstOrDefault(f => f.name == "myVariant");
        Assert.That(fieldVariant, Is.Not.Null);
        var qualifiedTypeVariant = json[fieldVariant.qualifiedType] as QualifiedType;
        Assert.That(qualifiedTypeVariant, Is.Not.Null);
        Assert.That(qualifiedTypeVariant.dataType, Is.EqualTo(variantType.index));

        var sharedPtrType = json.OfType<RecordDataType>().FirstOrDefault(r => r.name.StartsWith("std::shared_ptr"));
        Assert.That(sharedPtrType, Is.Not.Null);
        var fieldSharedPtr = structNode.fields.FirstOrDefault(f => f.name == "mySharedPtr");
        Assert.That(fieldSharedPtr, Is.Not.Null);
        var qualifiedTypeSharedPtr = json[fieldSharedPtr.qualifiedType] as QualifiedType;
        Assert.That(qualifiedTypeSharedPtr, Is.Not.Null);
        Assert.That(qualifiedTypeSharedPtr.dataType, Is.EqualTo(sharedPtrType.index));

        var functionType = json.OfType<FunctionDataType>().FirstOrDefault(dt => true);  //Any function will do, there's only one
        Assert.That(functionType, Is.Not.Null);
        var fieldFunction = structNode.fields.FirstOrDefault(f => f.name == "myFunction");
        Assert.That(fieldFunction, Is.Not.Null);
        var qualifiedTypeFunction = json[fieldFunction.qualifiedType] as QualifiedType;
        Assert.That(qualifiedTypeFunction, Is.Not.Null);
        var functionTypeNode = json[qualifiedTypeFunction.dataType] as RecordDataType;
        Assert.That(functionTypeNode, Is.Not.Null);
        Assert.That(functionTypeNode.templateArgs, Has.Count.EqualTo(1));
        Assert.That(functionTypeNode.templateArgs[0].value, Is.EqualTo(functionType.index));

        var enumType = json.OfType<EnumDataType>().FirstOrDefault(dt => dt.name == "MyEnum");
        Assert.That(enumType, Is.Not.Null);
        var fieldEnum = structNode.fields.FirstOrDefault(f => f.name == "myEnum");
        Assert.That(fieldEnum, Is.Not.Null);
        var qualifiedTypeEnum = json[fieldEnum.qualifiedType] as QualifiedType;
        Assert.That(qualifiedTypeEnum, Is.Not.Null);
        Assert.That(qualifiedTypeEnum.dataType, Is.EqualTo(enumType.index));
    }
}
