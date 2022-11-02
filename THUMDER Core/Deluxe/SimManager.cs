using THUMDER.Interpreter;
using System.Collections.Specialized;

namespace THUMDER.Deluxe
{
    internal sealed partial class SimManager
    {
        /// <summary>
        /// Stalls stats counters.
        /// </summary>
        public uint decodedInstructions { get; private set; }
        public uint LDStalls { get; private set; }
        public uint JumpStalls { get; private set; }
        public uint fpStalls { get; private set; }
        public uint WAWStalls { get; private set; }
        public uint StructuralStalls { get; private set; }
        public uint ControlStalls { get; private set; }
        public uint JumpsTaken { get; private set; }
        public uint JumpsNotTaken { get; private set; }
        public uint MemLoads { get; private set; }
        public uint MemStores { get; private set; }
        public uint fpAddCount { get; private set; }
        public uint fpMulCount { get; private set; }
        public uint fpDivCount { get; private set; }

        /// <summary>
        /// Dictionary that holds what memory address is represented by each label.
        /// </summary>
        private static readonly Dictionary<string, uint> labels = new Dictionary<string, uint>();

        /// <summary>
        /// Dictionary that holds breakpoints.
        /// </summary>
        private static readonly Dictionary<int, int> Breakpoints = new Dictionary<int, int>();

        private SimManager()
        {
            this.alus = new List<ALU>();
            this.adds = new List<FPU>();
            this.muls = new List<FPU>();
            this.divs = new List<FPU>();
            
            this.PedingMemAccess = new List<MemAccess?>();
            PedingMemAccess.Add(null);

            for (int i = 0; i < ADDUnits; i++)
                this.adds.Add(new FPU());
            for (int i = 0; i < MULUnits; i++)
                this.muls.Add(new FPU());
            for (int i = 0; i < DIVUnits; i++)
                this.divs.Add(new FPU());
            for (int i = 0; i < ALUunits; i++)
                this.alus.Add(new ALU());
            this.Registers = new BitVector32[32];
            this.fRegisters = new BitVector32[32];
            LMD = zeroBitsDouble;
            ALUout = zeroBitsDouble;
        }

        public static void RunFullSimulation()
        {
            while (!Instance.stop && Instance.PC <= Memsize)
            {
                Instance.DoCycle();
            }
        }

        public static void RunACycle()
        {
            if (!Instance.stop && Instance.PC <= Memsize)
                Instance.DoCycle();
        }
        public static void RunUntilBreakpoint()
        {
            if (!Instance.stop && Instance.PC <= Memsize)
            {
                do
                {
                    Instance.DoCycle();
                } while (!Breakpoints.ContainsKey((int)Instance.PC));
            }
        }

        internal static void SetBreakpoint(int address)
        {
            Breakpoints.Add(address, 1);
        }

        internal static void RemoveBreakpoint(int address)
        {
            Breakpoints.Remove(address);
        }

