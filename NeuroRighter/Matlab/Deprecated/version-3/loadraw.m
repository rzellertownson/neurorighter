%LOADRAW Load raw or LFP data from NeuroRighter files.
%    y = LOADRAW(filename) takes as input the filename of a NeuroRighter
%    raw or LFP file and returns traces in an MxN matrix, where M  
%    is the number of channels and N is the number of data points.
%
%    y = LOADRAW(filename, ch) extracts only the specified channel (1-based)
%
%    y = LOADRAW(filename, timespan) extracts all channel data
%    for the specific time range.  'timespan' is a 1x2 vector, [t0 t1].
%    Samples in the range t0 <= t < t1 are extracted.
%
%    y = LOADRAW(filename, ch, timespan) extracts data from the specified
%    channel for the specified time range.  Setting ch = -1 extracts
%    all channels.
%
%    y = LOADRAW(filename, ch, timespan, 'SuppressText') works as above,
%    but does not write any text to the Matlab command window.
%
%    [y, t] = LOADRAW(filename, ...) additionally returns the time stamps of
%    the acquired samples in a 1xN vector (in seconds).
% 
%    Created by: John Rolston (rolston2@gmail.com)
%    Created on: June 26, 2007
%    Last modified: Dec 08, 2010
%    
%    Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt