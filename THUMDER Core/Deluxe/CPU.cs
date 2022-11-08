using System.Collections.Specialized;
using System.Diagnostics;
using THUMDER.Interpreter;

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
        /// Instruction Memory Address Register.
        /// </summary>
        private BitVector32 IMAR;

        /// <summary>
        /// Data Memory Address Register.
        /// </summary>
        private BitVector32 DMAR;

        /// <summary>
        /// Instruction in each execution stage.
        /// </summary>
        private BitVector32 IFreg, IDreg, EXreg, MEMreg, WBreg;

        /// <summary>
        /// Register to store the instruction currentyly in execution.
        /// </summary>
        private BitVector32 OPreg;

        /// <summary>
        /// Loaded Memory Data.
        /// </summary>
        private BitVector32[] LMD;

        /// <summary>
        /// Saved Data Register
        /// </summary>
        private BitVector32[] SDR;

        /// <summary>
        /// Special register to keep the output from EX.
        /// </summary>
        private BitVector32[] ALUout; //Size of 2 to store doubles.

        /// <summary>
        /// Floating point status register.
        /// </summary>
        private BitVector32 FPstatus;

        /// <summary>
        /// Register to store jump conditions.
        /// </summary>
        private bool Condition;

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

        /// <summary>
        /// Stages of execution where the CPU might need to wait.
        /// </summary>
        private bool RStall, DStall;

        /// <summary>
        /// Controls the stopping of the emulation.
        /// </summary>
        private bool trap0Found, stop = false;

        /// <summary>
        /// Source register A
        /// </summary>
        private BitVector32[] A = new BitVector32[2];

        /// <summary>
        /// Source register B
        /// </summary>
        private BitVector32[] B = new BitVector32[2];
        /// <summary>
        /// Are we doing a fp comparison?
        /// </summary>
        private bool comparingFP = false;
        
        /// <summary>
        /// Reads the PC address and fetches de data on that memory address.
        /// </summary>
        private void IF()
        {
            if (IFreg[opSection] is 5 or 6 && ((IFreg[rs1Section] == IDreg[rdSection]) && (IDreg[opSection] == 0 && IDreg[functSection] is 32 or 33 or 34 or 35) || IDreg[opSection] is 8 or 9 or 10 or 11 && (IFreg[rs1Section] == IDreg[rs2Section])))
            {
                DStall = true;
                IDreg = new BitVector32(0);
            }
            if (!DStall && !trap0Found)
            {
                IDreg = IFreg;
                IMAR = new BitVector32((int)PC);
                IFreg = MemoryManager.Instance.ReadWordAsBitVector((uint)IMAR.Data);
                PC += 4;
            }
            if (trap0Found)
            {
                IDreg = new BitVector32(0);
                IFreg = new BitVector32(0);
            }
        }

        /// <summary>
        /// Decodes the instruction to execute.
        /// </summary>
        private void ID()
        {
            if (!DStall)
            {
                this.IDOpcode = 0;
                this.address  = 0;
                this.funct    = 0;
                this.shamt    = 0;
                this.rd       = 0;
                this.rs2      = 0;
                this.rs1      = 0;

                this.IDOpcode = IDreg[opSection];
                if (this.IDOpcode is 0 or 1)
                {
                    this.funct = IDreg[functSection];
                    this.shamt = IDreg[shamtSection];
                    this.rd = IDreg[rdSection];
                    this.rs2 = IDreg[rs2Section];
                    this.rs1 = IDreg[rs1Section];
                }
                else
                {
                    switch (IDOpcode)
                    {
                        case 2:
                        case 3:
                        case 10:
                        case 11:
                            this.address = IDreg.Data;         // then we can use the whole 32 bits as the address value and 
                            IDreg[opSection] = (int)IDOpcode;  // then we can place the opcode back into the instruction.
                            break;
                        case 17:
                            //trap0Found = true;
                            break;
                        default: //Immediate numbers
                            this.address = IDreg[addressSection]; //Place the immediate number in rs2 to operate it.
                            this.rd = IDreg[rs2Section]; //For I-type rd is placed in the 5 bits of rs2.
                            this.rs1 = IDreg[rs1Section];
                            break;
                    }
                }
            }
            DStall = !this.LoadInstruction();
            if (RStall)
                DStall = true;
            if (DStall && IFreg[opSection] is 4 or 5 or 6 or 7) //If the next instruction is a branch, the stall is a load stall.
            {
                ++JumpStalls;
            }
            if (DStall && MEMreg[opSection] is > 31 and < 40) // If the instruction in mem is a load, it is a load stall
            {
                ++LDStalls;
            }
            if (!DStall && IDreg.Data != 0)
            {
                decodedInstructions++;
                EXreg = IDreg;
            }
            if (Condition)
            {
                int addss = address;
                if (IDOpcode is 2 or 3)
                {
                    BitVector32 auxReg = new BitVector32(IDreg);
                    auxReg[opSection] = 0;
                    addss = auxReg.Data;
                } //This is hack, but needed in order to jump as intended.
                PC = (uint)addss;
                Condition = false;
                DStall = false;
                ClearPipeline();
            }
        }
        
        /// <summary>
        /// Executes the instruction.
        /// </summary>
        private void EX()
        {
            RStall = !ExecuteInstruction();
            TickAllUnits();
            if (PedingMemAccess.Count <= 1)
                PedingMemAccess.Add(null); //Put a null memory access to have it indicate that there is no pending memory access and next insctruction can be taken.
            if (RStall) //IF EX is stalled, pass 0 to the next stage.
            {
                MEMreg = new BitVector32(0);
                ++fpStalls;
            }
        }

        /// <summary>
        /// Access the memroy and write the result if needed.
        /// </summary>
        private void MEM()
        {
            if (MEMreg[opSection] > 31)
            {
                if (MEMreg[opSection] > 39) //Writes are opcode 40+
                {
                    uint addressToWrite = (uint)ALUout[0].Data;
                    List<byte> dataToWrite = new List<byte>();
                    dataToWrite.AddRange(BitConverter.GetBytes((uint)SDR[0].Data));
                    if (MEMreg[opSection] == 47) //Only read the second register if its a double
                        dataToWrite.AddRange(BitConverter.GetBytes((uint)SDR[1].Data));
                    byte[] wb = dataToWrite.ToArray();
                    foreach (byte b in wb)
                    {
                        MemoryManager.Instance.WriteByte(addressToWrite++, b);
                    }
                    Instance.MemStores++;
                }
                else
                {
                    switch (MEMreg[opSection])
                    {
                        case 32:
                            LMD[0] = new BitVector32((sbyte)MemoryManager.Instance.ReadByte((uint)ALUout[0].Data));
                            break;
                        case 36:
                            LMD[0] = new BitVector32(MemoryManager.Instance.ReadByte((uint)ALUout[0].Data));
                            break;
                        case 33:
                        case 37:
                            LMD[0] = new BitVector32(MemoryManager.Instance.ReadHalf(((uint)ALUout[0].Data)));
                            break;
                        case 35:
                            LMD[0] = new BitVector32(MemoryManager.Instance.ReadWordAsBitVector((uint)ALUout[0].Data));
                            break;
                        case 38:
                            LMD[0] = new BitVector32(MemoryManager.Instance.ReadFloatAsBitVector((uint)ALUout[0].Data));
                            break;
                        case 39:
                            byte[] value = BitConverter.GetBytes(MemoryManager.Instance.ReadDouble((uint)ALUout[0].Data));
                            LMD[0] = new BitVector32(BitConverter.ToInt32(value, 0));
                            LMD[1] = new BitVector32(BitConverter.ToInt32(value, 4));
                            break;
                    }
                    Instance.MemLoads++;
                }
            }
            else
            {
                LMD[0] = new BitVector32(ALUout[0]);
                LMD[1] = new BitVector32(ALUout[1]);
            }
            if (MEMreg[opSection] == 1 && MEMreg[functSection] is > 15 and not 22 or 23) //FP comparisons
                comparingFP = false;
            PedingMemAccess.RemoveAt(0);
            WBreg = MEMreg;
            MEMreg = new BitVector32(0);
        }

        /// <summary>
        /// Writes the result back to the register.
        /// </summary>
        private void WB()
        {
            switch (WBreg[opSection])
            {
                case 0: //Wirte Reg-Reg
                    if (WBreg[functSection] != 0)
                    {
                        Registers[WBreg[rdSection]] = LMD[0];
                        UsedRegisters[WBreg[rdSection]] = 0;
                    }
                    break;
                case 17:
                    stop = true;
                    break;
                case > 7 and < 16: //Wirte Reg-imm
                case > 19 and < 32:
                    Registers[WBreg[rs2Section]] = LMD[0];
                    UsedRegisters[WBreg[rs2Section]] = 0;
                    break;
                case 1: //Wirte Reg-Reg fp
                    if (WBreg[functSection] > 15 && WBreg[functSection] is 22 or 23)
                    {
                        fRegisters[WBreg[rdSection]] = LMD[0];
                        UsedRegisters[WBreg[rdSection]] = 0;
                        if (ALUout[1].Data != 0)
                            fRegisters[WBreg[rdSection] + 1] = LMD[1];
                    }
                    break;
                case > 32 and < 38: //Wirte loaded from meory
                    Registers[WBreg[rs2Section]] = LMD[0];
                    break;
                case 38: //Wirte fp from memory
                    fRegisters[WBreg[rs2Section]] = LMD[0];
                    break;
                case 39: //Wirte double from memory
                    fRegisters[WBreg[rs2Section]] = LMD[0];
                    fRegisters[WBreg[rs2Section] + 1] = LMD[1];
                    break;
                default:
                    //ALUout = zeroBits;
                    break;
            }
        }

        /// <summary>
        /// Clears the execution pipeline after a jump
        /// </summary>
        private void ClearPipeline()
        {
            ++ControlStalls;
            IDreg = new BitVector32(0); //Clear the execution pipeline
            IFreg = new BitVector32(0);
            this.IDOpcode = 0; //Clean the decoded instruction.
            this.address = 0;
            this.funct = 0;
            this.shamt = 0;
            this.rd = 0;
            this.rs2 = 0;
            this.rs1 = 0;
        }

        /// <summary>
        /// Unloads the data operated in the EX units.
        /// </summary>
        private void UnloadUnits()
        {
            foreach (ALU a in alus)
            {
                if (a.Done)
                {
                    int? output = a.GetValue(out _);
                    if (output != null)
                    {
                        ALUout[0] = new BitVector32((int)output);
                        ALUout[1] = new BitVector32(0);
                        MEMreg = OPreg;
                    }
                    return;
                }
            }
            foreach (FPU a in adds)
            {
                if (a.Done)
                {
                    int dest; //Need it to easily know if it is a bool value.
                    double? output = a.GetValue(out dest, out _);
                    if (output != null)
                    {
                        byte[] outBytes = BitConverter.GetBytes((double)output);
                        BitVector32[] aux = new BitVector32[2];
                        aux[0] = new BitVector32(BitConverter.ToInt32(outBytes, 0));
                        aux[1] = new BitVector32(BitConverter.ToInt32(outBytes, 4));
                        ALUout[0] = new BitVector32(aux[0]);
                        ALUout[1] = new BitVector32(aux[1]);
                        MEMreg = OPreg;
                    }
                    if (dest >= 33)
                    {
                        comparingFP = false;
                        FPstatus = new BitVector32((int)output);
                    }
                    return; //Unload only 1 fp unit per cycle.
                }
            }
            foreach (FPU a in muls)
            {
                if (a.Done)
                {
                    double? output = a.GetValue(out _, out _);
                    if (output != null)
                    {
                        byte[] outBytes = BitConverter.GetBytes((double)output);
                        BitVector32[] aux = new BitVector32[2];
                        aux[0] = new BitVector32(BitConverter.ToInt32(outBytes, 0));
                        aux[1] = new BitVector32(BitConverter.ToInt32(outBytes, 4));
                        ALUout[0] = new BitVector32(aux[0]);
                        ALUout[1] = new BitVector32(aux[1]);
                        MEMreg = OPreg;
                    }
                    return; //Unload only 1 fp unit per cycle.
                }
            }
            foreach (FPU a in divs)
            {
                if (a.Done)
                {
                    double? output = a.GetValue(out _, out _);
                    if (output != null)
                    {
                        byte[] outBytes = BitConverter.GetBytes((double)output);
                        BitVector32[] aux = new BitVector32[2];
                        aux[0] = new BitVector32(BitConverter.ToInt32(outBytes, 0));
                        aux[1] = new BitVector32(BitConverter.ToInt32(outBytes, 4));
                        ALUout[0] = new BitVector32(aux[0]);
                        ALUout[1] = new BitVector32(aux[1]);
                        MEMreg = OPreg;
                    }
                    return; //Unload only 1 fp unit per cycle.
                }
            }
            if (OPreg[opSection] is 17 or > 31) //MEM access will just pass by.
                MEMreg = OPreg;
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
            UnloadUnits();
        }

        /// <summary>
        /// Loads the instruction into an EX unit.
        /// </summary>
        /// <returns>If the instruction was corretly loaded.</returns>
        private bool LoadInstruction()
        {
            bool forwarded = false; //This flag will update on data forwarding. If data is forwarded while true a WAW hazard is detected.
            A[0] = new BitVector32(Registers[rs1].Data);
            A[1] = new BitVector32(0);
            B[0] = new BitVector32(Registers[rs2].Data);
            B[1] = new BitVector32(0);
            if (shamt != 0)
            {
                B[0] = new BitVector32(MemoryManager.Instance.ReadWord((uint)(rs2 + shamt)));
            }
            if (IDOpcode == 1)
            {
                A[0] = new BitVector32(fRegisters[rs1].Data);
                B[0] = new BitVector32(fRegisters[rs2].Data);
                if (funct is 4 or 5 or 6 or 7 or 10 or 11 or > 24) //If its double
                {
                    A[1] = new BitVector32(fRegisters[rs1 +1].Data);
                    B[1] = new BitVector32(fRegisters[rs2 +1].Data);
                }
            }

            if ((IDOpcode is not 2 or 3 or 16 or 17)) //first check the if instruction will use registers (not J-type).
            {
                if (!(IDOpcode is 0 && funct is 0)) //Skip if its a nop.
                {
                    switch(WBreg[opSection]) //Forwards from WB
                    {
                        case 2:
                        case 3:
                        case 16:
                        case 17:
                        case > 39:
                            break; //Do nothing for J types or stores
                        case 0:
                            if (WBreg[functSection] != 0)
                            {
                                switch (IDOpcode)
                                {
                                    case 0: //R-type
                                        if (WBreg[rdSection] != 0 && WBreg[rdSection] == rs1)
                                        {
                                            if (Forwarding)
                                            {
                                                A[0] = new BitVector32(LMD[0]);
                                                A[1] = new BitVector32(LMD[1]);
                                                forwarded = true;
                                            }
                                            else
                                                return false;
                                        }
                                        if (WBreg[rdSection] != 0 && WBreg[rdSection] == rs2)
                                        {
                                            if (Forwarding)
                                            {
                                                B[0] = new BitVector32(LMD[0]);
                                                B[1] = new BitVector32(LMD[1]);
                                                forwarded = true;
                                            }
                                            else
                                                return false;
                                        }
                                        break;
                                    case 1:
                                    case > 31 and < 40:
                                    case 46:
                                    case 47:
                                        break; //Wont forward to fp operations or loads.
                                    default: //I-type
                                        if (WBreg[rdSection] != 0 && WBreg[rdSection] == rs1)
                                        {
                                            if (Forwarding)
                                            {
                                                A[0] = new BitVector32(LMD[0]);
                                                A[1] = new BitVector32(LMD[1]);
                                                forwarded = true;
                                            }
                                            else
                                                return false;
                                        }
                                        break;
                                }
                            }
                            break;
                        case 1:
                            switch(IDOpcode)
                            {
                                case 1:
                                    if (WBreg[rdSection] == rs1)
                                    {
                                        if (Forwarding)
                                        {
                                            A[0] = new BitVector32(LMD[0]);
                                            A[1] = new BitVector32(LMD[1]);
                                            forwarded = true;
                                        }
                                        else
                                            return false;
                                    }
                                    if (WBreg[rdSection] == rs2)
                                    {
                                        if (Forwarding)
                                        {
                                            B[0] = new BitVector32(LMD[0]);
                                            B[1] = new BitVector32(LMD[1]);
                                            forwarded = true;
                                        }
                                        else
                                            return false;
                                    }
                                    break;
                                case 46:
                                case 47:
                                    if (WBreg[rdSection] == rs1)
                                    {
                                        if (Forwarding)
                                        {
                                            A[0] = new BitVector32(LMD[0]);
                                            A[1] = new BitVector32(LMD[1]);
                                            forwarded = true;
                                        }
                                        else
                                            return false;
                                    }
                                    if (WBreg[rdSection] == rs2)
                                    {
                                        if (Forwarding)
                                        {
                                            B[0] = new BitVector32(LMD[0]);
                                            B[1] = new BitVector32(LMD[1]);
                                            forwarded = true;
                                        }
                                        else
                                            return false;
                                    }
                                    break;
                                default:
                                    break; //Do nothing for non fp instructions.
                            }
                            break;
                        case 38:
                        case 39:
                            switch (IDOpcode)
                            {
                                case 1:
                                    if (WBreg[rs2Section] == rs1)
                                    {
                                        if (Forwarding)
                                        {
                                            A[0] = new BitVector32(LMD[0]);
                                            A[1] = new BitVector32(LMD[1]);
                                            forwarded = true;
                                        }
                                        else
                                            return false;
                                    }
                                    if (WBreg[rs2Section] == rs2)
                                    {
                                        if (Forwarding)
                                        {
                                            B[0] = new BitVector32(LMD[0]);
                                            B[1] = new BitVector32(LMD[1]);
                                            forwarded = true;
                                        }
                                        else
                                            return false;
                                    }
                                    break;
                                case 46:
                                case 47:
                                    if (WBreg[rs2Section] == rs1)
                                    {
                                        if (Forwarding)
                                        {
                                            A[0] = new BitVector32(LMD[0]);
                                            A[1] = new BitVector32(LMD[1]);
                                            forwarded = true;
                                        }
                                        else
                                            return false;
                                    }
                                    if (WBreg[rs2Section] == rs2)
                                    {
                                        if (Forwarding)
                                        {
                                            B[0] = new BitVector32(LMD[0]);
                                            B[1] = new BitVector32(LMD[1]);
                                            forwarded = true;
                                        }
                                        else
                                            return false;
                                    }
                                    break;
                                default:
                                    break; //Do nothing for non fp instructions.
                            }
                            break;
                        default:
                            switch (IDOpcode)
                            {
                                case 0: //R-type
                                    if (WBreg[rs2Section] != 0 && WBreg[rs2Section] == rs1)
                                    {
                                        if (Forwarding)
                                        {
                                            A[0] = new BitVector32(LMD[0]);
                                            A[1] = new BitVector32(LMD[1]);
                                            forwarded = true;
                                        }
                                        else
                                            return false;
                                    }
                                    if (WBreg[rs2Section] != 0 && WBreg[rs2Section] == rs2)
                                    {
                                        if (Forwarding)
                                        {
                                            B[0] = new BitVector32(LMD[0]);
                                            B[1] = new BitVector32(LMD[1]);
                                            forwarded = true;
                                        }
                                        else
                                            return false;
                                    }
                                    break;
                                case 1:
                                case > 31 and < 40:
                                case 46:
                                case 47:
                                    break; //Wont forward to fp operations or loads.
                                default: //I-type
                                    if (WBreg[rs2Section] != 0 && WBreg[rs2Section] == rs1)
                                    {
                                        if (Forwarding)
                                        {
                                            A[0] = new BitVector32(LMD[0]);
                                            A[1] = new BitVector32(LMD[1]);
                                            forwarded = true;
                                        }
                                        else
                                            return false;
                                    }
                                    break;
                            }
                            break;
                    }
                    switch (MEMreg[opSection]) //Forwards from MEM
                    {
                        case 2:
                        case 3:
                        case 16:
                        case 17:
                            break; //Do nothing for J types
                        case > 39:
                            if (IDOpcode is > 39)
                                return false;
                            break; //Do not prepare a store when storing.
                        case 0:
                            if (WBreg[functSection] != 0)
                            {
                                switch (IDOpcode)
                                {
                                    case 0: //R-type
                                        if (MEMreg[rdSection] != 0 && MEMreg[rdSection] == rs1)
                                        {
                                            if (forwarded)
                                            {
                                                ++WAWStalls;
                                                return false;
                                            }
                                            if (Forwarding)
                                            {
                                                A[0] = new BitVector32(ALUout[0]);
                                                A[1] = new BitVector32(ALUout[1]);
                                            }
                                            else
                                                return false;
                                        }
                                        if (MEMreg[rdSection] != 0 && MEMreg[rdSection] == rs2)
                                        {
                                            if (forwarded)
                                            {
                                                ++WAWStalls;
                                                return false;
                                            }
                                            if (Forwarding)
                                            {
                                                B[0] = new BitVector32(ALUout[0]);
                                                B[1] = new BitVector32(ALUout[1]);
                                            }
                                            else
                                                return false;
                                        }
                                        break;
                                    case 1:
                                    case > 31 and < 40:
                                    case 46:
                                    case 47:
                                        break; //Wont forward to fp operations or loads.
                                    default: //I-type
                                        if (MEMreg[rdSection] != 0 && MEMreg[rdSection] == rs1)
                                        {
                                            if (forwarded)
                                            {
                                                ++WAWStalls;
                                                return false;
                                            }
                                            if (Forwarding)
                                            {
                                                A[0] = new BitVector32(ALUout[0]);
                                                A[1] = new BitVector32(ALUout[1]);
                                            }
                                            else
                                                return false;
                                        }
                                        break;
                                }
                            }
                            break;
                        case 1:
                            switch (IDOpcode)
                            {
                                case 1:
                                    if (MEMreg[rdSection] == rs1)
                                    {
                                        if (forwarded)
                                        {
                                            ++WAWStalls;
                                            return false;
                                        }
                                        if (Forwarding)
                                        {
                                            A[0] = new BitVector32(ALUout[0]);
                                            A[1] = new BitVector32(ALUout[1]);
                                        }
                                        else
                                            return false;
                                    }
                                    if (MEMreg[rdSection] == rs2)
                                    {
                                        if (forwarded)
                                        {
                                            ++WAWStalls;
                                            return false;
                                        }
                                        if (Forwarding)
                                        {
                                            B[0] = new BitVector32(ALUout[0]);
                                            B[1] = new BitVector32(ALUout[1]);
                                        }
                                        else
                                            return false;
                                    }
                                    break;
                                case 46:
                                case 47:
                                    if (MEMreg[rs2Section] == rs1)
                                    {
                                        if (forwarded)
                                        {
                                            ++WAWStalls;
                                            return false;
                                        }
                                        if (Forwarding)
                                        {
                                            A[0] = new BitVector32(ALUout[0]);
                                            A[1] = new BitVector32(ALUout[1]);
                                        }
                                        else
                                            return false;
                                    }
                                    if (MEMreg[rs2Section] == rs2)
                                    {
                                        if (forwarded)
                                        {
                                            ++WAWStalls;
                                            return false;
                                        }
                                        if (Forwarding)
                                        {
                                            B[0] = new BitVector32(ALUout[0]);
                                            B[1] = new BitVector32(ALUout[1]);
                                        }
                                        else
                                            return false;
                                    }
                                    break;
                                default:
                                    break; //Do nothing for non fp instructions.
                            }
                            break;
                        case > 31 and < 40: //Do not forward loads.
                            switch (IDOpcode)
                            {
                                case 0:
                                case 1:
                                    if (MEMreg[rs2Section] != 0 && MEMreg[rs2Section] == rs1)
                                    {
                                        if (forwarded)
                                        {
                                            ++WAWStalls;
                                            return false;
                                        }
                                        return false;
                                    }
                                    if (MEMreg[rs2Section] != 0 && MEMreg[rs2Section] == rs2)
                                    {
                                        if (forwarded)
                                        {
                                            ++WAWStalls;
                                            return false;
                                        }
                                        return false;
                                    }
                                    break;
                                case > 31 and < 40:
                                    break; //Loads do not need to be forwarded any data.
                                default:

                                    if (MEMreg[rs2Section] != 0 && MEMreg[rs2Section] == rs1)
                                    {
                                        if (forwarded)
                                        {
                                            ++WAWStalls;
                                            return false;
                                        }
                                        return false;
                                    }
                                    break;
                            }
                            break;
                        default:
                            switch (IDOpcode)
                            {
                                case 0: //R-type
                                    if (MEMreg[rs2Section] != 0 && MEMreg[rs2Section] == rs1)
                                    {
                                        if (forwarded)
                                        {
                                            ++WAWStalls;
                                            return false;
                                        }
                                        if (Forwarding)
                                        {
                                            A[0] = new BitVector32(ALUout[0]);
                                            A[1] = new BitVector32(ALUout[1]);
                                        }
                                        else
                                            return false;
                                    }
                                    if (MEMreg[rs2Section] != 0 && MEMreg[rs2Section] == rs2)
                                    {
                                        if (forwarded)
                                        {
                                            ++WAWStalls;
                                            return false;
                                        }
                                        if (Forwarding)
                                        {
                                            B[0] = new BitVector32(ALUout[0]);
                                            B[1] = new BitVector32(ALUout[1]);
                                        }
                                        else
                                            return false;
                                    }
                                    break;
                                case 1:
                                case > 31 and < 40:
                                case 46:
                                case 47:
                                    break; //Do nothing for FP or loads.
                                default: //I-type
                                    if (MEMreg[rs2Section] != 0 && MEMreg[rs2Section] == rs1)
                                    {
                                        if (forwarded)
                                        {
                                            ++WAWStalls;
                                            return false;
                                        }
                                        if (Forwarding)
                                        {
                                            A[0] = new BitVector32(ALUout[0]);
                                            A[1] = new BitVector32(ALUout[1]);
                                        }
                                        else
                                            return false;
                                    }
                                    break;
                            }
                            break;
                    }
                }
            }
            switch (IDreg[opSection]) //Set the condition for jumps based on the instruction.
            {
                case 2: //JUMPS AND BRANCHES
                    Condition = true;
                    break;
                case 3:
                    Condition = true;
                    break;
                case 4:
                     Condition = A[0].Data != 0;
                    if (Condition)
                        JumpsTaken++;
                    else
                        JumpsNotTaken++;
                    break;
                case 5:
                    Condition = A[0].Data != 0;
                    if (Condition)
                        JumpsTaken++;
                    else
                        JumpsNotTaken++;
                    break;
                case 6:
                    if (comparingFP)
                        return false;
                    Condition = FPstatus.Data == 1;
                    if (Condition)
                        JumpsTaken++;
                    else
                        JumpsNotTaken++;
                    break;
                case 7:
                    if (comparingFP)
                        return false;
                    Condition = FPstatus.Data == 0;
                    if (Condition)
                        JumpsTaken++;
                    else
                        JumpsNotTaken++;
                    break;
                case 16: //RFE
                         //UNIMPLEMENTED
                    break;
                case 17:
                    //trap0Found = true;
                    break;
                case 18: //JR
                         //UNIMPLEMENTED
                    break;
                case 19:
                    Condition = true;
                    break;
                case 40: //Prepare data in SDR to save
                case 41:
                case 43:
                case 46:
                    SDR[0] = new BitVector32(A[0].Data);
                    SDR[1] = new BitVector32(0); //Ensure its a single register
                    break;
                case 47:
                    SDR[0] = new BitVector32(A[0].Data);
                    SDR[1] = new BitVector32(A[1].Data);
                    break;
            }
            return true;
        }
        
        private bool ExecuteInstruction()
        {
            bool success = true;
            BitVector32[] arr = new BitVector32[2]; //Aux vector
            List<byte> aux = new List<byte>();
            switch (EXreg[opSection])
            {
                case 1:
                    switch (EXreg[functSection])
                    {
                        case 8: //CVTF2D
                            foreach (FPU fpu in adds)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(EXreg[rdSection], BitConverter.Int32BitsToSingle(fRegisters[EXreg[rs1Section]].Data), 0, 0, 1);
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;
                        case 9: //CVTF2I
                            foreach (ALU alu in alus)
                            {
                                if (!alu.Busy)
                                {
                                    alu.LoadValues(EXreg[rdSection], BitConverter.SingleToInt32Bits(fRegisters[EXreg[rs1Section]].Data), 0, 8);
                                    break;
                                }
                                if (alus.Last() == alu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                }
                            }
                            break;
                        case 10: //CVTD2F
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(fRegisters[EXreg[rs1Section]].Data));
                            aux.AddRange(BitConverter.GetBytes(fRegisters[EXreg[rs1Section] - 1].Data));
                            foreach (FPU fpu in adds)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(EXreg[rdSection], BitConverter.SingleToInt32Bits(BitConverter.ToInt32(aux.ToArray(), 0)), 0, 0, 1);
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;
                        case 11: //CVTD2I
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(fRegisters[EXreg[rs1Section]].Data));
                            aux.AddRange(BitConverter.GetBytes(fRegisters[EXreg[rs1Section] - 1].Data));
                            foreach (ALU alu in alus)
                            {
                                if (!alu.Busy)
                                {
                                    alu.LoadValues(EXreg[rdSection], BitConverter.SingleToInt32Bits(BitConverter.ToInt32(aux.ToArray(), 0)), 0, 8);
                                    break;
                                }
                                if (alus.Last() == alu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                }
                            }
                            break;
                        case 12: //CVTI2F
                        case 13: //CVTI2D
                            foreach (FPU fpu in adds)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(EXreg[rdSection], Registers[EXreg[rs1Section]].Data, 0, 0, 1);
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;
                        case 0: //ADDF
                        case 1: //SUBF
                            foreach (FPU fpu in adds)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(EXreg[rdSection], BitConverter.Int32BitsToSingle(fRegisters[EXreg[rs1Section]].Data), BitConverter.Int32BitsToSingle(fRegisters[EXreg[rs2Section]].Data), EXreg[functSection], ADDDelay);
                                    fpAddCount++;
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;
                        case 4: //ADDD
                        case 5: //SUBD
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(A[0].Data));
                            aux.AddRange(BitConverter.GetBytes(A[1].Data));
                            aux.AddRange(BitConverter.GetBytes(B[0].Data));
                            aux.AddRange(BitConverter.GetBytes(B[1].Data));
                            foreach (FPU fpu in adds)
                            {
                                if (!fpu.Busy)
                                {
                                    byte[] auxArr = aux.ToArray();
                                    fpu.LoadValues(EXreg[rdSection], BitConverter.ToDouble(auxArr, 0), BitConverter.ToDouble(auxArr, 8), EXreg[functSection], ADDDelay);
                                    fpAddCount++;
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;
                        case 2: //MULTF
                            foreach (FPU fpu in muls)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(EXreg[rdSection], BitConverter.Int32BitsToSingle(A[0].Data), BitConverter.Int32BitsToSingle(B[1].Data), EXreg[functSection], MULDelay);
                                    fpMulCount++;
                                    break;
                                }
                                if (muls.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;
                        case 3: //DIVF
                            foreach (FPU fpu in divs)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(EXreg[rdSection], BitConverter.Int32BitsToSingle(A[0].Data), BitConverter.Int32BitsToSingle(B[1].Data), EXreg[functSection], DIVDelay);
                                    fpMulCount++;
                                    break;
                                }
                                if (divs.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;
                        case 6: //MULTD
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(A[0].Data));
                            aux.AddRange(BitConverter.GetBytes(A[1].Data));
                            aux.AddRange(BitConverter.GetBytes(B[0].Data));
                            aux.AddRange(BitConverter.GetBytes(B[1].Data));
                            foreach (FPU fpu in muls)
                            {
                                if (!fpu.Busy)
                                {
                                    byte[] auxArr = aux.ToArray();
                                    fpu.LoadValues(EXreg[rdSection], BitConverter.ToDouble(auxArr, 0), BitConverter.ToDouble(auxArr, 8), EXreg[functSection], MULDelay);
                                    fpMulCount++;
                                    break;
                                }
                                if (muls.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;
                        case 7: //DIVD
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(A[0].Data));
                            aux.AddRange(BitConverter.GetBytes(A[1].Data));
                            aux.AddRange(BitConverter.GetBytes(B[0].Data));
                            aux.AddRange(BitConverter.GetBytes(B[1].Data));
                            foreach (FPU fpu in divs)
                            {
                                if (!fpu.Busy)
                                {
                                    byte[] auxArr = aux.ToArray();
                                    fpu.LoadValues(EXreg[rdSection], BitConverter.ToDouble(auxArr, 0), BitConverter.ToDouble(auxArr, 8), EXreg[functSection], DIVDelay);
                                    fpDivCount++;
                                    break;
                                }
                                if (divs.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;
                        case 14: //MULT
                        case 22: //MULTU
                            foreach (FPU fpu in muls)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(EXreg[rdSection], BitConverter.Int32BitsToSingle(A[0].Data), BitConverter.Int32BitsToSingle(B[1].Data), EXreg[functSection], MULDelay);
                                    fpMulCount++;
                                    break;
                                }
                                if (muls.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;
                        case 15: //DIV
                        case 23: //DIVU
                            foreach (FPU fpu in divs)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(EXreg[rdSection], BitConverter.Int32BitsToSingle(A[0].Data), BitConverter.Int32BitsToSingle(B[1].Data), EXreg[functSection], DIVDelay);
                                    fpDivCount++;
                                    break;
                                }
                                if (divs.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
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
                                    fpu.LoadValues(33, BitConverter.Int32BitsToSingle(A[0].Data), BitConverter.Int32BitsToSingle(B[0].Data), EXreg[functSection], 1);
                                    comparingFP = true;
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;
                        default: //LOGIC OPERATIONS DOUBLES
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(fRegisters[EXreg[rs1Section]].Data));
                            aux.AddRange(BitConverter.GetBytes(fRegisters[EXreg[rs1Section] + 1].Data));
                            aux.AddRange(BitConverter.GetBytes(fRegisters[EXreg[rs2Section]].Data));
                            aux.AddRange(BitConverter.GetBytes(fRegisters[EXreg[rs2Section] + 1].Data));
                            foreach (FPU fpu in adds)
                            {
                                if (!fpu.Busy)
                                {
                                    byte[] auxArr = aux.ToArray();
                                    fpu.LoadValues(33, BitConverter.ToDouble(auxArr, 0), BitConverter.ToDouble(auxArr, 8), EXreg[functSection], 1);
                                    comparingFP = true;
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;

                    }
                    break;
                case 0:
                    switch (EXreg[functSection])
                    {
                        case 0: //NOP
                            UsedRegisters[EXreg[rdSection]] = 0; //solves a bug with register being marked as used with an incorrect instruction.
                            break;
                        case 48: //MOVI2S
                            //UNIMPLEMENTED
                            break;
                        case 49: //MOVS2I
                            //UNIMPLEMENTED
                            break;
                        case 50: //MOVF
                            foreach (FPU fpu in adds)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(EXreg[rdSection], fRegisters[EXreg[rs1Section]].Data, 0, 0, 1);
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;
                        case 51: //MOVD
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(fRegisters[EXreg[rs1Section]].Data));
                            aux.AddRange(BitConverter.GetBytes(fRegisters[EXreg[rs1Section] + 1].Data));
                            foreach (FPU fpu in adds)
                            {
                                if (!fpu.Busy)
                                {
                                    byte[] auxArr = aux.ToArray();
                                    fpu.LoadValues(EXreg[rdSection], BitConverter.ToDouble(auxArr, 0), 0, 0, 1);
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;
                        case 52: //MOVFP2I
                            foreach (ALU alu in alus)
                            {
                                if (!alu.Busy)
                                {
                                    alu.LoadValues(EXreg[rdSection], fRegisters[EXreg[rs1Section]].Data, 0, 8);
                                    break;
                                }
                                if (alus.Last() == alu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                }
                            }
                            break;
                        case 53: //MOVI2FP
                            foreach (FPU fpu in adds)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues(EXreg[rdSection], Registers[EXreg[rs1Section]].Data, 0, 0, 1);
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                    ++fpStalls;
                                }
                            }
                            break;
                        default: //ALU OPERATIONS
                            foreach (ALU alu in alus)
                            {
                                if (!alu.Busy)
                                {
                                    alu.LoadValues(EXreg[rdSection], Registers[EXreg[rs1Section]].Data, Registers[EXreg[rs2Section]].Data + EXreg[shamtSection], EXreg[functSection]);
                                    break;
                                }
                                if (alus.Last() == alu)
                                {
                                    ++StructuralStalls;
                                    success = false;
                                }
                            }
                            break;
                    }
                    break;
                case 2: //JUMPS AND BRANCHES
                    BitVector32 auxReg = new BitVector32(EXreg);
                    auxReg[opSection] = 0;
                    foreach (ALU alu in alus)
                    {
                        if (!alu.Busy)
                        {
                            alu.LoadValues(0, auxReg.Data, 0, 8);
                            break;
                        }
                        if (alus.Last() == alu)
                        {
                            ++StructuralStalls;
                            success = false;
                        }
                    }
                    break;
                case 3:
                    Registers[31] = new BitVector32((int)(PC));
                    auxReg = new BitVector32(EXreg);
                    auxReg[opSection] = 0;
                    foreach (ALU alu in alus)
                    {
                        if (!alu.Busy)
                        {
                            alu.LoadValues(0, auxReg.Data, 0, 8);
                            break;
                        }
                        if (alus.Last() == alu)
                        {
                            ++StructuralStalls;
                            success = false;
                        }
                    }
                    break;
                case 4:
                case 5:
                case 6:
                case 7:
                    foreach (ALU alu in alus)
                    {
                        if (!alu.Busy)
                        {
                            alu.LoadValues(0, EXreg[addressSection], Registers[EXreg[rs1Section]].Data, 8);
                            break;
                        }
                        if (alus.Last() == alu)
                        {
                            ++StructuralStalls;
                            success = false;
                        }
                    }
                    break;
                case 16: //RFE
                    //UNIMPLEMENTED
                    break;
                case 17:
                    trap0Found = true;//Found trap 0. will be processed in WB.
                    break;
                case 18: //JR
                    //UNIMPLEMENTED
                    break;
                case 19:
                    Registers[EXreg[rs1Section]] = new BitVector32((int)(PC));
                    foreach (ALU alu in alus)
                    {
                        if (!alu.Busy)
                        {
                            alu.LoadValues(0, EXreg[addressSection], 0, 8);
                            break;
                        }
                        if (alus.Last() == alu)
                        {
                            ++StructuralStalls;
                            success = false;
                        }
                    }
                    break;
                default: //ALU I OPERATIONS
                    foreach (ALU alu in alus)
                    {
                        if (!alu.Busy)
                        {
                            UsedRegisters[EXreg[rs2Section]] = 1; //Mark the register as being used.
                            alu.LoadValues(EXreg[rs2Section], Registers[EXreg[rs1Section]].Data, EXreg[addressSection], EXreg[opSection]);
                            break;
                        }
                        if (alus.Last() == alu)
                        {
                            ++StructuralStalls;
                            success = false;
                        }
                    }
                    break;
                case 32: //LOADS
                case 33:
                case 35:
                case 36:
                case 37:
                case 38:
                case 39:
                    foreach (ALU alu in alus)
                    {
                        if (!alu.Busy)
                        {
                            UsedRegisters[EXreg[rs2Section]] = 1; //Mark the register as being used.
                            alu.LoadValues(33, Registers[EXreg[rs1Section]].Data, EXreg[addressSection], 8);
                            break;
                        }
                        if (alus.Last() == alu)
                        {
                            ++StructuralStalls;
                            success = false;
                        }
                    }
                    break;
                case 40: //STORES
                case 41:
                case 43:
                case 46:
                case 47:
                    foreach (ALU alu in alus)
                    {
                        if (!alu.Busy)
                        {
                            UsedRegisters[EXreg[rs2Section]] = 1; //Mark the register as being used.
                            alu.LoadValues(33, Registers[EXreg[rs1Section]].Data, EXreg[addressSection], 8);
                            break;
                        }
                        if (alus.Last() == alu)
                        {
                            ++StructuralStalls;
                            success = false;
                        }
                    }
                    break;
            }
            if (success)
            {
                OPreg = EXreg; //Do not pass a nop to next stage.
                EXreg = new BitVector32(0); //IF the instruction was loaded correctly, the ALUreg should contain the instruction and the EXreg must be cleared to avoid loading the same instruction while stalling.
            }
            return success;
        }
    }
}
