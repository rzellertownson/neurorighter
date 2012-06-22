using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR_CL_Examples
{
    class Statistic
    {
        // Protected properties
        protected double avgTimeSec;
        protected double historySec;
        protected double historySamples;
        protected double sampleRateHz;
        protected double baseStat;
        protected List<double> buffer;
        protected List<double> states;

        public Statistic(double avgTimeSec, double sampleRateHz, double historySec, double DACPollingRateHz)
        {
            this.buffer = new List<double>();
            for (int i = 0; i < (int)Math.Ceiling(DACPollingRateHz * historySec); i++)
            {
                buffer.Add(0);
            }
            this.avgTimeSec = avgTimeSec;
            this.sampleRateHz = sampleRateHz;
            this.historySamples = (int)Math.Ceiling(historySec * DACPollingRateHz);
            this.historySec = historySec;
            this.states = new List<double>();
        }

        /// <summary>
        /// Update the windowed statistic
        /// </summary>
        /// <param name="newData">The new data buffer for one channel</param>
        /// <param name="startingSample">Starting sample of the new data</param>
        /// <param name="endSample">Ending sample of the new data</param>
        public virtual void Update(double[] newData, ulong startingSample, ulong endSample) { }

        /// <summary>
        /// Reset current estimates of statistic
        /// </summary>
        public virtual void Reset() { }

        /// <summary>
        /// Reset current estimates of statistic with an initial condition
        /// </summary>
        /// <param name="initialCondition">Intial value for the statistic</param>
        public virtual void Reset(List<double> initialCondition) { }

        /// <summary>
        /// Update the internal states of the statistic estimation algorithm
        /// </summary>
        public virtual void UpdateStates() { }

        /// <summary>
        /// The main base statistic (derived from sub-classes).
        /// </summary>
        public double BaseStat
        {
            get
            {
                return baseStat;
            }
        }

        /// <summary>
        /// The historySec Long Buffer
        /// </summary>
        public List<double> Buffer
        {
            get
            {
                return buffer;
            }
        }

        /// <summary>
        /// Get the internal states of the current statistic
        /// </summary>
        public List<Double> States
        {
            get
            {
                return states;
            }
        }

    }
}
