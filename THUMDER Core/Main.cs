using System.Runtime.ExceptionServices;
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
                                  "R. Reset CPU \n" +
                                  "L. Reload Program \n" +
                                  "\n" +
                                  "F3. Print registers \n" +
                                  "F4. Memory explorer \n" +
                                  "\n" +
                                  "C. Code explorer\n" +
                                  "F9. Set breakpoint \n" +
                                  "F10. Remove breakpoint \n" +
                                  "\n" +
                                  "T. Settings \n" +
                                  "S. Print stats \n" +
                                  "P. Print Pipeline \n" +
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
                        case ConsoleKey.F5:
                            Console.Clear();
                            SimManager.RunFullSimulation();
                            Console.SetCursorPosition(30, 5);
                            Console.WriteLine("Cycles: " + SimManager.Instance.Cycles + "\n");
                            Console.SetCursorPosition(0, 0);
                            break;
                        case ConsoleKey.F6:
                            Console.Clear();
                            SimManager.RunACycle();
                            Console.SetCursorPosition(30 ,5);
                            Console.WriteLine("Cycles: " + SimManager.Instance.Cycles + "\n" + SimManager.PrintPipelineShort());
                            Console.SetCursorPosition(0,0);
                            break;
                        case ConsoleKey.F7:
                            Console.Clear();
                            SimManager.RunUntilBreakpoint();
                            Console.SetCursorPosition(30, 5);
                            Console.WriteLine("Cycles: " + SimManager.Instance.Cycles + "\n" + SimManager.PrintPipelineShort());
                            Console.SetCursorPosition(0, 0);
                            break;
                        case ConsoleKey.F3:
                            Console.Clear();
                            Console.WriteLine(SimManager.PrintRegisters());

                            Console.WriteLine("Press any key to return.");
                            Console.ReadKey(false);
                            Console.Clear();
                            break;
                        case ConsoleKey.F4:
                            Console.Clear();
                            Console.WriteLine("Enter the address to read from (Decimal or Hex): ");
                            string text = Console.ReadLine();
                            int textnumber;
                            if (text != null && text.Contains("0x"))
                            {
                                text = text.Replace("0x", "");
                                textnumber = Convert.ToInt32(text, 16);
                            }
                            else if (text != null)
                            {
                                textnumber = Convert.ToInt32(text);
                            }
                            else
                            {
                                Console.Clear();
                                Console.WriteLine("Invalid input.\n" +
                                                  "Returning to menu");
                                break;
                            }
                            if (textnumber < 4)
                                textnumber = 4;
                            else if (textnumber > (SimManager.Memsize - 5))
                                textnumber = (int)(SimManager.Memsize - 5);
                            bool quitMemExplorer = false;
                            Console.Clear();
                            Console.WriteLine("Memory explorer\n");
                            Console.WriteLine(String.Format("{0} {1,-12}", "  Address", "  Content"));
                            int x = Console.GetCursorPosition().Left;
                            int y = Console.GetCursorPosition().Top;
                            Console.WriteLine(SimManager.MemoryExplorer(textnumber));
                            Console.WriteLine("\nNavigate with the arrow keys. Press Q to quit.");
                            while (!quitMemExplorer)
                            {
                                if (textnumber < 4)
                                    textnumber = 4;
                                else if (textnumber > (SimManager.Memsize - 5))
                                    textnumber = (int)(SimManager.Memsize - 5);
                                Console.SetCursorPosition(x,y);
                                Console.WriteLine(SimManager.MemoryExplorer(textnumber));
                                switch (Console.ReadKey(false).Key)
                                {
                                    case ConsoleKey.UpArrow:
                                        ++textnumber;
                                        break;
                                    case ConsoleKey.DownArrow:
                                        --textnumber;
                                        break;
                                    case ConsoleKey.Q:
                                        quitMemExplorer = true;
                                        break;
                                }
                            }
                            Console.Clear();
                            break;
                        case ConsoleKey.C:
                            bool quitCodeExplorer = false;
                            Console.Clear();
                            Console.WriteLine("Instruction explorer\n");
                            Console.WriteLine(String.Format("{0} {1,-12}", "  Address", "       Content"));
                            x = Console.GetCursorPosition().Left;
                            y = Console.GetCursorPosition().Top;
                            textnumber = 0;
                            Console.WriteLine(SimManager.InstructionExplorer(textnumber));
                            Console.WriteLine("\nNavigate with the arrow keys. Press Q to quit.");
                            while (!quitCodeExplorer)
                            {
                                if (textnumber < 4)
                                    textnumber = 4;
                                else if (textnumber > (SimManager.Memsize - 5))
                                    textnumber = (int)(SimManager.Memsize - 5);
                                Console.SetCursorPosition(x, y);
                                for (int i = y + 10; i >= y; i--)
                                {
                                    Console.SetCursorPosition(0, i);
                                    Console.Write(new string(' ', Console.WindowWidth));
                                }
                                Console.WriteLine(SimManager.InstructionExplorer(textnumber));
                                switch (Console.ReadKey(false).Key)
                                {
                                    case ConsoleKey.UpArrow:
                                        textnumber-=4;
                                        break;
                                    case ConsoleKey.DownArrow:
                                        textnumber+=4;
                                        break;
                                    case ConsoleKey.Q:
                                        quitCodeExplorer = true;
                                        break;
                                }

                            }
                            Console.Clear();
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
                        case ConsoleKey.S:
                            Console.Clear();
                            Console.WriteLine(SimManager.PrintStats());
                            
                            Console.WriteLine("Press any key to return.");
                            Console.ReadKey(false);
                            Console.Clear();
                            break;
                        case ConsoleKey.P:
                            Console.Clear();
                            Console.WriteLine(SimManager.PrintPipeline());

                            Console.WriteLine("Press any key to return.");
                            Console.ReadKey(false);
                            Console.Clear();
                            break;
                        case ConsoleKey.Q:
                            exitFlag = true;
                            break;
                        case ConsoleKey.R:
                            Console.Clear();
                            SimManager.Restart();
                            Console.WriteLine("CPU has been resetted.\n");
                            break;
                        case ConsoleKey.L:
                            Console.Clear();
                            SimManager.Reset();
                            Console.WriteLine("Settings and CPU resetted.\n");
                            break;
                        case ConsoleKey.T:
                            bool exitSettings = false;
                            bool settingChanged = false;
                            while (!exitSettings)
                            {
                                Console.Clear();
                                Console.WriteLine("                           Emulation Settings" +
                                                  "\n======================================================================" +
                                                  "\n1. Memory size: " + SimManager.Memsize + " bytes (" + (SimManager.Memsize/1024).ToString("N2") + " kB)" +
                                                  "\n2. Data Forwarding: " + SimManager.Forwarding +
                                                  "\n3. fAdd Units: " + SimManager.ADDUnits +
                                                  "\n4. fADD Delay: " + SimManager.ADDDelay + " cycles" +
                                                  "\n5. fMul Units: " + SimManager.MULUnits +
                                                  "\n6. fMUL Delay: " + SimManager.MULDelay + " cycles" +
                                                  "\n7. fDiv Units: " + SimManager.DIVUnits +
                                                  "\n8. fDIV Delay: " + SimManager.DIVDelay + " cycles" +
                                                  "\n\n WANING: Changing ANY setting will reset the emulator." +
                                                  "\nPress the number setting to change or Q to return to the main menu.");
                                switch (Console.ReadKey(false).Key)
                                {
                                    case ConsoleKey.D1:
                                        Console.Clear();
                                        Console.WriteLine("Enter the new memory size in bytes (Default: 32768): ");
                                        text = Console.ReadLine();
                                        if (text != null && text.Contains("0x"))
                                        {
                                            text = text.Replace("0x", "");
                                            textnumber = Convert.ToInt32(text, 16);
                                        }
                                        else if (text != null)
                                        {
                                            textnumber = Convert.ToInt32(text);
                                        }
                                        else
                                        {
                                            Console.Clear();
                                            Console.WriteLine("Invalid input.\n" +
                                                              "Returning to settings menu.");
                                            break;
                                        }
                                        if (textnumber is < 1024 or > 65536)
                                        {
                                            Console.Clear();
                                            Console.WriteLine("Enter a number within this range (1024 - 65536).\n" +
                                                              "Returning to settings menu.");
                                            break;
                                        }
                                        else
                                        {
                                            SimManager.ResizeMemory((uint)textnumber);
                                            settingChanged = true;
                                        }
                                        break;
                                    case ConsoleKey.D2:
                                        SimManager.SetForwarding(!SimManager.Forwarding);
                                        settingChanged = true;
                                        break;
                                    case ConsoleKey.D3:
                                        Console.Clear();
                                        Console.WriteLine("Enter the amount of floating point add units: ");
                                        text = Console.ReadLine();
                                        if (text != null && text.Contains("0x"))
                                        {
                                            text = text.Replace("0x", "");
                                            textnumber = Convert.ToInt32(text, 16);
                                        }
                                        else if (text != null)
                                        {
                                            textnumber = Convert.ToInt32(text);
                                        }
                                        else
                                        {
                                            Console.Clear();
                                            Console.WriteLine("Invalid input.\n" +
                                                              "Returning to settings menu.");
                                            break;
                                        }
                                        if (textnumber is < 1 or > 256)
                                        {
                                            Console.Clear();
                                            Console.WriteLine("Enter a number within this range (1 - 255).\n" +
                                                              "Returning to settings menu.");
                                            break;
                                        }
                                        else
                                        {
                                            SimManager.ADDUnits = (byte)textnumber;
                                            settingChanged = true;
                                        }
                                        break;
                                    case ConsoleKey.D4:
                                        Console.Clear();
                                        Console.WriteLine("Enter the delay of floating point add units in cycles: ");
                                        text = Console.ReadLine();
                                        if (text != null && text.Contains("0x"))
                                        {
                                            text = text.Replace("0x", "");
                                            textnumber = Convert.ToInt32(text, 16);
                                        }
                                        else if (text != null)
                                        {
                                            textnumber = Convert.ToInt32(text);
                                        }
                                        else
                                        {
                                            Console.Clear();
                                            Console.WriteLine("Invalid input.\n" +
                                                              "Returning to settings menu.");
                                            break;
                                        }
                                        if (textnumber is < 1 or > 256)
                                        {
                                            Console.Clear();
                                            Console.WriteLine("Enter a number within this range (1 - 255).\n" +
                                                              "Returning to settings menu.");
                                            break;
                                        }
                                        else
                                        {
                                            SimManager.ADDDelay = (byte)textnumber;
                                            settingChanged = true;
                                        }
                                        break;
                                    case ConsoleKey.D5:
                                        Console.Clear();
                                        Console.WriteLine("Enter the amount of floating point multiply units: ");
                                        text = Console.ReadLine();
                                        if (text != null && text.Contains("0x"))
                                        {
                                            text = text.Replace("0x", "");
                                            textnumber = Convert.ToInt32(text, 16);
                                        }
                                        else if (text != null)
                                        {
                                            textnumber = Convert.ToInt32(text);
                                        }
                                        else
                                        {
                                            Console.Clear();
                                            Console.WriteLine("Invalid input.\n" +
                                                              "Returning to settings menu.");
                                            break;
                                        }
                                        if (textnumber is < 1 or > 256)
                                        {
                                            Console.Clear();
                                            Console.WriteLine("Enter a number within this range (1 - 255).\n" +
                                                              "Returning to settings menu.");
                                            break;
                                        }
                                        else
                                        {
                                            SimManager.MULUnits = (byte)textnumber;
                                            settingChanged = true;
                                        }
                                        break;
                                    case ConsoleKey.D6:
                                        Console.Clear();
                                        Console.WriteLine("Enter the delay of floating point multiply units in cycles: ");
                                        text = Console.ReadLine();
                                        if (text != null && text.Contains("0x"))
                                        {
                                            text = text.Replace("0x", "");
                                            textnumber = Convert.ToInt32(text, 16);
                                        }
                                        else if (text != null)
                                        {
                                            textnumber = Convert.ToInt32(text);
                                        }
                                        else
                                        {
                                            Console.Clear();
                                            Console.WriteLine("Invalid input.\n" +
                                                              "Returning to settings menu.");
                                            break;
                                        }
                                        if (textnumber is < 1 or > 256)
                                        {
                                            Console.Clear();
                                            Console.WriteLine("Enter a number within this range (1 - 255).\n" +
                                                              "Returning to settings menu.");
                                            break;
                                        }
                                        else
                                        {
                                            SimManager.MULDelay = (byte)textnumber;
                                            settingChanged = true;
                                        }
                                        break;
                                    case ConsoleKey.D7:
                                        Console.Clear();
                                        Console.WriteLine("Enter the amount of floating point divide units: ");
                                        text = Console.ReadLine();
                                        if (text != null && text.Contains("0x"))
                                        {
                                            text = text.Replace("0x", "");
                                            textnumber = Convert.ToInt32(text, 16);
                                        }
                                        else if (text != null)
                                        {
                                            textnumber = Convert.ToInt32(text);
                                        }
                                        else
                                        {
                                            Console.Clear();
                                            Console.WriteLine("Invalid input.\n" +
                                                              "Returning to settings menu.");
                                            break;
                                        }
                                        if (textnumber is < 1 or > 256)
                                        {
                                            Console.Clear();
                                            Console.WriteLine("Enter a number within this range (1 - 255).\n" +
                                                              "Returning to settings menu.");
                                            break;
                                        }
                                        else
                                        {
                                            SimManager.DIVUnits = (byte)textnumber;
                                            settingChanged = true;
                                        }
                                        break;
                                    case ConsoleKey.D8:
                                        Console.Clear();
                                        Console.WriteLine("Enter the delay of floating point divide units in cycles: ");
                                        text = Console.ReadLine();
                                        if (text != null && text.Contains("0x"))
                                        {
                                            text = text.Replace("0x", "");
                                            textnumber = Convert.ToInt32(text, 16);
                                        }
                                        else if (text != null)
                                        {
                                            textnumber = Convert.ToInt32(text);
                                        }
                                        else
                                        {
                                            Console.Clear();
                                            Console.WriteLine("Invalid input.\n" +
                                                              "Returning to settings menu.");
                                            break;
                                        }
                                        if (textnumber is < 1 or > 256)
                                        {
                                            Console.Clear();
                                            Console.WriteLine("Enter a number within this range (1 - 255).\n" +
                                                              "Returning to settings menu.");
                                            break;
                                        }
                                        else
                                        {
                                            SimManager.DIVDelay = (byte)textnumber;
                                            settingChanged = true;
                                        }
                                        break;
                                    case ConsoleKey.Q:
                                    case ConsoleKey.Spacebar:
                                        exitSettings = true;
                                        break;
                                        
                                }
                                Console.Clear();
                                if (settingChanged)
                                {
                                    SimManager.Restart();
                                    Console.WriteLine("Settings chaged successfully.\n" +
                                                      "CPU has been reseted.\n");
                                }
                            }
                            break;
                        default:
                            Console.Clear();
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