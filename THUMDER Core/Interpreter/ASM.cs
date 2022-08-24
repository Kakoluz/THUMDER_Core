namespace THUMDER.Interpreter
{
    public struct ASM
    {
        public List<byte> DataSegment { get; private set; }
        public List<string> CodeSegemnt { get; private set; }
        public Dictionary<string, uint> Labels { get; private set; }
        public Dictionary<string, uint> GlobalLabels { get; private set; }

        public ASM()
        {
            this.DataSegment = new List<byte>();
            this.CodeSegemnt = new List<string>();
            this.Labels = new Dictionary<string, uint>();
            this.GlobalLabels = new Dictionary<string, uint>();
        }
    }
}
