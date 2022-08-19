using THUMDER.Interpreter;

namespace THUMDER
{ 
    public static class THUMDER
    {
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
                        Console.WriteLine("THUMDER Core " + version);
                        Console.WriteLine("");
                        Console.WriteLine("Usage: THUMDER [options] [file]");
                        Console.WriteLine("");
                        Console.WriteLine("Options:");
                        Console.WriteLine("  -h --help                          Show this help message and exit");
                        Console.WriteLine("  -S --server                        Launch as a network server");
                        Console.WriteLine("  -v --version                       Show version information and exit");
                        break;
                    case "-v":
                    case "--version":
                        Console.WriteLine("THUMDER Core " + version);
                        Console.WriteLine("Copyright © 2022 Escuela Politécnica Superior de Jaén.");
                        Console.WriteLine("License GPLv3+: GNU GPL version 3 or later <https://gnu.org/licenses/gpl.html>.");
                        Console.WriteLine("This is free software: you are free to change and redistribute it.");
                        Console.WriteLine("There is NO WARRANTY, to the extent permitted by law.");
                        Console.WriteLine("\n Written by Alberto Rodríguez Torres.");
                        break;
                    case "-S":
                    case "-s":
                    case "--server":
                        Console.WriteLine("Starting THUMDER Core as a server backend.");
                        //Start TCP socket
                        return;
                    default:
                        if (File.Exists(args[0]))
                        {
                            string[] file = File.ReadAllLines(args[0]);
                        }
                        else
                        {
                            Console.WriteLine("File not found");
                        }
                        break;
                }
            }
            else
            {
                Console.WriteLine("Invalid argument.");
                Console.WriteLine("THUMDER Core " + version);
                Console.WriteLine("");
                Console.WriteLine("Usage: THUMDER [options] [file]");
                Console.WriteLine("");
                Console.WriteLine("Options:");
                Console.WriteLine("  -h --help                          Show this help message and exit");
                Console.WriteLine("  -S --server                        Launch as a network server");
                Console.WriteLine("  -v --version                       Show version information and exit");
            }
            return;
        }
    }
}