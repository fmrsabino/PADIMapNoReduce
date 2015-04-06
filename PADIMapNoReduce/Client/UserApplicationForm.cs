using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Windows.Forms;

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
                textBox4.Text = OutputDirectoryDialog.SelectedPath;
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            int clientPort = (int) numericUpDown1.Value;
            string entryUrl = "tcp://localhost:" + numericUpDown2.Value + "/W";
            client = new Client(entryUrl, clientPort);

            try
            {
                TcpChannel channel = new TcpChannel(clientPort);
                ChannelServices.RegisterChannel(channel, true);
                RemotingServices.Marshal(
                    client,
                    Client.CLIENT_OBJECT_ID,
                    typeof(Client));

                button2.Enabled = true;

            } catch (Exception i){
                System.Console.WriteLine("EXCEPTION: " + i.Message);
                Close();
            }
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

            string dllLocation = "";
            if (dllLocationValue.Text.Length > 0)
            {
                dllLocation = dllLocationValue.Text;
            }

            string dllClassName = "";
            if (classNameValue.Text.Length > 0)
            {
                dllClassName = classNameValue.Text;
            }
           
            button2.Enabled = false;
            client.submitJob(inputFilePath, splits, outputFolderPath, fileSize, dllLocation, dllClassName);
            button2.Enabled = true;
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void dllBrowseBtn_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = dllLocationDialog.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                dllLocationValue.Text = dllLocationDialog.FileName;
            }
        }

        private void classNameValue_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
