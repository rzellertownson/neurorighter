function simoc = loadsimoc(fid)
%LOADSTIM reads *.simoc files created by the NeuroRighter software.
%    y = LOADSIMOC(filename) takes as input the filename of a NeuroRighter
%    DIG file and returns a struct containing:
%         time          [1 N] vector of times of simoc data points
%                             (in seconds)
%         obs           [1 N] vector of observation values
%         filt          [1 N] vector of filtered values
%         target        [1 N] vector of target values
%         error         [1 N] vector of error values
%         filtertype    [1 N] vector of integer codes for the filter type
%                             being using
%         feedbacktype  [1 N] vector of integer codes for the feedback algorithm
%                             being using
%         output        [M N] matrix containing the feedback signals. There
%                             meaning depends on the alogrithm used.
% 
%    Created by: Jon Newman (jnewman6<snail>gatech<dot>edu)
%    Created on: Aug 19, 2011
%    Last modified: Aug 19, 2011
%
%    Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt

if nargin < 1
    error('You must provide a file path')
end
if(~strcmp(fid(end-4:end),'simoc'))
    warning(['This file does not have a .simoc extension. '... 
             'trying to extract data anayway...'])
end

% Constants
HEADER_BYTES = 26;
SIMOC_REC_BYTES = 120;

% Main code
h = fopen(fid,'r');

% Get file size
fseek(h,0,'eof');
len = ftell(h);
fseek(h,0,'bof');

% Read header info
numstreams = fread(h,1,'int32'); % number of feedback streams
fs = fread(h,1,'double'); % sampling rate
dt = fread(h,7,'ushort'); % date and time

% Calculated number of stimuli
numdat = floor((len-HEADER_BYTES)/SIMOC_REC_BYTES);

% Display record info
fprintf('\nSIMOC RECORD\n');
fprintf(['\tSampling rate: ' num2str(fs) '\n']);
fprintf(['\tRecording time (yr-mo-dy-hr-mi-sc-ms): ' ... 
        num2str(dt(1)) '-' ...
        num2str(dt(2)) '-' ...
        num2str(dt(3)) '-' ...
        num2str(dt(4)) '-' ...
        num2str(dt(5)) '-' ...plo
        num2str(dt(6)) '-' ...
        num2str(dt(7)) '\n']);
fprintf(['\tNumber of streams recorded: ' num2str(numstreams) '\n\n']);

% Allocate space in data struct.
simoc.time = zeros(numdat,1);
simoc.obs = zeros(numdat,1);
simoc.filt = zeros(numdat,1);
simoc.target = zeros(numdat,1);
simoc.error = zeros(numdat,1);
simoc.filtertype = zeros(numdat,1);
simoc.feedbacktype = zeros(numdat,1);
simoc.output = zeros([numstreams-6,numdat]);

% Read the 'daturs
% , SIMOC_REC_BYTES - 8
simoc.time = fread(h,numdat,'double',SIMOC_REC_BYTES - 8);
fseek(h,HEADER_BYTES + 8,'bof');
simoc.obs = fread(h,numdat,'double',SIMOC_REC_BYTES - 8);
fseek(h,HEADER_BYTES + 16,'bof');
simoc.filt = fread(h,numdat,'double', SIMOC_REC_BYTES - 8);
fseek(h,HEADER_BYTES + 24,'bof');
simoc.target = fread(h,numdat,'double', SIMOC_REC_BYTES - 8);
fseek(h,HEADER_BYTES + 32,'bof');
simoc.error = fread(h,numdat,'double', SIMOC_REC_BYTES - 8);
fseek(h,HEADER_BYTES + 40,'bof');
simoc.filtertype = fread(h,numdat,'double', SIMOC_REC_BYTES - 8);
fseek(h,HEADER_BYTES + 48,'bof');
simoc.feedbacktype = fread(h,numdat,'double', SIMOC_REC_BYTES - 8);

% populated the temporary output array
for i = 1:8
    fseek(h,HEADER_BYTES + 48 + i*8,'bof');
    simoc.output(i,:) = fread(h,numdat,'double', SIMOC_REC_BYTES - 8);
end


% close file
fclose(h);

end



