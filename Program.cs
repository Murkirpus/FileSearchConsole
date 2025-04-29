using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Diagnostics;

namespace FileSearchConsole
{
    class Program
    {
        // Словари для локализации
        private static Dictionary<string, Dictionary<string, string>> _localizedStrings;
        private static string _currentLanguage;

        static void Main(string[] args)
        {
            try
            {
                // Инициализация локализации
                InitializeLocalization();

                // Настройка консоли для правильного отображения символов
                Console.OutputEncoding = Encoding.UTF8; // Выходная кодировка консоли
                Console.InputEncoding = Encoding.UTF8;  // Входная кодировка консоли

                // На Windows могут потребоваться дополнительные настройки консоли
                try
                {
                    // Меняем кодовую страницу консоли для корректного отображения кириллицы
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c chcp 65001",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        }).WaitForExit();
                    }
                }
                catch (Exception)
                {
                    /* Игнорируем ошибки, если команда не выполнилась */
                }

                Console.WriteLine(GetLocalizedString("ProgramTitle"));
                Console.WriteLine("================================");

                // Параметры поиска
                string folderPath = "";
                string filePatterns = "";
                string searchText = "";
                bool recursive = true;
                bool caseSensitive = false;
                bool saveToDesktop = false;
                string resultFileName = $"{GetLocalizedString("ResultsFileName")}.txt";
                List<string> excludeFolders = new List<string>();

                // Обработка аргументов командной строки
                if (args.Length > 0)
                {
                    if (args[0].Equals("/?", StringComparison.OrdinalIgnoreCase) ||
                        args[0].Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                        args[0].Equals("--help", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowHelp();
                        return;
                    }

                    try
                    {
                        ProcessCommandLineArgs(args, ref folderPath, ref filePatterns, ref searchText,
                            ref recursive, ref caseSensitive, ref saveToDesktop, ref resultFileName, excludeFolders);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(GetLocalizedString("ParamsError") + $": {ex.Message}");
                        Console.WriteLine(GetLocalizedString("UseHelpParam"));
                        return;
                    }
                }

                // Интерактивный ввод параметров, если не указаны в командной строке
                if (string.IsNullOrEmpty(folderPath))
                {
                    // Запрос пути к папке
                    Console.Write(GetLocalizedString("EnterFolderPath") + ": ");
                    folderPath = Console.ReadLine();
                    if (!Directory.Exists(folderPath))
                    {
                        Console.WriteLine(GetLocalizedString("FolderNotExist"));
                        Console.ReadLine();
                        return;
                    }
                }

                if (string.IsNullOrEmpty(filePatterns))
                {
                    // Запрос шаблона файлов
                    Console.Write(GetLocalizedString("EnterFilePatterns") + ": ");
                    filePatterns = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(filePatterns))
                    {
                        filePatterns = "*.cs;*.txt;*.xml";
                        Console.WriteLine(GetLocalizedString("UsingDefaultPattern") + $": {filePatterns}");
                    }
                }

                if (string.IsNullOrEmpty(searchText))
                {
                    // Запрос текста для поиска
                    Console.Write(GetLocalizedString("EnterSearchText") + ": ");
                    searchText = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(searchText))
                    {
                        Console.WriteLine(GetLocalizedString("EmptySearchQuery"));
                        Console.ReadLine();
                        return;
                    }
                }

                // Запрос опций поиска, если не указаны в командной строке
                if (args.Length == 0)
                {
                    Console.Write(GetLocalizedString("SearchInSubfolders") + " (y/n): ");
                    recursive = Console.ReadLine().ToLower().StartsWith("y");

                    Console.Write(GetLocalizedString("CaseSensitive") + " (y/n): ");
                    caseSensitive = Console.ReadLine().ToLower().StartsWith("y");

                    // Запрос исключаемых папок
                    Console.Write(GetLocalizedString("EnterExcludeFolders") + ": ");
                    string excludeFoldersInput = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(excludeFoldersInput))
                    {
                        excludeFolders.AddRange(excludeFoldersInput.Split(';').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
                        Console.WriteLine(GetLocalizedString("ExcludedFolders") + $": {string.Join(", ", excludeFolders)}");
                    }

                    // Запрос места для сохранения результатов
                    Console.Write(GetLocalizedString("SaveToDesktop") + " (y/n): ");
                    saveToDesktop = Console.ReadLine().ToLower().StartsWith("y");
                }

