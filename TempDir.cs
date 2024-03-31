using System;
using System.IO;
using System.Security;

namespace Upak
{
    internal sealed class TempDir : IDisposable
    {
        private DirectoryInfo? _directoryInfo;

        public string Path => _directoryInfo is null
                    ? throw new InvalidOperationException("Accessing path while in invalid state")
                    : _directoryInfo.FullName;

        public TempDir()
        {
            SafeMode.Prompt("Creating temporary directory");
            _directoryInfo = Directory.CreateTempSubdirectory();
        }

        public void Dispose()
        {
            DisposeImpl();
            GC.SuppressFinalize(this);
        }

        private void DisposeImpl()
        {
            if (_directoryInfo is not null)
            {
                var errorPrefix = "Failed to delete temporary directory: ";
                try
                {
                    SafeMode.Prompt($"Deleting temporary directory '{Path}'");
                    _directoryInfo.Delete(true);
                }
                catch (UnauthorizedAccessException e)
                {
                    Logger.LogError(errorPrefix + "It contains a read-only file\n" + e.Message);
                }
                catch (DirectoryNotFoundException)
                {
                    // Ignore
                }
                catch (IOException e)
                {
                    Logger.LogError(errorPrefix + "An I/O error occurred while deleting the directory\n" + e.Message);
                }
                catch (SecurityException e)
                {
                    Logger.LogError(errorPrefix + "The user does not have the required permissions\n" + e.Message);
                }
                _directoryInfo = null;
            }
        }

        ~TempDir()
        {
            DisposeImpl();
        }
    }
}
