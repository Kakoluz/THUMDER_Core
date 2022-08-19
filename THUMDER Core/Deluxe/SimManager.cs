namespace THUMDER.Deluxe
{
    internal sealed class SimManager
    {

        //Set C# auto properties for emulation settings and initialize to WinDLX default values
        /// <summary>
        /// Data forwarding status.
        /// </summary>
        public static bool Forwarding { get; set; } = true;

        /// <summary>
        /// Size of the memory.
        /// </summary>
        public static int Memsize { get; set; } = 32768;

        /// <summary>
        /// Number of summator units for floating point.
        /// </summary>
        public static int ADDUnits { get; set; } = 1;
        /// <summary>
        /// Number of multiplier units for floating point.
        /// </summary>
        public static int MULUnits { get; set; } = 1;
        /// <summary>
        /// Number of divisor units for floating point.
        /// </summary>
        public static int DIVUnits { get; set; } = 1;
        
        /// <summary>
        /// Number of cicles for a floating point add operation.
        /// </summary>
        public static int ADDDelay  { get; set; } = 2;
        /// <summary>
        /// Number of cicles for a floating point multiplication operation.
        /// </summary>
        public static int MULDDelay { get; set; } = 5;
        /// <summary>
        /// Number of cicles for a floatig point division operation.
        /// </summary>
        public static int DIVDelay { get; set; } = 19;

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

        private SimManager()
        {
            
        }
    }
}
