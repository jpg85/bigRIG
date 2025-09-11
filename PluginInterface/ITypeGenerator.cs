namespace BigRig.PluginInterface;

/// <summary>
/// Interface for type generators.
/// Implement this interface to create a generator for specific types.
/// Note, for wrapping types when extending/adding an additional language, it may be useful to re-use an existing type generator.
/// To do this, simply return a higher priority than those generators, and call them from your own TypeGenerator implementation.
/// Your type generator will need to keep a stack value or add an annotation indicating to disable itself on that recursive call.
/// </summary>
public interface ITypeGenerator
{
    /// <summary>
    /// Gets the priority of the type generator.
    /// Higher priority generators are chosen first when multiple generators can handle the same type.
    /// </summary>
    public int GetPriority(IGeneratorControl control, JsonData.Base node, List<JsonData.Annotation> annotations);
    /// <summary>
    /// Generates a type holder for the type generator.
    /// </summary>
    public ITypeHolder GenerateType(IGeneratorControl control, JsonData.Base node, List<JsonData.Annotation> annotations);
}