namespace THUMDER.Deluxe
{    
    internal class ALU
    {
        public bool busy { get; private set; }
        public bool done { get; private set; }
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
            switch (operation)
            {
                //Arithmetic Logical Operations
                case 8:  //ADDI
                case 9:  //ADDUI
                case 32: //ADD
                case 33: //ADDU
                    result = a + b;
                    break;
                case 10: //SUBI
                case 11: //SUBUI
                case 34: //SUB
                case 35: //SUBU
                    result = a - b;
                    break;
                case 12: //ANDI
                case 36: //AND
                    result = a & b;
                    break;
                case 13: //ORI
                case 37: //OR
                    result = a | b;
                    break;
                case 14: //XORI
                case 38: //XOR
                    result = a ^ b;
                    break;
                //Test Set Operations.
                case 27: //SGTI
                case 43: //SGT
                    result = (a > b ? 1 : 0);
                    break;
                case 24: //SEQI
                case 40: //SEQ
                    result = (a == b ? 1 : 0);
                    break;
                case 29: //SGEI
                case 45: //SGE
                    result = (a >= b ? 1 : 0);
                    break;
                case 26: //SLTI
                case 42: //SLT
                    result = (a < b ? 1 : 0);
                    break;
                case 25: //SNEI
                case 41: //SNE
                    result = (a != b ? 1 : 0);
                    break;
                case 28: //SLEI
                case 44: //SLE
                    result = (a <= b ? 1 : 0);
                    break;
                //Bit shift operations.
                case 20: //SLLI
                case 4:  //SLL
                    result = a << b;
                    break;
                case 22: //SRLI
                case 6:  //SRL
                    result = a >> b;
                    break;
                case 23: //SRAI
                case 7:  //SRA
                    //TODO
                    break;                  
            }
            return result;
        }
    }
}
