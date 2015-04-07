using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Worker
{
    class Program
    {
        public const int MAX_RANGE = 39999;
        public const int MIN_RANGE = 30001;

        static void Main(string[] args)
        {
            string portInput;
            int portInputFormatted;
            int jobTrackerPort;
            TcpChannel channel;

            //ARGS: -p ownPort [JobTrackerPath]
            if (args.Length == 2 || args.Length == 3)
            {
                if (args[0].Equals("-p"))
                {
                    portInput = args[1];
                    if (!int.TryParse(portInput, out portInputFormatted))
                    {
                        System.Console.WriteLine("Invalid port format supplied. Exitting...");
                        return;
                    }
                    if (portInputFormatted >= MIN_RANGE && portInputFormatted <= MAX_RANGE)
                    {
                        //WORKER
                        if (args.Length == 3)
                        {
                            try
                            {
                                string workerUrl = "tcp://localhost:" + portInputFormatted + "/" + Worker.WORKER_OBJECT_URI;
                                string jobTrackerUrl = args[2];
                                Worker worker = new Worker(workerUrl, jobTrackerUrl);

                                //Publish Worker object
                                channel = new TcpChannel(portInputFormatted);
                                ChannelServices.RegisterChannel(channel, true);

                                RemotingServices.Marshal(
                                    worker,
                                    Worker.WORKER_OBJECT_URI,
                                    typeof(PADIMapNoReduce.IWorker));

                                Console.Title = "Worker - " + workerUrl;
                                
                                //Register worker in JobTracker
                                PADIMapNoReduce.IJobTracker jobTracker =
                                    (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), args[2]);
                                if (jobTracker.registerWorker(workerUrl))
                                {
                                    System.Console.WriteLine("Worker registred");
                                }
                                else
                                {
                                    System.Console.WriteLine("Error: Could't register worker");
                                }
                            }
                            catch (Exception e)
                            {
                                System.Console.WriteLine("EXCEPTION: " + e.Message);
                                Console.ReadLine();
                                return;
                            }
                        }
                        //JOBTRACKER
                        else
                        {
                            try
                            {
                                string jobTrackerUrl = "tcp://localhost:" + portInputFormatted + "/" + Worker.WORKER_OBJECT_URI;
                                Worker worker = new Worker(jobTrackerUrl);

                                //Publish JobTrakcer object
                                channel = new TcpChannel(portInputFormatted);
                                ChannelServices.RegisterChannel(channel, true);
                                RemotingServices.Marshal(
                                        worker,
                                        Worker.WORKER_OBJECT_URI,
                                        typeof(PADIMapNoReduce.IJobTracker));

                                System.Console.WriteLine("JobTracker registred");
                                Console.Title = "JobTracker - " + "tcp://localhost:" + portInputFormatted;
                            }
                            catch (Exception e)
                            {
                                System.Console.WriteLine("EXCEPTION: " + e.Message);
                                Console.ReadLine();
                                return;
                            }
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Invalid port supplied. Range not supported. Exitting...");
                        return;
                    }
                }
                else
                {
                    System.Console.WriteLine("Invalid argument supplied. Exitting...");
                    return;
                }
            }
            //SO PARA TESTING    
            else
            {
                System.Console.WriteLine("Port to register worker (30001 to register JobTracker): ");

                portInput = System.Console.ReadLine();
                while (!int.TryParse(portInput, out portInputFormatted))
                {
                    System.Console.WriteLine("Invalid port format. Register port: ");
                    portInput = System.Console.ReadLine();
                }

                //DONE: Check exceptions
                //DONE: Check for port range
                channel = new TcpChannel(portInputFormatted);
                ChannelServices.RegisterChannel(channel, true);
                

                //TESTING
                jobTrackerPort = 30001;

                if (portInputFormatted == jobTrackerPort)
                {
                    System.Console.WriteLine("JobTracker registred");
                    Console.Title = "JobTracker - " + "tcp://localhost:" + portInputFormatted;

                    string jobTrackerUrl = "tcp://localhost:" + portInputFormatted + "/" + Worker.WORKER_OBJECT_URI;

                    Worker worker = new Worker(jobTrackerUrl);

                    RemotingServices.Marshal(
                        worker,
                        Worker.WORKER_OBJECT_URI,
                        typeof(PADIMapNoReduce.IJobTracker));
                }
                else
                {
                    Console.Title = "Worker - " + "tcp://localhost:" + portInputFormatted;
                    string jobTrackerUrl = "tcp://localhost:" + jobTrackerPort + "/" + Worker.WORKER_OBJECT_URI;
                    string workerUrl = "tcp://localhost:" + portInputFormatted + "/" + Worker.WORKER_OBJECT_URI;

                    Worker worker = new Worker(workerUrl, jobTrackerUrl);

                    RemotingServices.Marshal(
                        worker,
                        Worker.WORKER_OBJECT_URI,
                        typeof(PADIMapNoReduce.IWorker));

                    PADIMapNoReduce.IJobTracker jobTracker =
                        (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), jobTrackerUrl);
                    jobTracker.registerWorker(workerUrl);
                }
            }

            System.Console.WriteLine("<enter> to exit...");
            System.Console.ReadLine();
        }
    }
}
