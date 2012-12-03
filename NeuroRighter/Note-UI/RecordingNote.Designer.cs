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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RecordingNote));
            this.textBox_Note = new System.Windows.Forms.TextBox();
            this.Label_Note = new System.Windows.Forms.Label();
            this.button_EnterNote = new System.Windows.Forms.Button();
            this.label_TimeStamp = new System.Windows.Forms.Label();
            this.button_CancelNote = new System.Windows.Forms.Button();
            this.button_SettingSnapshot = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox_Note
            // 
            this.textBox_Note.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox_Note.Font = new System.Drawing.Font("Courier New", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Note.Location = new System.Drawing.Point(3, 20);
            this.textBox_Note.Multiline = true;
            this.textBox_Note.Name = "textBox_Note";
            this.textBox_Note.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_Note.Size = new System.Drawing.Size(506, 418);
            this.textBox_Note.TabIndex = 0;
            // 
            // Label_Note
            // 
            this.Label_Note.AutoSize = true;
            this.Label_Note.Dock = System.Windows.Forms.DockStyle.Left;
            this.Label_Note.Location = new System.Drawing.Point(3, 0);
            this.Label_Note.Name = "Label_Note";
            this.Label_Note.Size = new System.Drawing.Size(71, 17);
            this.Label_Note.TabIndex = 1;
            this.Label_Note.Text = "Enter a note..";
            this.Label_Note.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // button_EnterNote
            // 
            this.button_EnterNote.Location = new System.Drawing.Point(377, 3);
            this.button_EnterNote.Name = "button_EnterNote";
            this.button_EnterNote.Size = new System.Drawing.Size(60, 22);
            this.button_EnterNote.TabIndex = 2;
            this.button_EnterNote.Text = "Log Note";
            this.button_EnterNote.UseVisualStyleBackColor = true;
            this.button_EnterNote.Click += new System.EventHandler(this.button_EnterNote_Click);
            // 
            // label_TimeStamp
            // 
            this.label_TimeStamp.AutoSize = true;
            this.label_TimeStamp.Dock = System.Windows.Forms.DockStyle.Left;
            this.label_TimeStamp.Location = new System.Drawing.Point(3, 0);
            this.label_TimeStamp.Name = "label_TimeStamp";
            this.label_TimeStamp.Size = new System.Drawing.Size(69, 28);
            this.label_TimeStamp.TabIndex = 3;
            this.label_TimeStamp.Text = "Time Stamp: ";
            this.label_TimeStamp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // button_CancelNote
            // 
            this.button_CancelNote.Location = new System.Drawing.Point(443, 3);
            this.button_CancelNote.Name = "button_CancelNote";
            this.button_CancelNote.Size = new System.Drawing.Size(60, 22);
            this.button_CancelNote.TabIndex = 4;
            this.button_CancelNote.Text = "Cancel";
            this.button_CancelNote.UseVisualStyleBackColor = true;
            this.button_CancelNote.Click += new System.EventHandler(this.button_CancelNote_Click);
            // 
            // button_SettingSnapshot
            // 
            this.button_SettingSnapshot.Location = new System.Drawing.Point(311, 3);
            this.button_SettingSnapshot.Name = "button_SettingSnapshot";
            this.button_SettingSnapshot.Size = new System.Drawing.Size(60, 22);
            this.button_SettingSnapshot.TabIndex = 5;
            this.button_SettingSnapshot.Text = "Snapshot";
            this.button_SettingSnapshot.UseVisualStyleBackColor = true;
            this.button_SettingSnapshot.Click += new System.EventHandler(this.button_SettingSnapshot_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.textBox_Note, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.Label_Note, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 3.854875F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 96.14513F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(512, 475);
            this.tableLayoutPanel1.TabIndex = 8;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Controls.Add(this.button_SettingSnapshot, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.button_CancelNote, 3, 0);
            this.tableLayoutPanel2.Controls.Add(this.button_EnterNote, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.label_TimeStamp, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 444);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(506, 28);
            this.tableLayoutPanel2.TabIndex = 9;
            // 
            // RecordingNote
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(512, 475);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "RecordingNote";
            this.Text = "Log Entry";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_Note;
        private System.Windows.Forms.Label Label_Note;
        private System.Windows.Forms.Button button_EnterNote;
        private System.Windows.Forms.Label label_TimeStamp;
        private System.Windows.Forms.Button button_CancelNote;
        private System.Windows.Forms.Button button_SettingSnapshot;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
    }
}