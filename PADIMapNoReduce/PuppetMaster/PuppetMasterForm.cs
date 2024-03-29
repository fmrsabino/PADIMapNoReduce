﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace PuppetMaster
{
    public partial class PuppetMasterForm : Form
    {
        string puppetMasterURL, workerExecutablePath, clientExecutablePath;
        List<string> puppetMasters;
        List<string> commands;

        public delegate void MockStartWorker(int id, string serviceURL, string entryURL);
        public MockStartWorker delegateMockStartWorker;

        public PuppetMasterForm(int port, string _workerExecutablePath, string _clientExecutablePath)
        {
            InitializeComponent();

            puppetMasterURL = "tcp://localhost:" + port + "/PM";
            this.Text = "Puppet Master running on: " + puppetMasterURL;

            puppetMasters = new List<string>();
	    puppetMasters.Add(puppetMasterURL);
            commands = new List<string>();

            workerExecutablePath = _workerExecutablePath;
            clientExecutablePath = _clientExecutablePath;

            delegateMockStartWorker = new MockStartWorker(mockStartWorker);
        }

        private void executeCommand(string command) {

            // Regexes for commands
            // WORKER <ID> <PUPPETMASTER-URL> <SERVICE-URL>:
            Regex jobtracker = new Regex("^WORKER (\\d+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+)$");
            // WORKER <ID> <PUPPETMASTER-URL> <SERVICE-URL> <ENTRY-URL>:
            Regex worker = new Regex("^WORKER (\\d+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+)$");
            // SUBMIT <ENTRY-URL> <FILE> <OUTPUT> <S> <MAP> <DLL>
            Regex submit = new Regex("^SUBMIT (tcp://[a-z,A-Z,0-9]+:\\d+/[a-z,A-Z,0-9_]+) ([\\\\/\\.:a-z,A-Z,0-9_]+) ([\\\\/\\.:a-z,A-Z,0-9_]+) (\\d+) ([a-z,A-Z,0-9_]+) ([\\\\/\\.:a-z,A-Z,0-9_]+)$");
            //WAIT <SECS>
            Regex wait = new Regex("^WAIT (\\d+)$");
            //STATUS
            Regex status = new Regex("^STATUS$");
            //SLOWW <ID> <delay-in-seconds>
            Regex sloww = new Regex("^SLOWW (\\d+) (\\d+)$");
            //FREEZEW <ID>
            Regex freezew = new Regex("^FREEZEW (\\d+)$");
            //UNFREEZEW <ID>
            Regex unfreezew = new Regex("^UNFREEZEW (\\d+)$");
            //FREEZEC <ID>
            Regex freezec = new Regex("^FREEZEC (\\d+)$");
            //UNFREEZEC <ID>
            Regex unfreezec = new Regex("^UNFREEZEC (\\d+)$");
            // Any commented out command
            Regex comment = new Regex("^%");

            MatchCollection matches;
            
            // WORKER Command for jobtracker
            matches = jobtracker.Matches(command);
            if (matches.Count > 0)
            {
                executeWORKERCommand(matches);
                return;
            }

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
                executeWAITCommand(matches);
                return;
            }

            // STATUS Command
            matches = status.Matches(command);
            if (matches.Count > 0)
            {
                executeSTATUSCommand(matches);
                return;
            }

            // SLOWW Command
            matches = sloww.Matches(command);
            if (matches.Count > 0)
            {
                executeSLOWWCommand(matches);
                return;
            }

            // FREEZEW Command
            matches = freezew.Matches(command);
            if (matches.Count > 0)
            {
                executeFREEZEWCommand(matches);
                return;
            }

            // UNFREEZEW Command
            matches = unfreezew.Matches(command);
            if (matches.Count > 0)
            {
                executeUNFREEZEWCommand(matches);
                return;
            }

            // FREEZEC Command
            matches = freezec.Matches(command);
            if (matches.Count > 0)
            {
                executeFREEZECCommand(matches);
                return;
            }

            // UNFREEZEC Command
            matches = unfreezec.Matches(command);
            if (matches.Count > 0)
            {
                executeUNFREEZECCommand(matches);
                return;
            }

            // Commented out Command
            matches = comment.Matches(command);
            if (matches.Count > 0)
            {
                // Do nothing
                return;
            }

            // Else - no command matched
            MessageBox.Show("Error! Not a valid command in this system.");
        }

        private void executeWORKERCommand(MatchCollection matches) {
            try
            {
                int workerId = int.Parse(matches[0].Groups[1].Value);
                string PuppetMasterURL = matches[0].Groups[2].Value;
                string ServiceURL = matches[0].Groups[3].Value;
                string EntryURL = matches[0].Groups[4] != null ? matches[0].Groups[4].Value : "";

                PADIMapNoReduce.IPuppetMaster pm = (PADIMapNoReduce.IPuppetMaster)Activator.GetObject(
                    typeof(PADIMapNoReduce.IPuppetMaster), PuppetMasterURL);

                bool result = pm.startWorker(workerId, ServiceURL, EntryURL);
                if(!result)
                {
                    MessageBox.Show("workerId: " + workerId + "\nPuppetMasterURL: " + PuppetMasterURL + "\nServiceURL: " + ServiceURL + "\nEntryURL: " + EntryURL + "\nFailed to start! A worker with the same id may already exist or something weird may be happening with Windows - protip: Linux");
                }
                if (!puppetMasters.Contains(PuppetMasterURL))
                {
                    puppetMasters.Add(PuppetMasterURL);
                }

            }
            catch (Exception e)
            {
                MessageBox.Show("Error encountered: " + e.Message);
            }
        }

        private void executeSUBMITCommand(MatchCollection matches)
        {

            try
            {
                //SUBMIT <ENTRY-URL> <FILE> <OUTPUT> <S> <MAP> <DLL>
                string EntryURL = matches[0].Groups[1].Value;
                string File = matches[0].Groups[2].Value;
                string Output = matches[0].Groups[3].Value;
                int Splits = int.Parse(matches[0].Groups[4].Value);
                string Map = matches[0].Groups[5].Value;
                string DLL = matches[0].Groups[6].Value;

                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.FileName = clientExecutablePath;
                p.StartInfo.Arguments = EntryURL + " " + File + " " + Output + " " + Splits + " " + Map + " " + DLL;
                p.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error parsing splits: " + e.Message);
            }

        }

        private void executeWAITCommand(MatchCollection matches)
        {

            try
            {
                int seconds = int.Parse(matches[0].Groups[1].Value);

                //MessageBox.Show("WAIT - Seconds: " + seconds);
                Thread.Sleep(seconds * 1000);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error parsing seconds: " + e.Message);
            }

        }

        private void executeSTATUSCommand(MatchCollection matches)
        {
		foreach(String puppetMaster in puppetMasters){
            		PADIMapNoReduce.IPuppetMaster pm = (PADIMapNoReduce.IPuppetMaster)Activator.GetObject(
    typeof(PADIMapNoReduce.IPuppetMaster), puppetMaster);
            		pm.printStatus();
		}
        }

        private void executeSLOWWCommand(MatchCollection matches)
        {

            try
            {
                int id = int.Parse(matches[0].Groups[1].Value);
                int seconds = int.Parse(matches[0].Groups[2].Value);
		foreach(String puppetMaster in puppetMasters){
                	PADIMapNoReduce.IPuppetMaster pm = (PADIMapNoReduce.IPuppetMaster)Activator.GetObject(
typeof(PADIMapNoReduce.IPuppetMaster), puppetMaster);
                	pm.sloww(id, seconds);
		}
            }
            catch (Exception e)
            {
                MessageBox.Show("Error parsing id or seconds: " + e.Message);
            }

        }

        private void executeFREEZEWCommand(MatchCollection matches)
        {

            try
            {
                int id = int.Parse(matches[0].Groups[1].Value); 
		foreach(String puppetMaster in puppetMasters){
			PADIMapNoReduce.IPuppetMaster pm = (PADIMapNoReduce.IPuppetMaster)Activator.GetObject(
typeof(PADIMapNoReduce.IPuppetMaster), puppetMaster);
                	pm.freezew(id);
		}
            }
            catch (Exception e)
            {
                MessageBox.Show("Error parsing id: " + e.Message);
            }

        }

        private void executeUNFREEZEWCommand(MatchCollection matches)
        {

            try
            {
                int id = int.Parse(matches[0].Groups[1].Value);

		foreach(String puppetMaster in puppetMasters){
                	PADIMapNoReduce.IPuppetMaster pm = (PADIMapNoReduce.IPuppetMaster)Activator.GetObject(
typeof(PADIMapNoReduce.IPuppetMaster), puppetMaster);
                	pm.unfreezew(id);
		}
            }
            catch (Exception e)
            {
                MessageBox.Show("Error parsing id: " + e.Message);
            }

        }

        private void executeFREEZECCommand(MatchCollection matches)
        {

            try
            {
                int id = int.Parse(matches[0].Groups[1].Value);

		foreach(String puppetMaster in puppetMasters){
                	PADIMapNoReduce.IPuppetMaster pm = (PADIMapNoReduce.IPuppetMaster)Activator.GetObject(
typeof(PADIMapNoReduce.IPuppetMaster), puppetMaster);
                	pm.freezec(id);
		}
            }
            catch (Exception e)
            {
                MessageBox.Show("Error parsing id: " + e.Message);
            }

        }

        private void executeUNFREEZECCommand(MatchCollection matches)
        {

            try
            {
                int id = int.Parse(matches[0].Groups[1].Value);

		foreach(String puppetMaster in puppetMasters){
                	PADIMapNoReduce.IPuppetMaster pm = (PADIMapNoReduce.IPuppetMaster)Activator.GetObject(
typeof(PADIMapNoReduce.IPuppetMaster), puppetMaster);
                	pm.unfreezec(id);
		}
            }
            catch (Exception e)
            {
                MessageBox.Show("Error parsing id: " + e.Message);
            }

        }

        // This mock method provides a way to test our remoting object
        private void mockStartWorker(int id, string serviceURL, string entryURL) {
            MessageBox.Show("Spawned worker id: " + id + "\nat Service URL: " + serviceURL + "\nusing the Entry URL: " + entryURL);
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
