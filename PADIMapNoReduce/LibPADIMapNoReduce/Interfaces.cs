﻿using LibPADIMapNoReduce;
using System;
using System.Collections.Generic;

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
        void registerImAlive(string workerUrl);
        void printStatus();
        void freezec();
        void unfreezec();
        void checkWorkerStatus(Object state);
    }

    public interface IWorker : IJobTracker
    {
        void setup(byte[] code, string className, string clientUrl, string filePath);
        void work(FileSplits fileSplits);
        void sendImAlive(Object state);
        void sloww(int seconds);
        void freezew();
        void unfreezew();
        bool isAlive();
    }

    public interface IClient
    {
        // Receives bytes and returns the lines corresponding to those bytes
        List<byte> processBytes(Pair<long, long> byteInterval, string filePath);
        //Receives processed data from workers
        void receiveProcessData(string lines, int nrSplit);
        //Receives notification from JobTracker that job has concluded
        void jobConcluded();
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
