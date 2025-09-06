namespace RigGen;

using ClangSharp.Interop;

public class CppGen
{
    public required IEnumerable<string> directories { get; set; }

    List<string> CollectHeaderFiles()
    {
        //First collect all header files in the different directories
        //Do this by recursively walking each directory in the list
        var headerFiles = new List<string>();
        foreach (var directory in directories)
        {
            if (Directory.Exists(directory))
            {
                headerFiles.AddRange(
                    Directory.EnumerateFiles(directory, "*.h", SearchOption.AllDirectories)
                );
                headerFiles.AddRange(
                    Directory.EnumerateFiles(directory, "*.hpp", SearchOption.AllDirectories)
                );
            }
        }
        return headerFiles;
    }

    internal void Generate(string outputDir, IEnumerable<string> additionalArgs)
    {
        var files = CollectHeaderFiles();
        //Write files to a temporary output cpp file, which will then be parsed by libclang
        var tempFile = Path.Combine(outputDir, "temp.cpp");
        using (var writer = new StreamWriter(tempFile))
        {
            writer.WriteLine("#define RIG_GEN");
            foreach (var file in files)
            {
                writer.WriteLine($"#include \"{file.Replace("\\", "/")}\"");
            }
        }

        //Pull the files into libclang
        unsafe
        {
            var index = CXIndex.Create();
            var args = new ReadOnlySpan<string>(additionalArgs.ToArray());
            var unit = CXTranslationUnit.CreateFromSourceFile(index, tempFile, args, null);
            if (unit == null)
            {
                throw new Exception("Failed to create translation unit");
            }
            var location = unit.GetLocation(unit.GetFile(tempFile), 0, 0);
            unit.Cursor.VisitChildren((cursor, _, _) =>
            {
                switch (cursor.Kind)
                {
                    case CXCursorKind.CXCursor_UsingDeclaration:
                        break;
                    case CXCursorKind.CXCursor_TypedefDecl:
                        break;
                    case CXCursorKind.CXCursor_ClassDecl:
                    case CXCursorKind.CXCursor_StructDecl:
                        break;
                    case CXCursorKind.CXCursor_EnumDecl:
                        break;
                    case CXCursorKind.CXCursor_FunctionDecl:
                        break;
                    default:
                        Console.WriteLine($"Skipping Cursor: {cursor.Spelling} - {cursor.KindSpelling}");
                        break;
                }
                return CXChildVisitResult.CXChildVisit_Continue;
            }, new CXClientData(IntPtr.Zero));
        }
    }
}