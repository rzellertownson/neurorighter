function makeauxfile(filename, time, channel, voltage)
%MAKEAUXFILE turns data vectors into a *.oldig file for use by
%neurorighter
%
%    y = MAKEAUXFILE(filename, time, channel, voltage) takes as input:
%         filename    string specifying file name
%         time        [N 1] vector specifying the time of aux
%                     event i in N events (in seconds)
%         channel     [N 1] vector specifying the channel on which
%                     event i should be applied (0 to 3)
%         voltage     [N 1] vector specifying the voltage that the
%   `                 channel(i) should take at t(i) (-10 to 10 volts). 
%                     Unless updated again, channel(i) will maintain 
%                     this voltage after time(i).
%
%    The program returns a .olaux file that specifies an open-loop
%    auxiliary output protocol for use with the NeuroRighter system
%
%    Created by: Jon Newman (jonathan.p.newman at gmail dot com)
%    Created on: Mar, 05 2011
%    Last modified: Mar, 05 2011
%
%    Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt

% Make sure that input is correctly formated
if nargin < 3
    error('Error:dim',' All four arguments are needed');
end
if size(time,2) > 1 || size(channel,2) > 1 || size(voltage,2) > 1
vo    error(['Error:dim',' The time, channel ' ...
          'and voltage vectors are column vectors']);
end
if size(time,1) ~= size(channel,1) || size(time,1) ~= size(voltage,1)
    error(['Error:dim',' The non-singleton dimension of the time, channel ' ...
          'and voltage vectors must be equal in size']);
end
if min(time) < 0 || max(channel) >3 || min(channel) < 0 || min(voltage < -10) || max(voltage) > 10
    error([ 'Error:InvalidData',' One or more of values occupying the time, channel and/or voltage ' ... 
            'vectors is invalid. Type help makeolaux into matlab to see valid ranges']);
end

% open file and write header
fid = fopen(strcat([filename,'.olaux']),'w');
tmake = datestr(now,31);
fprintf(fid,'%s \n',[filename  ' : ' tmake ' : an auxiliary event file for use with Neurorighter.']);

% How many events are in the file
numevent = size(time,1);

% Next log10 of max stim time
time = time*100000; % Convert to 100th of millisecond precision
otime = ceil(log10(max(time)));

% Make c formating strings
cformat_t = strcat(['%0',num2str(otime + 1),'.0f \n']);
cformat_c = '%02.0f \n';
cformat_v = '%3.5f \n';

% How many aux events will there be?
fprintf(fid,'%d \n',numevent);

% When will the last event occur?
finalTime = time(end)/100000;%Convert back to seconds
fprintf(fid,cformat_t,finalTime);

% Write the file
for i = 1:numevent
    
    % save stimulation times
    fprintf(fid,cformat_t,time(i));
    
    % save channels
    fprintf(fid,cformat_c,channel(i));
    
    % save voltage
    fprintf(fid,cformat_v,voltage(i));
        
end

fclose(fid);