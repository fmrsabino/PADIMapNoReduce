using System;
using System.Windows.Forms;

namespace Client
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UserApplicationForm());

            /*Console.Title = "Client";
            UserLevelApp userApplication = new UserLevelApp();
            userApplication.execute();*/
        }
    }
}
