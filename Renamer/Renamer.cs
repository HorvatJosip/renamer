using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Renamer
{
    public static class Renamer
    {
        #region Fields

        private static Config config;

        private static string configPath;

        public static readonly Action<Message> DefaultReport = Console.WriteLine;
        public static readonly Action DefaultClearReports = Console.Clear;
        public static readonly Func<Message, bool> DefaultCancelRenaming = message =>
        {
            Console.WriteLine(message);
            Console.Write("(press y to confirm): ");

            if (Console.ReadKey(true).Key != ConsoleKey.Y)
            {
                Console.WriteLine("Bailing...");
                return true;
            }

            return false;
        };

        #endregion

        #region Properties

        public static string ConfigPath
        {
            get
            {
                return configPath;
            }
            set
            {
                configPath = value;

                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Config));

                using (var stream = File.OpenRead(value))
                    config = (Config)serializer.ReadObject(stream);
            }
        }

        public static Action<Message> Report { get; set; } = DefaultReport;
        public static Action ClearReports { get; set; } = DefaultClearReports;
        public static Func<Message, bool> CancelRenaming { get; set; } = DefaultCancelRenaming;

        #endregion

        static Renamer() => ConfigPath = "./config.json";

        public static void Run(string from, string to, string directory)
        {
            if (from == null || to == null)
                throw new Exception($"{nameof(from)} and {nameof(to)} must be specified!");

            if (!Directory.Exists(directory))
                throw new Exception($"{directory} is not a directory...");

            var comparison = config.MatchCase
                ? StringComparison.InvariantCulture
                : StringComparison.InvariantCultureIgnoreCase;

            ClearReports();

            var message = new Message($"Changing everything from '{from}' to '{to}' inside {directory}");
            message.AddLine($"Configuration{Environment.NewLine}============={Environment.NewLine}{config}");
            message.AddLine("Proceed?");

            if (CancelRenaming(message))
                return;

            ClearReports();

            Report(new Message("Skipping: " + Environment.NewLine + config.Skip));

            Report(Message.NewLine);

            var directoriesToMove = new HashSet<DirectoryInfo>();
            var directories = new List<string>();

            void Recursion(string baseDirectory)
            {
                foreach (var dir in Directory.GetDirectories(baseDirectory))
                {
                    var directoryInfo = new DirectoryInfo(dir);

                    if (config.Skip.Directory(directoryInfo.Name))
                        Report(new Message($"Skipping the following directory: {dir}...", MessageType.SkippingItem));
                    else
                    {
                        OnDirectoryFound(directoryInfo);

                        Recursion(dir);
                    }
                }
            }

            void OnDirectoryFound(DirectoryInfo directoryInfo)
            {
                Rename(directoryInfo.Name, from, to, comparison, out int changes);

                if (changes > 0 && directoryInfo.Parent != null)
                    directoriesToMove.Add(directoryInfo);

                directories.Add(directoryInfo.FullName);
            }

            OnDirectoryFound(new DirectoryInfo(directory));
            Recursion(directory);

            int totalOccurences = 0, fileCount = 0, directoryCount = 0;

            foreach (var dir in directories)
            {
                directoryCount++;

                foreach (var file in Directory.GetFiles(dir))
                {
                    if (config.RenameWithinFileContent == false || config.Skip.File(Path.GetFileName(file)))
                    {
                        Report(new Message($"Skipping the following file: {file}...", MessageType.SkippingItem));
                        continue;
                    }

                    if (Check(file))
                        fileCount++;
                }
            }

            if (config.RenameDirectories)
                foreach (var directoryInfo in directoriesToMove.Reverse())
                {
                    try
                    {
                        var newPath = Path.Combine(
                            directoryInfo.Parent.FullName,
                            directoryInfo.Name.Replace(from, to)
                        );

                        Directory.Move(directoryInfo.FullName, newPath);

                        totalOccurences++;
                    }
                    catch (Exception ex)
                    {
                        Report(new Message($"Could not move the directory {directoryInfo.FullName}, reason: {ex.Message}", MessageType.Error));
                    }
                };

            bool Check(string file)
            {
                var occurences = 0;
                Report(new Message($"Changing occurences in {file}...", MessageType.ChangeReport));

                try
                {
                    var content = File.ReadAllText(file);
                    var renamed = Rename(content, from, to, comparison, out int found);
                    occurences += found;

                    File.WriteAllText(file, renamed);

                    string fileName, extension;
                    if (config.UseFileExtensionWhileRenamingFile)
                    {
                        fileName = Path.GetFileName(file);
                        extension = "";
                    }
                    else
                    {
                        fileName = Path.GetFileNameWithoutExtension(file);
                        extension = Path.GetExtension(file);
                    }

                    fileName = Rename(fileName, from, to, comparison, out int changes);
                    if (config.RenameFiles && changes > 0)
                    {
                        try
                        {
                            var dot = fileName.Contains('.') || extension.Contains('.')
                                ? ""
                                : ".";

                            var newPath = Path.Combine(
                                Path.GetDirectoryName(file),
                                $"{fileName}{dot}{extension}"
                            );

                            File.Move(file, newPath);
                            occurences++;
                        }
                        catch (Exception ex)
                        {
                            Report(new Message($"Could not move the file {fileName}, reason: {ex.Message}", MessageType.Error));
                        }
                    }

                    Report(new Message($"{occurences} occurences changed!{Environment.NewLine}", MessageType.ChangeReport));

                    totalOccurences += occurences;

                    return true;
                }
                catch (Exception ex)
                {
                    Report(new Message($"Could not parse the file because of the following reason: {ex.Message}...", MessageType.Error));

                    return false;
                }
            }

            Report(Message.NewLine);

            Report(new Message($"Changed {totalOccurences} occurences of {from} to {to} inside {directoryCount} directories and {fileCount} files!", MessageType.ChangeReport));

            if (config.OpenDirectoryAfter)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer",
                        Arguments = directory
                    });
                }
                catch (Exception ex)
                {
                    Report(new Message($"Could not open the folder {directory} because of the following reason: {ex.Message}...", MessageType.Error));
                }
            }
        }

        public static string Rename(string content, string from, string to, StringComparison comparison, out int occurences)
        {
            occurences = 0;
            var builder = new System.Text.StringBuilder();

            var index = -1;
            while (true)
            {
                var startIndex = index + 1;

                if (startIndex >= content.Length)
                    break;

                var newIndex = content.IndexOf(from, startIndex, comparison);

                if (index == -1)
                    index = 0;

                if (newIndex >= content.Length || newIndex < 0)
                {
                    if (index < content.Length)
                        builder.Append(content, index, content.Length - index);

                    break;
                }

                bool CharOk(int charIndex)
                {
                    if (charIndex >= 0 && charIndex < content.Length)
                    {
                        var @char = content[charIndex];

                        if (Regex.IsMatch(@char.ToString(), config.FullWordRegex))
                            return false;
                    }

                    return true;
                }

                // Check left and right for full word
                var left = CharOk(newIndex - 1);
                var right = CharOk(newIndex + from.Length);

                // If it isn't a full word search OR if both chars are cool...
                if (!config.FullWord || (left && right))
                {
                    // Add the target word, move by the search word
                    builder.Append(content, index, newIndex - index);
                    builder.Append(to);

                    newIndex += from.Length;

                    occurences++;
                }
                // Otherwise, if the word is found, but it isn't the whole word...
                else
                {
                    // Just copy all of the content ending with the found word occurrence
                    newIndex += from.Length;

                    builder.Append(content, index, newIndex - index);
                }

                index = newIndex;
            }

            return builder.ToString();
        }

        public static void RunInConsole(string from = null, string to = null, string directory = null)
        {
            directory = directory ?? ".";

            while (!Directory.Exists(directory))
            {
                Console.Write("Enter a directory to use: ");
                directory = Console.ReadLine();
            }

            if (from == null || to == null)
            {
                Console.Clear();
                Console.Write("You have to enter two strings: one that you want to replace and");
                Console.WriteLine(" the other that you want to replace it with...");

                Console.Write(Environment.NewLine + "String to replace: ");
                from = Console.ReadLine();

                Console.Write(Environment.NewLine + "String to replace it with: ");
                to = Console.ReadLine();
            }

            Report = message =>
            {
                void ChangeColor(ConsoleColor color)
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine(message);
                    Console.ResetColor();
                }

                switch (message.Type)
                {
                    case MessageType.Error:
                        ChangeColor(ConsoleColor.Red);
                        break;
                    case MessageType.ChangeReport:
                        ChangeColor(ConsoleColor.Green);
                        break;
                    case MessageType.SkippingItem:
                        ChangeColor(ConsoleColor.DarkCyan);
                        break;
                    default:
                        Console.WriteLine(message);
                        break;
                }
            };
            ClearReports = DefaultClearReports;
            CancelRenaming = DefaultCancelRenaming;

            Run(from, to, directory);
        }
    }
}
