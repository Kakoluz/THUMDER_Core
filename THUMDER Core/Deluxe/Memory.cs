namespace THUMDER.Deluxe
{
    internal class Memory
    {
        /// <summary>
        /// Memory as a 32-bit unsigned integer array. 
        /// </summary>
        private uint[] memory;

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
            this.memory = new uint[32768];
        }

        /// <summary>
        /// Unified memory access to emulate the hardware behaviour.
        /// </summary>
        /// <param name="Address"> Emulated address bus.</param>
        /// <param name="Data"> Emulated data bus.</param>
        /// <param name="RW"> Specifies mode, high for writes, low for reads.</param>
        public static void MemoryAccess(in uint Address, ref byte[] Data, in bool RW)
        {
            if (!RW)
                Data = BitConverter.GetBytes(Instance.memory[Address]);
            else
                Instance.memory[Address] = BitConverter.ToUInt32(Data);
            return; 
        }

        /// <summary>
        /// Clears the memory and resizes it to the specified value.
        /// </summary>
        /// <param name="newSize">New memory size.</param>
        public static void ResizeMemory(in uint newSize)
        {
            Instance.memory = new uint[newSize];
        }
    }
}
