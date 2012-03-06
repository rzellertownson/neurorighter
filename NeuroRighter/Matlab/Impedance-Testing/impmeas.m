function badz = impmeas(f,z,badchan,fstd,zbound,pq,figtitle)
% IMPMEAS plot mea impedance data
%
% IMPMEAS(f,z,chan,fstd,zbound,pq,figtitle)
%       f = column vector of spike frequencies
%       z = column vector of either (1)channel numbers of (2) unit
%       numbers as direived from spike sorting for spikes of eqivalent
%       index in spiketimes
%       fstd = frequency standard for imp benchmark
%       badchan = channel.
%       zbound = bounry
%       pq = should I plot the impedance data (1/0, 'yes'/'no')
%       figtitle = figure title
%
%       Created by: Jon Newman (jnewman6 at gatech dot edu)
%       Location: The Georgia Institute of Technology
%       Created on: Nov 28, 2009
%       Last modified: Nov 30, 2009
%
%       Licensed under the GPL: http://www.gnu.org/licenses/gpl.txt

% check number and type of arguments
if nargin < 7
    figtitle = 'Impedance Spectra';
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

% plot log-log impedance measure if user requests
col = jet(size(z,1));
if pq == 1 || strcmp(pq,'y')
    
    ty = [1];
    for i = 1:length(chan)
        
        % Spectra
        subplot(1,5,1:4)
        loglog(f,z(chan(i),:),'color',col(i,:));
        grid on
        hold on
        
        % Legend
        subplot(1,5,5)
        hold on
        plot([0 1], [ty(end) ty(end)],'color',col(i,:));
        text(1.2,ty(end), ['ch.' num2str(chan(i))],'fontsize',9);
        ty = [ty ty(end)+1];
        xlim([0 2])
        set(gca,'XTick',[],'YTick',[])
        
    end
    
    % labels
    subplot(1,5,1:4)
    ylabel('Impedance (Ohms)','fontsize',12)
    xlabel('Frequency (Hz)','fontsize',12)
    title(figtitle,'fontsize',12,'Interpreter','none')
    
    subplot(1,5,5)
    title(['$Red \; \Rightarrow \; z_i \notin [' num2str(zbound(1)) '\;' num2str(zbound(2)) ']\;Ohms\;@\;' num2str(fstd) '\;Hz$'],'Interpreter','Latex');
end
grid on


% find analmolous impedance measures at a benchmark frequency fstd
z = z(chan,:); % remove data from unwanted channels
[sortfreq sortind] = sort(f);
sortz = z(:,sortind);
ind = find(sortfreq > fstd,1,'first');
badz = [];
for i = 1:size(z,1)
    p = polyfit([sortfreq(ind-1) sortfreq(ind)], [sortz(i,ind-1) sortz(i,ind)],1);
    zstd = p(1)*fstd + p(2);
    if zstd < zbound(1) || zstd > zbound(2)
        
        % Replot the bad channel as black
        subplot(1,5,5)
        hold on
        plot([0 1], [ty(i) ty(i)],'color',col(i,:));
        text(1.2,ty(i), ['ch.' num2str(chan(i))],'fontsize',9,'Color',[1 0 0]);
        xlim([0 3])
        set(gca,'XTick',[],'YTick',[])
        
        
        disp(strcat(['Channel ' num2str(chan(i)) ' does not meet impedance benchmark...']));
        disp(strcat(['impedance of ' num2str(zstd) ' Ohms at ' num2str(fstd) ' Hz']));
        badz = [badz; [chan(i) zstd/1000000]]; % bad channel versus impedance at fstd in MOhms
    end
end


end