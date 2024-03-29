using System;
using System.IO;

namespace Upak
{
    internal class TempDir : IDisposable
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_directoryInfo is not null)
            {
                try
                {
                    SafeMode.Prompt($"Deleting temporary directory '{Path}'");
                    _directoryInfo.Delete(true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to delete temporary directory: {e.Message}");
                }
                _directoryInfo = null;
            }
        }

        ~TempDir()
        {
            Dispose(false);
        }
    }
}
