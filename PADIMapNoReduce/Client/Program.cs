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
                System.Console.WriteLine("Write the number of splits and press <enter> to send job");
                string splits = System.Console.ReadLine();

                int splitsInputFormatted;
                while (!int.TryParse(splits, out splitsInputFormatted))
                {
                    System.Console.WriteLine("Invalid number. Number of splits: ");
                    splits = System.Console.ReadLine();
                }
                
                PADIMapNoReduce.IJobTracker jobTracker =
                        (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), "tcp://localhost:1000/Worker");
                jobTracker.registerJob("", splitsInputFormatted, "");
            }
        }
    }
}
