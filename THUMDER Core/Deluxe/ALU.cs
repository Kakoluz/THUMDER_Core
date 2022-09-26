namespace THUMDER.Deluxe
{    
    internal class ALU
    {
        public bool Busy { get; private set; }
        public bool Done { get; private set; }
        private int cyclesRemaining;
        private int a, b, c;
        private short operation;

        public ALU()
        {
            Busy = false;
            Done = false;
        }

        /// <summary>
        /// Load values in the internal ALU register to do the operations.
        /// </summary>
        /// <param name="a">Fist operand.</param>
        /// <param name="b">Second operand.</param>
        /// <param name="op">Operation to execute.</param>
        /// <exception cref="Exception">If the ALU is already loaded with data and working.</exception>
        public void LoadValues(int c, int a, int b, int op)
        {
            if (!Busy)
            {
                this.c = c;
                this.a = a;
                this.b = b;
                this.cyclesRemaining = 1; //All ALU operations are 1 cycle long.
                this.Done = false;
                this.operation = (short)op;
                this.Busy = true;
            }
            else
                throw new Exception("ALU is currently working.");
        }

        /// <summary>
        /// Ticks 1 clock cycle.
        /// </summary>
        public void DoTick()
        {
            if (Busy)
            {
                cyclesRemaining--;
                if (cyclesRemaining == 0)
                {
                    Busy = false;
                    Done = true;
                }
            }
        }

        /// <summary>
        /// Will return the result of the loaded operation when it finishes.
        /// </summary>
        /// <returns>The value or null if its not ready.</returns>
        public int? GetValue(out int dest)
        {
            dest = c;
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
                    result = a;
                    for (int i = 0; i < b; i++)
                        result /=  2; //C# does not implement the arithmetic shift so dividing by 2 does the job.
                    break;                  
            }
            bool returnVal = Done;
            Done = false;
            return returnVal ? result : null;
        }
    }
}
