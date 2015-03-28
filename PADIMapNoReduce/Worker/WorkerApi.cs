using System;
using System.Collections.Generic;
using System.Text;

namespace Worker
{
    interface WorkerApi
    {
        void registerWork(int[] splits);
        void registerSplitData(string data);
    }
}
