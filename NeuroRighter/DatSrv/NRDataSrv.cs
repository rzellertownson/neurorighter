// NeuroRighter
// Copyright (c) 2008-2009 John Rolston
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

namespace NeuroRighter.DatSrv
{
    public class NRDataSrv
    {
        /// <summary>
        /// Raw electrode persistant buffer.
        /// </summary>
        public RawDataSrv rawElectrodeSrv;

        /// <summary>
        /// SALPA filtered electrode data persistant buffer.
        /// </summary>
        public RawDataSrv salpaElectrodeSrv;

        /// <summary>
        /// Butterworth filtered electrode data persistant buffer.
        /// </summary>
        public RawDataSrv filteredElectrodeSrv;

        /// <summary>
        /// LFP persistant buffer.
        /// </summary>
        public RawDataSrv lfpSrv;

        /// <summary>
        /// EEG persistant buffer.
        /// </summary>
        public RawDataSrv eegSrv;

        /// <summary>
        /// Aux analog persistant buffer.
        /// </summary>
        public RawDataSrv auxAnalogSrv;

        /// <summary>
        /// Spike snippet persistant buffer.
        /// </summary>
        public EventDataSrv<SpikeEvent> spikeSrv;

        /// <summary>
        /// Digital input persistant buffer.
        /// </summary>
        public EventDataSrv<DigitalPortEvent> auxDigitalSrv;

        /// <summary>
        /// Stimulus server.
        /// </summary>
        public EventDataSrv<ElectricalStimEvent> stimSrv;

        /// <summary>
        /// The ADC polling periods in seconds.
        /// </summary>
        public double ADCPollingPeriodSec;

        /// <summary>
        /// The ADC polling periods in samples.
        /// </summary>
        public int ADCPollingPeriodSamples;

        /// <summary>
        /// NeuroRighter's Persistant Data Server
        /// </summary>
        /// <param name="bufferSizeSeconds"> History that is stored in the Server (seconds)</param>
        /// <param name="salpaAccess"> Using SALPA? </param>
        /// <param name="spikeFiltAccess"> Using spike filters? </param>
        public NRDataSrv(double bufferSizeSeconds, bool salpaAccess,int salpaWidth, bool spikeFiltAccess, int spikeDetlag)
        {
            // Set the polling periods
            ADCPollingPeriodSec = Properties.Settings.Default.ADCPollingPeriodSec;
            ADCPollingPeriodSamples = Convert.ToInt32(Properties.Settings.Default.ADCPollingPeriodSec * Properties.Settings.Default.RawSampleFrequency);

            // Set the spike server lag
            int spikeLag = spikeDetlag;

            // Figure out what servers we need to start up and start them with the correct parameters

            // 1. The raw server is always running
            rawElectrodeSrv = new RawDataSrv(
                Properties.Settings.Default.RawSampleFrequency,
                Convert.ToInt32(Properties.Settings.Default.DefaultNumChannels),
                bufferSizeSeconds,
                ADCPollingPeriodSamples,
                2);

            //2. SALPA data
            if (salpaAccess)
            {
                salpaElectrodeSrv = new RawDataSrv(
                    Properties.Settings.Default.RawSampleFrequency,
                    Convert.ToInt32(Properties.Settings.Default.DefaultNumChannels),
                    bufferSizeSeconds,
                    ADCPollingPeriodSamples,
                    2);

                spikeLag += salpaWidth;
            }

            //3. Spike Filter data
            if (true)
            {
                filteredElectrodeSrv = new RawDataSrv(
                    Properties.Settings.Default.RawSampleFrequency,
                    Convert.ToInt32(Properties.Settings.Default.DefaultNumChannels),
                    bufferSizeSeconds,
                    ADCPollingPeriodSamples,
                    2);
            }

            //4. LFP data
            if (Properties.Settings.Default.UseLFPs)
            {
                lfpSrv = new RawDataSrv(
                    Properties.Settings.Default.RawSampleFrequency,
                    Convert.ToInt32(Properties.Settings.Default.DefaultNumChannels),
                    bufferSizeSeconds,
                    Convert.ToInt32(Properties.Settings.Default.LFPSampleFrequency * 
                    (ADCPollingPeriodSamples / Properties.Settings.Default.RawSampleFrequency)),
                    1);
            }

            //5. EEG data
            if (Properties.Settings.Default.UseEEG)
            {
                eegSrv = new RawDataSrv(
                    Properties.Settings.Default.RawSampleFrequency,
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
                Properties.Settings.Default.RawSampleFrequency,bufferSizeSeconds,
                ADCPollingPeriodSamples,
                2, spikeDetlag);

            //8. Auxiliary Digital data
            if (Properties.Settings.Default.useAuxDigitalInput)
            {
                auxDigitalSrv = new EventDataSrv<DigitalPortEvent>(
                    Properties.Settings.Default.RawSampleFrequency, bufferSizeSeconds,
                    ADCPollingPeriodSamples,
                    1,0);
            }

            //9. Stimulus data
            if (Properties.Settings.Default.RecordStimTimes)
            {
                stimSrv = new EventDataSrv<ElectricalStimEvent>(
                    Properties.Settings.Default.RawSampleFrequency, bufferSizeSeconds,
                    ADCPollingPeriodSamples,
                    2,0);
            }

        }

    }
}
