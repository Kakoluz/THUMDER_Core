namespace THUMDER.Interpreter
{
    public struct ASM
    {
        public List<string> DataSegment { get; private set; }
        public List<string> CodeSegment { get; private set; }
        public Dictionary<string, string> OriginalText { get; private set; }
        public Dictionary<uint, string> Labels { get; private set; }
        public Dictionary<uint, string> TextLabels { get; private set; }
        public Dictionary<uint, string> GlobalLabels { get; private set; }
        public int dataAddress { get; set; }
        public int textAddress { get; set; }

        public ASM()
        {
            this.DataSegment = new List<string>();
            this.CodeSegment = new List<string>();
            this.OriginalText = new Dictionary<string, string>();
            this.Labels = new Dictionary<uint, string>();
            this.TextLabels = new Dictionary<uint, string>();
            this.GlobalLabels = new Dictionary<uint, string>();
            this.dataAddress = 0;
            this.textAddress = 1000;
        }

        public static bool operator ==(in ASM one, in ASM other)
        {
            if (one.DataSegment.Count == other.DataSegment.Count && one.CodeSegment.Count == other.CodeSegment.Count && one.Labels.Count == other.Labels.Count && one.GlobalLabels.Count == other.GlobalLabels.Count && one.dataAddress == other.dataAddress && one.textAddress == other.textAddress)
            {
                for (int i = 0; i < one.DataSegment.Count; i++)
                {
                    if (!(one.DataSegment[i] == other.DataSegment[i]))
                    {
                        return false;
                    }
                }
                for (int i = 0; i < one.CodeSegment.Count; i++)
                {
                    if (!(one.CodeSegment[i] == other.CodeSegment[i]))
                    {
                        return false;
                    }
                }
                foreach (KeyValuePair<uint, string> entry in one.Labels)
                {
                    if (other.Labels.ContainsKey(entry.Key))
                    {
                        if (!(other.Labels[entry.Key] == entry.Value))
                        {
                            return false;
                        }
                    }
                }
                foreach (KeyValuePair<uint, string> entry in one.GlobalLabels)
                {
                    if (other.GlobalLabels.ContainsKey(entry.Key))
                    {
                        if (!(other.GlobalLabels[entry.Key] == entry.Value))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            else
                return false;
        }
        public static bool operator !=(in ASM one, in ASM other) => !(one == other);

        public override bool Equals(object? obj)
        {
            if (obj != null)
            {
                ASM temp;
                try
                {
                    temp = (ASM)obj;
                }
                catch (Exception)
                {
                    return false;
                }
                return this == temp;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return DataSegment.GetHashCode() + CodeSegment.GetHashCode() + Labels.GetHashCode() + GlobalLabels.GetHashCode();
        }
    }
}
