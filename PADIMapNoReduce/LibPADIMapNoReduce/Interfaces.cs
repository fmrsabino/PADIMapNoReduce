using System.Collections.Generic;
using System.IO;

namespace PADIMapNoReduce {
    public interface IMapper
    {
        IList<KeyValuePair<string, string>> Map(string fileLine);
    }

    public interface IJobTracker
    {
        void registerJob
            (string inputFilePath, int nSplits, string outputResultPath, long nBytes, string clientUrl, byte[] mapperCode, string mapperClassName);
        void registerWorker(string src);
    }

    public interface IWorker : IJobTracker
    {
        void setup(byte[] code, string className, string clientUrl, string filePath);
        void work(Pair<long, long> byteInterval);
    }

    public interface IClient
    {
        // Receives bytes and returns the lines corresponding to those bytes
        List<string> processBytes(Pair<long, long> byteInterval, string filePath);
        //Receives processed data from workers
        void receiveProcessData(byte[] output);
    }
}
