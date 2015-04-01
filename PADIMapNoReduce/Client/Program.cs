using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Client
{
    class Program
    {

        public const string INPUT_FILE_PATH = "..\\..\\..\\test.txt";

        static void Main(string[] args)
        {

            Console.Title = "Client";

            while (true)
            {
                
                System.Console.WriteLine("Insert the path of the file you want to map and press <enter>:");
                string inputFilePath = System.Console.ReadLine();
                while (!File.Exists(inputFilePath)) {
                    System.Console.WriteLine(" Rewrite path and press <enter>: ");
                    inputFilePath = System.Console.ReadLine();
                }
           
                System.Console.WriteLine("Write the number of splits and press <enter> to send job");
                string splits = System.Console.ReadLine();
                int splitsInputFormatted;
                while (!int.TryParse(splits, out splitsInputFormatted))
                {
                    System.Console.WriteLine("Invalid number. Number of splits: ");
                    splits = System.Console.ReadLine();
                }
                

                System.Console.WriteLine("Insert the folder path where you want to keep the result and press <enter>:");
                string outputFolderPath = System.Console.ReadLine();
                while (!Directory.Exists(outputFolderPath)) {
                    System.Console.WriteLine("Invalid folder. Rewrite : ");
                    outputFolderPath = System.Console.ReadLine(); 
                }

                string worker_url = "tcp://localhost:1000/Worker";
                Client client = new Client(worker_url);
                long fileSize = new FileInfo(INPUT_FILE_PATH).Length;
                client.submitJob(inputFilePath, splitsInputFormatted, outputFolderPath, fileSize);

                System.Console.WriteLine("===============================");
                System.Console.WriteLine("===============================");
                System.Console.WriteLine("");
            }
        }
    }
}
