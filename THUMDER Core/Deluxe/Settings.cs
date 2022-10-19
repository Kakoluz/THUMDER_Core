using System.Collections.Specialized;

namespace THUMDER.Deluxe
{
    internal sealed partial class SimManager
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
        public static int ADDDelay { get; private set; } = 2;
        /// <summary>
        /// Number of cicles for a floating point multiplication operation.
        /// </summary>
        public static int MULDelay { get; private set; } = 5;
        /// <summary>
        /// Number of cicles for a floatig point division operation.
        /// </summary>
        public static int DIVDelay { get; private set; } = 19;

        /// <summary>
        /// R-Type Instruction function field.
        /// </summary>
        static readonly BitVector32.Section functSection = BitVector32.CreateSection(63);
        /// <summary>
        /// R-Type Instruction displacement field.
        /// </summary>
        static readonly BitVector32.Section shamtSection = BitVector32.CreateSection(31, functSection);
        /// <summary>
        /// R-Type Instruction destiny register field.
        /// </summary>
        static readonly BitVector32.Section rdSection = BitVector32.CreateSection(31, shamtSection);
        /// <summary>
        /// Source 2 register field or immediate number for I-type.
        /// </summary>
        static readonly BitVector32.Section rs2Section = BitVector32.CreateSection(31, rdSection);
        /// <summary>
        /// Source 1 register field.
        /// </summary>
        static readonly BitVector32.Section rs1Section = BitVector32.CreateSection(31, rs2Section);
        /// <summary>
        /// Address section for I-Type Instructions.
        /// </summary>
        static readonly BitVector32.Section addressSection = BitVector32.CreateSection(short.MaxValue - 1); //Displacement based on RS1.
        // For J-Types we need to create a new bitvector32 and put the op bits to 0 and convert to int32

        /// <summary>
        /// Instruction OpCode field.
        /// </summary>
        static readonly BitVector32.Section opSection = BitVector32.CreateSection(63, rs1Section);

        /// <summary>
        /// Bitvector storing 0 to use recurrently to clean registers.
        /// </summary>
        private static readonly BitVector32 zeroBits = new BitVector32(0);
        /// <summary>
        /// Bitvector storing 0 to use recurrently to clean double registers.
        /// </summary>
        private static readonly BitVector32[] zeroBitsDouble = { zeroBits, zeroBits };

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
    }
}
