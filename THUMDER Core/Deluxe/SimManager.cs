using THUMDER.Interpreter;
using System.Collections.Specialized;
using System.Text;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Reflection.Metadata.Ecma335;

namespace THUMDER.Deluxe
{
    internal sealed partial class SimManager
    {
        /// <summary>
        /// Program Counter
        /// </summary>
        private uint PC, startingPC;

        /// <summary>
        /// Assembly currently loaded in memory.
        /// </summary>
        private ASM loadedProgram;

        /// <summary>
        /// List of Arithmetic Logical Units.
        /// </summary>
        private List<ALU> alus;
        
        /// <summary>
        /// List of Floating Point add units.
        /// </summary>
        private List<FPU> adds;

        /// <summary>
        /// List of Floating Point multiply units.
        /// </summary>
        private List<FPU> muls;

        /// <summary>
        /// List of Floating Point divisor units.
        /// </summary>
        private List<FPU> divs;

        /// <summary>
        /// General Purpose 32 bits registers.
        /// </summary>
        private BitVector32[] Registers;

        /// <summary>
        /// Floating point 32 bits registers.
        /// </summary>
        private BitVector32[] fRegisters;

        /// <summary>
        /// Special register to store fetched data.
        /// </summary>
        private BitVector32 IMAR;

        /// <summary>
        /// Instruction in each execution stage.
        /// </summary>
        private BitVector32 IFreg, IDreg, EXreg, MEMreg, WBreg, WBDataReg;

        /// <summary>
        /// Special register to keep the output from the ALU.
        /// </summary>
        private BitVector32 ALUout;

        /// <summary>
        /// Register to store the instruction currentyly in execution.
        /// </summary>
        private BitVector32 OPreg;

        /// <summary>
        /// Memory output.
        /// </summary>
        private BitVector32[] LMD;

        /// <summary>
        /// Special register to keep the output from the FPU.
        /// </summary>
        private BitVector32[] FPUout; //Size of 2 to store doubles.

        /// <summary>
        /// Temporal register to store operations not going to the fp units.
        /// </summary>
        private BitVector32[] tempFpRegister; //this is a bit of a hack to avoid doing more operations in the EX units.
        
        /// <summary>
        /// Temporal register to store operations not going to the alu units.
        /// </summary>
        private BitVector32 tempOpRegister; //this is a bit of a hack to avoid doing more operations in the EX units.

        /// <summary>
        /// Floating point status register.
        /// </summary>
        private BitVector32 FPstatus;

        /// <summary>
        /// Register to store jump conditions.
        /// </summary>
        private bool Condition;
        private bool TempCondition;
        
        /// <summary>
        /// Stores if the FPU outputed a float or double.
        /// </summary>
        private bool fpuDouble = false;

        /// <summary>
        /// If PC jumped clear units on execution.
        /// </summary>
        private bool jump = false;

        /// <summary>
        /// The number of cycles runned in the curtent emulation.
        /// </summary>
        public ulong Cycles { get; private set; }

        /// <summary>
        /// Lists of pending memory accesses.
        /// </summary>
        private List<MemAccess?> PedingMemAccess = new List<MemAccess?>();

        /// <summary>
        /// A list that holds if a register is being written by an instruction.
        /// </summary>
        private byte[] UsedRegisters = new byte[32];
        
        /// <summary>
        /// A list that holds if a floatin point register is being written by an instruction.
        /// </summary>
        private byte[] UsedfRegisters = new byte[32];
        
        /// <summary>
        /// Instruction arguments.
        /// </summary>
        private int IDOpcode, rd, rs2, rs1, funct, shamt, address;
        private int tmpIDOpcode, tmprd, tmprs2, tmprs1, tmpfunct, tmpshamt, tmpaddress;

        /// <summary>
        /// Stages of execution where the CPU might need to wait.
        /// </summary>
        private bool RStall, DStall;

