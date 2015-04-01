using System.Collections.Generic;

namespace PADIMapNoReduce {
    public interface IMapper
    {
        IList<KeyValuePair<string, string>> Map(string fileLine);
    }

    public interface IJobTracker
    {
        void registerJob
            (string inputFilePath, int nSplits, string outputResultPath, int nBytes, string clientUrl, byte[] mapperCode, string mapperClassName);
        void registerWorker(string src);
    }

    public interface IWorker : IJobTracker
    {
        void setup(byte[] code, string className, string clientUrl);
        void work(Pair<int, int> byteInterval);
    }

    public interface IClient
    {
        // Receives bytes and returns the lines corresponding to those bytes
        int[] processBytes(Pair<int, int> byteInterval);
    }
}
