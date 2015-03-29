using System;
using System.Collections.Generic;
using System.Reflection;

namespace Worker
{
    class Worker : MarshalByRefObject, PADIMapNoReduce.IWorker
    {
        private List<string> workers = new List<string>();
        private int[] jobQueue;

        private byte[] mapperCode;
        private string mapperClass;

        /**** WorkerImpl ****/
        public void registerWork(int[] splits)
        {
            string splitsText = "";
            foreach (int splitId in splits)
            {
                splitsText += splitId + " ";
            }
            Console.WriteLine("Received job for splits: " + splitsText);
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
        public void registerJob(string inputFilePath, int nSplits, string outputResultPath)
        {
            int nWorkers = workers.Count;

            if (nWorkers == 0)
            {
                System.Console.WriteLine("Error: No workers created");
                return;
            }

            // Create split id's array
            int[] splitIds = new int[nSplits];
            for (int i = 0; i < nSplits; i++)
            {
                splitIds[i] = i;
            }

            int division = nSplits / nWorkers;
            int remainder = nSplits % nWorkers;

            if (remainder > 0) //Save remaining job
            {
                jobQueue = new int[remainder];
                int startIndex = nWorkers * division;
                Array.Copy(splitIds, startIndex, jobQueue, 0, remainder);

                //Print jobQueue
                string jobQueueText = "";
                foreach (int splitId in jobQueue)
                {
                    jobQueueText += splitId + " ";
                }
                System.Console.WriteLine("Job Remainder: " + jobQueueText);
            }

            // Distribute jobs between workers
            for (int i = 0; i < nWorkers; i++)
            {
                int[] splits = new int[division];
                Array.Copy(splitIds, i * division, splits, 0, division);
                PADIMapNoReduce.IWorker worker = (PADIMapNoReduce.IWorker)Activator.GetObject(typeof(PADIMapNoReduce.IWorker), workers[i] + "/Worker");
                worker.registerWork(splits);
            }
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
