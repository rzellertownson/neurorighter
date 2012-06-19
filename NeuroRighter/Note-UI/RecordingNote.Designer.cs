namespace NeuroRighter
{
    
    partial class RecordingNote
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
            this.textBox_Note = new System.Windows.Forms.TextBox();
            this.Label_Note = new System.Windows.Forms.Label();
            this.button_EnterNote = new System.Windows.Forms.Button();
            this.label_TimeStamp = new System.Windows.Forms.Label();
            this.button_CancelNote = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBox_Note
            // 
            this.textBox_Note.Location = new System.Drawing.Point(15, 22);
            this.textBox_Note.Multiline = true;
            this.textBox_Note.Name = "textBox_Note";
            this.textBox_Note.Size = new System.Drawing.Size(310, 266);
            this.textBox_Note.TabIndex = 0;
            // 
            // Label_Note
            // 
            this.Label_Note.AutoSize = true;
            this.Label_Note.Location = new System.Drawing.Point(12, 6);
            this.Label_Note.Name = "Label_Note";
            this.Label_Note.Size = new System.Drawing.Size(71, 13);
            this.Label_Note.TabIndex = 1;
            this.Label_Note.Text = "Enter a note..";
            // 
            // button_EnterNote
            // 
            this.button_EnterNote.Location = new System.Drawing.Point(259, 294);
            this.button_EnterNote.Name = "button_EnterNote";
            this.button_EnterNote.Size = new System.Drawing.Size(66, 23);
            this.button_EnterNote.TabIndex = 2;
            this.button_EnterNote.Text = "Log Note";
            this.button_EnterNote.UseVisualStyleBackColor = true;
            this.button_EnterNote.Click += new System.EventHandler(this.button_EnterNote_Click);
            // 
            // label_TimeStamp
            // 
            this.label_TimeStamp.AutoSize = true;
            this.label_TimeStamp.Location = new System.Drawing.Point(12, 299);
            this.label_TimeStamp.Name = "label_TimeStamp";
            this.label_TimeStamp.Size = new System.Drawing.Size(69, 13);
            this.label_TimeStamp.TabIndex = 3;
            this.label_TimeStamp.Text = "Time Stamp: ";
            // 
            // button_CancelNote
            // 
            this.button_CancelNote.Location = new System.Drawing.Point(183, 294);
            this.button_CancelNote.Name = "button_CancelNote";
            this.button_CancelNote.Size = new System.Drawing.Size(70, 23);
            this.button_CancelNote.TabIndex = 4;
            this.button_CancelNote.Text = "Cancel";
            this.button_CancelNote.UseVisualStyleBackColor = true;
            this.button_CancelNote.Click += new System.EventHandler(this.button_CancelNote_Click);
            // 
            // RecordingNote
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(337, 329);
            this.Controls.Add(this.button_CancelNote);
            this.Controls.Add(this.label_TimeStamp);
            this.Controls.Add(this.button_EnterNote);
            this.Controls.Add(this.Label_Note);
            this.Controls.Add(this.textBox_Note);
            this.Name = "RecordingNote";
            this.Text = "Log Entry";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_Note;
        private System.Windows.Forms.Label Label_Note;
        private System.Windows.Forms.Button button_EnterNote;
        private System.Windows.Forms.Label label_TimeStamp;
        private System.Windows.Forms.Button button_CancelNote;
    }
}