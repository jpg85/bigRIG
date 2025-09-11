namespace BigRig.JsonData;
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
    Function,
    Template,
}
public enum Qualifier
{
    Const,
    Reference,
    Output,
    Pointer
}
public enum DiagnosticSeverity
{
    Ignored = 0,
    Note = 1,
    Warning = 2,
    Error = 3,
    Fatal = 4,
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
    public int qualifiedType { get; set; }
    public uint offset { get; set; }
    public Access access { get; set; }
}
public class Parameter
{
    public string name { get; set; } = string.Empty;
    public int qualifiedType { get; set; }
}
public class EnumeratorField
{
    public string name { get; set; } = string.Empty;
    public long value { get; set; }
}
public class TemplateArgument
{
    public enum Kind
    {
        Unknown,
        Type,
        Integral,
    }
    public Kind kind { get; set; } = Kind.Unknown;
    /// <summary>
    /// If kind is Type, this is the index of the DataType
    /// If kind is Integral, this is the value of the integral
    /// Otherwise, it is -1
    /// </summary>
    public long value { get; set; }
}

[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
[JsonDerivedType(typeof(Message), "Message")]
[JsonDerivedType(typeof(Record), "Record")]
[JsonDerivedType(typeof(Function), "Function")]
[JsonDerivedType(typeof(Variable), "Variable")]
[JsonDerivedType(typeof(QualifiedType), "QualifiedType")]
[JsonDerivedType(typeof(DataType), "DataType")]
[JsonDerivedType(typeof(UnknownDataType), "UnknownDataType")]
[JsonDerivedType(typeof(BuiltinDataType), "BuiltinDataType")]
[JsonDerivedType(typeof(RecordDataType), "RecordDataType")]
[JsonDerivedType(typeof(EnumDataType), "EnumDataType")]
[JsonDerivedType(typeof(FunctionDataType), "FunctionDataType")]
public class Base
{
    public int index { get; set; }
    public string name { get; set; } = string.Empty;
    public Location location { get; set; } = new Location();
}
public class Message : Base
{
    public string message { get; set; } = string.Empty;
    public string category { get; set; } = string.Empty;
    public DiagnosticSeverity severity { get; set; }
}
public class RecordBase
{
    public int baseRecord { get; set; }
    public bool isVirtual { get; set; }
    public Access access { get; set; }
}
public class Record : Base
{
    public List<Field> fields { get; set; } = new List<Field>();
    public List<int> functions { get; set; } = new List<int>();
    public List<RecordBase> bases { get; set; } = new List<RecordBase>();
    public bool isAnonymous { get; set; }
    public List<Annotation> annotations { get; set; } = new List<Annotation>();
    public List<string> comments { get; set; } = new List<string>();
}
public class Function : Base
{
    public int returnQualifiedType { get; set; }
    public Access access { get; set; }
    public List<Parameter> parameters { get; set; } = new List<Parameter>();
    public List<Modifier> modifiers { get; set; } = new List<Modifier>();
    public List<Annotation> annotations { get; set; } = new List<Annotation>();
    public List<string> comments { get; set; } = new List<string>();
}
public class Variable : Base
{
    public required string value { get; set; } = string.Empty;
    public int qualifiedType { get; set; }
    public Access access { get; set; }
    public List<string> comments { get; set; } = new List<string>();
}
public class DataType : Base
{
}
public class UnknownDataType : DataType
{
}
public class BuiltinDataType : DataType
{
    public enum Kind
    {
        Void,
        Nullptr,
        Unsupported,
        Bool,
        Int8,
        UInt8,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Int128,
        UInt128,
        Float,
        Float16,
        BFloat16,
        Double,
        LongDouble,
        Float128,
    }
    public Kind builtinKind { get; set; } = Kind.Void;
}
public class RecordDataType : DataType
{
    public int recordType { get; set; }
    public bool isExternal { get { return recordType == -1; } }
    public List<TemplateArgument> templateArgs { get; set; } = new List<TemplateArgument>();
}
public class EnumDataType : DataType
{
    public int underlyingType { get; set; }
    public List<EnumeratorField> enumerators { get; set; } = new List<EnumeratorField>();
}
public class FunctionDataType : DataType
{
    public int returnType { get; set; }
    public List<int> argumentTypes { get; set; } = new List<int>();
    public bool isVariadic { get; set; }
}
public class QualifiedType : Base
{
    public int dataType { get; set; }
    public List<Qualifier> qualifiers { get; set; } = new List<Qualifier>();
    public List<Annotation> annotations { get; set; } = new List<Annotation>();
}

public static class JsonFileHandler
{
    public static List<Base> ParseFile(string fileName)
    {
        var json = File.ReadAllText(fileName);
        var options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
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
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        var json = JsonSerializer.SerializeToUtf8Bytes(data, options);
        File.WriteAllBytes(fileName, json);
    }
}