%LOADSTIM reads *.stim files created by the NeuroRighter software.
%    y = LOADSTIM(filename) takes as input the filename of a NeuroRighter
%    stim file and returns a struct containing:
%         channel     [1 N] vector of channels on which a stim pulse occured
%         time        [1 N] vector of stim times (in seconds)
%         voltage     [1 N] vector of max. voltage of deliver stim. waveforms, scaled to Volts
%         pulse width [1 N] vector of pulse widths (in microseconds)
%
% 
%    Created by: John Rolston (rolston2@gmail.com)
%    Created on: July 27, 2007
%    Last modified: January 29, 2009
%
%    Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt