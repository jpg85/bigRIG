namespace BigRig.PluginInterface;

/// <summary>
/// This interface is provided by the library to query information about the code being generated.
/// </summary>
public interface IGeneratorControl
{
    /// <summary>
    /// Gets a node from the JSON data by its index.
    /// </summary>
    /// <param name="idx">The index of the node to retrieve</param>
    /// <returns>The JSON node at the specified index</returns>
    public JsonData.Base GetNode(uint idx);
    /// <summary>
    /// Gets the root of the code tree structure.
    /// </summary>
    /// <returns>The root of the code tree</returns>
    public ICodeTree GetCodeTree();
    /// <summary>
    /// Generates a type holder for a given JSON node and its annotations.
    /// </summary>
    /// <param name="node">The JSON node to generate a type for</param>
    /// <param name="annotations">The annotations associated with the node</param>
    /// <returns>The generated type holder</returns>
    public ITypeHolder GenerateType(JsonData.Base node, List<JsonData.Annotation> annotations);
}
