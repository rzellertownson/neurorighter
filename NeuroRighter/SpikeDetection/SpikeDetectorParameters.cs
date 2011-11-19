using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NRSpikeSort;

namespace NeuroRighter.SpikeDetection
{
    /// <summary>
    /// Wrapper for spike detector settings.
    /// </summary>
    [Serializable]
    public class SpikeDetectorParameters
    {

        // Detector Parameters
        private decimal numPre; // num smaples to save pre-spike
        private decimal numPost; // num samples to save post-spike
        private decimal spikeDetectionLag; // number of samples that spike detector will cause buffers to lag
        private int detectorType;
        private int noiseAlgType;
        private decimal minSpikeWidth;
        private decimal maxSpikeWidth;
        private decimal minSpikeSlope;
        private decimal threshold;
        private decimal deadTime;
        private decimal maxSpikeAmp;
        private SpikeSorter ss;


        public SpikeDetectorParameters() { }

        public SpikeSorter SS
        {
            get
            {
                return ss;
            }
            set
            {
                ss = value;
            }
        }

        public decimal Threshold
        {
            get
            {
                return threshold;
            }
            set
            {
                threshold = value;
            }
        }

        public decimal DeadTime
        {
            get
            {
                return deadTime;
            }
            set
            {
                deadTime = value;
            }
        }

        public decimal MaxSpikeAmp
        {
            get
            {
                return maxSpikeAmp;
            }
            set
            {
                maxSpikeAmp = value;
            }
        }

        public decimal MinSpikeWidth
        {
            get
            {
                return minSpikeWidth;
            }
            set
            {
                minSpikeWidth = value;
            }
        }

        public decimal MaxSpikeWidth
        {
            get
            {
                return maxSpikeWidth;
            }
            set
            {
                maxSpikeWidth = value;
            }
        }

        public decimal MinSpikeSlope
        {
            get
            {
                return minSpikeSlope;
            }
            set
            {
                minSpikeSlope = value;
            }
        }

        public decimal NumPre
        {
            get
            {
                return numPre;
            }
            set
            {
                numPre = value;
            }
        }

        public decimal NumPost
        {
            get
            {
                return numPost;
            }
            set
            {
                numPost = value;
            }
        }

        public decimal SpikeDetectionLag
        {
            get
            {
                return spikeDetectionLag;
            }
            set
            {
                spikeDetectionLag = value;
            }
        }

        public int DetectorType
        {
            get
            {
                return detectorType;
            }
            set
            {
                detectorType = value;
            }
        }

        public int NoiseAlgType
        {
            get
            {
                return noiseAlgType;
            }
            set
            {
                noiseAlgType = value;
            }
        }

        public SpikeDetectorParameters(SerializationInfo info, StreamingContext ctxt)
        {
            this.ss = (SpikeSorter)info.GetValue("ss", typeof(SpikeSorter));
            this.threshold = (decimal)info.GetValue("threshold", typeof(decimal));
            this.deadTime = (decimal)info.GetValue("deadTime", typeof(decimal));
            this.maxSpikeAmp = (decimal)info.GetValue("maxSpikeAmp", typeof(decimal));
            this.minSpikeSlope = (decimal)info.GetValue("minSpikeSlope", typeof(decimal));
            this.minSpikeWidth = (decimal)info.GetValue("minSpikeWidth", typeof(decimal));
            this.maxSpikeWidth = (decimal)info.GetValue("maxSpikeWidth", typeof(decimal));
            this.numPre = (decimal)info.GetValue("numPre", typeof(decimal));
            this.numPost = (decimal)info.GetValue("numPost", typeof(decimal));
            this.spikeDetectionLag = (decimal)info.GetValue("spikeDetectionLag", typeof(decimal));
            this.detectorType = (int)info.GetValue("detectorType", typeof(int));
            this.noiseAlgType = (int)info.GetValue("noiseAlgType", typeof(int));

        }

        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("numPre", this.numPre);
            info.AddValue("numPost", this.numPost);
            info.AddValue("spikeDetectionLag", this.spikeDetectionLag);
            info.AddValue("detectorType", this.detectorType);
            info.AddValue("ss", this.ss);
            info.AddValue("threshold", this.threshold);
            info.AddValue("deadTime", this.deadTime);
            info.AddValue("maxSpikeAmp", this.maxSpikeAmp);
            info.AddValue("minSpikeSlope", this.minSpikeSlope);
            info.AddValue("minSpikeWidth", this.minSpikeWidth);
            info.AddValue("maxSpikeWidth", this.maxSpikeWidth);
            info.AddValue("noiseAlgType", this.noiseAlgType);
        }
    }
}
