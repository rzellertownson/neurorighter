using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using simoc.UI;
using simoc.srv;
using simoc.persistantstate;
using NeuroRighter.StimSrv;
using NeuroRighter.DataTypes;
using System.IO.Ports;

namespace simoc.filt2out
{
    class Filt2IBangBangArduino : Filt2Out
    {
        double currentFilteredValue;
        double currentTargetIntenal;
        double lastErrorIntenal;
        SerialPort serialPort;

        public Filt2IBangBangArduino(ref NRStimSrv stimSrv, ControlPanel cp, SerialPort sp)
            : base(ref stimSrv, cp)
        {
            numberOutStreams = 4; // P and I streams
            this.serialPort = sp;
        }

        internal override void CalculateError(ref double currentError, double currentTarget, double currentFilt)
        {
            currentFilteredValue = currentFilt;
            base.CalculateError(ref currentError, currentTarget, currentFilt);
            if (currentTarget != 0)
            {
                lastErrorIntenal = currentError;
                currentError = (currentTarget - currentFilt);  // currentTarget;
            }
            else
            {
                lastErrorIntenal = currentError;
                currentError = 0;
            }
            currentErrorIntenal = currentError;
            currentTargetIntenal = currentTarget;
        }


        internal override void SendFeedBack(PersistentSimocVar simocVariableStorage)
        {
            base.SendFeedBack(simocVariableStorage);

            simocVariableStorage.LastErrorValue = lastErrorIntenal;

            // Generate output frequency
            if (currentTargetIntenal != 0)
            {
                // Derivative Approx
                simocVariableStorage.GenericDouble4 = 0;

                // Tustin's Integral approximation
                simocVariableStorage.GenericDouble3 += stimSrv.DACPollingPeriodSec * currentErrorIntenal;

                // Proportional Term
                simocVariableStorage.GenericDouble2 = 0;

                // I feedback signal
                simocVariableStorage.GenericDouble1 = simocVariableStorage.GenericDouble3;
            }
            else
            {
                simocVariableStorage.GenericDouble4 = 0;
                simocVariableStorage.GenericDouble3 = 0;
                simocVariableStorage.GenericDouble2 = 0;
                simocVariableStorage.GenericDouble1 = 0;
            }

            // Set bang-bange control signal
            if (simocVariableStorage.GenericDouble1 <= 0)
                simocVariableStorage.GenericDouble1 = 0;
            else
                simocVariableStorage.GenericDouble1 = 1;

            // set the currentFeedback array
            currentFeedbackSignals = new double[numberOutStreams];

            // Put P,I and D error components in the rest of the currentFeedBack array
            currentFeedbackSignals[0] = simocVariableStorage.GenericDouble1;
            currentFeedbackSignals[1] = simocVariableStorage.GenericDouble2;
            currentFeedbackSignals[2] = simocVariableStorage.GenericDouble3;
            currentFeedbackSignals[3] = simocVariableStorage.GenericDouble4;


            // Send a V_ctl = simocVariableStorage.GenericDouble1 volt pulse to channel 0 for c2 milliseconds.
            if (simocVariableStorage.GenericDouble1 == 1)
            {
                // Use the serial port to send a command to the Arduino 
                serialPort.Write(new byte[] { 1 }, 0, 1);
            }


        }
    }
}
