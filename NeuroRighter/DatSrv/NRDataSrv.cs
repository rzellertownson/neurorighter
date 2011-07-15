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
        // Raw data buffers
        public RawDataSrv rawElectrodeSrv;
        public RawDataSrv salpaElectrodeSrv;
        public RawDataSrv filteredElectrodeSrv;
        public RawDataSrv lfpSrv;
        public RawDataSrv eegSrv;
        public RawDataSrv auxAnalogSrv;

        // Event Data buffers
        public EventDataSrv<SpikeEvent> spikeSrv;
        public EventDataSrv<DigitalPortEvent> auxDigitalSrv;
        public EventDataSrv<ElectricalStimEvent> stimSrv;

        // DAQ polling period
        public int ADCPollingPeriodSec;
        public int ADCPollingPeriodSamples;

        /// <summary>
        /// NeuroRighter's Persistant Data Server
        /// </summary>
        /// <param name="bufferSizeSeconds"> History that is stored in the Server (seconds)</param>
        /// <param name="salpaAccess"> Using SALPA? </param>
        /// <param name="spikeFiltAccess"> Using spike filters? </param>
        public NRDataSrv(double bufferSizeSeconds, bool salpaAccess, bool spikeFiltAccess)
        {
            // Set the polling periods
            ADCPollingPeriodSec = Convert.ToInt32(Properties.Settings.Default.ADCPollingPeriodSec);
            ADCPollingPeriodSamples = Convert.ToInt32(Properties.Settings.Default.ADCPollingPeriodSec * Properties.Settings.Default.RawSampleFrequency);

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
            }

            //3. Spike Filter data
            if (spikeFiltAccess)
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
                    ADCPollingPeriodSamples,
                    1);
            }

            //5. EEG data
            if (Properties.Settings.Default.UseEEG)
            {
                eegSrv = new RawDataSrv(
                    Properties.Settings.Default.RawSampleFrequency,
                    Convert.ToInt32(Properties.Settings.Default.DefaultNumChannels),
                    bufferSizeSeconds,
                    ADCPollingPeriodSamples,
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
                2);

            //8. Auxiliary Digital data
            if (Properties.Settings.Default.useAuxDigitalInput)
            {
                auxDigitalSrv = new EventDataSrv<DigitalPortEvent>(
                    Properties.Settings.Default.RawSampleFrequency, bufferSizeSeconds,
                    ADCPollingPeriodSamples,
                    1);
            }

            //9. Stimulus data
            if (Properties.Settings.Default.RecordStimTimes)
            {
                stimSrv = new EventDataSrv<ElectricalStimEvent>(
                    Properties.Settings.Default.RawSampleFrequency, bufferSizeSeconds,
                    ADCPollingPeriodSamples,
                    2
                );
            }

        }

    }
}
