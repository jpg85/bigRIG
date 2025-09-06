namespace JsonData;
using System.Text.Json;
using System.Text.Json.Serialization;

public enum Access
{
    Public = 0,
    Protected,
    Private,
}
public enum Modifier
{
    Const,
    Volatile,
    Static,
    Virtual,
    PureVirtual,
}
public enum DataTypeKind
{
    Unknown = 0,
    Builtin,
    Array,
    Record,
    Enum,
}
public enum DataTypeModifier
{
    Const,
    Reference,
    Output,
    Pointer,
}
public class Location
{
    public string file { get; set; } = string.Empty;
    public int line { get; set; }
    public int column { get; set; }
}
public class Annotation
{
    public string name { get; set; } = string.Empty;
    public List<string> attributes { get; set; } = new List<string>();
}
public class Field
{
    public string name { get; set; } = string.Empty;
    public int dataTypeHolder { get; set; }
    public uint offset { get; set; }
    public Access access { get; set; }
}
public class Parameter
{
    public string name { get; set; } = string.Empty;
    public int dataTypeHolder { get; set; }
    public List<Annotation> annotations { get; set; } = new List<Annotation>();
}

[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
[JsonDerivedType(typeof(Version), "Version")]
[JsonDerivedType(typeof(Record), "Record")]
[JsonDerivedType(typeof(Function), "Function")]
[JsonDerivedType(typeof(Variable), "Variable")]
[JsonDerivedType(typeof(DataType), "DataType")]
[JsonDerivedType(typeof(DataTypeHolder), "DataTypeHolder")]
public class Base
{
    public int index { get; set; }
    public string name { get; set; } = string.Empty;
    public Location location { get; set; } = new Location();
}
public class Version : Base
{
    public string versionString { get; set; } = string.Empty;
}
public class Record : Base
{
    public List<Field> fields { get; set; } = new List<Field>();
    public List<int> functions { get; set; } = new List<int>();
    public bool isAnonymous { get; set; }
    public List<Annotation> annotations { get; set; } = new List<Annotation>();
    public List<string> comments { get; set; } = new List<string>();
}
public class Function : Base
{
    public int returnTypeHolder { get; set; }
    public Access access { get; set; }
    public List<Parameter> parameters { get; set; } = new List<Parameter>();
    public List<Modifier> modifiers { get; set; } = new List<Modifier>();
    public List<Annotation> annotations { get; set; } = new List<Annotation>();
    public List<string> comments { get; set; } = new List<string>();
}
public class Variable : Base
{
    public required string value { get; set; } = string.Empty;
    public int dataType { get; set; }
    public Access access { get; set; }
    public List<Modifier> modifiers { get; set; } = new List<Modifier>();
    public List<Annotation> annotations { get; set; } = new List<Annotation>();
    public List<string> comments { get; set; } = new List<string>();
}
public class DataType : Base
{
    //TODO: Fill out the details of this class
    public required DataTypeKind kind { get; set; }
}
public class DataTypeHolder : Base
{
    public int dataType { get; set; }
    public List<DataTypeModifier> modifiers { get; set; } = new List<DataTypeModifier>();
    public List<Annotation> annotations { get; set; } = new List<Annotation>();
}

public static class JsonFileHandler
{
    public static List<Base> ParseFile(string fileName)
    {
        var json = File.ReadAllText(fileName);
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        return JsonSerializer.Deserialize<List<Base>>(json, options) ?? new List<Base>();
    }
    public static void WriteFile(string fileName, List<Base> data)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        var json = JsonSerializer.Serialize(data, options);
        File.WriteAllText(fileName, json);
    }
}