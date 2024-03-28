using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Newtonsoft.Json;

namespace Upak
{
    internal class Nuget
    {
        private record Restore(string PackagesPath);
        private record Library(string Path, string[] Files);
        private record Project(Restore Restore);
        private record ProjectAssets(Dictionary<string, Library> Libraries, Project Project);
        private record ConfigPackage(string Id, string Version);

        /// <summary>
        /// Downloads and installs assemblies to a directory
        /// </summary>
        /// <param name="packages">Collection of packages to install</param>
        /// <param name="outputPath">Path to directory to store assemblies at</param>
        /// <returns>Promise which resolves once operation is complete</returns>
        private static void InstallPackages(IReadOnlyCollection<ConfigPackage> packages, string outputPath)
        {
            SafeMode.Prompt("Creating temporary directory");
            var tmpObj = Directory.CreateTempSubdirectory();
            Console.WriteLine("Installing to " + tmpObj.FullName);
            var oldDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tmpObj.FullName);
            var confPth = Path.Combine(tmpObj.FullName, "proj.csproj");
            var doc = new XmlDocument();
            var root = doc.CreateElement("Project");
            doc.AppendChild(root);
            root.SetAttribute("Sdk", "Microsoft.NET.Sdk");
            var propertyGroup = doc.CreateElement("PropertyGroup");
            root.AppendChild(propertyGroup);
            var targetFramework = doc.CreateElement("TargetFramework");
            propertyGroup.AppendChild(targetFramework);
            targetFramework.InnerText = "netstandard2.0";
            var itemGroup = doc.CreateElement("ItemGroup");
            root.AppendChild(itemGroup);

            foreach (var x in packages)
            {
                var package = doc.CreateElement("PackageReference");
                itemGroup.AppendChild(package);
                package.SetAttribute("Include", x.Id);
                package.SetAttribute("Version", x.Version);
            }

            var xml = doc.OuterXml;
            SafeMode.Prompt($"Writing generated xml to project file '{confPth}'");
            File.WriteAllText(confPth, xml);
            DotnetRestore();
            var projectAssetsPath = Path.Combine(tmpObj.FullName, "obj", "project.assets.json");
            ProjectAssets? projectAssets = JsonConvert.DeserializeObject<ProjectAssets>(File.ReadAllText(projectAssetsPath));
            if (projectAssets is null)
            {
                Console.WriteLine("[ERROR] Failed to read project.assets.json");
                return;
            }
            static string? Validator(ProjectAssets projectAssets)
            {
                if (projectAssets.Libraries is null)
                {
                    return "libraries not found";
                }

                if (projectAssets.Project is null)
                {
                    return "project not found";
                }

                if (projectAssets.Libraries.Keys.Any(x => x == null))
                {
                    return "a library key is null";
                }



                if (projectAssets.Libraries.Values.Aggregate((string?)null,
                                                             (acc, v) =>
                                                             (acc is not null ? acc
                                                              : v is null ? "a library entry is null"
                                                              : v.Path is null ? "a library path is null"
                                                              : v.Files is null ? "a library files is null"
                                                              : v.Files.Any(x => x is null) ? "a library file is null"
                                                              : null))
                    is string s)
                {
                    return s;
                }

                if (projectAssets.Project.Restore == null)
                {
                    return "restore not found";
                }

                if (projectAssets.Project.Restore.PackagesPath == null)
                {
                    return "packages path not found in restore";
                }

                return null;
            }

            var issue = Validator(projectAssets);

            if (issue is not null)
            {
                Console.WriteLine($"[ERROR] Project assets file is invalid: {issue}");
                return;
            }

            if (!Directory.Exists(outputPath))
            {
                // Get all files in outputPath
                var items = Directory.GetFiles(outputPath);
                foreach (var item in items)
                {
                    if (Path.GetExtension(item) is ".dll" or ".xml")
                    {
                        var path = Path.GetFullPath(item);
                        SafeMode.Prompt($"Deleting {path}");
                        File.Delete(path);
                    }
                }
            }
            else
            {
                SafeMode.Prompt($"Creating directory '{outputPath}'");
                Directory.CreateDirectory(outputPath);
            }

