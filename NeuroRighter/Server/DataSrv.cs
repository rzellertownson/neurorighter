// NeuroRighter
// Copyright (c) 2008-2012 Potter Lab
//
// This file is part of NeuroRighter.
//
// NeuroRighter is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;

namespace NeuroRighter.Server
{
    /// <summary>
    /// NeuoroRighter data server collection. This class contains all of NeuroRighter's data servers as properties.
    /// </summary>
    public class DataSrv
    {
        /// <summary>
        /// Raw electrode persistant buffer.
        /// </summary>
        private RawDataSrv rawElectrodeSrv;

        /// <summary>
        /// SALPA filtered electrode data persistant buffer.
        /// </summary>
        private RawDataSrv salpaElectrodeSrv;

        /// <summary>
        /// Butterworth filtered electrode data persistant buffer.
        /// </summary>
        private RawDataSrv spikeBandSrv;

        /// <summary>
        /// LFP persistant buffer.
        /// </summary>
        private RawDataSrv lfpSrv;

        /// <summary>
        /// EEG persistant buffer.
        /// </summary>
        private RawDataSrv eegSrv;

        /// <summary>
        /// Aux analog persistant buffer.
        /// </summary>
        private RawDataSrv auxAnalogSrv;

        /// <summary>
        /// Spike snippet persistant buffer.
        /// </summary>
        private EventDataSrv<SpikeEvent> spikeSrv;

        /// <summary>
        /// Digital input persistant buffer.
        /// </summary>
        private EventDataSrv<DigitalPortEvent> auxDigitalSrv;

        /// <summary>
        /// Stimulus server.
        /// </summary>
        private EventDataSrv<ElectricalStimEvent> stimSrv;

        /// <summary>
        /// The ADC polling periods in seconds.
        /// </summary>
        private double aDCPollingPeriodSec;

        /// <summary>
        /// The ADC polling periods in samples.
        /// </summary>
        private int aDCPollingPeriodSamples;

        /// <summary>
        /// The number of times the ADC's have been polled since the start of aquisition
        /// </summary>
        private int numberOfPollsCompleted;

        /// <summary>
        /// NeuroRighter's Persistant Data Server
        /// </summary>
        /// <param name="bufferSizeSeconds"> History that is stored in the Server (seconds)</param>
        /// <param name="salpaAccess"> Using SALPA? </param>
        /// <param name="salpaWidth">half width of the salpa filter</param>
        /// <param name="spikeFiltAccess"> Using spike filters? </param>
        /// <param name="spikeDetlag">lag from the spike detection settings</param>
        internal DataSrv(double bufferSizeSeconds, bool salpaAccess, int salpaWidth, bool spikeFiltAccess, int spikeDetlag)
        {
            // Set the polling periods
            aDCPollingPeriodSec = Properties.Settings.Default.ADCPollingPeriodSec;
            aDCPollingPeriodSamples = Convert.ToInt32(Properties.Settings.Default.ADCPollingPeriodSec * Properties.Settings.Default.RawSampleFrequency);

            // Set the spike server lag
            int spikeLag = spikeDetlag;

            // Figure out what servers we need to start up and start them with the correct parameters

            // 1. The raw server is always running
            rawElectrodeSrv = new RawDataSrv(
                Properties.Settings.Default.RawSampleFrequency,
                Convert.ToInt32(Properties.Settings.Default.NumChannels),
                bufferSizeSeconds,
                ADCPollingPeriodSamples,
                Properties.Settings.Default.numSpikeTasks);

            //2. SALPA data
            if (salpaAccess)
            {
                salpaElectrodeSrv = new RawDataSrv(
                    Properties.Settings.Default.RawSampleFrequency,
                    Convert.ToInt32(Properties.Settings.Default.NumChannels),
                    bufferSizeSeconds,
                    ADCPollingPeriodSamples,
                    Properties.Settings.Default.numSpikeTasks);

                spikeLag += 2*salpaWidth;
            }

            //3. Spike Filter data
            if (true)
            {
                spikeBandSrv = new RawDataSrv(
                    Properties.Settings.Default.RawSampleFrequency,
                    Convert.ToInt32(Properties.Settings.Default.NumChannels),
                    bufferSizeSeconds,
                    ADCPollingPeriodSamples,
                    Properties.Settings.Default.numSpikeTasks);
            }

            //4. LFP data
            if (Properties.Settings.Default.UseLFPs)
            {
                lfpSrv = new RawDataSrv(
                    Properties.Settings.Default.LFPSampleFrequency,
                    Convert.ToInt32(Properties.Settings.Default.NumChannels),
                    bufferSizeSeconds,
                    Convert.ToInt32(Properties.Settings.Default.LFPSampleFrequency *
                    (ADCPollingPeriodSamples / Properties.Settings.Default.RawSampleFrequency)),
                    Properties.Settings.Default.numLFPTasks);
            }

            //5. EEG data
            if (Properties.Settings.Default.UseEEG)
            {
                eegSrv = new RawDataSrv(
                    Properties.Settings.Default.EEGSamplingRate,
                    Properties.Settings.Default.EEGNumChannels,
                    bufferSizeSeconds,
                    Convert.ToInt32(Properties.Settings.Default.EEGSamplingRate *
                    (ADCPollingPeriodSamples / Properties.Settings.Default.RawSampleFrequency)),
                    1);
            }

            //6. Auxiliary analog data
            if (Properties.Settings.Default.useAuxAnalogInput)
            {
                auxAnalogSrv = new RawDataSrv(
                    Properties.Settings.Default.RawSampleFrequency,
                    Properties.Settings.Default.auxAnalogInChan.Count,
                    bufferSizeSeconds,
                    ADCPollingPeriodSamples,
                    1);
            }

            //7. Spike data, always available
            spikeSrv = new EventDataSrv<SpikeEvent>(
                Properties.Settings.Default.RawSampleFrequency, bufferSizeSeconds,
                ADCPollingPeriodSamples,
                Properties.Settings.Default.numSpikeTasks, 
                spikeLag, 
                Convert.ToInt32(Properties.Settings.Default.NumChannels));

            //8. Auxiliary Digital data
            if (Properties.Settings.Default.useAuxDigitalInput)
            {
                auxDigitalSrv = new EventDataSrv<DigitalPortEvent>(
                    Properties.Settings.Default.RawSampleFrequency, bufferSizeSeconds,
                    ADCPollingPeriodSamples,
                    1, 0, 32);
            }

            //9. Stimulus data
            if (Properties.Settings.Default.RecordStimTimes)
            {
                stimSrv = new EventDataSrv<ElectricalStimEvent>(
                    Properties.Settings.Default.RawSampleFrequency, bufferSizeSeconds,
                    ADCPollingPeriodSamples,
                   1, 0, 0);
            }

        }

