using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace simoc.persistantstate
{
    /// <summary>
    /// Persistant variable storage for SIMOC. Jon Newman.
    /// </summary>
    class PersistentSimocVar
    {
        // Output
        private ulong simocStartSample = 0;
        private ulong lastStimSample = 0;
        private ulong nextStimSample = 0;
        private ulong lastDigEventSample = 0;
        private ulong nextDigEventSample = 0;
        private ulong lastAuxEventSample = 0;
        private ulong nextAuxEventSample = 0;
        private ulong numberOfLoopsCompleted = 0;
        private List<ulong> pendingOutputTimes = new List<ulong>();

        // Input
        private ulong lastSampleRead = 0;
        private double lastFilteredObs = 0;
        private double lastErrorValue = 0;
        private double cumulativeAverageObs = 0;
        private double frozenCumulativeAverageObs = 0;
        private double lastTargetValue = 0;
        private int lastTargetIndex = 0;
        private double lastTargetSwitchSec;
        private bool targetOn;

        // PID tuning
        private double ultimatePeriodEstimate;
        private double ultimateGainEstimate;
        private List<double> ultimatePeriodList = new List<double>();
        private List<double> relayCrossingTimeList = new List<double>();
        private List<double> errorUpStateAmpList = new List<double>();
        private List<double> errorDownStateAmpList = new List<double>();
        private List<double> errorSignalAmplitudeList = new List<double>();

        // Generic storage
        private double genericDouble1 = 0;
        private double genericDouble2 = 0;
        private double genericDouble3 = 0;
        private double genericDouble4 = 0;
        private double genericDouble5 = 0;
        private double genericDouble6 = 0;
        private double genericDouble7 = 0;

        private double genericUlong1 = 0;
        private double genericUlong2 = 0;
        private double genericUlong3 = 0;

        private int genericInt1 = 0;
        
        // Other
        private Random randGen1 = new Random();
        private int[] randPerm;

        /// <summary>
        /// This class holds onto variables that need to be stored outside of SIMOC's main loop without 
        /// being overwritten on each iteration.
        /// </summary>
        public PersistentSimocVar()
        {
            ultimatePeriodList = new List<double>();
            errorSignalAmplitudeList = new List<double>();
        }

        /// <summary>
        /// The last sample that an electrical stimulus event occured.
        /// </summary>
        public ulong SimocStartSample
        {
            get
            {
                return simocStartSample;
            }
            set
            {
                simocStartSample = value;
            }
        }

        /// <summary>
        /// The last sample that an electrical stimulus event occured.
        /// </summary>
        public ulong LastStimSample
        {
            get
            {
                return lastStimSample;
            }
            set
            {
                lastStimSample = value;
            }
        }

        /// <summary>
        /// The last sample that an aux digital event occured.
        /// </summary>
        public ulong LastDigEventSample
        {
            get
            {
                return lastDigEventSample;
            }
            set
            {
                lastDigEventSample = value;
            }
        }

        /// <summary>
        /// The last sample that an aux analog event occured.
        /// </summary>
        public ulong LastAuxEventSample
        {
            get
            {
                return lastAuxEventSample;
            }
            set
            {
                lastAuxEventSample = value;
            }
        }

        /// <summary>
        /// The next sample of an electrical stimulation event.
        /// </summary>
        public ulong NextStimSample
        {
            get
            {
                return nextStimSample;
            }
            set
            {
                nextStimSample = value;
            }
        }

        /// <summary>
        /// The next sample of an dig analog event.
        /// </summary>
        public ulong NextDigEventSample
        {
            get
            {
                return nextDigEventSample;
            }
            set
            {
                nextDigEventSample = value;
            }
        }

        /// <summary>
        /// The next sample of an aux analog event.
        /// </summary>
        public ulong NextAuxEventSample
        {
            get
            {
                return nextAuxEventSample;
            }
            set
            {
                nextAuxEventSample = value;
            }
        }

        /// <summary>
        /// The next last input sample retrieved.
        /// </summary>
        public ulong LastSampleRead
        {
            get
            {
                return lastSampleRead;
            }
            set
            {
                lastSampleRead = value;
            }
        }

        /// <summary>
        /// Number of times simoc has spun
        /// </summary>
        public ulong NumberOfLoopsCompleted
        {
            get
            {
                return numberOfLoopsCompleted;
            }
            set
            {
                numberOfLoopsCompleted = value;
            }
        }

        /// <summary>
        /// Number of times simoc has spun
        /// </summary>
        public List<ulong> PendingOutputTimes
        {
            get
            {
                return pendingOutputTimes;
            }
            set
            {
                pendingOutputTimes = value;
            }
        }

        /// <summary>
        /// The next last input sample retrieved.
        /// </summary>
        public double LastFilteredObs
        {
            get
            {
                return lastFilteredObs;
            }
            set
            {
                lastFilteredObs = value;
            }
        }

        /// <summary>
        /// Last Error value
        /// </summary>
        public double LastErrorValue
        {
            get
            {
                return lastErrorValue;
            }
            set
            {
                lastErrorValue = value;
            }
        }

        /// <summary>
        /// Stores the average of the observable over long time periods
        /// </summary>
        public double CumulativeAverageObs
        {
            get
            {
                return cumulativeAverageObs;
            }
            set
            {
                cumulativeAverageObs = value;
            }
        }

        /// <summary>
        /// Used to store a certain value of the cummulative average
        /// </summary>
        public double FrozenCumulativeAverageObs
        {
            get
            {
                return frozenCumulativeAverageObs;
            }
            set
            {
                frozenCumulativeAverageObs = value;
            }
        }

        /// <summary>
        /// Stores the last target index for custum, multistep target functions
        /// </summary>
        public int LastTargetIndex
        {
            get
            {
                return lastTargetIndex;
            }
            set
            {
                lastTargetIndex = value;
            }
        }

        /// <summary>
        /// The absolute sample time that we last switched the target function
        /// </summary>
        public double LastTargetSwitchedSec
        {
            get
            {
                return lastTargetSwitchSec;
            }
            set
            {
                lastTargetSwitchSec = value;
            }
        }

        /// <summary>
        /// Are we tracking a target?
        /// </summary>
        public bool TargetOn
        {
            get
            {
                return targetOn;
            }
            set
            {
                targetOn = value;
            }
        }

        /// <summary>
        /// Last Target value
        /// </summary>
        public double LastTargetValue
        {
            get
            {
                return lastTargetValue;
            }
            set
            {
                lastTargetValue = value;
            }
        }


        /// <summary>
        /// Ultimate period estimate
        /// </summary>
        public double UltimatePeriodEstimate
        {
            get
            {
                return ultimatePeriodEstimate;
            }
            set
            {
                ultimatePeriodEstimate = value;
            }
        }

        /// <summary>
        /// Ultimate period estimate
        /// </summary>
        public double UltimateGainEstimate
        {
            get
            {
                return ultimateGainEstimate;
            }
            set
            {
                ultimateGainEstimate = value;
            }
        }

        /// <summary>
        /// A List of the ultimate periods used to for frequency-response based tuning
        /// </summary>
        public List<double> UltimatePeriodList
        {
            get
            {
                return ultimatePeriodList;
            }
            set
            {
                ultimatePeriodList = value;
            }
        }

        /// <summary>
        /// A List of the times that the relay signal turned on
        /// </summary>
        public List<double> RelayCrossingTimeList
        {
            get
            {
                return relayCrossingTimeList;
            }
            set
            {
                relayCrossingTimeList = value;
            }
        }

        /// <summary>
        /// Stores the last target index for custum, multistep target functions
        /// </summary>
        public List<double> ErrorSignalAmplitudeList
        {
            get
            {
                return errorSignalAmplitudeList;
            }
            set
            {
                errorSignalAmplitudeList = value;
            }
        }

        /// <summary>
        /// Stores the error values when the error is above delta in relay FB
        /// </summary>
        public List<double> ErrorUpStateAmpList
        {
            get
            {
                return errorUpStateAmpList;
            }
            set
            {
                errorUpStateAmpList = value;
            }
        }

        /// <summary>
        /// Stores the error values when the error is above delta in relay FB
        /// </summary>
        public List<double> ErrorDownStateAmpList
        {
            get
            {
                return errorDownStateAmpList;
            }
            set
            {
                errorDownStateAmpList = value;
            }
        }

        /// <summary>
        /// A generic double.
        /// </summary>
        public double GenericDouble1
        {
            get
            {
                return genericDouble1;
            }
            set
            {
                genericDouble1 = value;
            }
        }

        /// <summary>
        /// A generic double.
        /// </summary>
        public double GenericDouble2
        {
            get
            {
                return genericDouble2;
            }
            set
            {
                genericDouble2 = value;
            }
        }

        /// <summary>
        /// A generic double.
        /// </summary>
        public double GenericDouble3
        {
            get
            {
                return genericDouble3;
            }
            set
            {
                genericDouble3 = value;
            }
        }

        /// <summary>
        /// A generic double.
        /// </summary>
        public double GenericDouble4
        {
            get
            {
                return genericDouble4;
            }
            set
            {
                genericDouble4 = value;
            }
        }

        /// <summary>
        /// A generic double.
        /// </summary>
        public double GenericDouble5
        {
            get
            {
                return genericDouble5;
            }
            set
            {
                genericDouble5 = value;
            }
        }

        /// <summary>
        /// A generic double.
        /// </summary>
        public double GenericDouble6
        {
            get
            {
                return genericDouble6;
            }
            set
            {
                genericDouble6 = value;
            }
        }

        /// <summary>
        /// A generic double.
        /// </summary>
        public double GenericDouble7
        {
            get
            {
                return genericDouble7;
            }
            set
            {
                genericDouble7 = value;
            }
        }

        /// <summary>
        /// A generic ulong.
        /// </summary>
        public double GenericUlong1
        {
            get
            {
                return genericUlong1;
            }
            set
            {
                genericUlong1 = value;
            }
        }

        /// <summary>
        /// A generic ulong.
        /// </summary>
        public double GenericUlong2
        {
            get
            {
                return genericUlong2;
            }
            set
            {
                genericUlong2 = value;
            }
        }

        /// <summary>
        /// A generic ulong.
        /// </summary>
        public double GenericUlong3
        {
            get
            {
                return genericUlong3;
            }
            set
            {
                genericUlong3 = value;
            }
        }

        /// <summary>
        /// A generic int.
        /// </summary>
        public int GenericInt1
        {
            get
            {
                return genericInt1;
            }
            set
            {
                genericInt1 = value;
            }
        }

        /// <summary>
        /// The persistant randome number generator.
        /// </summary>
        public Random RandGen1
        {
            get
            {
                return randGen1;
            }
        }

        /// <summary>
        /// Random permutation of 1:N
        /// </summary>
        public int[] RandPerm
        {
            get
            {
                return randPerm;
            }
        }

        // Methods

        /// <summary>
        /// Update running avareage
        /// </summary>
        /// <param name="currentObservation"></param>
        internal void UpdateRunningObsAverage(double currentObservation)
        {
            cumulativeAverageObs = (currentObservation + ((double)numberOfLoopsCompleted - 1.0) * cumulativeAverageObs) / (double)numberOfLoopsCompleted;
        }

        /// <summary>
        /// Reset the running average and number of loops averaged over
        /// </summary>
        internal void ResetRunningObsAverage()
        {
            numberOfLoopsCompleted = 0;
            cumulativeAverageObs = 0;
        }

        /// <summary>
        /// Generate random permutation of 1:N;
        /// </summary>
        /// <param name="N">Ordered array is 1:N</param>
        internal void GenerateRandPerm(int N)
        {

            int[] tmp = new int[N];
            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] = i;
            }

            randPerm = tmp.OrderBy(x => randGen1.Next()).ToArray();
        }


        

    }
}
