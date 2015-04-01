using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Client
{
    class UserLevelApp
    {
        public string inputFilePath, outputFolderPath;
        public int splitsInputFormatted;
        string worker_url = "tcp://localhost:1000/Worker";
        Client client;

        public UserLevelApp()
        {
            client = new Client(worker_url, this);
        }

        public void getInputFile()
        {
            System.Console.WriteLine("Insert the path of the file you want to map and press <enter>:");
            inputFilePath = System.Console.ReadLine();
            while (!File.Exists(inputFilePath))
            {
                System.Console.WriteLine(" Rewrite path and press <enter>: ");
                inputFilePath = System.Console.ReadLine();
            }
        }

        public void getNumberSplits()
        {
            System.Console.WriteLine("Write the number of splits and press <enter> to send job");
            string splits = System.Console.ReadLine();
            while (!int.TryParse(splits, out splitsInputFormatted))
            {
                System.Console.WriteLine("Invalid number. Number of splits: ");
                splits = System.Console.ReadLine();
            }
        }

        public void getOutputFolder()
        {
            System.Console.WriteLine("Insert the folder path where you want to keep the result and press <enter>:");
            outputFolderPath = System.Console.ReadLine();
            while (!Directory.Exists(outputFolderPath))
            {
                System.Console.WriteLine("Invalid folder. Rewrite : ");
                outputFolderPath = System.Console.ReadLine();
            }
        }

        public void createClient()
        {
            long fileSize = new FileInfo(inputFilePath).Length;
            client.submitJob(inputFilePath, splitsInputFormatted, outputFolderPath, fileSize);

            System.Console.WriteLine("===============================");
            System.Console.WriteLine("===============================");
            System.Console.WriteLine("");
        }

        public void execute()
        {
            getInputFile();
            getNumberSplits();
            getOutputFolder();
            createClient();
        }
    }
}
