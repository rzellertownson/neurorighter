namespace NR_CL_Examples
{
    partial class SeizureControlPanel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SeizureControlPanel));
            this.zgc = new ZedGraph.ZedGraphControl();
            this.button_EngageFB = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.numericUpDown_StimAmpVolts = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.numericUpDown_StimTimeSec = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpDown_StimFreqHz = new System.Windows.Forms.NumericUpDown();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label_SelectedStat = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.button_RetrainThresh = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.numericUpDown_TreshK = new System.Windows.Forms.NumericUpDown();
            this.backgroundWorker_UpdateUI = new System.ComponentModel.BackgroundWorker();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_StimAmpVolts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_StimTimeSec)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_StimFreqHz)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_TreshK)).BeginInit();
            this.SuspendLayout();
            // 
            // zgc
            // 
            this.zgc.EditButtons = System.Windows.Forms.MouseButtons.Left;
            this.zgc.Location = new System.Drawing.Point(12, 159);
            this.zgc.Name = "zgc";
            this.zgc.PanModifierKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.None)));
            this.zgc.ScrollGrace = 0D;
            this.zgc.ScrollMaxX = 0D;
            this.zgc.ScrollMaxY = 0D;
            this.zgc.ScrollMaxY2 = 0D;
            this.zgc.ScrollMinX = 0D;
            this.zgc.ScrollMinY = 0D;
            this.zgc.ScrollMinY2 = 0D;
            this.zgc.Size = new System.Drawing.Size(966, 300);
            this.zgc.TabIndex = 82;
            // 
            // button_EngageFB
            // 
            this.button_EngageFB.Location = new System.Drawing.Point(15, 76);
            this.button_EngageFB.Name = "button_EngageFB";
            this.button_EngageFB.Size = new System.Drawing.Size(141, 35);
            this.button_EngageFB.TabIndex = 83;
            this.button_EngageFB.Text = "Engage Feedback";
            this.button_EngageFB.UseVisualStyleBackColor = true;
            this.button_EngageFB.Click += new System.EventHandler(this.button_EngageFB_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.numericUpDown_StimAmpVolts);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.numericUpDown_StimTimeSec);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.numericUpDown_StimFreqHz);
            this.groupBox1.Location = new System.Drawing.Point(378, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(600, 132);
            this.groupBox1.TabIndex = 89;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Stimulation Parameters";
            // 
            // numericUpDown_StimAmpVolts
            // 
            this.numericUpDown_StimAmpVolts.DecimalPlaces = 2;
            this.numericUpDown_StimAmpVolts.Increment = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this.numericUpDown_StimAmpVolts.Location = new System.Drawing.Point(137, 105);
            this.numericUpDown_StimAmpVolts.Maximum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.numericUpDown_StimAmpVolts.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDown_StimAmpVolts.Name = "numericUpDown_StimAmpVolts";
            this.numericUpDown_StimAmpVolts.Size = new System.Drawing.Size(70, 20);
            this.numericUpDown_StimAmpVolts.TabIndex = 88;
            this.numericUpDown_StimAmpVolts.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_StimAmpVolts.ValueChanged += new System.EventHandler(this.numericUpDown_StimAmpVolts_ValueChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 107);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(111, 13);
            this.label5.TabIndex = 87;
            this.label5.Text = "Stim. Amplitude (Volts)";
            // 
            // numericUpDown_StimTimeSec
            // 
            this.numericUpDown_StimTimeSec.DecimalPlaces = 1;
            this.numericUpDown_StimTimeSec.Location = new System.Drawing.Point(137, 68);
            this.numericUpDown_StimTimeSec.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDown_StimTimeSec.Name = "numericUpDown_StimTimeSec";
            this.numericUpDown_StimTimeSec.Size = new System.Drawing.Size(70, 20);
            this.numericUpDown_StimTimeSec.TabIndex = 86;
            this.numericUpDown_StimTimeSec.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDown_StimTimeSec.ValueChanged += new System.EventHandler(this.numericUpDown_StimTimeSec_ValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 70);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(84, 13);
            this.label3.TabIndex = 85;
            this.label3.Text = "Stim. Time (Sec)";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 33);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(79, 13);
            this.label2.TabIndex = 85;
            this.label2.Text = "Stim. Freq. (Hz)";
            // 
            // numericUpDown_StimFreqHz
            // 
            this.numericUpDown_StimFreqHz.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericUpDown_StimFreqHz.Location = new System.Drawing.Point(137, 31);
            this.numericUpDown_StimFreqHz.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.numericUpDown_StimFreqHz.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_StimFreqHz.Name = "numericUpDown_StimFreqHz";
            this.numericUpDown_StimFreqHz.Size = new System.Drawing.Size(70, 20);
            this.numericUpDown_StimFreqHz.TabIndex = 85;
            this.numericUpDown_StimFreqHz.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDown_StimFreqHz.ValueChanged += new System.EventHandler(this.numericUpDown_StimFreqHz_ValueChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label_SelectedStat);
            this.groupBox2.Controls.Add(this.button_EngageFB);
            this.groupBox2.Location = new System.Drawing.Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(176, 132);
            this.groupBox2.TabIndex = 90;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Feedback Control";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 35);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(72, 13);
            this.label4.TabIndex = 84;
            this.label4.Text = "LFP Statsitic: ";
            // 
            // label_SelectedStat
            // 
            this.label_SelectedStat.AutoSize = true;
            this.label_SelectedStat.Location = new System.Drawing.Point(80, 35);
            this.label_SelectedStat.Name = "label_SelectedStat";
            this.label_SelectedStat.Size = new System.Drawing.Size(72, 13);
            this.label_SelectedStat.TabIndex = 0;
            this.label_SelectedStat.Text = "LFP Statsitic: ";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.button_RetrainThresh);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.numericUpDown_TreshK);
            this.groupBox3.Location = new System.Drawing.Point(194, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(176, 132);
            this.groupBox3.TabIndex = 90;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Threshold Parameters";
            // 
            // button_RetrainThresh
            // 
            this.button_RetrainThresh.Location = new System.Drawing.Point(19, 76);
            this.button_RetrainThresh.Name = "button_RetrainThresh";
            this.button_RetrainThresh.Size = new System.Drawing.Size(141, 35);
            this.button_RetrainThresh.TabIndex = 84;
            this.button_RetrainThresh.Text = "Retrain";
            this.button_RetrainThresh.UseVisualStyleBackColor = true;
            this.button_RetrainThresh.Click += new System.EventHandler(this.button_RetrainThresh_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 26);
            this.label1.TabIndex = 1;
            this.label1.Text = "Threshold \r\nCoefficient";
            // 
            // numericUpDown_TreshK
            // 
            this.numericUpDown_TreshK.DecimalPlaces = 2;
            this.numericUpDown_TreshK.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDown_TreshK.Location = new System.Drawing.Point(79, 31);
            this.numericUpDown_TreshK.Name = "numericUpDown_TreshK";
            this.numericUpDown_TreshK.Size = new System.Drawing.Size(81, 20);
            this.numericUpDown_TreshK.TabIndex = 0;
            this.numericUpDown_TreshK.Value = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numericUpDown_TreshK.ValueChanged += new System.EventHandler(this.numericUpDown_TreshK_ValueChanged);
            // 
            // backgroundWorker_UpdateUI
            // 
            this.backgroundWorker_UpdateUI.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_UpdateUI_DoWork);
            // 
            // SeizureControlPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(990, 471);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.zgc);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SeizureControlPanel";
            this.Text = "Seizure Stopper";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_StimAmpVolts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_StimTimeSec)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_StimFreqHz)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_TreshK)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private ZedGraph.ZedGraphControl zgc;
        private System.Windows.Forms.Button button_EngageFB;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label_SelectedStat;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.ComponentModel.BackgroundWorker backgroundWorker_UpdateUI;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericUpDown_TreshK;
        private System.Windows.Forms.Button button_RetrainThresh;
        private System.Windows.Forms.NumericUpDown numericUpDown_StimTimeSec;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericUpDown_StimFreqHz;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numericUpDown_StimAmpVolts;
        private System.Windows.Forms.Label label5;
    }
}