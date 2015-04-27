using System;
using System.Collections.Generic;
using System.IO;
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

        public List<byte> processBytes(PADIMapNoReduce.Pair<long, long> byteInterval, string filePath)
        {
            //System.Console.WriteLine("Received request for bytes from " + byteInterval.First + " to " + byteInterval.Second);

            List<byte> result = new List<byte>();

            long fileSize = new FileInfo(filePath).Length;
            if (byteInterval.First == 0 && byteInterval.Second == fileSize)
            {
                return new List<byte>(readByteInterval(byteInterval, filePath));
            }
            else
            {
                if (byteInterval.First == 0) //FirstSplit
                {
                    long lastByte;
                    result.AddRange(new List<byte>(readByteInterval(byteInterval, filePath)));
                    if (getCharFromBytePosition(result[result.Count - 1], filePath) != '\n')
                    {
                        result.AddRange(new List<byte>(readUntilNewLine(byteInterval.Second + 1, filePath, out lastByte)));
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

                    result.AddRange(readByteInterval(new PADIMapNoReduce.Pair<long, long>(lastByte, fileSize), filePath));
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

                    result.AddRange(readByteInterval(new PADIMapNoReduce.Pair<long, long>(lastByte, byteInterval.Second), filePath));
                    if (getCharFromBytePosition(result[result.Count - 1], filePath) != '\n')
                    {
                        result.AddRange(readUntilNewLine(byteInterval.Second + 1, filePath, out lastByte));
                    }
                }
            }

            return result;
        }

        //Reads the character from the byte position
        private char getCharFromBytePosition(long bytePos, string filePath)
        {
            byte[] bytes = new byte[1];
            string text;
            using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                reader.BaseStream.Seek(bytePos, SeekOrigin.Begin);
                reader.Read(bytes, 0, bytes.Length);
                text = Encoding.UTF8.GetString(bytes);
            }

            return text[0];
        }

        //Returns the string that corresponds to the byteInterval received
        private byte[] readByteInterval(PADIMapNoReduce.Pair<long, long> byteInterval, string filePath)
        {
            if (byteInterval.First > byteInterval.Second)
            {
                return new byte[0];
            }

            byte[] bytes = new byte[byteInterval.Second - byteInterval.First + 1];
            using (BinaryReader reader = new BinaryReader(new BufferedStream(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))))
            {
                reader.BaseStream.Seek(byteInterval.First, SeekOrigin.Begin);
                reader.Read(bytes, 0, bytes.Length);
            }

            return bytes;
        }

        //Get the portion of the string from startByte to the first newline encountered
        private byte[] readUntilNewLine(long startByte, string filePath, out long endByte)
        {

            string sb = "";
            long fileSize = new FileInfo(filePath).Length;
            byte[] bytes = new byte[200];

            long newLinePos = -1;

            using (BinaryReader reader = new BinaryReader(new BufferedStream(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))))
            {
                while (startByte < fileSize)
                {
                    //Retrieve more bytes
                    reader.BaseStream.Seek(startByte, SeekOrigin.Begin);
                    reader.Read(bytes, 0, bytes.Length);
                    string text = Encoding.UTF8.GetString(bytes);

                    if ((newLinePos = text.IndexOf("\n")) == -1)
                    {
                        //safe to write to string
                        sb += Encoding.UTF8.GetString(bytes);
                        startByte += 20;
                    }
                    else
                    {
                        //We now have the correct value of newLinePos
                        break;
                    }
                }
            }

            //Read until new line
            endByte = startByte + newLinePos;
            return readByteInterval(new PADIMapNoReduce.Pair<long, long>(startByte, endByte), filePath);
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

        public void receiveProcessData(string lines, int n)
        {
            using (StreamWriter writer = File.AppendText(outputFolderPath + "/" + n + ".out"))
            {
                writer.Write(lines);
            }
            //System.IO.File.WriteAllText(outputFilePath, output);*/
        }

        public void jobConcluded()
        {
            System.Console.WriteLine("////////////JOB CONCLUDED/////////////////");
        }
    }
}
