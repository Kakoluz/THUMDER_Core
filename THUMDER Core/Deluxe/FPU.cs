using System.Collections.Specialized;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;

namespace THUMDER.Deluxe
{
    internal class FPU
    {
        public bool Busy { get; private set; }
        public bool Done { get; private set; }
        private bool isDouble;
        private int cyclesRemaining;
        private double a, b, result;
        private int c, dest;
        private short operation;

        public FPU()
        {
            Busy = false;
            Done = false;
        }

        /// <summary>
        /// Load values in the internal FPU register to do the operations.
        /// </summary>
        /// <param name="a">Fist operand.</param>
        /// <param name="b">Second operand.</param>
        /// <param name="op">Operation to execute.</param>
        /// <param name="time">Clock cycles duration of operation.</param>
        /// <exception cref="Exception">If the FPU is already loaded with data and working.</exception>
        public void LoadValues(int c, double a, double b, int op, int time)
        {
            if (!Busy)
            {
                this.c = c;
                this.a = a;
                this.b = b;
                this.cyclesRemaining = time;
                this.operation = (short)op;
                this.Busy = true;
            }
            else
                throw new Exception("FPU is currently working.");
        }

        /// <summary>
        /// Ticks 1 clock cycle.
        /// </summary>
        public void DoTick()
        {
            Done = false;
            if (Busy)
            {
                cyclesRemaining--;
                if (cyclesRemaining == 0)
                {
                    dest = DoOperation();
                    Busy = false;
                }
            }
        }

        /// <summary>
        /// Will return the result of the loaded operation if its finished.
        /// </summary>
        /// <param name="dest">Register to store the result.</param>
        /// <param name="isDouble">Is the value a double?</param>
        /// <returns>The operated value or null if its not ready</returns>
        public double? GetValue(out int dest, out bool isDouble)
        {
            isDouble = this.isDouble;
            dest = this.dest;
            return Done ? result : null;
        }
        /// <summary>
        /// Will do the operation and set the Done flag to true.
        /// </summary>
        private int DoOperation()
        {
            switch (operation)
            {
                case 0: //ADDF
                    result = a + b;
                    break;
                case 4: //ADDD
                    isDouble = true;
                    result = a + b;
                    break;
                case 1: //SUBF
                    result = a - b;
                    break;
                case 5: //SUBD
                    isDouble = true;
                    result = a - b;
                    break;
                case 14://MULT  integers are operated in the fpu and truncated.
                case 22://MULTU unsigned are operated in the fpu and truncated.
                case 2: //MULTF
                    result = a / b;
                    break;
                case 6: //MULTD
                    isDouble = true;
                    result = a * b;
                    break;
                case 15://DIV  integers are operated in the fpu and truncated.
                case 23://DIVU unsigned are operated in the fpu and truncated.
                case 3: //DIVF
                    result = a / b;
                    break;
                case 7: //DIVD
                    isDouble = true;
                    result = a / b;
                    break;
                case 19: //GTF
                case 27: //GTD
                    result = (a > b ? 1 : 0);
                    break;
                case 16: //EQF
                case 24: //EQD
                    result = (a == b ? 1 : 0);
                    break;
                case 21: //GEF
                case 29: //GED
                    result = (a >= b ? 1 : 0);
                    break;
                case 18: //LTF
                case 26: //LTD
                    result = (a < b ? 1 : 0);
                    break;
                case 17: //NEF
                case 25: //NED
                    result = (a != b ? 1 : 0);
                    break;
                case 20: //LEF
                case 28: //LED
                    result = (a <= b ? 1 : 0);
                    break;
            }
            Done = true;
            return c;
        }
    }
}