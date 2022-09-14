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
            bool a = expected == actual;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void DataSyntaxErrorTest()
        {
            try
            {
                string[] test = {".data", ".aspace 5", ".text" };
                Assembler.Decode(test);
            }
            catch (ArgumentException e)
            {
                Assert.IsTrue(true);
            }
            catch(Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod()]
        public void CodeSyntaxErrorTest()
        {
            try
            {
                string[] test = { ".text", "addi r1 r2 r3", "j 2 5" };
                Assembler.Decode(test);
            }
            catch (ArgumentException e)
            {
                Assert.IsTrue(true);
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        private ASM testASM()
        {
            ASM testing = new ASM();
            //Add labels
            testing.Labels.Add(2, "count");
            testing.Labels.Add(4, "table");
            testing.Labels.Add(8, "main");
            testing.Labels.Add(12, "nextvalue");
            testing.Labels.Add(13, "loop");
            testing.Labels.Add(23, "isprim");
            testing.Labels.Add(28, "isnoprim");
            testing.Labels.Add(30, "finish");

            //Add global labels
            testing.GlobalLabels.Add(2, "count");
            testing.GlobalLabels.Add(4, "table");
            testing.GlobalLabels.Add(8, "main");

            //Add data directives
            testing.DataSegment.Add(".word 10");
            testing.DataSegment.Add(".space count*4");

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