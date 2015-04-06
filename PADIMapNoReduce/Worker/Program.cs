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
                        try
                        {
                            channel = new TcpChannel(portInputFormatted);
                            ChannelServices.RegisterChannel(channel, true);
                            RemotingConfiguration.RegisterWellKnownServiceType(
                                typeof(Worker),
                                Worker.WORKER_OBJECT_ID,
                                WellKnownObjectMode.Singleton);
                        }
                        catch (Exception i)
                        {
                            System.Console.WriteLine("EXCEPTION: " + i.Message);
                            return;
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Invalid port supplied. Range not supported. Exitting...");
                        return;
                    }

                    //WORKER
                    if (args.Length == 3)
                    {
                        try
                        {
                            Console.Title = "Worker - " + args[2];
                            System.Console.WriteLine("Worker registred");
                            PADIMapNoReduce.IJobTracker jobTracker =
                                (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), args[2]);
                            jobTracker.registerWorker("tcp://localhost:" + portInputFormatted);
                        }
                        catch (Exception e)
                        {
                            System.Console.WriteLine("EXCEPTION: " + e.Message);
                            return;
                        }
                    }
                    //JOBTRACKER
                    else
                    {
                        System.Console.WriteLine("JobTracker registred");
                        Console.Title = "JobTracker - " + "tcp://localhost:" + portInputFormatted;
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
                RemotingConfiguration.RegisterWellKnownServiceType(
                    typeof(Worker),
                    Worker.WORKER_OBJECT_ID,
                    WellKnownObjectMode.Singleton);

                //TESTING
                jobTrackerPort = 30001;

                if (portInputFormatted == jobTrackerPort)
                {
                    System.Console.WriteLine("JobTracker registred");
                    Console.Title = "JobTracker - " + "tcp://localhost:" + portInputFormatted;
                }
                else
                {
                    Console.Title = "Worker - " + "tcp://localhost:" + portInputFormatted;
                    System.Console.WriteLine("Worker registred");
                    PADIMapNoReduce.IJobTracker jobTracker =
                        (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), "tcp://localhost:" + jobTrackerPort + "/" + Worker.WORKER_OBJECT_ID);
                    jobTracker.registerWorker("tcp://localhost:" + portInputFormatted);
                }
            }

            System.Console.WriteLine("<enter> to exit...");
            System.Console.ReadLine();
        }
    }
}
