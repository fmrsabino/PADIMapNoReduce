using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Worker
{
    class Program
    {
        public const int JOB_TRACKER_PORT = 30001;

        static void Main(string[] args)
        {
            System.Console.WriteLine("Port to register worker (30001 to register JobTracker): ");

            string portInput = System.Console.ReadLine();
            int portInputFormatted;
            while (!int.TryParse(portInput, out portInputFormatted))
            {
                System.Console.WriteLine("Invalid port format. Register port: ");
                portInput = System.Console.ReadLine();
            }

            //TODO: Check for port range
            //TODO: Catch already in use port exception
            TcpChannel channel = new TcpChannel(portInputFormatted);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(Worker),
                Worker.WORKER_OBJECT_ID,
                WellKnownObjectMode.Singleton);

            if (portInputFormatted == JOB_TRACKER_PORT)
            {
                System.Console.WriteLine("JobTracker registred");
                Console.Title = "JobTracker - " + "tcp://localhost:" + portInputFormatted;
            }
            else
            {
                Console.Title = "Worker - " + "tcp://localhost:" + portInputFormatted;
                System.Console.WriteLine("Worker registred");
                PADIMapNoReduce.IJobTracker jobTracker =
                    (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), "tcp://localhost:" + JOB_TRACKER_PORT + "/" + Worker.WORKER_OBJECT_ID);
                jobTracker.registerWorker("tcp://localhost:" + portInputFormatted);
            }

            System.Console.WriteLine("<enter> to exit...");
            System.Console.ReadLine();
        }
    }
}
