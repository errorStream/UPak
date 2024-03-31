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
            try
            {
                var rootIsInPackages = Directory.GetParent(rootPath)?.Name == "Packages";
                var packageJsonExists = File.Exists(packagePath);
                return rootIsInPackages && packageJsonExists;
            }
            catch (Exception e)
            {
                Logger.LogWarning("Unexpected error while checking if Unity package: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Iterate through paths up from <c>CWD</c>, calling transform on each. The
        /// first time transform returns non-null, return result and path
        /// </summary>
        internal static (T result, string path)? FirstDir<T>(Func<string, T?> transform)
        {
            string? currPath;

            try
            {
                currPath = Directory.GetCurrentDirectory();
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to get current directory: " + e.Message);
                return null;
            }

            if (currPath == null)
            {
                Logger.LogError("Could not get current directory");
                return null;
            }

            for (int iterations = 0; iterations < 1000; ++iterations)
            {
                T? transformRes = transform(currPath);
                if (transformRes != null)
                {
                    return (result: transformRes, path: currPath);
                }
                if (currPath == Path.GetPathRoot(currPath))
                {
                    return null;
                }
                string? newCurrPath = null;
                try
                {
                    newCurrPath = Path.GetDirectoryName(currPath);
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to get parent directory: " + e.Message);
                }
                if (newCurrPath == null)
                {
                    Logger.LogError("Could not get parent directory");
                    return null;
                }
                currPath = newCurrPath;
            }
            Logger.LogError("Timeout while trying to find valid directory");
            return null;
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
        internal static string? FindUnityPackageRoot()
        {
            return FindValidDir(IsUnityPackage);
        }

        internal const int IndentSize = 4;
    }
}
