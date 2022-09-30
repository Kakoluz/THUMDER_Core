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
        private BitVector32 IFreg, IDreg, EXreg, MEMreg, WBreg;

        /// <summary>
        /// Special register to keep the output from the ALU.
        /// </summary>
        private BitVector32 ALUout;

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
        private bool RStall;

        /// <summary>
        /// Controls the stopping of the emulation.
        /// </summary>
        private bool trap0Found = false;

        /// <summary>
        /// Dictionary that holds what memory address is represented by each label.
        /// </summary>
        private static readonly Dictionary<string, uint> labels = new Dictionary<string, uint>();

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
            while (!Instance.trap0Found || Instance.PC > Memsize)
            {
                Instance.DoCycle();
            }
        }

        public static void RunACycle()
        {
            Instance.DoCycle();
        }

        private void DoCycle()
        {
            WBreg = MEMreg;
            if (!RStall)   //If the instruction wasn't loaded into memory stall the CPU.
            {
                MEMreg = EXreg;
                EXreg = IDreg;
                IDreg = IFreg;      //Move instructions to its stage register.
            }
            else
                MEMreg = zeroBits;
            //this.TickAllUnits();  //If we don't tick the units here, it won't be a segmented processor.
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
