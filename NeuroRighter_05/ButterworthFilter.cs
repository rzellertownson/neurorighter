// NeuroRighter v0.04
// Copyright (c) 2008 John Rolston
//
// This file is part of NeuroRighter v0.04.
//
// NeuroRighter v0.04 is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter v0.04 is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter v0.04.  If not, see <http://www.gnu.org/licenses/>.
//
// Some filtering code is based on C/C++ code from Exstrom Laboratories, LLC.
// The code from Extrom Laboratories LLC is licensed under the GNU Public License, version 2:
// http://www.gnu.org/copyleft/gpl.html.  

using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroRighter
{
    using rawType = System.Double;

    /// <author>John Rolston (rolston2@gmail.com)</author>
    internal sealed class ButterworthFilter
    {
        private rawType[] dcof;  //Filter's d coefficients
        private rawType[] ccof;     //Filter's c coefficients
        private rawType[] lastInput;
        private rawType[] lastOutput;
        private rawType[] oldData;

        public ButterworthFilter(int order, rawType samplingRate, rawType lowCutF, rawType highCutF, int bufferSize)
        {
            Reset(order, samplingRate, lowCutF, highCutF, bufferSize);
        }

        public void Reset(int order, rawType samplingRate, rawType lowCut, rawType highCut, int bufferSize)
        {
            lock (this)
            {
                dcof = ButterworthFilter.dcof_bwbp(order, (rawType)(lowCut / (samplingRate * 0.5)), (rawType)(highCut / (samplingRate * 0.5)));
                ccof = ButterworthFilter.ccof_bwbp(order, (rawType)(lowCut / (samplingRate * 0.5)), (rawType)(highCut / (samplingRate * 0.5)));
                //for (int i = 0; i < dcof.Length; ++i)
                //    dcof[i] = -dcof[i]; //Since you always subtract dcof
                lastInput = new rawType[ccof.Length - 1];
                lastInput.Initialize();
                lastOutput = new rawType[ccof.Length - 1];
                lastOutput.Initialize();

                oldData = new rawType[bufferSize];
            }
        }

        public void filterData(rawType[] data)
        {
            lock (this)
            {
                rawType temp = 0;
                //for (int i = 0; i < data.Length; ++i)
                //{
                //    temp = data[i];

                //    data[i] *= ccof[0];
                //    for (int j = 1; j < ccof.Length; ++j)
                //        data[i] += ccof[j] * lastInput[j - 1] - dcof[j] * lastOutput[j - 1];

                //    //Update lastInput and lastOutput
                //    for (int j = lastInput.Length - 1; j > 0; --j)
                //    {
                //        lastInput[j] = lastInput[j - 1];
                //        lastOutput[j] = lastOutput[j - 1];
                //    }
                //    lastInput[0] = temp;
                //    lastOutput[0] = data[i];
                //}
                for (int i = 0; i < data.Length; ++i)
                    oldData[i] = data[i];

                for (int i = 0; i < ccof.Length; ++i)
                {
                    temp = data[i];

                    data[i] *= ccof[0];
                    for (int j = 1; j < ccof.Length; ++j)
                        data[i] += ccof[j] * lastInput[j - 1] - dcof[j] * lastOutput[j - 1];

                    //Update lastInput and lastOutput
                    for (int j = lastInput.Length - 1; j > 0; --j)
                    {
                        lastInput[j] = lastInput[j - 1];
                        lastOutput[j] = lastOutput[j - 1];
                    }
                    lastInput[0] = temp;
                    lastOutput[0] = data[i];
                }

                for (int i = ccof.Length; i < data.Length; ++i)
                {
                    data[i] *= ccof[0];
                    for (int j = 1; j < ccof.Length; ++j)
                        data[i] += ccof[j] * oldData[i - j] - dcof[j] * data[i - j];
                }

                for (int i = 0; i < lastOutput.Length; ++i)
                {
                    lastOutput[i] = data[data.Length - 1 - i];
                    lastInput[i] = oldData[oldData.Length - 1 - i];
                }
            }
        }

        /**********************************************************************
          trinomial_mult - multiplies a series of trinomials together and returns
          the coefficients of the resulting polynomial.
          
          The multiplication has the following form:

          (x^2 + b[0]x + c[0])*(x^2 + b[1]x + c[1])*...*(x^2 + b[n-1]x + c[n-1])

          The b[i] and c[i] coefficients are assumed to be complex and are passed
          to the function as a pointers to arrays of doubles of length 2n. The real
          part of the coefficients are stored in the even numbered elements of the
          array and the imaginary parts are stored in the odd numbered elements.

          The resulting polynomial has the following form:
          
          x^2n + a[0]*x^2n-1 + a[1]*x^2n-2 + ... +a[2n-2]*x + a[2n-1]
          
          The a[i] coefficients can in general be complex but should in most cases
          turn out to be real. The a[i] coefficients are returned by the function as
          a pointer to an array of doubles of length 4n. The real and imaginary
          parts are stored, respectively, in the even and odd elements of the array.
          Storage for the array is allocated by the function and should be freed by
          the calling program when no longer needed.
          
          Function arguments:
          
          n  -  The number of trinomials to multiply
          b  -  Pointer to an array of doubles of length 2n.
          c  -  Pointer to an array of doubles of length 2n.
        */

        private static rawType[] trinomial_mult(int n, rawType[] b, rawType[] c)
        {
            int i, j;
            rawType[] a = new rawType[4 * n];

            a[2] = c[0];
            a[3] = c[1];
            a[0] = b[0];
            a[1] = b[1];

            for (i = 1; i < n; ++i)
            {
                a[2 * (2 * i + 1)] += c[2 * i] * a[2 * (2 * i - 1)] - c[2 * i + 1] * a[2 * (2 * i - 1) + 1];
                a[2 * (2 * i + 1) + 1] += c[2 * i] * a[2 * (2 * i - 1) + 1] + c[2 * i + 1] * a[2 * (2 * i - 1)];

                for (j = 2 * i; j > 1; --j)
                {
                    a[2 * j] += b[2 * i] * a[2 * (j - 1)] - b[2 * i + 1] * a[2 * (j - 1) + 1] +
                    c[2 * i] * a[2 * (j - 2)] - c[2 * i + 1] * a[2 * (j - 2) + 1];
                    a[2 * j + 1] += b[2 * i] * a[2 * (j - 1) + 1] + b[2 * i + 1] * a[2 * (j - 1)] +
                    c[2 * i] * a[2 * (j - 2) + 1] + c[2 * i + 1] * a[2 * (j - 2)];
                }

                a[2] += b[2 * i] * a[0] - b[2 * i + 1] * a[1] + c[2 * i];
                a[3] += b[2 * i] * a[1] + b[2 * i + 1] * a[0] + c[2 * i + 1];
                a[0] += b[2 * i];
                a[1] += b[2 * i + 1];
            }
            return (a);
        }


        /**********************************************************************
          dcof_bwbp - calculates the d coefficients for a butterworth bandpass 
          filter. The coefficients are returned as an array of doubles.
        */

        public static rawType[] dcof_bwbp(int n, rawType f1f, rawType f2f)
        {
            int k;            // loop variables
            rawType theta;     // M_PI * (f2f - f1f) / 2.0
            rawType cp;        // cosine of phi
            rawType st;        // sine of theta
            rawType ct;        // cosine of theta
            rawType s2t;       // sine of 2*theta
            rawType c2t;       // cosine 0f 2*theta
            rawType[] rcof;     // z^-2 coefficients
            rawType[] tcof;     // z^-1 coefficients
            rawType[] dcof;     // dk coefficients
            rawType parg;      // pole angle
            rawType sparg;     // sine of pole angle
            rawType cparg;     // cosine of pole angle
            rawType a;         // workspace variables

            cp = (rawType)(Math.Cos(Math.PI * (f2f + f1f) / 2.0));
            theta = (rawType)(Math.PI * (f2f - f1f) / 2.0);
            st = (rawType)(Math.Sin(theta));
            ct = (rawType)(Math.Cos(theta));
            s2t = (rawType)(2.0 * st * ct);        // sine of 2*theta
            c2t = (rawType)(2.0 * ct * ct - 1.0);  // cosine of 2*theta

            rcof = new rawType[2 * n];
            tcof = new rawType[2 * n];

            for (k = 0; k < n; ++k)
            {
                parg = (rawType)(Math.PI * (rawType)(2 * k + 1) / (rawType)(2 * n));
                sparg = (rawType)(Math.Sin(parg));
                cparg = (rawType)(Math.Cos(parg));
                a = (rawType)(1.0 + s2t * sparg);
                rcof[2 * k] = c2t / a;
                rcof[2 * k + 1] = s2t * cparg / a;
                tcof[2 * k] = (rawType) (- 2.0 * cp * (ct + st * sparg) / a);
                tcof[2 * k + 1] = (rawType)(-2.0 * cp * st * cparg / a);
            }

            dcof = trinomial_mult(n, tcof, rcof);

            dcof[1] = dcof[0];
            dcof[0] = (rawType)1.0;
            for (k = 3; k <= 2 * n; ++k)
                dcof[k] = dcof[2 * k - 2];
            return (dcof);
        }


        /**********************************************************************
          ccof_bwlp - calculates the c coefficients for a butterworth lowpass 
          filter. The coefficients are returned as an array of integers.
        */
        private static int[] ccof_bwlp(int n)
        {
            int[] ccof;
            int m;
            int i;

            ccof = new int[n + 1];

            ccof[0] = 1;
            ccof[1] = n;
            m = n / 2;
            for (i = 2; i <= m; ++i)
            {
                ccof[i] = (n - i + 1) * ccof[i - 1] / i;
                ccof[n - i] = ccof[i];
            }
            ccof[n - 1] = n;
            ccof[n] = 1;

            return (ccof);
        }

        /**********************************************************************
          ccof_bwhp - calculates the c coefficients for a butterworth highpass 
          filter. The coefficients are returned as an array of integers.
        */
        private static int[] ccof_bwhp(int n)
        {
            int[] ccof;
            int i;

            ccof = ccof_bwlp(n);

            for (i = 0; i <= n; ++i)
                if (i % 2 != 0) ccof[i] = -ccof[i];

            return (ccof);
        }

        /**********************************************************************
          ccof_bwbp - calculates the c coefficients for a butterworth bandpass 
          filter. The coefficients are returned as an array of integers.
        */
        public static rawType[] ccof_bwbp(int n, rawType lowCut, rawType highCut)
        {
            int[] tcof;
            int[] ccofTemp = new int[2 * n + 1];
            rawType[] ccof = new rawType[2 * n + 1];

            tcof = ccof_bwhp(n);

            for (int i = 0; i < n; ++i)
            {
                ccofTemp[2 * i] = tcof[i];
                ccofTemp[2 * i + 1] = 0;
            }
            ccofTemp[2 * n] = tcof[n];

            rawType scalingFactor = sf_bwbp(n, lowCut, highCut);
            for (int i = 0; i < ccof.Length; ++i)
                ccof[i] = ccofTemp[i] * scalingFactor;

            return (ccof);
        }

        /**********************************************************************
          sf_bwbp - calculates the scaling factor for a butterworth bandpass filter.
          The scaling factor is what the c coefficients must be multiplied by so
          that the filter response has a maximum value of 1.
        */

        private static rawType sf_bwbp(int n, rawType f1f, rawType f2f)
        {
            int k;            // loop variables
            rawType ctt;       // cotangent of theta
            rawType sfr, sfi;  // real and imaginary parts of the scaling factor
            rawType parg;      // pole angle
            rawType sparg;     // sine of pole angle
            rawType cparg;     // cosine of pole angle
            rawType a, b, c;   // workspace variables

            ctt = (rawType)(1.0 / Math.Tan(Math.PI * (f2f - f1f) / 2.0));
            sfr = (rawType)1.0;
            sfi = (rawType)0.0;

            for (k = 0; k < n; ++k)
            {
                parg = (rawType)(Math.PI * (rawType)(2 * k + 1) / (rawType)(2 * n));
                sparg = (rawType)(ctt + Math.Sin(parg));
                cparg = (rawType)(Math.Cos(parg));
                a = (sfr + sfi) * (sparg - cparg);
                b = sfr * sparg;
                c = -sfi * cparg;
                sfr = b - c;
                sfi = a - b - c;
            }

            return ((rawType)(1.0 / sfr));
        }
    }
}
