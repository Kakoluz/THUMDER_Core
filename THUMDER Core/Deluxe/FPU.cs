namespace THUMDER.Deluxe
{
    internal class FPU
    {
        public bool busy { get; private set; }
        public bool done { get; private set; }
        public bool ovf { get; private set; }
        private int cyclesRemaining;
        private double a, b;
        private short operation;

        public FPU()
        {
            busy = false;
            done = false;
        }

        public void LoadValues(int a, int b, short op, int time)
        {
            this.a = a;
            this.b = b;
            this.cyclesRemaining = time;
            this.done = false;
            this.operation = op;
            this.busy = true;
            this.ovf = false;
        }

        public void DoTick()
        {
            if (busy)
            {
                cyclesRemaining--;
                if (cyclesRemaining == 0)
                {
                    busy = false;
                    done = true;
                }
            }
        }

        public double? GetValue()
        {
            double? result = null;
            switch (operation)
            {
                case 0: //ADDF
                case 4: //ADDD
                    result = a + b;
                    break;
                case 1: //SUBF
                case 5: //SUBD
                    result = a - b;
                    break;
                case 14://MULT  integers are operated in the fpu and truncated.
                case 22://MULTU unsigned are operated in the fpu and truncated.
                case 2: //MULTF
                case 6: //MULTD
                    result = a * b;
                    break;
                case 15://DIV  integers are operated in the fpu and truncated.
                case 23://DIVU unsigned are operated in the fpu and truncated.
                case 3: //DIVF
                case 7: //DIVD
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
            return cyclesRemaining == 0 ? result : null;
        }
    }
}