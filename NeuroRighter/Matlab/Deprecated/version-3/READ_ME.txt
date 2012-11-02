NeuroRighter Matlab Files (for NeuroRighter v1.0.0.0 and up)

There are four matlab functions that can load NeuroRighter's binary data files into matlab structs:
1) loadraw: loads raw or LFP files( *.raw, *.salpa, *.spkflt, or *.aux)
2) loadspike: loads spk waveform data( *.spk, *.rawspk, *.salpaspk)
3) loadstim: loads stimulation data (*.stim)
4) loaddig: loads digital recordings (*.dig)

For usage of each function, type “help [function name]” at the Matlab command prompt.  For example, “help loadraw”.  

Troubleshooting:
loadraw is a MEX function, written in C, and compiled to work from within Matlab.  If it does not not work when initially installing NeuroRighter, type “mex [function file name]” at the  Matlab command prompt, where [function file name] is the *.c file for the desired function.  For  example, “mex loadraw.c”.  This should produce a working function. If you are having issues with this process, please see this this webpage http://www.mathworks.com/help/matlab/matlab_external/building-mex-files.html and then email the user's list if you still cannot figure it out.

This document was created by John Rolston (rolston2@gmail.com) on Jan. 29, 2009.  It was last  modified on June 27, 2012 by Jon Newman (jnewman6 <at> gatech <dot> edu).
