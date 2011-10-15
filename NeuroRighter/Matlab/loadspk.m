% LOADSPK Load NeuroRighter .spk files, v0.7.0.0 and up
%
% SPK = LAODSPK(FID) fid is the fully qualified path to a NeuroRighter .spk
% file. The field that is returned contains information about the spike
% data and the spike data itself.
%
% SPK = LOADSPK(FID,TIME,WAVE) loads the .spk file for spikes that occured between
% time = [t0 t1] on the channels specified in chan. wave is a boolean that
% specifies whether waveforms should be returned. Use empty brackets, [],
% to specify defaults.
function spk = loadspk(fid, varargin)

% Decode input arguments
if nargin == 2
    if length(varargin) == 1 % time range specified
        time = varargin{1};
        wave = 1;
    elseif length(varargin) == 2 % time range and channels specified
        time = varargin{1};
        wave = varargin{2};
    end
else
    time = [];
    wave = 1;
end

% Open file stream
h = fopen(fid,'r');

% Get file size in bytes
fseek(h,0,'eof');
len = ftell(h);
fseek(h,0,'bof');

% Read the header
version = fread(h, 1, 'int16');
if (version ~= -3)
    % This file is from a revision of NeuroRighter lower than 0.7.0.0
    error('Error:DepreciatedVersion',['This file is from an old verion of NeuroRighter. ' ...
        'Please try to load your .spk file with the depreciated matlab function (../depreciated/loadspike)'])
end
nochannels = fread(h,1,'int16'); % number of channels
fs = fread(h,1,'int32'); % sampling rate in Hz
waveSamples = fread(h,1,'int16'); % samples per waveform
gain = fread(h,1,'int16'); % gain
adcpoll = fread(h,1,'double'); % DAC polling period in seconds
dt = fread(h,7,'int16'); % date of recording
recunit = fread(h, 1); % does this file contain unit info?

%find fields:
fields = cell(0);
fieldcount = 0;
in =  fread(h,1, 'uint8=>char');
namesize = 0; c = [];
while (in ~= '|')
    c = [c in];
    in =  fread(h,1, 'uint8=>char');
    namesize = namesize+1;
end
while (namesize>0)
    fieldsize = fread(h,1, 'int32');
    fieldchar = fread(h,1,'uint8=>char');
    fieldcount = fieldcount+1;
    fields{fieldcount}.name = c;
    fields{fieldcount}.size = fieldsize;
    fields{fieldcount}.char = fieldchar;
    
    in =  fread(h,1, 'uint8=>char');
    namesize = 0;c = [];
    while (in ~= '|')
        c = [c in];
        in =  fread(h,1, 'uint8=>char');
        namesize = namesize+1;
    end
end

% Size of the header in bytes
headersize = ftell(h);

% Size of a single spike record
packetsize =0;
for i = 1:fieldcount
    packetsize = packetsize+fields{i}.size/8;
end
packetsize = packetsize+waveSamples*8;

% Determine the number of spikes in the file and the last spike time
datalength = len-headersize;
nospikes = ceil(datalength/packetsize);
fseek(h,headersize+(nospikes-1)*packetsize+2,'bof');
lastspiketime =  fread(h,1,'int32')./fs;

% Display record info
fprintf('\nNEURORIGHTER SPIKE RECORD\n');
fprintf(['\tSampling rate (Hz): ' num2str(fs) '\n']);
fprintf(['\tADC Polling period (sec): ' num2str(adcpoll) '\n']);
fprintf(['\tNumber of channels: ' num2str(nochannels) '\n']);
fprintf(['\tDigital gain: ' num2str(gain) '\n']);
if (recunit)
    fprintf('\tThis file contains unit information\n');
else
    fprintf('\tThis file does not contains unit information\n');
end
fprintf(['\tSamples per waveform: ' num2str(waveSamples) '\n']);
fprintf(['\tRecording time (yr-mo-dy-hr-mi-sc-ms): ' ...
    num2str(dt(1)) '-' ...
    num2str(dt(2)) '-' ...
    num2str(dt(3)) '-' ...
    num2str(dt(4)) '-' ...
    num2str(dt(5)) '-' ...
    num2str(dt(6)) '-' ...
    num2str(dt(7)) '\n']);

fprintf(['\tNumber of spikes: ' num2str(nospikes) '\n']);
fprintf(['\trecording duration: ' num2str(lastspiketime) '\n\n']);

% RZT: algorithm for getting intervals: estimate average firing rate- from that,
% estimate the start and stop indices, with a maximum of 1000 spikes between
% the two of them
fprintf('Entering search for desired spikes.\n');

% Fix inputs
if isempty(time)
    time = [0 lastspiketime +1];
end

% Calculate average firing rate
aveFiringRate = nospikes/lastspiketime;
fprintf('Looking for first spike...');

% Find start index:
estStart = floor(time(1)*aveFiringRate);%estimate based on firing rate
if(estStart < 1)
    estStart = 1;
end

startSpike = loadgroup(h,headersize,packetsize,fs,estStart,estStart);
estPre = estStart - 1;
if (estPre < 1)
    estPre = 1;
