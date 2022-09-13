using THUMDER.Interpreter;
using System.Collections.Specialized;

namespace THUMDER.Deluxe
{
    internal sealed class SimManager
    {

        //Set C# auto properties for emulation settings and initialize to WinDLX default values
        /// <summary>
        /// Data forwarding status.
        /// </summary>
        public static bool Forwarding { get; private set; } = true;

        /// <summary>
        /// Size of the memory.
        /// </summary>
        public static uint Memsize { get; private set; } = 32768;

        /// <summary>
        /// Number of Arithmetic Logical Units.
        /// </summary>
        public static int ALUunits { get; private set; } = 1;

        /// <summary>
        /// Number of summator units for floating point.
        /// </summary>
        public static int ADDUnits { get; private set; } = 1;
        /// <summary>
        /// Number of multiplier units for floating point.
        /// </summary>
        public static int MULUnits { get; private set; } = 1;
        /// <summary>
        /// Number of divisor units for floating point.
        /// </summary>
        public static int DIVUnits { get; private set; } = 1;
        
        /// <summary>
        /// Number of cicles for a floating point add operation.
        /// </summary>
        public static int ADDDelay  { get; private set; } = 2;
        /// <summary>
        /// Number of cicles for a floating point multiplication operation.
        /// </summary>
        public static int MULDDelay { get; private set; } = 5;
        /// <summary>
        /// Number of cicles for a floatig point division operation.
        /// </summary>
        public static int DIVDelay { get; private set; } = 19;

        /// <summary>
        /// Singleton internal instance.
        /// </summary>
        private static SimManager? instance = null;
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static SimManager Instance
        {
            private set => instance = value;
            get
            {   //Might need to add locks or lazy implementation for thread safety.
                if (instance == null)
                    instance = new SimManager();
                return instance;
            }
        }

        /// <summary>
        /// Program Counter
        /// </summary>
        private uint PC;

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
        private int[] Registers;

        /// <summary>
        /// Floating point 32 bits registers.
        /// </summary>
        private float[] fRegisters;

        /// <summary>
        /// Special Register to store data fetched.
        /// </summary>
        private BitVector32 IDRegister;

        /// <summary>
        /// Current Instruction in human legible format.
        /// </summary>
        private Assembler.Instruction currentInstruction;

        /// <summary>
        /// The number of cycles runned in the curtent emulation.
        /// </summary>
        public int Cycles { get; private set; }

        /// <summary>
        /// Lists of pending writebacks.
        /// </summary>
        private List<KeyValuePair<int, BitVector32>> PedingWriteBacks = new List<KeyValuePair<int, BitVector32>>();

        /// <summary>
        /// Instruction name extracted from memory.
        /// </summary>
        private string? IDInstruction = null;
        /// <summary>
        /// Instruction arguments.
        /// </summary>
        private int? IDOpcode, rd, rs2, rs1, funct, shamt, address = null;

        /// <summary>
        /// Stages of execution where the CPU might need to wait.
        /// </summary>
        private bool IDhold, MEMHold, WBHold, ForwadingHold;
        private SimManager()
        {
            this.alus = new List<ALU>();
            this.adds = new List<FPU>();
            this.muls = new List<FPU>();
            this.divs = new List<FPU>();

            for (int i = 0; i < ADDUnits; i++)
                this.adds.Add(new FPU());
            for (int i = 0; i < MULUnits; i++)
                this.muls.Add(new FPU());
            for (int i = 0; i < DIVUnits; i++)
                this.divs.Add(new FPU());
            for (int i = 0; i < ALUunits; i++)
                this.alus.Add(new ALU());
                this.PC = 0;
            this.Registers = new int[32];
            this.fRegisters = new float[32];
        }

        /// <summary>
        /// Resizes vRAM
        /// </summary>
        /// <param name="newSize">new memory size</param>
        public static void ResizeMemory(in uint newSize)
        {
            Memsize = newSize;
            Memory.ResizeMemory(newSize);
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
        public void Restart()
        {
            
        }

        /// <summary>
        /// Clears the pipeline and reloads the last file. 
        /// </summary>
        public void Reset()
        {
            
        }
        
        /// <summary>
        /// Reads the PC address and fetches de data on that memory address.
        /// </summary>
        private void IF()
        {
            IDRegister = Memory.Read(PC);
            ++PC;
        }

        /// <summary>
        /// Decodes the instruction to execute.
        /// </summary>
        private void ID()
        {
            //R-Type Instruction
            BitVector32.Section funct = BitVector32.CreateSection(64);
            BitVector32.Section shamt = BitVector32.CreateSection(32, funct);
            BitVector32.Section rd    = BitVector32.CreateSection(32, shamt);
            BitVector32.Section rs2   = BitVector32.CreateSection(32, rd);
            BitVector32.Section rs1   = BitVector32.CreateSection(32, rs2);
            BitVector32.Section op    = BitVector32.CreateSection(32, rs1);
            //I-Type Instruction
            BitVector32.Section address = BitVector32.CreateSection(short.MaxValue);
            // For J-Types we need to create a new bitvector32 and put the op bits to 0 and convert to int32
            int i;
            for (i = 0; i < Assembler.OpCodes.Length; i++)
            {
                //Check for R-type ALU operation function
                if (IDRegister[op] != 0)
                {
                    if (new BitVector32((int)Assembler.OpCodes[i].Opcode)[funct] == IDRegister[funct])
                    {
                        this.funct         = IDRegister[funct];
                        this.shamt         = IDRegister[shamt];
                        this.rd            = IDRegister[rd];
                        this.rs2           = IDRegister[rs2];
                        this.rs1           = IDRegister[rs1];
                        this.IDOpcode      = IDRegister[op];
                        this.address       = null;
                        this.IDInstruction = Assembler.OpCodes[i].Name;
                        this.currentInstruction = Assembler.OpCodes[i]; //Might be needed
                        bool assigned = false;
                        if (this.funct > 0x11)
                        foreach (var alu in alus)
                        {
                           if(!alu.busy)
                           {
                                alu.LoadValues((int)this.rs1, (int)this.rs2, (short)this.funct);
                                assigned = true;
                                break;
                           }                               
                        }
                        IDhold = assigned ? true : false;
                    }
                }
                else //Check for any other instrction
                {
                    if (new BitVector32((int)Assembler.OpCodes[i].Opcode)[op] == IDRegister[op])
                    {
                        this.IDOpcode      = IDRegister[op];
                        this.rd            = IDRegister[rs2]; //use the RS2 mask that holds the values in bits 25:21
                        this.rs1           = IDRegister[rs1];
                        this.address       = IDRegister[address];
                        this.funct         = null;
                        this.shamt         = null;
                        this.rs2           = null;
                        this.IDInstruction = Assembler.OpCodes[i].Name;
                        this.currentInstruction = Assembler.OpCodes[i]; //Might be needed
                    }
                }
            }
        }

        private void EX()
        {
            
        }

        private void MEM()
        {
            
        }

        private void WB()
        {
            var wb = PedingWriteBacks[0];
            PedingWriteBacks.RemoveAt(0);
            Memory.Write((uint) wb.Key, wb.Value);
        }
    }
}
