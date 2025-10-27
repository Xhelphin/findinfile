# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FindInFile is a command-line text search tool built with C# .NET 9.0 and Spectre.Console. It searches for text within files across directories with rich terminal output and real-time progress updates.

## Build and Development Commands

### Build
```bash
dotnet restore
dotnet build
```

### Run the application
```bash
dotnet run --project FindInFile -- --string "search_term" --directory "path/to/directory"
```

### Create executable
```bash
# Windows x64 self-contained
dotnet publish -c Release -r win-x64 --self-contained
```

### Testing the application manually
```bash
# Basic search
dotnet run --project FindInFile -- -s "TODO" -d "."

# Multiple directories with extensions filter
dotnet run --project FindInFile -- -s "class" -d ".,../OtherProject" -e ".cs"

# Case-insensitive with full paths
dotnet run --project FindInFile -- -s "error" -d "." -i --full-path

# Verbose mode for detailed logging
dotnet run --project FindInFile -- -s "TODO" -d "." -v
```

## Architecture

### Single-File Design
The entire application is contained in `FindInFile/Program.cs`. There are no separate service layers, utilities, or component directories.

### Core Classes (all in Program.cs)
- `Program`: Entry point that configures the Spectre.Console.Cli CommandApp
- `SearchCommand`: Implements the main search command logic
- `SearchCommand.Settings`: Command-line argument definitions and validation
- `SearchOptions`: Internal data structure for search configuration
- `SearchResult`: Represents a single match (file, line number, content, match position)

### Key Architecture Patterns

**Command-Line Parsing**: Uses Spectre.Console.Cli framework with CommandOption attributes for argument parsing and validation.

**Search Flow**:
1. Settings validation (Program.cs:53-69) checks directories exist
2. Options parsing converts CLI args to SearchOptions
3. SearchDirectories loops through multiple directories
4. SearchDirectory recursively gets all files
5. SearchFile reads lines and finds matches using IndexOf
6. DisplayResults groups by file and renders a Spectre.Console Table

**Path Display Logic**:
- Single directory: shows relative paths by default
- Multiple directories: automatically shows full paths
- Override with `--full-path` flag
- Implementation in DisplayResults (Program.cs:243-260)

**Binary File Detection**: Reads first 1024 bytes and checks for null bytes (Program.cs:201-216)

**Match Highlighting**: Splits line content at match position and wraps match in Spectre.Console markup `[black on yellow]` (Program.cs:280-292)

## Contributing Guidelines

Branch from `develop` (not master) when creating feature branches. Pull requests should target `develop`.
