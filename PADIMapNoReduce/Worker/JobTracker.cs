using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Worker
{
    public partial class Worker : MarshalByRefObject, PADIMapNoReduce.IWorker
    {
        private List<string> workers = new List<string>();
        private ConcurrentQueue<LibPADIMapNoReduce.FileSplit> jobQueue;
        private Dictionary<string, LibPADIMapNoReduce.FileSplit> onGoingWork = new Dictionary<string, LibPADIMapNoReduce.FileSplit>();

        private Timer timer;
        private const long ALIVE_TIME_INTERVAL_IN_MS = 10000;

        public Worker(string jobTrackerUrl)
        {
            this.url = jobTrackerUrl;
            CURRENT_STATUS = STATUS.JOBTRACKER_WAITING; // For STATUS command of PuppetMaster
            jobtrackerMonitor = new object();
            timer = new Timer(checkWorkerStatus, null, ALIVE_TIME_INTERVAL_IN_MS, Timeout.Infinite);
        }

        public void registerJob
           (string inputFilePath, int nSplits, string outputResultPath, long nBytes, string clientUrl, byte[] mapperCode, string mapperClassName)
        {
            handleFreezeJobTracker(); // For handling FREEZEC from PuppetMaster
            CURRENT_STATUS = STATUS.JOBTRACKER_WORKING; // For STATUS command of PuppetMaster
            this.clientUrl = clientUrl;

            if (nSplits == 0)
            {
                CURRENT_STATUS = STATUS.JOBTRACKER_WAITING;
                return;
            }

            long splitBytes = nBytes / nSplits;

            jobQueue = new ConcurrentQueue<LibPADIMapNoReduce.FileSplit>();

            for (int i = 0; i < nSplits; i++)
            {
                PADIMapNoReduce.Pair<long, long> pair;
                if (i == nSplits - 1)
                {
                    pair = new PADIMapNoReduce.Pair<long, long>(i * splitBytes, nBytes);
                    System.Console.WriteLine("Added split: " + pair.First + " to " + pair.Second);
                }
                else
                {
                    pair = new PADIMapNoReduce.Pair<long, long>(i * splitBytes, (i + 1) * splitBytes - 1);
                    System.Console.WriteLine("Added split: " + pair.First + " to " + pair.Second);
                }


                jobQueue.Enqueue(new LibPADIMapNoReduce.FileSplit(i, pair));
            }

            while (workers.Count == 0) { }

            //Distribute to each worker one split
            for (int i = 0; i < workers.Count; i++)
            {
                if (jobQueue.IsEmpty)
                {
                    break;
                }

                string workerUrl = workers[i];
                PADIMapNoReduce.IWorker worker =
                        (PADIMapNoReduce.IWorker)Activator.GetObject(typeof(PADIMapNoReduce.IWorker), workerUrl);
                worker.setup(mapperCode, mapperClassName, clientUrl, inputFilePath);

                LibPADIMapNoReduce.FileSplit job = null;
                if (jobQueue.TryDequeue(out job))
                {
                    if (onGoingWork.ContainsKey(workerUrl)) //UPDATE
                    {
                        onGoingWork[workerUrl] = job;
                    }
                    else //ADD
                    {
                        onGoingWork.Add(workerUrl, job);
                    }
                    worker.work(job);
                }
            }

            try
            {
                PADIMapNoReduce.IClient client =
                    (PADIMapNoReduce.IClient)Activator.GetObject(typeof(PADIMapNoReduce.IClient), clientUrl);
                client.jobConcluded();
                System.Console.WriteLine("////////////JOB CONCLUDED/////////////////");
                CURRENT_STATUS = STATUS.JOBTRACKER_WAITING; // For STATUS command of PuppetMaster
            }
            catch (Exception e)
            {
                System.Console.WriteLine("EXCEPTION: " + e.Message);
                CURRENT_STATUS = STATUS.JOBTRACKER_WAITING; // For STATUS command of PuppetMaster
                return;
            }
        }

        //Thread for each worker
        private void sendWork(Object obj)
        {
            KeyValuePair<PADIMapNoReduce.IWorker, ManualResetEvent> pair = (KeyValuePair<PADIMapNoReduce.IWorker, ManualResetEvent>)obj;
            PADIMapNoReduce.IWorker worker = pair.Key;
            //TODO: USE LOCAL WORKER URL SINCE JT HAVE THEM ALL AND DO NOT MAKE THIS REMOTE CALL
            string workerUrl = worker.getUrl();

            while (jobQueue.Count > 0)
            {
                LibPADIMapNoReduce.FileSplit job = null;

                if (jobQueue.TryDequeue(out job))
                {
                    try
                    {

                        if (onGoingWork.ContainsKey(workerUrl)) //UPDATE
                        {
                            onGoingWork[workerUrl] = job;
                        }
                        else //ADD
                        {
                            onGoingWork.Add(workerUrl, job);
                        }
                        worker.work(job);
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        // The worker is probably down but it'll be removed when the job tracker checks if they are alive or not
                        break; //Finish thread execution
                    }
                }
            }
            //thread "notify" jobtracker that jobqueue is empty
            pair.Value.Set();
        }

        public bool registerWorker(string workerUrl)
        {
            handleFreezeJobTracker(); // For handling FREEZEC from PuppetMaster
            if (!workers.Contains(workerUrl))
            {
                workers.Add(workerUrl);
                System.Console.WriteLine("Registered " + workerUrl);
                return true;
            }
            else
            {
                System.Console.WriteLine(workerUrl + " is already registered.");
                return false;
            }
        }

        public void registerImAlive(string workerUrl)
        {
            Console.WriteLine("I'M ALIVE from " + workerUrl);
        }

        public void checkWorkerStatus(Object state)
        {
            List<string> deadWorkers = new List<string>();
            for (int i = 0; i < workers.Count; i++)
            {
                PADIMapNoReduce.IWorker worker =
                        (PADIMapNoReduce.IWorker)Activator.GetObject(typeof(PADIMapNoReduce.IWorker), workers[i]);
                try
                {
                    worker.isAlive(workers);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    Console.WriteLine("Couldn't reach {0}. Marking as dead!", workers[i]);
                    deadWorkers.Add(workers[i]);

                    LibPADIMapNoReduce.FileSplit split;
                    if (onGoingWork.TryGetValue(workers[i], out split))
                    {
                        // This means that the worker was working on the split
                        // TODO: Request client to remove previous unfinished split result 
                        PADIMapNoReduce.IClient client =
                    (PADIMapNoReduce.IClient)Activator.GetObject(typeof(PADIMapNoReduce.IClient), clientUrl);
                        client.removeFile(split.splitId);
                        jobQueue.Enqueue(split);
                    }
                }
            }

            workers.RemoveAll(x => deadWorkers.Contains(x));

            timer.Change(ALIVE_TIME_INTERVAL_IN_MS, Timeout.Infinite);
        }

        public void notifySplitFinish(string workerUrl, LibPADIMapNoReduce.FileSplit fileSplit)
        {
            Console.WriteLine("Worker {0} finished split {1}", workerUrl, fileSplit.splitId);

            PADIMapNoReduce.IWorker worker =
                        (PADIMapNoReduce.IWorker)Activator.GetObject(typeof(PADIMapNoReduce.IWorker), workerUrl);

            LibPADIMapNoReduce.FileSplit job = null;
            if (jobQueue.TryDequeue(out job))
            {
                try
                {
                    if (onGoingWork.ContainsKey(workerUrl)) //UPDATE
                    {
                        onGoingWork[workerUrl] = job;
                    }
                    else //ADD
                    {
                        onGoingWork.Add(workerUrl, job);
                    }
                    worker.work(job);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    // The worker is probably down but it'll be removed when the job tracker checks if they are alive or not
                }
            }
        }
    }
}
