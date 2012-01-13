namespace NeuroRighter
{
    partial class NRConsole
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NRConsole));
            this.richTextBox_NRConsole = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // richTextBox_NRConsole
            // 
            this.richTextBox_NRConsole.Location = new System.Drawing.Point(13, 13);
            this.richTextBox_NRConsole.Name = "richTextBox_NRConsole";
            this.richTextBox_NRConsole.Size = new System.Drawing.Size(427, 468);
            this.richTextBox_NRConsole.TabIndex = 0;
            this.richTextBox_NRConsole.Text = "";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(365, 489);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Hide";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // NRConsole
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(452, 524);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.richTextBox_NRConsole);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "NRConsole";
            this.Text = "NeurRighter\'s Console";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        internal System.Windows.Forms.RichTextBox richTextBox_NRConsole;
    }
}