end
preSpike = loadgroup(h,headersize,packetsize,fs,estPre,estPre);

% Earliest spike index
upbound = nospikes;
downbound = 1;
lowLimit = time(1)-adcpoll;

% Binary search to find the earliest spike index we want
while(~(((estStart>1)&&((startSpike.time>=lowLimit)&&(preSpike.time<lowLimit)))...
        || ((estStart==1)&&(startSpike.time>=lowLimit))...
        || ((estStart==nospikes)&&(startSpike.time<lowLimit))) )
    
    if (startSpike.time< lowLimit)% too low
        downbound = estStart+1;
        if (downbound > nospikes)
            downbound = nospikes;
        end
        estStart = ceil((downbound+upbound)/2);
    end
    
    if ((startSpike.time>=lowLimit)&&(preSpike.time>lowLimit))% too high
        upbound = estStart-1;
        if (upbound<1)
            upbound =1;
        end
        estStart = floor((downbound+upbound)/2);
        
    end
    
    startSpike = loadgroup(h,headersize,packetsize,fs,estStart,estStart);
    estPre = estStart-1;
    if (estStart<1)
        estPre = 1;
    end
    preSpike = loadgroup(h,headersize,packetsize,fs,estPre,estPre);
end
fprintf(' done\n');

% Find end index
fprintf('Looking for last spike...');
estStop = floor((estStart+nospikes)/2);
stopSpike = loadgroup(h,headersize,packetsize,fs,estStop,estStop);

estPost = estStop+1;
if (estStart>nospikes)
    estPost = nospikes;
end
postSpike = loadgroup(h,headersize,packetsize,fs,estPost,estPost);

upbound = nospikes;
downbound = estStart;
hiLimit = time(2)+adcpoll;

% Binary search to find the last spike index we want
while(~(((estStop<nospikes)&&((stopSpike.time<=hiLimit)&&(postSpike.time>hiLimit)))...
        || ((estStop==nospikes)&&(stopSpike.time<=hiLimit))...
        || ((estStop==estStart)&&(stopSpike.time>hiLimit))) )
    
    if (stopSpike.time> hiLimit)%to hi
        upbound = estStop-1;
        if (upbound<estStart)
            upbound =estStart;
        end
        
        estStop = floor((downbound+upbound)/2);
    end
    
    if ((stopSpike.time<=hiLimit)&&(postSpike.time<hiLimit))% to low
        downbound = estStop+1;
        if (downbound>nospikes)
            downbound = nospikes;
        end
        estStop = ceil((downbound+upbound)/2);
    end
    
    
    stopSpike = loadgroup(h,headersize,packetsize,fs,estStop,estStop);
    estPost = estStop+1;
    if (estPost>nospikes)
        estPost=nospikes;
    end
    
    postSpike = loadgroup(h,headersize,packetsize,fs,estPost,estPost);
    
end
fprintf(' done\n');

fprintf(['Loading a total of ' num2str(estStop-estStart+1) ' spikes: \n\tStarting at time ' num2str(startSpike.time) '\n\tEnding at time ' num2str(stopSpike.time) '\n']);

spk = loadgroup(h,headersize,packetsize,fs,estStart,estStop);

% Write down meta-data
spk.meta.fs_Hz = fs;
spk.meta.num_chan = nochannels;
spk.meta.dig_gain = gain;
spk.meta.adc_polling_period_sec = adcpoll;
spk.meta.record_units = recunit;
spk.meta.date = [num2str(dt(1)) '-' ...
    num2str(dt(2)) '-' ...
    num2str(dt(3)) '-' ...
    num2str(dt(4)) '-' ...
    num2str(dt(5)) '-' ...
    num2str(dt(6)) '-' ...
    num2str(dt(7))];

fprintf('...done\nLoad complete.\n\n');


    function spk = loadgroup(h, headersize, packetsize,fs ,start, stop)
        
        % Loads a single spike instance
        numspk2load = stop-start+1;
        
        offset =headersize+(start-1)*packetsize;
        fseek(h,offset,'bof');
        channel = fread(h,numspk2load,'int16',packetsize - 2);
        fseek(h,offset + 2,'bof');
        stime = fread(h,numspk2load,'int32',packetsize - 4)./fs;
        fseek(h,offset + 6,'bof');
        threshold = fread(h,numspk2load,'double',packetsize - 8);
        fseek(h,offset + 14,'bof');
        if (recunit)
            unit = fread(h,numspk2load,'int16',packetsize - 2);
            fseek(h,offset + 8,'bof');
            waveoffset = 16;
        else
            waveoffset = 14;
        end
        
        % Create struct
        spk.time = stime;
        spk.channel = channel;
        if (recunit)
            spk.unit = unit;
        end
        spk.threshold = threshold;
        
        if (wave)
            fseek(h,offset+waveoffset,'bof');
            spk.waveform = fread(h,[waveSamples numspk2load],[num2str(waveSamples) '*float64=>float64'],packetsize-8*waveSamples);
        end
        
    end

end