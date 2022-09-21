using System.Collections.Specialized;
using System.ComponentModel;
using System.Security.Cryptography;

namespace THUMDER.Deluxe
{
    internal class MemoryManager
    {
        /// <summary>
        /// Memory as a 32-bit unsigned integer array. 
        /// </summary>
        private byte[] memory;

        /// <summary>
        /// Singleton instance internal value
        /// </summary>
        private static MemoryManager? instance = null;
        /// <summary>
        /// Memory singleton instance
        /// </summary>
        public static MemoryManager Instance
        {
            private set => instance = value;
            get
            {   //Might need to add locks or lazy implementation for thread safety.
                if (instance == null)
                    instance = new MemoryManager();
                return instance;
            }
        }
        private MemoryManager() //The Memory constructor should only be called once per execution  
        {
            this.memory = new byte[32768];
        }

        /// <summary>
        /// Writes a Byte to memory.
        /// </summary>
        /// <param name="Address">The address to write.</param>
        /// <param name="Data">Data to write in the memory.</param>
        public void WriteByte(in uint Address, in byte Data)
        {
            memory[Address] = Data;
        }

        /// <summary>
        /// Reads a byte from memory.
        /// </summary>
        /// <param name="Address">The address to read from.</param>
        /// <returns>The byte that memory cell.</returns>
        public byte ReadByte(in uint Address)
        {
            return memory[Address];
        }

        /// <summary>
        /// Writes a Word to memory.
        /// </summary>
        /// <param name="Address">The address to write.</param>
        /// <param name="Data">Data to write in the memory.</param>
        public void WriteWord (in uint Address, in int Data)
        {
            byte[] aux = BitConverter.GetBytes(Data);
            memory[Address]     = aux[0];
            memory[Address + 1] = aux[1];
            memory[Address + 2] = aux[2];
            memory[Address + 3] = aux[3];
        }
        
        /// <summary>
        /// Writes a Word to memory.
        /// </summary>
        /// <param name="Address">The address to write.</param>
        /// <param name="Data">Data to write in the memory.</param>
        public void WriteWord (in uint Address, in BitVector32 Data)
        {
            WriteWord(Address, Data.Data);
        }

        /// <summary>
        /// Access the memory and returns the specified memory address.
        /// </summary>
        /// <param name="Address">The address of the memory to read.</param>
        /// <returns>A word starting in the specified address.</returns>
        public int ReadWord(in uint Address)
        {
            return BitConverter.ToInt32(memory, (int)Address);
        }

        /// <summary>
        /// Access the memory and returns the specified memory address.
        /// </summary>
        /// <param name="Address">The address of the memory to read.</param>
        /// <returns>A word starting in the specified address as a BitVector32.</returns>
        public BitVector32 ReadWordAsBitVector(in uint Address)
        {
            return new BitVector32(BitConverter.ToInt32(memory, (int)Address));
        }

        /// <summary>
        /// Writes a Half Word to memory.
        /// </summary>
        /// <param name="Address">The address to write.</param>
        /// <param name="Data">Data to write in the memory.</param>
        public void WriteHalf(in uint Address, in short Data)
        {
            byte[] aux = BitConverter.GetBytes(Data);
            memory[Address] = aux[0];
            memory[Address + 1] = aux[1];
        }

        /// <summary>
        /// Access the memory and returns the specified memory address.
        /// </summary>
        /// <param name="Address">The address of the memory to read.</param>
        /// <returns>A word starting in the specified address.</returns>
        public int ReadHalf(in uint Address)
        {
            return BitConverter.ToInt32(memory, (int)Address);
        }

        /// <summary>
        /// Writes a Float to memory.
        /// </summary>
        /// <param name="Address">The address to write.</param>
        /// <param name="Data">Data to write in the memory.</param>
        public void WriteFloat(in uint Address, in float Data)
        {
            byte[] aux = BitConverter.GetBytes(Data);
            memory[Address] = aux[0];
            memory[Address + 1] = aux[1];
            memory[Address + 2] = aux[2];
            memory[Address + 3] = aux[3];
        }

        /// <summary>
        /// Writes a Float to memory.
        /// </summary>
        /// <param name="Address">The address to write.</param>
        /// <param name="Data">Data to write in the memory.</param>
        public void WriteFloat (in uint Address, in BitVector32 Data)
        {
            WriteFloat(Address, Data.Data);
        }

        /// <summary>
        /// Access the memory and returns the specified memory address.
        /// </summary>
        /// <param name="Address">The address of the memory to read.</param>
        /// <returns>A float starting in the specified address.</returns>
        public float ReadFloat(in uint Address)
        {
            return BitConverter.ToSingle(memory, (int)Address);
        }

        /// <summary>
        /// Access the memory and returns the specified memory address.
        /// </summary>
        /// <param name="Address">The address of the memory to read.</param>
        /// <returns>A word starting in the specified address as a BitVector32.</returns>
        public BitVector32 ReadFloatAsBitVector(in uint Address)
        {
            return new BitVector32(BitConverter.ToInt32(memory, (int)Address));
        }

        /// <summary>
        /// Writes a Double to memory.
        /// </summary>
        /// <param name="Address">The address to write.</param>
        /// <param name="Data">Data to write in the memory.</param>
        public void WriteDouble(in uint Address, in double Data)
        {
            byte[] aux = BitConverter.GetBytes(Data);
            memory[Address]     = aux[0];
            memory[Address + 1] = aux[1];
            memory[Address + 2] = aux[2];
            memory[Address + 3] = aux[3];
            memory[Address + 4] = aux[4];
            memory[Address + 5] = aux[5];
            memory[Address + 6] = aux[6];
            memory[Address + 7] = aux[7];
        }

        /// <summary>
        /// Access the memory and returns the specified memory address.
        /// </summary>
        /// <param name="Address">The address of the memory to read.</param>
        /// <returns>A double starting in the specified address.</returns>
        public double ReadDouble(in uint Address)
        {
            return BitConverter.ToDouble(memory, (int)Address);
        }

        /// <summary>
        /// Clears the memory and resizes it to the specified value.
        /// </summary>
        /// <param name="newSize">New memory size.</param>
        public void ResizeMemory (in uint newSize)
        {
            memory = new byte[newSize];
        }
    }
}
