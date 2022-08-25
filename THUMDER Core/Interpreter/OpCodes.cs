namespace THUMDER.Interpreter
{
    public static partial class Assembler
    {
        /// <summary>
        /// Struct that represent a DLX instruction for execution.
        /// </summary>
        private readonly struct Instruction
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
        // Opcondes and definitions are adapted to C# from the following document: https://opensource.apple.com/source/gdb/gdb-1119/src/include/opcode/dlx.h Licesed under GNU GPLv2
        #region OpCodeDefinitions 
        private readonly static uint ALUOP     = 0x00000000;
        private readonly static uint SPECIALOP = 0x00000000;

        private readonly static uint NOPF      = 0x00000000;
        private readonly static uint SLLF      = 0x00000004;
        private readonly static uint SRLF      = 0x00000006;
        private readonly static uint SRAF      = 0x00000007;

        private readonly static uint SEQUF     = 0x00000010;
        private readonly static uint SNEUF     = 0x00000011;
        private readonly static uint SLTUF     = 0x00000012;
        private readonly static uint SGTUF     = 0x00000013;
        private readonly static uint SLEUF     = 0x00000014;
        private readonly static uint SGEUF     = 0x00000015;

        private readonly static uint ADDF      = 0x00000020;
        private readonly static uint ADDUF     = 0x00000021;
        private readonly static uint SUBF      = 0x00000022;
        private readonly static uint SUBUF     = 0x00000023;
        private readonly static uint ANDF      = 0x00000024;
        private readonly static uint ORF       = 0x00000025;
        private readonly static uint XORF      = 0x00000026;

        private readonly static uint SEQF      = 0x00000028;
        private readonly static uint SNEF      = 0x00000029;
        private readonly static uint SLTF      = 0x0000002A;
        private readonly static uint SGTF      = 0x0000002B;
        private readonly static uint SLEF      = 0x0000002C;
        private readonly static uint SGEF      = 0x0000002D;

        private readonly static uint MVTSF     = 0x00000030;
        private readonly static uint MVFSF     = 0x00000031;
        private readonly static uint BSWAPF    = 0x00000032;
        private readonly static uint LUTF      = 0x00000033;

        private readonly static uint MULTF     = 0x00000005;
        private readonly static uint MULTUF    = 0x00000006;
        private readonly static uint DIVF      = 0x00000007;
        private readonly static uint DIVUF     = 0x00000008;

        private readonly static uint JOP       = 0x08000000;
        private readonly static uint JALOP     = 0x0c000000;
        private readonly static uint BEQOP     = 0x10000000;
        private readonly static uint BNEOP     = 0x14000000;

        private readonly static uint ADDIOP    = 0x20000000;
        private readonly static uint ADDUIOP   = 0x24000000;
        private readonly static uint SUBIOP    = 0x28000000;
        private readonly static uint SUBUIOP   = 0x2c000000;
        private readonly static uint ANDIOP    = 0x30000000;
        private readonly static uint ORIOP     = 0x34000000;
        private readonly static uint XORIOP    = 0x38000000;
        private readonly static uint LHIOP     = 0x3c000000;
        private readonly static uint RFEOP     = 0x40000000;
        private readonly static uint TRAPOP    = 0x44000000;
        private readonly static uint JROP      = 0x48000000;
        private readonly static uint JALROP    = 0x4c000000;
        private readonly static uint BREAKOP   = 0x50000000;

        private readonly static uint SEQIOP    = 0x60000000;
        private readonly static uint SNEIOP    = 0x64000000;
        private readonly static uint SLTIOP    = 0x68000000;
        private readonly static uint SGTIOP    = 0x6c000000;
        private readonly static uint SLEIOP    = 0x70000000;
        private readonly static uint SGEIOP    = 0x74000000;

        private readonly static uint LBOP      = 0x80000000;
        private readonly static uint LHOP      = 0x84000000;
        private readonly static uint LWOP      = 0x8c000000;
        private readonly static uint LBUOP     = 0x90000000;
        private readonly static uint LHUOP     = 0x94000000;
        private readonly static uint LDSTBU;
        private readonly static uint LDSTHU;
        private readonly static uint SBOP      = 0xa0000000;
        private readonly static uint SHOP      = 0xa4000000;
        private readonly static uint SWOP      = 0xac000000;
        private readonly static uint LDST;

        private readonly static uint SEQUIOP   = 0xc0000000;
        private readonly static uint SNEUIOP   = 0xc4000000;
        private readonly static uint SLTUIOP   = 0xc8000000;
        private readonly static uint SGTUIOP   = 0xcc000000;
        private readonly static uint SLEUIOP   = 0xd0000000;
        private readonly static uint SGEUIOP   = 0xd4000000;

        private readonly static uint SLLIOP    = 0xd8000000;
        private readonly static uint SRLIOP    = 0xdc000000;
        private readonly static uint SRAIOP    = 0xe0000000;

        private readonly static uint LSBUOP    = 0x98000000;
        private readonly static uint LSHUOP    = 0x9c000000;
        private readonly static uint LSWOP     = 0xb0000000;
        
        /// <summary>
        /// Contains a list of all supported operations and its arguments.
        /// </summary>
        private readonly static Instruction[] OpCodes = new Instruction[]
        {
            /* Arithmetic and Logic R-TYPE instructions.  */
            new Instruction ( "nop",      (ALUOP|NOPF),   ""      ),  /* NOP                          */
            new Instruction ( "add",      (ALUOP|ADDF),   "c,a,b" ),  /* Add                          */
            new Instruction ( "addu",     (ALUOP|ADDUF),  "c,a,b" ),  /* Add Unsigned                 */
            new Instruction ( "sub",      (ALUOP|SUBF),   "c,a,b" ),  /* SUB                          */
            new Instruction ( "subu",     (ALUOP|SUBUF),  "c,a,b" ),  /* Sub Unsigned                 */
            new Instruction ( "mult",     (ALUOP|MULTF),  "c,a,b" ),  /* MULTIPLY                     */
            new Instruction ( "multu",    (ALUOP|MULTUF), "c,a,b" ),  /* MULTIPLY Unsigned            */
            new Instruction ( "div",      (ALUOP|DIVF),   "c,a,b" ),  /* DIVIDE                       */
            new Instruction ( "divu",     (ALUOP|DIVUF),  "c,a,b" ),  /* DIVIDE Unsigned              */
            new Instruction ( "and",      (ALUOP|ANDF),   "c,a,b" ),  /* AND                          */
            new Instruction ( "or",       (ALUOP|ORF),    "c,a,b" ),  /* OR                           */
            new Instruction ( "xor",      (ALUOP|XORF),   "c,a,b" ),  /* Exclusive OR                 */
            new Instruction ( "sll",      (ALUOP|SLLF),   "c,a,b" ),  /* SHIFT LEFT LOGICAL           */
            new Instruction ( "sra",      (ALUOP|SRAF),   "c,a,b" ),  /* SHIFT RIGHT ARITHMETIC       */
            new Instruction ( "srl",      (ALUOP|SRLF),   "c,a,b" ),  /* SHIFT RIGHT LOGICAL          */
            new Instruction ( "seq",      (ALUOP|SEQF),   "c,a,b" ),  /* Set if equal                 */
            new Instruction ( "sne",      (ALUOP|SNEF),   "c,a,b" ),  /* Set if not equal             */
            new Instruction ( "slt",      (ALUOP|SLTF),   "c,a,b" ),  /* Set if less                  */
            new Instruction ( "sgt",      (ALUOP|SGTF),   "c,a,b" ),  /* Set if greater               */
            new Instruction ( "sle",      (ALUOP|SLEF),   "c,a,b" ),  /* Set if less or equal         */
            new Instruction ( "sge",      (ALUOP|SGEF),   "c,a,b" ),  /* Set if greater or equal      */
            new Instruction ( "sequ",     (ALUOP|SEQUF),  "c,a,b" ),  /* Set if equal unsigned        */
            new Instruction ( "sneu",     (ALUOP|SNEUF),  "c,a,b" ),  /* Set if not equal unsigned    */
            new Instruction ( "sltu",     (ALUOP|SLTUF),  "c,a,b" ),  /* Set if less unsigned         */
            new Instruction ( "sgtu",     (ALUOP|SGTUF),  "c,a,b" ),  /* Set if greater unsigned      */
            new Instruction ( "sleu",     (ALUOP|SLEUF),  "c,a,b" ),  /* Set if less or equal unsigned*/
            new Instruction ( "sgeu",     (ALUOP|SGEUF),  "c,a,b" ),  /* Set if greater or equal      */
            new Instruction ( "mvts",     (ALUOP|MVTSF),  "c,a"   ),  /* Move to special register     */
            new Instruction ( "mvfs",     (ALUOP|MVFSF),  "c,a"   ),  /* Move from special register   */
            new Instruction ( "bswap",    (ALUOP|BSWAPF), "c,a,b" ),  /* ??? Was not documented       */
            new Instruction ( "lut",      (ALUOP|LUTF),   "c,a,b" ),  /* ????? same as above          */

            /* Arithmetic and Logical Immediate I-TYPE instructions.  */
            new Instruction ( "addi",     ADDIOP,         "b,a,I" ),  /* Add Immediate                */
            new Instruction ( "addui",    ADDUIOP,        "b,a,i" ),  /* Add Usigned Immediate        */
            new Instruction ( "subi",     SUBIOP,         "b,a,I" ),  /* Sub Immediate                */
            new Instruction ( "subui",    SUBUIOP,        "b,a,i" ),  /* Sub Unsigned Immedated       */
            new Instruction ( "andi",     ANDIOP,         "b,a,i" ),  /* AND Immediate                */
            new Instruction ( "ori",      ORIOP,          "b,a,i" ),  /* OR  Immediate                */
            new Instruction ( "xori",     XORIOP,         "b,a,i" ),  /* Exclusive OR  Immediate      */
            new Instruction ( "slli",     SLLIOP,         "b,a,i" ),  /* SHIFT LEFT LOCICAL Immediate */
            new Instruction ( "srai",     SRAIOP,         "b,a,i" ),  /* SHIFT RIGHT ARITH. Immediate */
            new Instruction ( "srli",     SRLIOP,         "b,a,i" ),  /* SHIFT RIGHT LOGICAL Immediate*/
            new Instruction ( "seqi",     SEQIOP,         "b,a,i" ),  /* Set if equal                 */
            new Instruction ( "snei",     SNEIOP,         "b,a,i" ),  /* Set if not equal             */
            new Instruction ( "slti",     SLTIOP,         "b,a,i" ),  /* Set if less                  */
            new Instruction ( "sgti",     SGTIOP,         "b,a,i" ),  /* Set if greater               */
            new Instruction ( "slei",     SLEIOP,         "b,a,i" ),  /* Set if less or equal         */
            new Instruction ( "sgei",     SGEIOP,         "b,a,i" ),  /* Set if greater or equal      */
            new Instruction ( "sequi",    SEQUIOP,        "b,a,i" ),  /* Set if equal                 */
            new Instruction ( "sneui",    SNEUIOP,        "b,a,i" ),  /* Set if not equal             */
            new Instruction ( "sltui",    SLTUIOP,        "b,a,i" ),  /* Set if less                  */
            new Instruction ( "sgtui",    SGTUIOP,        "b,a,i" ),  /* Set if greater               */
            new Instruction ( "sleui",    SLEUIOP,        "b,a,i" ),  /* Set if less or equal         */
            new Instruction ( "sgeui",    SGEUIOP,        "b,a,i" ),  /* Set if greater or equal      */

            /* Macros for I type instructions.  */
            new Instruction ( "mov",      ADDIOP,         "b,P"   ),  /* a move macro                 */
            new Instruction ( "movu",     ADDUIOP,        "b,P"   ),  /* a move macro, unsigned       */

            /* Load high Immediate I-TYPE instruction.  */
            new Instruction ( "lhi",      LHIOP,          "b,i"   ),  /* Load High Immediate          */
            new Instruction ( "lui",      LHIOP,          "b,i"   ),  /* Load High Immediate          */
            new Instruction ( "sethi",    LHIOP,          "b,i"   ),  /* Load High Immediate          */
            
            /* LOAD/STORE BYTE 8 bits I-TYPE.  */
            new Instruction ( "lb",       LBOP,           "b,a" ),  /* Load Byte                    */
            new Instruction ( "lbu",      LBUOP,          "b,a" ),  /* Load Byte Unsigned           */
            new Instruction ( "ldstbu",   LSBUOP,         "b,a" ),  /* Load store Byte Unsigned     */
            new Instruction ( "sb",       SBOP,           "b,a" ),  /* Store Byte                   */

            /* LOAD/STORE HALFWORD 16 bits.  */
            new Instruction ( "lh",       LHOP,           "b,a" ),  /* Load Halfword                */
            new Instruction ( "lhu",      LHUOP,          "b,a" ),  /* Load Halfword Unsigned       */
            new Instruction ( "ldsthu",   LSHUOP,         "b,a" ),  /* Load Store Halfword Unsigned */
            new Instruction ( "sh",       SHOP,           "b,a" ),  /* Store Halfword               */

            /* LOAD/STORE WORD 32 bits.  */
            new Instruction ( "lw",       LWOP,           "b,a" ),  /* Load Word                    */
            new Instruction ( "sw",       SWOP,           "b,a" ),  /* Store Word                   */
            new Instruction ( "ldstw",    LSWOP,          "b,a" ),  /* Load Store Word              */

            /* Branch PC-relative, 16 bits offset.  */
            new Instruction ( "beqz",     BEQOP,          "a,d"   ),   /* Branch if a == 0             */
            new Instruction ( "bnez",     BNEOP,          "a,d"   ),   /* Branch if a != 0             */
            new Instruction ( "beq",      BEQOP,          "a,d"   ),   /* Branch if a == 0             */
            new Instruction ( "bne",      BNEOP,          "a,d"   ),   /* Branch if a != 0             */

            /* Jumps Trap and RFE J-TYPE.  */
            new Instruction ( "j",        JOP,            "D"     ),  /* Jump, PC-relative 26 bits    */
            new Instruction ( "jal",      JALOP,          "D"     ),  /* JAL, PC-relative 26 bits     */
            new Instruction ( "break",    BREAKOP,        "D"     ),  /* break to OS                  */
            new Instruction ( "trap" ,    TRAPOP,         "D"     ),  /* TRAP to OS                   */
            new Instruction ( "rfe",      RFEOP,          ""      ),  /* Return From Exception        */
            new Instruction ( "call",     JOP,            "D"     ),  /* Jump, PC-relative 26 bits    */

            /* Jumps Trap and RFE I-TYPE.  */
            new Instruction ( "jr",       JROP,           "a"     ),  /* Jump Register, Abs (32 bits) */
            new Instruction ( "jalr",     JALROP,         "a"     ),  /* JALR, Abs (32 bits)          */
            
            /* Macros.  */
            new Instruction ( "retr",      JROP,          "a"     ),   /* Jump Register, Abs (32 bits) */
        };
        #endregion
    }
}
