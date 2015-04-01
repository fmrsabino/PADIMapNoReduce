using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMaster
{
    public partial class PuppetMasterForm : Form
    {
        string puppetMasterURL;
        List<string> puppetMasters;
        List<Dictionary<int, string>> workers;
        List<string> commands;

        public PuppetMasterForm(int port)
        {
            InitializeComponent();

            puppetMasterURL = "tcp://localhost:" + port + "/";
            this.Text = "Puppet Master running on: " + puppetMasterURL;

            puppetMasters = new List<string>();
            workers = new List<Dictionary<int, string>>();
            commands = new List<string>();
        }

        private void executeCommand(string command) {
            MessageBox.Show("Executed command '" + command + "'.");

            // Regexes for commands
            // WORKER <ID> <PUPPETMASTER-URL> <SERVICE-URL> <ENTRY-URL>:
            Regex worker = new Regex("WORKER (\\d)+ (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+)");
            // SUBMIT <ENTRY-URL> <FILE> <OUTPUT> <S> <MAP> <DLL>
            Regex submit = new Regex("SUBMIT (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) ([\\\\/\\.:a-z,A-Z,0-9_]+) ([\\\\/\\.:a-z,A-Z,0-9_]+) (\\d+) ([a-z,A-Z,0-9_]+) ([\\\\/\\.:a-z,A-Z,0-9_]+)");
            Regex wait = new Regex("WORKER (\\d)+ (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+)");
            Regex status = new Regex("WORKER (\\d)+ (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+)");
            Regex sloww = new Regex("WORKER (\\d)+ (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+)");
            Regex freezew = new Regex("WORKER (\\d)+ (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+)");
            Regex unfreezew = new Regex("WORKER (\\d)+ (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+)");
            Regex freezec = new Regex("WORKER (\\d)+ (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+)");
            Regex unfreezec = new Regex("WORKER (\\d)+ (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+)");

            MatchCollection matches;
            // WORKER Command
            matches = worker.Matches(command);
            if (matches.Count > 0)
            {
                executeWORKERCommand(matches);
                return;
            }

            // SUBMIT Command
            matches = submit.Matches(command);
            if (matches.Count > 0)
            {
                executeSUBMITCommand(matches);
                return;
            }

            // WAIT Command
            matches = wait.Matches(command);
            if (matches.Count > 0)
            {
                executeWORKERCommand(matches);
                return;
            }

            // STATUS Command
            matches = status.Matches(command);
            if (matches.Count > 0)
            {
                executeWORKERCommand(matches);
                return;
            }

            // SLOWW Command
            matches = sloww.Matches(command);
            if (matches.Count > 0)
            {
                executeWORKERCommand(matches);
                return;
            }

            // FREEZEW Command
            matches = freezew.Matches(command);
            if (matches.Count > 0)
            {
                executeWORKERCommand(matches);
                return;
            }

            // UNFREEZEW Command
            matches = unfreezew.Matches(command);
            if (matches.Count > 0)
            {
                executeWORKERCommand(matches);
                return;
            }

            // FREEZEC Command
            matches = freezec.Matches(command);
            if (matches.Count > 0)
            {
                executeWORKERCommand(matches);
                return;
            }

            // UNFREEZEC Command
            matches = unfreezec.Matches(command);
            if (matches.Count > 0)
            {
                executeWORKERCommand(matches);
                return;
            }
        }

        private void executeWORKERCommand(MatchCollection matches) {
            try
            {
                int workerId = int.Parse(matches[0].Groups[1].Value);
                string PuppetMasterURL = matches[0].Groups[2].Value;
                string ServiceURL = matches[0].Groups[3].Value;
                string EntryURL = matches[0].Groups[4].Value;
                MessageBox.Show("workerId: " + workerId + "\nPuppetMasterURL: " + PuppetMasterURL + "\nServiceURL: " + ServiceURL + "\nEntryURL: " + EntryURL);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error parsing workerId: " + e.Message);
            }
        }

        private void executeSUBMITCommand(MatchCollection matches)
        {

            try
            {
                string EntryURL = matches[0].Groups[1].Value;
                string File = matches[0].Groups[2].Value;
                string Output = matches[0].Groups[3].Value;
                int Splits = int.Parse(matches[0].Groups[4].Value);
                string Map = matches[0].Groups[5].Value;
                string DLL = matches[0].Groups[6].Value;

                MessageBox.Show("EntryURL: " + EntryURL + "\nFile: " + File + "\nOutput: " + Output + "\nSplits: " + Splits + "\nMap: " + Map + "\nDLL: " + DLL);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error parsing splits: " + e.Message);
            }

        }


        private void openScriptDialog_FileOk(object sender, CancelEventArgs e)
        {
            System.IO.StreamReader reader = new System.IO.StreamReader(openScriptDialog.FileName);
            commands.Clear();
            try
            {
                string scriptContents;
                while (reader.Peek() >= 0)
                {
                    scriptContents = reader.ReadLine();
                    commands.Add(scriptContents);
                }
                this.commandListBox.Items.Clear();
                foreach (string command in commands)
                {
                    this.commandListBox.Items.Add(command);
                }
                this.commandListBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error loading the script file: " + ex.Message);
            }
            finally {
                reader.Close();
            }
        }

        private void openScriptButton_Click(object sender, EventArgs e)
        {
            openScriptDialog.ShowDialog();
        }

        private void executeCommandButton_Click(object sender, EventArgs e)
        {
            string executedCommand = (string)this.commandListBox.SelectedItem;
            if(this.commandListBox.SelectedIndex < this.commandListBox.Items.Count - 1) {
                this.commandListBox.SelectedIndex++;
            }
            executeCommand(executedCommand);
        }

        private void executeIndividualButton_Click(object sender, EventArgs e)
        {
            executeCommand(this.individualCommandBox.Text);
        }

        private void executeAllCommandsButton_Click(object sender, EventArgs e)
        {
            foreach(string command in this.commandListBox.Items){
                executeCommand(command);
            }
        }

    }
}
