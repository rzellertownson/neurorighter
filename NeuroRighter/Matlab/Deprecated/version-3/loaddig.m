function dig = loaddig(fid)
%LOADDIG reads *.dig files created by the NeuroRighter software.
%    y = LOADDIG(filename) takes as input the filename of a NeuroRighter
%    DIG file and returns a struct containing:
%         time          [1 N] vector of times of a digital event
%                             (in seconds)
%         state         [1 N] vector of 32-bit integers representing the 
%                             port state at the corresponding time
% 
%    Created by: Jon Newman (jnewman6<snail>gatech<dot>edu)
%    Created on: July 27, 2007
%    Last modified: January 29, 2009
%
%    Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt

if nargin < 1
    error('You must provide a file path')
end
if(~strcmp(fid(end-2:end),'dig'))
    warning(['This file does not have a .dig extension. '... 
             'trying to extract data anayway...'])
end

% Constants
HEADER_BYTES = 18;
DIG_REC_BYTES = 8;

% Main code
h = fopen(fid,'r');

% Get file size
fseek(h,0,'eof');
len = ftell(h);
fseek(h,0,'bof');

% Read header info
fs = fread(h,1,'uint'); % sampling rate
dt = fread(h,7,'ushort'); % date and time

% Calculated number of stimuli
numstm = (len-HEADER_BYTES)/DIG_REC_BYTES;

% Display record info
fprintf('\nNEURORIGHTER DIGITAL INPUT RECORD\n');
fprintf(['\tSampling rate: ' num2str(fs) '\n']);
fprintf(['\tRecording time (yr-mo-dy-hr-mi-sc-ms): ' ... 
        num2str(dt(1)) '-' ...
        num2str(dt(2)) '-' ...
        num2str(dt(3)) '-' ...
        num2str(dt(4)) '-' ...
        num2str(dt(5)) '-' ...
        num2str(dt(6)) '-' ...
        num2str(dt(7)) '\n']);
fprintf(['\tNumber of digital events: ' num2str(numstm) '\n\n']);

% Allocate space in data struct.
dig.time = zeros(numstm,1);
dig.state = zeros(numstm,1);

% Read the 'daturs
dig.time = fread(h,'uint', 4)/fs;
fseek(h,HEADER_BYTES + 4,'bof');
dig.state = fread(h,'uint', 4);

% close file
fclose(h);

end