        /// <summary>
        /// Controls the stopping of the emulation.
        /// </summary>
        private bool trap0Found, stop = false;

        /// <summary>
        /// Dictionary that holds what memory address is represented by each label.
        /// </summary>
        private static readonly Dictionary<string, uint> labels = new Dictionary<string, uint>();

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
            FPUout = zeroBitsDouble;
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
            registersText += String.Concat("PC=    0x" + Instance.PC.ToString("X8")                 + "     F0 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[0].Data).ToString("E4") + "    D0 = " + ds[0].ToString("E8") + "\n");
            registersText += String.Concat("IMAR=  0x" + Instance.IMAR.Data.ToString("X8")          + "     F1 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[1].Data).ToString("E4") + "\n");
            registersText += String.Concat("IR=    0x" + Instance.IDreg.Data.ToString("X8")         + "     F2 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[2].Data).ToString("E4") + "    D2 = " + ds[1].ToString("E8") + "\n");
            registersText += String.Concat("A=     0x" + Instance.Afp[0].Data.ToString("X8")        + "     F3 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[3].Data).ToString("E4") + "\n");
            registersText += String.Concat("AHI=   0x" + Instance.Afp[1].Data.ToString("X8")        + "     F4 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[4].Data).ToString("E4") + "    D4 = " + ds[2].ToString("E8") + "\n");
            registersText += String.Concat("B=     0x" + Instance.Bfp[0].Data.ToString("X8")        + "     F5 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[5].Data).ToString("E4") + "\n");
            registersText += String.Concat("BHI=   0x" + Instance.Bfp[1].Data.ToString("X8")        + "     F6 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[6].Data).ToString("E4") + "    D6 = " + ds[3].ToString("E8") + "\n");
            registersText += String.Concat("BTA=   0x" + zeroBits.Data.ToString("X8")               + "     F7 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[7].Data).ToString("E4") + "\n");
            registersText += String.Concat("ALU=   0x" + Instance.ALUout.Data.ToString("X8")        + "     F8 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[8].Data).ToString("E4") + "    D8 = " + ds[4].ToString("E8") + "\n");
            registersText += String.Concat("ALUHI= 0x" + Instance.FPUout[1].Data.ToString("X8")     + "     F9 = " + BitConverter.Int32BitsToSingle(Instance.fRegisters[9].Data).ToString("E4") + "\n");
            registersText += String.Concat("FPSR=  0x" + Instance.FPstatus.Data.ToString("X8")      + "     F10= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[10].Data).ToString("E4") + "    D10= " + ds[5].ToString("E8") + "\n");
            registersText += String.Concat("DMAR=  0x" + zeroBits.Data.ToString("X8")               + "     F11= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[11].Data).ToString("E4") + "\n");
            registersText += String.Concat("SDR=   0x" + zeroBits.Data.ToString("X8")               + "     F12= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[12].Data).ToString("E4") + "    D12= " + ds[6].ToString("E8") + "\n");
            registersText += String.Concat("SDRHI= 0x" + zeroBits.Data.ToString("X8")               + "     F13= " + BitConverter.Int32BitsToSingle(Instance.fRegisters[13].Data).ToString("E4") + "\n");
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
            throw new NotImplementedException();
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
                output += String.Concat("EX:    (stalled)     0x" + Instance.EXreg.Data.ToString("X8") + "\n");
            }
            else
            {
                output += String.Concat("EX:                0x" + Instance.EXreg.Data.ToString("X8") + "\n");
            }
            if (Instance.DStall)
            {
                output += String.Concat("ID:    (stalled)     0x" + Instance.IDreg.Data.ToString("X8") + "\n");
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
            Instance.PC = Instance.startingPC;
            Instance.Cycles = 0;
        }

        /// <summary>
        /// Clears the pipeline and reloads the last file. 
        /// </summary>
        public static void Reset()
        {
            Instance = new SimManager();
            LoadProgram(Instance.loadedProgram);
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
