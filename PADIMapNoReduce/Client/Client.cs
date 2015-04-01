using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
namespace Client
{
    class Client : MarshalByRefObject, PADIMapNoReduce.IClient
    {
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
    }
}
