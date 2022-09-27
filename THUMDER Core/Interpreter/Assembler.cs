using System;
using System.Security.Cryptography.X509Certificates;

namespace THUMDER.Interpreter
{
    public static partial class Assembler
    {
        /// <summary>
        /// Transforms assembly data directives into byte values and instructions syntax.
        /// </summary>
        /// <param name="file">The assembler file to process.</param>
        /// <returns>An assembly ready to be read by the emulated CPU.</returns>
        public static ASM Decode(in string[] file)
        {
            ASM assembly = new ASM();
            short? dataSegement = null;
            short? textSegment = null;

            //check for labels and locate where the data and code are located.

            for (uint l = 0; l < file.Length; l++)
            {
                if (file[l].Contains(';'))
                {
                    if (file[l].IndexOf(';') < 2) //There may be a space before the ;
                    {
                        file[l] = String.Empty; //Remove the entire line.
                    }
                    else
                    {
                        file[l] = file[l].Split(';')[0]; //Remove comments from end of the line.
                    }
                }
                if (file[l].Contains(".data"))
                {
                    string[] aux = file[l].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    assembly.dataAddress = aux.Length > 1 ? int.Parse(aux[1]) : 0;
                    dataSegement = (short)(l + 1); //The next line contains the data segment.
                    file[l] = String.Empty; //Delete line to avoid re processing.
                }
                if (file[l].Contains(".text"))
                {
                    string[] aux = file[l].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    assembly.textAddress = aux.Length > 1 ? int.Parse(aux[1]) : 1000;
                    textSegment = (short)(l + 1); //The next line contains the code segment.
                    file[l] = String.Empty; //Delete line to avoid re processing.
                }
                if (file[l].Contains(':'))
                {
                    assembly.Labels.Add(l, file[l].Split(':')[0]); //Get the part before : as label
                    file[l] = file[l].Split(':')[1]; //Remove the label directive from the text
                }
                if (file[l].Contains(".global"))
                {
                    assembly.GlobalLabels.Add(l+1, file[l].Substring(file[l].IndexOf(' ')+1)); //Split label and directive.
                    file[l] = String.Empty; //Delete line to avoid re processing.
                }
                foreach (var item in assembly.GlobalLabels)
                {
                    if (item.Value == assembly.Labels[l])
                    {
                        assembly.GlobalLabels.Remove(item.Key);
                        assembly.GlobalLabels.Add(l, assembly.Labels[l]);
                    }//Update the global labels if there is conflict.
                }
            }
            
            //Check if there is code segement specified.
            
            if (textSegment == null)
            {
                throw new ArgumentException("Missing .text directive.\nNo code in the file?");
            }
            
            //if there is code then do a scan in the data segment to get variables.
            
            else if (dataSegement != null)
            {
                for (int l = (int)dataSegement; l < textSegment; l++)
                {
                    if (file[l] != String.Empty)
                        assembly.DataSegment.Add(DecodeData(file[l], l)); //Decode data directives and add them to the assembly.
                }
            }

            //Then check the code syntax.
            
            for (int l = (int)textSegment; l < file.Length; l++ )
            {
                if (file[l] != String.Empty)
                    assembly.CodeSegment.Add(DecodeInstruction(file[l], l)); //Check instruction sintax and add them to the assembly.
            }
            return assembly;
        }

        /// <summary>
        /// Adds decoded data directives to the assembly.
        /// </summary>
        /// <param name="data">The data line of the text file.</param>
        /// <param name="line">The line number of the text file.</param>
        /// <exception cref="ArgumentException">If its an unknwon data directive.</exception>
        private static string DecodeData(in string data,in int line)
        {
            string[] aux = data.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!(aux[0].Contains(".align")
                || aux[0].Contains(".space")
                || aux[0].Contains(".byte")
                || aux[0].Contains(".word")
                || aux[0].Contains(".float")
                || aux[0].Contains(".double")
                || aux[0].Contains(".ascii")
                || aux[0].Contains(".asciiz")))
            {
                throw new ArgumentException("Unknown data directive " + aux[0] + " on line " + line + ".");
            }
            else
            {
                string clean = string.Empty;
                foreach (string s in aux)
                {
                    string temp;
                    string current = s;
                    while (current.IndexOf(',') != -1)
                    {
                        temp = current;
                        int comma = current.IndexOf(',');
                        if (comma > 0)
                        {
                            temp = current.Remove(comma);
                            current = current.Remove(0, comma + 1);
                        }
                        clean = string.Concat(clean, ' ', temp);
                    }
                    clean = string.Concat(clean, ' ', current);
                }
                return clean.Trim();
            }
        }
        private static string DecodeInstruction(in string instruction,in int lineCount)
        {
            string[] cleaned = instruction.Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string decoded = String.Empty;
            int i = 0;
            for (int j = 0; j < OpCodes.Length; j++)
            {
                if (OpCodes[j].Name == cleaned[i].ToLower())
                {
                    decoded = OpCodes[j].Name;
                    for (int x = 0; x < OpCodes[j].Args.Length; x++)
                    {
                        switch (OpCodes[j].Args[x])
                        {
                            case 'i':
                            case 'I':
                                if (cleaned[i + x + 1].Contains('#'))
                                    decoded = string.Concat(decoded, " ", cleaned[i + x + 1].AsSpan(cleaned[i+x+1].IndexOf('#') + 1)); //Remove # from immediate values if present.
                                else
                                    decoded += " " + cleaned[i + x + 1];
                                break;
                            case 'd':
                            case 'D':
                                if (cleaned[i + x + 1].Contains('$'))
                                    decoded = string.Concat(decoded, " ", cleaned[i + x + 1].AsSpan(cleaned[i + x + 1].IndexOf('$') + 1)); //Remove $ from labels if present.
                                else
                                    decoded += " " + cleaned[i + x + 1];
                                break;
                            case 'c':
                            case 'b':
                            case 'a':
                                if (cleaned[i + x + 1].Trim()[0] is 'r' or 'f')
                                {
                                    decoded = string.Concat(decoded, " ", cleaned[i + x + 1].AsSpan(1));
                                }
                                else if (cleaned[i + x + 1].Contains('r') || cleaned[i + x + 1].Contains('f'))
                                {
                                    int index;
                                    index = cleaned[i + x + 1].IndexOf('r') != -1 ? cleaned[i + x + 1].IndexOf('r') : cleaned[i + x + 1].IndexOf('f');
                                    decoded = string.Concat(decoded, " ", cleaned[i + x + 1].AsSpan(0, index));
                                    decoded = string.Concat(decoded, cleaned[i + x + 1].AsSpan(index + 1));
                                }
                                else
                                    decoded = string.Concat(decoded, " ", cleaned[i + x + 1].AsSpan(0));
                                break;
                            default:
                                throw new ArgumentException("Invalid argument \"" + cleaned[i + x + 1] + "\" at line " + lineCount); //Remove R or F from registers names.
                        }
                    }
                    break;
                }
            }
            if (decoded == String.Empty)
                throw new ArgumentException("Invalid instruction \"" + instruction[i] + "\" at line " + lineCount);
            return decoded;
        }
    }
}