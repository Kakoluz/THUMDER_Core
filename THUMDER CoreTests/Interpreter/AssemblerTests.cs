using Microsoft.VisualStudio.TestTools.UnitTesting;
using THUMDER.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            testing.Labels.Add("table", 4);
            testing.Labels.Add("main", 8);
            testing.Labels.Add("nextvalue", 12);
            testing.Labels.Add("loop", 13);
            testing.Labels.Add("isprim", 23);
            testing.Labels.Add("isnoprim", 28);
            testing.Labels.Add("finish", 30);

            //Add global labels
            testing.GlobalLabels.Add("count", 2);
            testing.GlobalLabels.Add("table", 4);
            testing.GlobalLabels.Add("main", 8);

            //Add data directives
            byte[] aux = BitConverter.GetBytes((int)10);
            foreach (byte b in aux)
                testing.DataSegment.Add(b);
            aux = BitConverter.GetBytes((int)40);
            foreach (byte b in aux)
                testing.DataSegment.Add(b);

            //Add code
            testing.CodeSegemnt.Add("addi r1 r2 0x0");
            testing.CodeSegemnt.Add("addi r16 r0 0x10");
            testing.CodeSegemnt.Add("addi r18 r0 0x0");
            testing.CodeSegemnt.Add("addi r4 r1 0x0");
            testing.CodeSegemnt.Add("seq r4 r1 r3");
            testing.CodeSegemnt.Add("bnez r4 isprim");
            testing.CodeSegemnt.Add("divu r6 r2 r5");
            testing.CodeSegemnt.Add("multu r7 r6 r5");
            testing.CodeSegemnt.Add("subu r8 r2 r7");
            testing.CodeSegemnt.Add("beqz r8 isnoprim");
            testing.CodeSegemnt.Add("addi r3 r3 0x4");
            testing.CodeSegemnt.Add("divu r20 r16 r18");
            testing.CodeSegemnt.Add("divu r22 r16 r18");
            testing.CodeSegemnt.Add("j loop");
            testing.CodeSegemnt.Add("sw 0(r0) r0");
            testing.CodeSegemnt.Add("addi r1 r1 0x4");
            testing.CodeSegemnt.Add("srli r10 r1 0x2");
            testing.CodeSegemnt.Add("sge r11 r10 r9");
            testing.CodeSegemnt.Add("bnez r11 finish");
            testing.CodeSegemnt.Add("j nextvalue");
            testing.CodeSegemnt.Add("trap 0x0");

            return testing;
        }
    }
}