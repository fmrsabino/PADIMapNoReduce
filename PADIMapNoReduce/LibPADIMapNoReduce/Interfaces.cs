using System.Collections.Generic;

namespace PADIMapNoReduce {
    public interface IMapper
    {
        IList<KeyValuePair<string, string>> Map(string fileLine);
    }

    public interface IMapperTransfer
    {
        bool SendMapper(byte[] code, string className);
    }

    public interface IWorker : IMapperTransfer
    {
        void registerWork(int[] splits);
        void registerSplitData(string data, int splitId);
    }

    public interface IJobTracker
    {
        void registerJob(string inputFilePath, int nSplits, string outputResultPath);
        void registerWorker(string src);
    }
}
