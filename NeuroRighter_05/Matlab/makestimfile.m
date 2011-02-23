function makestimfile(filename, time, channel, waveform)
%MAKESTIMFILE turns data matracies and vectors into a *.olstim file for use by
%neurorighter
%
%    y = MAKESTIMFILE(time, channel, waveform, filename) takes as input:
%         channel     [N 1] vector of channels to stimulate on
%         time        [N 1] vector of stimulation times (in seconds)
%         waveform    [N M] matrix of stimulation waveforms (in Volts)
%                           each with M samples. If this argument is left
%                           out, then the user must design the stimulation
%                           waveform in NeuroRighter, which only allows
%                           square waves to be used.
%    The program returns a .olstim file that specifies an open-loop stimulation
%    protocol for use with the NeuroRighter system
%
%    Created by: Jon Newman (jonathan.p.newman at gmail dot com)
%    Created on: Sept 30, 2009
%    Last modified: Feb, 21 2011
%
%    Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt

% Make sure that input is correctly formated
if nargin < 4
    waveform = [];
end
if size(channel,2) > 1 || size(time,2) > 1
    error('Error:dim','Time and channel vectors are column vectors with the vertical index indicated the stimulus number and the value indicating time or channel');
end
if size(channel,1) ~= size(time,1)
    error('Error:dim','The number of indicies in the first dimension of the time, channel, \n and waveform matracies must be equal since it is the number of stimuli to be delivered');
end
if ~isempty(waveform) && size(waveform,2) < 80
    error('Error:Wavelength','The length of your stimulus waveforms Should be at least 80 Samples long so that its parameters can be encoded by the DAQ in four 20 sample chunks. For shorter stimuli, you can define multiple ones per line so they are effictively one stimulus.');
elseif ~isempty(waveform) && size(waveform,1) ~= size(time,1)
    error('Error:dim','The number of indicies in the first dimension of the time, channel, \n and waveform matracies must be equal since it is the number of stimuli to be delivered');
end

% open file and write header
fid = fopen(strcat([filename,'.olstim']),'w');
now = datestr(today,31);
fprintf(fid,'%s \n', [filename  ' : ' now ' : a stimulation file for use with Neurorighter, created by John Rolston']);

% find how many stimuli are created in this protocol and write as second
% line
numstim = length(time);
fprintf(fid,'%d \n',numstim);
finalstimtime = time(end);
fprintf(fid,'%d \n',finalstimtime);

% Write the number of samples per stim waveform
if ~isempty(waveform)
    fprintf(fid,'%d \n',size(waveform,2));
else
    fprintf(fid,'%d \n',0);
end

% Next log10 of max stim time
time = time*100000; % Convert to 100th of millisecond precision
otime = ceil(log10(max(time)));

% Make c formating strings
cformat_t = strcat(['%0',num2str(otime)+1,'.0f \n']);
cformat_c = '%02.0f \n';
cformat_w = [];
for j = 1:size(waveform,2)-1
    cformat_w = [cformat_w '%f '];
end
cformat_w = [cformat_w '%f\n'];

% Write the file
for i = 1:numstim
    
    % save stimulation times
    fprintf(fid,cformat_t,time(i));
    
    % save channels
    fprintf(fid,cformat_c,channel(i));
    
    % save waveforms
    if ~isempty(waveform);
        wave = waveform(i,:);
        fprintf(fid,cformat_w,wave);
    end
    
end

fclose(fid);