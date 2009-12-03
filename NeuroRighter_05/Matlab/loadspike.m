%LOADSPIKE *.spk files created by the NeuroRighter software.
%    y = LOADSPIKE(filename) takes as input the filename of a NeuroRighter
%    spike file and returns a struct containing:
%         channel     [1 N] vector of channels on which a spike occured
%         time        [1 N] vector of spike times (in seconds)
%         waveform    [M N] matrix of clipped spike waveforms, scaled to Volts
%
%    y = LOADSPIKE(filename, channel) returns data only from the specified
%    channel
%
%    y = LOADSPIKE(filename, channel, [t0 t1]) returns data from the 
%    specified channel within the specified time range (>= t0 and <= t1).
% 
%    y = LOADSPIKE(filename, [t0 t1]) returns data within the specified 
%    time range (as above).
% 
%    Created by: John Rolston (rolston2@gmail.com)
%    Created on: June 15, 2007
%    Last modified: June 8, 2009
%
%    Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt