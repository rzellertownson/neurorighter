using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;

namespace NR_CL_Examples
{
    class IISRate : RMS
    {
        // For user
        private double mean;
        double iisRateHz;
        
        // For calculation
        private List<ulong> iisBuffer;
        double IISThresholdK = 7.5;
        double deadTimeSec = 0.02;
        int deadTimeSamples;

        public IISRate(double avgTimeSec, double sampleRateHz, double historySec, double DACPollingRateHz)
            : base(avgTimeSec, sampleRateHz, historySec, DACPollingRateHz)
        {
            this.deadTimeSamples = (int)Math.Ceiling(deadTimeSec * sampleRateHz);
            this.iisBuffer = new List<ulong>();

            states.Add(iisRateHz);
        }

        /// <summary>
        /// Update the IISRate estimate
        /// </summary>
        /// <param name="newData">The new data buffer for one channel</param>
        /// <param name="startingSample">Starting sample of the new data</param>
        /// <param name="endSample">Ending sample of the new data</param>
        public override void Update(double[] newData, ulong startingSample, ulong endSample)
        {

            buffer.RemoveAt(0);
            for (int i = 0; i < newData.Length; i++)
            {
                mean = EfficientMean(mean, newData[i]);
                rms = EfficientRMS(newData[i]);

                if (newData[i] < (-IISThresholdK*rms))
                {
                    iisBuffer.Add(startingSample + (ulong)i);
                    i += deadTimeSamples;
                }

                iisBuffer.RemoveAll(x => x < endSample - historySamples);
            }

            iisRateHz = iisBuffer.Count / historySec;
            buffer.Add(iisRateHz);
            baseStat = iisRateHz;
        }

        ///// <summary>
        ///// Reset current estimates of statistic
        ///// </summary>
        //public override void Reset()
        //{
        //    this.mean = 0.0;
        //}

        ///// <summary>
        ///// Reset current estimates of statistic with initial condition
        ///// </summary>
        //public override void Reset(double ic)
        //{
        //    this.mean = ic;
        //}


        public override void UpdateStates()
        {
            base.UpdateStates();
            states[4] = iisRateHz;
        }

        /// <summary>
        /// Rate of IIS's over HistorySec
        /// </summary>
        public double IISRateHz
        {
            get
            {
                return iisRateHz;
            }

        }

    }
}
