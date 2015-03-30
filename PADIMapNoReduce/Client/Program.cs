using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Client
{
    class Program
    {
        public const string MAP_FUNC_LOCATION = "..\\..\\..\\LibMapper\\bin\\Debug\\LibMapper.dll";
        public const string MAP_FUNC_CLASS_NAME = "Mapper";

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

                System.Console.WriteLine("Write file size in bytes:");
                string bytes = System.Console.ReadLine();
                int fileSizeInputFormatted;
                while (!int.TryParse(bytes, out fileSizeInputFormatted))
                {
                    System.Console.WriteLine("Invalid number. Number of bytes: ");
                    bytes = System.Console.ReadLine();
                }
                
                PADIMapNoReduce.IJobTracker jobTracker =
                        (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), "tcp://localhost:1000/Worker");
                jobTracker.registerJob("", splitsInputFormatted, "", fileSizeInputFormatted);

                try
                {
                    byte[] mapperCode = File.ReadAllBytes(MAP_FUNC_LOCATION);
                    jobTracker.broadcastMapper(mapperCode, MAP_FUNC_CLASS_NAME);
                }
                catch (SocketException)
                {
                    System.Console.WriteLine("Could not locate server");
                }
                
            }
        }
    }
}
