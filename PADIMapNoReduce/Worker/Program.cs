using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Worker
{
    class Program
    {

        public const int JOB_TRACKER_PORT = 1000;

        static void Main(string[] args)
        {
            System.Console.WriteLine("Port to register worker: ");

            string portInput = System.Console.ReadLine();
            int portInputFormatted;
            while (!int.TryParse(portInput, out portInputFormatted))
            {
                System.Console.WriteLine("Invalid port format. Register port: ");
                portInput = System.Console.ReadLine();
            }

            //TODO: Check for port range
            TcpChannel channel = new TcpChannel(portInputFormatted);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(Worker),
                "Worker",
                WellKnownObjectMode.Singleton);

            if (portInputFormatted == JOB_TRACKER_PORT)
            {
                System.Console.WriteLine("JobTracker registred");
            }
            else
            {
                System.Console.WriteLine("Worker registred");
                JobTrackerApi jobTracker = 
                    (JobTrackerApi) Activator.GetObject(typeof(JobTrackerApi), "tcp://localhost:" + JOB_TRACKER_PORT + "/Worker");
                jobTracker.hello("tcp://localhost:" + portInputFormatted);
            }
            

            

            System.Console.WriteLine("<enter> para sair...");
            System.Console.ReadLine();
        }
    }
}
