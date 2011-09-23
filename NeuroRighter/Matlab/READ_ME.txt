NeuroRighter Matlab Files (for NeuroRighter v0.7.0.0 and up)

There are four matlab functions that can load NeuroRighter's binary data files into matlab structs:
1) loadraw: loads raw or LFP files( *.raw, *.salpa, *.spkflt, or *.aux)
2) loadspike: loads spike waveform data( *.spk)
3) loadstim: loads stimulation data (*.dig)
4) loaddig: loads digital recordings (*.dig)

For usage of each function, type “help [function name]” at the Matlab command prompt.  For 
example, “help loadraw”.  

Troubleshooting:
loadraw is a MEX function, written in C, and compiled to work from within Matlab.  If it does not not work when initially 
installing NeuroRighter, type “mex [function file name]” at the  Matlab command prompt, where [function file name] is the
*.c file for the desired function.  For  example, “mex loadraw.c”.  This should produce a working function. 


This document was created by John Rolston (rolston2@gmail.com) on Jan. 29, 2009.  It was last 
modified on June 18rd, 2011 by Jon Newman (jnewman6 <at> gatech <dot> edu).
