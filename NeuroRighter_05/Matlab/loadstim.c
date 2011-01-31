/* loadstim.c
 * 
 * Reads *.stim files created by NeuroRighter software.
 *
 * y = LOADSTIM(filename) takes as input the filename of a NeuroRighter
 *     stim file and returns a struct containing:
 *          channel     [1 N] vector of channels on which a stim pulse occured
 *          time        [1 N] vector of stim times (in seconds)
 *          voltage     [1 N] vector of max. voltage of deliver stim. waveforms, scaled to Volts
 *          pulse width [1 N] vector of pulse widths (in microseconds)
 *
 * 
 * Created by: John Rolston (rolston2@gmail.com)
 * Created on: July 27, 2007
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
    mxArray *voltages;
    mxArray *widths;
    int i, j;  /* Loop indices */
    short wfmLength; /* Num. samples per waveform */
    short numChannels;
    int freq; /* Sampling rate */
    short gain;
    short* dt; /* Date and time */
    char* fieldnames[4];
    
    /* Variables for temp storage of input */
    short ch;
    int tm;
    double vlt;
    double wdth;
    double *chs;
    double *tms;
    double *vlts;
    double *wdths;
    
    /* Setup fieldnames for output struct (left-hand side) */
    fieldnames[0] = "channel";
    fieldnames[1] = "time";
    fieldnames[2] = "voltage";
    fieldnames[3] = "pulseWidth";
    

    /* Check number of arguments */
    if (nrhs != 1)
        mexErrMsgTxt("Filename (only) required as an argument.");
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
    
    /* Get file length */
    fseek(fp,0,SEEK_END); /* Seek from end of file */
    len = ftell(fp); /* Where am I? */
    fseek(fp,0,SEEK_SET); /* Return to beginning of file */
    fflush(fp);
    
    /* Read header info */
    fread(&freq,4,1,fp); /* Read sampling rate */
    dt = mxCalloc(7, sizeof(short)); /* Allocate memory for date&time */
    fread(dt,2,7,fp); /* Read date and time */
    
    /* Compute #records */
    numRecs = (len - 18) / 22; /* 22 bytes per record */

    /* Print summary (header) data */
    mexPrintf("sampling rate: %i\n", freq);
    mexPrintf("date/time: %i-%i-%i %i:%i:%i:%i\n", dt[0], dt[1], dt[2], dt[3], dt[4], dt[5], dt[6]);
    mexPrintf("\nTotal num. records (stim pulses): %i\n", numRecs);
    
    /* Create struct for output */
    plhs[0] = mxCreateStructMatrix(1, 1, 4, fieldnames);
    
    /* Create arrays for spike channels, times, voltages */
    vlts = mxCalloc(numRecs, sizeof(double)); /* Store stim voltages */
    chs = mxCalloc(numRecs, sizeof(double));
    tms = mxCalloc(numRecs, sizeof(double));
    wdths = mxCalloc(numRecs, sizeof(double));
    channels = mxCreateDoubleMatrix(1,numRecs,mxREAL); /* Allocate mem for channel vector */
    times = mxCreateDoubleMatrix(1,numRecs,mxREAL); /* Allocate mem for real times vector (after scaling) */
    voltages = mxCreateDoubleMatrix(1,numRecs,mxREAL); /* Allocate mem for voltages */
    widths = mxCreateDoubleMatrix(1,numRecs,mxREAL); /* Allocate mem for pulse widths */
    
    /* Read each record */
    for (i = 0; i < numRecs; ++i) {
        fread(&tm,4,1,fp); /* Read spike time */
        fread(&ch,2,1,fp); /* Read channel number */
        fread(&vlt,8,1,fp); /* Read scaled stim. voltage (scaling happens during data acquisition) */
        fread(&wdth,8,1,fp); /* Read pulse width (value read is divided by 100 microseconds) */
        
        chs[i] = (double)ch;
        tms[i] = (double)(tm)/(double)freq; /* Convert sample number to time (in seconds) */
        vlts[i] = (double)vlt;
        wdths[i] = 100.0*(double)wdth;
        /* NB: Matlab's indices go down each column, then to the next row */
    }
    
    /* Set pointers to Matlab variables */
    mxSetPr(channels,chs);
    mxSetPr(times,tms);
    mxSetPr(voltages,vlts);
    mxSetPr(widths,wdths);
    
    /* Set variables to LHS's struct */
    mxSetField(plhs[0], 0, "channel", channels);
    mxSetField(plhs[0], 0, "time", times);
    mxSetField(plhs[0], 0, "voltage", voltages);
    mxSetField(plhs[0], 0, "pulseWidth", widths);
    
    /* Free used memory */
    //mxFree(dt);
    fclose(fp);
}