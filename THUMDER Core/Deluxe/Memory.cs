using System.Collections.Specialized;

namespace THUMDER.Deluxe
{
    internal class Memory
    {
        /// <summary>
        /// Memory as a 32-bit unsigned integer array. 
        /// </summary>
        private BitVector32[] memory;

        /// <summary>
        /// Singleton instance internal value
        /// </summary>
        private static Memory? instance = null;
        /// <summary>
        /// Memory singleton instance
        /// </summary>
        private static Memory Instance
        {
            set => instance = value;
            get
            {   //Might need to add locks or lazy implementation for thread safety.
                if (instance == null)
                    instance = new Memory();
                return instance;
            }
        }
        private Memory() //The Memory constructor should only be called once per execution  
        {
            this.memory = new BitVector32[32768 / 4];
        }

        /// <summary>
        /// Emulate the hardware behaviour of the memory controller.
        /// </summary>
        /// <param name="Address"> Emulated address bus.</param>
        /// <param name="Data"> Emulated data bus.</param>
        /// <param name="RW"> Specifies mode, high for writes, low for reads.</param>
        public static void HardwareAccess(in uint Address, ref int Data, in bool RW)
        {
            if (!RW)
                Data = Instance.memory[Address].Data;
            else
                Instance.memory[Address] = new BitVector32(Data);
            return; 
        }

        /// <summary>
        /// Access the memory and returns the specified memory address.
        /// </summary>
        /// <param name="Address">The address of the memory to read.</param>
        /// <returns>Contents of the memory cell in address.</returns>
        public static BitVector32 Read (in uint Address)
        {
            return Instance.memory[Address];
        }

        /// <summary>
        /// Writes data to memory.
        /// </summary>
        /// <param name="Address">The address to write.</param>
        /// <param name="Data">Data to write in the memory.</param>
        public static void Write (in uint Address, in int Data)
        {
            Instance.memory[Address] = new BitVector32(Data);
        }
        /// <summary>
        /// Writes data to memory.
        /// </summary>
        /// <param name="Address">The address to write.</param>
        /// <param name="Data">Data to write in the memory.</param>
        public static void Write(in uint Address, in BitVector32 Data)
        {
            Instance.memory[Address] = new BitVector32(Data);
        }

        /// <summary>
        /// Clears the memory and resizes it to the specified value.
        /// </summary>
        /// <param name="newSize">New memory size.</param>
        public static void ResizeMemory(in uint newSize)
        {
            uint sizeFormatted = newSize % 4 == 0 ? (newSize / 4) : ((newSize / 4) + 1); //Size is given in bytes. Bitvector32 is 4 bytes.
            Instance.memory = new BitVector32[sizeFormatted];
        }
    }
}
