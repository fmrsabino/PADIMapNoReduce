using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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
                foreach (string command in commands) {
                    this.commandListBox.Items.Add(command);
                }
                this.commandListBox.SelectedIndex = 0;
            }
            catch (Exception ex) {
                MessageBox.Show("There was an error loading the script file: " + ex.Message);
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
