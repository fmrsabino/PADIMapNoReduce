using LibPADIMapNoReduce;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace PADIMapNoReduce {
    public interface IMapper
    {
        IList<KeyValuePair<string, string>> Map(string fileLine);
    }

    public interface IJobTracker
    {
        void registerJob
            (string inputFilePath, int nSplits, string outputResultPath, long nBytes, string clientUrl, byte[] mapperCode, string mapperClassName);
        bool registerWorker(string src);
        void printStatus();
        void freezec();
        void unfreezec();
        void checkWorkerStatus(Object state);
        void notifySplitFinish(string workerUrl,LibPADIMapNoReduce.FileSplit fileSplits);
        void removeJobTracker(string jobTracker);
        void updateWorkers(string workerUrl);
        void updateJobTrackers(string workerUrl);
        bool canSendProcessedData();
    }

    public interface IWorker : IJobTracker
    {
        void setup(byte[] code, string className, string clientUrl, string filePath);
        [OneWay()]
        void work(FileSplit fileSplits);
        void sloww(int seconds);
        void freezew();
        void unfreezew();
        bool isAlive(List<string> jobTrackers, List<string> workers, LibPADIMapNoReduce.FileSplit[] jobQueue, Dictionary<string, LibPADIMapNoReduce.FileSplit> onGoingWork);
    }

    public interface IClient
    {
        // Receives bytes and returns the lines corresponding to those bytes
        List<byte> processBytes(Pair<long, long> byteInterval, string filePath);
        //Receives processed data from workers
        void receiveProcessData(string lines, int nrSplit);
        //Receives notification from JobTracker that job has concluded
        void jobConcluded();
        void removeFile(int splitId);
    }

    public interface IPuppetMaster
    {
        bool startWorker(int id, string serviceURL, string entryURL);
        void printStatus();
        void freezec(int id);
        void unfreezec(int id);
        void sloww(int id, int seconds);
        void freezew(int id);
        void unfreezew(int id);
    }

}
