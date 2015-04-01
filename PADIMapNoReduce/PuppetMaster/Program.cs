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
            if(args.Length != 2){
                MessageBox.Show("Error! Port arguments not provided. Exitting.");
                return;
            }

            // Validating provided port number and option
            if(args[0].Equals("-p")) {
                try
                {
                    int port = int.Parse(args[1]);

                    if (port > 0 && port < 65535)
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        Application.Run(new PuppetMasterForm(port));
                    }
                    else {
                        MessageBox.Show("Error! Invalid port number provided. Exitting.");
                        return ;
                    }
                }
                catch (Exception e){
                    MessageBox.Show("Error! Invalid port format provided. Exitting.\n" + e.Message);
                    return;
                }
            }
        }
    }
}
