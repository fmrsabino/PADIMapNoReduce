using System;
using System.Collections.Generic;
using System.Text;

namespace Worker
{
    public interface JobTrackerApi
    {
        void registerJob(string inputFilePath, int nSplits, string outputResultPath);
        void registerWorker(string src);
    }
}
