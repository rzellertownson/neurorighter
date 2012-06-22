using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR_CL_Examples
{
    class LFPChannel
    {

        int number;
        double standardTimeSec;
        double tauSec;
        double tauSecStandard;
        double thresholdCoefficient;
        bool triggered;
        string statisticType;
        string compType;
        double updateRateHz;

        // The statistic used to trigger stimulation
        public Statistic stat;
        public Statistic statStandard;

        public LFPChannel(int number, double sampleRateHz, double DACPollingRateHz, string statisticType, double standardTimeSec, double bufferHistorySec, double tauSec, double tauSecStandard, double thresholdCoefficient)
        {
            this.number = number;
            this.standardTimeSec = standardTimeSec;
            this.tauSec = tauSec;
            this.tauSecStandard = tauSecStandard;
            this.thresholdCoefficient = thresholdCoefficient;
            this.statisticType = statisticType;
            this.updateRateHz = DACPollingRateHz;
            switch (statisticType)
            {
                case "RMS":
                    {
                        stat = new RMS(tauSec,sampleRateHz,bufferHistorySec,DACPollingRateHz);
                        statStandard = new RMS(tauSecStandard, sampleRateHz, bufferHistorySec, DACPollingRateHz);
                        compType = "lt";
                        break;
                    }
                case "IISRate":
                    {
                        stat = new IISRate(tauSec, sampleRateHz, bufferHistorySec, DACPollingRateHz);
                        statStandard = new IISRate(tauSecStandard, sampleRateHz, bufferHistorySec, DACPollingRateHz);
                        compType = "lt";
                        break;
                    }
                case "LineLength":
                    {
                        stat = new LineLength(tauSec, sampleRateHz, bufferHistorySec,DACPollingRateHz);
                        statStandard = new LineLength(tauSecStandard, sampleRateHz, bufferHistorySec, DACPollingRateHz);
                        compType = "gt";
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Non-valid LFP Statistic!");
                        break;
                    }
            }
        }

        /// <summary>
        /// Update the windowed statistic
        /// </summary>
        /// <param name="newLFPData">The new data buffer for one channel</param>
        /// <param name="startingSample">Starting sample of the new data</param>
        /// <param name="endSample">Ending sample of the new data</param>
        public void Update(double[] newLFPData, ulong startingSample, ulong endSample)
        {
            stat.Update(newLFPData, startingSample, endSample);
        }

        /// <summary>
        /// Update the standard statistic.
        /// </summary>
        /// <param name="newLFPData">The new data buffer for one channel</param>
        /// <param name="startingSample">Starting sample of the new data</param>
        /// <param name="endSample">Ending sample of the new data</param>
        public void UpdateStandard(double[] newLFPData, ulong startingSample, ulong endSample)
        {
            statStandard.Update(newLFPData, startingSample, endSample);
        }

        /// <summary>
        /// Compare the current value of the selected statistics to the standard value 
        /// </summary>
        public void Compare()
        {
            switch (compType)
            {
                case "gt":
                    {
                        triggered = stat.BaseStat > statStandard.BaseStat * thresholdCoefficient;
                        break;
                    }
                case "lt":
                    {
                        triggered = stat.BaseStat < statStandard.BaseStat * thresholdCoefficient;
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Non-valid comparison type!");
                        break;
                    }
            }

        }

        /// <summary>
        /// Is the selected statistic below/above the standard value?
        /// </summary>
        public bool Triggered
        {
            get
            {
                return triggered;
            }
        }

        /// <summary>
        /// Dynamically set or get the threshold K.
        /// </summary>
        public double ThesholdCoefficient
        {
            get
            {
                return thresholdCoefficient;
            }
            set
            {
                thresholdCoefficient = value;
            }
        }

        /// <summary>
        /// The type of statistic this base class is supporting
        /// </summary>
        public string StatisticType
        {
            get
            {
                return statisticType;
            }
        }

        /// <summary>
        /// How frequently this statistic estimate is updated per second
        /// </summary>
        public double UpdateRateHz
        {
            get
            {
                return updateRateHz;
            }
        }
    }
}
