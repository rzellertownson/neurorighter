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
 * Last modified: June 8, 2009
 *
 * Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt
 *
 */

#include "io64.h"
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
    FILE *fp;
    mxArray *times;
    mxArray *channels;
    mxArray *waveforms;
    int64_T i, j;  /* Loop indices */
    short wfmLength; /* Num. samples per waveform */
    short numChannels;
    int freq; /* Sampling rate */
    short gain;
    short* dt; /* Date and time */
    char* fieldnames[3];
    double* timeSpan; /* start and stop times to retrieve from file */
    int64_T position; /* current position of file */
    int64_T offset; /* offset for next file jump */
    /* Variables for temp storage of input */
    short ch;
    int tm;
    double *chs;
    double *tms;
    double *wfm;
    double *wfms;

    int64_T numRecordsToWrite = 0;
    short extractChannel = -1; /* Channel to extract (-1 for all) */
    int isUsingTimespan = 0;
    int isUsingChannel = 0;
    const int64_T HEADER_LENGTH = 24;

    /* Setup fieldnames for output struct (left-hand side) */
    fieldnames[0] = "channel";
    fieldnames[1] = "time";
    fieldnames[2] = "waveform";
    

    /* Check number of arguments */
    if (nrhs < 1)
        mexErrMsgTxt("Filename required as an argument.");
    if (nrhs > 3)
        mexErrMsgTxt("Too many input arguments (max. 3: filename, channel, and timespan)");
    if (nlhs > 1)
        mexErrMsgTxt("Too many output arguments (max. 1).");
    
    /* Input must be a string */
    if (mxIsChar(prhs[0]) != 1)
        mexErrMsgTxt("Input must be a string.");
    /* Input must be a row vector */
    if (mxGetM(prhs[0])!=1)
        mexErrMsgTxt("Input must be a row vector.");
    
    /* Get channel/times to extract (optional) */
    if (nrhs > 1) {
        if (!mxIsNumeric(prhs[1])) {
            mexErrMsgTxt("Channel argument must be numeric.");
        }
        else {
            int numMembers;
            numMembers = mxGetNumberOfElements(prhs[1]);
            if (numMembers == 1) {
                extractChannel = (short)(mxGetScalar(prhs[1]));    
                isUsingChannel = 1;
            }
            else if (numMembers == 2) {
                timeSpan = mxGetPr(prhs[1]);
                if (timeSpan[1] < timeSpan[0]) {
                    mexErrMsgTxt("First element of time span (i.e., start time) must be less than second (i.e., stop time).");
                }
                isUsingTimespan = 1;
            }
            else {
                mexErrMsgTxt("Too many elements in second argument.");
            }
        }
        
        if (nrhs > 2) {
            if (!mxIsNumeric(prhs[1]))
                mexErrMsgTxt("Time span argument must be numeric.");
            else {
                int numMembers;
                numMembers = mxGetNumberOfElements(prhs[2]);
                if (numMembers != 2)
                    mexErrMsgTxt("Time span must have start and stop times.");
                else {
                    timeSpan = mxGetPr(prhs[2]);
                    if (timeSpan[1] < timeSpan[0])
                        mexErrMsgTxt("First element of time span (i.e., start time) must be less than second (i.e., stop time).");
                    isUsingTimespan = 1;
                }
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
    {
        structStat statbuf;
        int64_T fileSize = 0;

        if (0 == getFileFstat(fileno(fp), &statbuf))
        {
            len = statbuf.st_size;
            mexPrintf("File size is %" FMT64 "d bytes\n", len);
        }
    }
    
    /* There are 24 bytes in the header, then each record */
    fread(&numChannels,2,1,fp); /* Read numChannels */
    fread(&freq,4,1,fp); /* Read sampling rate */
    fread(&wfmLength,2,1,fp); /* Read num. samples per waveform */
    fread(&gain,2,1,fp); /* Read gain */
    dt = mxCalloc(7, sizeof(short)); /* Allocate memory for date&time */
    fread(dt,2,7,fp); /* Read date and time */

    /* Compute #records */
    numRecs = (len - 24) / (6 + (int64_T)(wfmLength) * 8);
    
    /* Determine number of records to write */
    if (!isUsingChannel && !isUsingTimespan) numRecordsToWrite = numRecs;
    else {
        wfm = mxCalloc(wfmLength, sizeof(double)); /* Store single waveform */

        for (i = 0; i < numRecs; ++i) {
            fread(&ch,2,1,fp); /* Read channel number */
            fread(&tm,4,1,fp); /* Read spike time */
            fread(wfm,8,wfmLength,fp); /* Read scaled waveform (scaling happens during data acquisition) */
            
            /*Check inclusion criteria */
            if (isUsingChannel == 1) {
                if (extractChannel == ch)  {
                    if (isUsingTimespan == 1) {
                        if (timeSpan[0] <= (double)(tm)/(double)freq && timeSpan[1] >=(double)(tm)/(double)freq)
                            ++numRecordsToWrite;
                    }
                    else numRecordsToWrite = numRecordsToWrite + (int64_T)1;
                }
            }
            else if (isUsingTimespan == 1) {
                if (timeSpan[0] <= (double)(tm)/(double)freq && timeSpan[1] >=(double)(tm)/(double)freq)
                    ++numRecordsToWrite;
            }
        }
        
        /* Return to beginning of data in file, post-header */
        /* fseek(fp,24,SEEK_SET); */
        /*offset = position;
        setFilePos(fp, (fpos_T*) &offset); */
        setFilePos(fp, (fpos_T*) &HEADER_LENGTH); 
        mxFree(wfm);
        
        if (numRecordsToWrite < 1) {
            fclose(fp);
            mexErrMsgTxt("No data in specified channel/time range.");
        }
    }
    

    /* Print summary (header) data */
    mexPrintf("#chs: %i\nsampling rate: %i\nwaveform length: %i\ngain: %i\n", numChannels, freq, wfmLength, gain);
    mexPrintf("date/time: %i-%i-%i %i:%i:%i:%i\n", dt[0], dt[1], dt[2], dt[3], dt[4], dt[5], dt[6]);
    mexPrintf("\nNum. records (spikes) selected out of total: %" FMT64 "d of %" FMT64 "d\n", numRecordsToWrite, numRecs);
    
    /* Create struct for output */
    plhs[0] = mxCreateStructMatrix(1, 1, 3, fieldnames);
    
    /* Create arrays for spike channels, times, waveforms */
    wfm = mxCalloc(wfmLength, sizeof(double)); /* Store single waveform */
    chs = mxCalloc(numRecordsToWrite, sizeof(double));
    tms = mxCalloc(numRecordsToWrite, sizeof(double));
    wfms = mxCalloc((int64_T)wfmLength * numRecordsToWrite, sizeof(double));
    channels = mxCreateDoubleMatrix(1,numRecordsToWrite,mxREAL); /* Allocate mem for channel vector */
    times = mxCreateDoubleMatrix(1,numRecordsToWrite,mxREAL); /* Allocate mem for real times vector (after scaling) */
    waveforms = mxCreateDoubleMatrix(wfmLength,numRecordsToWrite,mxREAL); /* Allocate mem for waveforms */
    
    /* Read each record */
    for (i = 0; i < numRecordsToWrite; ) {
        fread(&ch,2,1,fp); /* Read channel number */
        fread(&tm,4,1,fp); /* Read spike time */
        fread(wfm,8,wfmLength,fp); /* Read scaled waveform (scaling happens during data acquisition) */

        /*Check inclusion criteria */
        if (isUsingChannel == 1) {
            if (extractChannel == ch)  {
                if (isUsingTimespan == 1) {
                    if (timeSpan[0] <= (double)(tm)/(double)freq && timeSpan[1] >=(double)(tm)/(double)freq) {
                        chs[i] = (double)ch;
                        tms[i] = (double)(tm)/(double)freq; /* Convert sample number to time (in seconds) */
                        for (j = 0; j < wfmLength; ++j)
                            wfms[i * wfmLength + j] = wfm[j];
                        ++i;
                    }
                }
                else {
                    chs[i] = (double)ch;
                    tms[i] = ((double)(tm))/((double)freq); /* Convert sample number to time (in seconds) */
                    for (j = 0; j < wfmLength; ++j)
                        wfms[i * wfmLength + j] = wfm[j];
                    /*mexPrintf("\n[DEBUG] channel = %f, time = %f\n", chs[i], tms[i]);*/
                    ++i;
                }
            }
        }
        else if (isUsingTimespan == 1) {
            if (timeSpan[0] <= (double)(tm)/(double)freq && timeSpan[1] >=(double)(tm)/(double)freq) {
                chs[i] = (double)ch;
                tms[i] = (double)(tm)/(double)freq; /* Convert sample number to time (in seconds) */
                for (j = 0; j < wfmLength; ++j)
                    wfms[i * wfmLength + j] = wfm[j];
                ++i;
            }
        }
        else {
            chs[i] = (double)ch;
            tms[i] = (double)(tm)/(double)freq; /* Convert sample number to time (in seconds) */
            for (j = 0; j < wfmLength; ++j)
                wfms[i * wfmLength + j] = wfm[j];
            ++i;
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

    /*for (i = 0; i < numRecordsToWrite; ++i)
        mexPrintf("[DEBUG] i: %i, ch %f, time %f\n", i, chs[i], tms[i]);*/
    
    /* Free used memory */
    mxFree(dt);
    mxFree(wfm);
    fclose(fp);
}