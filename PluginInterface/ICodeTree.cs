namespace BigRig.PluginInterface;

/// <summary>
/// Represents the structure of the code to be generated.
/// This tree follows the hierarchy of namespaces, classes, and structures.
/// Dynamic casting is used to determine the type of each node.
/// </summary>
public interface ICodeTree
{
    public string Name { get; }
}

/// <summary>
/// Represents a namespace node in the code tree.
/// A namespace can contain other namespaces, classes, and structures.
/// </summary>
public interface NamespaceNode : ICodeTree
{
    public List<ICodeTree> Children { get; }
}
/// <summary>
/// Represents a class node in the code tree.
/// A class can contain methods.
/// </summary>
public interface ClassNode : ICodeTree
{
    public List<FunctionNode> Methods { get; }
    public List<JsonData.Annotation> Annotations { get; }
}
[Flags]
public enum ParameterQualifier
{
    None = 0x0,
    Const = 0x1,
    Reference = 0x2,
    Output = 0x4
}
/// <summary>
/// Represents a parameter in a function overload.
/// A parameter has a name, type, and qualifiers.
/// A return re-uses this, but without a name
/// </summary>
public class Parameter
{
    public string Name { get; } = string.Empty;
    public ITypeHolder Type { get; } = null!;
    public ParameterQualifier Qualifiers { get; } = ParameterQualifier.None;
}
/// <summary>
/// Represents a function overload, which includes parameter types and return type.
/// </summary>
public class FunctionOverload
{
    public List<Parameter> Parameters { get; set; } = new();
    public Parameter ReturnType { get; set; } = null!;
    public bool IsConst { get; } = false;
    public List<JsonData.Annotation> Annotations { get; } = new();
}
/// <summary>
/// Represents a function node in the code tree.
/// A function can have multiple overloads.
/// </summary>
public interface FunctionNode : ICodeTree
{
    public List<FunctionOverload> Overloads { get; }
}
/// <summary>
/// Represents a type holder node in the code tree.
/// A type holder node can be used to generate code within a particular location in the tree.
/// </summary>
public interface TypeHolderNode : ICodeTree
{
    public WriteLanguage Delegate { get; }
}