        /// <summary>
        /// If there is a spike sorter, us this  to set the number of units that have been detected.
        /// </summary>
        /// <param name="numberOfUnits">The Number of units found by the spike sorter.</param>
        internal void SetNumberOfUnits(int numberOfUnits, Dictionary<int, int> unit2Channel)
        {
            if (spikeSrv != null)
            {
                spikeSrv.SetNumberOfUnits(numberOfUnits);
                spikeSrv.SetUnit2ChannelMap(unit2Channel);
            }

        }

        # region public accessors

        /// <summary>
        /// Raw electrode persistant buffer.
        /// </summary>
        public RawDataSrv RawElectrodeSrv
        {
            get
            {
                return rawElectrodeSrv;
            }
        }

        /// <summary>
        /// SALPA filtered electrode data persistant buffer.
        /// </summary>
        public RawDataSrv SalpaElectrodeSrv
        {
            get
            {
                return salpaElectrodeSrv;
            }
        }

        /// <summary>
        /// Butterworth filtered electrode data persistant buffer.
        /// </summary>
        public RawDataSrv SpikeBandSrv
        {
            get
            {
                return spikeBandSrv;
            }
        }

        /// <summary>
        /// LFP persistant buffer.
        /// </summary>
        public RawDataSrv LFPSrv
        {
            get
            {
                return lfpSrv;
            }
        }

        /// <summary>
        /// EEG persistant buffer.
        /// </summary>
        public RawDataSrv EEGSrv
        {
            get
            {
                return eegSrv;
            }
        }

        /// <summary>
        /// Aux analog persistant buffer.
        /// </summary>
        public RawDataSrv AuxAnalogSrv
        {
            get
            {
                return auxAnalogSrv;
            }
        }

        /// <summary>
        /// Spike snippet persistant buffer.
        /// </summary>
        public EventDataSrv<SpikeEvent> SpikeSrv
        {
            get
            {
                return spikeSrv;
            }
        }

        /// <summary>
        /// Digital input persistant buffer.
        /// </summary>
        public EventDataSrv<DigitalPortEvent> AuxDigitalSrv
        {
            get
            {
                return auxDigitalSrv;
            }
        }

        /// <summary>
        /// Stimulus server.
        /// </summary>
        public EventDataSrv<ElectricalStimEvent> StimSrv
        {
            get
            {
                return stimSrv;
            }
        }

        /// <summary>
        /// The ADC polling periods in seconds.
        /// </summary>
        public double ADCPollingPeriodSec
        {
            get
            {
                return aDCPollingPeriodSec;
            }
        }

        /// <summary>
        /// The ADC polling periods in samples.
        /// </summary>
        public int ADCPollingPeriodSamples
        {
            get
            {
                return aDCPollingPeriodSamples;
            }
        }

        /// <summary>
        /// The number of times the ADC's have been polled since the start of aquisition
        /// </summary>
        public int NumberOfPollsCompleted
        {
            get
            {
                return numberOfPollsCompleted;
            }

            internal set
            {
                numberOfPollsCompleted = value;
            }
        }

        # endregion


    }
}
