using System;
using System.Collections.Generic;
using System.Text;

namespace Worker
{
    class Worker : MarshalByRefObject, WorkerApi, JobTrackerApi
    {

        private List<string> workers = new List<string>();
        private int[] jobQueue;

        /**** WorkerImpl ****/
        public void registerWork(int[] splits)
        {
            string splitsText = "";
            foreach (int splitId in splits) {
                splitsText += splitId + " ";
            }
            Console.WriteLine("Received job for splits: " + splitsText);
        }

        public void registerSplitData(string data, int splitId)
        {
            throw new NotImplementedException();
        }


        /**** JobTrackerImpl ****/
        public void registerJob(string inputFilePath, int nSplits, string outputResultPath)
        {
            int nWorkers = workers.Count;

            if (nWorkers == 0) {
                System.Console.WriteLine("Error: No workers created");
                return;
            }

            // Create split id's array
            int[] splitIds = new int[nSplits];
            for(int i = 0; i < nSplits; i++) {
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
                Array.Copy(splitIds, i*division, splits, 0, division);
                WorkerApi worker = (WorkerApi)Activator.GetObject(typeof(WorkerApi), workers[i] + "/Worker");
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
    }
}
