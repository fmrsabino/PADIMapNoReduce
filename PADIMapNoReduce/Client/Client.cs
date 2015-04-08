using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Client : MarshalByRefObject, PADIMapNoReduce.IClient
    {
        public const string CLIENT_OBJECT_ID = "C";
        private string entryUrl;
        private long clientPort;

        //public String inputFilePath;
        public string outputFolderPath;

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public Client(string worker, long clientPort)
        {
            entryUrl = worker;
            this.clientPort = clientPort;
        }

        public List<string> processBytes(PADIMapNoReduce.Pair<long, long> byteInterval, string filePath)
        {
            System.Console.WriteLine("Received request for bytes from " + byteInterval.First + " to " + byteInterval.Second);

            List<string> result = new List<string>();
            StringBuilder stringBuilder = new StringBuilder();

            long fileSize = new FileInfo(filePath).Length;
            if (byteInterval.First == 0 && byteInterval.Second == fileSize)
            {
                stringBuilder.Append(readByteInterval(byteInterval, filePath));
            }
            else
            {
                if (byteInterval.First == 0) //FirstSplit
                {
                    long lastByte;
                    stringBuilder.Append(readByteInterval(byteInterval, filePath));
                    if (!stringBuilder.ToString().EndsWith("\n"))
                    {
                        stringBuilder.Append(readUntilNewLine(byteInterval.Second + 1, filePath, out lastByte));
                    }
                }
                else if (byteInterval.Second == fileSize) //lastSplit
                {
                    long lastByte = byteInterval.First;
                    char lastChar = getCharFromBytePosition(byteInterval.First - 1, filePath);

                    if (lastChar != '\n')
                    {
                        readUntilNewLine(byteInterval.First, filePath, out lastByte);
                        lastByte++;
                    }

                    if (lastByte >= byteInterval.Second)
                    {
                        return result;
                    }

                    stringBuilder.Append(readByteInterval(new PADIMapNoReduce.Pair<long, long>(lastByte, fileSize), filePath));
                }
                else //middle split
                {
                    long lastByte = byteInterval.First;
                    char lastChar = getCharFromBytePosition(byteInterval.First - 1, filePath);

                    if (lastChar != '\n')
                    {
                        readUntilNewLine(byteInterval.First, filePath, out lastByte);
                        lastByte++;
                    }

                    if (lastByte > byteInterval.Second)
                    {
                        return result;
                    }

                    stringBuilder.Append(readByteInterval(new PADIMapNoReduce.Pair<long, long>(lastByte, byteInterval.Second), filePath));
                    if (!stringBuilder.ToString().EndsWith("\n"))
                    {
                        stringBuilder.Append(readUntilNewLine(byteInterval.Second + 1, filePath, out lastByte));
                    }
                }
            }
            System.Console.WriteLine(stringBuilder.ToString());

            string finalString = stringBuilder.ToString();
            string[] lines = finalString.Split(new string[] { Environment.NewLine }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                result.Add(line);
            }

            return result;
        }

        //Reads the character from the byte position
        private char getCharFromBytePosition(long bytePos, string filePath)
        {
            byte[] bytes = new byte[1];
            FileStream f = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read); 
            BinaryReader reader = new BinaryReader(f);

            reader.BaseStream.Seek(bytePos, SeekOrigin.Begin);
            reader.Read(bytes, 0, bytes.Length);
            string text = Encoding.UTF8.GetString(bytes);

            reader.Close();
            return text[0];
        }

        //Returns the string that corresponds to the byteInterval received
        private string readByteInterval(PADIMapNoReduce.Pair<long, long> byteInterval, string filePath)
        {
            if (byteInterval.First > byteInterval.Second)
            {
                return "";
            }

            byte[] bytes = new byte[byteInterval.Second - byteInterval.First + 1];
            FileStream f = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read); 
            BinaryReader reader = new BinaryReader(f);

            reader.BaseStream.Seek(byteInterval.First, SeekOrigin.Begin);
            reader.Read(bytes, 0, bytes.Length);
            string text = Encoding.UTF8.GetString(bytes);

            reader.Close();
            return text;
        }

        //Get the portion of the string from startByte to the first newline encountered
        private string readUntilNewLine(long startByte, string filePath, out long endByte)
        {
            StringBuilder sb = new StringBuilder();
            FileStream f = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read); 
            BinaryReader reader = new BinaryReader(f);
            long fileSize = new FileInfo(filePath).Length;
            byte[] bytes = new byte[20];

            long newLinePos = -1;
            while (startByte < fileSize)
            {
                //Retrieve more bytes
                reader.BaseStream.Seek(startByte, SeekOrigin.Begin);
                reader.Read(bytes, 0, bytes.Length);
                string text = Encoding.UTF8.GetString(bytes);

                if ((newLinePos = text.IndexOf("\n")) == -1)
                {
                    //safe to write to string
                    sb.Append(Encoding.UTF8.GetString(bytes));
                    startByte += 20;
                }
                else
                {
                    //We now have the correct value of newLinePos
                    break;
                }
            }

            //Read until new line
            endByte = startByte + newLinePos;
            reader.Close();
            sb.Append(readByteInterval(new PADIMapNoReduce.Pair<long, long>(startByte, endByte), filePath));
            return sb.ToString();
        }

        public void submitJob(string inputFilePath, int splitsInputFormatted, string outputFolderPath, long fileSizeInputFormatted,
            string mapDllLocation, string mapClassName)
        {
            this.outputFolderPath = outputFolderPath;

            try
            {
                byte[] mapperCode = File.ReadAllBytes(mapDllLocation);
                PADIMapNoReduce.IJobTracker jobTracker =
                   (PADIMapNoReduce.IJobTracker)Activator.GetObject(typeof(PADIMapNoReduce.IJobTracker), entryUrl);

                jobTracker.registerJob(inputFilePath, splitsInputFormatted, outputFolderPath, fileSizeInputFormatted,
                    "tcp://" + System.Environment.MachineName + ":" + clientPort + "/" + CLIENT_OBJECT_ID,
                    mapperCode, mapClassName);
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate server");
            }
        }

        public void receiveProcessData(string output, int n)
        {
            String outputFilePath = outputFolderPath + "/" + n + ".out";
            System.IO.File.WriteAllText(outputFilePath, output);
        }

        public void jobConcluded()
        {
            System.Console.WriteLine("////////////JOB CONCLUDED/////////////////");
        }
    }
}
