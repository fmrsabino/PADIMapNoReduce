using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Worker
{
    public partial class Worker : MarshalByRefObject, PADIMapNoReduce.IWorker
    {
        private List<string> workers = new List<string>();
        private List<string> jobTrackers = new List<string>();
        private ConcurrentQueue<LibPADIMapNoReduce.FileSplit> jobQueue = new ConcurrentQueue<LibPADIMapNoReduce.FileSplit>();
        private ConcurrentDictionary<int, LibPADIMapNoReduce.FileSplit> zombieQueue = new ConcurrentDictionary<int,LibPADIMapNoReduce.FileSplit>();
        private ConcurrentDictionary<string, LibPADIMapNoReduce.FileSplit> onGoingWork = new ConcurrentDictionary<string, LibPADIMapNoReduce.FileSplit>();

        private Timer timerW;
        private Timer timerJT;
        private const long ALIVE_TIME_INTERVAL_IN_MS = 1000;

        public Worker(string jobTrackerUrl)
        {
            this.url = jobTrackerUrl;
            this.jobTrackerUrl = jobTrackerUrl;
            CURRENT_STATUS_JOBTRACKER = STATUS.JOBTRACKER_WAITING; // For STATUS command of PuppetMaster
            CURRENT_STATUS_WORKER = STATUS.WORKER_WAITING; // For STATUS command of PuppetMaster
            jobtrackerMonitor = new object();
            workerMonitor = new object();
            mapperMonitor = new object();
            timerW = new Timer(checkWorkerStatus, null, ALIVE_TIME_INTERVAL_IN_MS, Timeout.Infinite);
            workers.Add(url);
            jobTrackers.Add(jobTrackerUrl);
        }

        public void registerJob
           (string inputFilePath, int nSplits, string outputResultPath, long nBytes, string clientUrl, byte[] mapperCode, string mapperClassName)
        {
            handleFreezeJobTracker(); // For handling FREEZEC from PuppetMaster
            //handleFreezeWorker();
            CURRENT_STATUS_JOBTRACKER = STATUS.JOBTRACKER_WORKING; // For STATUS command of PuppetMaster
            this.clientUrl = clientUrl;

            if (nSplits == 0)
            {
                CURRENT_STATUS_JOBTRACKER = STATUS.JOBTRACKER_WAITING;
                return;
            }

            long splitBytes = nBytes / nSplits;

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
                        onGoingWork.TryAdd(workerUrl, job);
                    }
                    worker.work(job);
                }
            }

            checkWorkerStatus(null);
        }

        public bool registerWorker(string workerUrl)
        {
            handleFreezeJobTracker(); // For handling FREEZEC from PuppetMaster
            //handleFreezeWorker();
            if (!workers.Contains(workerUrl))
            {
                workers.Add(workerUrl);
                jobTrackers.Add(workerUrl);
                System.Console.WriteLine("Registered " + workerUrl);

                //When a worker appears after a job has began
                if (workerSetup)
                {
                    PADIMapNoReduce.IWorker worker =
                        (PADIMapNoReduce.IWorker)Activator.GetObject(typeof(PADIMapNoReduce.IWorker), workerUrl);
                    worker.setup(mapperCode, mapperClass, clientUrl, filePath);

                    LibPADIMapNoReduce.FileSplit job = null;
                    if (jobQueue.TryDequeue(out job))
                    {
                        onGoingWork.TryAdd(workerUrl, job);
                        worker.work(job);
                    }
                }
                return true;
            }
            else
            {
                System.Console.WriteLine(workerUrl + " is already registered.");
                return false;
            }
        }

        public void removeJobTracker(string jobTrackerUrl)
        {
            handleFreezeJobTracker();
            jobTrackers.Remove(jobTrackerUrl);
        }

        //called on UnfreezeW
        public void updateWorkers(string workerUrl)
        {
            handleFreezeJobTracker();
            workers.Add(workerUrl);
        }

        //called on UnfreezeC
        public void updateJobTrackers(string workerUrl)
        {
            handleFreezeJobTracker();
            jobTrackers.Add(workerUrl);
        }

        public bool jtIsAlive()
        {
            handleFreezeJobTracker();
            return true;
        }

        public bool canSendProcessedData(string workerUrl, int splitId)
        {
            handleFreezeJobTracker();
            LibPADIMapNoReduce.FileSplit jobActual;
            if (onGoingWork.TryGetValue(workerUrl, out jobActual))
            {
                if (splitId == jobActual.splitId)
                {
                    LibPADIMapNoReduce.FileSplit job1;
                    zombieQueue.TryRemove(splitId, out job1);
                    return true;
                }
                else
                {
                    Console.WriteLine("nao é suposto estar aqui. só para debugging");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Someone take my job during my timeout");
                return false;
            }
        }

        public void checkJTStatus(Object state)
        {
            handleFreezeJobTracker();
            if (jobTrackerUrl != url)
            {
                PADIMapNoReduce.IJobTracker jobTracker =
                    (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), jobTrackerUrl);
                try
                {
                    jobTracker.jtIsAlive();
                    System.Console.WriteLine("PING do JT " + url + " para o JT " + jobTrackerUrl);

                }
                catch (System.Net.Sockets.SocketException)
                {
                    jobTrackers.Remove(jobTrackerUrl);
                    if (jobTrackers[0] == url) 
                    {
                        Console.Title = "JobTracker - " + url;
                        timerW = new Timer(checkWorkerStatus, null, ALIVE_TIME_INTERVAL_IN_MS, Timeout.Infinite);
                        //timerJT = new Timer(checkJTStatus, null, ALIVE_TIME_INTERVAL_IN_MS, Timeout.Infinite);
                    }
                    jobTrackerUrl = jobTrackers[0];
                    System.Console.WriteLine("NOW THE JOBTRACKER IS " + jobTrackerUrl);
                }
            }
            timerJT.Change(ALIVE_TIME_INTERVAL_IN_MS, Timeout.Infinite);
        }


        public void checkWorkerStatus(Object state)
        {
            handleFreezeJobTracker();
            List<string> deadWorkers = new List<string>();
            for (int i = 0; i < workers.Count; i++)
            {
                PADIMapNoReduce.IWorker worker =
                    (PADIMapNoReduce.IWorker)Activator.GetObject(typeof(PADIMapNoReduce.IWorker), workers[i]);

                try
                {
                    worker.isAlive(zombieQueue, jobTrackers, workers, jobQueue.ToArray(), onGoingWork,jobTrackerUrl);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    Console.WriteLine("Couldn't reach {0}. Marking as dead!", workers[i]);
                    deadWorkers.Add(workers[i]);

                    //add on zombiequeue the unfinished job
                    LibPADIMapNoReduce.FileSplit splitNz;
                    if (onGoingWork.TryGetValue(workers[i], out splitNz))
                    {
                        zombieQueue.TryAdd(splitNz.splitId, splitNz);
                    }
                }
            }

            workers.RemoveAll(x => deadWorkers.Contains(x));
            if (jobQueue.Count == 1 && onGoingWork.Count == 0)
            {
                LibPADIMapNoReduce.FileSplit job = null;
                if (jobQueue.TryDequeue(out job))
                {
                    PADIMapNoReduce.IWorker worker =
                            (PADIMapNoReduce.IWorker)Activator.GetObject(typeof(PADIMapNoReduce.IWorker), workers[0]);
                    worker.work(job);
                }
            }

            timerW.Change(ALIVE_TIME_INTERVAL_IN_MS, Timeout.Infinite);
        }

  
        public void notifySplitFinish(string workerUrl, LibPADIMapNoReduce.FileSplit fileSplit)
        {
            handleFreezeJobTracker(); // For handling FREEZEC from PuppetMaster
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
                        onGoingWork.TryAdd(workerUrl, job);
                    }
                    worker.work(job);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    // The worker is probably down but it'll be removed when the job tracker checks if they are alive or not
                }
            }
            else
            {
                PADIMapNoReduce.IClient client =
                    (PADIMapNoReduce.IClient)Activator.GetObject(typeof(PADIMapNoReduce.IClient), clientUrl);

                LibPADIMapNoReduce.FileSplit jobz = null;
                IEnumerator<int> it = (IEnumerator<int>) zombieQueue.Keys.GetEnumerator();
                bool hasFirst = it.MoveNext();

                if (hasFirst && zombieQueue.TryRemove(it.Current, out jobz))
                {
                    client.removeFile(fileSplit.splitId);
                    try
                    {
                        //Removes the zombie worker from ongoingWork
                        foreach (string s in onGoingWork.Keys)
                        {
                            LibPADIMapNoReduce.FileSplit job1 = null;
                            onGoingWork.TryGetValue(s, out job1);
                            if (job1.splitId == jobz.splitId)
                            {
                                LibPADIMapNoReduce.FileSplit deletedJob = null;
                                onGoingWork.TryRemove(s, out deletedJob);
                                Console.WriteLine("removi a Key = {0}", s);
                            }
                        }

                        if (onGoingWork.ContainsKey(workerUrl)) //UPDATE
                        {
                            onGoingWork[workerUrl] = jobz;
                        }
                        else //ADD
                        {
                            onGoingWork.TryAdd(workerUrl, jobz);
                        }
                        worker.work(jobz);
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        // The worker is probably down but it'll be removed when the job tracker checks if they are alive or not
                    }
                }
                else {
                    LibPADIMapNoReduce.FileSplit job2 = null;
                    onGoingWork.TryRemove(workerUrl, out job2);

                    if (onGoingWork.Count == 0)
                    {
                        try
                        {
                           client.jobConcluded();
                            System.Console.WriteLine("////////////JOB CONCLUDED/////////////////");
                            CURRENT_STATUS_JOBTRACKER = STATUS.JOBTRACKER_WAITING; // For STATUS command of PuppetMaster
                        }
                        catch (Exception e)
                        {
                            System.Console.WriteLine("EXCEPTION: " + e.Message);
                            CURRENT_STATUS_JOBTRACKER = STATUS.JOBTRACKER_WAITING; // For STATUS command of PuppetMaster
                            return;
                        }
                    }
                }
            }
        }
    }
}
