% Jon Newman
% 2011-02-15
% Template for impedance reporting.

% 1. Load your impedance measurements
fid = 'C:\Users\Jon\Desktop\2011-01-25_Impedance-Test\2011-01-25_Impedance_14617.mat';
load(fid);
nogood = [1 8 33 57 64]; % channels to ignore

% 2. Plot the spectrum and return channels that do not meet bechmark of
% falling between 35e3 and 150e3 ohms for a 1KHz sine wave.
badc = impmeas(imp.f,imp.z,nogood,1000,[35e3 150e3]);

% 2. Find the avg. impedance at 1KHz excluding outliers
[mu std] = avgimp(imp.f,imp.z,nogood,1000);
disp(['Average impedance at 1000 Hz = ', num2str(mu) '+-' num2str(std) ' Ohms'])

% Save figure
saveas(gcf,[fid '.fig']);
export_fig(gcf,fid)

