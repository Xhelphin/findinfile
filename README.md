# File Search Console Application

A powerful command-line tool for searching text within files across directories. Built with C# .NET 9.0 and Spectre.Console for a rich terminal experience.

## Features

- 🔍 **Recursive Search**: Search through all files in specified directories and subdirectories
- 📁 **Multiple Directories**: Search across multiple directories simultaneously with comma-separated paths
- 🎯 **Smart Path Display**: Automatically shows full paths for multiple directories, relative paths for single directories
- 🔤 **Case-Insensitive Search**: Optional case-insensitive matching
- 📄 **File Extension Filtering**: Limit search to specific file types
- 🚫 **Binary File Detection**: Automatically skip binary files to avoid false matches
- 🌈 **Rich Output**: Colorized results with highlighted matches and formatted tables
- ⚡ **Real-time Progress**: Live status updates during search operations
- 🛡️ **Error Resilience**: Continues searching even when encountering permission errors

## Installation

### Prerequisites
- .NET 9.0 SDK or later

### Build from Source
```bash
git clone https://github.com/Xhelphin/findinfile.git
cd findinfile
dotnet restore
dotnet build
```

### Create Executable
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Usage

### Basic Syntax
```bash
findinfile --string "search_term" --directory "path/to/directory"
```

### Command Line Options

| Option | Short | Description |
|--------|-------|-------------|
| `--string` | `-s` | The string to search for in files (required) |
| `--directory` | `-d` | Directory or directories to search (required) |
| `--extensions` | `-e` | File extensions to search (comma-separated) |
| `--ignore-case` | `-i` | Perform case-insensitive search |
| `--exclude-binary` | | Skip binary files (enabled by default) |
| `--full-path` | | Display full file paths instead of relative paths |
| `--help` | `-h` | Show help information |

### Examples

#### Basic Search
```bash
# Search for "TODO" in the current directory
findinfile --string "TODO" --directory "."

# Search with short options
findinfile -s "Console.WriteLine" -d "C:\Projects"
```

#### Multiple Directories
```bash
# Search across multiple directories (automatically shows full paths)
findinfile --string "error" --directory "C:\Projects,C:\Source,D:\Code"

# Mix of relative and absolute paths
findinfile -s "function" -d ".,../OtherProject,C:\SharedLibraries"
```

#### File Extension Filtering
```bash
# Search only C# files
findinfile --string "class" --directory "." --extensions ".cs"

# Search multiple file types
findinfile -s "TODO" -d "C:\Projects" -e ".cs,.js,.ts,.json"
```

#### Case-Insensitive Search
```bash
# Find "error" regardless of case
findinfile --string "error" --directory "C:\Logs" --ignore-case
```

#### Force Full Paths
```bash
# Show full paths even for single directory
findinfile --string "config" --directory "." --full-path
```

## Output Format

The application displays results in a formatted table with:
- **File**: The file path (relative or full based on context)
- **Line**: The line number where the match was found
- **Content**: The line content with the search term highlighted

### Sample Output
```
Found 15 matches in 8 files (searched 247 total files)

┌─────────────────────────────────────┬──────┬────────────────────────────────────┐
│ File                                │ Line │ Content                            │
├─────────────────────────────────────┼──────┼────────────────────────────────────┤
│ C:\Projects\MyApp\Program.cs        │ 12   │     // TODO: Add error handling    │
│                                     │ 25   │     Console.WriteLine("TODO");     │
│ C:\Projects\MyApp\Utils\Helper.cs   │ 8    │     // TODO: Optimize this method  │
└─────────────────────────────────────┴──────┴────────────────────────────────────┘
```

## Path Display Logic

- **Single Directory**: Shows relative paths for cleaner output
- **Multiple Directories**: Automatically shows full paths to identify source directory
- **Override**: Use `--full-path` to force full paths in any scenario

## Error Handling

The application gracefully handles common issues:
- **Permission Denied**: Logs the error and continues searching other accessible files
- **Invalid Directories**: Validates all directories before starting the search
- **Binary Files**: Automatically detected and skipped by default
- **Large Files**: Processes files efficiently without loading entire contents into memory

## Performance Considerations

- Binary file detection prevents processing of non-text files
- Recursive directory traversal optimized for large directory structures
- Memory-efficient file processing handles large codebases
- Real-time progress updates for long-running searches

## Contributing

1. Fork the repository
2. Create a feature branch from the develop branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Dependencies

- [Spectre.Console.Cli](https://spectreconsole.net/) - Command-line interface and rich terminal output
- .NET 9.0 - Target framework

## Troubleshooting

### Common Issues

**"Directory does not exist"**
- Verify the directory path is correct
- Use quotes around paths with spaces
- Check that you have read permissions

**"Access denied" errors**
- The application will continue searching accessible files
- Run with elevated permissions if needed for system directories

**No results found**
- Verify the search string is correct
- Try case-insensitive search with `--ignore-case`
- Check if files are being filtered by extension

### Getting Help

```bash
findinfile --help
```

To report issues, please visit the [Project Issues Page](https://github.com/Xhelphin/findinfile/issues).