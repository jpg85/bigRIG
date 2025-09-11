namespace BigRig.JsonDataTests;

public class Tests
{
    //Very simple test to verify we can parse a json file that is just an empty list
    [Test]
    public void TestEmptyList()
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "[]");
        var data = JsonData.JsonFileHandler.ParseFile(tempFile);
        Assert.That(data, Is.Not.Null);
        Assert.That(data, Is.Empty);
    }
    //Verify we can serialize a list with a single Message object
    [Test]
    public void TestRoundTripSingleMessage()
    {
        var tempFile = Path.GetTempFileName();
        var message = new JsonData.Message
        {
            index = 0,
            name = "version",
            location = new JsonData.Location
            {
                file = "test.h",
                line = 1,
                column = 1
            },
            message = "1.0.0"
        };
        var list = new List<JsonData.Base> { message };
        JsonData.JsonFileHandler.WriteFile(tempFile, list);
        var data = JsonData.JsonFileHandler.ParseFile(tempFile);
        Assert.That(data, Is.Not.Null);
        Assert.That(data, Has.Count.EqualTo(list.Count));
        Assert.That(data[0], Is.TypeOf<JsonData.Message>());
        var parsedMessage = (JsonData.Message)data[0];
        Assert.That(parsedMessage.index, Is.EqualTo(message.index));
        Assert.That(parsedMessage.name, Is.EqualTo(message.name));
        Assert.That(parsedMessage.location.file, Is.EqualTo(message.location.file));
        Assert.That(parsedMessage.message, Is.EqualTo(message.message));
    }
    //Verify we can round trip a single record object
    [Test]
    public void TestRoundTripSingleRecord()
    {
        var tempFile = Path.GetTempFileName();
        var record = new JsonData.Record
        {
            index = 1,
            name = "MyStruct",
            location = new JsonData.Location
            {
                file = "test.h",
                line = 10,
                column = 5
            },
            fields = new List<JsonData.Field>
            {
                new JsonData.Field
                {
                    name = "myField",
                    qualifiedType = 2,
                    access = JsonData.Access.Public,
                    offset = 0,
                }
            },
            functions = new List<int> { 3 },
            isAnonymous = false,
            annotations = new List<JsonData.Annotation>
            {
                new JsonData.Annotation
                {
                    name = "serializable",
                    attributes = new List<string> { "true" }
                }
            },
            comments = new List<string> { "This is my struct" }
        };
        var list = new List<JsonData.Base> { record };
        JsonData.JsonFileHandler.WriteFile(tempFile, list);
        var data = JsonData.JsonFileHandler.ParseFile(tempFile);
        Assert.That(data, Is.Not.Null);
        Assert.That(data, Has.Count.EqualTo(list.Count));
        Assert.That(data[0], Is.TypeOf<JsonData.Record>());
        var parsedRecord = (JsonData.Record)data[0];
        Assert.That(parsedRecord.index, Is.EqualTo(record.index));
        Assert.That(parsedRecord.name, Is.EqualTo(record.name));
        Assert.That(parsedRecord.location.file, Is.EqualTo(record.location.file));
        Assert.That(parsedRecord.fields, Has.Count.EqualTo(record.fields.Count));
        Assert.That(parsedRecord.fields[0].name, Is.EqualTo(record.fields[0].name));
        Assert.That(parsedRecord.fields[0].qualifiedType, Is.EqualTo(record.fields[0].qualifiedType));
        Assert.That(parsedRecord.fields[0].access, Is.EqualTo(record.fields[0].access));
        Assert.That(parsedRecord.functions, Is.EquivalentTo(record.functions));
    }
    //Verify we can round trip a function object
    [Test]
    public void TestRoundTripSingleFunction()
    {
        var tempFile = Path.GetTempFileName();
        var function = new JsonData.Function
        {
            index = 2,
            name = "MyFunction",
            location = new JsonData.Location
            {
                file = "test.h",
                line = 20,
                column = 3
            },
            returnQualifiedType = 4,
            access = JsonData.Access.Public,
            parameters = new List<JsonData.Parameter>
            {
                new JsonData.Parameter
                {
                    name = "param1",
                    qualifiedType = 5,
                }
            },
            modifiers = new List<JsonData.Modifier> { JsonData.Modifier.Static },
            annotations = new List<JsonData.Annotation>
            {
                new JsonData.Annotation
                {
                    name = "deprecated",
                    attributes = new List<string> { "Use NewFunction instead" }
                }
            },
            comments = new List<string> { "This is my function" }
        };
        var list = new List<JsonData.Base> { function };
        JsonData.JsonFileHandler.WriteFile(tempFile, list);
        var data = JsonData.JsonFileHandler.ParseFile(tempFile);
        Assert.That(data, Is.Not.Null);
        Assert.That(data, Has.Count.EqualTo(list.Count));
        Assert.That(data[0], Is.TypeOf<JsonData.Function>());
        var parsedFunction = (JsonData.Function)data[0];
        Assert.That(parsedFunction.index, Is.EqualTo(function.index));
        Assert.That(parsedFunction.name, Is.EqualTo(function.name));
        Assert.That(parsedFunction.location.file, Is.EqualTo(function.location.file));
        Assert.That(parsedFunction.returnQualifiedType, Is.EqualTo(function.returnQualifiedType));
        Assert.That(parsedFunction.access, Is.EqualTo(function.access));
        Assert.That(parsedFunction.parameters, Has.Count.EqualTo(function.parameters.Count));
        Assert.That(parsedFunction.parameters[0].name, Is.EqualTo(function.parameters[0].name));
        Assert.That(parsedFunction.parameters[0].qualifiedType, Is.EqualTo(function.parameters[0].qualifiedType));
        Assert.That(parsedFunction.modifiers, Is.EquivalentTo(function.modifiers));
    }
    //Verify we can round trip a variable object
    [Test]
    public void TestRoundTripSingleVariable()
    {
        var tempFile = Path.GetTempFileName();
        var variable = new JsonData.Variable
        {
            index = 3,
            name = "MyVariable",
            location = new JsonData.Location
            {
                file = "test.h",
                line = 30,
                column = 2
            },
            value = "123",
            qualifiedType = 6,
            access = JsonData.Access.Private,
            comments = new List<string> { "This is my variable" }
        };
        var list = new List<JsonData.Base> { variable };
        JsonData.JsonFileHandler.WriteFile(tempFile, list);
        var data = JsonData.JsonFileHandler.ParseFile(tempFile);
        Assert.That(data, Is.Not.Null);
        Assert.That(data, Has.Count.EqualTo(list.Count));
        Assert.That(data[0], Is.TypeOf<JsonData.Variable>());
        var parsedVariable = (JsonData.Variable)data[0];
        Assert.That(parsedVariable.index, Is.EqualTo(variable.index));
        Assert.That(parsedVariable.name, Is.EqualTo(variable.name));
        Assert.That(parsedVariable.location.file, Is.EqualTo(variable.location.file));
        Assert.That(parsedVariable.access, Is.EqualTo(variable.access));
    }
    //Verify we can round trip a QualifiedType object
    [Test]
    public void TestRoundTripSingleQualifiedType()
    {
        var tempFile = Path.GetTempFileName();
        var qualifiedType = new JsonData.QualifiedType
        {
            index = 4,
            name = "MyDataTypeHolder",
            location = new JsonData.Location
            {
                file = "test.h",
                line = 40,
                column = 4
            },
            dataType = 7,
            qualifiers = new List<JsonData.Qualifier> { JsonData.Qualifier.Output },
            annotations = new List<JsonData.Annotation>
            {
                new JsonData.Annotation
                {
                    name = "nullable",
                    attributes = new List<string>()
                }
            },
        };
        var list = new List<JsonData.Base> { qualifiedType };
        JsonData.JsonFileHandler.WriteFile(tempFile, list);
        var data = JsonData.JsonFileHandler.ParseFile(tempFile);
        Assert.That(data, Is.Not.Null);
        Assert.That(data, Has.Count.EqualTo(list.Count));
        Assert.That(data[0], Is.TypeOf<JsonData.QualifiedType>());
        var parsedQualifiedType = (JsonData.QualifiedType)data[0];
        Assert.That(parsedQualifiedType.index, Is.EqualTo(qualifiedType.index));
        Assert.That(parsedQualifiedType.name, Is.EqualTo(qualifiedType.name));
        Assert.That(parsedQualifiedType.location.file, Is.EqualTo(qualifiedType.location.file));
        Assert.That(parsedQualifiedType.dataType, Is.EqualTo(qualifiedType.dataType));
        Assert.That(parsedQualifiedType.qualifiers, Is.EquivalentTo(qualifiedType.qualifiers));
    }
}
