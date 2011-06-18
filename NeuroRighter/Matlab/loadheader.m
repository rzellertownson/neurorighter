%LOADHEADER Load raw or LFP header data from NeuroRighter files.
%    y = LOADHEADER(filename) takes as input the filename of a NeuroRighter
%    raw or LFP file and returns the header information in a structure.
%    Structure fields are as follows:
%
%       NumChannels: number of channels in recording
%       SamplingRate: frequency of sampling
%       Gain: A/D gain (resolution; usu. from 1-100)
%       ScalingCoefficients: Used to calibrate data from 16-bit integers
%                            to double-precision floating point numbers
%       Date: sub-structure showing when the file was created
%
%    Created by: John Rolston (rolston2@gmail.com)
%    Created on: July 20, 2009
%    Last modified: July 20, 2009
%    
%    Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt

function Header = loadheader(filename)

if ~ischar(filename)
    error('Filename must be a character string.');
end

%Open file
fid = fopen(filename,'r');

%Parse header
Header.NumChannels = fread(fid,1,'int16');
Header.SamplingRate = fread(fid,1,'int32');
Header.Gain = fread(fid,1,'int16');
Header.ScalingCoefficients = fread(fid,4,'double');
Header.Date.Year = fread(fid,1,'int16');
Header.Date.Month = fread(fid,1,'int16');
Header.Date.Day = fread(fid,1,'int16');
Header.Date.Hour = fread(fid,1,'int16');
Header.Date.Minute = fread(fid,1,'int16');
Header.Date.Second = fread(fid,1,'int16');
Header.Date.Millisecond = fread(fid,1,'int16');

%Find length of file
startPos = ftell(fid);
fseek(fid, 0, 'eof');
endPos = ftell(fid);

dataLength = endPos - startPos;
Header.NumRecords = dataLength / (Header.NumChannels * 2); %each sample is two bytes
Header.Duration = Header.NumRecords / Header.SamplingRate;
Header.TimeRange = [0 Header.Duration-(1/Header.SamplingRate)];

%Close file
fclose(fid);