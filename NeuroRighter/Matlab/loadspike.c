/* loadspike.c
 * 
 * Reads *.spk files created by NeuroRighter software.
 *
 * y = LOADSPIKE(filename) takes as input the filename of a NeuroRighter
 *     spike file and returns a struct containing:
 *          channel     [1 N] vector of channels on which a spike occured
 *          time        [1 N] vector of spike times (in seconds)
 *          waveform    [M N] matrix of clipped spike waveforms, scaled to Volts
 *
 * 
 * Created by: John Rolston (rolston2@gmail.com)
 * Created on: June 15, 2007
 * Last modified: January 29, 2009
 *
 * Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt
 *
 */

#include <stdio.h>
#include <stdlib.h>
#include "mex.h"
#include "matrix.h"

void mexFunction(int nlhs, mxArray *plhs[],
                 int nrhs, const mxArray *prhs[])
{
    int len; /* File length in bytes */
    int numRecs; /* Num records in file */
    char *filename;
    FILE *fp;
    mxArray *times;
    mxArray *channels;
    mxArray *waveforms;
    int i, j;  /* Loop indices */
    short wfmLength; /* Num. samples per waveform */
    short numChannels;
    int freq; /* Sampling rate */
    short gain;
    short* dt; /* Date and time */
    char* fieldnames[3];
    int dims[2];
    double* timeSpan; /* start and stop times to retrieve from file */
    int startTimeIdx; /* Spike index at which to start copying waveforms (used with timespans) */
    int stopTimeIdx;

    
    /* Variables for temp storage of input */
    short ch;
    int tm;
    double *chs;
    double *tms;
    double *wfm;
    double *wfms;
    
    /* Setup fieldnames for output struct (left-hand side) */
    fieldnames[0] = "channel";
    fieldnames[1] = "time";
    fieldnames[2] = "waveform";
    

    /* Check number of arguments */
    if (nrhs < 1)
        mexErrMsgTxt("Filename required as an argument.");
    if (nrhs > 2)
        mexErrMsgTxt("Too many input arguments (max. 2: filename and timespan)");
    if (nlhs > 2)
        mexErrMsgTxt("Too many output arguments (max. 2).");
    
    /* Input must be a string */
    if (mxIsChar(prhs[0]) != 1)
        mexErrMsgTxt("Input must be a string.");
    /* Input must be a row vector */
    if (mxGetM(prhs[0])!=1)
        mexErrMsgTxt("Input must be a row vector.");
    
    /* Get times to extract (optional) */
    if (nrhs > 1) {
        if (!mxIsNumeric(prhs[1])) {
            mexErrMsgTxt("Time span must be numeric.");
        }
        else {
            int numMembers;
            numMembers = mxGetNumberOfElements(prhs[1]);
            if (numMembers != 2)
                mexErrMsgTxt("Time span must have start and stop times.");
            else {
                timeSpan = mxGetPr(prhs[1]);
                if (timeSpan[1] < timeSpan[0])
                    mexErrMsgTxt("First element of time span (i.e., start time) must be less than second (i.e., stop time).");
            }
        }
    }

    
    /* Copy the string data from prhs[0] into a C string 'filename'. */
    filename = mxArrayToString(prhs[0]);
    if (filename == NULL) 
        mexErrMsgTxt("Could not convert input to string.");
    
    /* Open file */
    fp = fopen(filename,"rb");
    if (fp == NULL)
        mexErrMsgTxt("Could not open file.");
    
    /* Get file length */
    fseek(fp,0,SEEK_END); /* Seek from end of file */
    len = ftell(fp); /* Where am I? */
    fseek(fp,0,SEEK_SET); /* Return to beginning of file */
    fflush(fp);
    
    /* There are 24 bytes in the header, then each record */
    fread(&numChannels,2,1,fp); /* Read numChannels */
    fread(&freq,4,1,fp); /* Read sampling rate */
    fread(&wfmLength,2,1,fp); /* Read num. samples per waveform */
    fread(&gain,2,1,fp); /* Read gain */
    dt = mxCalloc(7, sizeof(short)); /* Allocate memory for date&time */
    fread(dt,2,7,fp); /* Read date and time */

    /* Compute #records */
    numRecs = (len - 24) / (6 + wfmLength * 8);
    
    
    /* Find actual number of records to write, given timespan */
    if (nrhs > 1) {
        startTimeIdx = 0;
        stopTimeIdx = numRecs - 1;
        for (i = 0; i < numRecs; ++i) {
            fseek(fp,2,SEEK_CUR);
            fflush(fp);
            fread(&tm,4,1,fp); /* Read spike time */
            fseek(fp,8*wfmLength,SEEK_CUR);
            fflush(fp);
            
           /* mexPrintf("[DEBUG] tm/f = %f\n", (double)(tm)/(double)freq);*/

            if ((double)(tm)/(double)freq >= (double)timeSpan[0]) {
                startTimeIdx = i;
                break;
            }
        }
        for ( ; i < numRecs; ++i) {
            fseek(fp,2,SEEK_CUR);
            fread(&tm,4,1,fp); /* Read spike time */
            fseek(fp,8*wfmLength,SEEK_CUR);
            
            if ((double)(tm)/(double)freq <= (double)timeSpan[1]) {
                stopTimeIdx = i;
            }
        }
        numRecs = stopTimeIdx - startTimeIdx + 1;
        fseek(fp,24 + startTimeIdx * (6 + wfmLength * 8),SEEK_SET); /* Return to beginning of file, +24 bytes (for header), +x bytes for skipped recs */
        fflush(fp);
        mexPrintf("\n[DEBUG] timeSpan[0] = %f, timeSpan[1] = %f\n", timeSpan[0], timeSpan[1]);
        mexPrintf("\n[DEBUG] startTimeIdx = %i, stopTimeIdx = %i, numRecs = %i\n\n", startTimeIdx, stopTimeIdx, numRecs);
    }


    /* Print summary (header) data */
    mexPrintf("#chs: %i\nsampling rate: %i\nwaveform length: %i\ngain: %i\n", numChannels, freq, wfmLength, gain);
    mexPrintf("date/time: %i-%i-%i %i:%i:%i:%i\n", dt[0], dt[1], dt[2], dt[3], dt[4], dt[5], dt[6]);
    mexPrintf("\nTotal num. records (spikes): %i\n", numRecs);
    
    /* Create struct for output */
    plhs[0] = mxCreateStructMatrix(1, 1, 3, fieldnames);
    
    /* Create arrays for spike channels, times, waveforms */
    dims[0] = 1;
    dims[1] = numRecs;
    wfm = mxCalloc(wfmLength, sizeof(double)); /* Store single waveform */
    chs = mxCalloc(numRecs, sizeof(double));
    tms = mxCalloc(numRecs, sizeof(double));
    wfms = mxCalloc(wfmLength * numRecs, sizeof(double));
    channels = mxCreateDoubleMatrix(1,numRecs,mxREAL); /* Allocate mem for channel vector */
    times = mxCreateDoubleMatrix(1,numRecs,mxREAL); /* Allocate mem for real times vector (after scaling) */
    waveforms = mxCreateDoubleMatrix(wfmLength,numRecs,mxREAL); /* Allocate mem for waveforms */
    
    /* Read each record */
    for (i = 0; i < numRecs; ++i) {
        fread(&ch,2,1,fp); /* Read channel number */
        fread(&tm,4,1,fp); /* Read spike time */
        fread(wfm,8,wfmLength,fp); /* Read scaled waveform (scaling happens during data acquisition) */

        chs[i] = (double)ch;
        tms[i] = (double)(tm)/(double)freq; /* Convert sample number to time (in seconds) */
        for (j = 0; j < wfmLength; ++j) {
            wfms[i * wfmLength + j] = wfm[j];
        } /* NB: Matlab's indices go down each column, then to the next row */
    }

    
    /* Set pointers to Matlab variables */
    mxSetPr(channels,chs);
    mxSetPr(times,tms);
    mxSetPr(waveforms,wfms);
    
    /* Set variables to LHS's struct */
    mxSetField(plhs[0], 0, "channel", channels);
    mxSetField(plhs[0], 0, "time", times);
    mxSetField(plhs[0], 0, "waveform", waveforms);
    
    /* Free used memory */
    mxFree(dt);
    mxFree(wfm);
    fclose(fp);
}