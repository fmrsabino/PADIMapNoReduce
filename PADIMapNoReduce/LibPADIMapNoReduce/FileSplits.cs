using System;

namespace LibPADIMapNoReduce
{
    [Serializable]
    public class FileSplits
    {
        public int nrSplits;
        public PADIMapNoReduce.Pair<long, long> pair;

        public FileSplits(int i, PADIMapNoReduce.Pair<long, long> pair)
        {
            this.nrSplits = i;
            this.pair = pair;
        }

    }
}
