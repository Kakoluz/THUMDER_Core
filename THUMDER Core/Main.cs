using System;
using System.Security.Cryptography.X509Certificates;
using THUMDER.Deluxe;
using THUMDER.Interpreter;

namespace THUMDER
{ 
    public static class THUMDER
    {
        /// <summary>
        /// Contains the help command info.
        /// </summary>
        /// <param name="version">The program version to print.</param>
        static void PrintHelp(string version)
        {
            Console.WriteLine("THUMDER Core " + version);
            Console.WriteLine("");
            Console.WriteLine("Usage: THUMDER [options] [file]");
            Console.WriteLine("");
            Console.WriteLine("Options:");
            Console.WriteLine("  -h --help                          Show this help message and exit");
            Console.WriteLine("  -S --server                        Launch as a network server");
            Console.WriteLine("  -v --version                       Show version information and exit");
        }

        /// <summary>
        /// Contains the legal and version of the program.
        /// </summary>
        /// <param name="version">The program version to print.</param>
        static void PrintVersion(string version)
        {
            Console.WriteLine("THUMDER Core " + version);
            Console.WriteLine("Copyright © 2022 Alberto Rodríguez Torres");
            Console.WriteLine("License GPLv3+: GNU GPL version 3 or later <https://gnu.org/licenses/gpl.html>.");
            Console.WriteLine("This is free software: you are free to change and redistribute it.");
            Console.WriteLine("There is NO WARRANTY, to the extent permitted by law.");
        }

        /// <summary>
        /// The main menu of the local application.
        /// </summary>
        static readonly string menu = "                 Main menu \n" +
                                  "Press the key corresponding to the action you want to perform: \n" +
                                  "F1. Show this menu again\n" +
                                  "\n" +
                                  "F5. Run program \n" +
                                  "F6. Step in \n" +
                                  "F7. Run until breakpoint \n" +
                                  "\n" +
                                  "F3. Print registers \n" +
                                  "F4. Memory explorer \n" +
                                  "\n" +
                                  "F9. Set breakpoint \n" +
                                  "F10. Remove breakpoint \n" +
                                  "\n" +
                                  "F11. Print stats \n" +
                                  "Q. Exit \n";

        /// <summary>
        /// Contains the programs entry point.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.Title = "THUMDER Core";
            string version = System.Reflection.Assembly.GetExecutingAssembly()
                                                        .GetName().Version
                                                        .ToString();
            if (args.Length > 0)
            {
                switch(args[0])
                {
                    case "-h":
                    case "--help":
                        PrintHelp(version);
                        break;
                    case "-v":
                    case "--version":
                        PrintVersion(version);
                        break;
                    case "-S":
                    case "-s":
                    case "--server":
                        Console.WriteLine("Starting THUMDER Core as a server backend.");
                        //Start TCP socket
                        return;
                    default:
                        try
                        {
                            Console.WriteLine("Starting THUMDER Core locally.");
                            RunLocally(args[0]);
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e.Message);
                            Console.ReadKey();
                        }
                        break;
                }
            }
            else
            {
                PrintHelp(version);
            }
            return;
        }

        /// <summary>
        /// Starts THUMDER as a local emulator.
        /// </summary>
        /// <param name="PathToFile">Path to the file to load.</param>
        /// <exception cref="FileNotFoundException">The file doesn't exist or can't be read.</exception>
        public static void RunLocally(string PathToFile)
        {
            if (File.Exists(PathToFile))
            {

                string[] file = File.ReadAllLines(PathToFile);
                Console.WriteLine("Reading file contents and checking for errors.");
                ASM assembly = Assembler.Decode(file);
                Console.WriteLine("Loading file " + PathToFile + " into memory and starting THUMDER Core.");
                SimManager.LoadProgram(assembly);
                Console.WriteLine("Loaded program " + Path.GetFileName(PathToFile) + "\n");
                bool exitFlag = false;
                while (!exitFlag)
                {
                    Console.WriteLine(menu);
                    switch (Console.ReadKey(false).Key)
                    {
                        case ConsoleKey.F1:
                            Console.Clear();
                            Console.WriteLine(menu);
                            break;
                        case ConsoleKey.F5:
                            Console.Clear();
                            SimManager.RunFullSimulation();
                            break;
                        case ConsoleKey.F6:
                            Console.Clear();
                            SimManager.RunACycle();
                            break;
                        case ConsoleKey.F7:
                            Console.Clear();
                            SimManager.RunUntilBreakpoint();
                            break;
                        case ConsoleKey.F3:
                            Console.Clear();
                            Console.WriteLine(SimManager.PrintRegisters());

                            Console.WriteLine("\n Press any key to return.");
                            Console.ReadKey(false);
                            Console.Clear();
                            break;
                        case ConsoleKey.F4:
                            //SimManager.MemoryExplorer();
                            break;
                        case ConsoleKey.F9:
                            Console.Clear();
                            Console.WriteLine("Enter the address of the breakpoint: ");
                            string input = Console.ReadLine();
                            try
                            {
                                int address = Convert.ToInt32(input);
                                if (address != null && address >= 0 && address < SimManager.Memsize)
                                {
                                    SimManager.SetBreakpoint(address);
                                    Console.WriteLine("Breakpoint added successfully.");
                                }
                                else
                                {
                                    Console.WriteLine("Invalid address. \n" +
                                        "Returning to menu.");
                                }
                            }
                            catch (FormatException)
                            {
                                Console.WriteLine("The input is not a number. \n" +
                                    "Returning to menu.");
                            }
                            break;
                        case ConsoleKey.F10:
                            Console.Clear();
                            Console.WriteLine("Enter the address of the breakpoint: ");
                            input = Console.ReadLine();
                            try
                            {
                                int address = Convert.ToInt32(input);
                                if (address != null && address >= 0 && address < SimManager.Memsize)
                                {
                                    SimManager.RemoveBreakpoint(address);
                                    Console.WriteLine("Breakpoint added successfully.");
                                }
                                else
                                {
                                    Console.WriteLine("Invalid address. \n" +
                                        "Returning to menu.");
                                }
                            }
                            catch (FormatException)
                            {
                                Console.WriteLine("The input is not a number. \n" +
                                    "Returning to menu.");
                            }
                            break;
                            break;
                        case ConsoleKey.F11:
                            SimManager.PrintStats();
                            break;
                        case ConsoleKey.Q:
                            exitFlag = true;
                            break;
                    }
                }               
            }
            else
            {
                throw new FileNotFoundException("File not found or can't be read.");
            }
        }
    }
}