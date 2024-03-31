using System;

namespace Upak
{
    internal static class Logger
    {
        internal static void LogInfo(string message)
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("INFO");
            Console.ResetColor();
            Console.Write("] ");
            Console.WriteLine(message);
        }

        internal static void LogError(string message)
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("ERROR");
            Console.ResetColor();
            Console.Write("] ");
            Console.WriteLine(message);
        }

        internal static void LogWarning(string message)
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("WARNING");
            Console.ResetColor();
            Console.Write("] ");
            Console.WriteLine(message);
        }
    }
}
