function [figStart]=PlotHistograms(traces, figStart)

%do a time correct low resolution plot

[figStart]=exampleHist(traces,'currents','Raw','Current (pA)',figStart,false);

% [figStart]=exampleHist(examples,'flat','denoised','Current (pA)',figStart,true);

% [figStart]=exampleHist(examples,'bShortdata','Flattened','Current (pA)',figStart,false);

[figStart]=exampleHist(traces,'justPeaks','Just Peaks','Current (pA)',figStart,false);

[figStart]=exampleHist(traces,'peakMags','Spike amplitudes','Current (pA)',figStart,false);

% [figStart]=exampleHist(examples,'flatPeakMags','Level Jumps','Current (pA)',figStart,false);

[figStart]=exampleHist(traces,'levels','Levels','Current (pA)',figStart,false);

[figStart]=exampleHist(traces,'allLevels','All Peaks','Current (pA)',figStart,false);


end

