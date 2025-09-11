namespace BigRig.PluginInterface;

/// <summary>
/// Static class containing constants for supported languages.
/// Other languages can be added, but the built in type generators only support these.
/// </summary>
static class Language
{
    public const string Cpp = "Cpp";
    public const string CSharp = "CSharp";
    public const string Python = "Python";
    public const string Proto = "Proto";
}

/// <summary>
/// Interface for language-specific code generators.
/// Implement this interface to create a generator for a specific programming language.
/// </summary>
public interface ILanguageGenerator
{
    public string LanguageName { get; }
    /// <summary>
    /// Generates the configuration code for the language.
    /// </summary>
    /// <param name="control">The generator control.</param>
    /// <param name="outputPath">The output path for the generated code.</param>
    public void Configure(IGeneratorControl control, string outputPath);
    /// <summary>
    /// Generates the code based on the provided control.
    /// </summary>
    /// <param name="control">The generator control.</param>
    /// <param name="outputPath">The output path for the generated code.</param>
    public void GenerateCode(IGeneratorControl control, string outputPath);
}
