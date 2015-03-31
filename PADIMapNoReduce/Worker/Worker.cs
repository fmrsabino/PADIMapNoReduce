using System;
using System.Collections.Generic;
using System.Reflection;

namespace Worker
{
    class Worker : MarshalByRefObject, PADIMapNoReduce.IWorker
    {
        private List<string> workers = new List<string>();
        private List<PADIMapNoReduce.Pair<int, int>> jobQueue;

        private byte[] mapperCode;
        private string mapperClass;

        /**** WorkerImpl ****/
        public void registerWork(PADIMapNoReduce.Pair<int, int> byteInterval)
        {
            Console.WriteLine("Received job for bytes: " + byteInterval.First + " to " + byteInterval.Second);
        }

        public void registerSplitData(string data, int splitId)
        {
            throw new NotImplementedException();
        }

        public void saveMapper(byte[] code, string className)
        {
            Console.Out.WriteLine("Received code for class " + className);
            mapperCode = code;
            mapperClass = className;
        }

        /**** JobTrackerImpl ****/
        public void registerJob(string inputFilePath, int nSplits, string outputResultPath, int nBytes)
        {
            int nWorkers = workers.Count;

            /*if (nWorkers == 0)
            {
                System.Console.WriteLine("Error: No workers created");
                return;
            }*/

            int splitBytes = nBytes / nSplits;
            int remaindersplitBytes = nBytes % nSplits;

            jobQueue = new List<PADIMapNoReduce.Pair<int, int>>(nSplits);

            for (int i = 0; i < nSplits; i++)
            {
                PADIMapNoReduce.Pair<int, int> pair;
                if (i == nSplits - 1)
                {
                    pair = new PADIMapNoReduce.Pair<int, int>(i * splitBytes + 1, nBytes);
                    System.Console.WriteLine("Added split: " + pair.First + " to " + pair.Second);
                }
                else
                {
                    pair = new PADIMapNoReduce.Pair<int, int>(i * splitBytes + 1, (i + 1) * splitBytes);
                    System.Console.WriteLine("Added split: " + pair.First + " to " + pair.Second);
                }

                jobQueue.Add(pair);
            }

            /*
            // Distribute jobs between workers
            for (int i = 0; i < nWorkers; i++)
            {
                int[] splits = new int[division];
                Array.Copy(splitIds, i * division, splits, 0, division);
                PADIMapNoReduce.IWorker worker = (PADIMapNoReduce.IWorker)Activator.GetObject(typeof(PADIMapNoReduce.IWorker), workers[i] + "/Worker");
                worker.registerWork(splits);
            }
            */
        }

        public void registerWorker(string src)
        {
            if (!workers.Contains(src))
            {
                workers.Add(src);
                System.Console.WriteLine("Registered " + src);
            }
            else
            {
                System.Console.WriteLine(src + " is already registered.");
            }
        }

        public void broadcastMapper(byte[] code, string className)
        {
            foreach (string workerUrl in workers)
            {
                PADIMapNoReduce.IWorker worker = (PADIMapNoReduce.IWorker)Activator.GetObject(typeof(PADIMapNoReduce.IWorker), workerUrl + "/Worker");
                worker.saveMapper(code, className);
            }
        }
    }
}
