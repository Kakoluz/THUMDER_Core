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
                        Console.WriteLine("Copyright © 2022 Alberto Rodríguez Torres");
                        Console.WriteLine("License GPLv3+: GNU GPL version 3 or later <https://gnu.org/licenses/gpl.html>.");
                        Console.WriteLine("This is free software: you are free to change and redistribute it.");
                        Console.WriteLine("There is NO WARRANTY, to the extent permitted by law.");
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
                ASM assembly = Assembler.Decode(file);
            }
            else
            {
                throw new FileNotFoundException("File not found or can't be read.");
            }
        }
    }
}