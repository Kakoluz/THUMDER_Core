namespace THUMDER.Interpreter
{
    static internal partial class Assembler
    {
        /// <summary>
        /// Transforms assembly data directives into byte values and instructions syntax.
        /// </summary>
        /// <param name="file">The assembler file to process.</param>
        /// <returns>An assembly ready to be read by the emulated CPU.</returns>
        public static ASM Decode(in string[] file)
        {
            ASM assembly = new();
            bool dataSegement = false;
            
            //Loop throug the file
            foreach (string line in file)
            {
                //Skip empty lines.
                if (line != String.Empty || line.Contains(';'))
                {
                    //Detect where whe are reading code or data.
                    if (line.Contains(".data"))
                    {
                        dataSegement = true;
                    }
                    else if (line.Contains(".text"))
                    {
                        dataSegement = false;
                    }

                    //Split the current line into words to process one by one.
                    string[] aux = line.Split(' ');
                    uint i = 0;
                    string label = String.Empty;
                    //check for labels
                    if (aux[0].Contains(':'))
                    {
                        label = aux[0].Remove(aux[0].Length - 1);
                        ++i;
                    }

                    //Check for directives
                    if (dataSegement)
                    {
                                            if (label != String.Empty)
                    {
                        assembly.Labels.Add(label, (uint)assembly.DataSegment.Count);
                    }
                        uint args = 1;
                        switch (aux[i])
                        {
                            case ".align":
                                //TODO, is this really necessary for an emulator?
                                break;
                            case ".asciiz":
                                //Process both strings the same for now
                            case ".asii":
                                //TODO
                                break;
                            case ".byte":
                                args = 1;
                                while (i + args < aux.Length)
                                {
                                    assembly.DataSegment.Add(byte.Parse(aux[i + args]));
                                }
                                break;
                            case ".double":
                                args = 1;
                                while (i + args < aux.Length)
                                {
                                    byte[] array = BitConverter.GetBytes(double.Parse(aux[i + args]));
                                    foreach(byte b in array)
                                        assembly.DataSegment.Add(b);
                                }
                                break;
                            case ".float":
                                args = 1;
                                while (i + args < aux.Length)
                                {
                                    byte[] array = BitConverter.GetBytes(float.Parse(aux[i + args]));
                                    foreach (byte b in array)
                                        assembly.DataSegment.Add(b);
                                }
                                break;
                            case ".global":
                                assembly.GlobalLabels.Add(aux[i+1]);
                                break;
                            case ".space":
                                uint spaces = uint.Parse(aux[i + 1]);
                                for (int j = 0; j < spaces; j++)
                                {
                                    assembly.DataSegment.Add(0x0);
                                }
                                break;
                            case ".word":
                                args = 1;
                                while (i + args < aux.Length)
                                {
                                    byte[] array = BitConverter.GetBytes(int.Parse(aux[i + args]));
                                    foreach (byte b in array)
                                        assembly.DataSegment.Add(b);
                                }
                                break;
                            default:
                                throw new ArgumentException("Invalid data directive.");
                        }                       
                    }
                }
            }
            return assembly;
        }

        /* //Not needed, too much complexity for an interpreter.
        /// <summary>
        /// Checks and builds an instruction from an assembly line of code.
        /// </summary>
        /// <param name="line">The line trimmed in format: <c>Instruction arguments</c>. </param>
        /// <returns>The internal DLX operation code assembled.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the instruction or its parameters are incorrectly written.
        /// </exception>
        private static uint AssembleInstruction(in string[] line)
        {
            uint instruction = 0x00000000;
            bool found = false;
            ushort index = 0;
            while(index < OpCodes.Length || !found)
            {
                ++index;
                if (OpCodes[index].Name == line[0])
                    found = true;
            }
            if (!found || OpCodes[index].Args.Length != (line.Length -1))
                throw new ArgumentException("Invalid instruction.", nameof(line));
            foreach (char arg in OpCodes[index].Args)
            {
                switch (arg)
                {
                    case 'c':
                        instruction |= ((uint) arg << 11);
                        break;
                    case 'a':
                        instruction |= ((uint) arg << 21);
                        break;
                    case 'b':
                        instruction |= ((uint) arg << 16);
                        break;
                }
            }
            return instruction;
        }
        */
    }
}
