using System;
using System.Windows.Forms;

namespace Client
{
    class Program
    {

        [STAThread]
        static void Main(string[] args)
        {

            UserApplicationForm userApp;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length == 0)
            {
                userApp = new UserApplicationForm();
                Application.Run(userApp);

            }
            else {

                //ARGS: entryURL inputFilePath outputPath nrSplits NameClass MapperDLLPath
                string entryUrl = args[0];
                string inputFilePath = args[1];
                string outputPath = args[2];
                int nrSplitsFormatted;
                string mapperClassName = args[4];
                string dllPath = args[5];

                if (!int.TryParse(args[3], out nrSplitsFormatted))
                {
                    System.Console.WriteLine("Invalid number of splits format supplied. Exitting...");
                    return;
                }

                userApp = new UserApplicationForm(entryUrl, inputFilePath, outputPath, nrSplitsFormatted, mapperClassName, dllPath);
                Application.Run(userApp);
            }
        }
    }
}
