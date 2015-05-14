using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Worker
{
    public partial class Worker : MarshalByRefObject, PADIMapNoReduce.IWorker
    {
        public const string WORKER_OBJECT_URI = "W";

        // The number of bytes requested to the client
        public const long BATCH_REQUEST_SIZE = 10240000;
        // The number of lines of the map result sent to the client
        public const long BATCH_LINES = 102400;

        private byte[] mapperCode;
        private Assembly assembly;
        private object classObj;
        private Type type;

        private string url;
        private string clientUrl;
        private string jobTrackerUrl;
        private string mapperClass;
        private string filePath;
        private bool workerSetup = false;

        public static STATUS PREVIOUS_STATUS_WORKER;
        public static STATUS CURRENT_STATUS_WORKER;
        public static STATUS PREVIOUS_STATUS_JOBTRACKER;
        public static STATUS CURRENT_STATUS_JOBTRACKER;
        public static float PERCENTAGE_FINISHED;

        public static int JOBTRACKER_COMM_TIMEOUT = 3000;

        // For FREEZEs
        static object workerMonitor;
        static object jobtrackerMonitor;
        static object mapperMonitor; // For SLOWW

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public Worker(string url, string jobTrackerUrl)
        {
            this.url = url;
            this.jobTrackerUrl = jobTrackerUrl;
            CURRENT_STATUS_WORKER = STATUS.WORKER_WAITING; // For STATUS command of PuppetMaster
            CURRENT_STATUS_JOBTRACKER = STATUS.JOBTRACKER_DISABLED;
            workerMonitor = new object();
            mapperMonitor = new object();
            jobtrackerMonitor = new object();
            timerJT = new Timer(checkJTStatus, null, ALIVE_TIME_INTERVAL_IN_MS, Timeout.Infinite);
        }

        public void setup(byte[] code, string className, string clientUrl, string filePath)
        {
            handleFreezeWorker(); // For handling FREEZEW from PuppetMaster
            Console.Out.WriteLine("Received code for class " + className);
            mapperCode = code;
            mapperClass = className;
            this.clientUrl = clientUrl;
            this.filePath = filePath;

            assembly = Assembly.Load(mapperCode);
            // Walk through each type in the assembly looking for our class
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsClass == true)
                {
                    if (type.FullName.EndsWith("." + mapperClass))
                    {
                        this.type = type;
                        // create an instance of the object
                        classObj = Activator.CreateInstance(type);
                    }
                }
            }
            workerSetup = true;
        }

        public void work(LibPADIMapNoReduce.FileSplit fileSplits)
        {
            handleFreezeWorker(); // For handling FREEZEW from PuppetMaster
            handleSlowMap(); // For handling SLOWW from PuppetMaster
            lock (workerMonitor) CURRENT_STATUS_WORKER = STATUS.WORKER_WORKING; // For STATUS command of PuppetMaster
            PERCENTAGE_FINISHED = 0;
            PADIMapNoReduce.Pair<long, long> byteInterval = fileSplits.pair;
            Console.WriteLine("Received job for bytes: " + byteInterval.First + " to " + byteInterval.Second);
            if (workerSetup)
            {
                long splitSize = byteInterval.Second - byteInterval.First;

                PADIMapNoReduce.IClient client =
                    (PADIMapNoReduce.IClient)Activator.GetObject(typeof(PADIMapNoReduce.IClient), clientUrl);

                if (splitSize <= BATCH_REQUEST_SIZE) //Request all
                {
                    lock (workerMonitor) CURRENT_STATUS_WORKER = STATUS.WORKER_TRANSFERING_INPUT;
                    List<byte> splitBytes = client.processBytes(byteInterval, filePath);

                    lock (workerMonitor) CURRENT_STATUS_WORKER = STATUS.WORKER_WORKING;

                    string[] splitLines = System.Text.Encoding.UTF8.GetString(splitBytes.ToArray()).Split(new string[] { Environment.NewLine }, System.StringSplitOptions.RemoveEmptyEntries);
                    splitBytes.Clear();

                    map(ref splitLines, fileSplits.splitId, true);
                }
                else //request batch
                {
                    for (long i = byteInterval.First; i < byteInterval.Second; i += BATCH_REQUEST_SIZE)
                    {
                        PADIMapNoReduce.Pair<long, long> miniByteInterval;
                        if (i + BATCH_REQUEST_SIZE > byteInterval.Second)
                        {
                            miniByteInterval = new PADIMapNoReduce.Pair<long, long>(i, byteInterval.Second);
                        }
                        else
                        {
                            miniByteInterval = new PADIMapNoReduce.Pair<long, long>(i, i + BATCH_REQUEST_SIZE);
                        }

                        lock (workerMonitor) CURRENT_STATUS_WORKER = STATUS.WORKER_TRANSFERING_INPUT;

                        List<byte> splitBytes = client.processBytes(miniByteInterval, filePath);

                        lock (workerMonitor) CURRENT_STATUS_WORKER = STATUS.WORKER_WORKING;

                        string[] splitLines = System.Text.Encoding.UTF8.GetString(splitBytes.ToArray()).Split(new string[] { Environment.NewLine }, System.StringSplitOptions.RemoveEmptyEntries);
                        splitBytes.Clear();

                        if (!map(ref splitLines, fileSplits.splitId, false))
                            return;

                        // We need something more coarse because we can't get the current size being processed due to different encodings
                        PERCENTAGE_FINISHED = (float)(i - byteInterval.First) / (float)(byteInterval.Second - byteInterval.First);
                    }
                    PERCENTAGE_FINISHED = 1;
                }

            }
            else
            {
                Console.WriteLine("Worker is not set");
            }
            PERCENTAGE_FINISHED = 1; // For STATUS command of PuppetMaster
            lock (workerMonitor) CURRENT_STATUS_WORKER = STATUS.WORKER_WAITING; // For STATUS command of PuppetMaster

            //In case, at the call to the jobtracker he is down, the worker stay on the while until the variable jobTracker be actualized 
            bool success = false;
            while (!success)
            {
                //Notify JobTracker
                PADIMapNoReduce.IJobTracker jobTracker =
                        (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), jobTrackerUrl);
                try
                {
                    jobTracker.notifySplitFinish(url, fileSplits);
                    success = true;
                }
                catch (System.Net.Sockets.SocketException)
                {
                    Console.WriteLine("In WORK func - Couldn't contact to JobTracker! Waiting for the new one...");
                }
            }
        }

        private bool map(ref string[] lines, int splitId, bool singleCall)
        {
            handleFreezeWorker(); // For handling FREEZEW from PuppetMaster
            handleSlowMap(); // For handling SLOWW from PuppetMaster

            PADIMapNoReduce.IClient client =
                    (PADIMapNoReduce.IClient)Activator.GetObject(typeof(PADIMapNoReduce.IClient), clientUrl);
            StringBuilder sb = new StringBuilder();
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            int i = 0; // For PuppetMaster STATUS
            // Dynamically Invoke the method 
            for (int j = 0; j < lines.Length; j++)
            {
                handleFreezeWorker(); // For handling FREEZEW from PuppetMaster
                handleSlowMap(); // For handling SLOWW from PuppetMaster
                object[] args = new object[] { lines[j] };
                object resultObject = type.InvokeMember("Map",
                    BindingFlags.Default | BindingFlags.InvokeMethod,
                        null,
                        classObj,
                        args);

                result.AddRange((IList<KeyValuePair<string, string>>)resultObject);

                if (j % BATCH_LINES == 0)
                {
                    sb = new StringBuilder();
                    foreach (KeyValuePair<string, string> p in result)
                    {
                        sb.AppendLine("key: " + p.Key + ", value: " + p.Value);
                    }

                    //In case, at the call to the jobtracker he is down, the worker stay on the while until the variable jobTracker be actualized
                    bool success = false;
                    while (!success)
                    {
                        PADIMapNoReduce.IJobTracker jobTracker =
                            (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), jobTrackerUrl);
                        try
                        {
                            if (!jobTracker.canSendProcessedData(url, splitId))
                            {
                                success = true;
                                return false;
                            }
                            else
                            {
                                client.receiveProcessData(sb.ToString(), splitId);
                                sb.Clear();
                                result.Clear();
                                success = true;
                            }
                        }
                        catch (System.Net.Sockets.SocketException)
                        {
                            System.Console.WriteLine("In MAP func - Couldn't contact to JobTracker! Waiting for the new one...");
                        }
                    }

                }
                if (singleCall)
                {
                    i++; // For PuppetMaster STATUS
                    PERCENTAGE_FINISHED = (float)i / (float)lines.Length;
                }
            }

            if (result.Count != 0) //send the rest
            {
                foreach (KeyValuePair<string, string> p in result)
                {
                    sb.AppendLine("key: " + p.Key + ", value: " + p.Value);
                }

                //In case, at the call to the jobtracker he is down, the worker stay on the while until the variable jobTracker be actualized
                bool success = false;
                while (!success)
                {
                    PADIMapNoReduce.IJobTracker jobTracker =
                            (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), jobTrackerUrl);
                    try
                    {
                        if (!jobTracker.canSendProcessedData(url, splitId))
                        {
                            success = true;
                            return false;
                        }
                        else
                        {
                            client.receiveProcessData(sb.ToString(), splitId);
                            sb.Clear();
                            result.Clear();
                            success = true;
                        }
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        Console.WriteLine("In MAP func - Couldn't contact to JobTracker! Waiting for the new one...");
                    }
                }
            }
            return true;
        }

        public bool isAlive(ConcurrentDictionary<int, LibPADIMapNoReduce.FileSplit> zombieQueue, List<string> jobTrackers, List<string> workers, LibPADIMapNoReduce.FileSplit[] jobQueue, ConcurrentDictionary<string, LibPADIMapNoReduce.FileSplit> onGoingWork, string jobTrackerUrl)
        {
            handleFreezeWorker();
            if (url == jobTrackerUrl)
            {
                return true;
            }
            this.jobTrackerUrl = jobTrackerUrl;
            this.jobTrackers = jobTrackers;
            this.workers = workers;
            this.jobQueue = new ConcurrentQueue<LibPADIMapNoReduce.FileSplit>(jobQueue);
            this.zombieQueue = new ConcurrentDictionary<int, LibPADIMapNoReduce.FileSplit>(zombieQueue);
            this.onGoingWork = onGoingWork;
            return true;
        }


        public void printStatus()
        {
            switch (CURRENT_STATUS_JOBTRACKER)
            {
                case STATUS.JOBTRACKER_WAITING:
                    Console.WriteLine("[*] The JobTracker is waiting");
                    break;
                case STATUS.JOBTRACKER_WORKING:
                    Console.WriteLine("[*] The JobTracker is working");
                    break;
                case STATUS.JOBTRACKER_FROZEN:
                    Console.WriteLine("[*] The Job Tracker is frozen.");
                    break;
            }
            switch (CURRENT_STATUS_WORKER)
            {
                case STATUS.WORKER_WAITING:
                    Console.WriteLine("[*] The Worker is waiting");
                    break;
                case STATUS.WORKER_WORKING:
                    Console.WriteLine("[*] The Worker is working. It's " + 100 * PERCENTAGE_FINISHED + "% finished.");
                    break;
                case STATUS.WORKER_TRANSFERING_INPUT:
                    Console.WriteLine("[*] The Worker is transfering input data from the client.");
                    break;
                case STATUS.WORKER_TRANSFERING_OUTPUT:
                    Console.WriteLine("[*] The Worker is transfering output data to the client.");
                    break;
                case STATUS.WORKER_FROZEN:
                    Console.WriteLine("[*] The Worker is frozen.");
                    break;
                case STATUS.WORKER_SLOWED:
                    Console.WriteLine("[*] The Worker is slowed.");
                    break;
            }
        }

        private void handleFreezeJobTracker()
        {
            lock (jobtrackerMonitor)
            {
                if (CURRENT_STATUS_JOBTRACKER == STATUS.JOBTRACKER_FROZEN)
                {
                    Console.WriteLine("[D] Job tracker frozen. Locking...");
                    Console.Title = "Worker - " + url;
                    timerW.Change(Timeout.Infinite, Timeout.Infinite);
                    Monitor.Wait(jobtrackerMonitor);
                }
            }
        }

        private void handleFreezeWorker()
        {
            lock (workerMonitor)
            {
                if (CURRENT_STATUS_WORKER == STATUS.WORKER_FROZEN)
                {
                    Console.WriteLine("[D] Worker frozen. Locking...");
                    Monitor.Wait(workerMonitor);
                }
            }
        }

        private void handleSlowMap()
        {
            lock (mapperMonitor)
            {
                if (CURRENT_STATUS_WORKER == STATUS.WORKER_SLOWED)
                {
                    Console.WriteLine("[D] Worker slowed. Locking...");
                    Monitor.Wait(mapperMonitor);
                }
            }
        }

        class signallingThread
        {
            public static void signalAllThreads(int seconds)
            {
                Thread.Sleep(seconds * 1000);
                lock (mapperMonitor)
                {
                    if (CURRENT_STATUS_WORKER == STATUS.WORKER_SLOWED)
                    {
                        Console.WriteLine("[D] Speeding worker...");
                        CURRENT_STATUS_WORKER = PREVIOUS_STATUS_WORKER;
                        Monitor.PulseAll(mapperMonitor);
                    }
                    else
                    {
                        Console.WriteLine("[D] Something wrong happened on signalling threads slowed on worker...");
                    }
                }
            }
        }

        public void sloww(int seconds)
        {
            if (CURRENT_STATUS_WORKER != STATUS.WORKER_SLOWED)
            {
                Console.WriteLine("[D] Slowing worker for " + seconds + " seconds...");
                PREVIOUS_STATUS_WORKER = CURRENT_STATUS_WORKER;
                CURRENT_STATUS_WORKER = STATUS.WORKER_SLOWED;
                // Initiate a new thread for signaling all paused threads
                Thread t = new Thread(() => signallingThread.signalAllThreads(seconds));
                t.Start();
            }
            else
            {
                Console.WriteLine("[D] Slow called on worker, but it's already slowed...");
            }
        }

        public void freezew()
        {
            if (CURRENT_STATUS_WORKER != STATUS.WORKER_FROZEN)
            {
                Console.WriteLine("[D] Freezing worker...");
                PREVIOUS_STATUS_WORKER = CURRENT_STATUS_WORKER;
                CURRENT_STATUS_WORKER = STATUS.WORKER_FROZEN;
            }
            else
            {
                Console.WriteLine("[D] Freeze called on worker, but it's already frozen...");
            }
        }

        public void unfreezew()
        {
            lock (workerMonitor)
            {
                if (CURRENT_STATUS_WORKER == STATUS.WORKER_FROZEN)
                {
                    Console.WriteLine("[D] Unfreezing worker...");
                    CURRENT_STATUS_WORKER = PREVIOUS_STATUS_WORKER;
                    Monitor.PulseAll(workerMonitor);
                }
                else
                {
                    Console.WriteLine("[D] Unfreeze called on worker, but it's already unfrozen...");
                }
            }

            //In case, at the call to the jobtracker he is down, the worker stay on the while until the variable jobTracker be actualized
            bool success = false;
            while (!success)
            {
                PADIMapNoReduce.IJobTracker jobTracker =
                (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), jobTrackerUrl);
                try
                {
                    jobTracker.updateWorkers(url);
                    success = true;
                }
                catch (System.Net.Sockets.SocketException)
                {
                    Console.WriteLine("In UNFREEZEW func - Couldn't contact to JobTracker! Waiting for the new one...");
                }
            }
        }

        public void freezec()
        {
            if (CURRENT_STATUS_JOBTRACKER != STATUS.JOBTRACKER_FROZEN)
            {
                Console.WriteLine("[D] Freezing job tracker...");
                PREVIOUS_STATUS_JOBTRACKER = CURRENT_STATUS_JOBTRACKER;
                CURRENT_STATUS_JOBTRACKER = STATUS.JOBTRACKER_FROZEN;
            }
            else
            {
                Console.WriteLine("[D] Freeze called on job tracker, but it's already frozen...");
            }
        }

        public void unfreezec()
        {
            lock (jobtrackerMonitor)
            {
                if (CURRENT_STATUS_JOBTRACKER == STATUS.JOBTRACKER_FROZEN)
                {
                    Console.WriteLine("[D] Unfreezing job tracker...");
                    CURRENT_STATUS_JOBTRACKER = PREVIOUS_STATUS_JOBTRACKER;
                    Monitor.PulseAll(jobtrackerMonitor);
                    timerJT = new Timer(checkJTStatus, null, ALIVE_TIME_INTERVAL_IN_MS, Timeout.Infinite);
                }
                else
                {
                    Console.WriteLine("[D] Unfreeze called on job tracker, but it's already unfrozen...");
                }

                //In case, at the call to the jobtracker he is down, the worker stay on the while until the variable jobTracker be actualized
                bool success = false;
                while (!success)
                {
                    PADIMapNoReduce.IJobTracker jobTracker =
                    (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), jobTrackerUrl);
                    try
                    {
                        jobTracker.updateJobTrackers(url);
                        success = true;
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        Console.WriteLine("In UNFREEZEC func - Couldn't contact to JobTracker! Waiting for the new one...");
                    }
                }
            }
        }


        // For STATUS command of PuppetMaster
        public enum STATUS
        {
            JOBTRACKER_WAITING, JOBTRACKER_WORKING, JOBTRACKER_FROZEN, WORKER_WAITING, WORKER_TRANSFERING_INPUT, WORKER_WORKING, WORKER_TRANSFERING_OUTPUT, WORKER_FROZEN, WORKER_SLOWED, JOBTRACKER_DISABLED
        };

    }
}
