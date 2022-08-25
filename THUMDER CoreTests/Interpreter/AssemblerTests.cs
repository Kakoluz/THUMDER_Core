using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace THUMDER.Interpreter.Tests
{
    [TestClass()]
    public class AssemblerTests
    {
        [TestMethod()]
        public void DecodeTest()
        {
            ASM actual = Assembler.Decode(File.ReadAllLines("../../../test.dlx"));
            ASM expected = testASM();
            Assert.AreEqual(expected, actual);
        }

        private ASM testASM()
        {
            ASM testing = new ASM();
            //Add labels
            testing.Labels.Add("count", 2);
            testing.Labels.Add("table", 5);
            testing.Labels.Add("main", 9);
            testing.Labels.Add("nextvalue", 13);
            testing.Labels.Add("loop", 14);
            testing.Labels.Add("isprim", 24);
            testing.Labels.Add("isnoprim", 29);
            testing.Labels.Add("finish", 31);

            //Add global labels
            testing.GlobalLabels.Add("count", 2);
            testing.GlobalLabels.Add("table", 5);
            testing.GlobalLabels.Add("main", 9);

            //Add data directives
            byte[] aux = BitConverter.GetBytes((int)10);
            foreach (byte b in aux)
                testing.DataSegment.Add(b);
            for (int i = 0; i < 40*4; i++)
                testing.DataSegment.Add(0x0);

            //Add code
            testing.CodeSegment.Add("addi 1 2 0x0");
            testing.CodeSegment.Add("addi 2 0 0x2");
            testing.CodeSegment.Add("addi 16 0 0x10");
            testing.CodeSegment.Add("addi 18 0 0x0");
            testing.CodeSegment.Add("addi 4 1 0x0");
            testing.CodeSegment.Add("seq 4 1 3");
            testing.CodeSegment.Add("bnez 4 isprim");
            testing.CodeSegment.Add("divu 6 2 5");
            testing.CodeSegment.Add("multu 7 6 5");
            testing.CodeSegment.Add("subu 8 2 7");
            testing.CodeSegment.Add("beqz 8 isnoprim");
            testing.CodeSegment.Add("addi 3 3 0x4");
            testing.CodeSegment.Add("divu 20 16 18");
            testing.CodeSegment.Add("divu 22 16 18");
            testing.CodeSegment.Add("j loop");
            testing.CodeSegment.Add("sw 0(r0) 0");
            testing.CodeSegment.Add("addi 1 1 0x4");
            testing.CodeSegment.Add("srli 10 1 0x2");
            testing.CodeSegment.Add("sge 11 10 9");
            testing.CodeSegment.Add("bnez 11 finish");
            testing.CodeSegment.Add("addi 2 2 0x1");
            testing.CodeSegment.Add("j nextvalue");
            testing.CodeSegment.Add("trap 0x0");

            return testing;
        }
    }
}