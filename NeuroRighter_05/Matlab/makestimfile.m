function makestimfile(time, channel, waveform, filename)

%MAKESTIMFILE turns data matracies and vectors into a *.olstim file for use by
%neurorighter
% 
%    y = LOADSPIKE(time, channel, waveform, filename) takes as input:   
%         channel     [N 1] vector of channels to stimulate on
%         time        [N 1] vector of stimulation times (in milliseconds)
%         waveform    [N M] matrix of stimulation waveforms each with M
%                           samples at 10us per sample
%    and returns a .olstim file that specifies an open-loop stimulation
%    protocol for use with the NeuroRighter system
% 
%    Created by: Jon Newman (jonathan.p.newman at gmail dot com)
%    Created on: Sept 30, 2009
%    Last modified: Sept 30, 2009
%
%    Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt

% open file and write header
fid = fopen(strcat([filename,'.olstim']),'w');
fprintf(fid,'%s \n',strcat([filename, ': a stimulation file for use with Neurorighter, created by John Rolston']));

% find how many stimuli are created in this protocol and write as second
% line
numstim = length(time);
fprintf(fid,'%d \n',numstim);

% save stimulation times
otime = ceil(log10(max(time)));
cformat = strcat(['%0',num2str(otime)+1,'.0f \n']);
fprintf(fid,cformat,time);

% save channels
fprintf(fid,'%02.0f \n',channel);

% save waveforms
cformat = [];
for j = 1:size(waveform,2)-1
    cformat = [cformat '%f '];
end
cformat = [cformat '%f\n'];

for i = 1:numstim
    wave = waveform(i,:);
    fprintf(fid,cformat,wave);
end

fclose(fid);

end