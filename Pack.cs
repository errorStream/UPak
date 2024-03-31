using System;
using System.IO;
using System.Text;
using Sharprompt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Upak
{
    internal static class Pack
    {
        private record CommonPackageOptions(
            string OfficialName,
            string FullName,
            string Version,
            string? Description,
            string? DisplayName,
            string? MinimumUnityVersion,
            bool GenerateDocs
            );

        private sealed record EmbeddedPackageOptions(
            string OfficialName,
            string FullName,
            string Version,
            string? Description,
            string? DisplayName,
            string? MinimumUnityVersion,
            bool GenerateDocs,
            string UnityProjectRoot
            ) : CommonPackageOptions(OfficialName, FullName, Version, Description, DisplayName, MinimumUnityVersion, GenerateDocs);

        private sealed record LocalPackageOptions(
            string OfficialName,
            string FullName,
            string Version,
            string? Description,
            string? DisplayName,
            string? MinimumUnityVersion,
            bool GenerateDocs,
            string? FolderName
            ) : CommonPackageOptions(OfficialName, FullName, Version, Description, DisplayName, MinimumUnityVersion, GenerateDocs);

        private static Func<object?, ValidationResult?> ValidateRegularExpressionPrecomp(Regex regex, string errorMessage)
        {
            return input => input is not string strValue ? ValidationResult.Success
                            : regex.IsMatch(strValue) ? ValidationResult.Success
                            : new ValidationResult(errorMessage);
        }

        private static Func<object?, ValidationResult?> ValidatePredicate(Predicate<object?> predicate, string errorMessage)
        {
            return input => predicate(input) ? ValidationResult.Success : new ValidationResult(errorMessage);
        }

        internal static readonly string[] _packagePlacements = new[] { "embedded", "local" };

        private static void InitPackage()
        {
            string? officialName = null;
            string? companyName = null;
            string? topLevelDomain = null;
            string? version = null;
            string? description = null;
            string? displayName = null;
            string? minimumUnityVersion = null;
            bool? generateDocs = null;
            string? embeddedOrLocal = null;
            string? folderName = null;
            Console.WriteLine("The officially registered package name.");
            officialName = Prompt.Input<string>("Official Name",
                                                validators: new[] {
                                                    Validators.Required(),
                                                    ValidateRegularExpressionPrecomp(
                                                    Data.PackageNamePartRegex(),
                                                    "Invalid package name: May only contain lowercase letters, underscore, period, or hyphen and be between 1 and 214 characters") });
            Console.WriteLine("The name of the company producing this package. This is usually your domain, for (example.com), it would be 'example'.");
            companyName = Prompt.Input<string>("Company Name",
                                               validators: new[] {
                                                   Validators.Required(),
                                                   ValidateRegularExpressionPrecomp(
                                                   Data.PackageNamePartRegex(),
                                                   "Invalid company name") });
            Console.WriteLine("The top level domain of your site. For (example.com), it would be 'com'.");
            topLevelDomain = Prompt.Input<string>("Top Level Domain",
                                                  validators: new[] {
                                                      Validators.Required(),
                                                      ValidateRegularExpressionPrecomp(
                                                      Data.PackageNamePartRegex(),
                                                      "Invalid top level domain name") },
                                                  defaultValue: "com");
            Console.WriteLine("The package version number.");
            version = Prompt.Input<string>("Initial Version",
                                           validators: new[] {
                                               Validators.Required(),
                                               ValidateRegularExpressionPrecomp(
                                               Data.SemVarRegex(),
                                               "Must be a valid SemVar") },
                                           defaultValue: "1.0.0");
            Console.WriteLine("A brief description of the package.");
            description = Prompt.Input<string>("Description");
            Console.WriteLine("A user-friendly name to appear in the Unity Editor.");
            displayName = Prompt.Input<string>("Display Name");
            // TODO: set default to that of unity host project
            Console.WriteLine("The lowest Unity version the package is compatible with. Use 'any' for any version.");
            minimumUnityVersion = Prompt.Input<string>("Unity Version",
                                                       validators: new[] {
                                                           Validators.Required(),
                                                           ValidatePredicate(
                                                               (object? val) => val is string s && (s == "any" || Data.SemVarRegex().IsMatch(s)),
                                                               "Must be a valid SemVar or 'any'") },
                                                       defaultValue: "any");
            Console.WriteLine("Should doxygen document generation be setup?");
            generateDocs = Prompt.Confirm("Generate Docs?",
                                          defaultValue: true);
            Console.WriteLine("Embedded packages are installed to the packages folder of the nearest parent unity project.");
            embeddedOrLocal = Prompt.Select("Embedded Or Local",
                                            _packagePlacements,
                                            defaultValue: "embedded");
            if (embeddedOrLocal == "local")
            {
                Console.WriteLine("The name of the root folder for the project.");
                folderName = Prompt.Input<string>("Folder Name",
                                                  defaultValue: displayName ?? officialName);
            }

            if (officialName == null)
            {
                Logger.LogError("Retrieving official name failed.");
                return;
            }

            if (companyName == null)
            {
                Logger.LogError("Retrieving company name failed.");
                return;
            }

            if (topLevelDomain == null)
            {
                Logger.LogError("Retrieving top level domain failed.");
                return;
            }

            if (version == null)
            {
                Logger.LogError("Retrieving version failed.");
                return;
            }

            if (minimumUnityVersion == null)
            {
                Logger.LogError("Retrieving minimum unity version failed.");
                return;
            }

            if (embeddedOrLocal == null)
            {
                Logger.LogError("Retrieving embedded or local failed.");
                return;
            }

            var fullName = $"{topLevelDomain}.{companyName}.{officialName}";

            if (!Data.PackageNameRegex().IsMatch(fullName))
            {
                Logger.LogError($"Generated package name is invalid \"{fullName}\"");
                return;
            }
            var isLocalPackage = embeddedOrLocal == "local";

            if (isLocalPackage)
            {
                MakeLocalPackage(
                    new LocalPackageOptions(
                        OfficialName: officialName,
                        FullName: fullName,
                        Version: version,
                        Description: description,
                        DisplayName: displayName,
                        MinimumUnityVersion: minimumUnityVersion,
                        GenerateDocs: generateDocs.Value,
                        FolderName: folderName
                        ));
            }
            else
            {
                var unityProjectRoot = Misc.FindUnityProjectRoot();
                if (unityProjectRoot is null)
                {
                    Logger.LogError("Could not find unity host project root.");
                    return;
                }

                Logger.LogInfo($"Initializing package in unity host project {unityProjectRoot}");

                MakeEmbeddedPackage(
                    new EmbeddedPackageOptions(
                        OfficialName: officialName,
                        FullName: fullName,
                        UnityProjectRoot: unityProjectRoot,
                        Version: version,
                        Description: string.IsNullOrEmpty(description) ? null : description,
                        DisplayName: string.IsNullOrEmpty(displayName) ? null : displayName,
                        MinimumUnityVersion: minimumUnityVersion,
                        GenerateDocs: generateDocs.Value
                        ));
            }
        }

        private static void MakeEmbeddedPackage(EmbeddedPackageOptions options)
        {
            var packagesRoot = Path.Combine(options.UnityProjectRoot, "Packages");
            var packageRoot = Path.Combine(packagesRoot, options.FullName);
            MakePackageCommon(options, packageRoot);
        }

        private static void MakeLocalPackage(LocalPackageOptions options)
        {
            var packageRoot = Path.Combine(Directory.GetCurrentDirectory(), options.FolderName ?? options.FullName);
            MakePackageCommon(options, packageRoot);
        }

        private static void MakePackageCommon(CommonPackageOptions options, string packageRoot)
        {
            SafeMode.Prompt($"Creating directory at '{packageRoot}'");
            try
            {
                _ = Directory.CreateDirectory(packageRoot);
                var packageJsonPath = Path.Combine(packageRoot, "package.json");
                var packageJson = new Data.PackageJson
                {
                    Name = options.FullName,
                    Version = options.Version,
                    Description = options.Description,
                    DisplayName = options.DisplayName,
                };
                var packageJsonString = JsonConvert.SerializeObject(packageJson,
                                                                    Formatting.Indented,
                                                                    new JsonSerializerSettings
                                                                    {
                                                                        NullValueHandling = NullValueHandling.Ignore
                                                                    });
                SafeMode.Prompt($"Writing generated json to to '{packageJsonPath}'");
                File.WriteAllText(packageJsonPath, packageJsonString);
                var initialReadme = new StringBuilder()
                    .Append("# ")
                    .AppendLine(options.DisplayName ?? options.FullName)
                    .AppendLine()
                    .AppendLine(options.Description ?? "TODO")
                    .AppendLine()
                    .ToString();
                var readmePath = Path.Combine(packageRoot, "README.md");
                SafeMode.Prompt($"Writing initial README.md to '{readmePath}'");
                File.WriteAllText(readmePath, initialReadme);
                var changelogPath = Path.Combine(packageRoot, "CHANGELOG.md");
                SafeMode.Prompt($"Creating empty CHANGELOG.md at '{changelogPath}'");
                File.WriteAllText(changelogPath, "");
                var licensePath = Path.Combine(packageRoot, "LICENSE.md");
                SafeMode.Prompt($"Creating empty LICENSE.md at '{licensePath}'");
                File.WriteAllText(licensePath, "TODO");
                var editorRoot = Path.Combine(packageRoot, "Editor");
                SafeMode.Prompt($"Creating empty Editor folder at '{editorRoot}'");
                _ = Directory.CreateDirectory(editorRoot);
                var runtimeRoot = Path.Combine(packageRoot, "Runtime");
                SafeMode.Prompt($"Creating empty Runtime folder at '{runtimeRoot}'");
                _ = Directory.CreateDirectory(runtimeRoot);
                var testsRoot = Path.Combine(packageRoot, "Tests");
                SafeMode.Prompt($"Creating empty Tests folder at '{testsRoot}'");
                _ = Directory.CreateDirectory(testsRoot);
                var editorTestsRoot = Path.Combine(testsRoot, "Editor");
                SafeMode.Prompt($"Creating empty Editor folder at '{editorTestsRoot}'");
                _ = Directory.CreateDirectory(editorTestsRoot);
                var runtimeTestsRoot = Path.Combine(testsRoot, "Runtime");
                SafeMode.Prompt($"Creating empty Runtime folder at '{runtimeTestsRoot}'");
                _ = Directory.CreateDirectory(runtimeTestsRoot);
                var samplesRoot = Path.Combine(packageRoot, "Samples~");
                SafeMode.Prompt($"Creating empty Samples~ folder at '{samplesRoot}'");
                _ = Directory.CreateDirectory(samplesRoot);
                var documentationRoot = Path.Combine(packageRoot, "Documentation~");
                SafeMode.Prompt($"Creating empty Documentation~ folder at '{documentationRoot}'");
                _ = Directory.CreateDirectory(documentationRoot);
                var editorAssemblyDefinitionPath = Path.Combine(editorRoot, options.FullName + ".Editor.asmdef");
                var editorAssemblyDefinitionJson = new JObject
                {
                    ["name"] = options.FullName + ".Editor",
                    ["rootNamespace"] = "",
                    ["references"] = new JArray { options.FullName },
                    ["includePlatforms"] = new JArray { "Editor" },
                    ["excludePlatforms"] = new JArray(),
                    ["allowUnsafeCode"] = false,
                    ["overrideReferences"] = false,
                    ["precompiledReferences"] = new JArray(),
                    ["autoReferenced"] = true,
                    ["defineConstraints"] = new JArray(),
                    ["versionDefines"] = new JArray(),
                    ["noEngineReferences"] = false
                }.ToString(Formatting.Indented);
                SafeMode.Prompt($"Writing generated asmdef to '{editorAssemblyDefinitionPath}'");
                File.WriteAllText(
                    editorAssemblyDefinitionPath,
                    editorAssemblyDefinitionJson);
                var runtimeAssemblyDefinitionPath = Path.Combine(runtimeRoot, options.FullName + ".asmdef");
                var runtimeAssemblyDefinitionJson = new JObject
                {
                    ["name"] = options.FullName,
                }.ToString(Formatting.Indented);
                SafeMode.Prompt($"Writing generated asmdef to '{runtimeAssemblyDefinitionPath}'");
                File.WriteAllText(
                    runtimeAssemblyDefinitionPath,
                    runtimeAssemblyDefinitionJson);
                var editorTestsAssemblyDefinitionPath = Path.Combine(editorTestsRoot, options.FullName + ".Editor.Tests.asmdef");
                var editorTestsAssemblyDefinitionJson = new JObject
                {
                    ["name"] = options.FullName + ".Editor.Tests",
                    ["rootNamespace"] = "",
                    ["references"] = new JArray {
                "UnityEngine.TestRunner",
                "UnityEditor.TestRunner",
                options.FullName,
                options.FullName + ".Editor"
                },
                    ["includePlatforms"] = new JArray { "Editor" },
                    ["excludePlatforms"] = new JArray(),
                    ["allowUnsafeCode"] = false,
                    ["overrideReferences"] = true,
                    ["precompiledReferences"] = new JArray {
                "nunit.framework.dll"
                },
                    ["autoReferenced"] = false,
                    ["defineConstraints"] = new JArray {
                "UNITY_INCLUDE_TESTS"
                },
                    ["versionDefines"] = new JArray(),
                    ["noEngineReferences"] = false
                }.ToString(Formatting.Indented);
                SafeMode.Prompt($"Writing generated asmdef to '{editorTestsAssemblyDefinitionPath}'");
                File.WriteAllText(
                    editorTestsAssemblyDefinitionPath,
                    editorTestsAssemblyDefinitionJson);
                var runtimeTestsAssemblyDefinitionPath = Path.Combine(runtimeTestsRoot, options.FullName + ".Tests.asmdef");
                var runtimeTestsAssemblyDefinitionJson = new JObject
                {
                    ["name"] = options.FullName + ".Tests",
                    ["references"] = new JArray {
                    options.FullName
                    },
                    ["optionalUnityReferences"] = new JArray {
                    "TestAssemblies"
                    }
                }.ToString(Formatting.Indented);
                SafeMode.Prompt($"Writing generated asmdef to '{runtimeTestsAssemblyDefinitionPath}'");
                File.WriteAllText(
                    runtimeTestsAssemblyDefinitionPath,
                    runtimeTestsAssemblyDefinitionJson);
                var indexDocPath = Path.Combine(documentationRoot, options.OfficialName + ".md");
                if (options.GenerateDocs)
                {
                    SafeMode.Prompt($"Writing generated documentation to '{indexDocPath}'");
                    File.WriteAllText(indexDocPath, "# TODO\n");
                    Docs.SetupDoxygen(documentationRoot, options.DisplayName ?? options.OfficialName, options.Description);
                }
                // TODO: add git repo and gitignore
                // TODO: add license functionality
                //   [[https://spdx.org/licenses/][SPDX License List | Software Package Data Exchange (SPDX)]]
                // TODO: add changelog functionality
                //   [[https://keepachangelog.com/en/1.0.0/][Keep a Changelog]]
                //   [[https://www.npmjs.com/package/remark][remark - npm]]
                if (options.FullName.Length > 50)
                {
                    Logger.LogWarning("Package name is too long to show in editor");
                }
                Logger.LogInfo("Package initialized");
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to configure package: " + e.Message);
                // delete packageRoot
                try
                {
                    SafeMode.Prompt($"Deleting directory '{packageRoot}'");
                    Directory.Delete(packageRoot, true);
                }
                catch (Exception e2)
                {
                    Logger.LogError($"Failed to delete package root: " + e2.Message);
                }
                return;
            }
        }

        internal static void Category(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            for (int i = 0;
                 i < args.Length;
#pragma warning disable CS0162
                 // Currently all arguments end execution so this is unreachable, but it may not be that way in the future
                 ++i
#pragma warning restore CS0162
                )
            {
                var arg = args[i];
                if (arg is "-h" or "--help")
                {
                    PrintHelp();
                    return;
                }
                else if (arg is "init")
                {
                    InitPackage();
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

        private static void PrintHelp()
        {
            Console.WriteLine(
                @"upak pack: Tools for automating unity package operations

usage: upak pack [-h | --help] [command]

Commands:
    init        Initialize new package in current unity project
");
        }
    }
}
