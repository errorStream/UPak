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
    internal class Pack
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

        private record EmbeddedPackageOptions(
            string OfficialName,
            string FullName,
            string Version,
            string? Description,
            string? DisplayName,
            string? MinimumUnityVersion,
            bool GenerateDocs,
            string UnityProjectRoot
            ) : CommonPackageOptions(OfficialName, FullName, Version, Description, DisplayName, MinimumUnityVersion, GenerateDocs);

        private record LocalPackageOptions(
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
            return input =>
            {
                if (input is not string strValue)
                {
                    return ValidationResult.Success;
                }

                if (regex.IsMatch(strValue))
                {
                    return ValidationResult.Success;
                }

                return new ValidationResult(errorMessage);
            };
        }

        private static Func<object?, ValidationResult?> ValidatePredicate(Predicate<object?> predicate, string errorMessage)
        {
            return input =>
            {
                if (predicate(input))
                {
                    return ValidationResult.Success;
                }

                return new ValidationResult(errorMessage);
            };
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
            generateDocs = Prompt.Input<bool>("Generate Docs?",
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
                Console.WriteLine("Retrieving official name failed. Exiting.");
                return;
            }

            if (companyName == null)
            {
                Console.WriteLine("Retrieving company name failed. Exiting.");
                return;
            }

            if (topLevelDomain == null)
            {
                Console.WriteLine("Retrieving top level domain failed. Exiting.");
                return;
            }

            if (version == null)
            {
                Console.WriteLine("Retrieving version failed. Exiting.");
                return;
            }

            if (minimumUnityVersion == null)
            {
                Console.WriteLine("Retrieving minimum unity version failed. Exiting.");
                return;
            }

            if (generateDocs == null)
            {
                Console.WriteLine("Retrieving generate docs failed. Exiting.");
                return;
            }

            if (embeddedOrLocal == null)
            {
                Console.WriteLine("Retrieving embedded or local failed. Exiting.");
                return;
            }

            var fullName = $"{topLevelDomain}.{companyName}.{officialName}";

            if (!Data.PackageNameRegex().IsMatch(fullName))
            {
                Console.WriteLine($"Generated package name is invalid \"{fullName}\"");
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
                    Console.WriteLine("[ERROR] Could not find unity host project root.");
                    Environment.Exit(1); // NOTE: May want to rework this
                }
                else
                {
                    Console.WriteLine($"Initializing package in unity host project {unityProjectRoot}");
                }

                MakeEmbeddedPackage(
                    new EmbeddedPackageOptions(
                        OfficialName: officialName,
                        FullName: fullName,
                        UnityProjectRoot: unityProjectRoot,
                        Version: version,
                        Description: description == "" ? null : description,
                        DisplayName: displayName == "" ? null : displayName,
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
            Directory.CreateDirectory(packageRoot);
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
            Directory.CreateDirectory(editorRoot);
            var runtimeRoot = Path.Combine(packageRoot, "Runtime");
            SafeMode.Prompt($"Creating empty Runtime folder at '{runtimeRoot}'");
            Directory.CreateDirectory(runtimeRoot);
            var testsRoot = Path.Combine(packageRoot, "Tests");
            SafeMode.Prompt($"Creating empty Tests folder at '{testsRoot}'");
            Directory.CreateDirectory(testsRoot);
            var editorTestsRoot = Path.Combine(testsRoot, "Editor");
            SafeMode.Prompt($"Creating empty Editor folder at '{editorTestsRoot}'");
            Directory.CreateDirectory(editorTestsRoot);
            var runtimeTestsRoot = Path.Combine(testsRoot, "Runtime");
            SafeMode.Prompt($"Creating empty Runtime folder at '{runtimeTestsRoot}'");
            Directory.CreateDirectory(runtimeTestsRoot);
            var samplesRoot = Path.Combine(packageRoot, "Samples~");
            SafeMode.Prompt($"Creating empty Samples~ folder at '{samplesRoot}'");
            Directory.CreateDirectory(samplesRoot);
            var documentationRoot = Path.Combine(packageRoot, "Documentation~");
            SafeMode.Prompt($"Creating empty Documentation~ folder at '{documentationRoot}'");
            Directory.CreateDirectory(documentationRoot);
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
            SafeMode.Prompt($"Writing generated documentation to '{indexDocPath}'");
            File.WriteAllText(indexDocPath, "# TODO\n");
            Docs.SetupDoxygen(documentationRoot, options.DisplayName ?? options.OfficialName, options.Description);
            // TODO: add git repo and gitignore
            // TODO: add license functionality
            //   [[https://spdx.org/licenses/][SPDX License List | Software Package Data Exchange (SPDX)]]
            // TODO: add changelog functionality
            //   [[https://keepachangelog.com/en/1.0.0/][Keep a Changelog]]
            //   [[https://www.npmjs.com/package/remark][remark - npm]]
            if (options.FullName.Length > 50)
            {
                Console.WriteLine("[WARN] Package name is too long to show in editor");
            }
            Console.WriteLine("Package initialized");
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
                else if (arg is "init")
                {
                    InitPackage();
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
