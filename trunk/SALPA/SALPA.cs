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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NationalInstruments;
using NationalInstruments.Analysis;
using NationalInstruments.Analysis.Math;

namespace NeuroRighter
{
    using rawType = System.Double;

    sealed class SALPA
    {
        private int N;  //Half width of filter
        private rawType[,] SInv;    //S' matrix
        private rawType[][] W;
        private rawType[] a;
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
        //private int numElectrodes;
        private rawType delta;
        private rawType[,] Vprev; //Some previous samples of V
        private int[] endIdx;
        private int[] startPeg, stopPeg;
        private rawType[,] V;
        private int numSamples; //Buffer length
        private int offset3N1; // = 3 * N + 1;  for some loops, to save recalculating it
        private rawType Ncubed;
        private rawType Nsquared;
        private rawType N_1cubed;
        private rawType N_1squared;
       
        public SALPA():this(75, 5, 2, -5, 5, 5, 16, 5, 250)
        {
            //Default halfWidth of 75
        }

        public SALPA(int halfWidth, int prePeg, int postPeg, int postPegZero, rawType railLow, rawType railHigh, int numElectrodes, rawType delta, int bufferLength)
        {
            //Compute S' (inverse matrix of S)
            N = halfWidth;
            double[] T = new double[7];
            double[] n = new double[2 * N + 1];
            for (int i = 0; i < 2 * N + 1; ++i)
                n[i] = i - N;
            for (int i = 0; i < 7; ++i)
                for (int j = 0; j < 2*N+1; ++j)
                    T[i] += Math.Pow(n[j],i);

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

            a = new rawType[4];
            A = new rawType[numElectrodes][];

            startPeg = new int[numElectrodes];
            stopPeg = new int[numElectrodes];
            
            rails = new rawType[2];
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
            W = new rawType[numElectrodes][];
            for (int i = 0; i < numElectrodes; ++i)
            {
                PEGGING[i] = true;
                DEPEGGING[i] = false;
                FIT_UNFINISHED[i] = false;
                PEGGING_UNFINISHED[i] = false;
                W[i] = new rawType[4];
            }

            endIdx = new int[numElectrodes];

            //Make Vprev, which holds the final 3*N+1 samples of the last data run
            //Vprev = new double[numElectrodes][];
            //for (int i = 0; i < numElectrodes; ++i)
            //    Vprev[i] = new double[3 * N + 1];

            numSamples = bufferLength;
            //V = new double[numElectrodes][];
            //for (int i = 0; i < numElectrodes; ++i)
            //{
            //    V[i] = new double[numSamples + 3 * N + 1];
            //}
            Vprev = new rawType[numElectrodes, 3 * N + 1];
            V = new rawType[numElectrodes, numSamples + 3 * N + 1];

            offset3N1 = 3 * N + 1;
            Nsquared = N * N;
            Ncubed = Nsquared * N;
            N_1squared = (N + 1) * (N + 1);
            N_1cubed = N_1squared * (N + 1);
        }

        private rawType D_n(int n_c, int channel)
        {
            rawType y = (rawType)0.0; /* return value */
            int offset = n_c - N;
            for (int i = 0; i < delta; ++i)
            {
                y += V[channel, offset + i] - A[channel][i];
            }
            return (y * y); //Return y^2
            /* y = (sum(V(n_c-N:n_c-N+delta-1) - A(1:delta))).^2;  /* corresponding Matlab code */
        }


