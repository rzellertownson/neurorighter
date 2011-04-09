function stm = loadstim(fid)
%LOADSTIM reads *.stim files created by the NeuroRighter software.
%    y = LOADSTIM(filename) takes as input the filename of a NeuroRighter
%    stim file and returns a struct containing:
%         channel     [1 N] vector of channels on which a stim pulse occured
%         time        [1 N] vector of stim times (in seconds)
%         voltage     [1 N] vector of max. voltage of deliver stim. waveforms, scaled to Volts
%         pulse width [1 N] vector of pulse widths (in microseconds)
%
%
%    Created by: Jon Newman (jnewman<snail>gmail<dot>com)
%    Created on: July 27, 2007
%    Last modified: January 29, 2009
%
%    Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt

if nargin < 1
    error('You must provide a file path')
end
if(~strcmp(fid(end-4:end),'stim'))
    warning(['This file does not have a .stim extension. '... 
             'trying to extract data anayway...'])
end

% Constants
HEADER_BYTES = 18;
STIM_REC_BYTES = 22;

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
numstm = (len-HEADER_BYTES)/STIM_REC_BYTES;

% Display record info
fprintf('\nNEURORIGHTER STIMULATION RECORD\n');
fprintf(['\tSampling rate: ' num2str(fs) '\n']);
fprintf(['\tRecording time (yr-mo-dy-hr-mi-sc-ms): ' ... 
        num2str(dt(1)) '-' ...
        num2str(dt(2)) '-' ...
        num2str(dt(3)) '-' ...
        num2str(dt(4)) '-' ...
        num2str(dt(5)) '-' ...
        num2str(dt(6)) '-' ...
        num2str(dt(7)) '\n']);
fprintf(['\tNumber of stimuli: ' num2str(numstm) '\n\n']);

% Allocate space in data struct.
stm.time = zeros(numstm,1);
stm.channel = zeros(numstm,1);
stm.amplitude = zeros(numstm,1);
stm.pulsewidth = zeros(numstm,1);

% Read the 'daturs
stm.time = fread(h,'uint', 18)/fs;
fseek(h,HEADER_BYTES + 4,'bof');
stm.channel = fread(h,'short', 20);
fseek(h,HEADER_BYTES + 6,'bof');
stm.voltage = fread(h,'double', 14);
fseek(h,HEADER_BYTES + 14,'bof');
stm.pulsewidth = 100*fread(h,'double',14);

% close file
fclose(h);

end