            foreach (var (key, library) in projectAssets.Libraries)
            {
                if (!key.StartsWith("Newtonsoft.Json/", StringComparison.OrdinalIgnoreCase) &&
                    !key.StartsWith("Microsoft.CSharp/", StringComparison.OrdinalIgnoreCase) &&
                    !key.StartsWith("JetBrains.Annotations/", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var file in library.Files)
                    {
                        if ((file.StartsWith("lib/netstandard2.0/", StringComparison.OrdinalIgnoreCase)) &&
                            (file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                             file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
                        {
                            var filePath = Path.Combine(projectAssets.Project.Restore.PackagesPath, library.Path, file);
                            var destPath = Path.Combine(outputPath, Path.GetFileName(filePath));
                            SafeMode.Prompt($"Copying '{filePath}' to '{destPath}'");
                            File.Copy(filePath, destPath, true);
                        }
                    }
                }
            }

            Directory.SetCurrentDirectory(oldDir);

            try
            {
                SafeMode.Prompt($"Deleting temporary directory '{tmpObj.FullName}'");
                tmpObj.Delete(true);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to delete temporary directory: {e.Message}");
            }
            Console.WriteLine("NuGet package installation complete");
        }

        private static void DotnetRestore()
        {
            SafeMode.Prompt("Running dotnet restore");
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = "dotnet",
                Arguments = "restore",
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start dotnet restore");
                }
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    var msg = new StringBuilder();
                    var stdout = process.StandardOutput.ReadToEnd();
                    var stderr = process.StandardError.ReadToEnd();
                    msg.AppendLine("Dotnet restore execution failed");
                    msg.AppendLine("Standard Output:");
                    msg.AppendLine(stdout);
                    msg.AppendLine("Standard Error:");
                    msg.AppendLine(stderr);
                    msg.AppendLine("Exit Code:");
                    msg.AppendLine(process.ExitCode.ToString(CultureInfo.InvariantCulture));
                    throw new InvalidOperationException(msg.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to run dotnet restore: {e.Message}");
                return;
            }
        }

        private static void InstallInUnityProject(string packageName, string version)
        {
            static string? InstallLocationType(string path)
            {
                return Misc.IsUnityProject(path) ? "project"
                    : Misc.IsUnityPackage(path) ? "package"
                    : null;
            }
            var found = Misc.FirstDir(InstallLocationType);

            if (found is null)
            {
                Console.WriteLine("[ERROR] Could not find a unity project or package to install to");
                Environment.Exit(1);
            }

            (string result, string path) = found.Value;

            var installPath = (result == "project")
                ? Path.Combine(path, "Assets", "NugetPackages")
                : Path.Combine(path, "NugetPackages");

            if (!Directory.Exists(installPath))
            {
                SafeMode.Prompt($"Creating directory '{installPath}'");
                Directory.CreateDirectory(installPath);
            }

            InstallPackages(new ConfigPackage[] { new(Id: packageName, Version: version) }, installPath);
        }

        internal static void Category(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            for (int i = 0; i < args.Length; ++i)
            {
                var arg = args[i];
                if (arg is "-h" or "--help")
                {
                    PrintHelp();
                    return;
                }
                else if (arg is "--safe")
                {
                    SafeMode.Enabled = true;
                }
                else if (arg is "install")
                {
                    InstallCommand(args[(i + 1)..]);
                    return;
                }
                else
                {
                    Console.WriteLine($"\nUnknown argument '{arg}'\n");
                    PrintHelp();
                    return;
                }
            }
        }

        private static void InstallCommand(string[] args)
        {
            if (args.Length == 0)
            {
                PrintInstallHelp();
                return;
            }

            string? packageName = null;
            string? version = null;

            for (int i = 0; i < args.Length; ++i)
            {
                var arg = args[i];
                if (arg is "-h" or "--help")
                {
                    PrintInstallHelp();
                    return;
                }
                else if (packageName is null && Data.PackageNamePartCIRegex().IsMatch(arg))
                {
                    packageName = arg;
                }
                else if (packageName is not null && version is null && Data.SemVarRegex().IsMatch(arg))
                {
                    version = arg;
                }
                else
                {
                    Console.WriteLine($"\nUnknown argument '{arg}'\n");
                    PrintInstallHelp();
                    return;
                }
            }

            if (packageName is null || version is null)
            {
                Console.WriteLine($"\nNot enough arguments provided\n");
                PrintInstallHelp();
                return;
            }

            InstallInUnityProject(packageName, version);
        }

        private static void PrintHelp()
        {
            Console.WriteLine(
                @"upak nuget: A collection of tools for using nuget packages in unity

usage: upak nuget [-h | --help] [command] [<args>]

Commands:
    install        Install a nuget package
");
        }

        private static void PrintInstallHelp()
        {
            Console.WriteLine(
                @"upak nuget install: Download and install a nuget package to the parent unity project

usage: upak nuget install [-h | --help] <package_name> <version>

Arguments:
    package_name    The full name of the package, eg. 'Newtonsoft.Json'
    version         The version of the package to install, eg. '13.0.3'
");
        }
    }
}
