using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;

namespace Upak
{
    internal class Nugit
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
            var tmpObj = Directory.CreateTempSubdirectory();
            Console.WriteLine("Installing to " + tmpObj.FullName);
            var oldDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tmpObj.FullName);
            var confPth = Path.Combine(tmpObj.FullName, "proj.csproj");
            var doc = new XmlDocument();
            var root = doc.CreateElement("Project");
            doc.AppendChild(root);
            root.SetAttribute("Sdk", "Microsoft.NET.Sdk");
            root.AppendChild(doc.CreateElement("PropertyGroup"));
            var targetFramework = doc.CreateElement("TargetFramework");
            root.AppendChild(targetFramework);
            targetFramework.InnerText = "netstandard2.0";
            var itemGroup = doc.CreateElement("ItemGroup");

            foreach (var x in packages)
            {
                var package = doc.CreateElement("PackageReference");
                itemGroup.AppendChild(package);
                package.SetAttribute("Include", x.Id);
                package.SetAttribute("Version", x.Version);
            }

            var xml = doc.OuterXml;
            File.WriteAllText(confPth, xml);
            DotnetRestore();
            var projectAssetsPath = Path.Combine(tmpObj.FullName, "obj", "project.assets.json");
            ProjectAssets? projectAssets = JsonConvert.DeserializeObject<ProjectAssets>(File.ReadAllText(projectAssetsPath));
            if (projectAssets is null)
            {
                Console.WriteLine("[ERROR] Failed to read project.assets.json");
                return;
            }
            {
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
                            File.Delete(Path.GetFullPath(item));
                        }
                    }
                }
                else
                {
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
                                File.Copy(filePath, destPath, true);
                            }
                        }
                    }
                }

                Directory.SetCurrentDirectory(oldDir);

                try
                {
                    tmpObj.Delete(true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to delete temporary directory: {e.Message}");
                }
            }
        }

        private static void DotnetRestore()
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = "dotnet",
                Arguments = "restore",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start dotnet restore");
                }
                process.WaitForExit();
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
                return Misc.IsUnityPackage(path) ? "project"
                    : Misc.IsUnityPackage(path) ? "package"
                    : null;
            }
            var found = Misc.FirstDir(InstallLocationType);

            if (found is null)
            {
                Console.WriteLine("[ERROR] Could not find a unity project or package to install to");
                Environment.Exit(1);
            }

            (string path, string result) = found.Value;

            var installPath = (result == "project")
                ? Path.Combine(path, "Assets", "NugitPackages")
                : Path.Combine(path, "NugitPackages");

            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            InstallPackages(new ConfigPackage[] { new(Id: packageName, Version: version) }, installPath);
        }

        internal static void Category(string[] strings)
        {
            static void PrintHelp()
            {
                Console.WriteLine(
                    @"upak nugit: A collection of tools for using nugit packages in unity

usage: upak nugit [-h | --help] [command]

Commands:
    install        Install a nugit package
");
            }
            if (strings.Length == 0)
            {
                PrintHelp();
                return;
            }
            else if (strings[0] is "-h" or "--help")
            {
                PrintHelp();
                return;
            }
            else if (strings[0] == "install")
            {
                if (strings.Length < 3)
                {
                    Console.WriteLine("Not enough arguments for install command");
                    PrintHelp();
                    return;
                }
                InstallInUnityProject(strings[1], strings[2]);
                return;
            }
            else
            {
                Console.WriteLine("Unknown command: " + strings[0]);
                PrintHelp();
                return;
            }
        }
    }
}
