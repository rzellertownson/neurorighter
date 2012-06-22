using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR_CL_Examples
{
    class RMS : Statistic
    {
        // For user
        private double mean;
        protected double rms;

        // For calculation
        protected double avgCoeff;
        protected double recipRootMean;
        protected double sqmean;

        /// <summary>
        /// The RMS estimate for a given channel
        /// </summary>
        /// <param name="avgTimeSec">Averaging time constant</param>
        /// <param name="sampleRateHz">Sampline rate of the channel</param>
        /// <param name="historySec">Length of the buffer of RMS esimates</param>
        public RMS(double avgTimeSec, double sampleRateHz, double historySec, double DACPollingRateHz)
            : base(avgTimeSec, sampleRateHz, historySec,DACPollingRateHz)
        {
            this.avgCoeff = 1.0 - Math.Exp(-1.0 / (sampleRateHz * avgTimeSec));
            this.recipRootMean = 1.0e-10;      // 1 > initial RecipRootMean > 0
            this.mean = 0.0;
            this.rms = 0.0;
            this.sqmean = 0.0;

            states.Add(mean);
            states.Add(rms);
            states.Add(sqmean);
            states.Add(recipRootMean);
        }

        /// <summary>
        /// Update the RMS estimate
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
                
            }
            buffer.Add(rms);
            baseStat = rms;
            UpdateStates();
        }

        protected double EfficientRMS(double newSample)
        {
            // reciprocal square root method for RMS estimation
            sqmean = EfficientMean(sqmean, newSample*newSample);
            recipRootMean *= 0.5 * (3.0 - (recipRootMean * recipRootMean * sqmean));
            return recipRootMean * sqmean;
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
            this.recipRootMean = 1.0e-10;      // 1 > initial RecipRootMean > 0
            this.mean = 0.0;
            this.rms = 0.0;
            this.sqmean = 0.0;
        }

        /// <summary>
        /// Reset current estimates of statistic with initial condition
        /// </summary>
        public override void Reset(List<double> ic)
        {
            this.mean = ic[0];
            this.rms = ic[1];
            this.sqmean = ic[2];
            this.recipRootMean = ic[3];      // 1 > initial RecipRootMean > 0
        }

        /// <summary>
        ///  Update the internal states of the statistic estimation algorithm
        /// </summary>
        public override void UpdateStates()
        {
            states[0] = mean;
            states[1] = rms;
            states[2] = sqmean;
            states[3] = recipRootMean;
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

        /// <summary>
        /// The current RMS Estimate value over avgTimeSec
        /// </summary>
        public double RMSEstimate
        {
            get
            {
                return rms;
            }
        }

    }
}
