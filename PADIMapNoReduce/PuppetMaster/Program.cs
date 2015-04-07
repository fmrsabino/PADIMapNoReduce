using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMaster
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Checking for argument presence
            if(args.Length != 6){
                MessageBox.Show("Error! Required arguments not provided. Exitting.");
                return;
            }

            int portNumber = 0;
            string workerExecutablePath = null;
            string clientExecutablePath = null;
            
            for (int i = 0; i < args.Length; i += 2)
            {

                if (args[i].Equals("-p"))
                {
                    try
                    {
                        int port = int.Parse(args[i+1]);

                        if (port > 0 && port < 65535)
                        {
                            portNumber = port;
                        }
                        else
                        {
                            MessageBox.Show("Error! Invalid port number provided. Exitting.\n");
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Error! Invalid port format provided. Exitting.\n" + e.Message);
                        return;
                    }
                }

                if (args[i].Equals("-w"))
                {
                    workerExecutablePath = args[i+1];
                }

                if (args[i].Equals("-c"))
                {
                    clientExecutablePath = args[i + 1];
                }

            }

            if (portNumber != 0 && workerExecutablePath != null && clientExecutablePath != null)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new PuppetMasterForm(portNumber, workerExecutablePath, clientExecutablePath));
            } else
            {
                List<string> missingArguments = new List<string>();

                if (portNumber == 0)
                    missingArguments.Add("Port number");

                if(workerExecutablePath == null)
                    missingArguments.Add("Worker executable path");

                if (clientExecutablePath == null)
                    missingArguments.Add("Client executable path");

                MessageBox.Show("Error! Argument(s) missing: " + string.Join(", ", missingArguments)  + ". Exitting.\n");
                return;
            }
        }
    }
}
