namespace NeuroRighter.SpikeDetection
{
    partial class SpikeDetSettings
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SpikeDetSettings));
            this.numericUpDown_MinSpikeWidth = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_MaxSpikeWidth = new System.Windows.Forms.NumericUpDown();
            this.label71 = new System.Windows.Forms.Label();
            this.numericUpDown_DeadTime = new System.Windows.Forms.NumericUpDown();
            this.button_ForceDetectTrain = new System.Windows.Forms.Button();
            this.label92 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.comboBox_spikeDetAlg = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.numPostSamples = new System.Windows.Forms.NumericUpDown();
            this.numPreSamples = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.numericUpDown_MaxSpkAmp = new System.Windows.Forms.NumericUpDown();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label_PreSampConv = new System.Windows.Forms.Label();
            this.label_PostSampConv = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.thresholdMultiplier = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.label63 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpDown_MinSpikeSlope = new System.Windows.Forms.NumericUpDown();
            this.label_MinWidthSamp = new System.Windows.Forms.Label();
            this.label_MaxWidthSamp = new System.Windows.Forms.Label();
            this.label_deadTimeSamp = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.button_SaveAndClose = new System.Windows.Forms.Button();
            this.persistWindowComponent_ForSpkDet = new Mowog.PersistWindowComponent(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MinSpikeWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MaxSpikeWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_DeadTime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPostSamples)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPreSamples)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MaxSpkAmp)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.thresholdMultiplier)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MinSpikeSlope)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // numericUpDown_MinSpikeWidth
            // 
            this.numericUpDown_MinSpikeWidth.BackColor = System.Drawing.Color.Yellow;
            this.numericUpDown_MinSpikeWidth.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDown_MinSpikeWidth.Location = new System.Drawing.Point(151, 93);
            this.numericUpDown_MinSpikeWidth.Maximum = new decimal(new int[] {
            250,
            0,
            0,
            0});
            this.numericUpDown_MinSpikeWidth.Name = "numericUpDown_MinSpikeWidth";
            this.numericUpDown_MinSpikeWidth.Size = new System.Drawing.Size(65, 20);
            this.numericUpDown_MinSpikeWidth.TabIndex = 54;
            this.numericUpDown_MinSpikeWidth.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDown_MinSpikeWidth.Value = new decimal(new int[] {
            80,
            0,
            0,
            0});
            this.numericUpDown_MinSpikeWidth.ValueChanged += new System.EventHandler(this.numericUpDown_MinSpikeWidth_ValueChanged);
            // 
            // numericUpDown_MaxSpikeWidth
            // 
            this.numericUpDown_MaxSpikeWidth.BackColor = System.Drawing.Color.Lime;
            this.numericUpDown_MaxSpikeWidth.ForeColor = System.Drawing.SystemColors.ControlText;
            this.numericUpDown_MaxSpikeWidth.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numericUpDown_MaxSpikeWidth.Location = new System.Drawing.Point(162, 336);
            this.numericUpDown_MaxSpikeWidth.Maximum = new decimal(new int[] {
            1500,
            0,
            0,
            0});
            this.numericUpDown_MaxSpikeWidth.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDown_MaxSpikeWidth.Name = "numericUpDown_MaxSpikeWidth";
            this.numericUpDown_MaxSpikeWidth.Size = new System.Drawing.Size(65, 20);
            this.numericUpDown_MaxSpikeWidth.TabIndex = 53;
            this.numericUpDown_MaxSpikeWidth.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDown_MaxSpikeWidth.Value = new decimal(new int[] {
            750,
            0,
            0,
            0});
            this.numericUpDown_MaxSpikeWidth.ValueChanged += new System.EventHandler(this.numericUpDown_MaxSpikeWidth_ValueChanged);
            // 
            // label71
            // 
            this.label71.AutoSize = true;
            this.label71.Location = new System.Drawing.Point(18, 306);
            this.label71.Name = "label71";
            this.label71.Size = new System.Drawing.Size(114, 13);
            this.label71.TabIndex = 52;
            this.label71.Text = "Min spike width (uSec)";
            // 
            // numericUpDown_DeadTime
            // 
            this.numericUpDown_DeadTime.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.numericUpDown_DeadTime.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numericUpDown_DeadTime.Location = new System.Drawing.Point(162, 268);
            this.numericUpDown_DeadTime.Maximum = new decimal(new int[] {
            3000,
            0,
            0,
            0});
            this.numericUpDown_DeadTime.Name = "numericUpDown_DeadTime";
            this.numericUpDown_DeadTime.Size = new System.Drawing.Size(65, 20);
            this.numericUpDown_DeadTime.TabIndex = 51;
            this.numericUpDown_DeadTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDown_DeadTime.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDown_DeadTime.ValueChanged += new System.EventHandler(this.numericUpDown_DeadTime_ValueChanged);
            // 
            // button_ForceDetectTrain
            // 
            this.button_ForceDetectTrain.Location = new System.Drawing.Point(270, 17);
            this.button_ForceDetectTrain.Name = "button_ForceDetectTrain";
            this.button_ForceDetectTrain.Size = new System.Drawing.Size(57, 23);
            this.button_ForceDetectTrain.TabIndex = 49;
            this.button_ForceDetectTrain.Text = "Train";
            this.button_ForceDetectTrain.UseVisualStyleBackColor = true;
            this.button_ForceDetectTrain.Click += new System.EventHandler(this.button_ForceDetectTrain_Click);
            // 
            // label92
            // 
            this.label92.AutoSize = true;
            this.label92.Location = new System.Drawing.Point(18, 374);
            this.label92.Name = "label92";
            this.label92.Size = new System.Drawing.Size(125, 13);
            this.label92.TabIndex = 48;
            this.label92.Text = "Max spike amplitude (uV)";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(6, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(98, 13);
            this.label12.TabIndex = 41;
            this.label12.Text = "Detection algorithm";
            // 
            // comboBox_spikeDetAlg
            // 
            this.comboBox_spikeDetAlg.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_spikeDetAlg.FormattingEnabled = true;
            this.comboBox_spikeDetAlg.Items.AddRange(new object[] {
            "Fixed RMS",
            "Adaptive RMS",
            "LimAda (Wagenaar)"});
            this.comboBox_spikeDetAlg.Location = new System.Drawing.Point(6, 19);
            this.comboBox_spikeDetAlg.Name = "comboBox_spikeDetAlg";
            this.comboBox_spikeDetAlg.Size = new System.Drawing.Size(258, 21);
            this.comboBox_spikeDetAlg.TabIndex = 45;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 56);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(100, 13);
            this.label11.TabIndex = 40;
            this.label11.Text = "Post-spike samples:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 25);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(95, 13);
            this.label10.TabIndex = 39;
            this.label10.Text = "Pre-spike samples:";
            // 
            // numPostSamples
            // 
            this.numPostSamples.BackColor = System.Drawing.SystemColors.Control;
            this.numPostSamples.Location = new System.Drawing.Point(150, 54);
            this.numPostSamples.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numPostSamples.Name = "numPostSamples";
            this.numPostSamples.Size = new System.Drawing.Size(65, 20);
            this.numPostSamples.TabIndex = 44;
            this.numPostSamples.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numPostSamples.Value = new decimal(new int[] {
            35,
            0,
            0,
            0});
            this.numPostSamples.ValueChanged += new System.EventHandler(this.numPostSamples_ValueChanged);
            // 
            // numPreSamples
            // 
            this.numPreSamples.BackColor = System.Drawing.SystemColors.Control;
            this.numPreSamples.Location = new System.Drawing.Point(150, 23);
            this.numPreSamples.Maximum = new decimal(new int[] {
            49,
            0,
            0,
            0});
            this.numPreSamples.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numPreSamples.Name = "numPreSamples";
            this.numPreSamples.Size = new System.Drawing.Size(65, 20);
            this.numPreSamples.TabIndex = 43;
            this.numPreSamples.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numPreSamples.Value = new decimal(new int[] {
            14,
            0,
            0,
            0});
            this.numPreSamples.ValueChanged += new System.EventHandler(this.numPreSamples_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 340);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(117, 13);
            this.label1.TabIndex = 55;
            this.label1.Text = "Max spike width (uSec)";
            // 
            // numericUpDown_MaxSpkAmp
            // 
            this.numericUpDown_MaxSpkAmp.BackColor = System.Drawing.Color.Cyan;
            this.numericUpDown_MaxSpkAmp.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDown_MaxSpkAmp.Location = new System.Drawing.Point(162, 370);
            this.numericUpDown_MaxSpkAmp.Maximum = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            this.numericUpDown_MaxSpkAmp.Name = "numericUpDown_MaxSpkAmp";
            this.numericUpDown_MaxSpkAmp.Size = new System.Drawing.Size(65, 20);
            this.numericUpDown_MaxSpkAmp.TabIndex = 56;
            this.numericUpDown_MaxSpkAmp.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDown_MaxSpkAmp.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDown_MaxSpkAmp.ValueChanged += new System.EventHandler(this.numericUpDown_MaxSpkAmp_ValueChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.comboBox_spikeDetAlg);
            this.groupBox1.Controls.Add(this.button_ForceDetectTrain);
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Location = new System.Drawing.Point(11, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(342, 53);
            this.groupBox1.TabIndex = 57;
            this.groupBox1.TabStop = false;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label_PreSampConv);
            this.groupBox3.Controls.Add(this.label_PostSampConv);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.label11);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Controls.Add(this.numPreSamples);
            this.groupBox3.Controls.Add(this.numPostSamples);
            this.groupBox3.Location = new System.Drawing.Point(12, 109);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(341, 85);
            this.groupBox3.TabIndex = 58;
            this.groupBox3.TabStop = false;
            // 
            // label_PreSampConv
            // 
            this.label_PreSampConv.AutoSize = true;
            this.label_PreSampConv.Location = new System.Drawing.Point(255, 27);
            this.label_PreSampConv.Name = "label_PreSampConv";
            this.label_PreSampConv.Size = new System.Drawing.Size(17, 13);
            this.label_PreSampConv.TabIndex = 66;
            this.label_PreSampConv.Text = "xx";
            // 
            // label_PostSampConv
            // 
            this.label_PostSampConv.AutoSize = true;
            this.label_PostSampConv.Location = new System.Drawing.Point(255, 58);
            this.label_PostSampConv.Name = "label_PostSampConv";
            this.label_PostSampConv.Size = new System.Drawing.Size(17, 13);
            this.label_PostSampConv.TabIndex = 65;
            this.label_PostSampConv.Text = "xx";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(126, 13);
            this.label3.TabIndex = 41;
            this.label3.Text = "Spike-snippet parameters";
            // 
            // thresholdMultiplier
            // 
            this.thresholdMultiplier.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.thresholdMultiplier.DecimalPlaces = 1;
            this.thresholdMultiplier.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.thresholdMultiplier.Location = new System.Drawing.Point(151, 23);
            this.thresholdMultiplier.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.thresholdMultiplier.Name = "thresholdMultiplier";
            this.thresholdMultiplier.Size = new System.Drawing.Size(65, 20);
            this.thresholdMultiplier.TabIndex = 42;
            this.thresholdMultiplier.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.thresholdMultiplier.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.thresholdMultiplier.ValueChanged += new System.EventHandler(this.thresholdMultiplier_ValueChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(7, 25);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(57, 13);
            this.label9.TabIndex = 38;
            this.label9.Text = "Threshold:";
            // 
            // label63
            // 
            this.label63.AutoSize = true;
            this.label63.Location = new System.Drawing.Point(7, 61);
            this.label63.Name = "label63";
            this.label63.Size = new System.Drawing.Size(136, 13);
            this.label63.TabIndex = 50;
            this.label63.Text = "Detection dead-time (uSec)";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(7, 199);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(120, 13);
            this.label4.TabIndex = 60;
            this.label4.Text = "Min spike slope (uV/ms)";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 13);
            this.label2.TabIndex = 41;
            this.label2.Text = "Detection parameters";
            // 
            // numericUpDown_MinSpikeSlope
            // 
            this.numericUpDown_MinSpikeSlope.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.numericUpDown_MinSpikeSlope.Location = new System.Drawing.Point(151, 195);
            this.numericUpDown_MinSpikeSlope.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numericUpDown_MinSpikeSlope.Name = "numericUpDown_MinSpikeSlope";
            this.numericUpDown_MinSpikeSlope.Size = new System.Drawing.Size(65, 20);
            this.numericUpDown_MinSpikeSlope.TabIndex = 61;
            this.numericUpDown_MinSpikeSlope.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDown_MinSpikeSlope.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericUpDown_MinSpikeSlope.ValueChanged += new System.EventHandler(this.numericUpDown_MinSpikeSlope_ValueChanged);
            // 
            // label_MinWidthSamp
            // 
            this.label_MinWidthSamp.AutoSize = true;
            this.label_MinWidthSamp.Location = new System.Drawing.Point(256, 97);
            this.label_MinWidthSamp.Name = "label_MinWidthSamp";
            this.label_MinWidthSamp.Size = new System.Drawing.Size(17, 13);
            this.label_MinWidthSamp.TabIndex = 62;
            this.label_MinWidthSamp.Text = "xx";
            // 
            // label_MaxWidthSamp
            // 
            this.label_MaxWidthSamp.AutoSize = true;
            this.label_MaxWidthSamp.Location = new System.Drawing.Point(256, 129);
            this.label_MaxWidthSamp.Name = "label_MaxWidthSamp";
            this.label_MaxWidthSamp.Size = new System.Drawing.Size(17, 13);
            this.label_MaxWidthSamp.TabIndex = 63;
            this.label_MaxWidthSamp.Text = "xx";
            // 
            // label_deadTimeSamp
            // 
            this.label_deadTimeSamp.AutoSize = true;
            this.label_deadTimeSamp.Location = new System.Drawing.Point(256, 66);
            this.label_deadTimeSamp.Name = "label_deadTimeSamp";
            this.label_deadTimeSamp.Size = new System.Drawing.Size(17, 13);
            this.label_deadTimeSamp.TabIndex = 64;
            this.label_deadTimeSamp.Text = "xx";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label_deadTimeSamp);
            this.groupBox2.Controls.Add(this.numericUpDown_MinSpikeWidth);
            this.groupBox2.Controls.Add(this.label_MaxWidthSamp);
            this.groupBox2.Controls.Add(this.label_MinWidthSamp);
            this.groupBox2.Controls.Add(this.numericUpDown_MinSpikeSlope);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label63);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.thresholdMultiplier);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Location = new System.Drawing.Point(11, 209);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(342, 226);
            this.groupBox2.TabIndex = 58;
            this.groupBox2.TabStop = false;
            // 
            // label5
            // 
            this.label5.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label5.Location = new System.Drawing.Point(11, 93);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(342, 2);
            this.label5.TabIndex = 71;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.Location = new System.Drawing.Point(267, 72);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(76, 16);
            this.label16.TabIndex = 70;
            this.label16.Text = "Conversion";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.Location = new System.Drawing.Point(159, 72);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(43, 16);
            this.label15.TabIndex = 69;
            this.label15.Text = "Value";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(18, 72);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(71, 16);
            this.label8.TabIndex = 68;
            this.label8.Text = "Parameter";
            // 
            // button_SaveAndClose
            // 
            this.button_SaveAndClose.Location = new System.Drawing.Point(249, 441);
            this.button_SaveAndClose.Name = "button_SaveAndClose";
            this.button_SaveAndClose.Size = new System.Drawing.Size(104, 23);
            this.button_SaveAndClose.TabIndex = 59;
            this.button_SaveAndClose.Text = "Close and save";
            this.button_SaveAndClose.UseVisualStyleBackColor = true;
            this.button_SaveAndClose.Click += new System.EventHandler(this.button_SaveAndClose_Click);
            // 
            // persistWindowComponent_ForSpkDet
            // 
            this.persistWindowComponent_ForSpkDet.Form = this;
            this.persistWindowComponent_ForSpkDet.XMLFilePath = "WindowState.xml";
            // 
            // SpikeDetSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(365, 479);
            this.ControlBox = false;
            this.Controls.Add(this.label5);
            this.Controls.Add(this.numericUpDown_MaxSpkAmp);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.numericUpDown_MaxSpikeWidth);
            this.Controls.Add(this.label71);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.numericUpDown_DeadTime);
            this.Controls.Add(this.label92);
            this.Controls.Add(this.button_SaveAndClose);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox3);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.Name = "SpikeDetSettings";
            this.Text = "Spike Detection Settings";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MinSpikeWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MaxSpikeWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_DeadTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPostSamples)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPreSamples)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MaxSpkAmp)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.thresholdMultiplier)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MinSpikeSlope)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown numericUpDown_MinSpikeWidth;
        private System.Windows.Forms.NumericUpDown numericUpDown_MaxSpikeWidth;
        private System.Windows.Forms.Label label71;
        private System.Windows.Forms.NumericUpDown numericUpDown_DeadTime;
        private System.Windows.Forms.Button button_ForceDetectTrain;
        private System.Windows.Forms.Label label92;
        private System.Windows.Forms.Label label12;
        protected System.Windows.Forms.ComboBox comboBox_spikeDetAlg;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericUpDown_MaxSpkAmp;
        private System.Windows.Forms.GroupBox groupBox1;
        internal System.Windows.Forms.NumericUpDown numPostSamples;
        internal System.Windows.Forms.NumericUpDown numPreSamples;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown thresholdMultiplier;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label63;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericUpDown_MinSpikeSlope;
        private System.Windows.Forms.Label label_MinWidthSamp;
        private System.Windows.Forms.Label label_MaxWidthSamp;
        private System.Windows.Forms.Label label_deadTimeSamp;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button button_SaveAndClose;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label_PreSampConv;
        private System.Windows.Forms.Label label_PostSampConv;
        private Mowog.PersistWindowComponent persistWindowComponent_ForSpkDet;

    }
}