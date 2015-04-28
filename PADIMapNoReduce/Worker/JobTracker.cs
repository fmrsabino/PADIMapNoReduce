using System;
using System.Collections.Generic;
using System.Threading;

namespace Worker
{
    public partial class Worker : MarshalByRefObject, PADIMapNoReduce.IWorker
    {
        private List<string> workers = new List<string>();
        private Queue<LibPADIMapNoReduce.FileSplits> jobQueue;

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

            if (nSplits == 0)
            {
                CURRENT_STATUS = STATUS.JOBTRACKER_WAITING;
                return;
            }

            long splitBytes = nBytes / nSplits;

            jobQueue = new Queue<LibPADIMapNoReduce.FileSplits>();

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


                jobQueue.Enqueue(new LibPADIMapNoReduce.FileSplits(i, pair));
            }

            while (workers.Count == 0) { }

            ManualResetEvent[] threads = new ManualResetEvent[workers.Count];
            //Distribute to each worker one split
            for (int i = 0; i < workers.Count; i++)
            {
                string workerUrl = workers[i];
                try
                {
                    PADIMapNoReduce.IWorker worker =
                        (PADIMapNoReduce.IWorker)Activator.GetObject(typeof(PADIMapNoReduce.IWorker), workerUrl);
                    worker.setup(mapperCode, mapperClassName, clientUrl, inputFilePath);

                    threads[i] = new ManualResetEvent(false);
                    KeyValuePair<PADIMapNoReduce.IWorker, ManualResetEvent> pair =
                        new KeyValuePair<PADIMapNoReduce.IWorker, ManualResetEvent>(worker, threads[i]);
                    Thread t = new Thread(this.sendWork);
                    t.Start(pair);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("EXCEPTION: " + e.Message);
                    CURRENT_STATUS = STATUS.JOBTRACKER_WAITING; // For STATUS command of PuppetMaster
                    return;
                }
            }

            //wait for all threads to conclude
            WaitHandle.WaitAll(threads);

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

        private void sendWork(Object obj)
        {
            KeyValuePair<PADIMapNoReduce.IWorker, ManualResetEvent> pair = (KeyValuePair<PADIMapNoReduce.IWorker, ManualResetEvent>)obj;
            PADIMapNoReduce.IWorker worker = pair.Key;

            while (jobQueue.Count > 0)
            {
                LibPADIMapNoReduce.FileSplits job = null;
                lock (jobQueue)
                {
                    try
                    {
                        job = jobQueue.Dequeue();
                    }
                    catch (InvalidOperationException)
                    {
                        break;
                    }
                }
                worker.work(job);
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
                    worker.isAlive();
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    Console.WriteLine("Couldn't reach {0}. Marking as dead!", workers[i]);
                    deadWorkers.Add(workers[i]);
                }
            }

            workers.RemoveAll(x => deadWorkers.Contains(x));

            timer.Change(ALIVE_TIME_INTERVAL_IN_MS, Timeout.Infinite);
        }
    }
}
