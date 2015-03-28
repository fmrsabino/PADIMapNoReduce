using System;
using System.Collections.Generic;
using System.Text;

namespace Worker
{
    interface JobTrackerApi
    {
        void registerJob(string inputFilePath, int nSplits, string outputResultPath);
    }
}
