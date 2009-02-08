%LOADSPIKE *.spk files created by the NeuroRighter software.
%    y = LOADSPIKE(filename) takes as input the filename of a NeuroRighter
%    spike file and returns a struct containing:
%         channel     [1 N] vector of channels on which a spike occured
%         time        [1 N] vector of spike times (in seconds)
%         waveform    [M N] matrix of clipped spike waveforms, scaled to Volts
%
% 
%    Created by: John Rolston (rolston2@gmail.com)
%    Created on: June 15, 2007
%    Last modified: January 29, 2009
%
%    Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt