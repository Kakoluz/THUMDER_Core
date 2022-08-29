using THUMDER_Core.Deluxe;
using System.Collections.Specialized;

namespace THUMDER.Deluxe
{
    internal sealed class SimManager
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
        public static int ADDDelay  { get; private set; } = 2;
        /// <summary>
        /// Number of cicles for a floating point multiplication operation.
        /// </summary>
        public static int MULDDelay { get; private set; } = 5;
        /// <summary>
        /// Number of cicles for a floatig point division operation.
        /// </summary>
        public static int DIVDelay { get; private set; } = 19;

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

        /// <summary>
        /// Program Counter
        /// </summary>
        private uint PC;

        /// <summary>
        /// List of ALU units.
        /// </summary>
        private List<ALU> alus;

        /// <summary>
        /// List of multiply units.
        /// </summary>
        private List<FPU> muls;

        /// <summary>
        /// List of divisor units.
        /// </summary>
        private List<FPU> divs;

        /// <summary>
        /// General Purpose 32 bits registers.
        /// </summary>
        private int[] Registers;

        /// <summary>
        /// Floating point 32 bits registers.
        /// </summary>
        private float[] fRegisters;

        /// <summary>
        /// Special Register to store data fetched.
        /// </summary>
        private BitVector32 IDRegister;

        /// <summary>
        /// The number of cycles runned in the curtent emulation.
        /// </summary>
        public int Cycles { get; private set; }

        private SimManager()
        {
            this.alus = new List<ALU>();
            this.muls = new List<FPU>();
            this.divs = new List<FPU>();

            for (int i = 0; i < ADDUnits; i++)
                this.alus.Add(new ALU());
            for (int i = 0; i < MULUnits; i++)
                this.muls.Add(new FPU());
            for (int i = 0; i < DIVUnits; i++)
                this.divs.Add(new FPU());
            this.PC = 0;
            this.Registers = new int[32];
            this.fRegisters = new float[32];
        }

        /// <summary>
        /// Resizes vRAM
        /// </summary>
        /// <param name="newSize">new memory size</param>
        public static void ResizeMemory(in uint newSize)
        {
            Memsize = newSize;
            Memory.ResizeMemory(newSize);
        }

        /// <summary>
        /// Sets the data forwarding status.
        /// </summary>
        /// <param name="newforwarding">new status for data forwarding</param>
        public static void SetForwarding(in bool newforwarding)
        {
            Forwarding = newforwarding;
        }
        
        /// <summary>
        /// Resets and run the simulation.
        /// </summary>
        public void Restart()
        {
            
        }

        /// <summary>
        /// Clears the pipeline and reloads the last file. 
        /// </summary>
        public void Reset()
        {
            
        }
        
        /// <summary>
        /// Reads the PC address and fetches de data on that memory address.
        /// </summary>
        private void IF()
        {
            byte[] data = {0,0,0,0};
            Memory.MemoryAccess(PC, ref data, false);
            IDRegister = new BitVector32(BitConverter.ToInt32(data));
            ++PC;
        }
        
        /// <summary>
        /// Decodes the instruction to execute.
        /// </summary>
        private void ID()
        {
            
        }
    }
}
