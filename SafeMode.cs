using System;

namespace Upak
{
    internal static class SafeMode
    {
        internal static void Prompt(string message)
        {
            Console.WriteLine("*Safe Mode*");
            Console.WriteLine("CWD: " + Environment.CurrentDirectory);
            Console.WriteLine(message);
            // Prompt for yes or no
            Console.WriteLine("Do you want to continue? (y/n)");
            string? response = Console.ReadLine();
            if (response?.ToLower(System.Globalization.CultureInfo.InvariantCulture) != "y")
            {
                Console.WriteLine("Exiting...");
                Environment.Exit(0);
            }
        }
    }
}
