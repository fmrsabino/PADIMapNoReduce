using System;
using System.Collections.Generic;
using System.Reflection;

namespace Worker
{
    class Worker : MarshalByRefObject, PADIMapNoReduce.IWorker
    {
        public const string WORKER_OBJECT_ID = "W";

        private List<string> workers = new List<string>();
        private Queue<PADIMapNoReduce.Pair<long, long>> jobQueue;

        private byte[] mapperCode;
        private string mapperClass;
        private string clientUrl;
        private bool workerSetup = false;

        /**** WorkerImpl ****/
        public void setup(byte[] code, string className, string clientUrl)
        {
            Console.Out.WriteLine("Received code for class " + className);
            mapperCode = code;
            mapperClass = className;
            this.clientUrl = clientUrl;
            workerSetup = true;
        }

        public void work(PADIMapNoReduce.Pair<long, long> byteInterval)
        {
            Console.WriteLine("Received job for bytes: " + byteInterval.First + " to " + byteInterval.Second);
            if (workerSetup)
            {
                PADIMapNoReduce.IClient client =
                    (PADIMapNoReduce.IClient)Activator.GetObject(typeof(PADIMapNoReduce.IClient), clientUrl);

                List<string> resultLines = client.processBytes(byteInterval);
                System.Console.WriteLine("========= RESULT ==========");
                foreach (string s in resultLines)
                {
                    System.Console.WriteLine(s);
                }
                System.Console.WriteLine("===========================");
                map(resultLines);
            }
            else
            {
                Console.WriteLine("Worker is not set");
            }
        }

        private bool map(List<string> lines)
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
                        foreach (KeyValuePair<string, string> p in result)
                        {
                            Console.WriteLine("key: " + p.Key + ", value: " + p.Value);
                        }
                        return true;
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

            jobQueue = new Queue<PADIMapNoReduce.Pair<long, long>>();

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

                jobQueue.Enqueue(pair);
            }

            //Distribute to each worker one split
            foreach (string workerUrl in workers)
            {
                if (jobQueue.Count == 0)
                {
                    //No more work to distribute
                    break;
                }
                PADIMapNoReduce.IWorker worker =
                    (PADIMapNoReduce.IWorker)Activator.GetObject(typeof(PADIMapNoReduce.IWorker), workerUrl + "/" + WORKER_OBJECT_ID);
                worker.setup(mapperCode, mapperClassName, clientUrl);
                worker.work(jobQueue.Dequeue());
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
