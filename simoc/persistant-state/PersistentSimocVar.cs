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

        // Input
        private ulong lastSampleRead = 0;

        // Generic storage
        private double genericDouble1 = 0;
        private double genericDouble2 = 0;
        private double genericDouble3 = 0;

        private double genericUlong1 = 0;
        private double genericUlong2 = 0;
        private double genericUlong3 = 0;

        /// <summary>
        /// This class holds onto variables that need to be stored outside of SIMOC's main loop without 
        /// being overwritten on each iteration.
        /// </summary>
        public PersistentSimocVar()
        {

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

    }
}
