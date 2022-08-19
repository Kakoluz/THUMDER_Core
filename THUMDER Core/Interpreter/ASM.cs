namespace THUMDER.Interpreter
{
    internal struct ASM
    {
        public List<byte> DataSegment { get; private set; }
        public List<uint> CodeSegemnt { get; private set; }
        public Dictionary<string, uint> Labels { get; private set; }
        public List<string> GlobalLabels { get; private set; }

        public ASM()
        {
            this.DataSegment = new List<byte>();
            this.CodeSegemnt = new List<uint>();
            this.Labels = new Dictionary<string, uint>();
            this.GlobalLabels = new List<string>();
        }
    }
}
