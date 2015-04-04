using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Client
{
    public partial class UserApplicationForm : Form
    {
        private Client client;
        public UserApplicationForm()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = inputFile.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                textBox3.Text = inputFile.FileName;
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            DialogResult dialogResult = OutputDirectoryDialog.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                textBox4.Text = inputFile.FileName;
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            long clientPort = (long) numericUpDown1.Value;
            string entryUrl = "tcp://localhost:" + numericUpDown2.Value + "/W";
            client = new Client(entryUrl, clientPort);

            textBox3.Enabled = true;
            textBox4.Enabled = true;
            browseInputFileBtn.Enabled = true;
            OutputDrirectoryBrowseBtn.Enabled = true;
            button2.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string inputFilePath = "";
            if(textBox3.Text.Length > 0) {
                inputFilePath = textBox3.Text;
            }
            
            int splits = (int) numericUpDown3.Value;

            string outputFolderPath = "";
            if(textBox4.Text.Length > 0) {
                outputFolderPath = textBox4.Text;
            }

            long fileSize = new FileInfo(inputFilePath).Length;

            client.submitJob(inputFilePath, splits, outputFolderPath, fileSize);
        }
    }
}
