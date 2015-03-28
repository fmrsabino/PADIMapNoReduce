using System;
using System.Collections.Generic;
using System.Text;

namespace Worker
{
    class Worker : MarshalByRefObject, WorkerApi, JobTrackerApi
    {

        private List<string> workers = new List<string>();

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
            
            //TODO: Check remainder
            int division = nSplits / nWorkers;
            int workerIndex = 0;
            for (int i = 0; i < nSplits; i += division)
            {
                int[] splits = new int[division];
                Array.Copy(splitIds, i, splits, 0, division);
                
                WorkerApi worker = (WorkerApi) Activator.GetObject(typeof(WorkerApi), workers[workerIndex] + "/Worker");
                worker.registerWork(splits);
                workerIndex++;
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
