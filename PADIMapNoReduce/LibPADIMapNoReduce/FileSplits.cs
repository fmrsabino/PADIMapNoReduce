using System;

namespace LibPADIMapNoReduce
{
    [Serializable]
    public class FileSplit
    {
        public int splitId;
        public PADIMapNoReduce.Pair<long, long> pair;

        public FileSplit(int i, PADIMapNoReduce.Pair<long, long> pair)
        {
            this.splitId = i;
            this.pair = pair;
        }

    }
}
