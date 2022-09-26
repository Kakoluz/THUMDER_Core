using THUMDER.Interpreter;
using System.Collections.Specialized;
using System.Text;
using System.Runtime.CompilerServices;

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
        /// R-Type Instruction function field.
        /// </summary>
        static readonly BitVector32.Section functSection = BitVector32.CreateSection(64);
        /// <summary>
        /// R-Type Instruction displacement field.
        /// </summary>
        static readonly BitVector32.Section shamtSection = BitVector32.CreateSection(32, functSection);
        /// <summary>
        /// R-Type Instruction destiny register field.
        /// </summary>
        static readonly BitVector32.Section rdSection = BitVector32.CreateSection(32, shamtSection);
        /// <summary>
        /// Source 2 register field or immediate number for I-type.
        /// </summary>
        static readonly BitVector32.Section rs2Section = BitVector32.CreateSection(32, rdSection);
        /// <summary>
        /// Source 1 register field.
        /// </summary>
        static readonly BitVector32.Section rs1Section = BitVector32.CreateSection(32, rs2Section);
        /// <summary>
        /// Address section for I-Type Instructions.
        /// </summary>
        static readonly BitVector32.Section addressSection = BitVector32.CreateSection(short.MaxValue); //Displacement based on RS1.
        // For J-Types we need to create a new bitvector32 and put the op bits to 0 and convert to int32

        /// <summary>
        /// Instruction OpCode field.
        /// </summary>
        static readonly BitVector32.Section opSection = BitVector32.CreateSection(32, rs1Section);

        /// <summary>
        /// Bitvector storing 0 to use recurrently to clean registers.
        /// </summary>
        private static readonly BitVector32 zeroBits = new BitVector32(0);
        /// <summary>
        /// Bitvector storing 0 to use recurrently to clean double registers.
        /// </summary>
        private static readonly BitVector32[] zeroBitsDouble = { zeroBits, zeroBits};

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
        /// Special Register to store data fetched.
        /// </summary>
        private BitVector32 IR;

        /// <summary>
        /// Instruction in each execution stage.
        /// </summary>
        private Queue<uint> PipelinedInstructions;

        /// <summary>
        /// Current Instruction in human legible format.
        /// </summary>
        private Assembler.Instruction currentInstruction;

        /// <summary>
        /// Special register to keep the output from the ALU.
        /// </summary>
        private KeyValuePair<BitVector32, uint?> ALUout;

        /// <summary>
        /// Special register to keep the output from the FPU.
        /// </summary>
        private KeyValuePair<BitVector32[], uint?> FPUout; //Size of 2 to store doubles.
        
        /// <summary>
        /// Stores if the FPU outputed a float or double.
        /// </summary>
        private bool fpuDouble = false;

        /// <summary>
        /// The number of cycles runned in the curtent emulation.
        /// </summary>
        public ulong Cycles { get; private set; }

        /// <summary>
        /// Lists of pending memory accesses.
        /// </summary>
        private List<MemAccess?> PedingMemAccess = new List<MemAccess?>();

        /// <summary>
        /// A list that holds if a registers is being written by an instruction.
        /// </summary>
        private byte[] UsedRegisters = new byte[32];

        /// <summary>
        /// Instruction name extracted from memory.
        /// </summary>
        private string? IDInstruction = null;
        /// <summary>
        /// Instruction arguments.
        /// </summary>
        private int IDOpcode, rd, rs2, rs1, funct, shamt, address;

        /// <summary>
        /// Stages of execution where the CPU might need to wait.
        /// </summary>
        private bool IDstall, WBstall, ALUStall, MULTStall, DIVStall, ADDFStall, IDStall;

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
            
            this.PipelinedInstructions = new Queue<uint>(5);

            this.PedingMemAccess = new List<MemAccess?>();
            this.PedingWB = new List<KeyValuePair<int, BitVector32>>();
            this.PedingfpWB = new List<KeyValuePair<int, byte[]>>();

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
        }

        /// <summary>
        /// Processes the next instructions and places them into memory.
        /// </summary>
        /// <param name="assembly">The pre processed and cleaned assembly.</param>
        /// <exception cref="AccessViolationException">If data section is too long and overwrites data segment.</exception>
        /// <exception cref="ArgumentException">If the argument is not correctly formatted.</exception>
        /// <exception cref="NotImplementedException">If the instruction or the argument is not implemented.</exception>
        public static void LoadProgram(ASM assembly)
        {
            //Setup instance PC to run the emulation.
            Instance.PC = (uint)assembly.textAddress;
            Instance.startingPC = Instance.PC;
            Instance.loadedProgram = assembly;
            
            //Process data directives and load them into memory.
            int dataLength = assembly.dataAddress;
            for (uint i = 0; i < assembly.DataSegment.Count; i++)
            {
                if (assembly.Labels.ContainsKey(i))
                {
                    labels.Add(assembly.Labels[i], (uint)(dataLength + assembly.dataAddress));
                }
                string[] aux = assembly.DataSegment[(int)i].Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
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

            //Now assemble the instructions. and place them in memory.
            BitVector32 assembledInstruction;
            int instructionsPlaced = 0;
            foreach (string instruction in assembly.CodeSegment)
            {
                int parsedArguments = 1;
                Assembler.Instruction instructionSyntax = Assembler.OpCodes[0]; //Default to NOP so compiler doesn't complain.
                assembledInstruction = new BitVector32();
                string[] aux = instruction.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (var opcode in Assembler.OpCodes)
                {
                    if (opcode.Name == aux[0])
                    {
                        instructionSyntax = opcode;
                        BitVector32 plainInstruction = new BitVector32((int)opcode.Opcode);
                        assembledInstruction[opSection] = plainInstruction[opSection];
                        if (assembledInstruction[opSection] is 1 or 0)
                            assembledInstruction[functSection] = plainInstruction[functSection];
                        break;
                    }
                }
                //Process the arguments.
                foreach (char arg in instructionSyntax.Args)
                {
                    if (arg != ',')
                    {
                        string[] splitted;
                        switch (arg)
                        {
                            case 'c':
                                assembledInstruction[rdSection] = int.Parse(aux[parsedArguments]);
                                break;
                            case 'a':
                                if (aux[parsedArguments].Contains('('))
                                {
                                    splitted = aux[parsedArguments].Replace('(', ' ').Replace(')', ' ').Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                    try
                                    {
                                        if (assembledInstruction[opSection] is 1 or 0)
                                            assembledInstruction[shamtSection] = int.Parse(splitted[0]);
                                        assembledInstruction[addressSection] = int.Parse(splitted[0]);
                                    }
                                    catch (Exception)
                                    {
                                        if (assembledInstruction[opSection] is 1 or 0)
                                            assembledInstruction[shamtSection] = labels.ContainsKey(aux[parsedArguments]) ? (int)labels[aux[parsedArguments]] : throw new ArgumentException("Invalid label");
                                        assembledInstruction[addressSection] = labels.ContainsKey(aux[parsedArguments]) ? (int)labels[aux[parsedArguments]] : throw new ArgumentException("Invalid label");
                                    }
                                    assembledInstruction[rs1Section] = int.Parse(splitted[1]);
                                }
                                else
                                    assembledInstruction[rs1Section] = int.Parse(aux[parsedArguments]);
                                break;
                            case 'b':
                                if (aux[parsedArguments].Contains('('))
                                {
                                    splitted = aux[parsedArguments].Replace('(', ' ').Replace(')', ' ').Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                    try
                                    {
                                        if (assembledInstruction[opSection] is 1 or 0)
                                            assembledInstruction[shamtSection] = int.Parse(splitted[0]);
                                        assembledInstruction[addressSection] = int.Parse(splitted[0]);
                                    }
                                    catch (Exception)
                                    {
                                        if (assembledInstruction[opSection] is 1 or 0)
                                            assembledInstruction[shamtSection] = labels.ContainsKey(aux[parsedArguments]) ? (int)labels[aux[parsedArguments]] : throw new ArgumentException("Invalid label");
                                        assembledInstruction[addressSection] = labels.ContainsKey(aux[parsedArguments]) ? (int)labels[aux[parsedArguments]] : throw new ArgumentException("Invalid label");
                                    }
                                    assembledInstruction[rs2Section] = int.Parse(splitted[1]);
                                }
                                else
                                    assembledInstruction[rs2Section] = int.Parse(aux[parsedArguments]);
                                break;
                            case 'i':
                            case 'I':
                                assembledInstruction[addressSection] = int.Parse(aux[parsedArguments]);
                                break;
                            case 'd':
                            case 'D':
                            case 'p':
                            case 'P':
                                try
                                {
                                    assembledInstruction[addressSection] = int.Parse(aux[parsedArguments]);
                                }
                                catch (Exception)
                                {
                                    assembledInstruction[addressSection] = labels.ContainsKey(aux[parsedArguments]) ? (int)labels[aux[parsedArguments]] : throw new ArgumentException("Invalid label");
                                }
                                break;
                            default:
                                throw new NotImplementedException("Instruction argument not implemented. " + arg + " in line: " + instruction);
                        }
                        parsedArguments++;
                    }
                }
                MemoryManager.Instance.WriteWord((uint)(assembly.textAddress + instructionsPlaced++), assembledInstruction);
            }
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
            this.IF();  //Memory access is the first step. but won't happen if there are peding operations to memory WHICH SHOULD ONLY HAPPEN IF THERE IS A INSTRUCTION REQUESTING IT.
            this.WB();  //We write to registers in the first half of the cycle.
            this.ID();  //We read from registers in the second half to avoid data issues.
            this.EX();  //Apply a cycle to all execution units and place results, if available on the output register.
            this.MEM(); //Access the memory if needed.
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
        
        /// <summary>
        /// Reads the PC address and fetches de data on that memory address.
        /// </summary>
        private void IF()
        {
            if (PedingMemAccess[0] != null || !IDStall)
            {
                IR = MemoryManager.Instance.ReadWordAsBitVector(PC);
                PC += 4;
                PipelinedInstructions.Enqueue(PC);
            }
        }

        /// <summary>
        /// Decodes the instruction to execute.
        /// </summary>
        private void ID()
        {
            if (!IDStall)
            {
                this.IDOpcode = IR[opSection];
                if (this.IDOpcode == 0 || this.IDOpcode == 1)
                {
                    this.funct = IR[functSection];
                    this.shamt = IR[shamtSection];
                    this.rd = IR[rdSection];
                    this.rs2 = IR[rs2Section];
                    this.rs1 = IR[rs1Section];
                }
                else
                {
                    switch (IDOpcode)
                    {
                        case 2:
                        case 3:
                        case 10:
                        case 11:
                            IR[opSection] = 0;              // Since BitVector32 Sections only support up to 16bits, we need to place the upper 6 bits to 0
                            address = IR.Data;              // then we can use the whole 32 bits as the address value and 
                            IR[opSection] = (int)IDOpcode;  // then we can place the opcode back into the instruction.
                            break;
                        default:
                            this.address = IR[addressSection];
                            this.rs2 = IR[rs2Section];
                            this.rs1 = IR[rs1Section];
                            break;
                    }
                }
            }
            PedingMemAccess.Add(null);
            IDstall = !this.LoadInstruction();
        }

        /// <summary>
        /// Executes the instruction.
        /// </summary>
        private void EX()
        {
            this.TickAllUnits();
            this.UnloadUnits();
        }

        /// <summary>
        /// Access the memroy and write the result if needed.
        /// </summary>
        private void MEM()
        {
            if (PedingMemAccess[0] != null)
            {
                MemAccess wb = (MemAccess)PedingMemAccess[0];
                uint address = wb.Address;
                if (wb.isWrite)
                {
                    foreach (byte b in wb.Content)
                    {
                        MemoryManager.Instance.WriteByte(address++, b);
                    }
                }
                else
                {
                    switch (wb.Type.Value)
                    {
                        case "BYTE":
                            Registers[(int)wb.Destination] = new BitVector32((sbyte)MemoryManager.Instance.ReadByte(wb.Address));
                            break;
                        case "UBYTE":
                            Registers[(int)wb.Destination] = new BitVector32(MemoryManager.Instance.ReadByte(wb.Address));
                            break;
                        case "HALF":
                        case "UHALF":
                            Registers[(int)wb.Destination] = new BitVector32(MemoryManager.Instance.ReadHalf((wb.Address)));
                            break;
                        case "UWORD":
                        case "WORD":
                            Registers[(int)wb.Destination] = new BitVector32(MemoryManager.Instance.ReadWordAsBitVector(wb.Address));
                            break;
                        case "FLOAT":
                            fRegisters[(int)wb.Destination] = new BitVector32(MemoryManager.Instance.ReadFloatAsBitVector(wb.Address));
                            break;
                        case "DOUBLE":
                            byte[] value = BitConverter.GetBytes(MemoryManager.Instance.ReadDouble(wb.Address));
                            fRegisters[(int)wb.Destination] = new BitVector32(BitConverter.ToInt32(value, 0));
                            fRegisters[(int)wb.Destination + 1] = new BitVector32(BitConverter.ToInt32(value, 4));
                            break;
                    }
                }
            }
            PedingMemAccess.RemoveAt(0);
        }

        /// <summary>
        /// Writes the result back to the register.
        /// </summary>
        private void WB()
        {
            if (ALUout.Value != null)
            {
                Registers[(int)ALUout.Value] = ALUout.Key;
                UsedRegisters[(int)ALUout.Value] = 0;
                ALUout = new KeyValuePair<BitVector32, uint?>(zeroBits, null);
            }
            else if (FPUout.Value != null)
            {
                fRegisters[(int)FPUout.Value] = FPUout.Key[0];
                UsedRegisters[(int)FPUout.Value] = 0;
                if (fpuDouble)
                    fRegisters[(int)FPUout.Value + 1] = FPUout.Key[1];
                FPUout = new KeyValuePair<BitVector32[], uint?>(zeroBitsDouble, null);
            }
            PipelinedInstructions.Dequeue(); //Dequeue the instruction that finished.
        }

        private void UnloadUnits()
        {
            foreach (ALU a in alus)
            {
                if (a.Done)
                {
                    int dest;
                    int? output = a.GetValue(out dest);
                    if (output != null)
                    {
                        ALUout = new KeyValuePair<BitVector32, uint?>(new BitVector32((int)output), (uint)dest);
                    }
                    ALUStall = false;
                    break; //Unload only 1 unit per cycle.
                }
            }
            bool fpuDone = false;
            foreach (FPU a in adds)
            {
                if (a.Done)
                {
                    int dest;
                    double? output = a.GetValue(out dest, out fpuDouble);
                    if (output != null)
                    {
                        byte[] outBytes = BitConverter.GetBytes((double)output);
                        BitVector32[] aux = new BitVector32[2];
                        aux[0] = new BitVector32(BitConverter.ToInt32(outBytes,0));
                        aux[1] = new BitVector32(BitConverter.ToInt32(outBytes,4));
                        FPUout = new KeyValuePair<BitVector32[], uint?>(aux, (uint)dest);
                        fpuDone = true;
                    }
                    ADDFStall = false;
                    break; //Unload only 1 unit per cycle.
                }
            }
            if (fpuDone)
                return;
            foreach (FPU a in muls)
            {
                if (a.Done)
                {
                    int dest;
                    double? output = a.GetValue(out dest, out fpuDouble);
                    if (output != null)
                    {
                        byte[] outBytes = BitConverter.GetBytes((double)output);
                        BitVector32[] aux = new BitVector32[2];
                        aux[0] = new BitVector32(BitConverter.ToInt32(outBytes, 0));
                        aux[1] = new BitVector32(BitConverter.ToInt32(outBytes, 4));
                        FPUout = new KeyValuePair<BitVector32[], uint?>(aux, (uint)dest);
                        fpuDone = true;
                    }
                    MULTStall = false;
                    break; //Unload only 1 unit per cycle.
                }
            }
            if (fpuDone)
                return;
            foreach (FPU a in divs)
            {
                if (a.Done)
                {
                    int dest;
                    double? output = a.GetValue(out dest, out fpuDouble);
                    if (output != null)
                    {
                        byte[] outBytes = BitConverter.GetBytes((double)output);
                        BitVector32[] aux = new BitVector32[2];
                        aux[0] = new BitVector32(BitConverter.ToInt32(outBytes, 0));
                        aux[1] = new BitVector32(BitConverter.ToInt32(outBytes, 4));
                        FPUout = new KeyValuePair<BitVector32[], uint?>(aux, (uint)dest);
                    }
                    DIVStall = false;
                    break; //Unload only 1 unit per cycle.
                }
            }
        }
        
        /// <summary>
        /// Applies a clock cycle to all units.
        /// </summary>
        private void TickAllUnits()
        {
            foreach (var a in alus)
                a.DoTick();
            foreach (var a in adds)
                a.DoTick();
            foreach (var a in muls)
                a.DoTick();
            foreach (var a in divs)
                a.DoTick();
        }

        /// <summary>
        /// Loads the instruction into an EX unit.
        /// </summary>
        /// <returns>If the instruction was corretly loaded.</returns>
        private bool LoadInstruction()
        {
            bool success = true;

            BitVector32 A = new BitVector32(Registers[rs1]);
            BitVector32[] Afp = new BitVector32[2];
            BitVector32 B = new BitVector32(Registers[rs2]);
            BitVector32[] Bfp = new BitVector32[2];
            Afp[0] = new BitVector32(fRegisters[rs1]);
            Afp[1] = new BitVector32(fRegisters[rs1 + 1]);
            Bfp[0] = new BitVector32(fRegisters[rs2]);
            Bfp[1] = new BitVector32(fRegisters[rs2 + 1]);

            BitVector32[] arr = new BitVector32[2]; //Aux vector
            List<byte> aux = new List<byte>();
            if (UsedRegisters[rs1] != 0 || UsedRegisters[rs2] != 0) //Check if any of the registers need a data being calculated.
            {
                if (Forwarding) //Check if data forwarding is enabled and look for the data.
                {
                    if (ALUout.Value == rs1)
                        A = ALUout.Key;
                    else if (ALUout.Value == rs2)
                        B = ALUout.Key;
                    else if (FPUout.Value == rs1)
                        Afp = FPUout.Key;
                    else if (FPUout.Value == rs2)
                        Bfp = FPUout.Key;
                    else
                        return false; //Wait as the data is not ready yet.
                }
                else
                    return false; //Failed to load instruction due to possible data error.
            }
            UsedRegisters[rd] = 1;
            switch (IDOpcode)
            {
                case 1:
                    switch (funct)
                    {
                        case 8: //CVTF2D
                            arr[0] =new BitVector32(fRegisters[rs1]);
                            this.FPUout = new KeyValuePair<BitVector32[], uint?>(arr, (uint)rd);
                            break;
                        case 9: //CVTF2I
                            this.ALUout = new KeyValuePair<BitVector32, uint?>(new BitVector32(fRegisters[rs1]), (uint)rd);
                            break;
                        case 10: //CVTD2F
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(fRegisters[rs1].Data));
                            aux.AddRange(BitConverter.GetBytes(fRegisters[rs1 + 1].Data));
                            arr[0] = new BitVector32(BitConverter.ToInt32(aux.ToArray(), 0));
                            this.FPUout = new KeyValuePair<BitVector32[], uint?>(arr, (uint)rd);
                            break;
                        case 11: //CVTD2I
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(fRegisters[rs1].Data));
                            aux.AddRange(BitConverter.GetBytes(fRegisters[rs1 + 1].Data));
                            this.ALUout = new KeyValuePair<BitVector32, uint?>(new BitVector32(BitConverter.ToInt32(aux.ToArray())), (uint)rd);
                            break;
                        case 12: //CVTI2F
                            arr[0] = new BitVector32(Registers[rs1]);
                            this.FPUout = new KeyValuePair<BitVector32[], uint?>(arr, (uint)rd);
                            break;
                        case 13: //CVTI2D
                            arr[0] = new BitVector32(fRegisters[rs1]);
                            this.FPUout = new KeyValuePair<BitVector32[], uint?>(arr, (uint)rd);
                            break;
                        case 0: //ADDF
                        case 1: //SUBF
                            foreach (FPU fpu in adds)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(rd, BitConverter.Int32BitsToSingle(fRegisters[rs1].Data), BitConverter.Int32BitsToSingle(fRegisters[rs2].Data), funct, ADDDelay);
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
                                    ADDFStall = true;
                                    success = false;
                                }
                            }
                            break;
                        case 4: //ADDD
                        case 5: //SUBD
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(Afp[0].Data));
                            aux.AddRange(BitConverter.GetBytes(Afp[1].Data));
                            aux.AddRange(BitConverter.GetBytes(Bfp[0].Data));
                            aux.AddRange(BitConverter.GetBytes(Bfp[1].Data));
                            foreach (FPU fpu in adds)
                            {
                                if (!fpu.Busy)
                                {
                                    byte[] auxArr = aux.ToArray();
                                    fpu.LoadValues(rd, BitConverter.ToDouble(auxArr, 0), BitConverter.ToDouble(auxArr, 8), funct, ADDDelay);
                                    break;
                                }
                                if (adds.Last() == fpu)
                                { 
                                    ADDFStall = true;
                                    success = false;
                                }
                            }
                            break;
                        case 2: //MULTF
                            foreach (FPU fpu in muls)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(rd, BitConverter.Int32BitsToSingle(Afp[0].Data), BitConverter.Int32BitsToSingle(Bfp[1].Data), funct, MULDDelay);
                                    break;
                                }
                                if (muls.Last() == fpu)
                                { 
                                    MULTStall = true;
                                    success = false;
                                }
                            }
                            break;
                        case 3: //DIVF
                            foreach (FPU fpu in divs)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(rd, BitConverter.Int32BitsToSingle(Afp[0].Data), BitConverter.Int32BitsToSingle(Bfp[1].Data), funct, DIVDelay);
                                    break;
                                }
                                if (divs.Last() == fpu)
                                { 
                                    DIVStall = true;
                                    success = false;
                                }
                            }
                            break;
                        case 6: //MULTD
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(Afp[0].Data));
                            aux.AddRange(BitConverter.GetBytes(Afp[1].Data));
                            aux.AddRange(BitConverter.GetBytes(Bfp[0].Data));
                            aux.AddRange(BitConverter.GetBytes(Bfp[1].Data));
                            foreach (FPU fpu in muls)
                            {
                                if (!fpu.Busy)
                                {
                                    byte[] auxArr = aux.ToArray();
                                    fpu.LoadValues(rd, BitConverter.ToDouble(auxArr, 0), BitConverter.ToDouble(auxArr, 8), funct, MULDDelay);
                                    break;
                                }
                                if (muls.Last() == fpu)
                                {
                                    MULTStall = true;
                                    success = false;
                                }
                            }
                            break;
                        case 7: //DIVD
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(Afp[0].Data));
                            aux.AddRange(BitConverter.GetBytes(Afp[1].Data));
                            aux.AddRange(BitConverter.GetBytes(Bfp[0].Data));
                            aux.AddRange(BitConverter.GetBytes(Bfp[1].Data));
                            foreach (FPU fpu in divs)
                            {
                                if (!fpu.Busy)
                                {
                                    byte[] auxArr = aux.ToArray();
                                    fpu.LoadValues(rd, BitConverter.ToDouble(auxArr, 0), BitConverter.ToDouble(auxArr, 8), funct, DIVDelay);
                                    break;
                                }
                                if (divs.Last() == fpu)
                                { 
                                    DIVStall = true; 
                                    success = false;
                                }
                            }
                            break;
                        case 14: //MULT
                        case 22: //MULTU
                            foreach (FPU fpu in muls)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(rd, BitConverter.Int32BitsToSingle(Afp[0].Data), BitConverter.Int32BitsToSingle(Bfp[1].Data), funct, MULDDelay);
                                    break;
                                }
                                if (muls.Last() == fpu)
                                { 
                                    MULTStall = true;
                                    success = false;
                                }
                            }
                            break;
                        case 15: //DIV
                        case 23: //DIVU
                            foreach (FPU fpu in divs)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(rd, BitConverter.Int32BitsToSingle(Afp[0].Data), BitConverter.Int32BitsToSingle(Bfp[1].Data), funct, DIVDelay);
                                    break;
                                }
                                if (divs.Last() == fpu)
                                { 
                                    DIVStall = true;
                                    success = false;
                                }
                            }
                            break;
                        case 16: //LOGIC OPERATIONS FLOATS
                        case 17:
                        case 18:
                        case 19:
                        case 20:
                        case 21:
                            foreach (FPU fpu in adds)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(rd, BitConverter.Int32BitsToSingle(Afp[0].Data), BitConverter.Int32BitsToSingle(Bfp[1].Data), funct, 1);
                                    break;
                                }
                                if (adds.Last() == fpu)
                                { 
                                    ADDFStall = true;
                                    success = false;
                                }
                            }
                            break;
                        default: //LOGIC OPERATIONS DOUBLES
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(Afp[0].Data));
                            aux.AddRange(BitConverter.GetBytes(Afp[1].Data));
                            aux.AddRange(BitConverter.GetBytes(Bfp[0].Data));
                            aux.AddRange(BitConverter.GetBytes(Bfp[1].Data));
                            foreach (FPU fpu in adds)
                            {
                                if (!fpu.Busy)
                                {
                                    byte[] auxArr = aux.ToArray();
                                    fpu.LoadValues(rd, BitConverter.ToDouble(auxArr, 0), BitConverter.ToDouble(auxArr, 8), funct, 1);
                                    break;
                                }
                                if (adds.Last() == fpu)
                                { 
                                    ADDFStall = true;
                                    success = false;
                                }
                            }
                            break;

                    }
                    break;
                case 0:
                    switch (funct)
                    {
                        case 48: //MOVI2S
                            //UNIMPLEMENTED
                            break;
                        case 49: //MOVS2I
                            //UNIMPLEMENTED
                            break;
                        case 50: //MOVF
                            arr[0] = new BitVector32(fRegisters[rs1]);
                            this.FPUout = new KeyValuePair<BitVector32[], uint?>(arr, (uint)rd);
                            break;
                        case 51: //MOVD
                            arr[0] = new BitVector32(fRegisters[rs1]);
                            arr[1] = new BitVector32(fRegisters[rs1+1]);
                            this.FPUout = new KeyValuePair<BitVector32[], uint?>(arr, (uint)rd);
                            break;
                        case 52: //MOVFP2I
                            this.ALUout = new KeyValuePair<BitVector32, uint?>(new BitVector32(fRegisters[rs1]), (uint)rd);
                            break;
                        case 53: //MOVI2FP
                            arr[0] = new BitVector32(Registers[rs1]);
                            this.FPUout = new KeyValuePair<BitVector32[], uint?>(arr, (uint)rd);
                            break;
                        default: //ALU OPERATIONS
                            foreach (ALU alu in alus)
                            {
                                if (!alu.Busy)
                                {
                                    alu.LoadValues(rd, A.Data, B.Data, funct);
                                    break;
                                }
                                if (alus.Last() == alu)
                                { 
                                    ALUStall = true;
                                    success = false;
                                }
                            }
                            break;
                    }
                    break;
                case 2:
                    PC = (uint)address;
                    break;
                case 3:
                    Registers[31] = new BitVector32((int)(PC + 4));
                    PC = (uint)address;
                    break;
                case 4:
                    if (Registers[rs1].Data == 0)
                        PC = (uint)address;
                    break;
                case 5:
                    if (Registers[rs1].Data != 0)
                        PC = (uint)address;
                    break;
                case 6:
                    if (FPUout.Key[0].Data == 1)
                        PC = (uint)address;
                    break;
                case 7:
                    if (FPUout.Key[0].Data == 0)
                        PC = (uint)address;
                    break;
                case 17:
                    if (address == 0)
                        trap0Found = true;
                    break;
                case 32:
                    this.PedingMemAccess[PedingMemAccess.Count] = new MemAccess(rs2, (uint)(rs1 + address), MemAccessTypes.BYTE);
                    break;
                case 33:
                    this.PedingMemAccess[PedingMemAccess.Count] = new MemAccess(rs2, (uint)(rs1 + address), MemAccessTypes.HALF);
                    break;
                case 35:
                    this.PedingMemAccess[PedingMemAccess.Count] = new MemAccess(rs2, (uint)(rs1 + address), MemAccessTypes.WORD);
                    break;
                case 36:
                    this.PedingMemAccess[PedingMemAccess.Count] = new MemAccess(rs2, (uint)(rs1 + address), MemAccessTypes.UBYTE);
                    break;
                case 37:
                    this.PedingMemAccess[PedingMemAccess.Count] = new MemAccess(rs2, (uint)(rs1 + address), MemAccessTypes.UHALF);
                    break;
                case 38:
                    this.PedingMemAccess[PedingMemAccess.Count] = new MemAccess(rs2, (uint)(rs1 + address), MemAccessTypes.FLOAT);
                    break;
                case 39:
                    this.PedingMemAccess[PedingMemAccess.Count] = new MemAccess(rs2, (uint)(rs1 + address), MemAccessTypes.DOUBLE);
                    break;
                case 40:
                    this.PedingMemAccess[PedingMemAccess.Count] = new MemAccess((uint)(rs2 + address), BitConverter.GetBytes(Registers[rs1].Data), MemAccessTypes.BYTE);
                    break;
                case 41:
                    this.PedingMemAccess[PedingMemAccess.Count] = new MemAccess((uint)(rs2 + address), BitConverter.GetBytes(Registers[rs1].Data), MemAccessTypes.HALF);
                    break;
                case 43:
                    this.PedingMemAccess[PedingMemAccess.Count] = new MemAccess((uint)(rs2 + address), BitConverter.GetBytes(Registers[rs1].Data), MemAccessTypes.WORD);
                    break;
                case 46:
                    this.PedingMemAccess[PedingMemAccess.Count] = new MemAccess((uint)(rs2 + address), BitConverter.GetBytes(fRegisters[rs1].Data), MemAccessTypes.FLOAT);
                    break;
                case 47:
                    aux.Clear();
                    aux.AddRange(BitConverter.GetBytes(Afp[0].Data));
                    aux.AddRange(BitConverter.GetBytes(Afp[1].Data));
                    this.PedingMemAccess[PedingMemAccess.Count] = new MemAccess((uint)(rs2 + address), aux.ToArray(), MemAccessTypes.DOUBLE);
                    break;
            }
            return success;
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
