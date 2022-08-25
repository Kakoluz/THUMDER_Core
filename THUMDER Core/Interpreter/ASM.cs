using System.Data.Common;
using System.Runtime.CompilerServices;

namespace THUMDER.Interpreter
{
    public struct ASM
    {
        public List<string> DataSegment { get; private set; }
        public List<string> CodeSegment { get; private set; }
        public Dictionary<string, uint> Labels { get; private set; }
        public Dictionary<string, uint> GlobalLabels { get; private set; }

        public ASM()
        {
            this.DataSegment = new List<string>();
            this.CodeSegment = new List<string>();
            this.Labels = new Dictionary<string, uint>();
            this.GlobalLabels = new Dictionary<string, uint>();
        }

        public static bool operator ==(in ASM one, in ASM other)
        {
            if(one.DataSegment.Count == other.DataSegment.Count && one.CodeSegment.Count == other.CodeSegment.Count && one.Labels.Count == other.Labels.Count && one.GlobalLabels.Count == other.GlobalLabels.Count)
            {
                for(int i = 0; i< one.DataSegment.Count; i ++)
                {
                    if(!(one.DataSegment[i] == other.DataSegment[i]))
                    {
                        return false;
                    }
                }
                for (int i = 0; i < one.CodeSegment.Count; i++)
                {
                    if(!(one.CodeSegment[i] == other.CodeSegment[i]))
                    {
                        return false;
                    }
                }
                foreach (KeyValuePair<string, uint> entry in one.Labels)
                {
                    if (other.Labels.ContainsKey(entry.Key))
                    {
                        if(!(other.Labels[entry.Key] == entry.Value))
                        {
                            return false;
                        }
                    }
                }
                foreach (KeyValuePair<string, uint> entry in one.GlobalLabels)
                {
                    if (other.GlobalLabels.ContainsKey(entry.Key))
                    {
                        if(!(other.GlobalLabels[entry.Key] == entry.Value))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
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
