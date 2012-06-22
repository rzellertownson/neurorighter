using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR_CL_Examples
{
    class LineLength : Statistic
    {
        // For user
        protected double mean;
        protected double lastSample;

        // For calculation
        protected double avgCoeff;

        public LineLength(double avgTimeSec, double sampleRateHz, double historySec, double DACPollingRateHz)
            : base(avgTimeSec, sampleRateHz, historySec,DACPollingRateHz)
        {
            this.avgCoeff = 1.0 - Math.Exp(-1.0 / (sampleRateHz * avgTimeSec));
            this.mean = 0.0;
            this.lastSample = 0;

            states.Add(mean);
            states.Add(lastSample);
        }

        /// <summary>
        /// Update the LineLength estimate
        /// </summary>
        /// <param name="newData">The new data buffer for one channel</param>
        /// <param name="startingSample">Starting sample of the new data</param>
        /// <param name="endSample">Ending sample of the new data</param>
        public override void Update(double[] newData, ulong startingSample, ulong endSample)
        {
            buffer.RemoveAt(0);
            for (int i = 0; i < newData.Length; i++)
            {
                mean = EfficientLL(newData[i]);
            }

            UpdateStates();
            buffer.Add(mean);
            baseStat = mean;
        }

        protected double EfficientLL(double newSample)
        {
            // Recursive line length
            double l = EfficientMean(mean, Math.Abs(newSample - lastSample));
            lastSample = newSample;
            return l;
        }

        protected double EfficientMean(double oldMean, double newSample)
        {
            // recursive average
            return oldMean + avgCoeff * (newSample - oldMean);
        }

        /// <summary>
        /// Reset current estimates of statistic
        /// </summary>
        public override void Reset()
        {
            this.mean = 0.0;
            this.lastSample = 0;
        }

        /// <summary>
        /// Reset current estimates of statistic with initial condition
        /// </summary>
        public override void Reset(List<double> ic)
        {
            this.mean = ic[0];
            this.lastSample = ic[1];
        }

        /// <summary>
        ///  Update the internal states of the statistic estimation algorithm
        /// </summary>
        public override void UpdateStates()
        {
            states[0] = mean;
            states[1] = lastSample;
        }

        /// <summary>
        /// The current signal mean over avgTimeSec
        /// </summary>
        public double Mean
        {
            get
            {
                return mean;
            }
        }
    }
}
