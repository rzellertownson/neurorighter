namespace NeuroRighter
{
    partial class ProcessingSettings
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
            this.checkBox_processLFPs = new System.Windows.Forms.CheckBox();
            this.button_cancel = new System.Windows.Forms.Button();
            this.button_accept = new System.Windows.Forms.Button();
            this.checkBox_processMUA = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // checkBox_processLFPs
            // 
            this.checkBox_processLFPs.AutoSize = true;
            this.checkBox_processLFPs.Location = new System.Drawing.Point(12, 12);
            this.checkBox_processLFPs.Name = "checkBox_processLFPs";
            this.checkBox_processLFPs.Size = new System.Drawing.Size(91, 17);
            this.checkBox_processLFPs.TabIndex = 0;
            this.checkBox_processLFPs.Text = "Process LFPs";
            this.checkBox_processLFPs.UseVisualStyleBackColor = true;
            // 
            // button_cancel
            // 
            this.button_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button_cancel.Location = new System.Drawing.Point(197, 81);
            this.button_cancel.Name = "button_cancel";
            this.button_cancel.Size = new System.Drawing.Size(75, 23);
            this.button_cancel.TabIndex = 1;
            this.button_cancel.Text = "Cancel";
            this.button_cancel.UseVisualStyleBackColor = true;
            this.button_cancel.Click += new System.EventHandler(this.button_cancel_Click);
            // 
            // button_accept
            // 
            this.button_accept.Location = new System.Drawing.Point(116, 81);
            this.button_accept.Name = "button_accept";
            this.button_accept.Size = new System.Drawing.Size(75, 23);
            this.button_accept.TabIndex = 2;
            this.button_accept.Text = "Accept";
            this.button_accept.UseVisualStyleBackColor = true;
            this.button_accept.Click += new System.EventHandler(this.button_accept_Click);
            // 
            // checkBox_processMUA
            // 
            this.checkBox_processMUA.AutoSize = true;
            this.checkBox_processMUA.Location = new System.Drawing.Point(12, 35);
            this.checkBox_processMUA.Name = "checkBox_processMUA";
            this.checkBox_processMUA.Size = new System.Drawing.Size(91, 17);
            this.checkBox_processMUA.TabIndex = 3;
            this.checkBox_processMUA.Text = "Process MUA";
            this.checkBox_processMUA.UseVisualStyleBackColor = true;
            // 
            // ProcessingSettings
            // 
            this.AcceptButton = this.button_accept;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.button_cancel;
            this.ClientSize = new System.Drawing.Size(284, 116);
            this.Controls.Add(this.checkBox_processMUA);
            this.Controls.Add(this.button_accept);
            this.Controls.Add(this.button_cancel);
            this.Controls.Add(this.checkBox_processLFPs);
            this.Name = "ProcessingSettings";
            this.Text = "Processing Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBox_processLFPs;
        private System.Windows.Forms.Button button_cancel;
        private System.Windows.Forms.Button button_accept;
        private System.Windows.Forms.CheckBox checkBox_processMUA;
    }
}