﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.4211
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace NeuroRighter.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "9.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Dev1")]
        public string CineplexDevice {
            get {
                return ((string)(this["CineplexDevice"]));
            }
            set {
                this["CineplexDevice"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Dev1")]
        public string StimulatorDevice {
            get {
                return ((string)(this["StimulatorDevice"]));
            }
            set {
                this["StimulatorDevice"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool SeparateLFPBoard {
            get {
                return ((bool)(this["SeparateLFPBoard"]));
            }
            set {
                this["SeparateLFPBoard"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Dev1")]
        public string LFPDevice {
            get {
                return ((string)(this["LFPDevice"]));
            }
            set {
                this["LFPDevice"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UseProgRef {
            get {
                return ((bool)(this["UseProgRef"]));
            }
            set {
                this["UseProgRef"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("COM1")]
        public string SerialPortDevice {
            get {
                return ((string)(this["SerialPortDevice"]));
            }
            set {
                this["SerialPortDevice"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Dev1")]
        public string EEGDevice {
            get {
                return ((string)(this["EEGDevice"]));
            }
            set {
                this["EEGDevice"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UseEEG {
            get {
                return ((bool)(this["UseEEG"]));
            }
            set {
                this["UseEEG"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>Dev1</string>\r\n</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection AnalogInDevice {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["AnalogInDevice"]));
            }
            set {
                this["AnalogInDevice"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public short NumAnalogInDevices {
            get {
                return ((short)(this["NumAnalogInDevices"]));
            }
            set {
                this["NumAnalogInDevices"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UseCineplex {
            get {
                return ((bool)(this["UseCineplex"]));
            }
            set {
                this["UseCineplex"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UseStimulator {
            get {
                return ((bool)(this["UseStimulator"]));
            }
            set {
                this["UseStimulator"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("32")]
        public int MUXChannels {
            get {
                return ((int)(this["MUXChannels"]));
            }
            set {
                this["MUXChannels"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("8")]
        public int StimPortBandwidth {
            get {
                return ((int)(this["StimPortBandwidth"]));
            }
            set {
                this["StimPortBandwidth"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool RecordStimTimes {
            get {
                return ((bool)(this["RecordStimTimes"]));
            }
            set {
                this["RecordStimTimes"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Dev1")]
        public string StimInfoDevice {
            get {
                return ((string)(this["StimInfoDevice"]));
            }
            set {
                this["StimInfoDevice"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("invivo")]
        public string ChannelMapping {
            get {
                return ((string)(this["ChannelMapping"]));
            }
            set {
                this["ChannelMapping"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("16")]
        public string DefaultNumChannels {
            get {
                return ((string)(this["DefaultNumChannels"]));
            }
            set {
                this["DefaultNumChannels"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UseLFPs {
            get {
                return ((bool)(this["UseLFPs"]));
            }
            set {
                this["UseLFPs"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int Gain {
            get {
                return ((int)(this["Gain"]));
            }
            set {
                this["Gain"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public float SpikeDisplayGain {
            get {
                return ((float)(this["SpikeDisplayGain"]));
            }
            set {
                this["SpikeDisplayGain"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public float SpkWfmDisplayGain {
            get {
                return ((float)(this["SpkWfmDisplayGain"]));
            }
            set {
                this["SpkWfmDisplayGain"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public float LFPDisplayGain {
            get {
                return ((float)(this["LFPDisplayGain"]));
            }
            set {
                this["LFPDisplayGain"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Dev1")]
        public string ImpedanceDevice {
            get {
                return ((string)(this["ImpedanceDevice"]));
            }
            set {
                this["ImpedanceDevice"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UseSingleChannelPlayback {
            get {
                return ((bool)(this["UseSingleChannelPlayback"]));
            }
            set {
                this["UseSingleChannelPlayback"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Dev1")]
        public string SingleChannelPlaybackDevice {
            get {
                return ((string)(this["SingleChannelPlaybackDevice"]));
            }
            set {
                this["SingleChannelPlaybackDevice"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Dev1")]
        public string StimIvsVDevice {
            get {
                return ((string)(this["StimIvsVDevice"]));
            }
            set {
                this["StimIvsVDevice"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("500")]
        public double SpikesLowCut {
            get {
                return ((double)(this["SpikesLowCut"]));
            }
            set {
                this["SpikesLowCut"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("5000")]
        public double SpikesHighCut {
            get {
                return ((double)(this["SpikesHighCut"]));
            }
            set {
                this["SpikesHighCut"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public double LFPLowCut {
            get {
                return ((double)(this["LFPLowCut"]));
            }
            set {
                this["LFPLowCut"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("500")]
        public double LFPHighCut {
            get {
                return ((double)(this["LFPHighCut"]));
            }
            set {
                this["LFPHighCut"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3")]
        public ushort SpikesReferencingScheme {
            get {
                return ((ushort)(this["SpikesReferencingScheme"]));
            }
            set {
                this["SpikesReferencingScheme"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public ushort SpikesNumPoles {
            get {
                return ((ushort)(this["SpikesNumPoles"]));
            }
            set {
                this["SpikesNumPoles"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public ushort LFPNumPoles {
            get {
                return ((ushort)(this["LFPNumPoles"]));
            }
            set {
                this["LFPNumPoles"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool ProcessMUA {
            get {
                return ((bool)(this["ProcessMUA"]));
            }
            set {
                this["ProcessMUA"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public float EEGDisplayGain {
            get {
                return ((float)(this["EEGDisplayGain"]));
            }
            set {
                this["EEGDisplayGain"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1000")]
        public int EEGSamplingRate {
            get {
                return ((int)(this["EEGSamplingRate"]));
            }
            set {
                this["EEGSamplingRate"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public int EEGGain {
            get {
                return ((int)(this["EEGGain"]));
            }
            set {
                this["EEGGain"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public int EEGNumChannels {
            get {
                return ((int)(this["EEGNumChannels"]));
            }
            set {
                this["EEGNumChannels"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public double PreAmpGain {
            get {
                return ((double)(this["PreAmpGain"]));
            }
            set {
                this["PreAmpGain"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UseSigOut {
            get {
                return ((bool)(this["UseSigOut"]));
            }
            set {
                this["UseSigOut"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Dev1")]
        public string SigOutDev {
            get {
                return ((string)(this["SigOutDev"]));
            }
            set {
                this["SigOutDev"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool recordSpikes {
            get {
                return ((bool)(this["recordSpikes"]));
            }
            set {
                this["recordSpikes"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool recordRaw {
            get {
                return ((bool)(this["recordRaw"]));
            }
            set {
                this["recordRaw"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool recordSpikeFilt {
            get {
                return ((bool)(this["recordSpikeFilt"]));
            }
            set {
                this["recordSpikeFilt"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool recordSalpa {
            get {
                return ((bool)(this["recordSalpa"]));
            }
            set {
                this["recordSalpa"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool recordLFP {
            get {
                return ((bool)(this["recordLFP"]));
            }
            set {
                this["recordLFP"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool recordEEG {
            get {
                return ((bool)(this["recordEEG"]));
            }
            set {
                this["recordEEG"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool recordMUA {
            get {
                return ((bool)(this["recordMUA"]));
            }
            set {
                this["recordMUA"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool recordStim {
            get {
                return ((bool)(this["recordStim"]));
            }
            set {
                this["recordStim"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool recordAuxAnalog {
            get {
                return ((bool)(this["recordAuxAnalog"]));
            }
            set {
                this["recordAuxAnalog"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool recordAuxDigital {
            get {
                return ((bool)(this["recordAuxDigital"]));
            }
            set {
                this["recordAuxDigital"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0, 0")]
        public global::System.Drawing.Point recSetFormLoc {
            get {
                return ((global::System.Drawing.Point)(this["recSetFormLoc"]));
            }
            set {
                this["recSetFormLoc"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string auxAnalogInDev {
            get {
                return ((string)(this["auxAnalogInDev"]));
            }
            set {
                this["auxAnalogInDev"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string auxDigitalInPort {
            get {
                return ((string)(this["auxDigitalInPort"]));
            }
            set {
                this["auxDigitalInPort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>lalala</string>\r\n</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection auxAnalogInChan {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["auxAnalogInChan"]));
            }
            set {
                this["auxAnalogInChan"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool useAuxAnalogInput {
            get {
                return ((bool)(this["useAuxAnalogInput"]));
            }
            set {
                this["useAuxAnalogInput"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool useAuxDigitalInput {
            get {
                return ((bool)(this["useAuxDigitalInput"]));
            }
            set {
                this["useAuxDigitalInput"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("25000")]
        public double RawSampleFrequency {
            get {
                return ((double)(this["RawSampleFrequency"]));
            }
            set {
                this["RawSampleFrequency"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2000")]
        public double LFPSampleFrequency {
            get {
                return ((double)(this["LFPSampleFrequency"]));
            }
            set {
                this["LFPSampleFrequency"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2000")]
        public double MUASampleFrequency {
            get {
                return ((double)(this["MUASampleFrequency"]));
            }
            set {
                this["MUASampleFrequency"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.1")]
        public double ADCPollingPeriodSec {
            get {
                return ((double)(this["ADCPollingPeriodSec"]));
            }
            set {
                this["ADCPollingPeriodSec"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2.5")]
        public double datSrvBufferSizeSec {
            get {
                return ((double)(this["datSrvBufferSizeSec"]));
            }
            set {
                this["datSrvBufferSizeSec"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.1")]
        public double DACPollingPeriodSec {
            get {
                return ((double)(this["DACPollingPeriodSec"]));
            }
            set {
                this["DACPollingPeriodSec"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UseAODO {
            get {
                return ((bool)(this["UseAODO"]));
            }
            set {
                this["UseAODO"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(".")]
        public string CLstimdiretory {
            get {
                return ((string)(this["CLstimdiretory"]));
            }
            set {
                this["CLstimdiretory"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(".")]
        public string OLstimdirectory {
            get {
                return ((string)(this["OLstimdirectory"]));
            }
            set {
                this["OLstimdirectory"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(".")]
        public string savedirectory {
            get {
                return ((string)(this["savedirectory"]));
            }
            set {
                this["savedirectory"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(".")]
        public string saveImpedanceDirectory {
            get {
                return ((string)(this["saveImpedanceDirectory"]));
            }
            set {
                this["saveImpedanceDirectory"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string neurorighterAppDataPath {
            get {
                return ((string)(this["neurorighterAppDataPath"]));
            }
            set {
                this["neurorighterAppDataPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string persistWindowPath {
            get {
                return ((string)(this["persistWindowPath"]));
            }
            set {
                this["persistWindowPath"] = value;
            }
        }
    }
}