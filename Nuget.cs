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
    internal static class Nuget
    {
        #region Serialization types
#pragma warning disable CA1812
        private sealed record Restore(string PackagesPath);
        private sealed record Library(string Path, string[] Files);
        private sealed record Project(Restore Restore);
        private sealed record ProjectAssets(Dictionary<string, Library> Libraries, Project Project);
        private sealed record ConfigPackage(string Id, string Version);
#pragma warning restore CA1812
        #endregion Serialization types

        private static bool GenerateCSProj(string rootDir, IReadOnlyCollection<ConfigPackage> packages)
        {
            try
            {
                var confPth = Path.Combine(rootDir, "proj.csproj");
                var doc = new XmlDocument();
                var root = doc.CreateElement("Project");
                _ = doc.AppendChild(root);
                root.SetAttribute("Sdk", "Microsoft.NET.Sdk");
                var propertyGroup = doc.CreateElement("PropertyGroup");
                _ = root.AppendChild(propertyGroup);
                var targetFramework = doc.CreateElement("TargetFramework");
                _ = propertyGroup.AppendChild(targetFramework);
                targetFramework.InnerText = "netstandard2.0";
                var itemGroup = doc.CreateElement("ItemGroup");
                _ = root.AppendChild(itemGroup);

                foreach (var x in packages)
                {
                    var package = doc.CreateElement("PackageReference");
                    _ = itemGroup.AppendChild(package);
                    package.SetAttribute("Include", x.Id);
                    package.SetAttribute("Version", x.Version);
                }

                var xml = doc.OuterXml;
                SafeMode.Prompt($"Writing generated xml to project file '{confPth}'");
                File.WriteAllText(confPth, xml);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to generate csproj file: " + e.Message);
                return false;
            }
        }

        private static ProjectAssets? LoadProjectAssets(string rootDir)
        {
            try
            {
                var projectAssetsPath = Path.Combine(rootDir, "obj", "project.assets.json");
                ProjectAssets? projectAssets = JsonConvert.DeserializeObject<ProjectAssets>(File.ReadAllText(projectAssetsPath));
                if (projectAssets is null)
                {
                    Logger.LogError("Failed to read project.assets.json");
                    return null;
                }
                static string? Validator(ProjectAssets projectAssets)
                {
                    return projectAssets.Libraries is null ? "libraries not found"
                        : projectAssets.Project is null ? "project not found"
                        : projectAssets.Libraries.Keys.Any(x => x == null) ? "a library key is null"
                        : projectAssets.Libraries.Values.Aggregate((string?)null,
                                                                    (acc, v) =>
                                                                    acc is not null ? acc
                                                                    : v is null ? "a library entry is null"
                                                                    : v.Path is null ? "a library path is null"
                                                                    : v.Files is null ? "a library files is null"
                                                                    : v.Files.Any(x => x is null) ? "a library file is null"
                                                                    : null) is string s ? s
                        : projectAssets.Project.Restore == null ? "restore not found"
                        : projectAssets.Project.Restore.PackagesPath == null ? "packages path not found in restore"
                        : null;
                }

                var issue = Validator(projectAssets);

                if (issue is not null)
                {
                    Logger.LogError("Project assets file is invalid: " + issue);
                    return null;
                }

                return projectAssets;
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to load project assets: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Downloads and installs assemblies to a directory
        /// </summary>
        /// <param name="packages">Collection of packages to install</param>
        /// <param name="outputPath">Path to directory to store assemblies at</param>
        /// <returns>Promise which resolves once operation is complete</returns>
        private static void InstallPackages(IReadOnlyCollection<ConfigPackage> packages, string outputPath)
        {
            var success = false;
            using (TempDir tmpDir = new())
            {
                Logger.LogInfo("Installing to " + tmpDir.Path);
                string? oldDir = null;
                try
                {
                    oldDir = Directory.GetCurrentDirectory();
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to get current directory: " + e.Message);
                    return;
                }
                try
                {
                    Directory.SetCurrentDirectory(tmpDir.Path);
                    var generatedCSProj = GenerateCSProj(tmpDir.Path, packages);
                    if (!generatedCSProj)
                    {
                        Logger.LogError("Failed to generate csproj file. Aborting.");
                        return;
                    }
                    var dotnetRestored = DotnetRestore();
                    if (!dotnetRestored)
                    {
                        Logger.LogError("Failed to restore packages. Aborting.");
                        return;
                    }
                    ProjectAssets? projectAssets = LoadProjectAssets(tmpDir.Path);

                    if (projectAssets is null)
                    {
                        Logger.LogError("Could not retrive project assets information. Aborting.");
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
                        _ = Directory.CreateDirectory(outputPath);
                    }

                    foreach (var (key, library) in projectAssets.Libraries)
                    {
                        if (!key.StartsWith("Newtonsoft.Json/", StringComparison.OrdinalIgnoreCase) &&
                            !key.StartsWith("Microsoft.CSharp/", StringComparison.OrdinalIgnoreCase) &&
                            !key.StartsWith("JetBrains.Annotations/", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var file in library.Files)
                            {
                                if (file.StartsWith("lib/netstandard2.0/", StringComparison.OrdinalIgnoreCase) &&
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

                    success = true;
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to install NuGet package: " + e.Message);
                }

                Directory.SetCurrentDirectory(oldDir);
            }

            if (success)
            {
                Logger.LogInfo("NuGet package installation complete");
            }
        }

        private static bool DotnetRestore()
        {
            try
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
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start dotnet restore");
                }
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        new StringBuilder()
                        .AppendLine("Dotnet restore execution failed")
                        .AppendLine("Standard Output:")
                        .AppendLine(process.StandardOutput.ReadToEnd())
                        .AppendLine("Standard Error:")
                        .AppendLine(process.StandardError.ReadToEnd())
                        .AppendLine("Exit Code:")
                        .AppendLine(process.ExitCode.ToString(CultureInfo.InvariantCulture)).ToString());
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to run dotnet restore: " + e.Message);
                return false;
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
                Logger.LogError("Could not find a unity project or package to install to");
                return;
            }

            (string result, string path) = found.Value;

            var installPath = (result == "project")
                ? Path.Combine(path, "Assets", "NugetPackages")
                : Path.Combine(path, "NugetPackages");

            if (!Directory.Exists(installPath))
            {
                try
                {
                    SafeMode.Prompt($"Creating directory '{installPath}'");
                    _ = Directory.CreateDirectory(installPath);
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to create directory: " + e.Message);
                    return;
                }
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
                    Logger.LogError($"Unknown argument '{arg}'");
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
                    Logger.LogError($"Unknown argument '{arg}'");
                    PrintInstallHelp();
                    return;
                }
            }

            if (packageName is null || version is null)
            {
                Logger.LogError("Not enough arguments provided");
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
