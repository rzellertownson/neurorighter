/* loadraw.c
 * 
 * Reads *.raw or *.lfp files created by the NeuroRighter software.
 *
 * y = LOADRAW(filename) takes as input the filename of a NeuroRighter
 *     raw or LFP file and returns traces in an MxN matrix, where M  
 *     is the number of channels and N is the number of data points.
 *
 * y = LOADRAW(filename, ch) extracts only the specified channel (1-based)
 *
 * y = LOADRAW(filename, timespan) extracts all channel data
 *     for the specific time range.  'timespan' is a 1x2 vector, [t0 t1].
 *     Samples in the range t0 <= t < t1 are extracted.
 *
 * y = LOADRAW(filename, ch, timespan) extracts data from the specified
 *     channel for the specified time range.  Setting ch = -1 extracts
 *     all channels.
 *
 * y = LOADRAW(filename, ch, timespan, FLAG1, FLAG2, ...) uses FLAGs to
 *     define additional options.  Available flags are:
 *         1) SuppressText
 *
 * [y, t] = LOADRAW(filename, ...) additionally returns the time stamps of
 *     the acquired samples in a 1xN vector (in seconds).
 * 
 * Created by: John Rolston (rolston2@gmail.com)
 * Created on: June 26, 2007
 * Last modified: Dec 08, 2010
 *
 * Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt
 *
 */

#include "io64.h"
/*#include <stdio.h>*/
#include <stdlib.h>
#include <math.h>
#include <string.h>
#include "mex.h"
#include "matrix.h"


