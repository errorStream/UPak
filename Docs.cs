using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Upak
{
    internal partial class Docs
    {
        [GeneratedRegex(@"^\s*PROJECT_NAME\s*=\s*", RegexOptions.None, "en-US")]
        private static partial Regex ProjectNamePattern();

        [GeneratedRegex(@"^\s*PROJECT_BRIEF\s*=\s*", RegexOptions.None, "en-US")]
        private static partial Regex ProjectBriefPattern();

        [GeneratedRegex(@"^\s*INPUT\s*=\s*", RegexOptions.None, "en-US")]
        private static partial Regex InputPattern();

        [GeneratedRegex(@"^\s*FILE_PATTERNS\s*=\s*", RegexOptions.None, "en-US")]
        private static partial Regex FilesPattern();

        [GeneratedRegex(@"^\s*HTML_EXTRA_STYLESHEET\s*=\s*", RegexOptions.None, "en-US")]
        private static partial Regex HtmlExtraStylesheetPattern();

        private static void GenerateDefaultDoxyfile()
        {
            SafeMode.Prompt("Generating Default Doxyfile");
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = "doxygen",
                Arguments = "-g",
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start doxygen");
                }
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        new StringBuilder()
                        .AppendLine("Doxygen failed to generate default configuration file")
                        .AppendLine("Standard Output:")
                        .AppendLine(process.StandardOutput.ReadToEnd())
                        .AppendLine("Standard Error:")
                        .AppendLine(process.StandardError.ReadToEnd())
                        .AppendLine("Exit Code:")
                        .AppendLine(process.ExitCode.ToString(CultureInfo.InvariantCulture)).ToString());
                }
            }
            catch (Exception e)
            {
                throw new IOException("Failed to run doxygen", e);
            }
        }

        private static void CopyWithFilter(string sourcePath, string targetPath, Func<string, string> filter)
        {
            SafeMode.Prompt($"Copying '{sourcePath}' to '{targetPath}' with Filter");
            using var reader = new StreamReader(sourcePath);
            using var writer = new StreamWriter(targetPath);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                writer.WriteLine(filter(line));
            }

            writer.Flush();
            writer.Close();
            reader.Close();
        }

        internal static void SetupDoxygen(string documentationRoot, string projectName, string? projectBrief)
        {
            var oldCwd = Environment.CurrentDirectory;
            string? tempFilePath = null;
            try
            {
                Environment.CurrentDirectory = documentationRoot;
                GenerateDefaultDoxyfile();
                // Generate temp file
                SafeMode.Prompt("Generating Temp File");
                var tmpObj = Path.GetTempFileName();
                tempFilePath = tmpObj;
                // Process doxyfile line by line
                string ProcessDoxyfileLine(string line)
                {
                    static string MakePrefix(string match)
                    {
                        return match + (match[^1] == '=' ? " " : "");
                    }
                    Match? match = default;
                    if ((match = ProjectNamePattern().Match(line)).Success)
                    {
                        return MakePrefix(match.Value) + projectName;
                    }
                    else if ((match = ProjectBriefPattern().Match(line)).Success)
                    {
                        return MakePrefix(match.Value) + projectBrief;
                    }
                    else if ((match = InputPattern().Match(line)).Success)
                    {
                        var prefix = MakePrefix(match.Value);
                        return prefix + "../Editor \n" + (new String(' ', prefix.Length)) + "../Runtime ";
                    }
                    else if ((match = FilesPattern().Match(line)).Success)
                    {
                        var prefix = MakePrefix(match.Value);
                        return prefix + "*.cs \n" + (new String(' ', prefix.Length)) + "*.md ";
                    }
                    else if ((match = HtmlExtraStylesheetPattern().Match(line)).Success)
                    {
                        return MakePrefix(match.Value) + "./doxygen-awesome.css";
                    }

                    return line;
                }
                SafeMode.Prompt($"Copying Doxyfile to temp file '{tmpObj}' with Filter");
                CopyWithFilter("Doxyfile", tmpObj, ProcessDoxyfileLine);
                SafeMode.Prompt($"Moving file at '{tmpObj}' to Doxyfile");
                File.Move(tmpObj, "Doxyfile", true);
                DownloadDoxygenAwesome(documentationRoot);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[WARN] Failed to setup Doxygen: {e.Message}");
            }
            finally
            {
                if (tempFilePath is not null && File.Exists(tempFilePath))
                {
                    SafeMode.Prompt($"Deleting '{tempFilePath}'");
                    File.Delete(tempFilePath);
                }
                if (File.Exists("Doxyfile"))
                {
                    SafeMode.Prompt("Deleting Doxyfile");
                    File.Delete("Doxyfile");
                }
                Environment.CurrentDirectory = oldCwd;
            }
        }

        private static void DownloadDoxygenAwesome(string documentationRoot)
        {
            var url = "https://raw.githubusercontent.com/jothepro/doxygen-awesome-css/main/doxygen-awesome.css";
            var fileName = "doxygen-awesome.css";
            SafeMode.Prompt($"Downloading doxygen-awesome.css from '{url}' to '{fileName}'");
            try
            {
                using HttpClient client = new HttpClient();
                var streamTask = client.GetStreamAsync(url);
                streamTask.Wait();
                using Stream fileStream = streamTask.Result;
                using FileStream output = new FileStream(fileName, FileMode.Create);
                fileStream.CopyTo(output);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Failed to download doxygen-awesome.css: {e.Message}");
            }
        }
    }
}
