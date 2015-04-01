using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;

namespace Client
{
    class Client : MarshalByRefObject, PADIMapNoReduce.IClient
    {
        public int Client_PORT = 5000;
        public String worker_url;
        public const string MAP_FUNC_LOCATION = "..\\..\\..\\LibMapper\\bin\\Debug\\LibMapper.dll";
        public const string MAP_FUNC_CLASS_NAME = "Mapper";
        public UserLevelApp userApp;
        //ESTE PATH ESTÀ AQUI PARA POREM O CAMINHO CERTO NO TERMINAL
        //public const string INPUT_FILE_PATH = "..\\..\\..\\test.txt";         
        public String inputFilePath;
        public String outputFolderPath;

        public Client(string worker, UserLevelApp userApp)
        {
            worker_url = worker;
            this.userApp = userApp;

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
            BinaryReader reader = new BinaryReader(new FileStream(inputFilePath, FileMode.Open));

            reader.BaseStream.Seek(byteInterval.First, SeekOrigin.Begin);
            reader.Read(bytesRead, 0, bytesRead.Length);

            List<string> result = new List<string>();
            string text = Encoding.UTF8.GetString(bytesRead);
            StringBuilder stringBuilder = new StringBuilder();

            if (byteInterval.Second == new FileInfo(inputFilePath).Length)
            {
                if (text[text.Length - 1] != '\n')
                {
                    System.Console.WriteLine("Need to fetch more bytes to complete line");


                    byte[] newBytes = new byte[20];


                    StringBuilder sb = new StringBuilder(text);
                    bool foundNewLine = false;
                    long currentPosition = byteInterval.Second;

                    while (!foundNewLine)
                    {
                        //Retrieve more bytes
                        reader.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
                        reader.Read(newBytes, 0, newBytes.Length);
                        string newText = Encoding.UTF8.GetString(newBytes);

                        //Scan for the new line
                        foreach (char c in newText)
                        {
                            if (c != '\n')
                            {
                                sb.Append(c);
                            }
                            else
                            {
                                foundNewLine = true;
                                text = sb.ToString();
                                break;
                            }
                        }
                        currentPosition += 20;
                    }
                }
            }

            /*
            foreach (char c in text)
            {
                if (c == '\n')
                {
                    result.Add(stringBuilder.ToString());
                    System.Console.WriteLine("Found New Line");
                }
                else
                {
                    stringBuilder.Append(c);
                }
            }
            */

            System.Console.WriteLine(text);

            /*
            string[] parts = Regex.Split(text, @"(?<=\\n)");
            foreach (string s in parts)
            {
                System.Console.Write("Last char is: " + s[s.Length - 1]);
                //result.Add(s);
                /*if (s[s.Length - 1] == '\n')
                {
                    System.Console.WriteLine(s + " ends with a new line");
                    result.Add(s);
                }
                else
                {
                    System.Console.WriteLine(s + " doesn't end with a new line");
                }
            }*/

            /*

            StringReader stringReader = new StringReader(text);
            string line;
            while ((line = stringReader.ReadLine()) != null)
            {
                if (line[line.Length - 1] == '\n')
                {
                    result.Add(line);
                }
            }
            */
            reader.Close();
            return result;
        }

        public void submitJob(string inputFilePath, int splitsInputFormatted, string outputFolderPath, long fileSizeInputFormatted)
        {
            this.inputFilePath = inputFilePath;
            this.outputFolderPath = outputFolderPath;
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

        public void receiveProcessData()
        {
            userApp.execute();
        }

    }
}