                // Получаем путь к рабочему столу если нужно
                string desktopPath = "";
                if (saveToDesktop)
                {
                    desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }

                // Запрос имени файла для сохранения результатов, если не указан в командной строке
                if (args.Length == 0 || string.IsNullOrEmpty(resultFileName))
                {
                    Console.Write(GetLocalizedString("EnterResultFileName") + $" ({GetLocalizedString("DefaultIs")} '{GetLocalizedString("ResultsFileName")}'): ");
                    string userFileName = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(userFileName))
                    {
                        resultFileName = userFileName;
                    }
                }

                // Добавляем .txt если его нет
                if (!resultFileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    resultFileName += ".txt";
                }

                // Добавляем дату и время к имени файла (перед расширением)
                string dateTimeStr = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(resultFileName);
                string fileExt = Path.GetExtension(resultFileName);
                string fileNameWithDateTime = $"{fileNameWithoutExt}_{dateTimeStr}{fileExt}";

                // Формируем полный путь к файлу
                string fullPath;
                if (saveToDesktop)
                {
                    fullPath = Path.Combine(desktopPath, fileNameWithDateTime);
                    Console.WriteLine(GetLocalizedString("ResultsSavedToDesktop") + $": {fileNameWithDateTime}");
                }
                else
                {
                    fullPath = fileNameWithDateTime;
                    Console.WriteLine(GetLocalizedString("ResultsSavedToFile") + $": {fullPath}");
                }

                // Поиск
                Console.WriteLine("\n" + GetLocalizedString("SearchInProgress") + "...");
                int resultsCount = SearchFiles(folderPath, filePatterns.Split(';'), searchText, recursive, caseSensitive, fullPath, excludeFolders, saveToDesktop);

