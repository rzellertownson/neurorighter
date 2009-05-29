// NeuroRighter
// Copyright (c) 2008-2009 John Rolston
//
// This file is part of NeuroRighter.
//
// NeuroRighter is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter.  If not, see <http://www.gnu.org/licenses/>.

//#define DEBUG
#define USE_HIGHPASS
//#define DEBUG_JEFF

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NeuroRighter
{
    using rawType = System.Double;

    /// <author>John Rolston (rolston2@gmail.com)</author>
    sealed class SALPA2
    {
        /*call in matlab would be: [filtData, myfit] = SALPAmex(V, numElectrodes, rails, NPrePeg, salpathresh)*/

        /*last edited on July 17, 2008 */
        private int N;  //Half width of filter
        private rawType[][] oldData;
        private rawType[][] oldDataPrev;
        private rawType[,] SInv;    //S' matrix
        private rawType[][] W;
        private rawType[][] A;
        private rawType[] rails;
        private int prePeg;
        private int postPeg; //Num pts. to drop after a peg
        private int postPegZero; //Num pts. to set to zero after a successful fit has been achieved (helps control ringing)
        private bool[] PEGGING;
        private bool[] FULL_LOOK;
        private bool[] DEPEGGING;
        private bool[] FIT_UNFINISHED;
        private bool[] PEGGING_UNFINISHED;
        private rawType delta;
        private int[] endIdx;
        private int[] startPeg, stopPeg; //Indices at which the first 0'ing and last 0'ing will occur (i.e., inclusive bounds)
        private int numSamples; //Buffer length

        private static int PRE;
        private static int POST;

#if (USE_HIGHPASS)
        #region Variables for High-pass Filter
        private double[] lastOutputs;
        private double[] lastInputs;
        private const double ALPHA = 0.16 / (0.16 + (1 / 25E3));  //RC = 1.5 yields a 0.1 Hz high -3 dB point.  25E3 is the typical sampling rate
        #endregion
#endif
#if (DEBUG_JEFF)
        private StreamWriter debugLogFile;
#endif

        public SALPA2()
            : this(75, 5, 2, -5, 5, 5, 16, 5, 250)
        {
            //Default halfWidth of 75
        }

        public SALPA2(int halfWidth, int prePeg, int postPeg, int postPegZero, rawType railLow, rawType railHigh, int numElectrodes, rawType delta, int bufferLength)
        {
            //Compute S' (inverse matrix of S)
            N = halfWidth;
            double[] T = new double[7];
            double[] n = new double[2 * N + 1];
            for (int i = 0; i < 2 * N + 1; ++i)
                n[i] = i - N;
            for (int i = 0; i < 7; ++i)
                for (int j = 0; j < 2 * N + 1; ++j)
                    T[i] += Math.Pow(n[j], i);

            double[,] S = new double[5, 5]; //This should really be 4,4, but the matrix inversion stuff uses 1-based indexing
            for (int i = 1; i < 5; ++i)
                for (int j = 1; j < 5; ++j)
                    S[i, j] = T[i + j - 2]; //-2 in T since indices in S are offset

            double[,] SInvTemp = new double[5, 5];
            Array.Copy(S, SInvTemp, 5 * 5); //Copy S to SInv
            inv.inverse(ref SInvTemp, 4);
            SInv = new rawType[4, 4];
            //Copy SInv values into a real 4x4 matrix
            for (int i = 0; i < 4; ++i)
                for (int j = 0; j < 4; ++j)
                    SInv[i, j] = (rawType)SInvTemp[i + 1, j + 1];

            //a = new double[4];
            A = new double[numElectrodes][];

            startPeg = new int[numElectrodes];
            stopPeg = new int[numElectrodes];

            rails = new double[2];
            rails[0] = railLow;
            rails[1] = railHigh;

            this.prePeg = prePeg;
            this.postPeg = postPeg;
            this.postPegZero = postPegZero;

            this.delta = delta;

            PEGGING = new bool[numElectrodes];
            DEPEGGING = new bool[numElectrodes];
            FULL_LOOK = new bool[numElectrodes];
            FIT_UNFINISHED = new bool[numElectrodes];
            PEGGING_UNFINISHED = new bool[numElectrodes];
            W = new double[numElectrodes][];
            for (int i = 0; i < numElectrodes; ++i)
            {
                PEGGING[i] = true;
                DEPEGGING[i] = false;
                FIT_UNFINISHED[i] = false;
                PEGGING_UNFINISHED[i] = false;
                W[i] = new double[4];
            }

            endIdx = new int[numElectrodes];

            numSamples = bufferLength;
            //PRE = N + 1; //This has to be N+1 for fitting old points with W_recursive
            PRE = 2 * N + 1;
            POST = 2 * N; //This has to be 2N to fit new pts with W_nonrecursive

            oldData = new rawType[numElectrodes][];
            oldDataPrev = new rawType[numElectrodes][];

            for (int i = 0; i < numElectrodes; ++i)
            {
                oldDataPrev[i] = new rawType[PRE + POST];
                oldData[i] = new rawType[numSamples + PRE + POST];
            }

#if (USE_HIGHPASS)
            lastInputs = new double[numElectrodes];
            lastOutputs = new double[numElectrodes];
#endif
#if (DEBUG_JEFF)
            debugLogFile = new StreamWriter("salpa_log.txt");
            debugLogFile.WriteLine("SALPA Log File\r\n" + DateTime.Now);
#endif

        }

        //Commented out on 2/5/09: Not used, but useful to understand code
        //private void W_recursive(int n_c, double[] V, int channel)
        //{
        //    //double[] oldW = new double[3]; //Change to stackalloc (static array)
        //    //oldW[0] = W[channel][0];
        //    //oldW[1] = W[channel][1];
        //    //oldW[2] = W[channel][2];
        //    //W[channel][0] = oldW[0] + V[n_c + N] - V[n_c - N - 1];
        //    //W[channel][1] = -oldW[0] + oldW[1] + N * V[n_c + N] - (-N - 1) * V[n_c - N - 1];
        //    //W[channel][2] = oldW[0] - 2 * oldW[1] + oldW[2] + N * N * V[n_c + N] - (-N - 1) * (-N - 1) * V[n_c - N - 1];
        //    //W[channel][3] = -oldW[0] + 3 * oldW[1] - 3 * oldW[2] + W[channel][3] + N * N * N * V[n_c + N] - (-N - 1) * (-N - 1) * (-N - 1) * V[n_c - N - 1];  /* not sure about indexing for V's  */

        //    //W[channel][3] = -W[channel][0] + 3 * W[channel][1] - 3 * W[channel][2] + W[channel][3] + N * N * N * V[n_c + N] - (-N - 1) * (-N - 1) * (-N - 1) * V[n_c - N - 1];  /* not sure about indexing for V's  */
        //    //W[channel][2] = W[channel][0] - 2 * W[channel][1] + W[channel][2] + N * N * V[n_c + N] - (-N - 1) * (-N - 1) * V[n_c - N - 1];
        //    //W[channel][1] = -W[channel][0] + W[channel][1] + N * V[n_c + N] - (-N - 1) * V[n_c - N - 1];
        //    //W[channel][0] = W[channel][0] + V[n_c + N] - V[n_c - N - 1];

        //    W[channel][3] = -W[channel][0] + 3 * W[channel][1] - 3 * W[channel][2] + W[channel][3] + N * N * N * V[n_c + N] + (N + 1) * (N + 1) * (N + 1) * V[n_c - N - 1];  /* not sure about indexing for V's  */
        //    W[channel][2] = W[channel][0] - 2 * W[channel][1] + W[channel][2] + N * N * V[n_c + N] - (N + 1) * (N + 1) * V[n_c - N - 1];
        //    W[channel][1] = -W[channel][0] + W[channel][1] + N * V[n_c + N] + (N + 1) * V[n_c - N - 1];
        //    W[channel][0] = W[channel][0] + V[n_c + N] - V[n_c - N - 1];
        //} 

        private void W_nonRecursive(int n_c, double[] V, int channel)
        {
            W[channel][0] = W[channel][1] = W[channel][2] = W[channel][3] = 0.0;
            for (int k = 0; k <= 3; ++k)
            {
                for (int i = -N; i <= N; ++i)
                {
                    W[channel][k] += Math.Pow(i, k) * V[n_c + i];
                }
                /*  y[k] = sum((([-N:N]').^k).*V(n_c-N:n_c+N));  /* corresponding Matlab code */
            }
        }

        private void alpha(double[] a, int channel)
        {
            a[0] = SInv[0, 0] * W[channel][0] + SInv[0, 2] * W[channel][2];
            a[1] = SInv[1, 1] * W[channel][1] + SInv[1, 3] * W[channel][3];
            a[2] = SInv[2, 0] * W[channel][0] + SInv[2, 2] * W[channel][2];
            a[3] = SInv[3, 1] * W[channel][1] + SInv[3, 3] * W[channel][3];
        }

        private void A_n(double[] A, int n, int lenN, int n_c, int channel)
        {  /* n is the first of lenN pts. where you want the fit, n_c is the fit's center */
            double[] a = new double[4]; //Change to stackalloc
            alpha(a, channel);
            for (int i = 0; i < lenN; ++i)
            {
                A[i] = a[0] + a[1] * (n + i - n_c) + a[2] * (n + i - n_c) * (n + i - n_c) + a[3] * (n + i - n_c) * (n + i - n_c) * (n + i - n_c);
            }
            /* y = [a(1)*ones(1,length(n)) + a(2)*(n-n_c) + a(3)*(n-n_c).^2 + a(4)*(n-n_c).^3]'; /* corresponding Matlab code */
        }

        private double D_n(int n_c, double[] V, double[] A)
        {
            double y = 0.0;
            for (int i = 0; i < delta; ++i)
                y += V[n_c - N + i] - A[i];
            return (y * y);
            /* y = (sum(V(n_c-N:n_c-N+delta-1) - A(1:delta))).^2;  /* corresponding Matlab code */
        }


        internal void forcePegging() { for (int i = 0; i < oldData.GetLength(0); ++i) PEGGING[i] = true; }


        /******************************************************************
        /* Performs SALPA on signal to filter, then thresholds for spikes
        /*
        /* StimTimes allows you to automatically blank at a stimulus pulse, regardless of railing.  This should be in terms of indices, not seconds
        /******************************************************************/
        public void filter(ref rawType[][] filtData, int startChannel, int numChannels, rawType[] thresh,
            List<NeuroRighter.StimTick> stimIndicesIn, int numBufferReads)
        {
            List<int> stimIndices;
            lock (stimIndicesIn)
            {
#if (DEBUG_JEFF)
                int minStimIndexReads = Int32.MaxValue;
                for (int i = 0; i < stimIndicesIn.Count; ++i)
                    if (stimIndicesIn[i].numStimReads < minStimIndexReads) minStimIndexReads = stimIndicesIn[i].numStimReads;

                debugLogFile.WriteLine("[startChannel, numBufferReads, minStimIndexReads] " + startChannel + "\t" + numBufferReads + "\t" + minStimIndexReads);
#endif

                #region Deal With Stimultation Indices (times)
                //convert the stimindices input into something easier to search
                stimIndices = new List<int>(stimIndicesIn.Count);
                for (int i = 0; i < stimIndicesIn.Count; ++i)
                {
                    if (stimIndicesIn[i].numStimReads == numBufferReads)
                        stimIndices.Add(stimIndicesIn[i].index + PRE + POST);
                    else if (stimIndicesIn[i].numStimReads == numBufferReads - 1 &&
                        stimIndicesIn[i].index + POST >= numSamples)
                        stimIndices.Add(stimIndicesIn[i].index + PRE + POST - numSamples);
                }
            }
            #endregion

            //Start by organizing data
            for (int channel = startChannel; channel < startChannel + numChannels; ++channel)
            {
                #region Organize Data In Buffers
                //Copy tail of last buffer 
                for (int i = 0; i < PRE + POST; ++i)
                    oldData[channel][i] = oldDataPrev[channel][i];
                //Copy new data into a buffer
                for (int i = PRE + POST; i < numSamples + PRE + POST; ++i)
                    oldData[channel][i] = filtData[channel][i - (PRE + POST)];
                //Copy tail of recent buffer into oldDataPrev
                for (int i = 0; i < PRE + POST; ++i)
                    oldDataPrev[channel][i] = filtData[channel][numSamples - (PRE + POST) + i];
                #endregion

                FULL_LOOK[channel] = true; /* says whether you need to look at all 1:N future samples for rails (true), or just the Nth future sample (false) */
                int j = PRE;

                #region Check for Unfinished Pegging/Railing
                /********************************
                 * check unfinished pegging
                 ********************************/
                if (PEGGING_UNFINISHED[channel])
                {
                    //Calibrate pegs to new buffer's indices
                    startPeg[channel] -= numSamples;
                    stopPeg[channel] -= numSamples;

                    if (startPeg[channel] - prePeg > PRE)
                    {
                        for (int i = 0; i < startPeg[channel] - prePeg; ++i)
                            filtData[channel][i] = oldData[channel][PRE + i]; //copy unfiltered data to output
                        for (int i = startPeg[channel] - prePeg; i < stopPeg[channel] + postPeg; ++i)
                            filtData[channel][i - PRE] = 0.0; //zero out rest of fit
                    }
                    else
                    {
                        for (int i = PRE; i < stopPeg[channel] + postPeg; ++i)
                            filtData[channel][i - PRE] = 0.0; //zero out rest of fit
                    }
                    PEGGING[channel] = false;
                    PEGGING_UNFINISHED[channel] = false;
                    j = stopPeg[channel] + postPeg;


                    //
                    //Take care of post-peg
                    //
                    A[channel] = new double[N + 1];
                    int n_c = j + N;  /* start the fit N samples away */

                    W_nonRecursive(n_c, oldData[channel], channel);
                    A_n(A[channel], j, N + 1, n_c, channel);
                    if (D_n(n_c, oldData[channel], A[channel]) < thresh[channel])
                    {
                        if (j >= PRE)
                        {
                            /* satisfies fit criterion */
                            for (int k = 0; k < postPegZero; ++k)
                                filtData[channel][k + j - PRE] = 0.0;
                        }
                        else
                        {
                            for (int k = PRE; k < j + postPegZero; ++k)
                                filtData[channel][k - PRE] = 0.0;
                        }
                        for (int k = (j >= PRE ? postPegZero : PRE); k <= N; ++k)
                            /* subtract out fit */
                            filtData[channel][k + j - PRE] = oldData[channel][k + j] - A[channel][k];
                        j = n_c + 1;  /* jump ahead to end of fit */
                    }
                    else
                    {  /* fit wasn't good enough */
                        DEPEGGING[channel] = true;
                        if (j >= PRE)
                            filtData[channel][j - PRE] = 0.0; /* set pt. to zero, since the fit was too crappy */
                        ++j;  /* go to next pt. */

                        while (j < PRE)
                        {
                            n_c = j + N;
                            W_nonRecursive(n_c, oldData[channel], channel);
                            A_n(A[channel], j, N + 1, n_c, channel);
                            if (D_n(n_c, oldData[channel], A[channel]) < thresh[channel])
                            {
                                /* satisfies fit criterion */
                                for (int k = PRE; k < j + postPegZero; ++k)
                                    filtData[channel][k + j - PRE] = 0.0;
                                for (int k = (j >= PRE ? postPegZero : PRE); k <= N; ++k)
                                    /* subtract out fit */
                                    filtData[channel][k + j - PRE] = oldData[channel][k + j] - A[channel][k];
                                j = n_c + 1;  /* jump ahead to end of fit */
                                if (j < PRE)
                                {
                                    j = PRE;
                                    W_nonRecursive(j - 1, oldData[channel], channel);
                                }

                                DEPEGGING[channel] = false;
                            }
                            else
                            {  /* fit wasn't good enough */
                                ++j;  /* go to next pt. */
                            }
                        }
                    }
                }
                #endregion

                #region Check for Unfinished Fit
                //*********************************
                // Check fit unfinished
                //*********************************
                if (FIT_UNFINISHED[channel])
                {
                    for (int k = 0; k < A[channel].Length; ++k)
                        filtData[channel][j - PRE + k] = oldData[channel][j + k] - A[channel][k];
                    j += A[channel].Length - 1;
                    W_nonRecursive(j, oldData[channel], channel);
                    ++j;
                    FIT_UNFINISHED[channel] = false;
                }
                #endregion

                #region Main Algorithm for a Single Channel
                /*******************************************************************************
                 * Proceed through a single channel, checking for pegging, and subtracting fits
                 *******************************************************************************/
                while (j < numSamples + PRE)
                {
                    #region Look ahead in data for pegging
                    /* this  section of code looks ahead for pegging */
                    int startPegii, stopPegii;
                    startPeg[channel] = stopPeg[channel] = -1;
                    if (FULL_LOOK[channel])
                    {
                        for (int k = j; k <= j + N; ++k)
                        {
                            if (stimIndices.Contains(k) || oldData[channel][k] <= rails[0] || oldData[channel][k] >= rails[1])
                            {
                                if (startPeg[channel] == -1)
                                {
                                    startPeg[channel] = k;
                                    stopPeg[channel] = k;
                                }
                                else
                                {
                                    stopPeg[channel] = k;
                                }
                            }
                        }
                    }
                    else
                    {  /* no full look-ahead */
                        if (stimIndices.Contains(j + N) || oldData[channel][j + N] <= rails[0] || oldData[channel][j + N] >= rails[1])
                        {
                            startPeg[channel] = stopPeg[channel] = j + N;
                        }
                    }
                    #endregion

                    #region Deal with Pegging
                    if (startPeg[channel] >= 0)
                    {  /* we've detected pegging */
                        //Make sure we don't overshoot data
                        if (stopPeg[channel] + postPeg + postPegZero >= numSamples + PRE)
                        {
                            stopPegii = numSamples + PRE - 1;
                            PEGGING_UNFINISHED[channel] = true;
                        }
                        else
                            stopPegii = stopPeg[channel] + postPeg;

                        if (!PEGGING[channel]) //If we were not previously pegging
                        {
                            A[channel] = new double[N + 1];
                            /*check to make sure start index is valid*/
                            if (startPeg[channel] - prePeg < PRE)
                                startPegii = PRE; /*go as far back as possible */
                            else if (startPeg[channel] - prePeg >= numSamples + PRE)
                            {
                                startPegii = numSamples + PRE - 1;
                                PEGGING_UNFINISHED[channel] = true;
                            }
                            else startPegii = startPeg[channel] - prePeg; //Normal case

                            /* copy filtered data into output, up till peg , but scoot back by prePeg */
                            //W_recursive(j, oldData[channel], channel);
                            //A_n(A, j, startPegii - j, j, channel);
                            //for (int k = j; k < startPegii; ++k)
                            //{ /* prePeg should be number of samples needed to climb rail. I usually go conservative with 10*/
                            //    filtData[channel][k - PRE] = oldData[channel][k] - A[k - j];

                            //ADDED 8/4/08: ensure center of fit is not near artifact
                            W_nonRecursive(startPegii - (N + 1), oldData[channel], channel);
                            //ADDED 5/6/09: Make sure no A was calculated that included artifact
                            int backtrackJ = startPegii - (N + 1);
                            if (backtrackJ > j) backtrackJ = j;
                            if (backtrackJ < PRE) backtrackJ = PRE;
                            A_n(A[channel], backtrackJ, startPegii - backtrackJ, startPegii - (N + 1), channel);
                            for (int k = backtrackJ; k < startPegii; ++k)
                            { /* prePeg should be number of samples needed to climb rail. I usually go conservative with 10*/
                                filtData[channel][k - PRE] = oldData[channel][k] - A[channel][k - backtrackJ];
                            }
                            /* zero out peg */
                            for (int k = startPegii; k <= stopPegii; ++k)
                                filtData[channel][k - PRE] = 0.0;

                        }
                        else // PEGGING is 'true', which means we're still in the same peg
                        {
                            for (int k = j; k <= stopPegii; ++k)
                                filtData[channel][k - PRE] = 0.0;  /* zero out pegging */
                        }
                        PEGGING[channel] = true; /* declare that we're pegging */
                        DEPEGGING[channel] = false;
                        j = stopPegii + 1; /* move to the next data point after peg */

                        FULL_LOOK[channel] = true;  /* because we jumped ahead, we need to ensure we check for rails thoroughly */
                        if (j < numSamples + PRE) { }
                        else { continue; }
                    }
                    else
                    {
                        FULL_LOOK[channel] = false;  /* now, we know there is no pegging for the next N pts. */
                        //WE ARE SETTING THIS TOO MANY TIMES...
                    }
                    #endregion //End of peg checking


                    if (!PEGGING[channel] && !DEPEGGING[channel])
                    {  /* now, it's the normal algorithm */
                        //double[] A = new double[1];

                        //W_recursive(j, oldData[channel], channel);
                        //A_n(A, j, 1, j, channel);  /* get point to fit */
                        W[channel][3] = -W[channel][0] + 3 * W[channel][1] - 3 * W[channel][2] + W[channel][3] + N * N * N * oldData[channel][j + N] + (N + 1) * (N + 1) * (N + 1) * oldData[channel][j - N - 1];  /* not sure about indexing for V's  */
                        W[channel][2] = W[channel][0] - 2 * W[channel][1] + W[channel][2] + N * N * oldData[channel][j + N] - (N + 1) * (N + 1) * oldData[channel][j - N - 1];
                        W[channel][1] = -W[channel][0] + W[channel][1] + N * oldData[channel][j + N] + (N + 1) * oldData[channel][j - N - 1];
                        W[channel][0] = W[channel][0] + oldData[channel][j + N] - oldData[channel][j - N - 1];

                        filtData[channel][j - PRE] = oldData[channel][j] - SInv[0, 0] * W[channel][0] - SInv[0, 2] * W[channel][2];
                        //filtData[channel][j - PRE] = oldData[channel][j] - A[0];

                        ++j;
                        continue;
                    }

                    if (PEGGING[channel])
                    {
                        /* we were just at a rail, now we're trying to fit data */
                        A[channel] = new double[N + 1];
                        PEGGING[channel] = false;

                        int n_c = j + N;  /* start the fit N samples away */

                        /*obscure case where we attempt to start a fit, but there isn't enough data to do it. This can happen when I have very long RC
                         *constants, but the trial ends.  */
                        W_nonRecursive(n_c, oldData[channel], channel);

                        A_n(A[channel], j, N + 1, n_c, channel);
                        if (D_n(n_c, oldData[channel], A[channel]) < thresh[channel])
                        {
                            /* satisfies fit criterion */
                            for (int k = 0; k < postPegZero; ++k)
                            {
                                filtData[channel][k + j - PRE] = 0.0;
                            }
                            //Check for overrun
                            if (j - PRE + N < numSamples)
                                for (int k = postPegZero; k <= N; ++k)
                                    filtData[channel][k + j - PRE] = oldData[channel][k + j] - A[channel][k]; /* subtract out fit */
                            else
                            {
                                //[JDR] 09/02/06: fixed indexing error (only affected short device refresh rates
                                for (int k = j + postPegZero; k < numSamples + PRE; ++k)
                                    filtData[channel][k - PRE] = oldData[channel][k] - A[channel][k - (j + postPegZero)]; // subtract out fit, till end of data

                                FIT_UNFINISHED[channel] = true;
                                double[] ANew = new double[j - PRE + N - numSamples];
                                for (int k = 0; k < ANew.Length; ++k)
                                    ANew[k] = A[channel][numSamples - (j - PRE) + k];
                                A[channel] = ANew;
                            }
                            j = n_c + 1;  /* jump ahead to end of fit */
                            continue;
                        }
                        else
                        {  /* fit wasn't good enough */
                            filtData[channel][j - PRE] = 0.0; /* set pt. to zero, since the fit was too crappy */
                            ++j;  /* go to next pt. */
                            DEPEGGING[channel] = true;
                        }
                    }

                    while (DEPEGGING[channel] && j < numSamples + PRE)
                    {  /* we've come out of a peg, but the fit wasn't good */
                        A[channel] = new double[N + 1];

                        int n_c = j + N;

                        W_nonRecursive(n_c, oldData[channel], channel); //This was W_recursive before... which is right?
                        A_n(A[channel], j, N + 1, n_c, channel);
                        if (D_n(n_c, oldData[channel], A[channel]) < thresh[channel])
                        {
                            /* satisfies fit criterion */
                            if (j + postPegZero < numSamples - PRE)
                                for (int k = 0; k < postPegZero; ++k)
                                    filtData[channel][k + j - PRE] = 0.0;
                            else
                            {
                                for (int k = 0; k < numSamples + PRE - j; ++k)
                                    filtData[channel][k + j - PRE] = 0.0;
                                stopPeg[channel] = numSamples + PRE - postPeg;
                                PEGGING_UNFINISHED[channel] = true;
                                j = numSamples + PRE;
                                continue;
                            }

                            //Check for overrun
                            if (j - PRE + N < numSamples)
                                for (int k = postPegZero; k <= N; ++k)
                                    filtData[channel][k + j - PRE] = oldData[channel][k + j] - A[channel][k]; /* subtract out fit */
                            else
                            {
                                for (int k = j + postPegZero; k < numSamples - PRE; ++k)
                                    filtData[channel][k - PRE] = oldData[channel][k] - A[channel][k - j]; // subtract out fit, till end of data

                                FIT_UNFINISHED[channel] = true;
                                double[] ANew = new double[j - PRE + N - numSamples];
                                for (int k = 0; k < ANew.Length; ++k)
                                    ANew[k] = A[channel][numSamples - (j - PRE) + k];
                                A[channel] = ANew;
                            }

                            j = n_c + 1;  /* jump ahead to end of fit */
                            DEPEGGING[channel] = false;
                        }
                        else
                        {  /* fit wasn't good enough */
                            filtData[channel][j - PRE] = 0.0; /* set pt. to zero, since the fit was too crappy */
                            ++j;  /* go to next pt. */
                            //if (j >= numSamples + PRE)
                            //    break;
                        }
                    }
                } //Done with a single channel's worth of pegging/fitting
                #endregion

#if(USE_HIGHPASS)
                #region High-pass filter (1-pole)
                //This was added to deal with round-off error of long experiments.
                //The error caused a baseline drift on some channels.
                //J.D.R. Feb. 6, 2009

                double lastX = filtData[channel][0];
                filtData[channel][0] = ALPHA * (lastOutputs[channel]) + ALPHA * (filtData[channel][0] - lastInputs[channel]);
                for (int i = 1; i < filtData[channel].Length; ++i)
                {
                    double temp = filtData[channel][i];
                    filtData[channel][i] = ALPHA * (filtData[channel][i - 1]) + ALPHA * (filtData[channel][i] - lastX);
                    lastX = temp;
                }
                lastInputs[channel] = lastX;
                lastOutputs[channel] = filtData[channel][filtData[channel].Length - 1];
                #endregion
#endif
                //For debugging
                //for (int i = 0; i < filtData[channel].Length; ++i)
                //{
                //    if (filtData[channel][i] < -0.002)
                //    {
                //        int a = 1;
                //        a += 1;
                //    }
                //}
            }
        }
    }
}
