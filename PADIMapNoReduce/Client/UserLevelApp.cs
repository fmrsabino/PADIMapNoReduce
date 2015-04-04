using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Client
{
    class UserLevelApp
    {
        //Port Range for Client: 10001-19999
        public const int CLIENT_PORT = 10001;
        public const string INPUT_FILE_PATH = "..\\..\\..\\test.txt";
        public const string MAP_FUNC_LOCATION = "..\\..\\..\\LibMapper\\bin\\Debug\\LibMapper.dll";
        public const string MAP_FUNC_CLASS_NAME = "Mapper";
        
        public string inputFilePath, outputFolderPath;
        public int splitsInputFormatted;
       
        private string worker_url = "tcp://localhost:30001/W";
        private Client client;

        public UserLevelApp()
        {
            client = new Client(worker_url, CLIENT_PORT);

            TcpChannel channel = new TcpChannel(CLIENT_PORT);
            ChannelServices.RegisterChannel(channel, true);
            RemotingServices.Marshal(
                client,
                Client.CLIENT_OBJECT_ID,
                typeof(Client));
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

        public void execute()
        {
            while (true)
            {
                //getInputFile();
                getNumberSplits();
                //getOutputFolder();

                long fileSize = new FileInfo(INPUT_FILE_PATH).Length;
                client.submitJob(inputFilePath, splitsInputFormatted, outputFolderPath, fileSize);

                System.Console.WriteLine("===============================");
                System.Console.WriteLine("===============================");
                System.Console.WriteLine("");
            }
        }
    }
}
