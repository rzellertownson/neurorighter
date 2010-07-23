Readme.txt

NeuroRighter Matlab Files

There are three main files to open NeuroRighter data: 
1) loadraw: loads raw or LFP files
2) loadspike: loads spike waveform data
3) loadstim: loads stimulation data

For usage of each function, type “help [function name]” at the Matlab command prompt.  For 
example, “help loadraw”.  

Troubleshooting
These are MEX functions, written in C, and compiled to work from within Matlab.  If the compiled 
functions do not work when initially installing NeuroRighter, type “mex [function file name]” at the 
Matlab command prompt, where [function file name] is the *.c file for the desired function.  For 
example, “mex loadraw.c”.  This should produce a working function. 


In addition to these files for loading data, there is a matlab script for creating the .olstim files needed
for open loop stimulation from file: makestimfile.m. Finally, There is a matlab class, SqueakySpk, that can be used
to do basic preprocessing on the extracellular 'snippet' data that NeuroRighter nominally produces. This
includes artifact rejection and spike sorting methods, as well as a log of the methods used and efficient data 
storage. See the readme file in the SqueakySpk folder for examples and details.

This document was prepared by John Rolston (rolston2@gmail.com) on Jan. 29, 2009.  It was last 
modified on July 23rd, 2010 by Jon Newman (jnewman6 <at> gatech <dot> edu).
