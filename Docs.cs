using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        internal static async Task SetupDoxygen(string documentationRoot, string projectName, string? projectBrief)
        {
            var oldCwd = Environment.CurrentDirectory;
            Environment.CurrentDirectory = documentationRoot;
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = "doxygen",
                Arguments = "-g",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start doxygen");
                }
                process.WaitForExit();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to run doxygen: {e.Message}");
                return;
            }
            // Generate temp file
            var tmpObj = Path.GetTempFileName();
            // Process doxyfile line by line
            using var reader = new StreamReader("Doxyfile");
            using var writer = new StreamWriter(tmpObj);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string MakePrefix(string match)
                {
                    return match + (match[^1] == '=' ? " " : "");
                }
                Match? match = default;
                var newContent = "";
                if ((match = ProjectNamePattern().Match(line)).Success)
                {
                    newContent = MakePrefix(match.Value) + projectName;
                }
                else if ((match = ProjectBriefPattern().Match(line)).Success)
                {
                    newContent = MakePrefix(match.Value) + projectBrief;
                }
                else if ((match = InputPattern().Match(line)).Success)
                {
                    var prefix = MakePrefix(match.Value);
                    newContent = prefix + "../Editor \n" + (new String(' ', prefix.Length)) + "../Runtime ";
                }
                else if ((match = FilesPattern().Match(line)).Success)
                {
                    var prefix = MakePrefix(match.Value);
                    newContent = prefix + "*.cs \n" + (new String(' ', prefix.Length)) + "*.md ";
                }
                else if ((match = HtmlExtraStylesheetPattern().Match(line)).Success)
                {
                    newContent = MakePrefix(match.Value) + "./doxygen-awesome.css";
                }
                else
                {
                    newContent = line;
                }
                writer.WriteLine(newContent);
            }
            writer.Flush();
            writer.Close();
            reader.Close();
            File.Delete("Doxyfile");
            File.Move(tmpObj, "Doxyfile");
            await DownloadDoxygenAwesome(documentationRoot);
            Environment.CurrentDirectory = oldCwd;
        }

        private static async Task DownloadDoxygenAwesome(string documentationRoot)
        {
            var url = "https://raw.githubusercontent.com/jothepro/doxygen-awesome-css/main/doxygen-awesome.css";
            var fileName = "doxygen-awesome.css";
            try
            {
                using HttpClient client = new HttpClient();
                using Stream fileStream = await client.GetStreamAsync(url);
                using FileStream output = new FileStream(fileName, FileMode.Create);
                await fileStream.CopyToAsync(output);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Failed to download doxygen-awesome.css: {e.Message}");
            }
        }
    }
}
