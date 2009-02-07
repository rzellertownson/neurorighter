/* loadraw.c
 * 
 * Reads *.raw or *.lfp files created by the NeuroRighter software.
 *
 * y = LOADRAW(filename) takes as input the filename of a NeuroRighter
 *     raw or LFP file and returns traces in an MxN matrix, where M  
 *     is the number of channels and N is the number of data points.
 *
 * y = LOADRAW(filename, ch) extracts only the specified channel (0-based)
 *
 * y = LOADRAW(filename, timespan) extracts all channel data
 *     for the specific time range.  'timespan' is a 1x2 vector, [t0 t1].
 *     Samples in the range t0 <= t < t1 are extracted.
 *
 * y = LOADRAW(filename, ch, timespan) extracts data from the specified
 *     channel for the specified time range.  Setting ch = -1 extracts
 *     all channels.
 *
 * [y, t] = LOADRAW(filename, ...) additionally returns the time stamps of
 *     the acquired samples in a 1xN vector (in seconds).
 * 
 * Created by: John Rolston (rolston2@gmail.com)
 * Created on: June 26, 2007
 * Last modified: January 29, 2009
 *
 * Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt
 *
 */

#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include "mex.h"
#include "matrix.h"

void mexFunction(int nlhs, mxArray *plhs[],
                 int nrhs, const mxArray *prhs[])
{
    int len; /* File length in bytes */
    int numRecs; /* Num records in file */
    char *filename;
    FILE *fp;
    double *V;
    double *t;
    int i, j;  /* Loop indices */
    short numChannels;
    int freq; /* Sampling rate */
    short gain;
    short* dt; /* Date and time */
    double scalingCoeffs[4]; /* Scaling coefficients to convert raw digital values to voltages */
    double* timeSpan; /* start and stop times to retrieve from file */
    int outN; /* number of time pts. for output vectors */
    int* timeSpanIdx;
    
    
    /* Variables for temp storage of input */
    short val;
    
    int ch = -1; /* Channel num to extract (-1 if all channels, the default) */
    
    /* Check number of arguments */
    if (nrhs > 3)
        mexErrMsgTxt("Filename, channel number (optional), and timespan (optional) are only allowable arguments.");
    if (nrhs < 1)
        mexErrMsgTxt("No input arguments. Include filename, channel number (optional), and timespan (optional).");
    if (nlhs > 2)
        mexErrMsgTxt("Too many output arguments (max. 2).");
    
    /* Input must be a string */
    if (mxIsChar(prhs[0]) != 1)
        mexErrMsgTxt("Input must be a string.");
    /* Input must be a row vector */
    if (mxGetM(prhs[0])!=1)
        mexErrMsgTxt("Input must be a row vector.");
    
    /* Copy the string data from prhs[0] into a C string 'filename'. */
    filename = mxArrayToString(prhs[0]);
    if (filename == NULL) 
        mexErrMsgTxt("Could not convert input to string.");
    
    /* Open file */
    fp = fopen(filename,"rb");
    if (fp == NULL)
        mexErrMsgTxt("Could not open file.");
    
    /* Get channel to extract (optional) */
    if (nrhs > 1) {
        if (!mxIsNumeric(prhs[1])) {
            mexErrMsgTxt("Channel number or time span must be numeric.");
        }
        else {
            int numMembers;
            numMembers = mxGetNumberOfElements(prhs[1]);
            if (numMembers == 1)
                ch = (int)(mxGetScalar(prhs[1]));    
            else if (numMembers == 2) {
                timeSpan = mxGetPr(prhs[1]);
                if (timeSpan[1] < timeSpan[0])
                    mexErrMsgTxt("First element of time span (i.e., start time) must be less than second (i.e., stop time).");
            }
            else {
                mexErrMsgTxt("Too many elements in second argument.");
            }
        }
    }
    
    /* Get times to extract (optional) */
    if (nrhs > 2) {
        if (!mxIsNumeric(prhs[2])) {
            mexErrMsgTxt("Time span must be numeric.");
        }
        else {
            int numMembers;
            numMembers = mxGetNumberOfElements(prhs[2]);
            if (numMembers != 2)
                mexErrMsgTxt("Time span must have start and stop times.");
            else {
                timeSpan = mxGetPr(prhs[2]);
                if (timeSpan[1] < timeSpan[0])
                    mexErrMsgTxt("First element of time span (i.e., start time) must be less than second (i.e., stop time).");
            }
        }
    }
    
    /* Get file length */
    fseek(fp,0,SEEK_END); /* Seek from end of file */
    len = ftell(fp); /* Where am I? */
    fseek(fp,0,SEEK_SET); /* Return to beginning of file */
    fflush(fp);
    
    /* There are 54 bytes in the header, then each record */
    fread(&numChannels,2,1,fp); /* Read numChannels */
    fread(&freq,4,1,fp); /* Read sampling rate */
    fread(&gain,2,1,fp); /* Read gain */
    fread(scalingCoeffs,8,4,fp);     /* Read scaling coeffs */
    dt = mxCalloc(7, sizeof(short)); /* Allocate memory for date&time */
    fread(dt,2,7,fp); /* Read date and time */

    /* Compute #records */
    numRecs = (len - 54) / (2 * numChannels);

    /* Print summary (header) data */
    mexPrintf("#chs: %i\nsampling rate: %i\ngain: %i\n", numChannels, freq, gain);
    mexPrintf("date/time: %i-%i-%i %i:%i:%i:%i\n", dt[0], dt[1], dt[2], dt[3], dt[4], dt[5], dt[6]);
    mexPrintf("\nTotal num. samples per channel: %i\n", numRecs);

    /* Compute start and stop indices, if time span was specified */
    timeSpanIdx = mxCalloc(2, sizeof(int)); /* Allocate memory for start/stop indices */
    if (nrhs == 3) {
        timeSpanIdx[0] = (int)(ceil(timeSpan[0] * (double)freq));
        timeSpanIdx[1] = (int)(ceil(timeSpan[1] * (double)freq));
        ++timeSpanIdx[1];
        /* Take care of over/underrun */
        if (timeSpanIdx[0] < 0) {
            timeSpanIdx[0] = 0;
        }
        else if (timeSpanIdx[0] >= numRecs || timeSpanIdx[1] < 0) {
            mexErrMsgTxt("No records in indicated time span.");
        }
        if (timeSpanIdx[1] >= numRecs) {
            timeSpanIdx[1] = numRecs;
        }
        
        outN = timeSpanIdx[1] - timeSpanIdx[0];
    }
    else {
        timeSpanIdx[0] = 0;
        timeSpanIdx[1] = numRecs;
        outN = numRecs;
    }
    
    
    /* Create output matrix */
    if (ch < 0) /* default, all channels */
        plhs[0] = mxCreateDoubleMatrix(numChannels,outN,mxREAL);
    else { /* just the specified channel */
        if (ch > numChannels - 1)
            mexErrMsgTxt("Specified extraction channel does not exist in dataset.");
        plhs[0] = mxCreateDoubleMatrix(1,outN,mxREAL);
    }
    V = mxGetPr(plhs[0]);
    
    /* If more than two left-hand side arguments, output times */
    if (nlhs > 1) {
        plhs[1] = mxCreateDoubleMatrix(1,outN,mxREAL);
        t = mxGetPr(plhs[1]);
    }
    
    /* Seek to start of time span, if specified */
    fseek(fp, 2*numChannels*timeSpanIdx[0], SEEK_CUR);
    
    /* Read each record */
    for (i = 0; i < outN; ++i) {
        if (nlhs > 1)
            t[i] = (double)(i + timeSpanIdx[0]) / (double)freq;
        if (ch < 0) {
            for (j = 0; j < numChannels; ++j) {
                fread(&val,2,1,fp); /* Read digital value */
                V[i*numChannels+j] = scalingCoeffs[0] + scalingCoeffs[1] * (double)val +
                    scalingCoeffs[2] * scalingCoeffs[2] * (double)val +
                    scalingCoeffs[3] * scalingCoeffs[3] * scalingCoeffs[3] * (double)val;
            } /* NB: Matlab's indices go down each column, then to the next row */
        }
        else { /* just extract one channel */
            fseek(fp, 2*ch, SEEK_CUR); /* advance to next time channel appears */
            fread(&val,2,1,fp); /* Read value */
            V[i] = scalingCoeffs[0] + scalingCoeffs[1] * (double)val +
                scalingCoeffs[2] * scalingCoeffs[2] * (double)val +
                scalingCoeffs[3] * scalingCoeffs[3] * scalingCoeffs[3] * (double)val;
            fseek(fp, 2*(numChannels - 1 - ch), SEEK_CUR); /* skip past remaining channels */
            /* NB: We could just do one initial seek, then always seek ahead by all channels,
             * but that code would be harder to read and not a whole lot faster */
        }
    }
    
    /* Free used memory */
    mxFree(timeSpanIdx);
    mxFree(dt);
    fclose(fp);
}