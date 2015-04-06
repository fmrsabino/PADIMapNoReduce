using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Worker
{
    class Worker : MarshalByRefObject, PADIMapNoReduce.IWorker
    {
        public const string WORKER_OBJECT_ID = "W";

        private List<string> workers = new List<string>();
        private Queue<LibPADIMapNoReduce.FileSplits> jobQueue;

        private byte[] mapperCode;
        private string mapperClass;
        private string clientUrl;
        private string filePath;
        private bool workerSetup = false;

        /**** WorkerImpl ****/
        public void setup(byte[] code, string className, string clientUrl, string filePath)
        {
            Console.Out.WriteLine("Received code for class " + className);
            mapperCode = code;
            mapperClass = className;
            this.clientUrl = clientUrl;
            this.filePath = filePath;
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
                PADIMapNoReduce.IWorker worker =
                    (PADIMapNoReduce.IWorker)Activator.GetObject(typeof(PADIMapNoReduce.IWorker), workerUrl + "/" + WORKER_OBJECT_ID);
                worker.setup(mapperCode, mapperClassName, clientUrl, inputFilePath);

                threads[i] = new ManualResetEvent(false);
                KeyValuePair<PADIMapNoReduce.IWorker, ManualResetEvent> pair = new KeyValuePair<PADIMapNoReduce.IWorker, ManualResetEvent>(worker, threads[i]);
                Thread t = new Thread(this.sendWork);
                t.Start(pair);
            }

            //wait for all threads to conclude
            WaitHandle.WaitAll(threads);

            PADIMapNoReduce.IClient client =
            (PADIMapNoReduce.IClient)Activator.GetObject(typeof(PADIMapNoReduce.IClient), clientUrl);
            client.jobConcluded();
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
