using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Client
{
    class Client : MarshalByRefObject, PADIMapNoReduce.IClient
    {
        public int Client_PORT = 5000;
        public String worker_url;
        public const string MAP_FUNC_LOCATION = "..\\..\\..\\LibMapper\\bin\\Debug\\LibMapper.dll";
        public const string MAP_FUNC_CLASS_NAME = "Mapper";

        
        public Client(string worker) {
            worker_url = worker;

            TcpChannel channel = new TcpChannel(Client_PORT);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(Client),
                "Client",
                WellKnownObjectMode.Singleton);
        }
    
        public List<string> processBytes(PADIMapNoReduce.Pair<long, long> byteInterval)
        {
            System.Console.WriteLine("Received request for bytes from " + byteInterval.First + " to " + byteInterval.Second);

            byte[] bytesRead = new byte[byteInterval.Second - byteInterval.First + 1];
            BinaryReader reader = new BinaryReader(new FileStream(Program.INPUT_FILE_PATH, FileMode.Open));

            reader.BaseStream.Seek(byteInterval.First, SeekOrigin.Begin);
            reader.Read(bytesRead, 0, bytesRead.Length);

            string text = Encoding.UTF8.GetString(bytesRead);

            List<string> lines = new List<string>();
            lines.Add(text);

            reader.Close();
            return lines;
        }

        public void submitJob(string inputFilePath, int splitsInputFormatted, string outputFolderPath, long fileSizeInputFormatted)
        {
            try
            {
                byte[] mapperCode = File.ReadAllBytes(MAP_FUNC_LOCATION);
                PADIMapNoReduce.IJobTracker jobTracker =
                   (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), worker_url);
                jobTracker.registerJob(inputFilePath, splitsInputFormatted, outputFolderPath, fileSizeInputFormatted, "tcp://localhost:" + Client_PORT + "/Client",
                    mapperCode, MAP_FUNC_CLASS_NAME);
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate server");
            }
                
        }

        public void receiveProcessData() {
 
            //Avisa o UserLevel App
        }

    }
}
