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
            if(args.Length != 4){
                MessageBox.Show("Error! Required arguments not provided. Exitting.");
                return;
            }

            // Attempt to obtain workerExecutablePath
            string workerExecutablePath = null;

            if(args[0].Equals("-w"))
            {
                workerExecutablePath = args[1];
            }

            if (args[2].Equals("-w"))
            {
                workerExecutablePath = args[3];
            }


            // Attempt to obtain port number
            int portNumber = 0;

            if (args[0].Equals("-p"))
            {
                try
                {
                    int port = int.Parse(args[1]);

                    if (port > 0 && port < 65535)
                    {
                        portNumber = port;
                    } else
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

            if (args[2].Equals("-p"))
            {
                try
                {
                    int port = int.Parse(args[3]);

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

            if(workerExecutablePath != null && portNumber != 0)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new PuppetMasterForm(portNumber, workerExecutablePath));
            } else
            {
                MessageBox.Show("Error! Worker executable path not provided. Exitting.\n");
                return;
            }
        }
    }
}
