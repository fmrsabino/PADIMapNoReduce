using System;
using System.Collections.Generic;
using System.Text;

namespace Worker
{
    public interface WorkerApi
    {
        void registerWork(int[] splits);
        void registerSplitData(string data, int splitId);
    }
}
