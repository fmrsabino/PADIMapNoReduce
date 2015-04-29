using System;
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

        public Worker(string url, string jobTrackerUrl)
        {
            this.url = url;
            this.jobTrackerUrl = jobTrackerUrl;
            CURRENT_STATUS = STATUS.WORKER_WAITING; // For STATUS command of PuppetMaster
            workerMonitor = new object();
            mapperMonitor = new object();
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

        public bool isAlive()
        {
            return true;
        }

        public void sendImAlive(Object state)
        {
            PADIMapNoReduce.IJobTracker jobTracker =
                        (PADIMapNoReduce.IJobTracker) Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), jobTrackerUrl);
            try {
                jobTracker.registerImAlive(url);
            } catch (System.Net.Sockets.SocketException e) {
                Console.WriteLine("Could't find JobTracker!");
            }

            timer.Change(ALIVE_TIME_INTERVAL_IN_MS, Timeout.Infinite);
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


        public string getUrl()
        {
            return url;
        }
    }
}
