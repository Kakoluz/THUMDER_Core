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
                case 0x00:
                    result = a + b;
                    break;
                case 0x01:
                    result = a - b;
                    break;
                case 0x02:
                    result = a * b;
                    break;
                case 0x03:
                    result = a / b;
                    break;
                case 0x04:
                    result = -a;
                    break;
                case 0x05:
                    result = Math.Abs(a);
                    break;
                case 0x06:
                    result = Math.Sqrt(a);
                    break;
                case 0x07:
                    result = a % b;
                    break;
            }
            return result;
        }
    }
}