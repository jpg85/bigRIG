namespace BigRig.PluginInterface;

/// <summary>
/// Represents a type holder that can provide language-specific type information.
/// </summary>
public interface ITypeHolder
{
    /// <summary>
    /// Gets the language-specific type information for the specified language.
    /// </summary>
    /// <param name="language">The language type to get</param>
    /// <returns>The language-specific type information</returns>
    public LanguageType GetLanguage(string language);
    /// <summary>
    /// Optional delegate to write the type in a specific language context.
    /// </summary>
    public WriteLanguage? WriteType { get; }
}
/// <summary>
/// Delegate for writing type information in a specific language.
/// </summary>
/// <param name="writer">The text writer to write to</param>
/// <param name="language">The language type to write for</param>
public delegate void WriteLanguage(TextWriter writer, string language);
/// <summary>
/// Represents language-specific type information.
/// </summary>
public class LanguageType
{
    /// <summary>
    /// Gets the native type name, if it necessary for that language
    /// </summary>
    public string? NativeTypeName { get; }
    /// <summary>
    /// Gets the intermediate type name, if it is necessary for that language
    /// </summary>
    public string? IntermediateTypeName { get; }
    /// <summary>
    /// Gets the native converter name
    /// </summary>
    public string NativeConverter { get; } = string.Empty;
    /// <summary>
    /// Gets the intermediate converter name
    /// </summary>
    public string IntermediateConverter { get; } = string.Empty;
}