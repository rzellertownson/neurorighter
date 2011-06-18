%LOADSPIKE *.spk files created by the NeuroRighter software.
%    y = LOADSPIKE(filename) takes as input the filename of a NeuroRighter
%    spike file and returns a struct containing:
%         channel     [1 N] vector of channels on which a spike occured
%         time        [1 N] vector of spike times (in seconds)
%         threshold   [1 N] vector of thresholds used to detect each spike
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
%    Last modified: November 4, 2009
%
%    Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt

function y = loadspike(filename, varargin)

IS_OLD_VERSION = 0;

TARGET_CHANNEL = -1;
TIME_RANGE = -1;
if nargin == 2
    if length(varargin{1}) == 1 %specific channel
        TARGET_CHANNEL = varargin{1};
    elseif length(varargin{1}) == 2 %time range
        TIME_RANGE = varargin{1};
    end
end

%Open file
fid = fopen(filename, 'r');
if (fid <= 0)
    error('File not found.');
end

%Read header
%First, check version
version = fread(fid, 1, 'int16');
if (version >= 0)
    %Old spike file
    IS_OLD_VERSION = 1;
end
   
if (~IS_OLD_VERSION)
    numChannels = fread(fid, 1, 'int16');
    samplingRate = fread(fid, 1, 'int32');
    numSamplesPerWaveform = fread(fid, 1, 'int16');
    gain = fread(fid, 1, 'int16');
    dt = fread(fid, 7, 'int16'); %date/time
    
    %get fields
    numFields = 0;
    fieldNames = [];
    types = [];
    headerCorr = 0;
    isDone = 0;
    while (~isDone) 
        tempFieldName = fread(fid, 1, 'char');
        if tempFieldName(end) == '|'
            isDone = 1;
            continue;
        else
            numFields = numFields + 1;
            while (tempFieldName(end) ~= '|')
                tempFieldName = [tempFieldName fread(fid, 1, 'char')];
            end
            fieldNames{numFields} = char(tempFieldName(1:end-1)); %get rid of delimiter by not taking last char
            
            %get field type
            numBits = fread(fid, 1, 'int32');
            type = fread(fid, 1, 'char');
            if type == 'I'
                types{numFields} = 'int';
            elseif type == 'F'
                types{numFields} = 'float';
            end
            types{numFields} = [types{numFields} num2str(numBits)];
            headerCorr(numFields + 1) = numBits/8 + headerCorr(numFields);
        end
    end
else
    numChannels = version;
    samplingRate = fread(fid, 1, 'int32');
    numSamplesPerWaveform = fread(fid, 1, 'int16');
    gain = fread(fid, 1, 'int16');
    dt = fread(fid, 7, 'int16'); %date/time
    
    %get fields
    numFields = 2;
    fieldNames = {'channel', 'time'};
    types = {'int16', 'int32'};
    headerCorr = [0 2 6];
end

HEADER_POS = ftell(fid);

data = [];
SKIP = headerCorr(end) + numSamplesPerWaveform * 8;
dHeaderCorr = diff(headerCorr);
tidx = [];
cidx = [];
for f = 1:numFields
    fseek(fid, HEADER_POS + headerCorr(f), 'bof');
    data{f} = fread(fid, inf, types{f}, SKIP - dHeaderCorr(f));
    
    %Convert time to seconds
    if strcmpi(fieldNames{f}, 'time')
        data{f} = data{f} / samplingRate;
        
        %Adjust time range, if applicable
        if (length(TIME_RANGE) == 2)
            tidx = find(data{f} >= TIME_RANGE(1) & data{f} <= TIME_RANGE(2));
        end
    end
end

%Filter for time range if applicable
waveformOffset = 0;
numWaveforms = length(data{1});
if (length(TIME_RANGE) == 2)
    for f = 1:numFields
        data{f} = data{f}(tidx);
    end
    if ~isempty(tidx)
        waveformOffset = tidx(1) - 1;
        numWaveforms = length(tidx);
    else
        numWaveforms = 0;
    end
end

%Read waveforms
fseek(fid, HEADER_POS + headerCorr(end) + waveformOffset * SKIP, 'bof');
data{numFields+1} = fread(fid, [numSamplesPerWaveform numWaveforms], [num2str(numSamplesPerWaveform) '*float64=>float64'], SKIP - numSamplesPerWaveform * 8);
fieldNames{numFields+1} = 'waveform';

%Set to output
y = cell2struct(data, fieldNames, 2);

%Close file
fclose(fid);

%Prune waveforms for specific channel, if applicable
if TARGET_CHANNEL >= 0
    cidx = find(y.channel == TARGET_CHANNEL);
    for f=1:numFields
        data{f} = data{f}(cidx);
    end
    data{f+1} = data{f+1}(:,cidx);
    
    y = cell2struct(data, fieldNames, 2);
end