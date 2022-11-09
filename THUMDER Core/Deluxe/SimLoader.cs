using System.Collections.Specialized;
using System.Text;
using THUMDER.Interpreter;

namespace THUMDER.Deluxe
{
    internal sealed partial class SimManager
    {
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
                if (assembly.Labels.ContainsKey(i + 1))
                {
                    labels.Add(assembly.Labels[i + 1], (uint)(dataLength + assembly.dataAddress));
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
                            MemoryManager.Instance.WriteByte((uint)(assembly.dataAddress + dataLength++), byte.Parse(aux[j]));
                        }
                        break;
                    case ".float":
                        for (int j = 1; j < aux.Length; j++)
                        {
                            MemoryManager.Instance.WriteFloat((uint)(assembly.dataAddress + dataLength), float.Parse(aux[j]));
                            dataLength += 4;
                        }
                        break;
                    case ".double":
                        for (int j = 1; j < aux.Length; j++)
                        {
                            MemoryManager.Instance.WriteDouble((uint)(assembly.dataAddress + dataLength), double.Parse(aux[j]));
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
            foreach (var label in assembly.TextLabels)
            {
                labels.Add(label.Value, (uint)((label.Key * 4) + assembly.textAddress));
            }
            for (uint i = 0; i < assembly.CodeSegment.Count; i++)
            {
                string instruction = assembly.CodeSegment[(int)i];
                int parsedArguments = 1;
                Assembler.Instruction instructionSyntax = Assembler.OpCodes[0]; //Default to NOP so compiler doesn't complain.
                assembledInstruction = new BitVector32(0);
                string[] aux = instruction.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (var opcode in Assembler.OpCodes)
                {
                    if (opcode.Name == aux[0])
                    {
                        instructionSyntax = opcode;
                        assembledInstruction = new BitVector32((int)opcode.Opcode);
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
                                    try
                                    {
                                        assembledInstruction[rs1Section] = int.Parse(aux[parsedArguments]);
                                    }
                                    catch (Exception)
                                    {
                                        assembledInstruction[rs1Section] = labels.ContainsKey(aux[parsedArguments]) ? (int)labels[aux[parsedArguments]] : throw new ArgumentException("Invalid label");
                                    }
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
                                    try
                                    {
                                        assembledInstruction[rs2Section] = int.Parse(aux[parsedArguments]);
                                    }
                                    catch (Exception)
                                    {
                                        assembledInstruction[rs2Section] = labels.ContainsKey(aux[parsedArguments]) ? (int)labels[aux[parsedArguments]] : throw new ArgumentException("Invalid label");
                                    }
                                break;
                            case 'i':
                            case 'I':
                                assembledInstruction[addressSection] = int.Parse(aux[parsedArguments]);
                                break;
                            case 'd':
                            case 'D':
                            case 'p':
                            case 'P':
                                if (aux[parsedArguments].Contains('('))
                                {
                                    splitted = aux[parsedArguments].Replace('(', ' ').Replace(')', ' ').Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                    try
                                    {
                                        assembledInstruction[addressSection] = int.Parse(splitted[0]);
                                    }
                                    catch (Exception)
                                    {
                                        assembledInstruction[addressSection] = labels.ContainsKey(aux[parsedArguments]) ? (int)labels[aux[parsedArguments]] : throw new ArgumentException("Invalid label");
                                    }
                                    assembledInstruction[rs1Section] = int.Parse(splitted[1]);
                                }
                                else
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
                MemoryManager.Instance.WriteWord((uint)(assembly.textAddress + instructionsPlaced), assembledInstruction);
                OriginalText.Add(assembledInstruction.Data, assembly.OriginalText[instruction]);
                InstructionAddresses.Add((uint)(assembly.textAddress + instructionsPlaced), assembledInstruction.Data);
                instructionsPlaced += 4;
            }
        }
    }
}
