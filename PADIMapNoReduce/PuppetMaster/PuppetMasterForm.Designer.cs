namespace PuppetMaster
{
    partial class PuppetMasterForm
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
            this.commandListBox = new System.Windows.Forms.ListBox();
            this.openScriptButton = new System.Windows.Forms.Button();
            this.executeCommandButton = new System.Windows.Forms.Button();
            this.individualCommandBox = new System.Windows.Forms.TextBox();
            this.executeIndividualButton = new System.Windows.Forms.Button();
            this.openScriptDialog = new System.Windows.Forms.OpenFileDialog();
            this.executeAllCommandsButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // commandListBox
            // 
            this.commandListBox.FormattingEnabled = true;
            this.commandListBox.Location = new System.Drawing.Point(12, 12);
            this.commandListBox.Name = "commandListBox";
            this.commandListBox.Size = new System.Drawing.Size(629, 368);
            this.commandListBox.TabIndex = 0;
            // 
            // openScriptButton
            // 
            this.openScriptButton.Location = new System.Drawing.Point(648, 382);
            this.openScriptButton.Name = "openScriptButton";
            this.openScriptButton.Size = new System.Drawing.Size(75, 47);
            this.openScriptButton.TabIndex = 1;
            this.openScriptButton.Text = "Open Script";
            this.openScriptButton.UseVisualStyleBackColor = true;
            this.openScriptButton.Click += new System.EventHandler(this.openScriptButton_Click);
            // 
            // executeCommandButton
            // 
            this.executeCommandButton.Location = new System.Drawing.Point(648, 12);
            this.executeCommandButton.Name = "executeCommandButton";
            this.executeCommandButton.Size = new System.Drawing.Size(75, 192);
            this.executeCommandButton.TabIndex = 2;
            this.executeCommandButton.Text = "Execute Selected Command";
            this.executeCommandButton.UseVisualStyleBackColor = true;
            this.executeCommandButton.Click += new System.EventHandler(this.executeCommandButton_Click);
            // 
            // individualCommandBox
            // 
            this.individualCommandBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.individualCommandBox.Location = new System.Drawing.Point(12, 386);
            this.individualCommandBox.Name = "individualCommandBox";
            this.individualCommandBox.Size = new System.Drawing.Size(541, 38);
            this.individualCommandBox.TabIndex = 3;
            // 
            // executeIndividualButton
            // 
            this.executeIndividualButton.Location = new System.Drawing.Point(561, 382);
            this.executeIndividualButton.Name = "executeIndividualButton";
            this.executeIndividualButton.Size = new System.Drawing.Size(80, 47);
            this.executeIndividualButton.TabIndex = 4;
            this.executeIndividualButton.Text = "Execute individual";
            this.executeIndividualButton.UseVisualStyleBackColor = true;
            this.executeIndividualButton.Click += new System.EventHandler(this.executeIndividualButton_Click);
            // 
            // openScriptDialog
            // 
            this.openScriptDialog.FileName = "commands.PADIscript";
            this.openScriptDialog.FileOk += new System.ComponentModel.CancelEventHandler(this.openScriptDialog_FileOk);
            // 
            // executeAllCommandsButton
            // 
            this.executeAllCommandsButton.Location = new System.Drawing.Point(648, 210);
            this.executeAllCommandsButton.Name = "executeAllCommandsButton";
            this.executeAllCommandsButton.Size = new System.Drawing.Size(75, 170);
            this.executeAllCommandsButton.TabIndex = 5;
            this.executeAllCommandsButton.Text = "Execute All Commands";
            this.executeAllCommandsButton.UseVisualStyleBackColor = true;
            this.executeAllCommandsButton.Click += new System.EventHandler(this.executeAllCommandsButton_Click);
            // 
            // PuppetMasterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(725, 430);
            this.Controls.Add(this.executeAllCommandsButton);
            this.Controls.Add(this.executeIndividualButton);
            this.Controls.Add(this.individualCommandBox);
            this.Controls.Add(this.executeCommandButton);
            this.Controls.Add(this.openScriptButton);
            this.Controls.Add(this.commandListBox);
            this.Name = "PuppetMasterForm";
            this.Text = "Puppet Master";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox commandListBox;
        private System.Windows.Forms.Button openScriptButton;
        private System.Windows.Forms.Button executeCommandButton;
        private System.Windows.Forms.TextBox individualCommandBox;
        private System.Windows.Forms.Button executeIndividualButton;
        private System.Windows.Forms.OpenFileDialog openScriptDialog;
        private System.Windows.Forms.Button executeAllCommandsButton;
    }
}

