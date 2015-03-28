using System;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Client";
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);
            while (true)
            {
                System.Console.WriteLine("Write the number of splits");
                string splits = System.Console.ReadLine();

                int splitsInputFormatted;
                while (!int.TryParse(splits, out splitsInputFormatted))
                {
                    System.Console.WriteLine("Invalid port format. Register port: ");
                    splits = System.Console.ReadLine();
                }

                System.Console.WriteLine("Press Enter to submit job");
                System.Console.ReadLine();

                
                Worker.JobTrackerApi jobTracker =
                        (Worker.JobTrackerApi)Activator.GetObject(typeof(Worker.JobTrackerApi), "tcp://localhost:1000/Worker");
                jobTracker.registerJob("", splitsInputFormatted, "");
            }
        }
    }
}
