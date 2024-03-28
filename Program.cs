using System;

namespace Upak
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            for (int i = 0; i < args.Length; ++i)
            {
                var arg = args[i];
                if (arg is "-v" or "--version")
                {
                    PrintVersion();
                    return;
                }
                else if (arg is "-h" or "--help")
                {
                    PrintHelp();
                    return;
                }
                else if (arg is "--safe")
                {
                    SafeMode.Enabled = true;
                }
                else if (arg is "pack")
                {
                    Pack.Category(args[(i + 1)..]);
                    return;
                }
                else if (arg is "nuget")
                {
                    Nuget.Category(args[(i + 1)..]);
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
                @"upak: A CLI for automating unity package operations

usage: upak [-v | --version] [-h | --help] [--safe] <category> [<args>]

Category:
    pack        Tools for automating unity package operations
    nuget       A collection of tools for using nuget packages in unity
");
        }

        private static void PrintVersion()
        {
            Console.Write("upak ");
            Console.WriteLine(typeof(Program).Assembly.GetName().Version?.ToString());
        }
    }
}

