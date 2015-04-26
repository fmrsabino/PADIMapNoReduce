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

        public static STATUS CURRENT_STATUS;
        public static float PERCENTAGE_FINISHED;

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public Worker(string jobTrackerUrl)
        {
            this.jobTrackerUrl = jobTrackerUrl;
            CURRENT_STATUS = STATUS.JOBTRACKER_WAITING; // For STATUS command of PuppetMaster
        }

        public Worker(string workerUrl, string jobTrackerUrl)
        {
            this.workerUrl = workerUrl;
            this.jobTrackerUrl = jobTrackerUrl;
            CURRENT_STATUS = STATUS.WORKER_WAITING; // For STATUS command of PuppetMaster
        }

        /**** WorkerImpl ****/
        public void setup(byte[] code, string className, string clientUrl, string filePath)
        {
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
            CURRENT_STATUS = STATUS.WORKER_WORKING; // For STATUS command of PuppetMaster
            PERCENTAGE_FINISHED = 0;
            PADIMapNoReduce.Pair<long, long> byteInterval = fileSplits.pair;
            Console.WriteLine("Received job for bytes: " + byteInterval.First + " to " + byteInterval.Second);
            if (workerSetup)
            {
                PADIMapNoReduce.IClient client =
                    (PADIMapNoReduce.IClient)Activator.GetObject(typeof(PADIMapNoReduce.IClient), clientUrl);
                
                CURRENT_STATUS = STATUS.WORKER_TRANSFERING_INPUT;
                List<byte> splitBytes = client.processBytes(byteInterval, filePath);
                Console.WriteLine("Worker.work() - received split data from worker");

                CURRENT_STATUS = STATUS.WORKER_WORKING;
                
                string[] splitLines = System.Text.Encoding.UTF8.GetString(splitBytes.ToArray()).Split(new string[] { Environment.NewLine }, System.StringSplitOptions.RemoveEmptyEntries);
                splitBytes.Clear();

                map(ref splitLines, fileSplits.nrSplits);
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
            Console.WriteLine("========START MAP========");
            PADIMapNoReduce.IClient client =
                    (PADIMapNoReduce.IClient)Activator.GetObject(typeof(PADIMapNoReduce.IClient), clientUrl);
            StringBuilder sb = new StringBuilder();
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            // Dynamically Invoke the method 
            int i = 0; // For STATUS command of PuppetMaster
            for (int j = 0; j < lines.Length; j++)
            {
                    object[] args = new object[] { lines[j] };
                    object resultObject = type.InvokeMember("Map",
                        BindingFlags.Default | BindingFlags.InvokeMethod,
                            null,
                            classObj,
                            args);

                    result.AddRange((IList<KeyValuePair<string, string>>)resultObject);

                    if (j % 1024 == 0)
                    {
                        //Console.WriteLine("Reached Batch Size... Sending data");
                        sb = new StringBuilder();
                        foreach (KeyValuePair<string, string> p in result)
                        {
                            sb.AppendLine("key: " + p.Key + ", value: " + p.Value);
                        }
                        client.receiveProcessData(sb.ToString(), splitId);
                        sb.Clear();
                        result.Clear();
                       // Console.WriteLine("Finished Sending Batch");
                    }
                
                // For STATUS command of PuppetMaster
                i++;
                PERCENTAGE_FINISHED = i / lines.Length;
            }

            //Console.WriteLine("Send last bits of data");
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
            //Console.WriteLine("Finished Sending!");

            Console.WriteLine("========END MAP========");
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
            }
        }

        // For STATUS command of PuppetMaster
        public enum STATUS
        {
            JOBTRACKER_WAITING, JOBTRACKER_WORKING, WORKER_WAITING, WORKER_TRANSFERING_INPUT, WORKER_WORKING, WORKER_TRANSFERING_OUTPUT 
        };
    }
}
