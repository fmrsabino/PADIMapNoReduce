using System.Collections.Generic;

namespace PADIMapNoReduce {
    public interface IMapper
    {
        IList<KeyValuePair<string, string>> Map(string fileLine);
    }

    public interface IJobTracker
    {
        void registerJob(string inputFilePath, int nSplits, string outputResultPath, int nBytes);
        void registerWorker(string src);
        void broadcastMapper(byte[] code, string className);
    }

    public interface IWorker : IJobTracker
    {
        void registerWork(Pair<int, int> byteInterval);
        void registerSplitData(string data, int splitId);
        void saveMapper(byte[] code, string className);
    }
}
