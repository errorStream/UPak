using System.Text.RegularExpressions;
using System.Collections.Generic;

using Email = System.String;
using Url = System.String;
using Path = System.String;
using PackageName = System.String;
using SemVar = System.String;
using Newtonsoft.Json;

namespace Upak
{
    internal static partial class Data
    {
        [GeneratedRegex("^(([^<>()\\[\\]\\\\.,;:\\s@\"]+(\\.[^<>()\\[\\]\\\\.,;:\\s@\"]+)*)|(\".+\"))@((\\[[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}])|(([a-zA-Z\\-0-9]+\\.)+[a-zA-Z]{2,}))$", RegexOptions.None, "en-US")]
        internal static partial Regex EmailRegex();

        [GeneratedRegex(@"^(?:(?<=^v?|\sv?)(?:(?:0|[1-9]\d{0,9})\.){2}(?:0|[1-9]\d{0,9})(?:-(?:0|[1-9]\d*?|[\da-z-]*?[a-z-][\da-z-]*?){0,100}(?:\.(?:0|[1-9]\d*?|[\da-z-]*?[a-z-][\da-z-]*?))*?){0,100}(?:\+[\da-z-]+?(?:\.[\da-z-]+?)*?){0,100}\b){1,200}$", RegexOptions.IgnoreCase, "en-US")]
        internal static partial Regex SemVarRegex();

        [GeneratedRegex(@"^((([A-Za-z]{3,9}:(?:\/\/)?)(?:[\-;:&=\+\$,\w]+@)?[A-Za-z0-9\.\-]+|(?:www\.|[\-;:&=\+\$,\w]+@)[A-Za-z0-9\.\-]+)((?:\/[\+~%\/\.\w\-_]*)?\??(?:[\-\+=&;%@\.\w_]*)#?(?:[\.\!\/\\\w]*))?)$", RegexOptions.None, "en-US")]
        internal static partial Regex UrlRegex();

        [GeneratedRegex(@"^((?:[^/]*/)*)(.*)$", RegexOptions.None, "en-US")]
        internal static partial Regex PathRegex();

        [GeneratedRegex("^[a-z_.-]+$", RegexOptions.None, "en-US")]
        internal static partial Regex PackageNamePartRegex();

        [GeneratedRegex("^[a-z_.-]+$", RegexOptions.IgnoreCase, "en-US")]
        internal static partial Regex PackageNamePartCIRegex();

        [GeneratedRegex("^[a-z_.-]{1,214}$", RegexOptions.None, "en-US")]
        internal static partial Regex PackageNameRegex();


        internal class Author
        {
            [JsonProperty("name")]
            internal required string Name { get; init; }

            [JsonProperty("email")]
            internal Email? Email { get; init; }

            [JsonProperty("url")]
            internal Url? Url { get; init; }
        }

        internal class Sample
        {
            [JsonProperty("displayName")]
            internal required string DisplayName { get; init; }

            [JsonProperty("description")]
            internal string? Description { get; init; }

            [JsonProperty("path")]
            internal required Path Path { get; init; }
        }

        internal class PackageJson
        {
            [JsonProperty("name")]
            internal required PackageName Name { get; init; }

            [JsonProperty("version")]
            internal required SemVar Version { get; init; }

            [JsonProperty("description")]
            internal string? Description { get; init; }

            [JsonProperty("displayName")]
            internal string? DisplayName { get; init; }

            [JsonProperty("unity")]
            internal SemVar? Unity { get; init; }

            [JsonProperty("author")]
            internal Author? Author { get; init; }

            [JsonProperty("changelogUrl")]
            internal Url? ChangelogUrl { get; init; }

            [JsonProperty("dependencies")]
            internal Dictionary<string, SemVar>? Dependencies { get; init; }

            [JsonProperty("documentationUrl")]
            internal Url? DocumentationUrl { get; init; }

            [JsonProperty("hideInEditor")]
            internal bool? HideInEditor { get; init; }

            [JsonProperty("keywords")]
            internal string[]? Keywords { get; init; }

            [JsonProperty("license")]
            internal string? License { get; init; }

            [JsonProperty("licensesUrl")]
            internal Url? LicensesUrl { get; init; }

            [JsonProperty("samples")]
            internal Sample[]? Samples { get; init; }

            [JsonProperty("unityRelease")]
            internal SemVar? UnityRelease { get; init; }
        }
    }
}