                Console.WriteLine("\n" + GetLocalizedString("SearchCompleted") + $". {GetLocalizedString("MatchesFound")}: {resultsCount}");
                Console.WriteLine(GetLocalizedString("ResultsSavedToPath") + $": {Path.GetFullPath(fullPath)}");
                Console.WriteLine(GetLocalizedString("PressEnter"));
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error: " + ex.Message);
                Console.ReadLine();
            }
        }

        static void InitializeLocalization()
        {
            _localizedStrings = new Dictionary<string, Dictionary<string, string>>();

            // Английский
            var enStrings = new Dictionary<string, string>
            {
                { "ProgramTitle", "Text Search in Files Program" },
                { "ResultsFileName", "search_results" },
                { "ParamsError", "Error in parameters" },
                { "UseHelpParam", "Use /? parameter for help" },
                { "EnterFolderPath", "Enter the folder path for search" },
                { "FolderNotExist", "Folder does not exist!" },
                { "EnterFilePatterns", "Enter file patterns separated by semicolons (for example *.cs;*.txt)" },
                { "UsingDefaultPattern", "Default pattern will be used" },
                { "EnterSearchText", "Enter text to search" },
                { "EmptySearchQuery", "Search query is empty!" },
                { "SearchInSubfolders", "Search in subfolders?" },
                { "CaseSensitive", "Case sensitive?" },
                { "EnterExcludeFolders", "Enter folders to exclude separated by semicolons (leave empty if not required)" },
                { "ExcludedFolders", "Excluded folders" },
                { "SaveToDesktop", "Save results to desktop?" },
                { "EnterResultFileName", "Enter file name for saving results" },
                { "DefaultIs", "default is" },
                { "ResultsSavedToDesktop", "Results will be saved to desktop in file" },
                { "ResultsSavedToFile", "Results will be saved to file" },
                { "SearchInProgress", "Searching" },
                { "SearchCompleted", "Search completed" },
                { "MatchesFound", "Matches found" },
                { "ResultsSavedToPath", "Results saved to file" },
                { "PressEnter", "Press Enter to exit" },
                { "SearchResults", "Search Results" },
                { "SearchFolder", "Search folder" },
                { "SearchQuery", "Search query" },
                { "RecursiveSearch", "Search in subfolders" },
                { "CaseSensitiveSearch", "Case sensitive search" },
                { "FilePatterns", "File patterns" },
                { "Yes", "Yes" },
                { "No", "No" },
                { "FilesFound", "Files found" },
                { "UniqueFiles", "Total unique files" },
                { "Results", "Results" },
                { "FilesProcessed", "Files processed" },
                { "ErrorReadingFile", "Error reading file" },
                { "SearchError", "Search error" },
                { "RepeatSearchCommand", "To repeat the search with the same parameters, use the command" },
                { "SearchDate", "Search date and time" },
                { "Help_Usage", "USAGE:" },
                { "Help_Options", "OPTIONS:" },
                { "Help_Option_Dir", "Folder to search" },
                { "Help_Option_Pattern", "File patterns (default: *.cs;*.txt;*.xml)" },
                { "Help_Option_Search", "Text to search" },
                { "Help_Option_Recursive", "Search in subfolders (default: enabled)" },
                { "Help_Option_NoRecursive", "Do not search in subfolders" },
                { "Help_Option_Case", "Case sensitive search (default: disabled)" },
                { "Help_Option_Desktop", "Save results to desktop" },
                { "Help_Option_Output", "File name for saving results" },
                { "Help_Option_Exclude", "Exclude folders from search (separator: ;)" },
                { "Help_Option_Help", "Show this help" },
                { "Help_Examples", "EXAMPLES:" },
                { "SearchingPattern", "Searching for files by pattern" },
                { "ProcessedFiles", "Processed files" },
                { "Of", "of" },
            };
            _localizedStrings["en"] = enStrings;

            // Русский
            var ruStrings = new Dictionary<string, string>
            {
                { "ProgramTitle", "Программа поиска текста в файлах" },
                { "ResultsFileName", "результаты_поиска" },
                { "ParamsError", "Ошибка в параметрах" },
                { "UseHelpParam", "Используйте параметр /? для получения справки" },
                { "EnterFolderPath", "Введите путь к папке для поиска" },
                { "FolderNotExist", "Папка не существует!" },
                { "EnterFilePatterns", "Введите шаблоны файлов через точку с запятой (например *.cs;*.txt)" },
                { "UsingDefaultPattern", "Будет использован шаблон по умолчанию" },
                { "EnterSearchText", "Введите текст для поиска" },
                { "EmptySearchQuery", "Поисковый запрос пуст!" },
                { "SearchInSubfolders", "Искать в подпапках?" },
                { "CaseSensitive", "Учитывать регистр?" },
                { "EnterExcludeFolders", "Введите папки для исключения через точку с запятой (оставьте пустым, если не требуется)" },
                { "ExcludedFolders", "Исключенные папки" },
                { "SaveToDesktop", "Сохранить результаты на рабочий стол?" },
                { "EnterResultFileName", "Введите имя файла для сохранения результатов" },
                { "DefaultIs", "по умолчанию" },
                { "ResultsSavedToDesktop", "Результаты будут сохранены на рабочий стол в файл" },
                { "ResultsSavedToFile", "Результаты будут сохранены в файл" },
                { "SearchInProgress", "Идет поиск" },
                { "SearchCompleted", "Поиск завершен" },
                { "MatchesFound", "Найдено совпадений" },
                { "ResultsSavedToPath", "Результаты сохранены в файл" },
                { "PressEnter", "Нажмите Enter для выхода" },
                { "SearchResults", "Результаты поиска" },
                { "SearchFolder", "Папка поиска" },
                { "SearchQuery", "Поисковый запрос" },
                { "RecursiveSearch", "Поиск в подпапках" },
                { "CaseSensitiveSearch", "Учет регистра" },
                { "FilePatterns", "Шаблоны файлов" },
                { "Yes", "Да" },
                { "No", "Нет" },
                { "FilesFound", "Найдено файлов" },
                { "UniqueFiles", "Всего уникальных файлов" },
                { "Results", "Результаты" },
                { "FilesProcessed", "Обработано файлов" },
                { "ErrorReadingFile", "Ошибка при чтении файла" },
                { "SearchError", "Ошибка при поиске" },
                { "RepeatSearchCommand", "Для повторения поиска с теми же параметрами используйте команду" },
                { "SearchDate", "Дата и время поиска" },
                { "Help_Usage", "ИСПОЛЬЗОВАНИЕ:" },
                { "Help_Options", "ПАРАМЕТРЫ:" },
                { "Help_Option_Dir", "Папка для поиска" },
                { "Help_Option_Pattern", "Шаблон файлов (по умолчанию: *.cs;*.txt;*.xml)" },
                { "Help_Option_Search", "Текст для поиска" },
                { "Help_Option_Recursive", "Искать в подпапках (по умолчанию: включено)" },
                { "Help_Option_NoRecursive", "Не искать в подпапках" },
                { "Help_Option_Case", "Учитывать регистр (по умолчанию: выключено)" },
                { "Help_Option_Desktop", "Сохранить результаты на рабочий стол" },
                { "Help_Option_Output", "Имя файла для сохранения результатов" },
                { "Help_Option_Exclude", "Исключить папки из поиска (разделитель: ;)" },
                { "Help_Option_Help", "Показать эту справку" },
                { "Help_Examples", "ПРИМЕРЫ:" },
                { "SearchingPattern", "Поиск файлов по шаблону" },
                { "ProcessedFiles", "Обработано файлов" },
                { "Of", "из" },
            };
            _localizedStrings["ru"] = ruStrings;

            // Украинский
            var ukStrings = new Dictionary<string, string>
            {
                { "ProgramTitle", "Програма пошуку тексту у файлах" },
                { "ResultsFileName", "результати_пошуку" },
                { "ParamsError", "Помилка в параметрах" },
                { "UseHelpParam", "Використовуйте параметр /? для отримання довідки" },
                { "EnterFolderPath", "Введіть шлях до папки для пошуку" },
                { "FolderNotExist", "Папка не існує!" },
                { "EnterFilePatterns", "Введіть шаблони файлів через крапку з комою (наприклад *.cs;*.txt)" },
                { "UsingDefaultPattern", "Буде використаний шаблон за замовчуванням" },
                { "EnterSearchText", "Введіть текст для пошуку" },
                { "EmptySearchQuery", "Пошуковий запит порожній!" },
                { "SearchInSubfolders", "Шукати в підпапках?" },
                { "CaseSensitive", "Враховувати регістр?" },
                { "EnterExcludeFolders", "Введіть папки для виключення через крапку з комою (залиште порожнім, якщо не потрібно)" },
                { "ExcludedFolders", "Виключені папки" },
                { "SaveToDesktop", "Зберегти результати на робочий стіл?" },
                { "EnterResultFileName", "Введіть ім'я файлу для збереження результатів" },
                { "DefaultIs", "за замовчуванням" },
                { "ResultsSavedToDesktop", "Результати будуть збережені на робочий стіл у файл" },
                { "ResultsSavedToFile", "Результати будуть збережені у файл" },
                { "SearchInProgress", "Йде пошук" },
                { "SearchCompleted", "Пошук завершено" },
                { "MatchesFound", "Знайдено збігів" },
                { "ResultsSavedToPath", "Результати збережені у файл" },
                { "PressEnter", "Натисніть Enter для виходу" },
                { "SearchResults", "Результати пошуку" },
                { "SearchFolder", "Папка пошуку" },
                { "SearchQuery", "Пошуковий запит" },
                { "RecursiveSearch", "Пошук у підпапках" },
                { "CaseSensitiveSearch", "Врахування регістру" },
                { "FilePatterns", "Шаблони файлів" },
                { "Yes", "Так" },
                { "No", "Ні" },
                { "FilesFound", "Знайдено файлів" },
                { "UniqueFiles", "Всього унікальних файлів" },
                { "Results", "Результати" },
                { "FilesProcessed", "Оброблено файлів" },
                { "ErrorReadingFile", "Помилка при читанні файлу" },
                { "SearchError", "Помилка при пошуку" },
                { "RepeatSearchCommand", "Для повторення пошуку з тими ж параметрами використовуйте команду" },
                { "SearchDate", "Дата і час пошуку" },
                { "Help_Usage", "ВИКОРИСТАННЯ:" },
                { "Help_Options", "ПАРАМЕТРИ:" },
                { "Help_Option_Dir", "Папка для пошуку" },
                { "Help_Option_Pattern", "Шаблон файлів (за замовчуванням: *.cs;*.txt;*.xml)" },
                { "Help_Option_Search", "Текст для пошуку" },
                { "Help_Option_Recursive", "Шукати в підпапках (за замовчуванням: увімкнено)" },
                { "Help_Option_NoRecursive", "Не шукати в підпапках" },
                { "Help_Option_Case", "Враховувати регістр (за замовчуванням: вимкнено)" },
                { "Help_Option_Desktop", "Зберегти результати на робочий стіл" },
                { "Help_Option_Output", "Ім'я файлу для збереження результатів" },
                { "Help_Option_Exclude", "Виключити папки з пошуку (розділювач: ;)" },
                { "Help_Option_Help", "Показати цю довідку" },
                { "Help_Examples", "ПРИКЛАДИ:" },
                { "SearchingPattern", "Пошук файлів за шаблоном" },
                { "ProcessedFiles", "Оброблено файлів" },
                { "Of", "з" },
            };
            _localizedStrings["uk"] = ukStrings;

            // Определяем язык системы
            string cultureName = CultureInfo.CurrentUICulture.Name.ToLower();
            if (cultureName.StartsWith("ru"))
            {
                _currentLanguage = "ru";
            }
            else if (cultureName.StartsWith("uk"))
            {
                _currentLanguage = "uk";
            }
            else
            {
                _currentLanguage = "en"; // По умолчанию английский
            }
        }

        static string GetLocalizedString(string key)
        {
            if (_localizedStrings.ContainsKey(_currentLanguage) &&
                _localizedStrings[_currentLanguage].ContainsKey(key))
            {
                return _localizedStrings[_currentLanguage][key];
            }

            // Если строка не найдена, возвращаем ключ
            return key;
        }

        static void ShowHelp()
        {
            Console.WriteLine(GetLocalizedString("Help_Usage"));
            Console.WriteLine("FileSearchConsole [параметры]");
            Console.WriteLine();
            Console.WriteLine(GetLocalizedString("Help_Options"));
            Console.WriteLine("-d, --dir <путь>        " + GetLocalizedString("Help_Option_Dir"));
            Console.WriteLine("-p, --pattern <шаблон>  " + GetLocalizedString("Help_Option_Pattern"));
            Console.WriteLine("-s, --search <текст>    " + GetLocalizedString("Help_Option_Search"));
            Console.WriteLine("-r, --recursive         " + GetLocalizedString("Help_Option_Recursive"));
            Console.WriteLine("-nr, --no-recursive     " + GetLocalizedString("Help_Option_NoRecursive"));
            Console.WriteLine("-c, --case-sensitive    " + GetLocalizedString("Help_Option_Case"));
            Console.WriteLine("-dt, --desktop          " + GetLocalizedString("Help_Option_Desktop"));
            Console.WriteLine("-o, --output <файл>     " + GetLocalizedString("Help_Option_Output"));
            Console.WriteLine("-e, --exclude <папки>   " + GetLocalizedString("Help_Option_Exclude"));
            Console.WriteLine("-h, --help, /?          " + GetLocalizedString("Help_Option_Help"));
            Console.WriteLine();
            Console.WriteLine(GetLocalizedString("Help_Examples"));
            Console.WriteLine("FileSearchConsole -d \"C:\\Projects\" -p \"*.cs\" -s \"class Program\" -c");
            Console.WriteLine("FileSearchConsole --dir \"D:\\Docs\" --pattern \"*.txt;*.doc\" --search \"важно\" --no-recursive");
            Console.WriteLine("FileSearchConsole -d \"C:\\Work\" -s \"TODO\" -e \"bin;obj;packages\"");
        }

        static void ProcessCommandLineArgs(string[] args, ref string folderPath, ref string filePatterns,
            ref string searchText, ref bool recursive, ref bool caseSensitive, ref bool saveToDesktop,
            ref string resultFileName, List<string> excludeFolders)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();

                switch (arg)
                {
                    case "-d":
                    case "--dir":
                        if (i + 1 < args.Length)
                        {
                            folderPath = args[++i];
                            if (!Directory.Exists(folderPath))
                            {
                                throw new ArgumentException($"{GetLocalizedString("FolderNotExist")}: {folderPath}");
                            }
                        }
                        break;

                    case "-p":
                    case "--pattern":
                        if (i + 1 < args.Length)
                        {
                            filePatterns = args[++i];
                        }
                        break;

                    case "-s":
                    case "--search":
                        if (i + 1 < args.Length)
                        {
                            searchText = args[++i];
                        }
                        break;

                    case "-r":
                    case "--recursive":
                        recursive = true;
                        break;

                    case "-nr":
                    case "--no-recursive":
                        recursive = false;
                        break;

                    case "-c":
                    case "--case-sensitive":
                        caseSensitive = true;
                        break;

                    case "-dt":
                    case "--desktop":
                        saveToDesktop = true;
                        break;

                    case "-o":
                    case "--output":
                        if (i + 1 < args.Length)
                        {
                            resultFileName = args[++i];
                        }
                        break;

                    case "-e":
                    case "--exclude":
                        if (i + 1 < args.Length)
                        {
                            string excludeList = args[++i];
                            excludeFolders.AddRange(excludeList.Split(';').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
                        }
                        break;
                }
            }

            // Проверка обязательных параметров
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new ArgumentException(GetLocalizedString("EnterFolderPath"));
            }

            if (string.IsNullOrEmpty(searchText))
            {
                throw new ArgumentException(GetLocalizedString("EnterSearchText"));
            }

            // Установка значений по умолчанию
            if (string.IsNullOrEmpty(filePatterns))
            {
                filePatterns = "*.cs;*.txt;*.xml";
            }
        }

        // Метод для получения файлов с исключением определенных папок
        static List<string> GetFilesWithExclusions(string rootPath, string searchPattern, List<string> excludeFolders)
        {
            List<string> result = new List<string>();

            try
            {
                if (!Directory.Exists(rootPath))
                {
                    return result;
                }

                // Получить файлы в текущей директории
                string[] files = Directory.GetFiles(rootPath, searchPattern);
                result.AddRange(files);

                // Получить все поддиректории
                string[] directories;
                try
                {
                    directories = Directory.GetDirectories(rootPath);
                }
                catch (Exception)
                {
                    // Если нет доступа к поддиректориям, возвращаем только файлы из текущей директории
                    return result;
                }

                foreach (string directory in directories)
                {
                    try
                    {
                        // Проверяем, не в списке ли исключений данная папка
                        string directoryName = Path.GetFileName(directory);

                        bool shouldExclude = false;
                        foreach (string excluded in excludeFolders)
                        {
                            if (directoryName.Equals(excluded, StringComparison.OrdinalIgnoreCase) ||
                                directory.Equals(excluded, StringComparison.OrdinalIgnoreCase))
                            {
                                shouldExclude = true;
                                break;
                            }
                        }

                        if (!shouldExclude)
                        {
                            // Рекурсивно обрабатываем поддиректорию
                            result.AddRange(GetFilesWithExclusions(directory, searchPattern, excludeFolders));
                        }
                    }
                    catch (Exception)
                    {
                        // Игнорируем ошибки для отдельных поддиректорий
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{GetLocalizedString("SearchError")} {rootPath}: {ex.Message}");
            }

            return result;
        }

        // Вспомогательный метод для получения относительного пути
        static string GetRelativePath(string fullPath, string basePath)
        {
            try
            {
                if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                {
                    string relativePath = fullPath.Substring(basePath.Length);
                    if (relativePath.StartsWith("\\") || relativePath.StartsWith("/"))
                    {
                        relativePath = relativePath.Substring(1);
                    }
                    return relativePath;
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки
            }

            return fullPath;
        }

        static int SearchFiles(string folderPath, string[] filePatterns, string searchText, bool recursive,
            bool caseSensitive, string resultFileName, List<string> excludeFolders = null, bool saveToDesktop = false)
        {
            int totalMatches = 0;
            int processedFiles = 0;

            try
            {
                // Опции поиска
                SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                StringComparison stringComparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                // Создаем файл для записи результатов с явным указанием BOM для UTF-8
                using (StreamWriter writer = new StreamWriter(resultFileName, false, new UTF8Encoding(true)))
                {
                    // Записываем заголовок
                    writer.WriteLine(GetLocalizedString("SearchResults"));
                    writer.WriteLine("=======================================");
                    writer.WriteLine($"{GetLocalizedString("SearchFolder")}: {folderPath}");
                    writer.WriteLine($"{GetLocalizedString("SearchQuery")}: \"{searchText}\"");
                    writer.WriteLine($"{GetLocalizedString("RecursiveSearch")}: {(recursive ? GetLocalizedString("Yes") : GetLocalizedString("No"))}");
                    writer.WriteLine($"{GetLocalizedString("CaseSensitiveSearch")}: {(caseSensitive ? GetLocalizedString("Yes") : GetLocalizedString("No"))}");
                    writer.WriteLine($"{GetLocalizedString("FilePatterns")}: {string.Join(", ", filePatterns)}");

                    if (excludeFolders != null && excludeFolders.Count > 0)
                    {
                        writer.WriteLine($"{GetLocalizedString("ExcludedFolders")}: {string.Join(", ", excludeFolders)}");
                    }

                    // Добавляем строку запуска из командной строки
                    StringBuilder cmdLine = new StringBuilder("FileSearchConsole.exe");
                    cmdLine.Append($" -d \"{folderPath}\"");
                    cmdLine.Append($" -s \"{searchText}\"");
                    cmdLine.Append($" -p \"{string.Join(";", filePatterns)}\"");

                    if (!recursive)
                        cmdLine.Append(" -nr");

                    if (caseSensitive)
                        cmdLine.Append(" -c");

                    if (saveToDesktop)
                        cmdLine.Append(" -dt");

                    if (excludeFolders != null && excludeFolders.Count > 0)
                        cmdLine.Append($" -e \"{string.Join(";", excludeFolders)}\"");

                    writer.WriteLine();
                    writer.WriteLine(GetLocalizedString("RepeatSearchCommand") + ":");
                    writer.WriteLine(cmdLine.ToString());

                    writer.WriteLine("=======================================");
                    writer.WriteLine();

                    // Получение списка файлов с учетом исключений
                    List<string> allFiles = new List<string>();

                    if (recursive && excludeFolders != null && excludeFolders.Count > 0)
                    {
                        // Если нужно исключить папки, не можем использовать стандартный метод Directory.GetFiles с рекурсией
                        // Вместо этого собираем файлы вручную, пропуская исключенные папки
                        foreach (string pattern in filePatterns)
                        {
                            if (string.IsNullOrWhiteSpace(pattern)) continue;
                            string cleanPattern = pattern.Trim();
                            if (cleanPattern.Length == 0) continue;

                            Console.WriteLine($"{GetLocalizedString("SearchingPattern")}: {cleanPattern}");
                            writer.WriteLine($"{GetLocalizedString("SearchingPattern")}: {cleanPattern}");

                            List<string> filesForPattern = GetFilesWithExclusions(folderPath, cleanPattern, excludeFolders);

                            Console.WriteLine($"{GetLocalizedString("FilesFound")}: {filesForPattern.Count}");
                            writer.WriteLine($"{GetLocalizedString("FilesFound")}: {filesForPattern.Count}");

                            allFiles.AddRange(filesForPattern);
                        }
                    }
                    else
                    {
                        // Стандартный поиск без исключений
                        foreach (string pattern in filePatterns)
                        {
                            if (string.IsNullOrWhiteSpace(pattern)) continue;
                            string cleanPattern = pattern.Trim();
                            if (cleanPattern.Length == 0) continue;

                            Console.WriteLine($"{GetLocalizedString("SearchingPattern")}: {cleanPattern}");
                            writer.WriteLine($"{GetLocalizedString("SearchingPattern")}: {cleanPattern}");

                            string[] files = Directory.GetFiles(folderPath, cleanPattern, searchOption);

                            Console.WriteLine($"{GetLocalizedString("FilesFound")}: {files.Length}");
                            writer.WriteLine($"{GetLocalizedString("FilesFound")}: {files.Length}");

                            allFiles.AddRange(files);
                        }
                    }

                    // Удаление дубликатов
                    allFiles = new List<string>(new HashSet<string>(allFiles));

                    Console.WriteLine($"{GetLocalizedString("UniqueFiles")}: {allFiles.Count}");
                    writer.WriteLine($"{GetLocalizedString("UniqueFiles")}: {allFiles.Count}");
                    writer.WriteLine();
                    writer.WriteLine(GetLocalizedString("Results") + ":");
                    writer.WriteLine("---------------------------------------");

                    // Обработка каждого файла
                    foreach (string file in allFiles)
                    {
                        processedFiles++;
                        if (processedFiles % 100 == 0)
                        {
                            Console.WriteLine($"{GetLocalizedString("ProcessedFiles")}: {processedFiles} {GetLocalizedString("Of")} {allFiles.Count}");
                            writer.WriteLine($"{GetLocalizedString("ProcessedFiles")}: {processedFiles} {GetLocalizedString("Of")} {allFiles.Count}");
                        }

                        try
                        {
                            // Создаем список кодировок для попытки чтения
                            List<Encoding> encodingsList = new List<Encoding>();
                            encodingsList.Add(Encoding.UTF8);  // UTF-8

                            try { encodingsList.Add(Encoding.GetEncoding(1251)); } catch (Exception) { } // Windows-1251
                            encodingsList.Add(Encoding.Default); // ANSI по умолчанию системы
                            try { encodingsList.Add(Encoding.GetEncoding(866)); } catch (Exception) { } // DOS/OEM
                            try { encodingsList.Add(Encoding.GetEncoding(20866)); } catch (Exception) { } // KOI8-R
                            try { encodingsList.Add(Encoding.GetEncoding(21866)); } catch (Exception) { } // KOI8-U

                            // Пробуем разные кодировки для чтения файла
                            string[] lines = null;
                            Exception lastException = null;
                            Encoding successEncoding = null;

                            // Пробуем каждую кодировку по очереди
                            foreach (Encoding encoding in encodingsList)
                            {
                                try
                                {
                                    lines = File.ReadAllLines(file, encoding);
                                    successEncoding = encoding;
                                    break; // Если успешно прочитали, выходим из цикла
                                }
                                catch (Exception ex)
                                {
                                    lastException = ex;
                                }
                            }

                            // Если не удалось прочитать ни одной кодировкой
                            if (lines == null)
                            {
                                throw lastException ?? new Exception("Не удалось прочитать файл ни одной из доступных кодировок");
                            }

                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (lines[i].IndexOf(searchText, stringComparison) >= 0)
                                {
                                    // Найдено совпадение
                                    int lineNumber = i + 1;

                                    // Если кодировка файла не UTF-8, пробуем конвертировать текст
                                    string lineContent = lines[i].Trim();
                                    if (successEncoding != null && successEncoding != Encoding.UTF8)
                                    {
                                        try
                                        {
                                            // Преобразуем из исходной кодировки в массив байтов
                                            byte[] bytes = successEncoding.GetBytes(lineContent);
                                            // Затем из массива байтов обратно в строку, но уже в UTF-8
                                            lineContent = Encoding.UTF8.GetString(
                                                Encoding.Convert(successEncoding, Encoding.UTF8, bytes)
                                            );
                                        }
                                        catch (Exception)
                                        {
                                            // Если конвертация не удалась, оставляем как есть
                                        }
                                    }

                                    string result = $"{file}({lineNumber}): {lineContent}";

                                    Console.WriteLine(result);
                                    writer.WriteLine(result);

                                    totalMatches++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            string errorMessage = $"{GetLocalizedString("ErrorReadingFile")} {file}: {ex.Message}";
                            Console.WriteLine(errorMessage);
                            writer.WriteLine(errorMessage);
                        }
                    }

                    // Записываем итог
                    writer.WriteLine();
                    writer.WriteLine("=======================================");
                    writer.WriteLine($"{GetLocalizedString("SearchCompleted")}. {GetLocalizedString("MatchesFound")}: {totalMatches}");
                    writer.WriteLine($"{GetLocalizedString("FilesProcessed")}: {processedFiles}");
                    writer.WriteLine($"{GetLocalizedString("SearchDate")}: {DateTime.Now}");
                }

                return totalMatches;
            }
            catch (Exception ex)
            {
                string errorMessage = $"{GetLocalizedString("SearchError")}: {ex.Message}";
                Console.WriteLine(errorMessage);

                // Запись ошибки в файл
                try
                {
                    using (StreamWriter writer = new StreamWriter(resultFileName, true, new UTF8Encoding(true)))
                    {
                        writer.WriteLine("=======================================");
                        writer.WriteLine(errorMessage);
                    }
                }
                catch (Exception)
                {
                    // Игнорируем ошибки при записи ошибки
                }

                return totalMatches;
            }
        }
    }
}