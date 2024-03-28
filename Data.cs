using System.Text.RegularExpressions;
using System.Collections.Generic;

using Email = System.String;
using Url = System.String;
using Path = System.String;
using PackageName = System.String;
using SemVar = System.String;

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

        [GeneratedRegex("^[a-z_.-]{1,214}$", RegexOptions.None, "en-US")]
        internal static partial Regex PackageNameRegex();


        internal class Author
        {
            internal required string Name { get; init; }
            internal Email? Email { get; init; }
            internal Url? Url { get; init; }
        }

        internal class Sample
        {
            internal required string DisplayName { get; init; }
            internal string? Description { get; init; }
            internal required Path Path { get; init; }
        }

        internal class PackageJson
        {
            internal required PackageName Name { get; init; }
            internal required SemVar Version { get; init; }
            internal string? Description { get; init; }
            internal string? DisplayName { get; init; }
            internal SemVar? Unity { get; init; }
            internal Author? Author { get; init; }
            internal Url? ChangelogUrl { get; init; }
            internal Dictionary<string, SemVar>? Dependencies { get; init; }
            internal Url? DocumentationUrl { get; init; }
            internal bool? HideInEditor { get; init; }
            internal string[]? Keywords { get; init; }
            internal string? License { get; init; }
            internal Url? LicensesUrl { get; init; }
            internal Sample[]? Samples { get; init; }
            internal SemVar? UnityRelease { get; init; }
        }
    }
}
