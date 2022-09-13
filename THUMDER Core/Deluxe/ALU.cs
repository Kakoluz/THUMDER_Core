namespace THUMDER.Deluxe
{    
    internal class ALU
    {
        public bool busy { get; private set; }
        public bool done { get; private set; }
        public bool ovf { get; private set; }
        private int cyclesRemaining;
        private int a, b;
        private short operation;

        public ALU()
        {
            busy = false;
            done = false;
        }
        
        public void LoadValues(int a, int b, short op)
        {
            this.a = a;
            this.b = b;
            this.cyclesRemaining = 1;
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

        public int? GetValue()
        {
            int? result = null;
            int foo;
            switch (operation)
            {
                //Arithmetic Logical Operations
                case 0x08:                   
                case 0x20:
                    result = a + b;
                    try
                    {
                        foo = checked(a + b);
                    }
                    catch (OverflowException)
                    {
                        ovf = true;
                    }
                    break;
                case 0x09:
                case 0x21:
                    result = a + b;
                    break;
                case 0x0A:
                case 0x22:
                    result = a - b;
                    try
                    {
                        foo = checked(a - b);
                    }
                    catch (OverflowException)
                    {
                        ovf = true;
                    }
                    break;
                case 0x0B:
                case 0x23:
                    result = a - b;
                    break;
                case 0x0C:
                case 0x24:
                    result = a & b;
                    break;
                case 0x0D:
                case 0x25:
                    result = a | b;
                    break;
                case 0x0E:
                case 0x26:
                    result = a ^ b;
                    break;
                case 0x0F:
                case 0x27:
                    //TODO
                    break;
                //Test Set Operations.
                case 0x18:
                case 0x28:
                    result = 0;
                    break;
                case 0x19:
                case 0x29:
                    result = (a > b ? 1 : 0);
                    break;
                case 0x1A:
                case 0x2A:
                    result = (a == b ? 1 : 0);
                    break;
                case 0x1B:
                case 0x2B:
                    result = (a >= b ? 1 : 0);
                    break;
                case 0x1C:
                case 0x2C:
                    result = (a < b ? 1 : 0);
                    break;
                case 0x1D:
                case 0x2D:
                    result = (a != b ? 1 : 0);
                    break;
                case 0x1E:
                case 0x2E:
                    result = (a <= b ? 1 : 0);
                    break;
                case 0x1F:
                case 0x2F:
                    result = 1;
                    break;
            }
            return result;
        }
    }
}
