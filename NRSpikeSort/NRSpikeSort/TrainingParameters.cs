using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NRSpikeSort
{

    /// <summary>
    /// Training parameters wrapper class.
    /// </summary>
   public class TrainingParameters
    {
       private string type;
        private int peakSample;
        private double mSecToSecondSample;
        private int sampleFreqHz;

        /// <summary>
        /// Training parameters wrapper class.
        /// </summary>
        public TrainingParameters(string type)
        {
            this.type = type;
        }

        /// <summary>
        /// Training parameters wrapper class.
        /// </summary>
        /// <param name="peakSample">Location of peak sample in waveform snippet.</param>
        public TrainingParameters(string type, int peakSample)
        {
            this.type = type;
            this.peakSample = peakSample;
        }

        /// <summary>
        /// Training parameters wrapper class.
        /// </summary>
        /// <param name="peakSample">Location of peak sample in waveform snippet.</param>
        /// <param name="mSecToSecondSample">Time to second sample, from peak sample, used for double inflection projection</param>
        /// <param name="sampleFreqHz">The sampling frequency that waveforms are sampled at.</param>
        public TrainingParameters(string type, int peakSample, double mSecToSecondSample, int sampleFreqHz)
        {
            this.type = type;
            this.peakSample = peakSample;
            this.mSecToSecondSample = mSecToSecondSample;
            this.sampleFreqHz = sampleFreqHz;
        }

        #region Public Accessors

        public string Type
        {
            get
            {
                return type;
            }
        }

        public int PeakSample
        {
            get
            {
                return peakSample;
            }
        }

        public double MSecToSecondSample
        {
            get
            {
                return mSecToSecondSample;
            }
        }

        public int SampleFreqHz
        {
            get
            {
                return sampleFreqHz;
            }
        }

        #endregion
    }
}
