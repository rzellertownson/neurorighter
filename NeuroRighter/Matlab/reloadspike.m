%loadspike extended cut
function spk = reloadspike(fid, times,dacPolling, waveform)%,ch,times,DACperiod)
h = fopen(fid,'r');

% Get file size
fseek(h,0,'eof');
len = ftell(h);
fseek(h,0,'bof');

%read header
version = fread(h,1,'int16'); % version
nochannels = fread(h,1,'int16'); % number of channels
fs = fread(h,1,'int32'); %sampling rate
waveSamples = fread(h,1,'int16');% samples per waveform
gain = fread(h,1,'int16'); %gain
dt = fread(h,7,'int16');

if (nargin<4)
    waveform = 0;
end

%find fields:
fields = cell(0);

fieldcount = 0;
in =  fread(h,1, 'uint8=>char');
    namesize = 0;c = [];
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

headersize = ftell(h);

packetsize =0;
for i = 1:fieldcount
    packetsize = packetsize+fields{i}.size/8;
end
packetsize = packetsize+waveSamples*8;

datalength = len-headersize;
nospikes = ceil(datalength/packetsize);
fseek(h,headersize+(nospikes-1)*packetsize+2,'bof');
lastspiketime =  fread(h,1,'int32')./fs;
%lastspike = loadgroup(h, headersize, packetsize,fs ,nospikes, nospikes)
% Display record info
fprintf('\nNEURORIGHTER SPIKE RECORD\n');
fprintf(['\tSampling rate: ' num2str(fs) '\n']);
fprintf(['\tNumber of channels: ' num2str(nochannels) '\n']);
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

%algorithm for getting intervals:
%estimate average firing rate- from that, estimate the start and stop
%indices, with a maximum of 1000 spikes between the two of them
aveFiringRate = nospikes/lastspiketime;
fprintf('looking for first spike...');
%find start index:
estStart = floor(times(1)*aveFiringRate);%estimate based on firing rate
if(estStart<1)
    estStart = 1;
end

startSpike = loadgroup(h,headersize,packetsize,fs,estStart,estStart);
estPre = estStart-1;
if (estPre<1)
  estPre = 1;
end
preSpike = loadgroup(h,headersize,packetsize,fs,estPre,estPre);


upbound = nospikes;
downbound = 1;
lowLimit = times(1)-dacPolling;
%binary search to find the earliest spike index we want
while(~(((estStart>1)&&((startSpike.time>=lowLimit)&&(preSpike.time<lowLimit)))...
        || ((estStart==1)&&(startSpike.time>=lowLimit))...
        || ((estStart==nospikes)&&(startSpike.time<lowLimit))) )
    
    if (startSpike.time< lowLimit)%to low
        downbound = estStart+1;
        if (downbound>nospikes)
            downbound = nospikes;
        end
        estStart = ceil((downbound+upbound)/2);
    end
    
    if ((startSpike.time>=lowLimit)&&(preSpike.time>lowLimit))% to high
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


%find end index
fprintf('looking for last spike...');
%find start index:
estStop = floor((estStart+nospikes)/2);
stopSpike = loadgroup(h,headersize,packetsize,fs,estStop,estStop);

estPost = estStop+1;
if (estStart>nospikes)
  estPost = nospikes;
end
postSpike = loadgroup(h,headersize,packetsize,fs,estPost,estPost);

upbound = nospikes;
downbound = estStart;
hiLimit = times(2)+dacPolling;
%binary search to find the last spike index we want
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

fprintf(['loading a total of ' num2str(estStop-estStart+1) ' spikes, starting at time ' num2str(startSpike.time) ' and ending at time ' num2str(stopSpike.time) '\n']);
fprintf(['nearest outliers at ' num2str(preSpike.time) ' and ' num2str(postSpike.time) '...']);
if waveform
    waveform = waveSamples;
end

spk = loadgroup(h,headersize,packetsize,fs,estStart,estStop,waveform);
fprintf('done\n\n');

%           
end

function spk = loadgroup(h, headersize, packetsize,fs ,start, stop,waveformsamples)

if (nargin<7)
    waveformsamples = 0;
end
nospikes = stop-start+1;

offset =headersize+(start-1)*packetsize;
fseek(h,offset,'bof');
channel = fread(h,nospikes,'int16',packetsize-2);
spk.channel = channel;
fseek(h,offset+2,'bof');
time = fread(h,nospikes,'int32',packetsize-4)./fs;
spk.time = time;
fseek(h,offset+6,'bof');
spk.threshold = fread(h,nospikes,'int64',packetsize-8);
if (waveformsamples>0)
    fprintf(' done with times/channels...');
    fseek(h,offset+14,'bof');
    
    
    spk.waveform = fread(h,[ waveformsamples nospikes],[num2str(waveformsamples) '*float64=>float64'],packetsize-8*waveformsamples);
end

end