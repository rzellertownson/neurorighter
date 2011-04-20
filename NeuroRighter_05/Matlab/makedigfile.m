function makedigfile(varargin)
%MAKEDIGFILE turns data vectors into a *.oldig file for use by
%neurorighter
%
%    y = MAKEDIGFILE(filename, bit, t1, t0) takes as input:
%         filename    string specifying file name
%         bit         [N 1] vector specifying the bit of digital event i in N events(0 to 31)
%         t1          [N 1] vector specifying the time that event i in
%                           N events goes high (sec)
%         t0          [N 1] vector specifying the time that event i in
%                           N events goes low (sec)
%
%    y = MAKEDIGFILE(filename, int, tchange) takes as input:
%         filename    string specifying file name
%         int         [N 1] vector specifying a 32 bit integer
%                     (0 to 4,294,967,295) that dictates the digital output
%                     port state at the coresponding index of tchange.
%         tchange     [N 1] vector specifying the time the digital output
%                     port shoult take the state specified by the
%                     corresponding index of integer.
%
%    The program returns a .oldig file that specifies an open-loop digital
%    output protocol for use with the NeuroRighter system
%
%    Created by: Jon Newman (jonathan.p.newman at gmail dot com)
%    Created on: Jan, 20 2011
%    Last modified: Jan, 20 2011
%
%    Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt

% Make sure that input is correctly formated and find out what type of info
% the usere provided
if nargin == 4
    
    filename = varargin{1};
    bit = varargin{2};
    t1 = varargin{3};
    t0 = varargin{4};
    
    if ~ischar(filename)
        error('Error:arguments','filename must be a string)');
    end
    
    if max(bit) > 1
         error('Error:arguments','bit is an [NX1] array of binary values (0 or 1)');
    end
    
    if size(bit,2) > 1 || size(t0,2) > 1 || size(t1,2) > 1 || size(bit,1) ~= size(t0,1) || size(t0,1) ~= size(t1,1)
        error('Error:dim','bit, t0, and t1 are column vectors of equal length');
    end
    
elseif nargin == 3
    
    filename = varargin{1};
    int = varargin{2};
    tchange = varargin{3};
    
    if ~ischar(filename)
        error('Error:arguments','filename must be a string)');
    end
    
    if max(int) > 2^32 || min(int) < 0
         error('Error:arguments','integer is an [NX1] array of usigned, 32 bit integer values');
    end

    if size(int,2) > 1 || size(tchange,2) > 1 || size(int,1) ~= size(tchange,1)
        error('Error:dim','int and tchange are column vectors of equal length');
    end
else
    error('Error:arguments','The number of arguments provided is invalid');
end

% open file and write header
fid = fopen(strcat([filename,'.oldig']),'w');
tmake = datestr(now,31);
fprintf(fid,'%s \n',[filename  ' : ' tmake ' : a digital event file for use with Neurorighter.']);

if nargin == 4
    % Next log10 of max stim time
    t0 = t0*100000; % Convert to 100th of millisecond precision
    t1 = t1*100000; % Convert to 100th of millisecond precision
    otime = ceil(log10(max(t0)));
    
    % Conversion from bits to port integers and from on/off times to port
    % change times and write to file
    tchange = sort(unique([t1;t0]));
else
    tchange = tchange*100000;
    otime = ceil(log10(max(tchange)));
end

% Make c formating strings
cformat_t = strcat(['%0',num2str(otime + 1),'.0f \n']);
cformat_p = '%02.0f \n';

% How many times will port's state be updated?
numUp = length(tchange);
fprintf(fid,'%d \n',numUp);

% When will the last change occur?
finalTime = tchange(end)/100000;%Convert back to seconds
fprintf(fid,cformat_t,finalTime);

for i = 1:length(tchange)-1;
    
    if nargin == 4
        BitsOn = bit(t1<=tchange(i)&t0>tchange(i));
        PortOut = sum([2.^BitsOn 0]);
    else
        PortOut = int(i);
    end
    
    % write the digital event time
    fprintf(fid,cformat_t,tchange(i));
    
    % write the port state
    fprintf(fid,cformat_p,PortOut);
    
end

% save digital event time
fprintf(fid,cformat_t,tchange(end));

% zero the port
fprintf(fid,cformat_p,0);

%close the file
fclose(fid);

end