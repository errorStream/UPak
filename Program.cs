using System;
using System.Reflection;
using System.Diagnostics;

namespace Upak
{
    static class Program
    {
        static void Main(string[] args)
        {
            static void PrintHelp()
            {
                Console.WriteLine(
                    @"upak: A CLI for automating unity package operations

usage: upak [-v | --version] [-h | --help] <category> [<args>]

Category:
    pack        Tools for automating unity package operations
    nugit       A collection of tools for using nugit packages in unity
");
            }
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }
            else if (args[0] is "-v" or "--version")
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                string? version = fileVersionInfo.ProductVersion;

                Console.Write("upak ");
                Console.WriteLine(version);
                return;
            }
            else if (args[0] is "-h" or "--help")
            {
                PrintHelp();
                return;
            }
            else if (args[0] is "pack")
            {
                Pack.Category(args[1..]);
                return;
            }
            else if (args[0] is "nugit")
            {
                Nugit.Category(args[1..]);
                return;
            }
            else
            {
                Console.WriteLine("Failed to parse command line arguments.\n");
                PrintHelp();
                return;
            }
        }
    }
}

