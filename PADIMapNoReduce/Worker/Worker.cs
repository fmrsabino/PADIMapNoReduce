using System;
using System.Collections.Generic;
using System.Reflection;

namespace Worker
{
    class Worker : MarshalByRefObject, PADIMapNoReduce.IWorker
    {
        private List<string> workers = new List<string>();
        private Queue<PADIMapNoReduce.Pair<int, int>> jobQueue;

        private byte[] mapperCode;
        private string mapperClass;
        private string clientUrl;

        /**** WorkerImpl ****/

        public void setup(byte[] code, string className, string clientUrl)
        {
            Console.Out.WriteLine("Received code for class " + className);
            mapperCode = code;
            mapperClass = className;
            this.clientUrl = clientUrl;
        }

        public void work(PADIMapNoReduce.Pair<int, int> byteInterval)
        {
            Console.WriteLine("Received job for bytes: " + byteInterval.First + " to " + byteInterval.Second);
            if (clientUrl != null)
            {
                PADIMapNoReduce.IClient client =
                    (PADIMapNoReduce.IClient)Activator.GetObject(typeof(PADIMapNoReduce.IClient), clientUrl);

                client.processBytes(byteInterval);
            }
            else
            {
                Console.WriteLine("Worker is not set");
            }
            
        }

        /**** JobTrackerImpl ****/
        public void registerJob
            (string inputFilePath, int nSplits, string outputResultPath, int nBytes, string clientUrl, byte[] mapperCode, string mapperClassName)
        {
            int splitBytes = nBytes / nSplits;

            jobQueue = new Queue<PADIMapNoReduce.Pair<int, int>>();

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

                jobQueue.Enqueue(pair);
            }

            //Distribute to each worker one split
            foreach (string workerUrl in workers)
            {
                if (jobQueue.Count == 0)
                {
                    //No more work to distribute
                    break;
                }
                PADIMapNoReduce.IWorker worker = 
                    (PADIMapNoReduce.IWorker)Activator.GetObject(typeof(PADIMapNoReduce.IWorker), workerUrl + "/Worker");
                worker.setup(mapperCode, mapperClassName, clientUrl);
                worker.work(jobQueue.Dequeue());
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
    }
}