void mexFunction(int nlhs, mxArray *plhs[],
                 int nrhs, const mxArray *prhs[])
{
    int64_T len; /* File length in bytes */
    int64_T numRecs; /* Num records in file */
    char *filename;
    char *flag;
    short suppressText; /* boolean */
    FILE *fp;
    double *V;
    double *t;
    int64_T i, j;  /* Loop indices */
    short numChannels;
    int freq; /* Sampling rate */
    short gain;
    short* dt; /* Date and time */
    double scalingCoeffs[4]; /* Scaling coefficients to convert raw digital values to voltages */
    double* timeSpan; /* start and stop times to retrieve from file */
    int64_T outN; /* number of time pts. for output vectors */
    int64_T* timeSpanIdx;
    int64_T position; /* current position of file */
    int64_T offset; /* offset for next file jump */
    int isUsingTimespan = 0;
    int isUsingChannel = 0;
    
    
    /* Variables for temp storage of input */
    short val;
    
    int ch = -1; /* Channel num to extract (-1 if all channels, the default) */
    
    /* Check number of arguments */
    if (nrhs > 4)
        mexErrMsgTxt("Filename, channel number (optional), timespan (optional), and FLAGs (optional) are only allowable arguments.");
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
    mxFree(filename);
    
    /* Get channel to extract (optional) */
    if (nrhs > 1) {
        if (!mxIsNumeric(prhs[1])) {
            fclose(fp);
            mexErrMsgTxt("Channel number or time span must be numeric.");
        }
        else {
            int numMembers;
            numMembers = mxGetNumberOfElements(prhs[1]);
            if (numMembers == 1) {
                ch = (int)(mxGetScalar(prhs[1]));    
                isUsingChannel = 1;
            }
            else if (numMembers == 2) {
                timeSpan = mxGetPr(prhs[1]);
                if (timeSpan[1] < timeSpan[0]) {
                    fclose(fp);
                    mexErrMsgTxt("First element of time span (i.e., start time) must be less than second (i.e., stop time).");
                }
                isUsingTimespan = 1;
            }
            else {
                fclose(fp);
                mexErrMsgTxt("Too many elements in second argument.");
            }
        }
    }
    
    /* Get times to extract (optional) */
    if (nrhs > 2) {
        if (!mxIsNumeric(prhs[2])) {
            fclose(fp);
            mexErrMsgTxt("Time span must be numeric.");
        }
        else {
            int numMembers;
            numMembers = mxGetNumberOfElements(prhs[2]);
            if (numMembers != 2) {
                fclose(fp);
                mexErrMsgTxt("Time span must have start and stop times.");
            }
            else {
                timeSpan = mxGetPr(prhs[2]);
                if (timeSpan[1] < timeSpan[0]) {
                    fclose(fp);
                    mexErrMsgTxt("First element of time span (i.e., start time) must be less than second (i.e., stop time).");
                }
                isUsingTimespan = 1;
            }
        }
    }
    
    /* Check whether text was suppressed */
    suppressText = 0;
    if (nrhs > 3) {
        if (mxIsChar(prhs[3]) != 1)
            mexErrMsgTxt("FLAGs must be strings.");
        flag = mxArrayToString(prhs[0]);
        if (flag == NULL)
            mexErrMsgTxt("Could not convert FLAG input to string.");
        if (strcmpi(flag, "suppresstext"))
            suppressText = 1;
        mxFree(flag);
    }
    
    /* Get file length */  
/*    fseek(fp,0,SEEK_END); /* Seek from end of file */
/*    len = ftell(fp); /* Where am I? */
/*    fseek(fp,0,SEEK_SET); /* Return to beginning of file */
/*    fflush(fp);
 */
    /* Deal with large files */
    {
        structStat statbuf;
        int64_T fileSize = 0;

        if (0 == getFileFstat(fileno(fp), &statbuf))
        {
            len = statbuf.st_size;
            if(!suppressText)
                mexPrintf("File size is %" FMT64 "d bytes\n", len);
        }
    }
    
    /* There are 54 bytes in the header, then each record */
    fread(&numChannels,2,1,fp); /* Read numChannels */
    fread(&freq,4,1,fp); /* Read sampling rate */
    fread(&gain,2,1,fp); /* Read gain */
    fread(scalingCoeffs,8,4,fp);     /* Read scaling coeffs */
    dt = mxCalloc(7, sizeof(short)); /* Allocate memory for date&time */
    fread(dt,2,7,fp); /* Read date and time */

    /* Compute #records */
    numRecs = (len - 54) / (2 * (int64_T)numChannels);

    /* Print summary (header) data */
    if (!suppressText) {
        mexPrintf("#chs: %i\nsampling rate: %i\ngain: %i\n", numChannels, freq, gain);
        mexPrintf("date/time: %i-%i-%i %i:%i:%i:%i\n", dt[0], dt[1], dt[2], dt[3], dt[4], dt[5], dt[6]);
		 mexPrintf("scaling coefficients: %f +%f *x + %f*x^2 + %f*x^3 \n", scalingCoeffs[0], scalingCoeffs[1], scalingCoeffs[2], scalingCoeffs[3]);
        mexPrintf("\nTotal num. samples per channel: %i\n", numRecs);
    }

    /* Compute start and stop indices, if time span was specified */
    timeSpanIdx = mxCalloc(2, sizeof(int64_T)); /* Allocate memory for start/stop indices */
    if (isUsingTimespan) {
        timeSpanIdx[0] = (int64_T)(ceil(timeSpan[0] * (double)freq));
        timeSpanIdx[1] = (int64_T)(ceil(timeSpan[1] * (double)freq));
        ++timeSpanIdx[1];
        /* Take care of over/underrun */
        if (timeSpanIdx[0] < 0) {
            timeSpanIdx[0] = 0;
        }
        else if (timeSpanIdx[0] >= numRecs || timeSpanIdx[1] < 0) {
            fclose(fp);
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
        if (ch == 0 || ch > numChannels) {
            fclose(fp);
            mexErrMsgTxt("Specified extraction channel does not exist in dataset.");
        }
        plhs[0] = mxCreateDoubleMatrix(1,outN,mxREAL);
    }
    V = mxGetPr(plhs[0]);
    
    /* If more than two left-hand side arguments, output times */
    if (nlhs > 1) {
        plhs[1] = mxCreateDoubleMatrix(1,outN,mxREAL);
        t = mxGetPr(plhs[1]);
    }
    
    /* Seek to start of time span, if specified */
    getFilePos(fp, (fpos_T*) &position);
    offset = position + (int64_T)(2*numChannels*timeSpanIdx[0]);
    setFilePos(fp, (fpos_T*) &offset);
    /*fseek(fp, 2*numChannels*timeSpanIdx[0], SEEK_CUR);*/
    
    /* Read each record */
    for (i = 0; i < outN; ++i) {
        if (nlhs > 1)
            t[i] = (double)(i + timeSpanIdx[0]) / (double)freq;
        if (ch < 0) {
            for (j = 0; j < numChannels; ++j) {
                fread(&val,2,1,fp); /* Read digital value */
                V[i*numChannels+j] = scalingCoeffs[0] + scalingCoeffs[1] * (double)val +
                    scalingCoeffs[2] * (double)val * (double)val +
                    scalingCoeffs[3] * (double)val * (double)val * (double)val;
            } /* NB: Matlab's indices go down each column, then to the next row */
        }
        else { /* just extract one channel */
            getFilePos(fp, (fpos_T*) &position);
            offset = position + (int64_T)(2*(ch-1));
            setFilePos(fp, (fpos_T*) &offset);
            /*fseek(fp, 2*ch, SEEK_CUR); /* advance to next time channel appears */
            fread(&val,2,1,fp); /* Read value */
            V[i] = scalingCoeffs[0] + scalingCoeffs[1] * (double)val +
                scalingCoeffs[2] * (double)val * (double)val +
                scalingCoeffs[3] * (double)val * (double)val * (double)val;
            getFilePos(fp, (fpos_T*) &position);
            offset = position + (int64_T)(2*(numChannels - 1 - (ch-1)));
            setFilePos(fp, (fpos_T*) &offset);
            /*fseek(fp, 2*(numChannels - 1 - ch), SEEK_CUR); /* skip past remaining channels */
            /* NB: We could just do one initial seek, then always seek ahead by all channels,
             * but that code would be harder to read and not a whole lot faster */
        }
    }
    
    /* Free used memory */
    mxFree(timeSpanIdx);
    mxFree(dt);
    fclose(fp);
}