        internal static string PrintRegisters()
        {
            string registersText = String.Empty;
            List<byte> bytes = new List<byte>();
            double[] ds = new double[16];
            int j = 0;
            for (int i = 0; i < 32; i+=2)
            {
                bytes.AddRange(BitConverter.GetBytes(Instance.fRegisters[i].Data));
                bytes.AddRange(BitConverter.GetBytes(Instance.fRegisters[i+1].Data));
                ds[j++] = BitConverter.ToDouble(bytes.ToArray(), 0);
                bytes.Clear();
            }
            registersText += String.Concat("PC=    0x" + Instance.PC.ToString("X8")                 + "     F0 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[0].Data).ToString("E4")  + "    D0 = " + ds[0].ToString("E8") + "\n");
            registersText += String.Concat("IMAR=  0x" + Instance.IMAR.Data.ToString("X8")          + "     F1 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[1].Data).ToString("E4")  + "\n");
            registersText += String.Concat("IR=    0x" + Instance.IDreg.Data.ToString("X8")         + "     F2 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[2].Data).ToString("E4")  + "    D2 = " + ds[1].ToString("E8") + "\n");
            registersText += String.Concat("A=     0x" + Instance.A[0].Data.ToString("X8")          + "     F3 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[3].Data).ToString("E4")  + "\n");
            registersText += String.Concat("AHI=   0x" + Instance.A[1].Data.ToString("X8")          + "     F4 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[4].Data).ToString("E4")  + "    D4 = " + ds[2].ToString("E8") + "\n");
            registersText += String.Concat("B=     0x" + Instance.B[0].Data.ToString("X8")          + "     F5 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[5].Data).ToString("E4")  + "\n");
            registersText += String.Concat("BHI=   0x" + Instance.B[1].Data.ToString("X8")          + "     F6 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[6].Data).ToString("E4")  + "    D6 = " + ds[3].ToString("E8") + "\n");
            registersText += String.Concat("BTA=   0x" + zeroBits.Data.ToString("X8")               + "     F7 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[7].Data).ToString("E4")  + "\n");
            registersText += String.Concat("ALU=   0x" + Instance.ALUout[0].Data.ToString("X8")     + "     F8 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[8].Data).ToString("E4")  + "    D8 = " + ds[4].ToString("E8") + "\n");
            registersText += String.Concat("ALUHI= 0x" + Instance.ALUout[1].Data.ToString("X8")     + "     F9 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[9].Data).ToString("E4")  + "\n");
            registersText += String.Concat("FPSR=  0x" + Instance.FPstatus.Data.ToString("X8")      + "     F10= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[10].Data).ToString("E4") + "    D10= " + ds[5].ToString("E8") + "\n");
            registersText += String.Concat("DMAR=  0x" + Instance.DMAR.Data.ToString("X8")          + "     F11= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[11].Data).ToString("E4") + "\n");
            registersText += String.Concat("SDR=   0x" + Instance.SDR[0].Data.ToString("X8")        + "     F12= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[12].Data).ToString("E4") + "    D12= " + ds[6].ToString("E8") + "\n");
            registersText += String.Concat("SDRHI= 0x" + Instance.SDR[1].Data.ToString("X8")        + "     F13= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[13].Data).ToString("E4") + "\n");
            registersText += String.Concat("LDR=   0x" + Instance.LMD[0].Data.ToString("X8")        + "     F14= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[14].Data).ToString("E4") + "    D14= " + ds[7].ToString("E8") + "\n");
            registersText += String.Concat("LDRHI= 0x" + Instance.LMD[1].Data.ToString("X8")        + "     F15= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[15].Data).ToString("E4") + "\n");

            registersText += String.Concat("R0=    0x" + Instance.Registers[0].Data.ToString("X8")  + "     F16= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[16].Data).ToString("E4") + "    D16= " + ds[8].ToString("E8") + "\n");
            registersText += String.Concat("R1=    0x" + Instance.Registers[1].Data.ToString("X8")  + "     F17= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[17].Data).ToString("E4") + "\n");
            registersText += String.Concat("R2=    0x" + Instance.Registers[2].Data.ToString("X8")  + "     F18= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[18].Data).ToString("E4") + "    D18= " + ds[9].ToString("E8") + "\n");
            registersText += String.Concat("R3=    0x" + Instance.Registers[3].Data.ToString("X8")  + "     F19= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[19].Data).ToString("E4") + "\n");
            registersText += String.Concat("R4=    0x" + Instance.Registers[4].Data.ToString("X8")  + "     F20= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[20].Data).ToString("E4") + "    D20= " + ds[10].ToString("E8") + "\n");
            registersText += String.Concat("R5=    0x" + Instance.Registers[5].Data.ToString("X8")  + "     F21= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[21].Data).ToString("E4") + "\n");
            registersText += String.Concat("R6=    0x" + Instance.Registers[6].Data.ToString("X8")  + "     F22= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[22].Data).ToString("E4") + "    D22= " + ds[11].ToString("E8") + "\n");
            registersText += String.Concat("R7=    0x" + Instance.Registers[7].Data.ToString("X8")  + "     F23= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[23].Data).ToString("E4") + "\n");
            registersText += String.Concat("R8=    0x" + Instance.Registers[8].Data.ToString("X8")  + "     F24= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[16].Data).ToString("E4") + "    D24= " + ds[12].ToString("E8") + "\n");
            registersText += String.Concat("R9=    0x" + Instance.Registers[9].Data.ToString("X8")  + "     F25= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[17].Data).ToString("E4") + "\n");
            registersText += String.Concat("R10=   0x" + Instance.Registers[10].Data.ToString("X8") + "     F26= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[18].Data).ToString("E4") + "    D26= " + ds[13].ToString("E8") + "\n");
            registersText += String.Concat("R11=   0x" + Instance.Registers[11].Data.ToString("X8") + "     F27= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[19].Data).ToString("E4") + "\n");
            registersText += String.Concat("R12=   0x" + Instance.Registers[12].Data.ToString("X8") + "     F28= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[20].Data).ToString("E4") + "    D28= " + ds[14].ToString("E8") + "\n");
            registersText += String.Concat("R13=   0x" + Instance.Registers[13].Data.ToString("X8") + "     F29= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[21].Data).ToString("E4") + "\n");
            registersText += String.Concat("R14=   0x" + Instance.Registers[14].Data.ToString("X8") + "     F30= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[22].Data).ToString("E4") + "    D30= " + ds[15].ToString("E8") + "\n");
            registersText += String.Concat("R15=   0x" + Instance.Registers[15].Data.ToString("X8") + "     F31= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[23].Data).ToString("E4") + "\n");
            for (int i = 16; i < 32; i++)
            {
                registersText += String.Concat("R" + (i).ToString() + "=   0x" + Instance.Registers[i].Data.ToString("X8") + "\n");
            }
            return registersText;
        }

        internal static string MemoryExplorer(int address)
        {
            string output = String.Empty;
            for(uint i = (uint)(address + 4); i >= address - 4 && i <= Memsize; i--)
            {
                output += String.Concat(String.Format("{0}    {1}\n", "0x" + i.ToString("X8").ToUpper(), MemoryManager.Instance.ReadByte(i).ToString("X2").ToUpper()));
            }
            return output;
        }

        internal static string PrintStats()
        {
            uint instructionsPipeline = 0;
            if (Instance.IFreg.Data != zeroBits.Data)
                instructionsPipeline++;
            if (Instance.IDreg.Data != zeroBits.Data)
                instructionsPipeline++;
            if (Instance.OPreg.Data != zeroBits.Data)
                instructionsPipeline++;
            if (Instance.MEMreg.Data != zeroBits.Data)
                instructionsPipeline++;
            if (Instance.WBreg.Data != zeroBits.Data)
                instructionsPipeline++;

            string output = String.Empty;

            output += String.Concat("Total: \n   ");
            output += String.Concat(Instance.Cycles + " Cycles executed \n   ");
            output += String.Concat("ID Executed by: " + Instance.decodedInstructions + " Instructions \n   ");
            output += String.Concat(instructionsPipeline + " Instructions currently in the pipeline \n   ");

            output += String.Concat("\nHardware configuration: \n   ");
            output += String.Concat("Memory size: " + Memsize + " Bytes \n   ");
            output += String.Concat("faddEX-Stages: " + ADDUnits + ", required Cycles: " + ADDDelay + "\n   ");
            output += String.Concat("fmulEX-Stages: " + MULUnits + ", required Cycles: " + MULDelay + "\n   ");
            output += String.Concat("fdivEX-Stages: " + DIVUnits + ", required Cycles: " + DIVDelay + "\n   ");
            output += String.Concat("Forwarding enabled: " + Forwarding + "\n");

            output += String.Concat("\n Stalls: \n   ");
            output += String.Concat("RAW stalls: " + (Instance.LDStalls + Instance.JumpStalls + Instance.fpStalls).ToString() + "\n      ");
            output += String.Concat("LD stalls: " + Instance.LDStalls + "\n      ");
            output += String.Concat("Branch/Jump stalls: " + Instance.JumpStalls + "\n      ");
            output += String.Concat("Floating Point stalls: " + Instance.fpStalls + "\n   ");
            output += String.Concat("WAW stalls: " + Instance.WAWStalls + "\n   ");
            output += String.Concat("Structural stalls: " + Instance.StructuralStalls + "\n   ");
            output += String.Concat("Control stalls: " + Instance.ControlStalls + "\n   ");
            output += String.Concat("Total stalls: " + (Instance.LDStalls + Instance.JumpStalls + Instance.fpStalls + Instance.WAWStalls + Instance.StructuralStalls + Instance.ControlStalls).ToString() + "\n");

            output += String.Concat("\nConditional Branches: \n   ");
            output += String.Concat("Total: " + (Instance.JumpsTaken + Instance.JumpsNotTaken).ToString() + "\n      ");
            output += String.Concat("Taken: " + Instance.JumpsTaken + "\n      ");
            output += String.Concat("Not taken: " + Instance.JumpsNotTaken + "\n");

            output += String.Concat("\nLoad/Store Instructions: \n   ");
            output += String.Concat("Total: " + (Instance.MemLoads + Instance.MemStores).ToString() + "\n      ");
            output += String.Concat("Loads: " + Instance.MemLoads + "\n      ");
            output += String.Concat("Stores: " + Instance.MemStores + "\n");

            output += String.Concat("\nFloating Point Stage Instructions: \n   ");
            output += String.Concat("Total: " + (Instance.fpAddCount + Instance.fpMulCount + Instance.fpDivCount).ToString() + "\n      ");
            output += String.Concat("Additions: " + Instance.fpAddCount + "\n      ");
            output += String.Concat("Multiplications: " + Instance.fpMulCount + "\n      ");
            output += String.Concat("Divisions: " + Instance.fpDivCount + "\n");

            return output;
        }

        internal static string PrintPipeline()
        {
            string output = String.Empty;
            output += String.Concat("Pipeline stage     Instruction \n" +
                                    "--------------------------------------\n");
            output += String.Concat("WB:                 0x" + Instance.WBreg.Data.ToString("X8")  + "\n");
            output += String.Concat("MEM:                0x" + Instance.MEMreg.Data.ToString("X8") + "\n");
            if (Instance.RStall)
            {
                output += String.Concat("EX:    (stalled)    0x" + Instance.EXreg.Data.ToString("X8") + "\n");
            }
            else
            {
                output += String.Concat("EX:                 0x" + Instance.EXreg.Data.ToString("X8") + "\n");
            }
            if (Instance.DStall)
            {
                output += String.Concat("ID:    (stalled)    0x" + Instance.IDreg.Data.ToString("X8") + "\n");
            }
            else
            {
                output += String.Concat("ID:                 0x" + Instance.IDreg.Data.ToString("X8") + "\n");
            }
            output += String.Concat("IF:                 0x" + Instance.IFreg.Data.ToString("X8")  + "\n");
            return output;
        }

        private void DoCycle()
        {
            this.WB();            //We write to registers in the first half of the cycle.
            this.MEM();           //Access the memory if needed.
            this.EX();            //Apply a cycle to all execution units and place results, if available on the output register.
            this.ID();            //We read from registers in the second half to avoid data issues.
            this.IF();            //Memory access is the first step. but won't happen if there are peding operations to memory WHICH SHOULD ONLY HAPPEN IF THERE IS A INSTRUCTION REQUESTING IT.
            ++Cycles;
        }

        /// <summary>
        /// Resizes vRAM
        /// </summary>
        /// <param name="newSize">new memory size</param>
        public static void ResizeMemory(in uint newSize)
        {
            Memsize = newSize;
            MemoryManager.Instance.ResizeMemory(newSize);
        }

        /// <summary>
        /// Sets the data forwarding status.
        /// </summary>
        /// <param name="newforwarding">new status for data forwarding</param>
        public static void SetForwarding(in bool newforwarding)
        {
            Forwarding = newforwarding;
        }
        
        /// <summary>
        /// Resets and run the simulation.
        /// </summary>
        public static void Restart()
        {
            Instance.trap0Found = false;
            Instance.stop = false;
            Instance.ClearPipeline();
            Instance.PC = Instance.startingPC;
            Instance.Cycles = 0;
            Instance.LDStalls = 0;
            Instance.JumpStalls = 0;
            Instance.fpStalls = 0;
            Instance.WAWStalls = 0;
            Instance.StructuralStalls = 0;
            Instance.ControlStalls = 0;
            Instance.JumpsTaken = 0;
            Instance.JumpsNotTaken = 0;
            Instance.MemLoads = 0;
            Instance.MemStores = 0;
            Instance.fpAddCount = 0;
            Instance.fpMulCount = 0;
            Instance.fpDivCount = 0;
        }

        /// <summary>
        /// Clears the pipeline and reloads the last file. 
        /// </summary>
        public static void Reset()
        {
            ASM lastProgram = Instance.loadedProgram;
            Instance = new SimManager();
            labels.Clear();
            LoadProgram(lastProgram);
        }

        struct MemAccess
        {
            public int? Destination { get; private set; }
            public uint Address { get; private set; }
            public byte[]? Content { get; private set; }
            public MemAccessTypes Type { get; private set; }
            public bool isWrite { get; private set; }

            public MemAccess(uint address, byte[] content, MemAccessTypes type)
            {
                Destination = null;
                Address = address;
                Content = content;
                Type = type;
                isWrite = true;
            }
            public MemAccess(int destination, uint address, MemAccessTypes type)
            {
                Destination = destination;
                Address = address;
                Content = null;
                Type = type;
                isWrite = false;
            }
        }
        internal struct MemAccessTypes
        {
            public static readonly MemAccessTypes BYTE = new("BYTE");
            public static readonly MemAccessTypes UBYTE = new("UBYTE");
            public static readonly MemAccessTypes WORD = new("WORD");
            public static readonly MemAccessTypes UWORD = new("UWORD");
            public static readonly MemAccessTypes FLOAT = new("FLOAT");
            public static readonly MemAccessTypes DOUBLE = new("DOUBLE");
            public static readonly MemAccessTypes HALF = new("HALF");
            public static readonly MemAccessTypes UHALF = new("UHALF");

            public string Value { get; private set; }
            private MemAccessTypes (string value)
            {
                Value = value;
            }
        }
    }
}
