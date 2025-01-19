using System.CommandLine;
using System.IO;
using System.Linq;

var bundleCommand = new Command("bundle", "Bundle code files into a single file");
bundleCommand.AddAlias("b");


var languageOption = new Option<string>(
    "--language",
    "Comma-separated list of programming languages to include in the bundle. Use 'all' to include all code files.")
{
    IsRequired = true
};
languageOption.AddAlias("-l");


var bundleOption = new Option<FileInfo>(
    "--output",
    "File path and name")
{
    IsRequired = true
};
bundleOption.AddAlias("-d");


var noteOption = new Option<bool>(
    "--note",
    "Include notes on source and file names.");
noteOption.AddAlias("-n");


var sortOption = new Option<bool>(
    "--sort",
    "Sort files in alphabetical order if true, sort by file type if false.");
sortOption.AddAlias("-s");


var removeEmptyLinesOption = new Option<bool>(
    "--remove-empty-lines",
    "Remove empty lines from the files.");
removeEmptyLinesOption.AddAlias("-r");


var authorOption = new Option<string>(
    "--author",
    "Name of the author to include in the bundle.");
authorOption.AddAlias("-a");


bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(bundleOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);

bundleCommand.SetHandler((string languages, FileInfo output, bool note, bool sort, bool removeEmptyLines, string author) =>
{
    try
    {
        // Split the languages string into a list
        var languagesList = languages.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(lang => lang.Trim())
                                      .ToList();

        // Validate the language options
        var validLanguages = new[] { "c++", "c#", "javascript", "java", "python", "react", "php", "c", "assembly", "all" };
        foreach (var language in languagesList)
        {
            if (!validLanguages.Contains(language.ToLower()))
            {
                Console.WriteLine($"Error: Unsupported language '{language}'. Please use one of the following: {string.Join(", ", validLanguages)}");
                return;
            }
        }

        // Validate the output file path
        if (output == null || string.IsNullOrWhiteSpace(output.FullName))
        {
            Console.WriteLine("Error: Output file path is required.");
            return;
        }

        using (var writer = new StreamWriter(output.FullName))
        {
            if (!string.IsNullOrWhiteSpace(author))
            {
                writer.WriteLine($"// Author: {author}");
            }

            var codeExtensions = languagesList.SelectMany(GetCodeExtensions).Distinct().ToArray();
            var ignoredDirectories = new[] { "bin", "debug", "obj" };

            var allFiles = codeExtensions.SelectMany(extension =>
                Directory.EnumerateFiles(Directory.GetCurrentDirectory(), extension, SearchOption.AllDirectories)
                .Where(file => !ignoredDirectories.Any(dir => file.Contains(Path.Combine(Directory.GetCurrentDirectory(), dir))))
            ).ToList();

            allFiles = sort ? allFiles.OrderBy(Path.GetFileName).ToList() : allFiles.OrderBy(Path.GetExtension).ToList();

            foreach (var file in allFiles)
            {
                if (note)
                {
                    var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                    writer.WriteLine($"// Source: {relativePath}");
                }

                var fileContent = File.ReadAllText(file);

                if (removeEmptyLines)
                {
                    fileContent = string.Join(Environment.NewLine, fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
                }

                writer.WriteLine(fileContent);
                Console.WriteLine($"Added {file} to the bundle.");
            }
        }
        Console.WriteLine($"Bundled files into {output.FullName}");
    }
    catch (DirectoryNotFoundException)
    {
        Console.WriteLine("Error: Directory not found");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}, languageOption, bundleOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command");



createRspCommand.SetHandler(async () =>
{
    Console.WriteLine("Please enter the values for the following options:");

    Console.Write("Language (comma-separated): ");
    string language = Console.ReadLine();

    Console.Write("Output file path: ");
    string output = Console.ReadLine();

    Console.Write("Include notes (true/false): ");
    bool note = bool.Parse(Console.ReadLine());

    Console.Write("Sort files (true/false): ");
    bool sort = bool.Parse(Console.ReadLine());

    Console.Write("Remove empty lines (true/false): ");
    bool removeEmptyLines = bool.Parse(Console.ReadLine());

    Console.Write("Author: ");
    string author = Console.ReadLine();

    // Create the full command line
    string commandLine = $"fib bundle --language {language} --output {output} --note {note} --sort {sort} --remove-empty-lines {removeEmptyLines} --author \"{author}\"";

    // Save the command to a response file
    string responseFileName = "responseFile.rsp";
    await File.WriteAllTextAsync(responseFileName, commandLine);

    Console.WriteLine($"Response file created: {responseFileName}");
});



// Add createRspCommand to root command
var rootCommand = new RootCommand("Root command for file Bundler CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);

await rootCommand.InvokeAsync(args);

string[] GetCodeExtensions(string language)
{
    language = language.ToLower() switch
    {
        "c++" => "cpp",
        "c#" => "csharp",
        "javascript" => "javascript",
        "java" => "java",
        "python" => "python",
        "react" => "react",
        "php" => "php",
        "c" => "c",
        "assembly" => "assembly",
        "all" => "all",
        _ => language
    };

    if (language == "all")
    {
        return new[] {
            "*.cs", "*.js", "*.py", "*.java", "*.cpp", "*.h", "*.rb", "*.go",
            "*.php", "*.html", "*.css", "*.swift", "*.kt", "*.ts", "*.sql",
            "*.xml", "*.pl", "*.r", "*.lua", "*.dart", "*.scala", "*.groovy", "*.clj", "*.asm", "*.m", "*.v", "*.verilog","*.docx"
        };
    }

    var languageExtensions = new Dictionary<string, string[]>
    {
        { "csharp", new[] { "*.cs", "*.html", "*.css" } },
        { "javascript", new[] { "*.js", "*.html", "*.css" } },
        { "python", new[] { "*.py" } },
        { "java", new[] { "*.java", "*.xml", "*.sql" } },
        { "react", new[] { "*.js", "*.jsx", "*.tsx" } },
        { "cpp", new[] { "*.cpp", "*.h" } },
        { "php", new[] { "*.php", "*.html", "*.css" } },
        { "c", new[] { "*.c", "*.h" } },
        { "assembly", new[] { "*.asm" } }
    };

    if (languageExtensions.TryGetValue(language, out var extensions))
    {
        return extensions;
    }

    throw new ArgumentException("Unsupported language");
}
