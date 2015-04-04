using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Client
{
    class UserLevelApp
    {
        public string inputFilePath, outputFolderPath;
        public int splitsInputFormatted;
        private Client client;

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

        public void execute()
        {
            /*while (true)
            {
                //getInputFile();
                getNumberSplits();
                //getOutputFolder();

                long fileSize = new FileInfo(INPUT_FILE_PATH).Length;
                client.submitJob(inputFilePath, splitsInputFormatted, outputFolderPath, fileSize, MAP_FUNC_LOCATION, MAP_FUNC_CLASS_NAME);

                System.Console.WriteLine("===============================");
                System.Console.WriteLine("===============================");
                System.Console.WriteLine("");
            }*/
        }
    }
}
