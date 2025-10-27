using System.ComponentModel;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FindInFile;

public class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp<SearchCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("findinfile");
            config.SetApplicationVersion("1.0.0-dev-verbose-mode");
            config.AddExample(new[] { "--string", "TODO", "--directory", "C:\\Projects,C:\\Source", "--full-path" });
            config.AddExample(new[] { "-s", "Console.WriteLine", "-d", ".,../OtherProject" });
            config.AddExample(new[] { "-s", "error", "-d", ".", "-v" });
        });

        return app.Run(args);
    }
}

public class SearchCommand : Command<SearchCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-s|--string")]
        [Description("The string to search for in files")]
        public required string SearchString { get; set; }

        [CommandOption("-d|--directory")]
        [Description("The directory or directories to search in (searches recursively). Use comma-separated values for multiple directories.")]
        public required string Directory { get; set; }

        [CommandOption("-e|--extensions")]
        [Description("File extensions to search (comma-separated, e.g., '.cs,.txt,.json'). If not specified, searches all files.")]
        public string? Extensions { get; set; }

        [CommandOption("-i|--ignore-case")]
        [Description("Perform case-insensitive search")]
        public bool IgnoreCase { get; set; }

        [CommandOption("--exclude-binary")]
        [Description("Skip binary files (files that appear to contain non-text data)")]
        public bool ExcludeBinary { get; set; } = true;

        [CommandOption("--full-path")]
        [Description("Display full file paths instead of relative paths (automatically enabled when multiple directories are specified)")]
        public bool FullPath { get; set; }

        [CommandOption("-v|--verbose")]
        [Description("Enable verbose output with detailed logging")]
        public bool Verbose { get; set; }

        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(SearchString))
                return ValidationResult.Error("Search string cannot be empty");

            if (string.IsNullOrWhiteSpace(Directory))
                return ValidationResult.Error("Directory cannot be empty");

            var directories = ParseDirectories(Directory);
            foreach (var dir in directories)
            {
                if (!System.IO.Directory.Exists(dir))
                    return ValidationResult.Error($"Directory '{dir}' does not exist");
            }

            return ValidationResult.Success();
        }

        public static List<string> ParseDirectories(string directoryString)
        {
            return directoryString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(dir => dir.Trim())
                .Where(dir => !string.IsNullOrEmpty(dir))
                .ToList();
        }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var searchOptions = new SearchOptions
        {
            SearchString = settings.SearchString,
            Directories = Settings.ParseDirectories(settings.Directory),
            Extensions = ParseExtensions(settings.Extensions),
            IgnoreCase = settings.IgnoreCase,
            ExcludeBinary = settings.ExcludeBinary,
            FullPath = settings.FullPath,
            Verbose = settings.Verbose
        };

        AnsiConsole.MarkupLine($"[green]Searching for:[/] [yellow]{settings.SearchString}[/]");
        AnsiConsole.MarkupLine($"[green]Directories:[/] [blue]{string.Join(", ", searchOptions.Directories)}[/]");
        AnsiConsole.MarkupLine($"[green]Case sensitive:[/] {(settings.IgnoreCase ? "[red]No[/]" : "[green]Yes[/]")}");

        if (settings.Verbose)
        {
            AnsiConsole.MarkupLine($"[green]Verbose mode:[/] [cyan]Enabled[/]");
        }

        if (searchOptions.Extensions?.Any() == true)
        {
            AnsiConsole.MarkupLine($"[green]Extensions:[/] [cyan]{string.Join(", ", searchOptions.Extensions)}[/]");
        }

        AnsiConsole.WriteLine();

        var results = new List<SearchResult>();
        var totalFiles = 0;

        AnsiConsole.Status()
            .Start("Searching files...", ctx =>
            {
                try
                {
                    SearchDirectories(searchOptions, results, ref totalFiles, ctx);
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                    return;
                }
            });

        DisplayResults(results, totalFiles, searchOptions);
        return 0;
    }

    private static HashSet<string>? ParseExtensions(string? extensions)
    {
        if (string.IsNullOrWhiteSpace(extensions))
            return null;

        return extensions.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ext => ext.Trim())
            .Where(ext => !string.IsNullOrEmpty(ext))
            .Select(ext => ext.StartsWith('.') ? ext : $".{ext}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static void SearchDirectories(SearchOptions options, List<SearchResult> results, ref int totalFiles, StatusContext ctx)
    {
        foreach (var directory in options.Directories)
        {
            ctx.Status($"Searching directory: {directory}");
            SearchDirectory(directory, options, results, ref totalFiles, ctx);
        }
    }

    private static void SearchDirectory(string directory, SearchOptions options, List<SearchResult> results, ref int totalFiles, StatusContext ctx)
    {
        try
        {
            var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

            if (options.Verbose)
            {
                AnsiConsole.MarkupLine($"[dim]Found {files.Length} files in {directory}[/]");
            }

            foreach (var file in files)
            {
                ctx.Status($"Searching: {Path.GetFileName(file)}");
                totalFiles++;

                if (options.Extensions != null && !options.Extensions.Contains(Path.GetExtension(file)))
                {
                    if (options.Verbose)
                    {
                        AnsiConsole.MarkupLine($"[dim]Skipping {file} (extension filter)[/]");
                    }
                    continue;
                }

                try
                {
                    if (options.ExcludeBinary && IsBinaryFile(file))
                    {
                        if (options.Verbose)
                        {
                            AnsiConsole.MarkupLine($"[dim]Skipping {file} (binary file)[/]");
                        }
                        continue;
                    }

                    var matchesBefore = results.Count;
                    SearchFile(file, options, results);
                    var matchesFound = results.Count - matchesBefore;

                    if (options.Verbose && matchesFound > 0)
                    {
                        AnsiConsole.MarkupLine($"[green]Found {matchesFound} match(es) in {file}[/]");
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error reading file {file}: {ex.Message}[/]");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error accessing directory {directory}: {ex.Message}[/]");
        }
    }

    private static void SearchFile(string filePath, SearchOptions options, List<SearchResult> results)
    {
        var lines = File.ReadAllLines(filePath);
        var comparison = options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var index = line.IndexOf(options.SearchString, comparison);

            if (index >= 0)
            {
                results.Add(new SearchResult
                {
                    FilePath = filePath,
                    LineNumber = i + 1,
                    LineContent = line,
                    MatchIndex = index
                });
            }
        }
    }

    private static bool IsBinaryFile(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var buffer = new byte[1024];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);

            // Check for null bytes which typically indicate binary files
            return buffer.Take(bytesRead).Any(b => b == 0);
        }
        catch
        {
            return false; // If we can't read it, assume it's not binary
        }
    }

    private static void DisplayResults(List<SearchResult> results, int totalFiles, SearchOptions options)
    {
        AnsiConsole.WriteLine();

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]No matches found for '{options.SearchString}' in {totalFiles} files.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[green]Found {results.Count} matches in {results.Select(r => r.FilePath).Distinct().Count()} files (searched {totalFiles} total files)[/]");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.AddColumn("File");
        table.AddColumn("Line");
        table.AddColumn("Content");

        var groupedResults = results.GroupBy(r => r.FilePath).OrderBy(g => g.Key);

        foreach (var fileGroup in groupedResults)
        {
            string displayPath;

            // Default to full path if multiple directories are specified, unless explicitly overridden
            bool shouldShowFullPath = options.FullPath || options.Directories.Count > 1;

            if (shouldShowFullPath)
            {
                displayPath = fileGroup.Key;
            }
            else
            {
                // Try to find the best base directory for relative path calculation
                var bestBaseDir = options.Directories
                    .Where(dir => fileGroup.Key.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(dir => dir.Length)
                    .FirstOrDefault();

                displayPath = bestBaseDir != null
                    ? Path.GetRelativePath(bestBaseDir, fileGroup.Key)
                    : fileGroup.Key;
            }

            var isFirst = true;

            foreach (var result in fileGroup.OrderBy(r => r.LineNumber))
            {
                var fileName = isFirst ? $"[blue]{displayPath}[/]" : "";
                var lineNumber = $"[dim]{result.LineNumber}[/]";

                // Highlight the match in the line content
                var highlightedContent = HighlightMatch(result.LineContent, options.SearchString, result.MatchIndex, options.IgnoreCase);

                table.AddRow(fileName, lineNumber, highlightedContent);
                isFirst = false;
            }
        }

        AnsiConsole.Write(table);
    }

    private static string HighlightMatch(string content, string searchString, int matchIndex, bool ignoreCase)
    {
        var beforeMatch = content.Substring(0, matchIndex);
        var match = content.Substring(matchIndex, searchString.Length);
        var afterMatch = content.Substring(matchIndex + searchString.Length);

        // Escape markup characters
        beforeMatch = beforeMatch.Replace("[", "[[").Replace("]", "]]");
        match = match.Replace("[", "[[").Replace("]", "]]");
        afterMatch = afterMatch.Replace("[", "[[").Replace("]", "]]");

        return $"{beforeMatch}[black on yellow]{match}[/]{afterMatch}";
    }
}

public class SearchOptions
{
    public required string SearchString { get; set; }
    public required List<string> Directories { get; set; }
    public HashSet<string>? Extensions { get; set; }
    public bool IgnoreCase { get; set; }
    public bool ExcludeBinary { get; set; }
    public bool FullPath { get; set; }
    public bool Verbose { get; set; }
}

public class SearchResult
{
    public required string FilePath { get; set; }
    public int LineNumber { get; set; }
    public required string LineContent { get; set; }
    public int MatchIndex { get; set; }
}
