using THUMDER.Interpreter;
using System.Collections.Specialized;
using System.Text;

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
        private BitVector32[] Registers;

        /// <summary>
        /// Floating point 32 bits registers.
        /// </summary>
        private BitVector32[] fRegisters;

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
        private List<KeyValuePair<int, byte[]>> PedingMemWrites = new List<KeyValuePair<int, byte[]>>();

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
            this.Registers = new BitVector32[32];
            this.fRegisters = new BitVector32[32];
        }

        public static void LoadProgram(ASM assembly)
        {
            int dataLength = assembly.dataAddress;
            for (int i = 0; i < assembly.DataSegment.Count; i++)
            {
                string[] aux = assembly.DataSegment[i].Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                switch (aux[0])
                {
                    case ".word":
                        for (int j = 1; j < aux.Length; j++)
                        {
                            MemoryManager.Instance.WriteWord((uint)(assembly.dataAddress + dataLength), int.Parse(aux[j]));
                            dataLength += 4;
                        }
                        break;
                    case ".ascii":
                        for (int j = 1; j < aux.Length; j++)
                        {
                            byte[] bytes = Encoding.ASCII.GetBytes(aux[j]);
                            foreach (byte b in bytes)
                                MemoryManager.Instance.WriteByte((uint)(assembly.dataAddress + dataLength++), b);
                        }
                        break;
                    case ".asciiz":
                        for (int j = 1; j < aux.Length; j++)
                        {
                            byte[] bytes = Encoding.ASCII.GetBytes(aux[j]);
                            foreach (byte b in bytes)
                                MemoryManager.Instance.WriteByte((uint)(assembly.dataAddress + dataLength++), b);
                            MemoryManager.Instance.WriteByte((uint)(assembly.dataAddress + dataLength++), 0);   //0 byte for the z in asciiz
                        }
                        break;
                    case ".byte":
                        for (int j = 1; j < aux.Length; j++)
                        {
                            MemoryManager.Instance.WriteByte((uint)(assembly.dataAddress + dataLength++), byte.Parse(aux[1]));
                        }
                        break;
                    case ".float":
                        for (int j = 1; j < aux.Length; j++)
                        {
                            MemoryManager.Instance.WriteFloat((uint)(assembly.dataAddress + dataLength), float.Parse(aux[1]));
                            dataLength += 4;
                        }
                        break;
                    case ".double":
                        for (int j = 1; j < aux.Length; j++)
                        {
                            MemoryManager.Instance.WriteDouble((uint)(assembly.dataAddress + dataLength), double.Parse(aux[1]));
                            dataLength += 8;
                        }
                        break;
                }
            }
            if (assembly.dataAddress + dataLength > assembly.textAddress)
                throw new AccessViolationException("Data segment overwrites code segment.");
            //TODO assemble instructions.
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
            IDRegister = MemoryManager.Instance.ReadWordAsBitVector(PC);
            PC += 4;
        }

        /// <summary>
        /// Decodes the instruction to execute.
        /// </summary>
        private void ID()
        {
            //R-Type Instruction format.
            BitVector32.Section funct = BitVector32.CreateSection(64);
            BitVector32.Section shamt = BitVector32.CreateSection(32, funct);
            BitVector32.Section rd    = BitVector32.CreateSection(32, shamt);
            BitVector32.Section rs2   = BitVector32.CreateSection(32, rd);
            BitVector32.Section rs1   = BitVector32.CreateSection(32, rs2);
            BitVector32.Section op    = BitVector32.CreateSection(32, rs1);
            //I-Type Instruction format.
            BitVector32.Section address = BitVector32.CreateSection(short.MaxValue);
            // For J-Types we need to create a new bitvector32 and put the op bits to 0 and convert to int32
            
            
        }

        private void EX()
        {
            
        }

        private void MEM()
        {
            var wb = PedingMemWrites[0];
            PedingMemWrites.RemoveAt(0);
            uint address = (uint)wb.Key;
            foreach (byte b in wb.Value)
            {
                MemoryManager.Instance.WriteByte(address++, b);
            }
        }

        private void WB()
        {

        }
    }
}
