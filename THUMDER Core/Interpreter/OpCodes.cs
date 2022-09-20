using THUMDER.Deluxe;

namespace THUMDER.Interpreter
{
    public static partial class Assembler
    {
        /// <summary>
        /// Struct that represent a DLX instruction for execution.
        /// </summary>
        public readonly struct Instruction
        {
            /// <summary>
            /// Builds a generic instruction.
            /// </summary>
            /// <param name="name">The common name for this instruction <example>ADDI</example> </param>
            /// <param name="op">The internal Operation code for the specified instruction</param>
            /// <param name="args">Arguments taken by the instruction</param>
            public Instruction(in string name, in uint op, in string args)
            {
                this.Name = name;
                this.Opcode = op;
                string aux = String.Empty;
                foreach (string s in args.Split(','))
                    aux += s;
                this.Args = aux.ToCharArray();
            }

            public readonly string Name;
            public readonly uint Opcode;
            public readonly char[] Args;
        }

        // This code region contains every opcode possible on the DLX
        // Opcondes and definitions are from the following document:https://www.csd.uoc.gr/~hy425/2002s/dlxmap.html
        #region OpCodeDefinitions         
        /// <summary>
        /// Contains a list of all supported operations and its arguments.
        /// </summary>
        public readonly static Instruction[] OpCodes = new Instruction[]
        {
            new Instruction ( "nop",      0x00000000,   ""      ),  /* NOP                           */
                                                                                                     
            /* Arithmetic and Logic R-TYPE instructions.  */                                         
            new Instruction ( "add",      0x00000020,   "c,a,b" ),  /* Add                           */
            new Instruction ( "addu",     0x00000021,   "c,a,b" ),  /* Add Unsigned                  */
            new Instruction ( "sub",      0x00000022,   "c,a,b" ),  /* SUB                           */
            new Instruction ( "subu",     0x00000023,   "c,a,b" ),  /* Sub Unsigned                  */
            new Instruction ( "and",      0x00000024,   "c,a,b" ),  /* AND                           */
            new Instruction ( "or",       0x00000025,   "c,a,b" ),  /* OR                            */
            new Instruction ( "xor",      0x00000026,   "c,a,b" ),  /* Exclusive OR                  */
            new Instruction ( "sll",      0x00000004,   "c,a,b" ),  /* SHIFT LEFT LOGICAL            */
            new Instruction ( "sra",      0x00000007,   "c,a,b" ),  /* SHIFT RIGHT ARITHMETIC        */
            new Instruction ( "srl",      0x00000006,   "c,a,b" ),  /* SHIFT RIGHT LOGICAL           */
            new Instruction ( "seq",      0x00000028,   "c,a,b" ),  /* Set if equal                  */
            new Instruction ( "sne",      0x00000029,   "c,a,b" ),  /* Set if not equal              */
            new Instruction ( "slt",      0x0000002A,   "c,a,b" ),  /* Set if less                   */
            new Instruction ( "sgt",      0x0000002B,   "c,a,b" ),  /* Set if greater                */
            new Instruction ( "sle",      0x0000002C,   "c,a,b" ),  /* Set if less or equal          */
            new Instruction ( "sge",      0x0000002D,   "c,a,b" ),  /* Set if greater or equal       */
          //new Instruction ( "movi2s",   0x00000030,   "c,a"   ),  /* Move to special register      */
          //new Instruction ( "movs2i",   0x00000031,   "c,a"   ),  /* Move to general register      */
            new Instruction ( "movf",     0x00000032,   "c,a"   ),  /* Move float                    */
            new Instruction ( "movd",     0x00000033,   "c,a"   ),  /* Move double                   */
            new Instruction ( "movfp2i",  0x00000034,   "c,a"   ),  /* Move float to gp registers    */
            new Instruction ( "movi2fp",  0x00000035,   "c,a"   ),  /* Move integer to fp registers  */
            
            /* FPU R-TYPE instructions.  */
            new Instruction ( "addf",     0x04000000,   "c,a,b" ),  /* Add float                     */
            new Instruction ( "subf",     0x04000001,   "c,a,b" ),  /* Sub float                     */
            new Instruction ( "multf",    0x04000002,   "c,a,b" ),  /* Multiply float                */
            new Instruction ( "divf",     0x04000003,   "c,a,b" ),  /* Divide float                  */
            new Instruction ( "addd",     0x04000004,   "c,a,b" ),  /* Add double                    */
            new Instruction ( "subd",     0x04000005,   "c,a,b" ),  /* Sub double                    */
            new Instruction ( "multd",    0x04000006,   "c,a,b" ),  /* Multiply double               */
            new Instruction ( "divd",     0x04000007,   "c,a,b" ),  /* Divide double                 */
            new Instruction ( "cvtf2d",   0x04000008,   "c,a,b" ),  /* Convert float to double       */
            new Instruction ( "cvtf2i",   0x04000009,   "c,a,b" ),  /* Convert float to integer      */
            new Instruction ( "cvtd2f",   0x0400000A,   "c,a,b" ),  /* Convert double to float       */
            new Instruction ( "cvtd2i",   0x0400000B,   "c,a,b" ),  /* Convert double to integer     */
            new Instruction ( "cvti2f",   0x0400000C,   "c,a,b" ),  /* Convert integer to float      */
            new Instruction ( "cvti2d",   0x0400000D,   "c,a,b" ),  /* Convert integer to double     */
            new Instruction ( "mult",     0x0400000E,   "c,a,b" ),  /* Multiply integer              */
            new Instruction ( "div",      0x0400000F,   "c,a,b" ),  /* Divide integer                */
            new Instruction ( "eqf",      0x04000010,   "c,a,b" ),  /* Set if equal float            */
            new Instruction ( "nef",      0x04000011,   "c,a,b" ),  /* Set if not equal float        */
            new Instruction ( "ltf",      0x04000012,   "c,a,b" ),  /* Set if less  float            */
            new Instruction ( "lgf",      0x04000013,   "c,a,b" ),  /* Set if greater float          */
            new Instruction ( "lef",      0x04000014,   "c,a,b" ),  /* Set if less or equal float    */
            new Instruction ( "gef",      0x04000015,   "c,a,b" ),  /* Set if greater or equal float */
            new Instruction ( "multu",    0x04000016,   "c,a,b" ),  /* Multiply Unsigned             */
            new Instruction ( "divu",     0x04000017,   "c,a,b" ),  /* Divide Unsigned               */
            new Instruction ( "eqd",      0x04000018,   "c,a,b" ),  /* Set if equal double           */
            new Instruction ( "ned",      0x04000019,   "c,a,b" ),  /* Set if not equal double       */
            new Instruction ( "ltd",      0x0400001A,   "c,a,b" ),  /* Set if less  double           */
            new Instruction ( "gtd",      0x0400001B,   "c,a,b" ),  /* Set if greater double         */
            new Instruction ( "led",      0x0400001C,   "c,a,b" ),  /* Set if less or equal double   */
            new Instruction ( "ged",      0x0400001D,   "c,a,b" ),  /* Set if greater or equal double*/

            /* Arithmetic and Logical Immediate I-TYPE instructions.  */
            new Instruction ( "addi",     0x20000000,   "b,a,I" ),  /* Add Immediate                 */
            new Instruction ( "addui",    0x24000000,   "b,a,i" ),  /* Add Usigned Immediate         */
            new Instruction ( "subi",     0x28000000,   "b,a,I" ),  /* Sub Immediate                 */
            new Instruction ( "subui",    0x2C000000,   "b,a,i" ),  /* Sub Unsigned Immedated        */
            new Instruction ( "andi",     0x30000000,   "b,a,i" ),  /* AND Immediate                 */
            new Instruction ( "ori",      0x34000000,   "b,a,i" ),  /* OR  Immediate                 */
            new Instruction ( "xori",     0x38000000,   "b,a,i" ),  /* Exclusive OR  Immediate       */
            new Instruction ( "slli",     0x50000000,   "b,a,i" ),  /* SHIFT LEFT LOCICAL Immediate  */
            new Instruction ( "srai",     0x5C000000,   "b,a,i" ),  /* SHIFT RIGHT ARITH. Immediate  */
            new Instruction ( "srli",     0x58000000,   "b,a,i" ),  /* SHIFT RIGHT LOGICAL Immediate */
            new Instruction ( "seqi",     0x60000000,   "b,a,i" ),  /* Set if equal                  */
            new Instruction ( "snei",     0x64000000,   "b,a,i" ),  /* Set if not equal              */
            new Instruction ( "slti",     0x68000000,   "b,a,i" ),  /* Set if less                   */
            new Instruction ( "sgti",     0x6C000000,   "b,a,i" ),  /* Set if greater                */
            new Instruction ( "slei",     0x70000000,   "b,a,i" ),  /* Set if less or equal          */
            new Instruction ( "sgei",     0x74000000,   "b,a,i" ),  /* Set if greater or equal       */
                                                                                                     
            /* Load high Immediate I-TYPE instruction.  */                                           
            new Instruction ( "lhi",      0x3C000000,   "b,i"   ),  /* Load High Immediate           */
                                                                                                     
            /* LOAD/STORE BYTE 8 bits I-TYPE.  */                                                    
            new Instruction ( "lb",       0x80000000,   "b,a"   ),  /* Load Byte                     */
            new Instruction ( "lbu",      0x90000000,   "b,a"   ),  /* Load Byte Unsingned           */
            new Instruction ( "sb",       0xA0000000,   "b,a"   ),  /* Store Byte                    */
                                                                                                     
            /* LOAD/STORE HALFWORD 16 bits.  */                                                      
            new Instruction ( "lh",       0x84000000,   "b,a"   ),  /* Load Halfword                 */
            new Instruction ( "lhu",      0x94000000,   "b,a"   ),  /* Load Halfword Unsingned       */
            new Instruction ( "sh",       0xA4000000,   "b,a"   ),  /* Store Halfword                */
                                                                                                     
            /* LOAD/STORE WORD 32 bits.  */                                                          
            new Instruction ( "lw",       0x8C000000,   "b,a"   ),  /* Load Word                     */
            new Instruction ( "sw",       0xAC000000,   "b,a"   ),  /* Store Word                    */
                                                                                                     
            /* LOAD/STORE FLOATS.  */                                                                
            new Instruction ( "lf",       0x98000000,   "b,a"   ),  /* Load Float                    */
            new Instruction ( "sf",       0xB8000000,   "b,a"   ),  /* Store Float                   */
                                                                                                     
            /* LOAD/STORE DOUBLES.  */                                                               
            new Instruction ( "ld",       0x9C000000,   "b,a"   ),  /* Load Double                   */
            new Instruction ( "sd",       0xBC000000,   "b,a"   ),  /* Store Double                  */
                                                                                                     
            /* Branch PC-relative, 16 bits offset.  */                                               
            new Instruction ( "beqz",     0x10000000,   "a,d"   ),  /* Branch if a == 0              */
            new Instruction ( "bfpt",     0x18000000,   "d"     ),  /* Branch if fp status == 0      */
            new Instruction ( "bfpf",     0x1C000000,   "d"     ),  /* Branch if fp status != 0      */
            new Instruction ( "bnez",     0x14000000,   "a,d"   ),  /* Branch if a != 0              */
                                                                                                     
            /* Jumps Trap and RFE J-TYPE.  */                                                        
            new Instruction ( "j",        0x08000000,   "D"     ),  /* Jump, PC-relative 26 bits     */
            new Instruction ( "jal",      0x0C000000,   "D"     ),  /* JAL, PC-relative 26 bits      */
            new Instruction ( "trap" ,    0x44000000,   "D"     ),  /* TRAP to OS                    */
            new Instruction ( "rfe",      0x40000000,   ""      ),  /* Return From Exception         */
            new Instruction ( "call",     0x08000000,   "D"     ),  /* Jump, PC-relative 26 bits     */
                                                                                                     
            /* Jumps Trap and RFE I-TYPE.  */                                                        
            new Instruction ( "jr",       0x48000000,   "a"     ),  /* Jump Register, Abs (32 bits)  */
            new Instruction ( "jalr",     0x4C000000,   "a"     ),  /* JALR, Abs (32 bits)           */
        };
        #endregion
    }
}
