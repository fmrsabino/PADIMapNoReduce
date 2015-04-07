using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Worker
{
    class Worker : MarshalByRefObject, PADIMapNoReduce.IWorker
    {
        public const string WORKER_OBJECT_URI = "W";

        private List<string> workers = new List<string>();
        private Queue<LibPADIMapNoReduce.FileSplits> jobQueue;

        private byte[] mapperCode;
        private string mapperClass;
        private string clientUrl;
        private string filePath;
        private bool workerSetup = false;
        private string jobTrackerUrl;
        private string workerUrl;
        private Timer timer;
        private const long TIME_INTERVAL_IN_MS = 5000;

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public Worker(string jobTrackerUrl)
        {
            this.jobTrackerUrl = jobTrackerUrl;
        }

        public Worker(string workerUrl, string jobTrackerUrl)
        {
            this.workerUrl = workerUrl;
            this.jobTrackerUrl = jobTrackerUrl;
        }

        /**** WorkerImpl ****/
        public void setup(byte[] code, string className, string clientUrl, string filePath)
        {
            Console.Out.WriteLine("Received code for class " + className);
            mapperCode = code;
            mapperClass = className;
            this.clientUrl = clientUrl;
            this.filePath = filePath;

            //timer = new Timer(sendImAlive, null, TIME_INTERVAL_IN_MS, Timeout.Infinite);
            workerSetup = true;
        }

        public void work(LibPADIMapNoReduce.FileSplits fileSplits)
        {
            PADIMapNoReduce.Pair<long, long> byteInterval = fileSplits.pair;
            Console.WriteLine("Received job for bytes: " + byteInterval.First + " to " + byteInterval.Second);
            if (workerSetup)
            {
                PADIMapNoReduce.IClient client =
                    (PADIMapNoReduce.IClient)Activator.GetObject(typeof(PADIMapNoReduce.IClient), clientUrl);

                List<string> resultLines = client.processBytes(byteInterval, filePath);
                System.Console.WriteLine("========= RESULT ==========");
                foreach (string s in resultLines)
                {
                    System.Console.WriteLine(s);
                }
                System.Console.WriteLine("===========================");
                string result = map(resultLines);
                client.receiveProcessData(result, fileSplits.nrSplits);
            }
            else
            {
                Console.WriteLine("Worker is not set");
            }
        }

        private string map(List<string> lines)
        {
            Assembly assembly = Assembly.Load(mapperCode);
            // Walk through each type in the assembly looking for our class
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsClass == true)
                {
                    if (type.FullName.EndsWith("." + mapperClass))
                    {
                        // create an instance of the object
                        object ClassObj = Activator.CreateInstance(type);

                        List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
                        // Dynamically Invoke the method
                        foreach (string line in lines)
                        {
                            object[] args = new object[] { line };
                            object resultObject = type.InvokeMember("Map",
                              BindingFlags.Default | BindingFlags.InvokeMethod,
                                   null,
                                   ClassObj,
                                   args);

                            IList<KeyValuePair<string, string>> tempResult = (IList<KeyValuePair<string, string>>)resultObject;
                            //Can't join two ILists :(
                            foreach (KeyValuePair<string, string> p in tempResult)
                            {
                                result.Add(p);
                            }

                        }

                        Console.WriteLine("Map call result was: ");
                        string output = "";
                        foreach (KeyValuePair<string, string> p in result)
                        {
                            string format = "key: " + p.Key + ", value: " + p.Value;
                            Console.WriteLine(format);
                            output += format + Environment.NewLine;
                        }
                        return output;
                    }
                }
            }
            throw (new System.Exception("could not invoke method"));
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
            if (nSplits == 0)
            {
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
            }
            catch (Exception e)
            {
                System.Console.WriteLine("EXCEPTION: " + e.Message);
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
    }
}
