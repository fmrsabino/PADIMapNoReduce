namespace Client
{
    partial class UserApplicationForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.inputFile = new System.Windows.Forms.OpenFileDialog();
            this.browseInputFileBtn = new System.Windows.Forms.Button();
            this.OutputDrirectoryBrowseBtn = new System.Windows.Forms.Button();
            this.OutputDirectoryDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown3 = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.dllLocationValue = new System.Windows.Forms.TextBox();
            this.dllBrowseBtn = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.classNameValue = new System.Windows.Forms.TextBox();
            this.dllLocationDialog = new System.Windows.Forms.OpenFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(52, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Client Port";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(91, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Worker Entry Port";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(219, 7);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 48);
            this.button1.TabIndex = 4;
            this.button1.Text = "Create Client";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox3
            // 
            this.textBox3.Enabled = false;
            this.textBox3.Location = new System.Drawing.Point(113, 118);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(198, 20);
            this.textBox3.TabIndex = 5;
            this.textBox3.TextChanged += new System.EventHandler(this.textBox3_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(169, 93);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(77, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Map Operation";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(47, 121);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(50, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Input File";
            // 
            // textBox4
            // 
            this.textBox4.Enabled = false;
            this.textBox4.Location = new System.Drawing.Point(113, 144);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(198, 20);
            this.textBox4.TabIndex = 8;
            this.textBox4.TextChanged += new System.EventHandler(this.textBox4_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 147);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(84, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Output Directory";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 175);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(84, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Number of Splits";
            // 
            // button2
            // 
            this.button2.Enabled = false;
            this.button2.Location = new System.Drawing.Point(113, 282);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(198, 23);
            this.button2.TabIndex = 12;
            this.button2.Text = "Submit Job";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // inputFile
            // 
            this.inputFile.FileName = "inputFile";
            this.inputFile.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
            // 
            // browseInputFileBtn
            // 
            this.browseInputFileBtn.Enabled = false;
            this.browseInputFileBtn.Location = new System.Drawing.Point(317, 116);
            this.browseInputFileBtn.Name = "browseInputFileBtn";
            this.browseInputFileBtn.Size = new System.Drawing.Size(75, 23);
            this.browseInputFileBtn.TabIndex = 13;
            this.browseInputFileBtn.Text = "Browse";
            this.browseInputFileBtn.UseVisualStyleBackColor = true;
            this.browseInputFileBtn.Click += new System.EventHandler(this.button3_Click);
            // 
            // OutputDrirectoryBrowseBtn
            // 
            this.OutputDrirectoryBrowseBtn.Enabled = false;
            this.OutputDrirectoryBrowseBtn.Location = new System.Drawing.Point(317, 142);
            this.OutputDrirectoryBrowseBtn.Name = "OutputDrirectoryBrowseBtn";
            this.OutputDrirectoryBrowseBtn.Size = new System.Drawing.Size(75, 23);
            this.OutputDrirectoryBrowseBtn.TabIndex = 14;
            this.OutputDrirectoryBrowseBtn.Text = "Browse";
            this.OutputDrirectoryBrowseBtn.UseVisualStyleBackColor = true;
            this.OutputDrirectoryBrowseBtn.Click += new System.EventHandler(this.button3_Click_1);
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(113, 7);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            19999,
            0,
            0,
            0});
            this.numericUpDown1.Minimum = new decimal(new int[] {
            10001,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(100, 20);
            this.numericUpDown1.TabIndex = 15;
            this.numericUpDown1.Value = new decimal(new int[] {
            10001,
            0,
            0,
            0});
            // 
            // numericUpDown2
            // 
            this.numericUpDown2.Location = new System.Drawing.Point(113, 35);
            this.numericUpDown2.Maximum = new decimal(new int[] {
            39999,
            0,
            0,
            0});
            this.numericUpDown2.Minimum = new decimal(new int[] {
            30001,
            0,
            0,
            0});
            this.numericUpDown2.Name = "numericUpDown2";
            this.numericUpDown2.Size = new System.Drawing.Size(100, 20);
            this.numericUpDown2.TabIndex = 16;
            this.numericUpDown2.Value = new decimal(new int[] {
            30001,
            0,
            0,
            0});
            // 
            // numericUpDown3
            // 
            this.numericUpDown3.Location = new System.Drawing.Point(113, 173);
            this.numericUpDown3.Maximum = new decimal(new int[] {
            -1981284353,
            -1966660860,
            0,
            0});
            this.numericUpDown3.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown3.Name = "numericUpDown3";
            this.numericUpDown3.Size = new System.Drawing.Size(198, 20);
            this.numericUpDown3.TabIndex = 17;
            this.numericUpDown3.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(25, 219);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(71, 13);
            this.label7.TabIndex = 18;
            this.label7.Text = "DLL Location";
            this.label7.Click += new System.EventHandler(this.label7_Click);
            // 
            // dllLocationValue
            // 
            this.dllLocationValue.Location = new System.Drawing.Point(113, 216);
            this.dllLocationValue.Name = "dllLocationValue";
            this.dllLocationValue.Size = new System.Drawing.Size(198, 20);
            this.dllLocationValue.TabIndex = 19;
            // 
            // dllBrowseBtn
            // 
            this.dllBrowseBtn.Location = new System.Drawing.Point(317, 214);
            this.dllBrowseBtn.Name = "dllBrowseBtn";
            this.dllBrowseBtn.Size = new System.Drawing.Size(75, 23);
            this.dllBrowseBtn.TabIndex = 20;
            this.dllBrowseBtn.Text = "Browse";
            this.dllBrowseBtn.UseVisualStyleBackColor = true;
            this.dllBrowseBtn.Click += new System.EventHandler(this.dllBrowseBtn_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(28, 245);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(63, 13);
            this.label8.TabIndex = 21;
            this.label8.Text = "Class Name";
            this.label8.Click += new System.EventHandler(this.label8_Click);
            // 
            // classNameValue
            // 
            this.classNameValue.Location = new System.Drawing.Point(113, 245);
            this.classNameValue.Name = "classNameValue";
            this.classNameValue.Size = new System.Drawing.Size(198, 20);
            this.classNameValue.TabIndex = 22;
            this.classNameValue.TextChanged += new System.EventHandler(this.classNameValue_TextChanged);
            // 
            // dllLocationDialog
            // 
            this.dllLocationDialog.FileName = "dllLocationDialog";
            // 
            // UserApplicationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(410, 317);
            this.Controls.Add(this.classNameValue);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.dllBrowseBtn);
            this.Controls.Add(this.dllLocationValue);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.numericUpDown3);
            this.Controls.Add(this.numericUpDown2);
            this.Controls.Add(this.numericUpDown1);
            this.Controls.Add(this.OutputDrirectoryBrowseBtn);
            this.Controls.Add(this.browseInputFileBtn);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBox4);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "UserApplicationForm";
            this.Text = "UserApplicationForm";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.OpenFileDialog inputFile;
        private System.Windows.Forms.Button browseInputFileBtn;
        private System.Windows.Forms.Button OutputDrirectoryBrowseBtn;
        private System.Windows.Forms.FolderBrowserDialog OutputDirectoryDialog;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.NumericUpDown numericUpDown2;
        private System.Windows.Forms.NumericUpDown numericUpDown3;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox dllLocationValue;
        private System.Windows.Forms.Button dllBrowseBtn;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox classNameValue;
        private System.Windows.Forms.OpenFileDialog dllLocationDialog;
    }
}