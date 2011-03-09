function makedigfile(filename, bit, t1, t0)
%MAKEDIGFILE turns data vectors into a *.oldig file for use by
%neurorighter
%
%    y = MAKEDIGFILE(filename, bit, t1, t0) takes as input:
%         filename    string specifying file name
%         bit         [N 1] vector specifying the bit of digital event i in N events(0 to 31)
%         time        [N 1] vector specifying the time that event i in
%                           N events goes high (sec)
%         waveform    [N M] vector specifying the time that event i in
%                           N events goes low (sec)
%
%    The program returns a .oldig file that specifies an open-loop digital
%    output protocol for use with the NeuroRighter system
%
%    Created by: Jon Newman (jonathan.p.newman at gmail dot com)
%    Created on: Jan, 20 2011
%    Last modified: Jan, 20 2011
%
%    Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt

% Make sure that input is correctly formated
if nargin < 4
    error('Error:arguments','All four arguments are needed');
end
if size(bit,2) > 1 || size(t0,2) > 1 || size(t1,2) > 1
    error('Error:dim','bit, t0, and t1 are column vectors');
end

% open file and write header
fid = fopen(strcat([filename,'.oldig']),'w');
tmake = datestr(now,31);
fprintf(fid,'%s \n',[filename  ' : ' tmake ' : a digital event file for use with Neurorighter.']);

% Next log10 of max stim time
t0 = t0*100000; % Convert to 100th of millisecond precision
t1 = t1*100000; % Convert to 100th of millisecond precision
otime = ceil(log10(max(t0)));

% Make c formating strings
cformat_t = strcat(['%0',num2str(otime)+1,'.0f \n']);
cformat_p = '%02.0f \n';

% Conversion from bits to port integers and from on/off times to port
% change times and write to file
ChangeTime = sort(unique([t1;t0]));

% How many times will port's state be updated?
numUp = length(ChangeTime);
fprintf(fid,'%d \n',numUp);

% When will the last change occur?
finalTime = ChangeTime(end)/100000;%Convert back to seconds
fprintf(fid,cformat_t,finalTime);

for i = 1:length(ChangeTime)-1;
    
    BitsOn = bit(t1<=ChangeTime(i)&t0>ChangeTime(i));
    PortOut = sum([2.^BitsOn 0]);
    
    % save digital event times
    fprintf(fid,cformat_t,ChangeTime(i));
    
    % save channels
    fprintf(fid,cformat_p,PortOut);
    
end

% save digital event times
fprintf(fid,cformat_t,ChangeTime(end));

% save channels
fprintf(fid,cformat_p,0);

%close the file
fclose(fid);

end