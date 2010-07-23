namespace NeuroRighter
{
    partial class HardwareSettings
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
            this.button_accept = new System.Windows.Forms.Button();
            this.button_cancel = new System.Windows.Forms.Button();
            this.tabPage_misc = new System.Windows.Forms.TabPage();
            this.groupBox12 = new System.Windows.Forms.GroupBox();
            this.checkBox_useChannelPlayback = new System.Windows.Forms.CheckBox();
            this.label11 = new System.Windows.Forms.Label();
            this.comboBox_singleChannelPlaybackDevice = new System.Windows.Forms.ComboBox();
            this.groupBox11 = new System.Windows.Forms.GroupBox();
            this.label9 = new System.Windows.Forms.Label();
            this.comboBox_impedanceDevice = new System.Windows.Forms.ComboBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.checkBox_useProgRef = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.comboBox_progRefSerialPort = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.checkBox_useCineplex = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBox_cineplexDevice = new System.Windows.Forms.ComboBox();
            this.tabPage_stim = new System.Windows.Forms.TabPage();
            this.groupBox13 = new System.Windows.Forms.GroupBox();
            this.label12 = new System.Windows.Forms.Label();
            this.comboBox_IVControlDevice = new System.Windows.Forms.ComboBox();
            this.groupBox10 = new System.Windows.Forms.GroupBox();
            this.comboBox_stimInfoDevice = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.checkBox_recordStimulationInfo = new System.Windows.Forms.CheckBox();
            this.groupBox9 = new System.Windows.Forms.GroupBox();
            this.radioButton_32bit = new System.Windows.Forms.RadioButton();
            this.radioButton_8bit = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioButton_16Mux = new System.Windows.Forms.RadioButton();
            this.radioButton_8Mux = new System.Windows.Forms.RadioButton();
            this.checkBox_useStimulator = new System.Windows.Forms.CheckBox();
            this.comboBox_stimulatorDevice = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tabPage_input = new System.Windows.Forms.TabPage();
            this.groupBox15 = new System.Windows.Forms.GroupBox();
            this.textBox_PreAmpGain = new System.Windows.Forms.TextBox();
            this.groupBox16 = new System.Windows.Forms.GroupBox();
            this.label14 = new System.Windows.Forms.Label();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.checkBox_useSecondBoard = new System.Windows.Forms.CheckBox();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.checkBox_sepLFPBoard2 = new System.Windows.Forms.CheckBox();
            this.comboBox_LFPDevice2 = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.comboBox_analogInputDevice2 = new System.Windows.Forms.ComboBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.checkBox_useEEG = new System.Windows.Forms.CheckBox();
            this.comboBox_EEG = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox14 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.checkBox_sepLFPBoard1 = new System.Windows.Forms.CheckBox();
            this.comboBox_LFPDevice1 = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_analogInputDevice1 = new System.Windows.Forms.ComboBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage_misc.SuspendLayout();
            this.groupBox12.SuspendLayout();
            this.groupBox11.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabPage_stim.SuspendLayout();
            this.groupBox13.SuspendLayout();
            this.groupBox10.SuspendLayout();
            this.groupBox9.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage_input.SuspendLayout();
            this.groupBox15.SuspendLayout();
            this.groupBox7.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // button_accept
            // 
            this.button_accept.Location = new System.Drawing.Point(164, 463);
            this.button_accept.Name = "button_accept";
            this.button_accept.Size = new System.Drawing.Size(75, 23);
            this.button_accept.TabIndex = 2;
            this.button_accept.Text = "Accept";
            this.button_accept.UseVisualStyleBackColor = true;
            this.button_accept.Click += new System.EventHandler(this.button_accept_Click);
            // 
            // button_cancel
            // 
            this.button_cancel.Location = new System.Drawing.Point(245, 463);
            this.button_cancel.Name = "button_cancel";
            this.button_cancel.Size = new System.Drawing.Size(75, 23);
            this.button_cancel.TabIndex = 10;
            this.button_cancel.Text = "Cancel";
            this.button_cancel.UseVisualStyleBackColor = true;
            this.button_cancel.Click += new System.EventHandler(this.button_cancel_Click);
            // 
            // tabPage_misc
            // 
            this.tabPage_misc.Controls.Add(this.groupBox12);
            this.tabPage_misc.Controls.Add(this.groupBox11);
            this.tabPage_misc.Controls.Add(this.groupBox5);
            this.tabPage_misc.Controls.Add(this.groupBox2);
            this.tabPage_misc.Location = new System.Drawing.Point(4, 22);
            this.tabPage_misc.Name = "tabPage_misc";
            this.tabPage_misc.Size = new System.Drawing.Size(300, 417);
            this.tabPage_misc.TabIndex = 2;
            this.tabPage_misc.Text = "Miscellaneous";
            this.tabPage_misc.UseVisualStyleBackColor = true;
            // 
            // groupBox12
            // 
            this.groupBox12.Controls.Add(this.checkBox_useChannelPlayback);
            this.groupBox12.Controls.Add(this.label11);
            this.groupBox12.Controls.Add(this.comboBox_singleChannelPlaybackDevice);
            this.groupBox12.Location = new System.Drawing.Point(3, 218);
            this.groupBox12.Name = "groupBox12";
            this.groupBox12.Size = new System.Drawing.Size(286, 69);
            this.groupBox12.TabIndex = 17;
            this.groupBox12.TabStop = false;
            this.groupBox12.Text = "Single Channel Playback";
            // 
            // checkBox_useChannelPlayback
            // 
            this.checkBox_useChannelPlayback.AutoSize = true;
            this.checkBox_useChannelPlayback.Location = new System.Drawing.Point(6, 19);
            this.checkBox_useChannelPlayback.Name = "checkBox_useChannelPlayback";
            this.checkBox_useChannelPlayback.Size = new System.Drawing.Size(217, 17);
            this.checkBox_useChannelPlayback.TabIndex = 9;
            this.checkBox_useChannelPlayback.Text = "Enable Single Channel Playback/Output";
            this.checkBox_useChannelPlayback.UseVisualStyleBackColor = true;
            this.checkBox_useChannelPlayback.CheckedChanged += new System.EventHandler(this.checkBox_useChannelPlayback_CheckedChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 41);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(159, 13);
            this.label11.TabIndex = 8;
            this.label11.Text = "NI-DAQ Device for BNC Output:";
            // 
            // comboBox_singleChannelPlaybackDevice
            // 
            this.comboBox_singleChannelPlaybackDevice.FormattingEnabled = true;
            this.comboBox_singleChannelPlaybackDevice.Location = new System.Drawing.Point(174, 38);
            this.comboBox_singleChannelPlaybackDevice.Name = "comboBox_singleChannelPlaybackDevice";
            this.comboBox_singleChannelPlaybackDevice.Size = new System.Drawing.Size(97, 21);
            this.comboBox_singleChannelPlaybackDevice.TabIndex = 7;
            this.comboBox_singleChannelPlaybackDevice.Text = "Dev1";
            // 
            // groupBox11
            // 
            this.groupBox11.Controls.Add(this.label9);
            this.groupBox11.Controls.Add(this.comboBox_impedanceDevice);
            this.groupBox11.Location = new System.Drawing.Point(3, 143);
            this.groupBox11.Name = "groupBox11";
            this.groupBox11.Size = new System.Drawing.Size(286, 69);
            this.groupBox11.TabIndex = 16;
            this.groupBox11.TabStop = false;
            this.groupBox11.Text = "Impedance Measurements";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 22);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(227, 13);
            this.label9.TabIndex = 8;
            this.label9.Text = "NI-DAQ Device for Impedance Measurements:";
            // 
            // comboBox_impedanceDevice
            // 
            this.comboBox_impedanceDevice.FormattingEnabled = true;
            this.comboBox_impedanceDevice.Location = new System.Drawing.Point(174, 38);
            this.comboBox_impedanceDevice.Name = "comboBox_impedanceDevice";
            this.comboBox_impedanceDevice.Size = new System.Drawing.Size(97, 21);
            this.comboBox_impedanceDevice.TabIndex = 7;
            this.comboBox_impedanceDevice.Text = "Dev1";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.checkBox_useProgRef);
            this.groupBox5.Controls.Add(this.label5);
            this.groupBox5.Controls.Add(this.comboBox_progRefSerialPort);
            this.groupBox5.Location = new System.Drawing.Point(3, 73);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(286, 64);
            this.groupBox5.TabIndex = 15;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Plexon referencing";
            // 
            // checkBox_useProgRef
            // 
            this.checkBox_useProgRef.AutoSize = true;
            this.checkBox_useProgRef.Location = new System.Drawing.Point(6, 19);
            this.checkBox_useProgRef.Name = "checkBox_useProgRef";
            this.checkBox_useProgRef.Size = new System.Drawing.Size(225, 17);
            this.checkBox_useProgRef.TabIndex = 7;
            this.checkBox_useProgRef.Text = "Enable Plexon Programmable Referencing";
            this.checkBox_useProgRef.UseVisualStyleBackColor = true;
            this.checkBox_useProgRef.CheckedChanged += new System.EventHandler(this.checkBox_useProgRef_CheckedChanged_1);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 42);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(134, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Serial Port for Referencing:";
            // 
            // comboBox_progRefSerialPort
            // 
            this.comboBox_progRefSerialPort.FormattingEnabled = true;
            this.comboBox_progRefSerialPort.Location = new System.Drawing.Point(174, 39);
            this.comboBox_progRefSerialPort.Name = "comboBox_progRefSerialPort";
            this.comboBox_progRefSerialPort.Size = new System.Drawing.Size(97, 21);
            this.comboBox_progRefSerialPort.TabIndex = 8;
            this.comboBox_progRefSerialPort.Text = "Dev1";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.checkBox_useCineplex);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.comboBox_cineplexDevice);
            this.groupBox2.Location = new System.Drawing.Point(3, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(286, 64);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Video";
            // 
            // checkBox_useCineplex
            // 
            this.checkBox_useCineplex.AutoSize = true;
            this.checkBox_useCineplex.Location = new System.Drawing.Point(6, 19);
            this.checkBox_useCineplex.Name = "checkBox_useCineplex";
            this.checkBox_useCineplex.Size = new System.Drawing.Size(170, 17);
            this.checkBox_useCineplex.TabIndex = 0;
            this.checkBox_useCineplex.Text = "Use Cineplex (video recording)";
            this.checkBox_useCineplex.UseVisualStyleBackColor = true;
            this.checkBox_useCineplex.CheckedChanged += new System.EventHandler(this.checkBox_useCineplex_CheckedChanged_1);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 40);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(142, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "NI-DAQ Device for Cineplex:";
            // 
            // comboBox_cineplexDevice
            // 
            this.comboBox_cineplexDevice.FormattingEnabled = true;
            this.comboBox_cineplexDevice.Location = new System.Drawing.Point(174, 37);
            this.comboBox_cineplexDevice.Name = "comboBox_cineplexDevice";
            this.comboBox_cineplexDevice.Size = new System.Drawing.Size(97, 21);
            this.comboBox_cineplexDevice.TabIndex = 4;
            this.comboBox_cineplexDevice.Text = "Dev1";
            // 
            // tabPage_stim
            // 
            this.tabPage_stim.Controls.Add(this.groupBox13);
            this.tabPage_stim.Controls.Add(this.groupBox10);
            this.tabPage_stim.Controls.Add(this.groupBox9);
            this.tabPage_stim.Controls.Add(this.groupBox1);
            this.tabPage_stim.Controls.Add(this.checkBox_useStimulator);
            this.tabPage_stim.Controls.Add(this.comboBox_stimulatorDevice);
            this.tabPage_stim.Controls.Add(this.label2);
            this.tabPage_stim.Location = new System.Drawing.Point(4, 22);
            this.tabPage_stim.Name = "tabPage_stim";
            this.tabPage_stim.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_stim.Size = new System.Drawing.Size(300, 417);
            this.tabPage_stim.TabIndex = 1;
            this.tabPage_stim.Text = "Stimulation";
            this.tabPage_stim.UseVisualStyleBackColor = true;
            // 
            // groupBox13
            // 
            this.groupBox13.Controls.Add(this.label12);
            this.groupBox13.Controls.Add(this.comboBox_IVControlDevice);
            this.groupBox13.Location = new System.Drawing.Point(6, 192);
            this.groupBox13.Name = "groupBox13";
            this.groupBox13.Size = new System.Drawing.Size(286, 55);
            this.groupBox13.TabIndex = 18;
            this.groupBox13.TabStop = false;
            this.groupBox13.Text = "Stimulator I/V Control";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(6, 20);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(153, 13);
            this.label12.TabIndex = 8;
            this.label12.Text = "NI-DAQ Device for I/V Control:";
            // 
            // comboBox_IVControlDevice
            // 
            this.comboBox_IVControlDevice.FormattingEnabled = true;
            this.comboBox_IVControlDevice.Location = new System.Drawing.Point(174, 17);
            this.comboBox_IVControlDevice.Name = "comboBox_IVControlDevice";
            this.comboBox_IVControlDevice.Size = new System.Drawing.Size(97, 21);
            this.comboBox_IVControlDevice.TabIndex = 7;
            this.comboBox_IVControlDevice.Text = "Dev1";
            // 
            // groupBox10
            // 
            this.groupBox10.Controls.Add(this.comboBox_stimInfoDevice);
            this.groupBox10.Controls.Add(this.label10);
            this.groupBox10.Controls.Add(this.checkBox_recordStimulationInfo);
            this.groupBox10.Location = new System.Drawing.Point(6, 110);
            this.groupBox10.Name = "groupBox10";
            this.groupBox10.Size = new System.Drawing.Size(287, 76);
            this.groupBox10.TabIndex = 14;
            this.groupBox10.TabStop = false;
            this.groupBox10.Text = "Stimulation Timing";
            // 
            // comboBox_stimInfoDevice
            // 
            this.comboBox_stimInfoDevice.FormattingEnabled = true;
            this.comboBox_stimInfoDevice.Location = new System.Drawing.Point(96, 42);
            this.comboBox_stimInfoDevice.Name = "comboBox_stimInfoDevice";
            this.comboBox_stimInfoDevice.Size = new System.Drawing.Size(91, 21);
            this.comboBox_stimInfoDevice.TabIndex = 18;
            this.comboBox_stimInfoDevice.Text = "Dev1";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 45);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(84, 13);
            this.label10.TabIndex = 19;
            this.label10.Text = "NI-DAQ Device:";
            // 
            // checkBox_recordStimulationInfo
            // 
            this.checkBox_recordStimulationInfo.AutoSize = true;
            this.checkBox_recordStimulationInfo.Location = new System.Drawing.Point(6, 19);
            this.checkBox_recordStimulationInfo.Name = "checkBox_recordStimulationInfo";
            this.checkBox_recordStimulationInfo.Size = new System.Drawing.Size(167, 17);
            this.checkBox_recordStimulationInfo.TabIndex = 15;
            this.checkBox_recordStimulationInfo.Text = "Record stimulation information";
            this.checkBox_recordStimulationInfo.UseVisualStyleBackColor = true;
            // 
            // groupBox9
            // 
            this.groupBox9.Controls.Add(this.radioButton_32bit);
            this.groupBox9.Controls.Add(this.radioButton_8bit);
            this.groupBox9.Location = new System.Drawing.Point(182, 52);
            this.groupBox9.Name = "groupBox9";
            this.groupBox9.Size = new System.Drawing.Size(111, 52);
            this.groupBox9.TabIndex = 13;
            this.groupBox9.TabStop = false;
            this.groupBox9.Text = "Port bandwidth";
            // 
            // radioButton_32bit
            // 
            this.radioButton_32bit.AutoSize = true;
            this.radioButton_32bit.Checked = true;
            this.radioButton_32bit.Location = new System.Drawing.Point(57, 19);
            this.radioButton_32bit.Name = "radioButton_32bit";
            this.radioButton_32bit.Size = new System.Drawing.Size(51, 17);
            this.radioButton_32bit.TabIndex = 12;
            this.radioButton_32bit.TabStop = true;
            this.radioButton_32bit.Text = "32 bit";
            this.radioButton_32bit.UseVisualStyleBackColor = true;
            this.radioButton_32bit.CheckedChanged += new System.EventHandler(this.radioButton_32bit_CheckedChanged);
            // 
            // radioButton_8bit
            // 
            this.radioButton_8bit.AutoSize = true;
            this.radioButton_8bit.Location = new System.Drawing.Point(6, 19);
            this.radioButton_8bit.Name = "radioButton_8bit";
            this.radioButton_8bit.Size = new System.Drawing.Size(45, 17);
            this.radioButton_8bit.TabIndex = 11;
            this.radioButton_8bit.Text = "8 bit";
            this.radioButton_8bit.UseVisualStyleBackColor = true;
            this.radioButton_8bit.CheckedChanged += new System.EventHandler(this.radioButton_8bit_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioButton_16Mux);
            this.groupBox1.Controls.Add(this.radioButton_8Mux);
            this.groupBox1.Location = new System.Drawing.Point(6, 52);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(170, 52);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Multiplexor Type";
            // 
            // radioButton_16Mux
            // 
            this.radioButton_16Mux.AutoSize = true;
            this.radioButton_16Mux.Checked = true;
            this.radioButton_16Mux.Location = new System.Drawing.Point(85, 19);
            this.radioButton_16Mux.Name = "radioButton_16Mux";
            this.radioButton_16Mux.Size = new System.Drawing.Size(79, 17);
            this.radioButton_16Mux.TabIndex = 12;
            this.radioButton_16Mux.TabStop = true;
            this.radioButton_16Mux.Text = "16 Channel";
            this.radioButton_16Mux.UseVisualStyleBackColor = true;
            this.radioButton_16Mux.CheckedChanged += new System.EventHandler(this.radioButton_16Mux_CheckedChanged);
            // 
            // radioButton_8Mux
            // 
            this.radioButton_8Mux.AutoSize = true;
            this.radioButton_8Mux.Location = new System.Drawing.Point(6, 19);
            this.radioButton_8Mux.Name = "radioButton_8Mux";
            this.radioButton_8Mux.Size = new System.Drawing.Size(73, 17);
            this.radioButton_8Mux.TabIndex = 11;
            this.radioButton_8Mux.Text = "8 Channel";
            this.radioButton_8Mux.UseVisualStyleBackColor = true;
            this.radioButton_8Mux.CheckedChanged += new System.EventHandler(this.radioButton_8Mux_CheckedChanged);
            // 
            // checkBox_useStimulator
            // 
            this.checkBox_useStimulator.AutoSize = true;
            this.checkBox_useStimulator.Location = new System.Drawing.Point(6, 6);
            this.checkBox_useStimulator.Name = "checkBox_useStimulator";
            this.checkBox_useStimulator.Size = new System.Drawing.Size(94, 17);
            this.checkBox_useStimulator.TabIndex = 7;
            this.checkBox_useStimulator.Text = "Use Stimulator";
            this.checkBox_useStimulator.UseVisualStyleBackColor = true;
            this.checkBox_useStimulator.CheckedChanged += new System.EventHandler(this.checkBox_useStimulator_CheckedChanged_1);
            // 
            // comboBox_stimulatorDevice
            // 
            this.comboBox_stimulatorDevice.FormattingEnabled = true;
            this.comboBox_stimulatorDevice.Location = new System.Drawing.Point(160, 25);
            this.comboBox_stimulatorDevice.Name = "comboBox_stimulatorDevice";
            this.comboBox_stimulatorDevice.Size = new System.Drawing.Size(79, 21);
            this.comboBox_stimulatorDevice.TabIndex = 3;
            this.comboBox_stimulatorDevice.Text = "Dev1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(148, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "NI-DAQ Device for Stimulator:";
            // 
            // tabPage_input
            // 
            this.tabPage_input.Controls.Add(this.groupBox15);
            this.tabPage_input.Controls.Add(this.groupBox7);
            this.tabPage_input.Controls.Add(this.groupBox6);
            this.tabPage_input.Controls.Add(this.groupBox3);
            this.tabPage_input.Location = new System.Drawing.Point(4, 22);
            this.tabPage_input.Name = "tabPage_input";
            this.tabPage_input.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_input.Size = new System.Drawing.Size(300, 417);
            this.tabPage_input.TabIndex = 0;
            this.tabPage_input.Text = "Input";
            this.tabPage_input.UseVisualStyleBackColor = true;
            // 
            // groupBox15
            // 
            this.groupBox15.Controls.Add(this.textBox_PreAmpGain);
            this.groupBox15.Controls.Add(this.groupBox16);
            this.groupBox15.Controls.Add(this.label14);
            this.groupBox15.Location = new System.Drawing.Point(7, 9);
            this.groupBox15.Name = "groupBox15";
            this.groupBox15.Size = new System.Drawing.Size(286, 57);
            this.groupBox15.TabIndex = 16;
            this.groupBox15.TabStop = false;
            this.groupBox15.Text = "Preamp/Headstage Gain";
            // 
            // textBox_PreAmpGain
            // 
            this.textBox_PreAmpGain.Location = new System.Drawing.Point(172, 21);
            this.textBox_PreAmpGain.Name = "textBox_PreAmpGain";
            this.textBox_PreAmpGain.Size = new System.Drawing.Size(100, 20);
            this.textBox_PreAmpGain.TabIndex = 16;
            this.textBox_PreAmpGain.Text = "1";
            this.textBox_PreAmpGain.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // groupBox16
            // 
            this.groupBox16.Location = new System.Drawing.Point(2, -63);
            this.groupBox16.Name = "groupBox16";
            this.groupBox16.Size = new System.Drawing.Size(286, 59);
            this.groupBox16.TabIndex = 15;
            this.groupBox16.TabStop = false;
            this.groupBox16.Text = "Analog Input #1";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(6, 24);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(158, 13);
            this.label14.TabIndex = 1;
            this.label14.Text = "Amplifier Gain for Extracell. Rec.";
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.checkBox_useSecondBoard);
            this.groupBox7.Controls.Add(this.groupBox8);
            this.groupBox7.Controls.Add(this.label8);
            this.groupBox7.Controls.Add(this.comboBox_analogInputDevice2);
            this.groupBox7.Location = new System.Drawing.Point(7, 194);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(286, 145);
            this.groupBox7.TabIndex = 15;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "Analog Input #2";
            // 
            // checkBox_useSecondBoard
            // 
            this.checkBox_useSecondBoard.AutoSize = true;
            this.checkBox_useSecondBoard.Location = new System.Drawing.Point(6, 19);
            this.checkBox_useSecondBoard.Name = "checkBox_useSecondBoard";
            this.checkBox_useSecondBoard.Size = new System.Drawing.Size(116, 17);
            this.checkBox_useSecondBoard.TabIndex = 7;
            this.checkBox_useSecondBoard.Text = "Use Second Board";
            this.checkBox_useSecondBoard.UseVisualStyleBackColor = true;
            this.checkBox_useSecondBoard.CheckedChanged += new System.EventHandler(this.checkBox_useSecondBoard_CheckedChanged);
            // 
            // groupBox8
            // 
            this.groupBox8.Controls.Add(this.label7);
            this.groupBox8.Controls.Add(this.checkBox_sepLFPBoard2);
            this.groupBox8.Controls.Add(this.comboBox_LFPDevice2);
            this.groupBox8.Location = new System.Drawing.Point(6, 68);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.Size = new System.Drawing.Size(271, 68);
            this.groupBox8.TabIndex = 14;
            this.groupBox8.TabStop = false;
            this.groupBox8.Text = "LFP Input";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 44);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(148, 13);
            this.label7.TabIndex = 13;
            this.label7.Text = "NI-DAQ Device for LFP Input:";
            // 
            // checkBox_sepLFPBoard2
            // 
            this.checkBox_sepLFPBoard2.AutoSize = true;
            this.checkBox_sepLFPBoard2.Location = new System.Drawing.Point(6, 19);
            this.checkBox_sepLFPBoard2.Name = "checkBox_sepLFPBoard2";
            this.checkBox_sepLFPBoard2.Size = new System.Drawing.Size(179, 17);
            this.checkBox_sepLFPBoard2.TabIndex = 11;
            this.checkBox_sepLFPBoard2.Text = "Use Separate NI-DAQ for LFPs?";
            this.checkBox_sepLFPBoard2.UseVisualStyleBackColor = true;
            // 
            // comboBox_LFPDevice2
            // 
            this.comboBox_LFPDevice2.FormattingEnabled = true;
            this.comboBox_LFPDevice2.Location = new System.Drawing.Point(168, 41);
            this.comboBox_LFPDevice2.Name = "comboBox_LFPDevice2";
            this.comboBox_LFPDevice2.Size = new System.Drawing.Size(97, 21);
            this.comboBox_LFPDevice2.TabIndex = 12;
            this.comboBox_LFPDevice2.Text = "Dev1";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 44);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(162, 13);
            this.label8.TabIndex = 1;
            this.label8.Text = "NI-DAQ Device for Analog Input:";
            // 
            // comboBox_analogInputDevice2
            // 
            this.comboBox_analogInputDevice2.Enabled = false;
            this.comboBox_analogInputDevice2.FormattingEnabled = true;
            this.comboBox_analogInputDevice2.Location = new System.Drawing.Point(174, 41);
            this.comboBox_analogInputDevice2.Name = "comboBox_analogInputDevice2";
            this.comboBox_analogInputDevice2.Size = new System.Drawing.Size(97, 21);
            this.comboBox_analogInputDevice2.TabIndex = 0;
            this.comboBox_analogInputDevice2.Text = "Dev1";
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.checkBox_useEEG);
            this.groupBox6.Controls.Add(this.comboBox_EEG);
            this.groupBox6.Controls.Add(this.label6);
            this.groupBox6.Location = new System.Drawing.Point(7, 344);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(286, 65);
            this.groupBox6.TabIndex = 9;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "EEG";
            // 
            // checkBox_useEEG
            // 
            this.checkBox_useEEG.AutoSize = true;
            this.checkBox_useEEG.Location = new System.Drawing.Point(6, 19);
            this.checkBox_useEEG.Name = "checkBox_useEEG";
            this.checkBox_useEEG.Size = new System.Drawing.Size(279, 17);
            this.checkBox_useEEG.TabIndex = 7;
            this.checkBox_useEEG.Text = "Use EEG Channels (i.e., separate analog in channels)";
            this.checkBox_useEEG.UseVisualStyleBackColor = true;
            this.checkBox_useEEG.CheckedChanged += new System.EventHandler(this.checkBox_useEEG_CheckedChanged);
            // 
            // comboBox_EEG
            // 
            this.comboBox_EEG.Enabled = false;
            this.comboBox_EEG.FormattingEnabled = true;
            this.comboBox_EEG.Location = new System.Drawing.Point(174, 38);
            this.comboBox_EEG.Name = "comboBox_EEG";
            this.comboBox_EEG.Size = new System.Drawing.Size(97, 21);
            this.comboBox_EEG.TabIndex = 3;
            this.comboBox_EEG.Text = "Dev1";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 41);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(124, 13);
            this.label6.TabIndex = 5;
            this.label6.Text = "NI-DAQ Device for EEG:";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.groupBox14);
            this.groupBox3.Controls.Add(this.groupBox4);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.comboBox_analogInputDevice1);
            this.groupBox3.Location = new System.Drawing.Point(7, 73);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(286, 116);
            this.groupBox3.TabIndex = 12;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Analog Input #1";
            // 
            // groupBox14
            // 
            this.groupBox14.Location = new System.Drawing.Point(2, -63);
            this.groupBox14.Name = "groupBox14";
            this.groupBox14.Size = new System.Drawing.Size(286, 59);
            this.groupBox14.TabIndex = 15;
            this.groupBox14.TabStop = false;
            this.groupBox14.Text = "Analog Input #1";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.label4);
            this.groupBox4.Controls.Add(this.checkBox_sepLFPBoard1);
            this.groupBox4.Controls.Add(this.comboBox_LFPDevice1);
            this.groupBox4.Location = new System.Drawing.Point(6, 40);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(271, 68);
            this.groupBox4.TabIndex = 14;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "LFP Input";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 44);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(148, 13);
            this.label4.TabIndex = 13;
            this.label4.Text = "NI-DAQ Device for LFP Input:";
            // 
            // checkBox_sepLFPBoard1
            // 
            this.checkBox_sepLFPBoard1.AutoSize = true;
            this.checkBox_sepLFPBoard1.Location = new System.Drawing.Point(6, 19);
            this.checkBox_sepLFPBoard1.Name = "checkBox_sepLFPBoard1";
            this.checkBox_sepLFPBoard1.Size = new System.Drawing.Size(179, 17);
            this.checkBox_sepLFPBoard1.TabIndex = 11;
            this.checkBox_sepLFPBoard1.Text = "Use Separate NI-DAQ for LFPs?";
            this.checkBox_sepLFPBoard1.UseVisualStyleBackColor = true;
            this.checkBox_sepLFPBoard1.CheckedChanged += new System.EventHandler(this.checkBox_sepLFPBoard_CheckedChanged);
            // 
            // comboBox_LFPDevice1
            // 
            this.comboBox_LFPDevice1.FormattingEnabled = true;
            this.comboBox_LFPDevice1.Location = new System.Drawing.Point(168, 41);
            this.comboBox_LFPDevice1.Name = "comboBox_LFPDevice1";
            this.comboBox_LFPDevice1.Size = new System.Drawing.Size(97, 21);
            this.comboBox_LFPDevice1.TabIndex = 12;
            this.comboBox_LFPDevice1.Text = "Dev1";
            this.comboBox_LFPDevice1.SelectedIndexChanged += new System.EventHandler(this.comboBox_LFPs_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(162, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "NI-DAQ Device for Analog Input:";
            // 
            // comboBox_analogInputDevice1
            // 
            this.comboBox_analogInputDevice1.FormattingEnabled = true;
            this.comboBox_analogInputDevice1.Location = new System.Drawing.Point(174, 13);
            this.comboBox_analogInputDevice1.Name = "comboBox_analogInputDevice1";
            this.comboBox_analogInputDevice1.Size = new System.Drawing.Size(97, 21);
            this.comboBox_analogInputDevice1.TabIndex = 0;
            this.comboBox_analogInputDevice1.Text = "Dev1";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage_input);
            this.tabControl1.Controls.Add(this.tabPage_stim);
            this.tabControl1.Controls.Add(this.tabPage_misc);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(308, 443);
            this.tabControl1.TabIndex = 16;
            // 
            // HardwareSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(335, 494);
            this.ControlBox = false;
            this.Controls.Add(this.button_cancel);
            this.Controls.Add(this.button_accept);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "HardwareSettings";
            this.Text = "Settings";
            this.TopMost = true;
            this.tabPage_misc.ResumeLayout(false);
            this.groupBox12.ResumeLayout(false);
            this.groupBox12.PerformLayout();
            this.groupBox11.ResumeLayout(false);
            this.groupBox11.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tabPage_stim.ResumeLayout(false);
            this.tabPage_stim.PerformLayout();
            this.groupBox13.ResumeLayout(false);
            this.groupBox13.PerformLayout();
            this.groupBox10.ResumeLayout(false);
            this.groupBox10.PerformLayout();
            this.groupBox9.ResumeLayout(false);
            this.groupBox9.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage_input.ResumeLayout(false);
            this.groupBox15.ResumeLayout(false);
            this.groupBox15.PerformLayout();
            this.groupBox7.ResumeLayout(false);
            this.groupBox7.PerformLayout();
            this.groupBox8.ResumeLayout(false);
            this.groupBox8.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button_accept;
        private System.Windows.Forms.Button button_cancel;
        private System.Windows.Forms.TabPage tabPage_misc;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.CheckBox checkBox_useProgRef;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox comboBox_progRefSerialPort;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox checkBox_useCineplex;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBox_cineplexDevice;
        private System.Windows.Forms.TabPage tabPage_stim;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioButton_16Mux;
        private System.Windows.Forms.RadioButton radioButton_8Mux;
        private System.Windows.Forms.CheckBox checkBox_useStimulator;
        private System.Windows.Forms.ComboBox comboBox_stimulatorDevice;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TabPage tabPage_input;
        private System.Windows.Forms.GroupBox groupBox7;
        private System.Windows.Forms.CheckBox checkBox_useSecondBoard;
        private System.Windows.Forms.GroupBox groupBox8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox checkBox_sepLFPBoard2;
        private System.Windows.Forms.ComboBox comboBox_LFPDevice2;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox comboBox_analogInputDevice2;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.CheckBox checkBox_useEEG;
        private System.Windows.Forms.ComboBox comboBox_EEG;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox checkBox_sepLFPBoard1;
        private System.Windows.Forms.ComboBox comboBox_LFPDevice1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_analogInputDevice1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.GroupBox groupBox9;
        private System.Windows.Forms.RadioButton radioButton_32bit;
        private System.Windows.Forms.RadioButton radioButton_8bit;
        private System.Windows.Forms.GroupBox groupBox10;
        private System.Windows.Forms.CheckBox checkBox_recordStimulationInfo;
        private System.Windows.Forms.ComboBox comboBox_stimInfoDevice;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.GroupBox groupBox11;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox comboBox_impedanceDevice;
        private System.Windows.Forms.GroupBox groupBox12;
        private System.Windows.Forms.CheckBox checkBox_useChannelPlayback;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox comboBox_singleChannelPlaybackDevice;
        private System.Windows.Forms.GroupBox groupBox13;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ComboBox comboBox_IVControlDevice;
        private System.Windows.Forms.GroupBox groupBox14;
        private System.Windows.Forms.GroupBox groupBox15;
        private System.Windows.Forms.GroupBox groupBox16;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox textBox_PreAmpGain;
    }
}