namespace NeuroRighter
{
    partial class DisplaySettings
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioButton_inVivoMapping = new System.Windows.Forms.RadioButton();
            this.radioButton_inVitroMapping = new System.Windows.Forms.RadioButton();
            this.button_cancel = new System.Windows.Forms.Button();
            this.button_accept = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioButton_inVitroMapping);
            this.groupBox1.Controls.Add(this.radioButton_inVivoMapping);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(260, 72);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Channel Mappings";
            // 
            // radioButton_inVivoMapping
            // 
            this.radioButton_inVivoMapping.AutoSize = true;
            this.radioButton_inVivoMapping.Checked = true;
            this.radioButton_inVivoMapping.Location = new System.Drawing.Point(6, 19);
            this.radioButton_inVivoMapping.Name = "radioButton_inVivoMapping";
            this.radioButton_inVivoMapping.Size = new System.Drawing.Size(57, 17);
            this.radioButton_inVivoMapping.TabIndex = 0;
            this.radioButton_inVivoMapping.TabStop = true;
            this.radioButton_inVivoMapping.Text = "In vivo";
            this.radioButton_inVivoMapping.UseVisualStyleBackColor = true;
            // 
            // radioButton_inVitroMapping
            // 
            this.radioButton_inVitroMapping.AutoSize = true;
            this.radioButton_inVitroMapping.Location = new System.Drawing.Point(6, 42);
            this.radioButton_inVitroMapping.Name = "radioButton_inVitroMapping";
            this.radioButton_inVitroMapping.Size = new System.Drawing.Size(89, 17);
            this.radioButton_inVitroMapping.TabIndex = 1;
            this.radioButton_inVitroMapping.TabStop = true;
            this.radioButton_inVitroMapping.Text = "In vitro (MCS)";
            this.radioButton_inVitroMapping.UseVisualStyleBackColor = true;
            // 
            // button_cancel
            // 
            this.button_cancel.Location = new System.Drawing.Point(197, 108);
            this.button_cancel.Name = "button_cancel";
            this.button_cancel.Size = new System.Drawing.Size(75, 23);
            this.button_cancel.TabIndex = 1;
            this.button_cancel.Text = "Cancel";
            this.button_cancel.UseVisualStyleBackColor = true;
            this.button_cancel.Click += new System.EventHandler(this.button_cancel_Click);
            // 
            // button_accept
            // 
            this.button_accept.Location = new System.Drawing.Point(116, 108);
            this.button_accept.Name = "button_accept";
            this.button_accept.Size = new System.Drawing.Size(75, 23);
            this.button_accept.TabIndex = 2;
            this.button_accept.Text = "Accept";
            this.button_accept.UseVisualStyleBackColor = true;
            this.button_accept.Click += new System.EventHandler(this.button_accept_Click);
            // 
            // DisplaySettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 141);
            this.Controls.Add(this.button_accept);
            this.Controls.Add(this.button_cancel);
            this.Controls.Add(this.groupBox1);
            this.Name = "DisplaySettings";
            this.Text = "DisplaySettings";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioButton_inVitroMapping;
        private System.Windows.Forms.RadioButton radioButton_inVivoMapping;
        private System.Windows.Forms.Button button_cancel;
        private System.Windows.Forms.Button button_accept;
    }
}