# FileSearchConsole
# Text Search in Files Program (FileSearchConsole)

## Description
FileSearchConsole is a console utility for searching text in files with multiple options and multilingual interface. The program automatically detects the system language (Russian, Ukrainian, and English are supported) and adapts the user interface accordingly.

## Features
- Text search in files using specified patterns
- Support for recursive search in subfolders
- Case-sensitive or case-insensitive search
- Ability to exclude specific folders from search
- Saving results to a text file or to the desktop
- Support for multiple text encodings (UTF-8, Windows-1251, KOI8-R, etc.)
- Multilingual interface (Russian, Ukrainian, English)
- Can be run both interactively and with command line parameters

## System Requirements
- .NET Framework (version compatible with the used features)
- Operating system: Windows, Linux, or macOS

## Installation
1. Compile the project using Visual Studio or .NET CLI
2. Copy the executable file `FileSearchConsole.exe` to a convenient location

## Usage

### Interactive Mode
Run the program without parameters to work in interactive mode:
```
FileSearchConsole.exe
```

The program will sequentially request:
- Path to the folder for search
- File patterns (separated by semicolons, e.g.: `*.cs;*.txt`)
- Text to search
- Search options (recursive search, case sensitivity, etc.)

### Command Line
The program can be run with command line parameters:

```
FileSearchConsole [options]
```

#### Parameters:
- `-d, --dir <path>` - Folder to search
- `-p, --pattern <pattern>` - File patterns (default: *.cs;*.txt;*.xml)
- `-s, --search <text>` - Text to search
- `-r, --recursive` - Search in subfolders (default: enabled)
- `-nr, --no-recursive` - Do not search in subfolders
- `-c, --case-sensitive` - Case sensitive search (default: disabled)
- `-dt, --desktop` - Save results to desktop
- `-o, --output <file>` - File name for saving results
- `-e, --exclude <folders>` - Exclude folders from search (separator: ;)
- `-h, --help, /?` - Show help

#### Examples:
```
FileSearchConsole -d "C:\Projects" -p "*.cs" -s "class Program" -c
FileSearchConsole --dir "D:\Docs" --pattern "*.txt;*.doc" --search "important" --no-recursive
FileSearchConsole -d "C:\Work" -s "TODO" -e "bin;obj;packages"
```

## Output Format
Search results are saved to a text file in UTF-8 format. Result files contain:
- Header with search parameters
- Command to repeat the search with the same parameters
- List of found matches in the format `file_path(line_number): line_content`
- Search statistics (number of processed files, found matches)

## Implementation Features
- The program automatically detects the system language and adapts the interface
- Various encodings are used for file processing with automatic selection
- When saving results, the date and time of the search are added to the filename
- Exception handling is included for correct operation even with file access errors

## License
GNU General Public License v3.0 (GPL-3.0)

This project is distributed under the GPL-3.0 license, which means:
- You can freely use, modify, and distribute this code
- All derivative works must also be distributed under the GPL-3.0 license
- The full license text is available at: https://www.gnu.org/licenses/gpl-3.0.html

## Author
Vitaly Litvinov

## Contributing
If you want to contribute to the project development, please create an issue or pull request in the project repository.

## Support the Project
If you like this project and want to support its development, you can make a donation via PayPal:
- PayPal: murkir@gmail.com
