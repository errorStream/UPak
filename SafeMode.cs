using System;

namespace Upak
{
    internal static class SafeMode
    {
        public static bool Enabled { get; internal set; }

        internal static void Prompt(string message)
        {
            if (!Enabled) { return; }
            Console.WriteLine("*Safe Mode*");
            Console.WriteLine("CWD: " + Environment.CurrentDirectory);
            Console.WriteLine(message);
            // Prompt for yes or no
            Console.WriteLine("Do you want to continue? (y/n)");
            string? response = Console.ReadLine();
            if (response?.ToUpperInvariant() != "Y")
            {
                Console.WriteLine("Exiting...");
                Environment.Exit(0);
            }
        }
    }
}
