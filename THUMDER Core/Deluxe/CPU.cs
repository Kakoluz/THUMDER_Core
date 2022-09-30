using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace THUMDER.Deluxe
{
    internal sealed partial class SimManager
    {
        private BitVector32 A = new BitVector32(0);
        private BitVector32[] Afp = new BitVector32[2];
        private BitVector32 B = new BitVector32(0);
        private BitVector32[] Bfp = new BitVector32[2];
        private BitVector32 Imm = new BitVector32(0);
        private bool comparingFP = false;
        /// <summary>
        /// Reads the PC address and fetches de data on that memory address.
        /// </summary>
        private void IF()
        {
            if (!RStall)
            {
                IMAR = new BitVector32((int)PC);
                IFreg = MemoryManager.Instance.ReadWordAsBitVector((uint)IMAR.Data);
                PC += 4;
            }
        }

        /// <summary>
        /// Decodes the instruction to execute.
        /// </summary>
        private void ID()
        {
            if (!RStall)
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
                            trap0Found = true;
                            break;
                        default: //Immediate numbers
                            this.address = IDreg[addressSection]; //Place the immediate number in rs2 to operate it.
                            this.rd = IDreg[rs2Section]; //For I-type rd is placed in the 5 bits of rs2.
                            this.rs1 = IDreg[rs1Section];
                            break;
                    }
                }
                switch (IDreg[opSection]) //Set the condition for jumps based on the isntruction on mem.
                {
                    case 2: //JUMPS AND BRANCHES
                        Condition = true;
                        break;
                    case 3:
                        Condition = true;
                        break;
                    case 4:
                        Condition = Registers[rs1].Data == 0;
                        break;
                    case 5:
                        Condition = Registers[rs1].Data != 0;
                        break;
                    case 6:
                        Condition = FPstatus.Data == 1;
                        break;
                    case 7:
                        Condition = FPstatus.Data == 0;
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
                }
            }
            this.LoadInstruction();
        }
        
        /// <summary>
        /// Executes the instruction.
        /// </summary>
        private void EX()
        {
            TickAllUnits();
            RStall = !ExecuteInstruction();
            if (PedingMemAccess.Count <= 1)
                PedingMemAccess.Add(null); //Put a null memory access to have it indicate that there is no pending memory access and next insctruction can be taken.
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
                            LMD[0] = new BitVector32((sbyte)MemoryManager.Instance.ReadByte(wb.Address));
                            break;
                        case "UBYTE":
                            LMD[0] = new BitVector32(MemoryManager.Instance.ReadByte(wb.Address));
                            break;
                        case "HALF":
                        case "UHALF":
                            LMD[0] = new BitVector32(MemoryManager.Instance.ReadHalf((wb.Address)));
                            break;
                        case "UWORD":
                        case "WORD":
                            LMD[0] = new BitVector32(MemoryManager.Instance.ReadWordAsBitVector(wb.Address));
                            break;
                        case "FLOAT":
                            LMD[0] = new BitVector32(MemoryManager.Instance.ReadFloatAsBitVector(wb.Address));
                            break;
                        case "DOUBLE":
                            byte[] value = BitConverter.GetBytes(MemoryManager.Instance.ReadDouble(wb.Address));
                            LMD[0] = new BitVector32(BitConverter.ToInt32(value, 0));
                            LMD[1] = new BitVector32(BitConverter.ToInt32(value, 4));
                            break;
                    }
                }
            }
            if (Condition)
            {
                PC = (uint)ALUout.Data;
                RStall = false;
                ClearPipeline();
                Condition = false;
            }
            PedingMemAccess.RemoveAt(0);
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
                        Registers[WBreg[rdSection]] = ALUout;
                        UsedRegisters[WBreg[rdSection]] = 0;
                    }
                    break;
                case 17:
                    trap0Found = true;
                    break;
                case > 7 and < 16: //Wirte Reg-imm
                case > 19 and < 32:
                    Registers[WBreg[rs2Section]] = ALUout;
                    UsedRegisters[WBreg[rs2Section]] = 0;
                    break;
                case 1: //Wirte Reg-Reg fp
                    if (WBreg[functSection] > 15 && WBreg[functSection] is 22 or 23)
                    {
                        fRegisters[WBreg[rdSection]] = FPUout[0];
                        UsedRegisters[WBreg[rdSection]] = 0;
                        if (FPUout[1].Data != 0)
                            fRegisters[WBreg[rdSection] + 1] = FPUout[1];
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
                    fRegisters[WBreg[rs2Section] - 1] = LMD[1];
                    break;
            }
        }

        /// <summary>
        /// Clears the execution pipeline after a jump
        /// </summary>
        private void ClearPipeline()
        {
            IDreg = zeroBits; //Clear the execution pipeline\
            IFreg = zeroBits;
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
                        ALUout = new BitVector32((int)output);
                    }
                    break; //Unload only 1 unit per cycle.
                }
            }
            foreach (FPU a in adds)
            {
                if (a.Done)
                {
                    int dest; //Need it to easily know if it is a bool value.
                    double? output = a.GetValue(out dest, out _);
                    if (output != null && dest < 33)
                    {
                        byte[] outBytes = BitConverter.GetBytes((double)output);
                        BitVector32[] aux = new BitVector32[2];
                        aux[0] = new BitVector32(BitConverter.ToInt32(outBytes, 0));
                        aux[1] = new BitVector32(BitConverter.ToInt32(outBytes, 4));
                        FPUout = aux;
                    }
                    else if (dest >= 33)
                    {
                        FPstatus = new BitVector32((int)output);
                        comparingFP = false;
                    }
                    return; //Unload only 1 unit per cycle.
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
                        FPUout = aux;
                    }
                    return; //Unload only 1 unit per cycle.
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
                        FPUout = aux;
                    }
                    return; //Unload only 1 unit per cycle.
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
            UnloadUnits();
        }

        /// <summary>
        /// Loads the instruction into an EX unit.
        /// </summary>
        /// <returns>If the instruction was corretly loaded.</returns>
        private bool LoadInstruction()
        {
            
            A = new BitVector32(0);
            Afp = new BitVector32[2];
            B = new BitVector32(0);
            Bfp = new BitVector32[2];
            Imm = new BitVector32(address);
            if (rs1 < 33)
            {
                A = new BitVector32(Registers[rs1].Data);
                Afp[0] = new BitVector32(fRegisters[rs1].Data);
                Afp[1] = new BitVector32(fRegisters[rs1].Data - 1);
            }
            if (rs2 < 33)
            {
                B = new BitVector32(Registers[rs2].Data);
                Bfp[0] = new BitVector32(fRegisters[rs2].Data);
                Bfp[1] = new BitVector32(fRegisters[rs2].Data - 1);
            }
            if (shamt != 0)
            {
                B = new BitVector32(MemoryManager.Instance.ReadWord((uint)(rs2 + shamt)));
            }
            if ((IDOpcode is 1 or 0) || (IDOpcode > 7 && IDOpcode < 40)) //first check the instruction if it will use registers.
            {
                if (!(IDOpcode is 0 && funct is 0)) //Check if its a nop.
                {
                    if (MEMreg.Data != zeroBits.Data && (MEMreg[rdSection] == rs1 || MEMreg[rdSection] == rs2)) //Check if we are using data on its way.
                    {
                        if (MEMreg[opSection] is 0 || MEMreg[opSection] is > 7 and < 32) //Check de instruction in MEM stage to forward data from. (this one just got out of the EX unit)
                        {
                            if (MEMreg[rdSection] != 0 && (MEMreg[rdSection] == rs1) && rs1 != 0)
                            {
                                if (Forwarding)
                                    A = ALUout;
                                else
                                    return false;
                            }
                            else if (MEMreg[rdSection] != 0 && (MEMreg[rdSection] == rs2) && rs2 != 0)
                            {
                                if (Forwarding)
                                    B = ALUout;
                                else
                                    return false;
                            }
                        }
                        else if (MEMreg[opSection] == 1) //Same check but for fp instructions.
                        {
                            if (MEMreg[rdSection] == rs1)
                            {
                                if (Forwarding)
                                    Afp = FPUout;
                                else
                                    return false;
                            }
                            else if (MEMreg[rdSection] == rs2)
                            {
                                if (Forwarding)
                                    Bfp = FPUout;
                                else
                                    return false;
                            }
                        }
                        else if (MEMreg[opSection] is > 32 and < 40)
                        {
                            if (IDOpcode != 1 && MEMreg[opSection] < 38)
                                return false;
                            else if (IDOpcode == 1 && MEMreg[opSection] is 38 or 39)
                                return false;
                        }
                    }
                    /*else if (WBreg.Data != zeroBits.Data && (WBreg[rdSection] == rs1 || WBreg[rdSection] == rs2)) //Check if we are using ready to be written to registers.
                    {
                        if (WBreg[opSection] is 0 || WBreg[opSection] is > 7 and < 32) //Check de instruction in MEM stage to forward data from. (this one just got out of the EX unit)
                        {
                            if (WBreg[rdSection] != 0 && (WBreg[rdSection] == rs1) && rs1 != 0)
                            {
                                if (Forwarding)
                                    A = ALUout;
                                else
                                    return false;
                            }
                            else if (WBreg[rdSection] != 0 && (WBreg[rdSection] == rs2) && rs2 != 0)
                            {
                                if (Forwarding)
                                    B = ALUout;
                                else
                                    return false;
                            }
                        }
                        else if (WBreg[opSection] is > 31 and < 38) //Check if we are loading from memory
                        {
                            if (WBreg[rs2Section] == rs1 && rs1 != 0)
                            {
                                if (Forwarding)
                                    A = LMD[0];
                                else
                                    return false;
                            }
                            else if (WBreg[rs2Section] == rs2 && rs2 != 0)
                            {
                                if (Forwarding)
                                    B = LMD[0];
                                else
                                    return false;
                            }
                        }
                        else if (WBreg[opSection] == 1) //Same check but for fp instructions.
                        {
                            if (WBreg[rdSection] == rs1)
                            {
                                if (Forwarding)
                                    Afp = FPUout;
                                else
                                    return false;
                            }
                            else if (WBreg[rdSection] == rs2)
                            {
                                if (Forwarding)
                                    Bfp = FPUout;
                                else
                                    return false;
                            }
                        }
                        else if (WBreg[opSection] is 38 or 39) //Check if we are loading a floatin point number.
                        {
                            if (WBreg[rs2Section] == rs1)
                            {
                                if (Forwarding)
                                    Afp = LMD;
                                else
                                    return false;
                            }
                            else if (WBreg[rs2Section] == rs2)
                            {
                                if (Forwarding)
                                    Bfp = LMD;
                                else
                                    return false;
                            }
                        }
                    }*/
                }
            }
            return true;
        }
        
        private bool ExecuteInstruction()
        {
            bool success = true;
            BitVector32[] arr = new BitVector32[2]; //Aux vector
            List<byte> aux = new List<byte>();
            if ((IDOpcode is 1 or 0) || (IDOpcode > 7 && IDOpcode < 40)) //first check the instruction if it will use registers.
            {
                if (!(IDOpcode is 0 && funct is 0)) //Check if its a nop.
                {
                    if (WBreg.Data != zeroBits.Data && (WBreg[rdSection] == rs1 || WBreg[rdSection] == rs2)) //Check if we are using ready to be written to registers.
                    {
                        if (WBreg[opSection] is 0 || WBreg[opSection] is > 7 and < 32) //Check de instruction in WB stage to forward data from. (this one just got out of the MEM unit)
                        {
                            if (WBreg[rdSection] != 0 && (WBreg[rdSection] == rs1) && rs1 != 0)
                            {
                                if (Forwarding)
                                    A = ALUout;
                                else
                                    return false;
                            }
                            else if (WBreg[rdSection] != 0 && (WBreg[rdSection] == rs2) && rs2 != 0)
                            {
                                if (Forwarding)
                                    B = ALUout;
                                else
                                    return false;
                            }
                        }
                        else if (WBreg[opSection] is > 31 and < 38) //Check if we are loading from memory
                        {
                            if (WBreg[rs2Section] == rs1 && rs1 != 0)
                            {
                                if (Forwarding)
                                    A = LMD[0];
                                else
                                    return false;
                            }
                            else if (WBreg[rs2Section] == rs2 && rs2 != 0)
                            {
                                if (Forwarding)
                                    B = LMD[0];
                                else
                                    return false;
                            }
                        }
                        else if (WBreg[opSection] == 1) //Same check but for fp instructions.
                        {
                            if (WBreg[rdSection] == rs1)
                            {
                                if (Forwarding)
                                    Afp = FPUout;
                                else
                                    return false;
                            }
                            else if (WBreg[rdSection] == rs2)
                            {
                                if (Forwarding)
                                    Bfp = FPUout;
                                else
                                    return false;
                            }
                        }
                        else if (WBreg[opSection] is 38 or 39) //Check if we are loading a floatin point number.
                        {
                            if (WBreg[rs2Section] == rs1)
                            {
                                if (Forwarding)
                                    Afp = LMD;
                                else
                                    return false;
                            }
                            else if (WBreg[rs2Section] == rs2)
                            {
                                if (Forwarding)
                                    Bfp = LMD;
                                else
                                    return false;
                            }
                        }
                    }
                }
            }
            switch (IDOpcode)
            {
                case 1:
                    switch (funct)
                    {
                        case 8: //CVTF2D
                            arr[0] = new BitVector32(fRegisters[rs1]);
                            this.FPUout = arr;
                            break;
                        case 9: //CVTF2I
                            this.ALUout= fRegisters[rs1];
                            break;
                        case 10: //CVTD2F
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(fRegisters[rs1].Data));
                            aux.AddRange(BitConverter.GetBytes(fRegisters[rs1 + 1].Data));
                            arr[0] = new BitVector32(BitConverter.ToInt32(aux.ToArray(), 0));
                            this.FPUout = arr;
                            break;
                        case 11: //CVTD2I
                            aux.Clear();
                            aux.AddRange(BitConverter.GetBytes(fRegisters[rs1].Data));
                            aux.AddRange(BitConverter.GetBytes(fRegisters[rs1 + 1].Data));
                            this.ALUout = new BitVector32(BitConverter.ToInt32(aux.ToArray()));
                            break;
                        case 12: //CVTI2F
                            arr[0] = new BitVector32(Registers[rs1]);
                            this.FPUout = arr;
                            break;
                        case 13: //CVTI2D
                            arr[0] = new BitVector32(fRegisters[rs1]);
                            this.FPUout= arr;
                            break;
                        case 0: //ADDF
                        case 1: //SUBF
                            foreach (FPU fpu in adds)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues((int)rd, BitConverter.Int32BitsToSingle(fRegisters[(int)rs1].Data), BitConverter.Int32BitsToSingle(fRegisters[(int)rs2].Data), (int)funct, ADDDelay);
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
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
                                    fpu.LoadValues((int)rd, BitConverter.ToDouble(auxArr, 0), BitConverter.ToDouble(auxArr, 8), (int)funct, ADDDelay);
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
                                    success = false;
                                }
                            }
                            break;
                        case 2: //MULTF
                            foreach (FPU fpu in muls)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues((int)rd, BitConverter.Int32BitsToSingle(Afp[0].Data), BitConverter.Int32BitsToSingle(Bfp[1].Data), (int)funct, MULDDelay);
                                    break;
                                }
                                if (muls.Last() == fpu)
                                {
                                    success = false;
                                }
                            }
                            break;
                        case 3: //DIVF
                            foreach (FPU fpu in divs)
                            {
                                if (!fpu.Busy)
                                {
                                    fpu.LoadValues((int)rd, BitConverter.Int32BitsToSingle(Afp[0].Data), BitConverter.Int32BitsToSingle(Bfp[1].Data), (int)funct, DIVDelay);
                                    break;
                                }
                                if (divs.Last() == fpu)
                                {
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
                                    fpu.LoadValues((int)rd, BitConverter.ToDouble(auxArr, 0), BitConverter.ToDouble(auxArr, 8), (int)funct, MULDDelay);
                                    break;
                                }
                                if (muls.Last() == fpu)
                                {
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
                                    fpu.LoadValues((int)rd, BitConverter.ToDouble(auxArr, 0), BitConverter.ToDouble(auxArr, 8), (int)funct, DIVDelay);
                                    break;
                                }
                                if (divs.Last() == fpu)
                                {
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
                                    fpu.LoadValues((int)rd, BitConverter.Int32BitsToSingle(Afp[0].Data), BitConverter.Int32BitsToSingle(Bfp[1].Data), (int)funct, MULDDelay);
                                    break;
                                }
                                if (muls.Last() == fpu)
                                {
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
                                    fpu.LoadValues((int)rd, BitConverter.Int32BitsToSingle(Afp[0].Data), BitConverter.Int32BitsToSingle(Bfp[1].Data), (int)funct, DIVDelay);
                                    break;
                                }
                                if (divs.Last() == fpu)
                                {
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
                                    fpu.LoadValues(33, BitConverter.Int32BitsToSingle(Afp[0].Data), BitConverter.Int32BitsToSingle(Bfp[1].Data), (int)funct, 1);
                                    comparingFP = true;
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
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
                                    fpu.LoadValues(33, BitConverter.ToDouble(auxArr, 0), BitConverter.ToDouble(auxArr, 8), (int)funct, 1);
                                    comparingFP = true;
                                    break;
                                }
                                if (adds.Last() == fpu)
                                {
                                    success = false;
                                }
                            }
                            break;

                    }
                    break;
                case 0:
                    switch (funct)
                    {
                        case 0: //NOP
                            UsedRegisters[(int)rd] = 0; //solves a bug with register being marked as used with an incorrect instruction.
                            break;
                        case 48: //MOVI2S
                            //UNIMPLEMENTED
                            break;
                        case 49: //MOVS2I
                            //UNIMPLEMENTED
                            break;
                        case 50: //MOVF
                            arr[0] = new BitVector32(fRegisters[(int)rs1]);
                            this.FPUout = arr;
                            break;
                        case 51: //MOVD
                            arr[0] = new BitVector32(fRegisters[(int)rs1]);
                            arr[1] = new BitVector32(fRegisters[(int)rs1 - 1]);
                            this.FPUout = arr;
                            break;
                        case 52: //MOVFP2I
                            this.ALUout = fRegisters[(int)rs1];
                            break;
                        case 53: //MOVI2FP
                            arr[0] = new BitVector32(Registers[(int)rs1]);
                            this.FPUout= arr;
                            break;
                        default: //ALU OPERATIONS
                            foreach (ALU alu in alus)
                            {
                                if (!alu.Busy)
                                {
                                    alu.LoadValues((int)rd, A.Data, B.Data, (int)funct);
                                    break;
                                }
                                if (alus.Last() == alu)
                                {
                                    success = false;
                                }
                            }
                            break;
                    }
                    break;
                case 2: //JUMPS AND BRANCHES
                    ALUout = new BitVector32(address);
                    break;
                case 3:
                    Registers[31] = new BitVector32((int)(PC));
                    ALUout = new BitVector32(address);
                    break;
                case 4:
                    ALUout = new BitVector32((int)(address));
                    break;
                case 5:
                    ALUout = new BitVector32((int)(address));
                    break;
                case 6:
                    ALUout = new BitVector32((int)(address));
                    break;
                case 7:
                    ALUout = new BitVector32((int)(address));
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
                    Registers[rs1] = new BitVector32((int)(PC));
                    ALUout = new BitVector32((int)(address));
                    break;
                default: //ALU I OPERATIONS
                    foreach (ALU alu in alus)
                    {
                        if (!alu.Busy)
                        {
                            UsedRegisters[(int)rd] = 1; //Mark the register as being used.
                            alu.LoadValues((int)rd, A.Data, Imm.Data, IDOpcode);
                            break;
                        }
                        if (alus.Last() == alu)
                        {
                            success = false;
                        }
                    }
                    break;
                case 32: //LOADS
                    UsedRegisters[(int)rd] = 1; //Mark the register as being used.
                    this.PedingMemAccess.Add(new MemAccess((int)rd, (uint)(A.Data + address), MemAccessTypes.BYTE));
                    break;
                case 33:
                    UsedRegisters[(int)rd] = 1; //Mark the register as being used.
                    this.PedingMemAccess.Add(new MemAccess((int)rd, (uint)(A.Data + address), MemAccessTypes.HALF));
                    break;
                case 35:
                    UsedRegisters[(int)rd] = 1; //Mark the register as being used.
                    this.PedingMemAccess.Add(new MemAccess((int)rd, (uint)(A.Data + address), MemAccessTypes.WORD));
                    break;
                case 36:
                    UsedRegisters[(int)rd] = 1; //Mark the register as being used.
                    this.PedingMemAccess.Add(new MemAccess((int)rd, (uint)(A.Data + address), MemAccessTypes.UBYTE));
                    break;
                case 37:
                    UsedRegisters[(int)rd] = 1; //Mark the register as being used.
                    this.PedingMemAccess.Add(new MemAccess((int)rd, (uint)(A.Data + address), MemAccessTypes.UHALF));
                    break;
                case 38:
                    UsedfRegisters[(int)rd] = 1; //Mark the register as being used.
                    this.PedingMemAccess.Add(new MemAccess((int)rd, (uint)(A.Data + address), MemAccessTypes.FLOAT));
                    break;
                case 39:
                    UsedfRegisters[(int)rd] = 1; //Mark the register as being used.
                    this.PedingMemAccess.Add(new MemAccess((int)rd, (uint)(A.Data + address), MemAccessTypes.DOUBLE));
                    break;
                case 40: //STORES
                    this.PedingMemAccess.Add(new MemAccess((uint)(rd + address), BitConverter.GetBytes(Registers[(int)rs1].Data), MemAccessTypes.BYTE));
                    break;
                case 41:
                    this.PedingMemAccess.Add(new MemAccess((uint)(rd + address), BitConverter.GetBytes(Registers[(int)rs1].Data), MemAccessTypes.HALF));
                    break;
                case 43:
                    this.PedingMemAccess.Add(new MemAccess((uint)(rd + address), BitConverter.GetBytes(Registers[(int)rs1].Data), MemAccessTypes.WORD));
                    break;
                case 46:
                    this.PedingMemAccess.Add(new MemAccess((uint)(rd + address), BitConverter.GetBytes(fRegisters[(int)rs1].Data), MemAccessTypes.FLOAT));
                    break;
                case 47:
                    aux.Clear();
                    aux.AddRange(BitConverter.GetBytes(Afp[0].Data));
                    aux.AddRange(BitConverter.GetBytes(Afp[1].Data));
                    this.PedingMemAccess.Add(new MemAccess((uint)(rs2 + address), aux.ToArray(), MemAccessTypes.DOUBLE));
                    break;
            }
            return success;
        }
    }
}
