using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Upak
{
    internal static partial class Docs
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

        /// <summary>
        /// Generates a default Doxyfile in the current directory
        /// </summary>
        /// <returns>True if the Doxyfile was generated successfully, false otherwise</returns>
        private static bool GenerateDefaultDoxyfile()
        {
            SafeMode.Prompt("Generating Default Doxyfile");
            try
            {
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
                Logger.LogWarning($"Failed to run doxygen: {e.Message}");
                return false;
            }
            return true;
        }

        private static bool CopyWithFilter(string sourcePath, string targetPath, Func<string, string> filter)
        {
            try
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
                return true;
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Failed to copy '{sourcePath}' to '{targetPath}': {e.Message}");
                return false;
            }
        }

        internal static bool ConfigureDoxygenFile(string projectName, string? projectBrief, bool referenceAwesome)
        {
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
                    return prefix + "../Editor \n" + new string(' ', prefix.Length) + "../Runtime ";
                }
                else if ((match = FilesPattern().Match(line)).Success)
                {
                    var prefix = MakePrefix(match.Value);
                    return prefix + "*.cs \n" + new string(' ', prefix.Length) + "*.md ";
                }
                else if (referenceAwesome && (match = HtmlExtraStylesheetPattern().Match(line)).Success)
                {
                    return MakePrefix(match.Value) + "./" + DoxygenAwesomeFileName;
                }

                return line;
            }

            // Generate temp file
            SafeMode.Prompt("Generating Temp File");
            string? tempFilePath = null;
            bool success = false;
            try
            {
                var tmpObj = Path.GetTempFileName();
                tempFilePath = tmpObj;
                // Process doxyfile line by line
                SafeMode.Prompt($"Copying Doxyfile to temp file '{tmpObj}' with Filter");
                Func<string, string> filter = ProcessDoxyfileLine;
                var copiedWithFilter = CopyWithFilter("Doxyfile", tmpObj, filter);
                if (copiedWithFilter)
                {
                    SafeMode.Prompt($"Moving file at '{tmpObj}' to Doxyfile");
                    File.Move(tmpObj, "Doxyfile", true);
                }
                success = copiedWithFilter;
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Failed to configure Doxyfile, skipping: {e.Message}");
                success = false;
            }
            finally
            {
                if (tempFilePath is not null && File.Exists(tempFilePath))
                {
                    SafeMode.Prompt($"Deleting '{tempFilePath}'");
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning($"Failed to delete '{tempFilePath}': {e.Message}");
                    }
                }
            }
            return success;
        }

        internal static void SetupDoxygen(string documentationRoot, string projectName, string? projectBrief)
        {
            string? oldCwd;
            try
            {
                oldCwd = Environment.CurrentDirectory;
                Environment.CurrentDirectory = documentationRoot;
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Failed to change directory to '{documentationRoot}', skipping doxygen setup: {e.Message}");
                return;
            }
            var doxygenAwesomeDownloaded = DownloadDoxygenAwesome();
            var doxyfileGenerated = GenerateDefaultDoxyfile();
            if (!doxyfileGenerated)
            {
                Logger.LogWarning("Failed to generate default Doxyfile. Skipping Doxygen setup.");
                if (doxygenAwesomeDownloaded && File.Exists(DoxygenAwesomeFileName))
                {
                    SafeMode.Prompt("Deleting " + DoxygenAwesomeFileName);
                    try
                    {
                        File.Delete(DoxygenAwesomeFileName);
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning($"Failed to delete {DoxygenAwesomeFileName}: {e.Message}");
                    }
                }
                return;
            }
            var configureDoxygenFile = ConfigureDoxygenFile(projectName, projectBrief, doxygenAwesomeDownloaded);
            if (!configureDoxygenFile)
            {
                Logger.LogWarning("Failed to configure Doxyfile. Skipping Doxygen setup.");
                if (doxygenAwesomeDownloaded && File.Exists(DoxygenAwesomeFileName))
                {
                    SafeMode.Prompt("Deleting " + DoxygenAwesomeFileName);
                    try
                    {
                        File.Delete(DoxygenAwesomeFileName);
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning($"Failed to delete {DoxygenAwesomeFileName}: {e.Message}");
                    }
                }
                if (File.Exists("Doxyfile"))
                {
                    SafeMode.Prompt("Deleting " + "Doxyfile");
                    try
                    {
                        File.Delete("Doxyfile");
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning($"Failed to delete Doxyfile: {e.Message}");
                    }
                }
                return;
            }
        }

        private static readonly Uri DoxygenAwesomeUri = new("https://raw.githubusercontent.com/jothepro/doxygen-awesome-css/main/doxygen-awesome.css");
        private const string DoxygenAwesomeFileName = "doxygen-awesome.css";

        /// <summary>
        /// Downloads the doxygen-awesome.css file from the internet to the current directory
        /// </summary>
        /// <returns>True if the download was successful, false otherwise</returns>
        private static bool DownloadDoxygenAwesome()
        {
            SafeMode.Prompt($"Downloading {DoxygenAwesomeFileName} from '{DoxygenAwesomeUri}' to '{DoxygenAwesomeFileName}'");
            try
            {
                using HttpClient client = new();
                var streamTask = client.GetStreamAsync(DoxygenAwesomeUri);
                streamTask.Wait();
                using Stream fileStream = streamTask.Result;
                using FileStream output = new(DoxygenAwesomeFileName, FileMode.Create);
                fileStream.CopyTo(output);
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Failed to download {DoxygenAwesomeFileName}: {e.Message}");
                return false;
            }
            return true;
        }
    }
}
