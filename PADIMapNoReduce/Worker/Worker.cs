using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Worker
{
    class Worker : MarshalByRefObject, PADIMapNoReduce.IWorker
    {
        public const string WORKER_OBJECT_URI = "W";
        
        // The number of bytes requested to the client
        public const long BATCH_REQUEST_SIZE = 102400;
        // The number of lines of the map result sent to the client
        public const long BATCH_LINES = 1024;

        private List<string> workers = new List<string>();
        private Queue<LibPADIMapNoReduce.FileSplits> jobQueue;

        private byte[] mapperCode;
        private Assembly assembly;
        private object classObj;
        private Type type;

        private string mapperClass;
        private string clientUrl;
        private string filePath;
        private bool workerSetup = false;
        private string jobTrackerUrl;
        private string workerUrl;
        private Timer timer;
        private const long TIME_INTERVAL_IN_MS = 5000;

        public static STATUS PREVIOUS_STATUS;
        public static STATUS CURRENT_STATUS;
        public static float PERCENTAGE_FINISHED;

        // For FREEZEs
        static object workerMonitor;
        static object jobtrackerMonitor;
        static object mapperMonitor; // For SLOWW

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public Worker(string jobTrackerUrl)
        {
            this.jobTrackerUrl = jobTrackerUrl;
            CURRENT_STATUS = STATUS.JOBTRACKER_WAITING; // For STATUS command of PuppetMaster
            jobtrackerMonitor = new object();
        }

        public Worker(string workerUrl, string jobTrackerUrl)
        {
            this.workerUrl = workerUrl;
            this.jobTrackerUrl = jobTrackerUrl;
            CURRENT_STATUS = STATUS.WORKER_WAITING; // For STATUS command of PuppetMaster
            workerMonitor = new object();
            mapperMonitor = new object();
        }

        /**** WorkerImpl ****/
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

            //timer = new Timer(sendImAlive, null, TIME_INTERVAL_IN_MS, Timeout.Infinite);
            workerSetup = true;
        }

        public void work(LibPADIMapNoReduce.FileSplits fileSplits)
        {
            handleFreezeWorker(); // For handling FREEZEW from PuppetMaster
            handleSlowMap(); // For handling SLOWW from PuppetMaster
            CURRENT_STATUS = STATUS.WORKER_WORKING; // For STATUS command of PuppetMaster
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
                    CURRENT_STATUS = STATUS.WORKER_TRANSFERING_INPUT;
                    List<byte> splitBytes = client.processBytes(byteInterval, filePath);

                    CURRENT_STATUS = STATUS.WORKER_WORKING;

                    string[] splitLines = System.Text.Encoding.UTF8.GetString(splitBytes.ToArray()).Split(new string[] { Environment.NewLine }, System.StringSplitOptions.RemoveEmptyEntries);
                    splitBytes.Clear();

                    map(ref splitLines, fileSplits.nrSplits);
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
                            miniByteInterval = new PADIMapNoReduce.Pair<long, long>(i, i+BATCH_REQUEST_SIZE);
                        }
                          
                        
                        CURRENT_STATUS = STATUS.WORKER_TRANSFERING_INPUT;
                        List<byte> splitBytes = client.processBytes(miniByteInterval, filePath);

                        CURRENT_STATUS = STATUS.WORKER_WORKING;

                        string[] splitLines = System.Text.Encoding.UTF8.GetString(splitBytes.ToArray()).Split(new string[] { Environment.NewLine }, System.StringSplitOptions.RemoveEmptyEntries);
                        splitBytes.Clear();

                        map(ref splitLines, fileSplits.nrSplits);
                    }
                }
                 
            }
            else
            {
                Console.WriteLine("Worker is not set");
            }
            PERCENTAGE_FINISHED = 1; // For STATUS command of PuppetMaster
            CURRENT_STATUS = STATUS.WORKER_WAITING; // For STATUS command of PuppetMaster
        }

        private bool map(ref string[] lines, int splitId)
        {
            handleFreezeWorker(); // For handling FREEZEW from PuppetMaster
            handleSlowMap(); // For handling SLOWW from PuppetMaster
            
            PADIMapNoReduce.IClient client =
                    (PADIMapNoReduce.IClient)Activator.GetObject(typeof(PADIMapNoReduce.IClient), clientUrl);
            StringBuilder sb = new StringBuilder();
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            // Dynamically Invoke the method 
            int i = 0; // For STATUS command of PuppetMaster
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
                        client.receiveProcessData(sb.ToString(), splitId);
                        sb.Clear();
                        result.Clear();
                    }
                
                // For STATUS command of PuppetMaster
                i++;
                PERCENTAGE_FINISHED = i / lines.Length;
            }

            if (result.Count != 0) //send the rest
            {
                foreach (KeyValuePair<string, string> p in result)
                {
                    sb.AppendLine("key: " + p.Key + ", value: " + p.Value);
                }
                client.receiveProcessData(sb.ToString(), splitId);
                sb.Clear();
                result.Clear();
            }

            return true;
        }

        public void sendImAlive(Object state)
        {
            PADIMapNoReduce.IJobTracker jobTracker =
                        (PADIMapNoReduce.IJobTracker) Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), jobTrackerUrl);
            jobTracker.registerImAlive(workerUrl);

            timer.Change(TIME_INTERVAL_IN_MS, Timeout.Infinite);
        }

        /**** JobTrackerImpl ****/
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

        public void printStatus()
        {
            switch (CURRENT_STATUS)
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
                case STATUS.WORKER_WAITING:
                    Console.WriteLine("[*] The Worker is waiting");
                    break;
                case STATUS.WORKER_WORKING:
                    Console.WriteLine("[*] The Worker is working. It's " + 100*PERCENTAGE_FINISHED + "% finished.");
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
                if (CURRENT_STATUS == STATUS.JOBTRACKER_FROZEN)
                {
                    Console.WriteLine("[D] Job tracker frozen. Locking...");
                    Monitor.Wait(jobtrackerMonitor);
                }
            }
        }

        private void handleFreezeWorker()
        {
            lock (workerMonitor)
            {
                if (CURRENT_STATUS == STATUS.WORKER_FROZEN)
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
                if (CURRENT_STATUS == STATUS.WORKER_SLOWED)
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
                Thread.Sleep(seconds*1000);
                lock (mapperMonitor)
                {
                    if (CURRENT_STATUS == STATUS.WORKER_SLOWED)
                    {
                        Console.WriteLine("[D] Speeding worker...");
                        CURRENT_STATUS = PREVIOUS_STATUS;
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
            if (CURRENT_STATUS != STATUS.WORKER_SLOWED)
            {
                Console.WriteLine("[D] Slowing worker for " + seconds + " seconds...");
                PREVIOUS_STATUS = CURRENT_STATUS;
                CURRENT_STATUS = STATUS.WORKER_SLOWED;
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
            if (CURRENT_STATUS != STATUS.WORKER_FROZEN)
            {
                Console.WriteLine("[D] Freezing worker...");
                PREVIOUS_STATUS = CURRENT_STATUS;
                CURRENT_STATUS = STATUS.WORKER_FROZEN;
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
                if (CURRENT_STATUS == STATUS.WORKER_FROZEN)
                {
                    Console.WriteLine("[D] Unfreezing worker...");
                    CURRENT_STATUS = PREVIOUS_STATUS;
                    Monitor.PulseAll(workerMonitor);
                }
                else
                {
                    Console.WriteLine("[D] Unfreeze called on worker, but it's already unfrozen...");
                }
            }
        }

        public void freezec()
        {
            if (CURRENT_STATUS != STATUS.JOBTRACKER_FROZEN)
            {
                Console.WriteLine("[D] Freezing job tracker...");
                Monitor.Enter(jobtrackerMonitor);
                PREVIOUS_STATUS = CURRENT_STATUS;
                CURRENT_STATUS = STATUS.JOBTRACKER_FROZEN;
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
                if (CURRENT_STATUS == STATUS.JOBTRACKER_FROZEN)
                {
                    Console.WriteLine("[D] Unfreezing job tracker...");
                    CURRENT_STATUS = PREVIOUS_STATUS;
                    Monitor.PulseAll(jobtrackerMonitor);
                }
                else
                {
                    Console.WriteLine("[D] Unfreeze called on job tracker, but it's already unfrozen...");
                }
            }

        }


        // For STATUS command of PuppetMaster
        public enum STATUS
        {
            JOBTRACKER_WAITING, JOBTRACKER_WORKING, JOBTRACKER_FROZEN, WORKER_WAITING, WORKER_TRANSFERING_INPUT, WORKER_WORKING, WORKER_TRANSFERING_OUTPUT, WORKER_FROZEN, WORKER_SLOWED
        };
    }
}
