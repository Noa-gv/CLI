using System;
using System.CommandLine;


class Program
{
    static void Main(string[] args)
    {
        // הגדרת אפשרויות לפקודה bundle
        var bundleOption = new Option<FileInfo>(new[] { "--output", "-o" }, "Output file path") { IsRequired = true };
        var languageOption = new Option<string>(new[] { "--language", "-l" }, "Programming languages. Use 'all' for all files") { IsRequired = true };
        var noteOption = new Option<bool>(new[] { "--note", "-n" }, "Include source code paths as comments in the bundle");
        var sortOption = new Option<string>(new[] { "--sort", "-s" }, "Sort files by name or type") { IsRequired = false };
        var removeEmptyLinesOption = new Option<bool>(new[] { "--remove-empty-lines", "-r" }, "Remove empty lines from the source code");
        var authorOption = new Option<string>(new[] { "--author", "-a" }, "Name of the file creator") { IsRequired = false };

        var bundleCommand = new Command("bundle", "Bundle code files")
        {
            bundleOption,
            languageOption,
            noteOption,
            sortOption,
            removeEmptyLinesOption,
            authorOption
        };

        bundleCommand.SetHandler((output, language, note, sort, removeEmptyLines, author) =>
        {
            try
            {
                // בדיקת תקינות על נתיב הפלט - אם הוא קיים ואם הוא תקין
                if (output == null || string.IsNullOrWhiteSpace(output.FullName))
                {
                    Console.WriteLine("Invalid output file path.");
                    return;
                }

                // בדיקת תקינות על שפת התכנות - אם נבחרה 'all' או שפה קיימת
                var validLanguages = new[] { "csharp", "python", "java", "javascript", "all" }; // רשימת שפות תקינות
                if (!validLanguages.Contains(language.ToLower()))
                {
                    Console.WriteLine("Invalid programming language. Please use 'all' or a valid language: " + string.Join(", ", validLanguages));
                    return;
                }

                var allFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*") // קבלת כל הקבצים בתיקייה הנוכחית
                    .Where(file => !file.Contains("\\bin\\") && !file.Contains("\\debug\\")) // אי הכללה של bin ו-debug
                    .OrderBy(file => sort == "type" ? Path.GetExtension(file) : Path.GetFileName(file)) // מיון הקבצים לפי סוג או שם
                    .ToList();

                using (var writer = new StreamWriter(output.FullName)) // פתיחת קובץ לכתיבה
                {
                    if (!string.IsNullOrWhiteSpace(author))
                    {
                        writer.WriteLine($"// Created by: {author}"); // כתיבה לקובץ מי יצר אותו
                    }

                    writer.WriteLine("Bundled files:");
                    if (language.Equals("all"))
                    {
                        foreach (var file in allFiles)
                        {
                            var fileContent = File.ReadAllLines(file);
                            if (removeEmptyLines)
                                fileContent = fileContent.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();

                            foreach (var line in fileContent)
                                writer.WriteLine(line);

                            if (note)
                                writer.WriteLine($"// Source: {Path.GetFileName(file)} - {file}");
                        }
                        Console.WriteLine($"Including {allFiles.Count} files.");
                    }
                    else
                    {
                        Console.WriteLine($"Selected languages: {language}");
                    }
                }
                Console.WriteLine("File created");
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Invalid file path");
            }
        }, bundleOption, languageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

        // פקודת create-rsp ליצירת קובץ תגובה
        var rspCommand = new Command("create-rsp", "Create a response file with the bundle command");

        rspCommand.SetHandler(() =>
        {
            // פונקציה לקבלת קלט מהמשתמש
            string GetUserInput(string prompt)
            {
                Console.Write(prompt);
                return Console.ReadLine();
            }

            // פונקציה לקבלת קלט בוליאני מהמשתמש (true/false)
            bool GetBooleanInput(string prompt)
            {
                Console.Write(prompt);
                string input = Console.ReadLine();
                return input.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            // קליטת קלט מהמשתמש לכל אחת מהאפשרויות
            var output = GetUserInput("Enter output file path: ");
            var language = GetUserInput("Enter programming languages (or 'all'): ");
            var note = GetBooleanInput("Include source code paths as comments (true/false): ");
            var sort = GetUserInput("Sort by 'name' or 'type' (optional): ");
            var removeEmptyLines = GetBooleanInput("Remove empty lines (true/false): ");
            var author = GetUserInput("Enter the name of the file creator (optional): ");

            try
            {
                var rspFilePath = "bundleCommand.rsp";
                using (var writer = new StreamWriter(rspFilePath))
                {
                    writer.WriteLine($"--output \"{output}\"");
                    writer.WriteLine($"--language \"{language}\"");
                    if (note)
                        writer.WriteLine("--note");
                    if (!string.IsNullOrEmpty(sort))
                        writer.WriteLine($"--sort \"{sort}\"");
                    if (removeEmptyLines)
                        writer.WriteLine("--remove-empty-lines");
                    if (!string.IsNullOrEmpty(author))
                        writer.WriteLine($"--author \"{author}\"");

                    Console.WriteLine($"Response file '{rspFilePath}' created successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating response file: {ex.Message}");
            }
        });

        // הגדרת הפקודה הראשית עם bundle ו-create-rsp
        var rootCommand = new RootCommand("File Bundler CLI") { bundleCommand, rspCommand };
        rootCommand.InvokeAsync(args).Wait();
    }
}
