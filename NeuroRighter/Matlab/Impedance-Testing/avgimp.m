function [mu sd] = avgimp(f,z,badchan,fstd)
% AVGIMP  find the average impedance +-SD of an MEA at a given frequency,
% excluding statistical outliers
%
% AVGIMP(f,z,chan,fstd)
%       f = column vector of spike frequencies
%       z = column vector of either (1)channel numbers of (2) unit
%       numbers as direived from spike sorting for spikes of eqivalent
%       index in spiketimes
%       fstd = frequency standard for imp benchmark
%       badchan = channels to exclude.
%
%       Created by: Jon Newman (jnewman6 at gatech dot edu)
%       Location: The Georgia Institute of Technology
%       Created on: Nov 28, 2009
%       Last modified: Nov 30, 2009
%
%       Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt

if nargin < 7
    figtitle = '';
end
if nargin < 6 || isempty(pq)
    pq = 1;
end
if nargin < 5 || isempty(zbound)
    zbound = [7000 100000]; %acceptable imp bounds for std freq
end
if nargin < 4 || isempty(fstd)
    fstd = 1000;
end
if nargin < 3 || isempty(badchan)
    badchan = [1 8 33 57 64];
end
if nargin < 2
    error('Function requires first two input arguments');
end

chan = setdiff(1:64,badchan);

% estimate impedance measure at the std frequency
z = z(chan,:); % remove data from unwanted channels
[sortfreq sortind] = sort(f);
sortz = z(:,sortind);
ind = find(sortfreq > fstd,1,'first');
zstd = [];
for i = 1:length(chan)
    p = polyfit([sortfreq(ind-1) sortfreq(ind)], [sortz(i,ind-1) sortz(i,ind)],1);
    zstd = [zstd; p(1)*fstd + p(2)];
end

% Find outliers (Chauvenet's Criterion)
mu = mean(zstd);
sig = std(zstd);
X = (zstd-mu)/sig;
Y = normpdf(X,0,1);
CS = length(zstd)*Y;
zstd(CS < 0.5) = [];

% Calculate mu and sigma without ouliers
mu = mean(zstd);
sd = std(zstd);

end