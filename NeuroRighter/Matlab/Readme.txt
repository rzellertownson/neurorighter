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


*****
[EDIT, JN 08-12-2010]
In newer version of NR, code revision 150 and up, you do not need to compile loadspike.c in matlab. You can use the interpreted code, loadspike.m instead.If you are having weird results, completely remove the loadspike.c, loadspike.mex and loadspike.mex32 from the path where loadspike.m is.

In addition to these files for loading data, there is a matlab script for creating the .olstim files needed
for open loop stimulation from file: makestimfile.m. Finally, There is a matlab class, SqueakySpk, that can be used
to do basic preprocessing on the extracellular 'snippet' data that NeuroRighter nominally produces. This
includes artifact rejection and spike sorting methods, as well as a log of the methods used and efficient data 
storage. See the readme file in the SqueakySpk folder for examples and details.


*****
[EDIT, JN 06-18-2011]
The latest versions of NR, 0.6.0 and up, include matlab only functions for loading stimulus times and digtial data, loadstim.m and loaddig.m. Only loading raw data requires a compiled mex file (loadraw.c).



This document was prepared by John Rolston (rolston2@gmail.com) on Jan. 29, 2009.  It was last 
modified on June 18rd, 2011 by Jon Newman (jnewman6 <at> gatech <dot> edu).
