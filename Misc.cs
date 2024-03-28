using System;
using System.IO;

namespace Upak
{
    internal static class Misc
    {
        internal static bool IsUnityProject(string rootPath)
        {
            var packagesPath = Path.Combine(rootPath, "Packages");
            var manifestPath = Path.Combine(packagesPath, "manifest.json");
            return Directory.Exists(packagesPath) && File.Exists(manifestPath);
        }

        internal static bool IsUnityPackage(string rootPath)
        {
            var packagePath = Path.Combine(rootPath, "package.json");
            var rootIsInPackages = Directory.GetParent(rootPath)?.Name == "Packages";
            var packageJsonExists = File.Exists(packagePath);
            return rootIsInPackages && packageJsonExists;
        }

        /// <summary>
        /// Iterate through paths up from <c>CWD</c>, calling transform on each. The
        /// first time transform returns non-null, return result and path
        /// </summary>
        internal static (T result, string path)? FirstDir<T>(Func<string, T?> transform)
        {
            string currPath = Directory.GetCurrentDirectory();
            if (currPath == null)
            {
                throw new IOException("Could not get current directory");
            }
            for (int iterations = 0; iterations < 1000; ++iterations)
            {
                T? transformRes = transform(currPath);
                if (transformRes != null)
                {
                    return (result: transformRes, path: currPath);
                }
                if (currPath is "/" or "C:\\")
                {
                    return null;
                }
                var newCurrPath = Path.GetDirectoryName(currPath);
                if (newCurrPath == null)
                {
                    throw new IOException("Could not get parent directory");
                }
                currPath = newCurrPath;
            }
            throw new TimeoutException("Timeout while trying to find valid directory");
        }

        internal static string? FindValidDir(Predicate<string> predicate)
        {
            var res = FirstDir<bool?>(p => predicate(p) ? true : null);
            return res?.path;
        }

        /// <summary>
        /// Find the nearest parent unity project root path relative to <c>CWD</c>.
        /// </summary>
        // <returns>
        // The path to the package root.
        // </returns>
        internal static string? FindUnityProjectRoot()
        {
            return FindValidDir(IsUnityProject);
        }

        /// <summary>
        /// Find the nearest parent unity project root path relative to <c>CWD</c>
        /// </summary>
        /// <returns>
        /// The path to the package root
        /// </returns>
        internal static string? FindUnityPackageRoot(string path)
        {
            return FindValidDir(IsUnityPackage);
        }

        internal const int IndentSize = 4;
    }
}
