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
        private const int DEFAULCLIENTPORT = 15555;
        private int clientPort;
        private string inputFilePath;
        private int splits;
        private string outputFolderPath;
        private string dllLocation;
        private string dllClassName;
        private string entryUrl;

        public UserApplicationForm()
        {
            InitializeComponent();
        }

        public UserApplicationForm(string entryUrl, string inputFilePath, string outputPath, int nrSplits, string mapperClassName, string dllPath)
        {
            this.clientPort = DEFAULCLIENTPORT;
            this.entryUrl = entryUrl;
            this.inputFilePath = inputFilePath;
            this.outputFolderPath = outputPath;
            this.splits = nrSplits;
            this.dllClassName = mapperClassName;
            this.dllLocation = dllPath;
            InitializeComponent();
            Load += new EventHandler(UserApplicationForm_Load);
        }

        private void UserApplicationForm_Load(object sender, EventArgs e)
        {
            textBox3.Text = inputFilePath;
            textBox1.Text = entryUrl;
            numericUpDown1.Value = DEFAULCLIENTPORT;
            numericUpDown3.Value = splits;
            textBox4.Text = outputFolderPath;
            dllLocationValue.Text = dllLocation;
            classNameValue.Text = dllClassName;
            button1.Enabled = false;
            createClient();
            submitJob();
            button2.Enabled = true;
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
            createClient();
            button2.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            submitJob();
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

        private void createClient()
        {
            clientPort = (int)numericUpDown1.Value;
            entryUrl = textBox1.Text;
            client = new Client(entryUrl, clientPort);

            try
            {
                TcpChannel channel = new TcpChannel(clientPort);
                ChannelServices.RegisterChannel(channel, true);
                RemotingServices.Marshal(
                    client,
                    Client.CLIENT_OBJECT_ID,
                    typeof(Client));
            }
            catch (Exception i)
            {
                System.Console.WriteLine("EXCEPTION: " + i.Message);
                Close();
            }
        }

        private void submitJob()
        {         
            if (textBox3.Text.Length > 0)
            {
                inputFilePath = textBox3.Text;
            }

            splits = (int)numericUpDown3.Value;

            
            if (textBox4.Text.Length > 0)
            {
                outputFolderPath = textBox4.Text;
            }

            long fileSize = new FileInfo(inputFilePath).Length;

            if (dllLocationValue.Text.Length > 0)
            {
                dllLocation = dllLocationValue.Text;
            }

            
            if (classNameValue.Text.Length > 0)
            {
                dllClassName = classNameValue.Text;
            }

            button2.Enabled = false;
            client.submitJob(inputFilePath, splits, outputFolderPath, fileSize, dllLocation, dllClassName);
        }

    }
}
