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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HardwareSettings));
            this.button_accept = new System.Windows.Forms.Button();
            this.button_cancel = new System.Windows.Forms.Button();
            this.tabPage_misc = new System.Windows.Forms.TabPage();
            this.groupBox11 = new System.Windows.Forms.GroupBox();
            this.checkBox_EnableImpedanceMeasurements = new System.Windows.Forms.CheckBox();
            this.label9 = new System.Windows.Forms.Label();
            this.comboBox_impedanceDevice = new System.Windows.Forms.ComboBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.checkBox_UseFloatingRef = new System.Windows.Forms.CheckBox();
            this.checkBox_useProgRef = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.comboBox_progRefSerialPort = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.checkBox_useCineplex = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBox_cineplexDevice = new System.Windows.Forms.ComboBox();
            this.tabPage_stim = new System.Windows.Forms.TabPage();
            this.groupBox26 = new System.Windows.Forms.GroupBox();
            this.label25 = new System.Windows.Forms.Label();
            this.checkBox_useBuffloader = new System.Windows.Forms.CheckBox();
            this.groupBox12 = new System.Windows.Forms.GroupBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.label17 = new System.Windows.Forms.Label();
            this.comboBox3 = new System.Windows.Forms.ComboBox();
            this.groupBox17 = new System.Windows.Forms.GroupBox();
            this.checkBox_UseAODO = new System.Windows.Forms.CheckBox();
            this.label13 = new System.Windows.Forms.Label();
            this.comboBox_SigOutDev = new System.Windows.Forms.ComboBox();
            this.checkBox_useStimulator = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox18 = new System.Windows.Forms.GroupBox();
            this.comboBox_stimulatorDevice = new System.Windows.Forms.ComboBox();
            this.groupBox13 = new System.Windows.Forms.GroupBox();
            this.label12 = new System.Windows.Forms.Label();
            this.comboBox_IVControlDevice = new System.Windows.Forms.ComboBox();
            this.groupBox9 = new System.Windows.Forms.GroupBox();
            this.radioButton_32bit = new System.Windows.Forms.RadioButton();
            this.radioButton_8bit = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioButton_16Mux = new System.Windows.Forms.RadioButton();
            this.radioButton_8Mux = new System.Windows.Forms.RadioButton();
            this.tabPage_input = new System.Windows.Forms.TabPage();
            this.groupBox27 = new System.Windows.Forms.GroupBox();
            this.numericUpDown_MUArate = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_LFPrate = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_samplingRate = new System.Windows.Forms.NumericUpDown();
            this.label73 = new System.Windows.Forms.Label();
            this.comboBox_LFPGain = new System.Windows.Forms.ComboBox();
            this.label38 = new System.Windows.Forms.Label();
            this.comboBox_SpikeGain = new System.Windows.Forms.ComboBox();
            this.label_SpikeGain = new System.Windows.Forms.Label();
            this.label_LFPSamplingRate = new System.Windows.Forms.Label();
            this.label_SpikeSamplingRate = new System.Windows.Forms.Label();
            this.label26 = new System.Windows.Forms.Label();
            this.comboBox_numChannels = new System.Windows.Forms.ComboBox();
            this.groupBox24 = new System.Windows.Forms.GroupBox();
            this.checkBox_processMUA = new System.Windows.Forms.CheckBox();
            this.checkBox_processLFPs = new System.Windows.Forms.CheckBox();
            this.groupBox15 = new System.Windows.Forms.GroupBox();
            this.numericUpDown_PreAmpGain = new System.Windows.Forms.NumericUpDown();
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
            this.tabPag_Gen = new System.Windows.Forms.TabPage();
            this.groupBox25 = new System.Windows.Forms.GroupBox();
            this.checkBox_UseAuxDataBuffer = new System.Windows.Forms.CheckBox();
            this.checkBox_UseDigDataBuffer = new System.Windows.Forms.CheckBox();
            this.checkBox_UseStimDataBuffer = new System.Windows.Forms.CheckBox();
            this.checkBox_UseEEGDataBuffer = new System.Windows.Forms.CheckBox();
            this.checkBox_UseLFPDataBuffer = new System.Windows.Forms.CheckBox();
            this.checkBox_UseSALPADataBuffer = new System.Windows.Forms.CheckBox();
            this.checkBox_UseBPDataBuffer = new System.Windows.Forms.CheckBox();
            this.checkBox_UseSpikeDataBuffer = new System.Windows.Forms.CheckBox();
            this.checkBox_UseRawDataBuffer = new System.Windows.Forms.CheckBox();
            this.groupBox21 = new System.Windows.Forms.GroupBox();
            this.robustStim_checkbox = new System.Windows.Forms.CheckBox();
            this.numericUpDown_datSrvBufferSizeSec = new System.Windows.Forms.NumericUpDown();
            this.label21 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.numericUpDown_DACPollingPeriodSec = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_ADCPollingPeriodSec = new System.Windows.Forms.NumericUpDown();
            this.label22 = new System.Windows.Forms.Label();
            this.groupBox23 = new System.Windows.Forms.GroupBox();
            this.tabPage_AuxInput = new System.Windows.Forms.TabPage();
            this.groupBox20 = new System.Windows.Forms.GroupBox();
            this.comboBox_AuxDigInputPort = new System.Windows.Forms.ComboBox();
            this.label18 = new System.Windows.Forms.Label();
            this.checkBox_UseAuxDigitalInput = new System.Windows.Forms.CheckBox();
            this.groupBox19 = new System.Windows.Forms.GroupBox();
            this.listBox_AuxAnalogInChan = new System.Windows.Forms.ListBox();
            this.label19 = new System.Windows.Forms.Label();
            this.comboBox_AuxAnalogInputDevice = new System.Windows.Forms.ComboBox();
            this.label16 = new System.Windows.Forms.Label();
            this.checkBox_UseAuxAnalogInput = new System.Windows.Forms.CheckBox();
            this.groupBox10 = new System.Windows.Forms.GroupBox();
            this.comboBox_stimInfoDev = new System.Windows.Forms.ComboBox();
            this.label15 = new System.Windows.Forms.Label();
            this.checkBox_RecStimTimes = new System.Windows.Forms.CheckBox();
            this.comboBox_stimInfoDevice = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.checkBox_recordStimulationInfo = new System.Windows.Forms.CheckBox();
            this.checkBox_useChannelPlayback = new System.Windows.Forms.CheckBox();
            this.label11 = new System.Windows.Forms.Label();
            this.comboBox_singleChannelPlaybackDevice = new System.Windows.Forms.ComboBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.groupBox22 = new System.Windows.Forms.GroupBox();
            this.label20 = new System.Windows.Forms.Label();
            this.HWpersistWindowComponent = new Mowog.PersistWindowComponent(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.tabPage_misc.SuspendLayout();
            this.groupBox11.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabPage_stim.SuspendLayout();
            this.groupBox26.SuspendLayout();
            this.groupBox12.SuspendLayout();
            this.groupBox17.SuspendLayout();
            this.groupBox18.SuspendLayout();
            this.groupBox13.SuspendLayout();
            this.groupBox9.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage_input.SuspendLayout();
            this.groupBox27.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MUArate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_LFPrate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_samplingRate)).BeginInit();
            this.groupBox24.SuspendLayout();
            this.groupBox15.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_PreAmpGain)).BeginInit();
            this.groupBox7.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPag_Gen.SuspendLayout();
            this.groupBox25.SuspendLayout();
            this.groupBox21.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_datSrvBufferSizeSec)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_DACPollingPeriodSec)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_ADCPollingPeriodSec)).BeginInit();
            this.tabPage_AuxInput.SuspendLayout();
            this.groupBox20.SuspendLayout();
            this.groupBox19.SuspendLayout();
            this.groupBox10.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // button_accept
            // 
            this.button_accept.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_accept.Location = new System.Drawing.Point(5, 8);
            this.button_accept.Name = "button_accept";
            this.button_accept.Size = new System.Drawing.Size(117, 23);
            this.button_accept.TabIndex = 2;
            this.button_accept.Text = "Accept";
            this.button_accept.UseVisualStyleBackColor = true;
            this.button_accept.Click += new System.EventHandler(this.button_accept_Click);
            
            // 
            // button_cancel
            // 
            this.button_cancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_cancel.Location = new System.Drawing.Point(128, 8);
            this.button_cancel.Name = "button_cancel";
            this.button_cancel.Size = new System.Drawing.Size(117, 23);
            this.button_cancel.TabIndex = 10;
            this.button_cancel.Text = "Cancel";
            this.button_cancel.UseVisualStyleBackColor = true;
            this.button_cancel.Click += new System.EventHandler(this.button_cancel_Click);
            // 
            // tabPage_misc
            // 
            this.tabPage_misc.Controls.Add(this.groupBox11);
            this.tabPage_misc.Controls.Add(this.groupBox5);
            this.tabPage_misc.Controls.Add(this.groupBox2);
            this.tabPage_misc.Location = new System.Drawing.Point(4, 25);
            this.tabPage_misc.Name = "tabPage_misc";
            this.tabPage_misc.Size = new System.Drawing.Size(349, 667);
            this.tabPage_misc.TabIndex = 2;
            this.tabPage_misc.Text = "Misc.";
            this.tabPage_misc.UseVisualStyleBackColor = true;
            // 
            // groupBox11
            // 
            this.groupBox11.Controls.Add(this.checkBox_EnableImpedanceMeasurements);
            this.groupBox11.Controls.Add(this.label9);
            this.groupBox11.Controls.Add(this.comboBox_impedanceDevice);
            this.groupBox11.Location = new System.Drawing.Point(3, 3);
            this.groupBox11.Name = "groupBox11";
            this.groupBox11.Size = new System.Drawing.Size(341, 102);
            this.groupBox11.TabIndex = 16;
            this.groupBox11.TabStop = false;
            this.groupBox11.Text = "Impedance Measurements";
            // 
            // checkBox_EnableImpedanceMeasurements
            // 
            this.checkBox_EnableImpedanceMeasurements.AutoSize = true;
            this.checkBox_EnableImpedanceMeasurements.Location = new System.Drawing.Point(6, 21);
            this.checkBox_EnableImpedanceMeasurements.Name = "checkBox_EnableImpedanceMeasurements";
            this.checkBox_EnableImpedanceMeasurements.Size = new System.Drawing.Size(233, 20);
            this.checkBox_EnableImpedanceMeasurements.TabIndex = 10;
            this.checkBox_EnableImpedanceMeasurements.Text = "Enable Impedance Measurements";
            this.checkBox_EnableImpedanceMeasurements.UseVisualStyleBackColor = true;
            this.checkBox_EnableImpedanceMeasurements.CheckedChanged += new System.EventHandler(this.checkBox_EnableImpedanceMeasurements_CheckedChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(5, 52);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(171, 32);
            this.label9.TabIndex = 8;
            this.label9.Text = "NI-DAQ Device for \r\nImpedance Measurements:";
            // 
            // comboBox_impedanceDevice
            // 
            this.comboBox_impedanceDevice.FormattingEnabled = true;
            this.comboBox_impedanceDevice.Location = new System.Drawing.Point(226, 60);
            this.comboBox_impedanceDevice.Name = "comboBox_impedanceDevice";
            this.comboBox_impedanceDevice.Size = new System.Drawing.Size(97, 24);
            this.comboBox_impedanceDevice.TabIndex = 7;
            this.comboBox_impedanceDevice.Text = "Dev1";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.checkBox_UseFloatingRef);
            this.groupBox5.Controls.Add(this.checkBox_useProgRef);
            this.groupBox5.Controls.Add(this.label5);
            this.groupBox5.Controls.Add(this.comboBox_progRefSerialPort);
            this.groupBox5.Location = new System.Drawing.Point(3, 111);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(341, 119);
            this.groupBox5.TabIndex = 15;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Referencing";
            // 
            // checkBox_UseFloatingRef
            // 
            this.checkBox_UseFloatingRef.AutoSize = true;
            this.checkBox_UseFloatingRef.Location = new System.Drawing.Point(6, 21);
            this.checkBox_UseFloatingRef.Name = "checkBox_UseFloatingRef";
            this.checkBox_UseFloatingRef.Size = new System.Drawing.Size(223, 20);
            this.checkBox_UseFloatingRef.TabIndex = 10;
            this.checkBox_UseFloatingRef.Text = "Use Common Mode Referencing";
            this.checkBox_UseFloatingRef.UseVisualStyleBackColor = true;
            // 
            // checkBox_useProgRef
            // 
            this.checkBox_useProgRef.AutoSize = true;
            this.checkBox_useProgRef.Location = new System.Drawing.Point(6, 51);
            this.checkBox_useProgRef.Name = "checkBox_useProgRef";
            this.checkBox_useProgRef.Size = new System.Drawing.Size(283, 20);
            this.checkBox_useProgRef.TabIndex = 7;
            this.checkBox_useProgRef.Text = "Enable Plexon Programmable Referencing";
            this.checkBox_useProgRef.UseVisualStyleBackColor = true;
            this.checkBox_useProgRef.CheckedChanged += new System.EventHandler(this.checkBox_useProgRef_CheckedChanged_1);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 80);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(167, 16);
            this.label5.TabIndex = 9;
            this.label5.Text = "Serial Port for Referencing:";
            // 
            // comboBox_progRefSerialPort
            // 
            this.comboBox_progRefSerialPort.FormattingEnabled = true;
            this.comboBox_progRefSerialPort.Location = new System.Drawing.Point(226, 77);
            this.comboBox_progRefSerialPort.Name = "comboBox_progRefSerialPort";
            this.comboBox_progRefSerialPort.Size = new System.Drawing.Size(97, 24);
            this.comboBox_progRefSerialPort.TabIndex = 8;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.checkBox_useCineplex);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.comboBox_cineplexDevice);
            this.groupBox2.Location = new System.Drawing.Point(5, 236);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(341, 79);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Video";
            this.groupBox2.Visible = false;
            // 
            // checkBox_useCineplex
            // 
            this.checkBox_useCineplex.AutoSize = true;
            this.checkBox_useCineplex.Location = new System.Drawing.Point(6, 19);
            this.checkBox_useCineplex.Name = "checkBox_useCineplex";
            this.checkBox_useCineplex.Size = new System.Drawing.Size(212, 20);
            this.checkBox_useCineplex.TabIndex = 0;
            this.checkBox_useCineplex.Text = "Use Cineplex (video recording)";
            this.checkBox_useCineplex.UseVisualStyleBackColor = true;
            this.checkBox_useCineplex.CheckedChanged += new System.EventHandler(this.checkBox_useCineplex_CheckedChanged_1);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 49);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(176, 16);
            this.label3.TabIndex = 6;
            this.label3.Text = "NI-DAQ Device for Cineplex:";
            // 
            // comboBox_cineplexDevice
            // 
            this.comboBox_cineplexDevice.FormattingEnabled = true;
            this.comboBox_cineplexDevice.Location = new System.Drawing.Point(226, 46);
            this.comboBox_cineplexDevice.Name = "comboBox_cineplexDevice";
            this.comboBox_cineplexDevice.Size = new System.Drawing.Size(97, 24);
            this.comboBox_cineplexDevice.TabIndex = 4;
            this.comboBox_cineplexDevice.Text = "Dev1";
            // 
            // tabPage_stim
            // 
            this.tabPage_stim.Controls.Add(this.groupBox26);
            this.tabPage_stim.Controls.Add(this.groupBox12);
            this.tabPage_stim.Controls.Add(this.groupBox17);
            this.tabPage_stim.Controls.Add(this.checkBox_useStimulator);
            this.tabPage_stim.Controls.Add(this.label2);
            this.tabPage_stim.Controls.Add(this.groupBox18);
            this.tabPage_stim.Location = new System.Drawing.Point(4, 25);
            this.tabPage_stim.Name = "tabPage_stim";
            this.tabPage_stim.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_stim.Size = new System.Drawing.Size(349, 667);
            this.tabPage_stim.TabIndex = 1;
            this.tabPage_stim.Text = "Output";
            this.tabPage_stim.UseVisualStyleBackColor = true;
            // 
            // groupBox26
            // 
            this.groupBox26.Controls.Add(this.label25);
            this.groupBox26.Controls.Add(this.checkBox_useBuffloader);
            this.groupBox26.Location = new System.Drawing.Point(6, 275);
            this.groupBox26.Name = "groupBox26";
            this.groupBox26.Size = new System.Drawing.Size(335, 128);
            this.groupBox26.TabIndex = 23;
            this.groupBox26.TabStop = false;
            this.groupBox26.Text = "Double Buffered Output";
            // 
            // label25
            // 
            this.label25.Location = new System.Drawing.Point(3, 48);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(329, 83);
            this.label25.TabIndex = 10;
            this.label25.Text = "Disabling the double buffering system will prevent closed-loop applications from " +
                "using the \'StimSrv\' interface to your NI-DAQ hardware, and will also disable the" +
                " \'Loop()\' method.  ";
            // 
            // checkBox_useBuffloader
            // 
            this.checkBox_useBuffloader.AutoSize = true;
            this.checkBox_useBuffloader.Location = new System.Drawing.Point(6, 19);
            this.checkBox_useBuffloader.Name = "checkBox_useBuffloader";
            this.checkBox_useBuffloader.Size = new System.Drawing.Size(224, 20);
            this.checkBox_useBuffloader.TabIndex = 9;
            this.checkBox_useBuffloader.Text = "Disable Double Buffering System";
            this.checkBox_useBuffloader.UseVisualStyleBackColor = true;
            // 
            // groupBox12
            // 
            this.groupBox12.Controls.Add(this.checkBox3);
            this.groupBox12.Controls.Add(this.label17);
            this.groupBox12.Controls.Add(this.comboBox3);
            this.groupBox12.Location = new System.Drawing.Point(6, 409);
            this.groupBox12.Name = "groupBox12";
            this.groupBox12.Size = new System.Drawing.Size(335, 81);
            this.groupBox12.TabIndex = 22;
            this.groupBox12.TabStop = false;
            this.groupBox12.Text = "Single Channel Playback";
            this.groupBox12.Visible = false;
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Location = new System.Drawing.Point(6, 19);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(265, 20);
            this.checkBox3.TabIndex = 9;
            this.checkBox3.Text = "Enable Single Channel Playback/Output";
            this.checkBox3.UseVisualStyleBackColor = true;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(9, 54);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(193, 16);
            this.label17.TabIndex = 8;
            this.label17.Text = "NI-DAQ Device for BNC Output:";
            // 
            // comboBox3
            // 
            this.comboBox3.FormattingEnabled = true;
            this.comboBox3.Location = new System.Drawing.Point(227, 51);
            this.comboBox3.Name = "comboBox3";
            this.comboBox3.Size = new System.Drawing.Size(97, 24);
            this.comboBox3.TabIndex = 7;
            this.comboBox3.Text = "Dev1";
            // 
            // groupBox17
            // 
            this.groupBox17.Controls.Add(this.checkBox_UseAODO);
            this.groupBox17.Controls.Add(this.label13);
            this.groupBox17.Controls.Add(this.comboBox_SigOutDev);
            this.groupBox17.Location = new System.Drawing.Point(6, 188);
            this.groupBox17.Name = "groupBox17";
            this.groupBox17.Size = new System.Drawing.Size(335, 81);
            this.groupBox17.TabIndex = 19;
            this.groupBox17.TabStop = false;
            this.groupBox17.Text = "General AO and DO";
            // 
            // checkBox_UseAODO
            // 
            this.checkBox_UseAODO.AutoSize = true;
            this.checkBox_UseAODO.Location = new System.Drawing.Point(8, 19);
            this.checkBox_UseAODO.Name = "checkBox_UseAODO";
            this.checkBox_UseAODO.Size = new System.Drawing.Size(206, 20);
            this.checkBox_UseAODO.TabIndex = 24;
            this.checkBox_UseAODO.Text = "Use Analog and Digital Output";
            this.checkBox_UseAODO.UseVisualStyleBackColor = true;
            this.checkBox_UseAODO.CheckedChanged += new System.EventHandler(this.checkBox_UseAODO_CheckedChanged);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(8, 54);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(164, 16);
            this.label13.TabIndex = 8;
            this.label13.Text = "NI-DAQ Device for AO/DO";
            // 
            // comboBox_SigOutDev
            // 
            this.comboBox_SigOutDev.Enabled = false;
            this.comboBox_SigOutDev.FormattingEnabled = true;
            this.comboBox_SigOutDev.Location = new System.Drawing.Point(227, 51);
            this.comboBox_SigOutDev.Name = "comboBox_SigOutDev";
            this.comboBox_SigOutDev.Size = new System.Drawing.Size(97, 24);
            this.comboBox_SigOutDev.TabIndex = 7;
            this.comboBox_SigOutDev.Text = "Dev1";
            // 
            // checkBox_useStimulator
            // 
            this.checkBox_useStimulator.AutoSize = true;
            this.checkBox_useStimulator.Location = new System.Drawing.Point(14, 6);
            this.checkBox_useStimulator.Name = "checkBox_useStimulator";
            this.checkBox_useStimulator.Size = new System.Drawing.Size(114, 20);
            this.checkBox_useStimulator.TabIndex = 7;
            this.checkBox_useStimulator.Text = "Use Stimulator";
            this.checkBox_useStimulator.UseVisualStyleBackColor = true;
            this.checkBox_useStimulator.CheckedChanged += new System.EventHandler(this.checkBox_useStimulator_CheckedChanged_1);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(183, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "NI-DAQ Device for Stimulator:";
            // 
            // groupBox18
            // 
            this.groupBox18.Controls.Add(this.comboBox_stimulatorDevice);
            this.groupBox18.Controls.Add(this.groupBox13);
            this.groupBox18.Controls.Add(this.groupBox9);
            this.groupBox18.Controls.Add(this.groupBox1);
            this.groupBox18.Location = new System.Drawing.Point(6, 6);
            this.groupBox18.Name = "groupBox18";
            this.groupBox18.Size = new System.Drawing.Size(335, 176);
            this.groupBox18.TabIndex = 23;
            this.groupBox18.TabStop = false;
            this.groupBox18.Text = "groupBox18";
            // 
            // comboBox_stimulatorDevice
            // 
            this.comboBox_stimulatorDevice.FormattingEnabled = true;
            this.comboBox_stimulatorDevice.Location = new System.Drawing.Point(245, 23);
            this.comboBox_stimulatorDevice.Name = "comboBox_stimulatorDevice";
            this.comboBox_stimulatorDevice.Size = new System.Drawing.Size(79, 24);
            this.comboBox_stimulatorDevice.TabIndex = 3;
            this.comboBox_stimulatorDevice.Text = "Dev1";
            // 
            // groupBox13
            // 
            this.groupBox13.Controls.Add(this.label12);
            this.groupBox13.Controls.Add(this.comboBox_IVControlDevice);
            this.groupBox13.Location = new System.Drawing.Point(12, 115);
            this.groupBox13.Name = "groupBox13";
            this.groupBox13.Size = new System.Drawing.Size(318, 55);
            this.groupBox13.TabIndex = 18;
            this.groupBox13.TabStop = false;
            this.groupBox13.Text = "Stimulator I/V Control";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(6, 23);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(185, 16);
            this.label12.TabIndex = 8;
            this.label12.Text = "NI-DAQ Device for I/V Control:";
            // 
            // comboBox_IVControlDevice
            // 
            this.comboBox_IVControlDevice.FormattingEnabled = true;
            this.comboBox_IVControlDevice.Location = new System.Drawing.Point(215, 20);
            this.comboBox_IVControlDevice.Name = "comboBox_IVControlDevice";
            this.comboBox_IVControlDevice.Size = new System.Drawing.Size(97, 24);
            this.comboBox_IVControlDevice.TabIndex = 7;
            this.comboBox_IVControlDevice.Text = "Dev1";
            // 
            // groupBox9
            // 
            this.groupBox9.Controls.Add(this.radioButton_32bit);
            this.groupBox9.Controls.Add(this.radioButton_8bit);
            this.groupBox9.Location = new System.Drawing.Point(208, 57);
            this.groupBox9.Name = "groupBox9";
            this.groupBox9.Size = new System.Drawing.Size(122, 52);
            this.groupBox9.TabIndex = 13;
            this.groupBox9.TabStop = false;
            this.groupBox9.Text = "Port bandwidth";
            // 
            // radioButton_32bit
            // 
            this.radioButton_32bit.AutoSize = true;
            this.radioButton_32bit.Checked = true;
            this.radioButton_32bit.Location = new System.Drawing.Point(59, 19);
            this.radioButton_32bit.Name = "radioButton_32bit";
            this.radioButton_32bit.Size = new System.Drawing.Size(57, 20);
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
            this.radioButton_8bit.Size = new System.Drawing.Size(50, 20);
            this.radioButton_8bit.TabIndex = 11;
            this.radioButton_8bit.Text = "8 bit";
            this.radioButton_8bit.UseVisualStyleBackColor = true;
            this.radioButton_8bit.CheckedChanged += new System.EventHandler(this.radioButton_8bit_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioButton_16Mux);
            this.groupBox1.Controls.Add(this.radioButton_8Mux);
            this.groupBox1.Location = new System.Drawing.Point(12, 57);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(190, 52);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Multiplexor Type";
            // 
            // radioButton_16Mux
            // 
            this.radioButton_16Mux.AutoSize = true;
            this.radioButton_16Mux.Checked = true;
            this.radioButton_16Mux.Location = new System.Drawing.Point(99, 19);
            this.radioButton_16Mux.Name = "radioButton_16Mux";
            this.radioButton_16Mux.Size = new System.Drawing.Size(92, 20);
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
            this.radioButton_8Mux.Size = new System.Drawing.Size(85, 20);
            this.radioButton_8Mux.TabIndex = 11;
            this.radioButton_8Mux.Text = "8 Channel";
            this.radioButton_8Mux.UseVisualStyleBackColor = true;
            this.radioButton_8Mux.CheckedChanged += new System.EventHandler(this.radioButton_8Mux_CheckedChanged);
            // 
            // tabPage_input
            // 
            this.tabPage_input.Controls.Add(this.groupBox27);
            this.tabPage_input.Controls.Add(this.groupBox24);
            this.tabPage_input.Controls.Add(this.groupBox15);
            this.tabPage_input.Controls.Add(this.groupBox7);
            this.tabPage_input.Controls.Add(this.groupBox6);
            this.tabPage_input.Controls.Add(this.groupBox3);
            this.tabPage_input.Location = new System.Drawing.Point(4, 25);
            this.tabPage_input.Name = "tabPage_input";
            this.tabPage_input.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_input.Size = new System.Drawing.Size(349, 667);
            this.tabPage_input.TabIndex = 0;
            this.tabPage_input.Text = "Neural Input";
            this.tabPage_input.UseVisualStyleBackColor = true;
            // 
            // groupBox27
            // 
            this.groupBox27.Controls.Add(this.numericUpDown_MUArate);
            this.groupBox27.Controls.Add(this.numericUpDown_LFPrate);
            this.groupBox27.Controls.Add(this.numericUpDown_samplingRate);
            this.groupBox27.Controls.Add(this.label73);
            this.groupBox27.Controls.Add(this.comboBox_LFPGain);
            this.groupBox27.Controls.Add(this.label38);
            this.groupBox27.Controls.Add(this.comboBox_SpikeGain);
            this.groupBox27.Controls.Add(this.label_SpikeGain);
            this.groupBox27.Controls.Add(this.label_LFPSamplingRate);
            this.groupBox27.Controls.Add(this.label_SpikeSamplingRate);
            this.groupBox27.Controls.Add(this.label26);
            this.groupBox27.Controls.Add(this.comboBox_numChannels);
            this.groupBox27.Location = new System.Drawing.Point(6, 6);
            this.groupBox27.Name = "groupBox27";
            this.groupBox27.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.groupBox27.Size = new System.Drawing.Size(334, 170);
            this.groupBox27.TabIndex = 18;
            this.groupBox27.TabStop = false;
            this.groupBox27.Text = "A/D Input Properties";
            // 
            // numericUpDown_MUArate
            // 
            this.numericUpDown_MUArate.Location = new System.Drawing.Point(222, 111);
            this.numericUpDown_MUArate.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numericUpDown_MUArate.Name = "numericUpDown_MUArate";
            this.numericUpDown_MUArate.Size = new System.Drawing.Size(99, 22);
            this.numericUpDown_MUArate.TabIndex = 28;
            this.numericUpDown_MUArate.Value = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            // 
            // numericUpDown_LFPrate
            // 
            this.numericUpDown_LFPrate.Location = new System.Drawing.Point(222, 82);
            this.numericUpDown_LFPrate.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numericUpDown_LFPrate.Name = "numericUpDown_LFPrate";
            this.numericUpDown_LFPrate.Size = new System.Drawing.Size(99, 22);
            this.numericUpDown_LFPrate.TabIndex = 27;
            this.numericUpDown_LFPrate.Value = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            // 
            // numericUpDown_samplingRate
            // 
            this.numericUpDown_samplingRate.Location = new System.Drawing.Point(222, 52);
            this.numericUpDown_samplingRate.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numericUpDown_samplingRate.Name = "numericUpDown_samplingRate";
            this.numericUpDown_samplingRate.Size = new System.Drawing.Size(99, 22);
            this.numericUpDown_samplingRate.TabIndex = 26;
            this.numericUpDown_samplingRate.Value = new decimal(new int[] {
            25000,
            0,
            0,
            0});
            // 
            // label73
            // 
            this.label73.AutoSize = true;
            this.label73.Location = new System.Drawing.Point(9, 113);
            this.label73.Name = "label73";
            this.label73.Size = new System.Drawing.Size(163, 16);
            this.label73.TabIndex = 24;
            this.label73.Text = "MUA Sampling Rate (Hz): ";
            // 
            // comboBox_LFPGain
            // 
            this.comboBox_LFPGain.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_LFPGain.FormattingEnabled = true;
            this.comboBox_LFPGain.Items.AddRange(new object[] {
            "1",
            "2",
            "5",
            "10",
            "20",
            "50",
            "100"});
            this.comboBox_LFPGain.Location = new System.Drawing.Point(274, 139);
            this.comboBox_LFPGain.Name = "comboBox_LFPGain";
            this.comboBox_LFPGain.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.comboBox_LFPGain.Size = new System.Drawing.Size(47, 24);
            this.comboBox_LFPGain.TabIndex = 21;
            // 
            // label38
            // 
            this.label38.AutoSize = true;
            this.label38.Location = new System.Drawing.Point(175, 142);
            this.label38.Name = "label38";
            this.label38.Size = new System.Drawing.Size(101, 16);
            this.label38.TabIndex = 23;
            this.label38.Text = "Dig. Gain (LFP):";
            // 
            // comboBox_SpikeGain
            // 
            this.comboBox_SpikeGain.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_SpikeGain.FormattingEnabled = true;
            this.comboBox_SpikeGain.Items.AddRange(new object[] {
            "1",
            "2",
            "5",
            "10",
            "20",
            "50",
            "100"});
            this.comboBox_SpikeGain.Location = new System.Drawing.Point(119, 139);
            this.comboBox_SpikeGain.Name = "comboBox_SpikeGain";
            this.comboBox_SpikeGain.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.comboBox_SpikeGain.Size = new System.Drawing.Size(46, 24);
            this.comboBox_SpikeGain.TabIndex = 20;
            // 
            // label_SpikeGain
            // 
            this.label_SpikeGain.AutoSize = true;
            this.label_SpikeGain.Location = new System.Drawing.Point(10, 142);
            this.label_SpikeGain.Name = "label_SpikeGain";
            this.label_SpikeGain.Size = new System.Drawing.Size(104, 16);
            this.label_SpikeGain.TabIndex = 22;
            this.label_SpikeGain.Text = "Dig. Gain (Raw):";
            // 
            // label_LFPSamplingRate
            // 
            this.label_LFPSamplingRate.AutoSize = true;
            this.label_LFPSamplingRate.Location = new System.Drawing.Point(9, 84);
            this.label_LFPSamplingRate.Name = "label_LFPSamplingRate";
            this.label_LFPSamplingRate.Size = new System.Drawing.Size(157, 16);
            this.label_LFPSamplingRate.TabIndex = 17;
            this.label_LFPSamplingRate.Text = "LFP Sampling Rate (Hz): ";
            // 
            // label_SpikeSamplingRate
            // 
            this.label_SpikeSamplingRate.AutoSize = true;
            this.label_SpikeSamplingRate.Location = new System.Drawing.Point(8, 54);
            this.label_SpikeSamplingRate.Name = "label_SpikeSamplingRate";
            this.label_SpikeSamplingRate.Size = new System.Drawing.Size(157, 16);
            this.label_SpikeSamplingRate.TabIndex = 16;
            this.label_SpikeSamplingRate.Text = "Raw Sampling Rate (Hz):";
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(9, 24);
            this.label26.Name = "label26";
            this.label26.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.label26.Size = new System.Drawing.Size(101, 16);
            this.label26.TabIndex = 8;
            this.label26.Text = "Num. Channels:";
            // 
            // comboBox_numChannels
            // 
            this.comboBox_numChannels.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_numChannels.FormattingEnabled = true;
            this.comboBox_numChannels.Items.AddRange(new object[] {
            "16",
            "32",
            "64"});
            this.comboBox_numChannels.Location = new System.Drawing.Point(222, 21);
            this.comboBox_numChannels.Name = "comboBox_numChannels";
            this.comboBox_numChannels.Size = new System.Drawing.Size(99, 24);
            this.comboBox_numChannels.TabIndex = 2;
            // 
            // groupBox24
            // 
            this.groupBox24.Controls.Add(this.checkBox_processMUA);
            this.groupBox24.Controls.Add(this.checkBox_processLFPs);
            this.groupBox24.Location = new System.Drawing.Point(6, 615);
            this.groupBox24.Name = "groupBox24";
            this.groupBox24.Size = new System.Drawing.Size(335, 47);
            this.groupBox24.TabIndex = 17;
            this.groupBox24.TabStop = false;
            this.groupBox24.Text = "Digital Filtering for LFP/MUA Streams";
            // 
            // checkBox_processMUA
            // 
            this.checkBox_processMUA.AutoSize = true;
            this.checkBox_processMUA.Location = new System.Drawing.Point(127, 19);
            this.checkBox_processMUA.Name = "checkBox_processMUA";
            this.checkBox_processMUA.Size = new System.Drawing.Size(110, 20);
            this.checkBox_processMUA.TabIndex = 5;
            this.checkBox_processMUA.Text = "Process MUA";
            this.checkBox_processMUA.UseVisualStyleBackColor = true;
            // 
            // checkBox_processLFPs
            // 
            this.checkBox_processLFPs.AutoSize = true;
            this.checkBox_processLFPs.Location = new System.Drawing.Point(7, 19);
            this.checkBox_processLFPs.Name = "checkBox_processLFPs";
            this.checkBox_processLFPs.Size = new System.Drawing.Size(114, 20);
            this.checkBox_processLFPs.TabIndex = 4;
            this.checkBox_processLFPs.Text = "Process LFPs ";
            this.checkBox_processLFPs.UseVisualStyleBackColor = true;
            // 
            // groupBox15
            // 
            this.groupBox15.Controls.Add(this.numericUpDown_PreAmpGain);
            this.groupBox15.Controls.Add(this.groupBox16);
            this.groupBox15.Controls.Add(this.label14);
            this.groupBox15.Location = new System.Drawing.Point(6, 182);
            this.groupBox15.Name = "groupBox15";
            this.groupBox15.Size = new System.Drawing.Size(338, 57);
            this.groupBox15.TabIndex = 16;
            this.groupBox15.TabStop = false;
            this.groupBox15.Text = "External Amplifier/Headstage Gain";
            // 
            // numericUpDown_PreAmpGain
            // 
            this.numericUpDown_PreAmpGain.Location = new System.Drawing.Point(222, 22);
            this.numericUpDown_PreAmpGain.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numericUpDown_PreAmpGain.Name = "numericUpDown_PreAmpGain";
            this.numericUpDown_PreAmpGain.Size = new System.Drawing.Size(99, 22);
            this.numericUpDown_PreAmpGain.TabIndex = 17;
            this.numericUpDown_PreAmpGain.Value = new decimal(new int[] {
            1200,
            0,
            0,
            0});
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
            this.label14.Size = new System.Drawing.Size(159, 16);
            this.label14.TabIndex = 1;
            this.label14.Text = "Amplifier Passband Gain:";
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.checkBox_useSecondBoard);
            this.groupBox7.Controls.Add(this.groupBox8);
            this.groupBox7.Controls.Add(this.label8);
            this.groupBox7.Controls.Add(this.comboBox_analogInputDevice2);
            this.groupBox7.Location = new System.Drawing.Point(7, 380);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(334, 152);
            this.groupBox7.TabIndex = 15;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "Electrode Input #2";
            // 
            // checkBox_useSecondBoard
            // 
            this.checkBox_useSecondBoard.AutoSize = true;
            this.checkBox_useSecondBoard.Location = new System.Drawing.Point(9, 21);
            this.checkBox_useSecondBoard.Name = "checkBox_useSecondBoard";
            this.checkBox_useSecondBoard.Size = new System.Drawing.Size(142, 20);
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
            this.groupBox8.Location = new System.Drawing.Point(6, 71);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.Size = new System.Drawing.Size(322, 71);
            this.groupBox8.TabIndex = 14;
            this.groupBox8.TabStop = false;
            this.groupBox8.Text = "LFP Input";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 44);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(179, 16);
            this.label7.TabIndex = 13;
            this.label7.Text = "NI-DAQ Device for LFP Input:";
            // 
            // checkBox_sepLFPBoard2
            // 
            this.checkBox_sepLFPBoard2.AutoSize = true;
            this.checkBox_sepLFPBoard2.Location = new System.Drawing.Point(6, 19);
            this.checkBox_sepLFPBoard2.Name = "checkBox_sepLFPBoard2";
            this.checkBox_sepLFPBoard2.Size = new System.Drawing.Size(219, 20);
            this.checkBox_sepLFPBoard2.TabIndex = 11;
            this.checkBox_sepLFPBoard2.Text = "Use Separate NI-DAQ for LFPs?";
            this.checkBox_sepLFPBoard2.UseVisualStyleBackColor = true;
            // 
            // comboBox_LFPDevice2
            // 
            this.comboBox_LFPDevice2.FormattingEnabled = true;
            this.comboBox_LFPDevice2.Location = new System.Drawing.Point(215, 41);
            this.comboBox_LFPDevice2.Name = "comboBox_LFPDevice2";
            this.comboBox_LFPDevice2.Size = new System.Drawing.Size(97, 24);
            this.comboBox_LFPDevice2.TabIndex = 12;
            this.comboBox_LFPDevice2.Text = "Dev1";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(4, 44);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(198, 16);
            this.label8.TabIndex = 1;
            this.label8.Text = "NI-DAQ Device for Analog Input:";
            // 
            // comboBox_analogInputDevice2
            // 
            this.comboBox_analogInputDevice2.Enabled = false;
            this.comboBox_analogInputDevice2.FormattingEnabled = true;
            this.comboBox_analogInputDevice2.Location = new System.Drawing.Point(221, 41);
            this.comboBox_analogInputDevice2.Name = "comboBox_analogInputDevice2";
            this.comboBox_analogInputDevice2.Size = new System.Drawing.Size(97, 24);
            this.comboBox_analogInputDevice2.TabIndex = 0;
            this.comboBox_analogInputDevice2.Text = "Dev1";
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.checkBox_useEEG);
            this.groupBox6.Controls.Add(this.comboBox_EEG);
            this.groupBox6.Controls.Add(this.label6);
            this.groupBox6.Location = new System.Drawing.Point(6, 538);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(334, 71);
            this.groupBox6.TabIndex = 9;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "EEG";
            // 
            // checkBox_useEEG
            // 
            this.checkBox_useEEG.AutoSize = true;
            this.checkBox_useEEG.Location = new System.Drawing.Point(6, 19);
            this.checkBox_useEEG.Name = "checkBox_useEEG";
            this.checkBox_useEEG.Size = new System.Drawing.Size(322, 20);
            this.checkBox_useEEG.TabIndex = 7;
            this.checkBox_useEEG.Text = "Use EEG Channels (separate analog in channels)";
            this.checkBox_useEEG.UseVisualStyleBackColor = true;
            this.checkBox_useEEG.CheckedChanged += new System.EventHandler(this.checkBox_useEEG_CheckedChanged);
            // 
            // comboBox_EEG
            // 
            this.comboBox_EEG.Enabled = false;
            this.comboBox_EEG.FormattingEnabled = true;
            this.comboBox_EEG.Location = new System.Drawing.Point(222, 41);
            this.comboBox_EEG.Name = "comboBox_EEG";
            this.comboBox_EEG.Size = new System.Drawing.Size(97, 24);
            this.comboBox_EEG.TabIndex = 3;
            this.comboBox_EEG.Text = "Dev1";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 44);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(152, 16);
            this.label6.TabIndex = 5;
            this.label6.Text = "NI-DAQ Device for EEG:";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.groupBox14);
            this.groupBox3.Controls.Add(this.groupBox4);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.comboBox_analogInputDevice1);
            this.groupBox3.Location = new System.Drawing.Point(7, 245);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(334, 129);
            this.groupBox3.TabIndex = 12;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Electrode Input #1";
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
            this.groupBox4.Location = new System.Drawing.Point(6, 52);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(322, 71);
            this.groupBox4.TabIndex = 14;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "LFP Input";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 44);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(179, 16);
            this.label4.TabIndex = 13;
            this.label4.Text = "NI-DAQ Device for LFP Input:";
            // 
            // checkBox_sepLFPBoard1
            // 
            this.checkBox_sepLFPBoard1.AutoSize = true;
            this.checkBox_sepLFPBoard1.Location = new System.Drawing.Point(6, 19);
            this.checkBox_sepLFPBoard1.Name = "checkBox_sepLFPBoard1";
            this.checkBox_sepLFPBoard1.Size = new System.Drawing.Size(219, 20);
            this.checkBox_sepLFPBoard1.TabIndex = 11;
            this.checkBox_sepLFPBoard1.Text = "Use Separate NI-DAQ for LFPs?";
            this.checkBox_sepLFPBoard1.UseVisualStyleBackColor = true;
            this.checkBox_sepLFPBoard1.CheckedChanged += new System.EventHandler(this.checkBox_sepLFPBoard_CheckedChanged);
            // 
            // comboBox_LFPDevice1
            // 
            this.comboBox_LFPDevice1.FormattingEnabled = true;
            this.comboBox_LFPDevice1.Location = new System.Drawing.Point(215, 41);
            this.comboBox_LFPDevice1.Name = "comboBox_LFPDevice1";
            this.comboBox_LFPDevice1.Size = new System.Drawing.Size(97, 24);
            this.comboBox_LFPDevice1.TabIndex = 12;
            this.comboBox_LFPDevice1.Text = "Dev1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(198, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "NI-DAQ Device for Analog Input:";
            // 
            // comboBox_analogInputDevice1
            // 
            this.comboBox_analogInputDevice1.FormattingEnabled = true;
            this.comboBox_analogInputDevice1.Location = new System.Drawing.Point(221, 21);
            this.comboBox_analogInputDevice1.Name = "comboBox_analogInputDevice1";
            this.comboBox_analogInputDevice1.Size = new System.Drawing.Size(97, 24);
            this.comboBox_analogInputDevice1.TabIndex = 0;
            this.comboBox_analogInputDevice1.Text = "Dev1";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPag_Gen);
            this.tabControl1.Controls.Add(this.tabPage_input);
            this.tabControl1.Controls.Add(this.tabPage_AuxInput);
            this.tabControl1.Controls.Add(this.tabPage_stim);
            this.tabControl1.Controls.Add(this.tabPage_misc);
            this.tabControl1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabControl1.Location = new System.Drawing.Point(5, 5);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(357, 696);
            this.tabControl1.TabIndex = 16;
            // 
            // tabPag_Gen
            // 
            this.tabPag_Gen.Controls.Add(this.groupBox25);
            this.tabPag_Gen.Controls.Add(this.groupBox21);
            this.tabPag_Gen.Location = new System.Drawing.Point(4, 25);
            this.tabPag_Gen.Name = "tabPag_Gen";
            this.tabPag_Gen.Padding = new System.Windows.Forms.Padding(3);
            this.tabPag_Gen.Size = new System.Drawing.Size(349, 667);
            this.tabPag_Gen.TabIndex = 4;
            this.tabPag_Gen.Text = "Real-Time";
            this.tabPag_Gen.UseVisualStyleBackColor = true;
            // 
            // groupBox25
            // 
            this.groupBox25.Controls.Add(this.checkBox_UseAuxDataBuffer);
            this.groupBox25.Controls.Add(this.checkBox_UseDigDataBuffer);
            this.groupBox25.Controls.Add(this.checkBox_UseStimDataBuffer);
            this.groupBox25.Controls.Add(this.checkBox_UseEEGDataBuffer);
            this.groupBox25.Controls.Add(this.checkBox_UseLFPDataBuffer);
            this.groupBox25.Controls.Add(this.checkBox_UseSALPADataBuffer);
            this.groupBox25.Controls.Add(this.checkBox_UseBPDataBuffer);
            this.groupBox25.Controls.Add(this.checkBox_UseSpikeDataBuffer);
            this.groupBox25.Controls.Add(this.checkBox_UseRawDataBuffer);
            this.groupBox25.Location = new System.Drawing.Point(6, 165);
            this.groupBox25.Name = "groupBox25";
            this.groupBox25.Size = new System.Drawing.Size(335, 328);
            this.groupBox25.TabIndex = 18;
            this.groupBox25.TabStop = false;
            this.groupBox25.Text = "Real-Time Data Streams";
            // 
            // checkBox_UseAuxDataBuffer
            // 
            this.checkBox_UseAuxDataBuffer.AutoSize = true;
            this.checkBox_UseAuxDataBuffer.Location = new System.Drawing.Point(9, 302);
            this.checkBox_UseAuxDataBuffer.Name = "checkBox_UseAuxDataBuffer";
            this.checkBox_UseAuxDataBuffer.Size = new System.Drawing.Size(331, 20);
            this.checkBox_UseAuxDataBuffer.TabIndex = 23;
            this.checkBox_UseAuxDataBuffer.Text = "Use Auxilary Analog Data Buffer (When Applicable)";
            this.checkBox_UseAuxDataBuffer.UseVisualStyleBackColor = true;
            this.checkBox_UseAuxDataBuffer.CheckedChanged += new System.EventHandler(this.checkBox_UseAuxDataBuffer_CheckedChanged);
            // 
            // checkBox_UseDigDataBuffer
            // 
            this.checkBox_UseDigDataBuffer.AutoSize = true;
            this.checkBox_UseDigDataBuffer.Location = new System.Drawing.Point(9, 267);
            this.checkBox_UseDigDataBuffer.Name = "checkBox_UseDigDataBuffer";
            this.checkBox_UseDigDataBuffer.Size = new System.Drawing.Size(276, 20);
            this.checkBox_UseDigDataBuffer.TabIndex = 23;
            this.checkBox_UseDigDataBuffer.Text = "Use Digital Data Buffer (When Applicable)";
            this.checkBox_UseDigDataBuffer.UseVisualStyleBackColor = true;
            this.checkBox_UseDigDataBuffer.CheckedChanged += new System.EventHandler(this.checkBox_UseDigDataBuffer_CheckedChanged);
            // 
            // checkBox_UseStimDataBuffer
            // 
            this.checkBox_UseStimDataBuffer.AutoSize = true;
            this.checkBox_UseStimDataBuffer.Location = new System.Drawing.Point(9, 232);
            this.checkBox_UseStimDataBuffer.Name = "checkBox_UseStimDataBuffer";
            this.checkBox_UseStimDataBuffer.Size = new System.Drawing.Size(282, 20);
            this.checkBox_UseStimDataBuffer.TabIndex = 23;
            this.checkBox_UseStimDataBuffer.Text = "Use E. Stim. Data Buffer (When Applicable)";
            this.checkBox_UseStimDataBuffer.UseVisualStyleBackColor = true;
            this.checkBox_UseStimDataBuffer.CheckedChanged += new System.EventHandler(this.checkBox_UseStimDataBuffer_CheckedChanged);
            // 
            // checkBox_UseEEGDataBuffer
            // 
            this.checkBox_UseEEGDataBuffer.AutoSize = true;
            this.checkBox_UseEEGDataBuffer.Location = new System.Drawing.Point(9, 197);
            this.checkBox_UseEEGDataBuffer.Name = "checkBox_UseEEGDataBuffer";
            this.checkBox_UseEEGDataBuffer.Size = new System.Drawing.Size(266, 20);
            this.checkBox_UseEEGDataBuffer.TabIndex = 23;
            this.checkBox_UseEEGDataBuffer.Text = "Use EEG Data Buffer (When Applicable)";
            this.checkBox_UseEEGDataBuffer.UseVisualStyleBackColor = true;
            this.checkBox_UseEEGDataBuffer.CheckedChanged += new System.EventHandler(this.checkBox_UseEEGDataBuffer_CheckedChanged);
            // 
            // checkBox_UseLFPDataBuffer
            // 
            this.checkBox_UseLFPDataBuffer.AutoSize = true;
            this.checkBox_UseLFPDataBuffer.Location = new System.Drawing.Point(9, 162);
            this.checkBox_UseLFPDataBuffer.Name = "checkBox_UseLFPDataBuffer";
            this.checkBox_UseLFPDataBuffer.Size = new System.Drawing.Size(262, 20);
            this.checkBox_UseLFPDataBuffer.TabIndex = 23;
            this.checkBox_UseLFPDataBuffer.Text = "Use LFP Data Buffer (When Applicable)";
            this.checkBox_UseLFPDataBuffer.UseVisualStyleBackColor = true;
            this.checkBox_UseLFPDataBuffer.CheckedChanged += new System.EventHandler(this.checkBox_UseLFPDataBuffer_CheckedChanged);
            // 
            // checkBox_UseSALPADataBuffer
            // 
            this.checkBox_UseSALPADataBuffer.AutoSize = true;
            this.checkBox_UseSALPADataBuffer.Location = new System.Drawing.Point(9, 127);
            this.checkBox_UseSALPADataBuffer.Name = "checkBox_UseSALPADataBuffer";
            this.checkBox_UseSALPADataBuffer.Size = new System.Drawing.Size(281, 20);
            this.checkBox_UseSALPADataBuffer.TabIndex = 23;
            this.checkBox_UseSALPADataBuffer.Text = "Use SALPA Data Buffer (When Applicable)";
            this.checkBox_UseSALPADataBuffer.UseVisualStyleBackColor = true;
            this.checkBox_UseSALPADataBuffer.CheckedChanged += new System.EventHandler(this.checkBox_UseSALPADataBuffer_CheckedChanged);
            // 
            // checkBox_UseBPDataBuffer
            // 
            this.checkBox_UseBPDataBuffer.AutoSize = true;
            this.checkBox_UseBPDataBuffer.Location = new System.Drawing.Point(9, 92);
            this.checkBox_UseBPDataBuffer.Name = "checkBox_UseBPDataBuffer";
            this.checkBox_UseBPDataBuffer.Size = new System.Drawing.Size(305, 20);
            this.checkBox_UseBPDataBuffer.TabIndex = 23;
            this.checkBox_UseBPDataBuffer.Text = "Use Band-Pass Data Buffer (When Applicable)";
            this.checkBox_UseBPDataBuffer.UseVisualStyleBackColor = true;
            this.checkBox_UseBPDataBuffer.CheckedChanged += new System.EventHandler(this.checkBox_UseBPDataBuffer_CheckedChanged);
            // 
            // checkBox_UseSpikeDataBuffer
            // 
            this.checkBox_UseSpikeDataBuffer.AutoSize = true;
            this.checkBox_UseSpikeDataBuffer.Location = new System.Drawing.Point(9, 57);
            this.checkBox_UseSpikeDataBuffer.Name = "checkBox_UseSpikeDataBuffer";
            this.checkBox_UseSpikeDataBuffer.Size = new System.Drawing.Size(159, 20);
            this.checkBox_UseSpikeDataBuffer.TabIndex = 23;
            this.checkBox_UseSpikeDataBuffer.Text = "Use Spike Data Buffer";
            this.checkBox_UseSpikeDataBuffer.UseVisualStyleBackColor = true;
            this.checkBox_UseSpikeDataBuffer.CheckedChanged += new System.EventHandler(this.checkBox_UseSpikeDataBuffer_CheckedChanged);
            // 
            // checkBox_UseRawDataBuffer
            // 
            this.checkBox_UseRawDataBuffer.AutoSize = true;
            this.checkBox_UseRawDataBuffer.Location = new System.Drawing.Point(9, 22);
            this.checkBox_UseRawDataBuffer.Name = "checkBox_UseRawDataBuffer";
            this.checkBox_UseRawDataBuffer.Size = new System.Drawing.Size(151, 20);
            this.checkBox_UseRawDataBuffer.TabIndex = 23;
            this.checkBox_UseRawDataBuffer.Text = "Use Raw Data Buffer";
            this.checkBox_UseRawDataBuffer.UseVisualStyleBackColor = true;
            this.checkBox_UseRawDataBuffer.CheckedChanged += new System.EventHandler(this.checkBox_UseRawDataBuffer_CheckedChanged);
            // 
            // groupBox21
            // 
            this.groupBox21.Controls.Add(this.robustStim_checkbox);
            this.groupBox21.Controls.Add(this.numericUpDown_datSrvBufferSizeSec);
            this.groupBox21.Controls.Add(this.label21);
            this.groupBox21.Controls.Add(this.label24);
            this.groupBox21.Controls.Add(this.label23);
            this.groupBox21.Controls.Add(this.numericUpDown_DACPollingPeriodSec);
            this.groupBox21.Controls.Add(this.numericUpDown_ADCPollingPeriodSec);
            this.groupBox21.Controls.Add(this.label22);
            this.groupBox21.Controls.Add(this.groupBox23);
            this.groupBox21.Location = new System.Drawing.Point(6, 6);
            this.groupBox21.Name = "groupBox21";
            this.groupBox21.Size = new System.Drawing.Size(335, 153);
            this.groupBox21.TabIndex = 17;
            this.groupBox21.TabStop = false;
            this.groupBox21.Text = "DAC Polling/Buffering";
            // 
            // robustStim_checkbox
            // 
            this.robustStim_checkbox.AutoSize = true;
            this.robustStim_checkbox.Location = new System.Drawing.Point(9, 127);
            this.robustStim_checkbox.Name = "robustStim_checkbox";
            this.robustStim_checkbox.Size = new System.Drawing.Size(324, 20);
            this.robustStim_checkbox.TabIndex = 22;
            this.robustStim_checkbox.Text = "Enable recovery from real-time scheduling failures";
            this.robustStim_checkbox.UseVisualStyleBackColor = true;
            // 
            // numericUpDown_datSrvBufferSizeSec
            // 
            this.numericUpDown_datSrvBufferSizeSec.DecimalPlaces = 3;
            this.numericUpDown_datSrvBufferSizeSec.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numericUpDown_datSrvBufferSizeSec.Location = new System.Drawing.Point(240, 94);
            this.numericUpDown_datSrvBufferSizeSec.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDown_datSrvBufferSizeSec.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDown_datSrvBufferSizeSec.Name = "numericUpDown_datSrvBufferSizeSec";
            this.numericUpDown_datSrvBufferSizeSec.Size = new System.Drawing.Size(89, 22);
            this.numericUpDown_datSrvBufferSizeSec.TabIndex = 20;
            this.numericUpDown_datSrvBufferSizeSec.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(6, 60);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(156, 16);
            this.label21.TabIndex = 21;
            this.label21.Text = "DAC Polling Period (sec)";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(6, 24);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(156, 16);
            this.label24.TabIndex = 21;
            this.label24.Text = "ADC Polling Period (sec)";
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(6, 54);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(0, 16);
            this.label23.TabIndex = 19;
            this.label23.Tag = "ADC Polling Period (sec)";
            // 
            // numericUpDown_DACPollingPeriodSec
            // 
            this.numericUpDown_DACPollingPeriodSec.CausesValidation = false;
            this.numericUpDown_DACPollingPeriodSec.DecimalPlaces = 3;
            this.numericUpDown_DACPollingPeriodSec.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.numericUpDown_DACPollingPeriodSec.Location = new System.Drawing.Point(240, 58);
            this.numericUpDown_DACPollingPeriodSec.Maximum = new decimal(new int[] {
            2,
            0,
            0,
            65536});
            this.numericUpDown_DACPollingPeriodSec.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.numericUpDown_DACPollingPeriodSec.Name = "numericUpDown_DACPollingPeriodSec";
            this.numericUpDown_DACPollingPeriodSec.Size = new System.Drawing.Size(89, 22);
            this.numericUpDown_DACPollingPeriodSec.TabIndex = 18;
            this.numericUpDown_DACPollingPeriodSec.Value = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDown_DACPollingPeriodSec.ValueChanged += new System.EventHandler(this.numericUpDown_DACPollingPeriodSec_ValueChanged);
            // 
            // numericUpDown_ADCPollingPeriodSec
            // 
            this.numericUpDown_ADCPollingPeriodSec.DecimalPlaces = 3;
            this.numericUpDown_ADCPollingPeriodSec.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.numericUpDown_ADCPollingPeriodSec.Location = new System.Drawing.Point(240, 22);
            this.numericUpDown_ADCPollingPeriodSec.Maximum = new decimal(new int[] {
            2,
            0,
            0,
            65536});
            this.numericUpDown_ADCPollingPeriodSec.Minimum = new decimal(new int[] {
            4,
            0,
            0,
            196608});
            this.numericUpDown_ADCPollingPeriodSec.Name = "numericUpDown_ADCPollingPeriodSec";
            this.numericUpDown_ADCPollingPeriodSec.Size = new System.Drawing.Size(89, 22);
            this.numericUpDown_ADCPollingPeriodSec.TabIndex = 18;
            this.numericUpDown_ADCPollingPeriodSec.Value = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDown_ADCPollingPeriodSec.ValueChanged += new System.EventHandler(this.numericUpDown_ADCPollingPeriodSec_ValueChanged);
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(6, 96);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(164, 16);
            this.label22.TabIndex = 18;
            this.label22.Text = "On Board Buffer Size (sec)";
            // 
            // groupBox23
            // 
            this.groupBox23.Location = new System.Drawing.Point(2, -63);
            this.groupBox23.Name = "groupBox23";
            this.groupBox23.Size = new System.Drawing.Size(286, 59);
            this.groupBox23.TabIndex = 15;
            this.groupBox23.TabStop = false;
            this.groupBox23.Text = "Analog Input #1";
            // 
            // tabPage_AuxInput
            // 
            this.tabPage_AuxInput.Controls.Add(this.groupBox20);
            this.tabPage_AuxInput.Controls.Add(this.groupBox19);
            this.tabPage_AuxInput.Controls.Add(this.groupBox10);
            this.tabPage_AuxInput.Location = new System.Drawing.Point(4, 25);
            this.tabPage_AuxInput.Name = "tabPage_AuxInput";
            this.tabPage_AuxInput.Size = new System.Drawing.Size(349, 667);
            this.tabPage_AuxInput.TabIndex = 3;
            this.tabPage_AuxInput.Text = "Aux. Input";
            this.tabPage_AuxInput.UseVisualStyleBackColor = true;
            // 
            // groupBox20
            // 
            this.groupBox20.Controls.Add(this.comboBox_AuxDigInputPort);
            this.groupBox20.Controls.Add(this.label18);
            this.groupBox20.Controls.Add(this.checkBox_UseAuxDigitalInput);
            this.groupBox20.Location = new System.Drawing.Point(10, 259);
            this.groupBox20.Name = "groupBox20";
            this.groupBox20.Size = new System.Drawing.Size(334, 77);
            this.groupBox20.TabIndex = 21;
            this.groupBox20.TabStop = false;
            this.groupBox20.Text = "Auxiliary Digital Input";
            // 
            // comboBox_AuxDigInputPort
            // 
            this.comboBox_AuxDigInputPort.FormattingEnabled = true;
            this.comboBox_AuxDigInputPort.Location = new System.Drawing.Point(228, 42);
            this.comboBox_AuxDigInputPort.Name = "comboBox_AuxDigInputPort";
            this.comboBox_AuxDigInputPort.Size = new System.Drawing.Size(91, 24);
            this.comboBox_AuxDigInputPort.TabIndex = 18;
            this.comboBox_AuxDigInputPort.Text = "Dev1";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(6, 45);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(103, 16);
            this.label18.TabIndex = 19;
            this.label18.Text = "NI-DAQ Device:";
            // 
            // checkBox_UseAuxDigitalInput
            // 
            this.checkBox_UseAuxDigitalInput.AutoSize = true;
            this.checkBox_UseAuxDigitalInput.Location = new System.Drawing.Point(6, 19);
            this.checkBox_UseAuxDigitalInput.Name = "checkBox_UseAuxDigitalInput";
            this.checkBox_UseAuxDigitalInput.Size = new System.Drawing.Size(167, 20);
            this.checkBox_UseAuxDigitalInput.TabIndex = 15;
            this.checkBox_UseAuxDigitalInput.Text = "Enable aux. digital input";
            this.checkBox_UseAuxDigitalInput.UseVisualStyleBackColor = true;
            this.checkBox_UseAuxDigitalInput.CheckedChanged += new System.EventHandler(this.checkBox_UseAuxDigitalInput_CheckedChanged);
            // 
            // groupBox19
            // 
            this.groupBox19.Controls.Add(this.listBox_AuxAnalogInChan);
            this.groupBox19.Controls.Add(this.label19);
            this.groupBox19.Controls.Add(this.comboBox_AuxAnalogInputDevice);
            this.groupBox19.Controls.Add(this.label16);
            this.groupBox19.Controls.Add(this.checkBox_UseAuxAnalogInput);
            this.groupBox19.Location = new System.Drawing.Point(10, 94);
            this.groupBox19.Name = "groupBox19";
            this.groupBox19.Size = new System.Drawing.Size(334, 159);
            this.groupBox19.TabIndex = 20;
            this.groupBox19.TabStop = false;
            this.groupBox19.Text = "Auxiliary Analog Input";
            // 
            // listBox_AuxAnalogInChan
            // 
            this.listBox_AuxAnalogInChan.FormattingEnabled = true;
            this.listBox_AuxAnalogInChan.ItemHeight = 16;
            this.listBox_AuxAnalogInChan.Location = new System.Drawing.Point(228, 72);
            this.listBox_AuxAnalogInChan.Name = "listBox_AuxAnalogInChan";
            this.listBox_AuxAnalogInChan.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.listBox_AuxAnalogInChan.Size = new System.Drawing.Size(91, 68);
            this.listBox_AuxAnalogInChan.TabIndex = 22;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(6, 71);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(67, 16);
            this.label19.TabIndex = 21;
            this.label19.Text = "Channels:";
            // 
            // comboBox_AuxAnalogInputDevice
            // 
            this.comboBox_AuxAnalogInputDevice.FormattingEnabled = true;
            this.comboBox_AuxAnalogInputDevice.Location = new System.Drawing.Point(228, 42);
            this.comboBox_AuxAnalogInputDevice.Name = "comboBox_AuxAnalogInputDevice";
            this.comboBox_AuxAnalogInputDevice.Size = new System.Drawing.Size(91, 24);
            this.comboBox_AuxAnalogInputDevice.TabIndex = 18;
            this.comboBox_AuxAnalogInputDevice.Text = "Dev1";
            this.comboBox_AuxAnalogInputDevice.SelectedIndexChanged += new System.EventHandler(this.comboBox_AuxAnalogInputDevice_SelectedIndexChanged);
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(6, 45);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(103, 16);
            this.label16.TabIndex = 19;
            this.label16.Text = "NI-DAQ Device:";
            // 
            // checkBox_UseAuxAnalogInput
            // 
            this.checkBox_UseAuxAnalogInput.AutoSize = true;
            this.checkBox_UseAuxAnalogInput.Location = new System.Drawing.Point(6, 19);
            this.checkBox_UseAuxAnalogInput.Name = "checkBox_UseAuxAnalogInput";
            this.checkBox_UseAuxAnalogInput.Size = new System.Drawing.Size(235, 20);
            this.checkBox_UseAuxAnalogInput.TabIndex = 15;
            this.checkBox_UseAuxAnalogInput.Text = "Enable extra analog input channels";
            this.checkBox_UseAuxAnalogInput.UseVisualStyleBackColor = true;
            this.checkBox_UseAuxAnalogInput.CheckedChanged += new System.EventHandler(this.checkBox_UseAuxAnalogInput_CheckedChanged);
            // 
            // groupBox10
            // 
            this.groupBox10.Controls.Add(this.comboBox_stimInfoDev);
            this.groupBox10.Controls.Add(this.label15);
            this.groupBox10.Controls.Add(this.checkBox_RecStimTimes);
            this.groupBox10.Location = new System.Drawing.Point(10, 12);
            this.groupBox10.Name = "groupBox10";
            this.groupBox10.Size = new System.Drawing.Size(334, 76);
            this.groupBox10.TabIndex = 18;
            this.groupBox10.TabStop = false;
            this.groupBox10.Text = "Stimulation Timing";
            // 
            // comboBox_stimInfoDev
            // 
            this.comboBox_stimInfoDev.Enabled = false;
            this.comboBox_stimInfoDev.FormattingEnabled = true;
            this.comboBox_stimInfoDev.Location = new System.Drawing.Point(228, 42);
            this.comboBox_stimInfoDev.Name = "comboBox_stimInfoDev";
            this.comboBox_stimInfoDev.Size = new System.Drawing.Size(91, 24);
            this.comboBox_stimInfoDev.TabIndex = 18;
            this.comboBox_stimInfoDev.Text = "Dev1";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(6, 45);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(103, 16);
            this.label15.TabIndex = 19;
            this.label15.Text = "NI-DAQ Device:";
            // 
            // checkBox_RecStimTimes
            // 
            this.checkBox_RecStimTimes.AutoSize = true;
            this.checkBox_RecStimTimes.Location = new System.Drawing.Point(6, 19);
            this.checkBox_RecStimTimes.Name = "checkBox_RecStimTimes";
            this.checkBox_RecStimTimes.Size = new System.Drawing.Size(288, 20);
            this.checkBox_RecStimTimes.TabIndex = 15;
            this.checkBox_RecStimTimes.Text = "Enable pulsatile stimulation-timing recording";
            this.checkBox_RecStimTimes.UseVisualStyleBackColor = true;
            this.checkBox_RecStimTimes.CheckedChanged += new System.EventHandler(this.checkBox_RecStimTimes_CheckedChanged);
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
            // checkBox_useChannelPlayback
            // 
            this.checkBox_useChannelPlayback.AutoSize = true;
            this.checkBox_useChannelPlayback.Location = new System.Drawing.Point(6, 19);
            this.checkBox_useChannelPlayback.Name = "checkBox_useChannelPlayback";
            this.checkBox_useChannelPlayback.Size = new System.Drawing.Size(217, 17);
            this.checkBox_useChannelPlayback.TabIndex = 9;
            this.checkBox_useChannelPlayback.Text = "Enable Single Channel Playback/Output";
            this.checkBox_useChannelPlayback.UseVisualStyleBackColor = true;
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
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(172, 21);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 20);
            this.textBox1.TabIndex = 16;
            this.textBox1.Text = "1";
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // groupBox22
            // 
            this.groupBox22.Location = new System.Drawing.Point(2, -63);
            this.groupBox22.Name = "groupBox22";
            this.groupBox22.Size = new System.Drawing.Size(286, 59);
            this.groupBox22.TabIndex = 15;
            this.groupBox22.TabStop = false;
            this.groupBox22.Text = "Analog Input #1";
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(6, 24);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(158, 13);
            this.label20.TabIndex = 1;
            this.label20.Text = "Amplifier Gain for Extracell. Rec.";
            // 
            // HWpersistWindowComponent
            // 
            this.HWpersistWindowComponent.Form = this;
            this.HWpersistWindowComponent.XMLFilePath = ".";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.panel1.Controls.Add(this.button_cancel);
            this.panel1.Controls.Add(this.button_accept);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(381, 37);
            this.panel1.TabIndex = 17;
            // 
            // panel2
            // 
            this.panel2.AutoScroll = true;
            this.panel2.AutoSize = true;
            this.panel2.Controls.Add(this.tabControl1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 37);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(381, 525);
            this.panel2.TabIndex = 18;
            // 
            // HardwareSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(381, 562);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "HardwareSettings";
            this.Text = "Hardware Settings";
            this.tabPage_misc.ResumeLayout(false);
            this.groupBox11.ResumeLayout(false);
            this.groupBox11.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tabPage_stim.ResumeLayout(false);
            this.tabPage_stim.PerformLayout();
            this.groupBox26.ResumeLayout(false);
            this.groupBox26.PerformLayout();
            this.groupBox12.ResumeLayout(false);
            this.groupBox12.PerformLayout();
            this.groupBox17.ResumeLayout(false);
            this.groupBox17.PerformLayout();
            this.groupBox18.ResumeLayout(false);
            this.groupBox13.ResumeLayout(false);
            this.groupBox13.PerformLayout();
            this.groupBox9.ResumeLayout(false);
            this.groupBox9.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage_input.ResumeLayout(false);
            this.groupBox27.ResumeLayout(false);
            this.groupBox27.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MUArate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_LFPrate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_samplingRate)).EndInit();
            this.groupBox24.ResumeLayout(false);
            this.groupBox24.PerformLayout();
            this.groupBox15.ResumeLayout(false);
            this.groupBox15.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_PreAmpGain)).EndInit();
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
            this.tabPag_Gen.ResumeLayout(false);
            this.groupBox25.ResumeLayout(false);
            this.groupBox25.PerformLayout();
            this.groupBox21.ResumeLayout(false);
            this.groupBox21.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_datSrvBufferSizeSec)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_DACPollingPeriodSec)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_ADCPollingPeriodSec)).EndInit();
            this.tabPage_AuxInput.ResumeLayout(false);
            this.groupBox20.ResumeLayout(false);
            this.groupBox20.PerformLayout();
            this.groupBox19.ResumeLayout(false);
            this.groupBox19.PerformLayout();
            this.groupBox10.ResumeLayout(false);
            this.groupBox10.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private System.Windows.Forms.GroupBox groupBox11;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox comboBox_impedanceDevice;
        private System.Windows.Forms.GroupBox groupBox14;
        private System.Windows.Forms.GroupBox groupBox15;
        private System.Windows.Forms.GroupBox groupBox16;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.GroupBox groupBox17;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox comboBox_SigOutDev;
        private System.Windows.Forms.GroupBox groupBox13;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ComboBox comboBox_IVControlDevice;
        private System.Windows.Forms.ComboBox comboBox_stimInfoDevice;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckBox checkBox_recordStimulationInfo;
        private System.Windows.Forms.GroupBox groupBox12;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.ComboBox comboBox3;
        private System.Windows.Forms.CheckBox checkBox_useChannelPlayback;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox comboBox_singleChannelPlaybackDevice;
        private System.Windows.Forms.CheckBox checkBox_UseAODO;
        private System.Windows.Forms.GroupBox groupBox18;
        private System.Windows.Forms.TabPage tabPage_AuxInput;
        private System.Windows.Forms.GroupBox groupBox10;
        private System.Windows.Forms.GroupBox groupBox19;
        private System.Windows.Forms.ComboBox comboBox_AuxAnalogInputDevice;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.CheckBox checkBox_UseAuxAnalogInput;
        private System.Windows.Forms.ComboBox comboBox_stimInfoDev;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.CheckBox checkBox_RecStimTimes;
        private System.Windows.Forms.GroupBox groupBox20;
        private System.Windows.Forms.ComboBox comboBox_AuxDigInputPort;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.CheckBox checkBox_UseAuxDigitalInput;
        private System.Windows.Forms.ListBox listBox_AuxAnalogInChan;
        private System.Windows.Forms.Label label19;
        private Mowog.PersistWindowComponent HWpersistWindowComponent;
        private System.Windows.Forms.TabPage tabPag_Gen;
        private System.Windows.Forms.GroupBox groupBox21;
        private System.Windows.Forms.GroupBox groupBox23;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.GroupBox groupBox22;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.GroupBox groupBox24;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.CheckBox checkBox_processMUA;
        private System.Windows.Forms.CheckBox checkBox_processLFPs;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.NumericUpDown numericUpDown_ADCPollingPeriodSec;
        private System.Windows.Forms.NumericUpDown numericUpDown_datSrvBufferSizeSec;
        private System.Windows.Forms.NumericUpDown numericUpDown_PreAmpGain;
        private System.Windows.Forms.CheckBox robustStim_checkbox;
        private System.Windows.Forms.GroupBox groupBox25;
        private System.Windows.Forms.CheckBox checkBox_UseStimDataBuffer;
        private System.Windows.Forms.CheckBox checkBox_UseEEGDataBuffer;
        private System.Windows.Forms.CheckBox checkBox_UseLFPDataBuffer;
        private System.Windows.Forms.CheckBox checkBox_UseSALPADataBuffer;
        private System.Windows.Forms.CheckBox checkBox_UseBPDataBuffer;
        private System.Windows.Forms.CheckBox checkBox_UseSpikeDataBuffer;
        private System.Windows.Forms.CheckBox checkBox_UseRawDataBuffer;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.NumericUpDown numericUpDown_DACPollingPeriodSec;
        private System.Windows.Forms.CheckBox checkBox_UseAuxDataBuffer;
        private System.Windows.Forms.CheckBox checkBox_UseDigDataBuffer;
        private System.Windows.Forms.CheckBox checkBox_EnableImpedanceMeasurements;
        private System.Windows.Forms.CheckBox checkBox_UseFloatingRef;
        private System.Windows.Forms.GroupBox groupBox26;
        private System.Windows.Forms.CheckBox checkBox_useBuffloader;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.GroupBox groupBox27;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.ComboBox comboBox_numChannels;
        private System.Windows.Forms.Label label73;
        private System.Windows.Forms.ComboBox comboBox_LFPGain;
        private System.Windows.Forms.Label label38;
        private System.Windows.Forms.ComboBox comboBox_SpikeGain;
        private System.Windows.Forms.Label label_SpikeGain;
        private System.Windows.Forms.Label label_LFPSamplingRate;
        private System.Windows.Forms.Label label_SpikeSamplingRate;
        private System.Windows.Forms.NumericUpDown numericUpDown_MUArate;
        private System.Windows.Forms.NumericUpDown numericUpDown_LFPrate;
        private System.Windows.Forms.NumericUpDown numericUpDown_samplingRate;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
    }
}