        /*****************************************************
         * FILTER - run SALPA on a matrix of data            *
         * ***************************************************/
        public unsafe void filter(ref rawType[][] filtData, int startChannel, int numChannels, rawType[] thresh, List<NeuroRighter.StimTick> stimIndicesIn) 
        {
            //convert the stimindices input into something easier to search
            List<int> stimIndices = new List<int>(stimIndicesIn.Count);
            for (int i = 0; i < stimIndicesIn.Count; ++i)
                stimIndices.Add((int)(stimIndicesIn[i].index));

            /* We want to have a delay in V, so we add the previous 3*N+1 samples of the last run to the current V */
            //double[][] V = new double[numElectrodes][];
            fixed (rawType* pV = &V[0, 0], pVprev = &Vprev[0, 0])
            {
                for (int i = startChannel; i < startChannel + numChannels; ++i)
                {
                    int baseOfDim = i * V.GetLength(1);
                    int baseofDimVPrev = i * Vprev.GetLength(1);
                    for (int j = 0; j < offset3N1; ++j)
                    {
                        pV[baseOfDim + j] = pVprev[baseofDimVPrev + j]; //Copy Vprev into new V
                        pVprev[baseofDimVPrev + j] = filtData[i][numSamples - offset3N1 + j]; //Copy last 3*N+1 samples into Vprev
                    }
                    for (int j = 0; j < numSamples; ++j)
                    {
                        pV[baseOfDim + j + offset3N1] = filtData[i][j];  //Copy new data into V
                    }
                }
            }

            fixed (rawType* pV = &V[0,0]) 
            {
                for (int channel = startChannel; channel < startChannel + numChannels; ++channel)
                {
                    int baseOfDim = channel * V.GetLength(1);
                    FULL_LOOK[channel] = true; //Always set to true to start of call to filter()
                    int j = N + 1;  // Start at N+1, so that we can dip into prev samples with W_recursive if necessary

                    /* Subtract out fit, since we hadn't finished doing so at end of last filter() */
                    if (!FIT_UNFINISHED[channel]) { /* put least common case first */}
                    else
                    {
                        //for (int k = endIdx[channel]; k < N; ++k)
                        //    filtData[channel][k - endIdx[channel]] = V[channel][k + N + 1 - endIdx[channel]] - A[channel][k];

                        //Knock out first postPegzero points of fit, to get rid of ringing
                        if (postPegZero < endIdx[channel])
                        {
                            for (int k = endIdx[channel]; k < N; ++k)
                                filtData[channel][k - endIdx[channel]] = pV[baseOfDim + k + N + 1 - endIdx[channel]] - A[channel][k];
                        }
                        else
                        {
                            for (int k = 0; k < postPegZero - endIdx[channel]; ++k)
                                filtData[channel][k] = (rawType)0.0;
                            for (int k = postPegZero; k < N; ++k)
                                filtData[channel][k - endIdx[channel]] = pV[baseOfDim + k + N + 1 - endIdx[channel]] - A[channel][k];
                        }

                        j += N - endIdx[channel] + 1;  /* jump ahead to end of fit */
                        DEPEGGING[channel] = false; /* Because we had a successful fit, we're no longer depegging (if we even were) */
                        FIT_UNFINISHED[channel] = false;
                    }

                    /* Finish pegging, since the peg happened outside the last call's data bounds */
                    if (!PEGGING_UNFINISHED[channel]) { }
                    else
                    {
                        startPeg[channel] -= numSamples;
                        stopPeg[channel] -= numSamples;
                        int startPegii, stopPegii;
                        /* Take care of prePeg */
                        if (startPeg[channel] - prePeg < N + 1)
                            startPegii = N + 1; //Go back as far as we can, even though it's less than prePeg
                        else
                            startPegii = startPeg[channel] - prePeg;
                        stopPegii = stopPeg[channel] + postPeg; //Hopefully, this will never go beyond length of buffer
                        /* Copy un-filtered data into output, up till peg , but scoot back by prepeg */
                        for (int k = j; k < startPegii; ++k) /* prePeg should be number of samples needed to climb rail. */
                            filtData[channel][k - N - 1] = pV[baseOfDim + k];
                        /* Zero out peg */
                        for (int k = startPegii; k <= stopPegii; ++k)
                            filtData[channel][k - N - 1] = (rawType)0.0;
                        /* Set variables and increment j */
                        PEGGING[channel] = true;   // Declare that we're pegging
                        DEPEGGING[channel] = false;

                        j = stopPegii + 1;   // Move to the next data point after peg

                        FULL_LOOK[channel] = true;     // Because we jumped ahead, we need to ensure we check for rails thoroughly
                        PEGGING_UNFINISHED[channel] = false;
                    }

                    /**************************************
                     * Check for Pegging                  *
                     **************************************/
                    while (j < numSamples + N + 1)  // Note that we stop before numSamples + N*3
                    {
                        /* This first section of code looks ahead for pegging */
                        startPeg[channel] = stopPeg[channel] = -1;
                        int startPegii, stopPegii; //We have this "faux" startPeg to allow us to hold onto real startPeg for later use, perhaps in next call to filter()
                        if (!FULL_LOOK[channel]) //Abbreviated look-ahead (just the furthest pt.)
                        {
                            if (pV[baseOfDim + j + N] <= rails[0] || pV[baseOfDim + j + N] >= rails[1] || stimIndices.Contains(j - (2 * N - 4))) //I had to go an extra 5 pts. backward to make the stimIndices match the real signal... don't know quite why yet.
                            {
                                startPeg[channel] = stopPeg[channel] = j + N;
                                FULL_LOOK[channel] = true;
                            }
                        }
                        else  //We should look ahead all N pts. for a rail
                        {
                            FULL_LOOK[channel] = false; //This was true, so let's reset it less we find a new peg
                            for (int k = j; k <= j + N; ++k)
                            {
                                if (pV[baseOfDim + k] <= rails[0] || pV[baseOfDim + k] >= rails[1] || stimIndices.Contains(k - (2 * N - 4)))
                                {
                                    if (startPeg[channel] < 0) //This is the first pegging we've detected in this look
                                    {
                                        startPeg[channel] = stopPeg[channel] = k;
                                        FULL_LOOK[channel] = true;  //Found a new peg, reset full_look
                                    }
                                    else //Otherwise, we already have startPeg set
                                    {
                                        stopPeg[channel] = k;
                                    }
                                }
                            }
                        }

                        //Now, we respond to pegging, if found
                        if (startPeg[channel] == -1) { /* do nothing */ }
                        else //We've detected pegging
                        {
                            //First, ensure we don't go past good data
                            if (stopPeg[channel] + postPeg < numSamples + N + 1)
                                stopPegii = stopPeg[channel] + postPeg;
                            else
                            {
                                stopPegii = numSamples + N;
                                PEGGING_UNFINISHED[channel] = true;
                            }

                            if (!PEGGING[channel])  //If we weren't previously pegging
                            {
                                //Check to make sure start index is valid
                                if (startPeg[channel] - prePeg < N + 1)
                                    startPegii = N + 1; //Go back as far as we can, even though it's less than prePeg
                                else
                                    startPegii = startPeg[channel] - prePeg;

                                /* Check to ensure startPegii isn't outside of data bounds */
                                if (startPegii > numSamples + N + 1)
                                {
                                    startPegii = numSamples + N + 1; //Set to last index of dataset, so that the data copy stops there
                                    PEGGING_UNFINISHED[channel] = true;
                                }

                                /* copy un-filtered data into output, up till peg , but scoot back by prepeg */
                                A[channel] = new rawType[startPegii - j];
                                //n_c = j;
                                //if (W[channel][0] == 0 && W[channel][1] == 0) //I don't know how this snuck into the code...
                                //{
                                //W_recursive(ref V);
                                W[channel][3] = -W[channel][0] + 3 * W[channel][1] - 3 * W[channel][2] + W[channel][3] + N * N * N * pV[baseOfDim + j + N] + (N + 1) * (N + 1) * (N + 1) * pV[baseOfDim + j - N - 1];
                                W[channel][2] = W[channel][0] - 2 * W[channel][1] + W[channel][2] + N * N * pV[baseOfDim + j + N] - (N + 1) * (N + 1) * pV[baseOfDim + j - N - 1];
                                W[channel][1] = -W[channel][0] + W[channel][1] + N * pV[baseOfDim + j + N] + (N + 1) * pV[baseOfDim + j - N - 1];
                                W[channel][0] = W[channel][0] + pV[baseOfDim + j + N] - pV[baseOfDim + j - N - 1];
                                //}
                                //A_n(j, startPegii - j);
                                a[0] = SInv[0, 0] * W[channel][0] + SInv[0, 2] * W[channel][2];
                                a[1] = SInv[1, 1] * W[channel][1] + SInv[1, 3] * W[channel][3];
                                a[2] = SInv[2, 0] * W[channel][0] + SInv[2, 2] * W[channel][2];
                                a[3] = SInv[3, 1] * W[channel][1] + SInv[3, 3] * W[channel][3];
                                //int temp = j - n_c;  //n_c = j, so temp = 0;
                                for (int i = 0; i < startPegii - j; ++i)
                                    //A[channel][i] = a[0] + a[1] * (temp + i) + a[2] * (temp + i) * (temp + i) + a[3] * (temp + i) * (temp + i) * (temp + i);
                                    //A[channel][i] = SInv[0, 0] * W[channel][0] + SInv[0, 2] * W[channel][2] + SInv[1, 1] * W[channel][1] + SInv[1, 3] * W[channel][3] * i + SInv[2, 0] * W[channel][0] + SInv[2, 2] * W[channel][2] * i * i + SInv[3, 1] * W[channel][1] + SInv[3, 3] * W[channel][3] * i * i * i;
                                    A[channel][i] = a[0] + a[1] * i + a[2] * i * i + a[3] * i * i * i;

                                for (int k = j; k < startPegii; ++k) /* prePeg should be number of samples needed to climb rail. */
                                {
                                    filtData[channel][k - N - 1] = pV[baseOfDim + k] - A[channel][k - j];
                                }
                                /* zero out peg */
                                for (int k = startPegii; k <= stopPegii; ++k)
                                    filtData[channel][k - N - 1] = (rawType)0.0;
                            }
                            else   /* PEGGING is true, which means we're still in the same peg */
                            {
                                for (int k = j; k <= stopPegii; ++k)
                                    filtData[channel][k - N - 1] = (rawType)0.0;  // Zero out pegging 
                            }

                            PEGGING[channel] = true;   // Declare that we're pegging
                            DEPEGGING[channel] = false;
                            j = stopPegii + 1;   // Move to the next data point after peg
                            //FULL_LOOK[channel] = true;     // Because we jumped ahead, we need to ensure we check for rails thoroughly
                            continue;   // Continue to next point (j)
                        }
                        /* This ends peg checking */


                        /******************************************************
                         * Compute fit                                        *
                         ******************************************************/
                        if (!PEGGING[channel] && !DEPEGGING[channel])
                        {
                            //I've gone through this and inlined the sub-routines, to speed things up (despite hurting readability)

                            //A[channel] = new double[1];
                            //n_c = j;

                            //W_recursive(V[channel]);
                            //double[] oldW = new double[3];
                            //oldW[0] = W[channel][0];
                            //oldW[1] = W[channel][1];
                            //oldW[2] = W[channel][2];
                            //W[channel][0] = oldW[0] + V[channel][j + N] - V[channel][j - N - 1];
                            //W[channel][1] = -oldW[0] + oldW[1] + N * V[channel][j + N] - (-N - 1) * V[channel][j - N - 1];
                            //W[channel][2] = oldW[0] - 2 * oldW[1] + oldW[2] + N * N * V[channel][j + N] - (-N - 1) * (-N - 1) * V[channel][j - N - 1];
                            //W[channel][3] = -oldW[0] + 3 * oldW[1] - 3 * oldW[2] + W[channel][3] + N * N * N * V[channel][j + N] - (-N - 1) * (-N - 1) * (-N - 1) * V[channel][j - N - 1];

                            //Trying something a little new, to avoid creating new variable: reverse order of assignments (5/13/2008)
                            //W[channel][3] = -W[channel][0] + 3.0 * W[channel][1] - 3.0 * W[channel][2] + W[channel][3] + N * N * N * pV[baseOfDim + j + N] + (N + 1) * (N + 1) * (N + 1) * pV[baseOfDim + j - N - 1];
                            //W[channel][2] = W[channel][0] - 2.0 * W[channel][1] + W[channel][2] + N * N * pV[baseOfDim + j + N] - (N + 1) * (N + 1) * pV[baseOfDim + j - N - 1];
                            //W[channel][1] = -W[channel][0] + W[channel][1] + N * pV[baseOfDim + j + N] + (N + 1) * pV[baseOfDim + j - N - 1];
                            //W[channel][0] = W[channel][0] + pV[baseOfDim + j + N] - pV[baseOfDim + j - N - 1];

                            W[channel][3] = -W[channel][0] + 3.0 * W[channel][1] - 3.0 * W[channel][2] + W[channel][3] + Ncubed * pV[baseOfDim + j + N] + N_1cubed * pV[baseOfDim + j - N - 1];
                            W[channel][2] = W[channel][0] - 2.0 * W[channel][1] + W[channel][2] + Nsquared * pV[baseOfDim + j + N] - N_1squared * pV[baseOfDim + j - N - 1];
                            W[channel][1] = -W[channel][0] + W[channel][1] + N * pV[baseOfDim + j + N] + (N + 1) * pV[baseOfDim + j - N - 1];
                            W[channel][0] = W[channel][0] + pV[baseOfDim + j + N] - pV[baseOfDim + j - N - 1];

                            //A_n(j, 1);  /* get point to fit */
                            //filtData[channel][j - N - 1] = V[channel][j] - A[channel][0];
                            filtData[channel][j - N - 1] = pV[baseOfDim + j] - SInv[0, 0] * W[channel][0] - SInv[0, 2] * W[channel][2];
                            ++j;
                        }

                        else if (PEGGING[channel])  /* we were just at a rail, now we're trying to fit data */
                        {
                            A[channel] = new rawType[N + 1];
                            PEGGING[channel] = false;

                            //n_c = j + N;  /* start the fit N samples away */

                            //W_nonRecursive(V[channel]);
                            W[channel][0] = W[channel][1] = W[channel][2] = W[channel][3] = (rawType)0.0;
                            for (int k = 0; k <= 3; ++k)
                                for (int i = -N; i <= N; ++i)
                                    W[channel][k] += (rawType)(Math.Pow(i, k) * pV[baseOfDim + j + N + i]);

                            //A_n(j, N+1);
                            a[0] = SInv[0, 0] * W[channel][0] + SInv[0, 2] * W[channel][2];
                            a[1] = SInv[1, 1] * W[channel][1] + SInv[1, 3] * W[channel][3];
                            a[2] = SInv[2, 0] * W[channel][0] + SInv[2, 2] * W[channel][2];
                            a[3] = SInv[3, 1] * W[channel][1] + SInv[3, 3] * W[channel][3];
                            for (int m = 0; m < N + 1; ++m)
                                A[channel][m] = a[0] + a[1] * (m - N) + a[2] * (m - N) * (m - N) + a[3] * (m - N) * (m - N) * (m - N);

                            if (D_n(j + N, channel) < thresh[channel]) /* Satisfies fit criterion */
                            {
                                // Ensure that we don't zoom past end of filtData
                                if (j > numSamples + 1)
                                {
                                    endIdx[channel] = numSamples + N + 1 - j;
                                    FIT_UNFINISHED[channel] = true;
                                }
                                else
                                {
                                    endIdx[channel] = N;
                                }
                                //Subtract out fit
                                //for (int k = 0; k < endIdx[channel]; ++k)
                                //    filtData[channel][k + j - N - 1] = V[channel][k + j] - A[channel][k];

                                //Knock out first postPeg points of fit, to get rid of ringing
                                if (postPegZero < endIdx[channel])
                                {
                                    for (int k = 0; k < postPegZero; ++k)
                                        filtData[channel][k + j - N - 1] = (rawType)0.0;
                                    for (int k = postPegZero; k < endIdx[channel]; ++k)
                                        filtData[channel][k + j - N - 1] = pV[baseOfDim + k + j] - A[channel][k];
                                }
                                else
                                {
                                    for (int k = 0; k < endIdx[channel]; ++k)
                                        filtData[channel][k + j - N - 1] = (rawType)0.0;
                                }
                                //j = n_c + 1;  /* jump ahead to end of fit */
                                j += N + 1;
                            }
                            else
                            {  /* fit wasn't good enough */
                                filtData[channel][j - N - 1] = (rawType)0.0; /* set pt. to zero, since the fit was too crappy */
                                ++j;  /* go to next pt. */
                                DEPEGGING[channel] = true;
                            }
                            continue;
                        }

                        else //DEPEGGING[channel] is true
                        {  /* we've come out of a peg, but the fit wasn't good */
                            A[channel] = new rawType[N + 1];

                            //n_c = j + N;
                            //W_recursive(ref V);
                            W[channel][3] = -W[channel][0] + 3 * W[channel][1] - 3 * W[channel][2] + W[channel][3] + N * N * N * pV[baseOfDim + j + N + N] + (N + 1) * (N + 1) * (N + 1) * pV[baseOfDim + j - 1];
                            W[channel][2] = W[channel][0] - 2 * W[channel][1] + W[channel][2] + N * N * pV[baseOfDim + j + N + N] - (N + 1) * (N + 1) * pV[baseOfDim + j - 1];
                            W[channel][1] = -W[channel][0] + W[channel][1] + N * pV[baseOfDim + j + N + N] + (N + 1) * pV[baseOfDim + j - 1];
                            W[channel][0] = W[channel][0] + pV[baseOfDim + j + N + N] - pV[baseOfDim + j - 1];

                            //A_n(j, N+1);
                            a[0] = SInv[0, 0] * W[channel][0] + SInv[0, 2] * W[channel][2];
                            a[1] = SInv[1, 1] * W[channel][1] + SInv[1, 3] * W[channel][3];
                            a[2] = SInv[2, 0] * W[channel][0] + SInv[2, 2] * W[channel][2];
                            a[3] = SInv[3, 1] * W[channel][1] + SInv[3, 3] * W[channel][3];
                            //int temp = j - n_c; // = N
                            for (int m = 0; m < N + 1; ++m)
                                A[channel][m] = a[0] + a[1] * (N + m) + a[2] * (N + m) * (N + m) + a[3] * (N + m) * (N + m) * (N + m);


                            if (D_n(j + N, channel) < thresh[channel])
                            { /* satisfies fit criterion */
                                // Ensure that we don't zoom past end of filtData
                                if (j > numSamples + 1)
                                {
                                    endIdx[channel] = numSamples + N + 1 - j;
                                    FIT_UNFINISHED[channel] = true;
                                }
                                else
                                    endIdx[channel] = N;

                                //for (int k = 0; k < endIdx[channel]; ++k)  /* subtract out fit */
                                //    filtData[channel][k + j - N - 1] = V[channel][k + j] - A[channel][k];

                                //Knock out first postPeg points of fit, to get rid of ringing
                                if (postPegZero < endIdx[channel])
                                {
                                    for (int k = 0; k < postPegZero; ++k)
                                        filtData[channel][k + j - N - 1] = (rawType)0.0;
                                    for (int k = postPegZero; k < endIdx[channel]; ++k)
                                        filtData[channel][k + j - N - 1] = pV[baseOfDim + k + j] - A[channel][k];
                                }
                                else
                                {
                                    for (int k = 0; k < endIdx[channel]; ++k)
                                        filtData[channel][k + j - N - 1] = (rawType)0.0;
                                }
                                j = j + N + 1;  /* jump ahead to end of fit */
                                DEPEGGING[channel] = false;
                            }
                            else
                            {  /* fit wasn't good enough */
                                filtData[channel][j - N - 1] = (rawType)0.0; /* set pt. to zero, since the fit was too crappy */
                                ++j;  /* go to next pt. */
                            }
                        }
                    }
                }
            }
        }
    }
}
