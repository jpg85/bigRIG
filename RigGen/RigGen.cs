using CommandLine;

namespace BigRig.RigGen
{
    class RigGen
    {
        private enum Language
        {
            Cpp,
            CSharp,
            Python
        };
        
        [Verb("genjson", HelpText = "Generate a JSON file from the code structure provided")]
        class GenJson
        {
            [Option("language", Default=Language.Cpp)]
            public required Language Language { get; set; }
            [Option("directory", Required=true)]
            public required IEnumerable<string> Directories { get; set; }
            [Option("outpath", Required=true)]
            public required string OutputPath { get; set; }
            [Value(0)]
            public required IEnumerable<string> AdditionalArgs { get; set; }
        }

        [Verb(name: "gencode", HelpText = "Generate code from the provided JSON file")]
        private class GenCode
        {
            [Option("client_language")]
            public required IEnumerable<Language> ClientLanguages { get; set; }
            [Option("server_language", Default=Language.Cpp)]
            public required Language ServerLanguage { get; set; }
        }
        
        static void Main(string[] args)
        {
            var parser = new Parser(with => { with.EnableDashDash = true; });
            var result = parser.ParseArguments<GenJson, GenCode>(args)
                .WithParsed<GenJson>(Run)
                .WithParsed<GenCode>(Run)
                .WithNotParsed(HandleParseError);
        }

        static void Run(GenJson options)
        {
            //Generating code consists of collecting all files in the provided directory
            //For C++, these files are header files, and these header files are "included" in a single file
            //Other languages may approach this differently
            if (options.Language == Language.Cpp)
            {
                var cppGen = new CppGen(options.Directories);
                var result = cppGen.Generate(options.OutputPath, options.AdditionalArgs);
                JsonData.JsonFileHandler.WriteFile($"{options.OutputPath}/result.json", result);
            }
            else
            {
                throw new ArgumentException("Language must be C++");
            }
        }

        static void Run(GenCode options)
        {
            
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            
        }
    }
}