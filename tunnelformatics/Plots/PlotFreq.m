function [figStart]=PlotFreq(traces,figStart)

%do a time correct low resolution plot

figure(figStart);
clf;

dLegend={};
dirs=cell(size(traces));
ccs=zeros(size(traces));
for  I=1:length(traces)
    dirs{I}=zeros( size(traces{I}.ffResponseW));
    ccs(I)=0;
end
maxD=0;
for  I=1:length(traces)
    c=traces{I}.ffResponseW;
    c(1)=c(end);
    d=traces{I}.fileInfo.dirNumber;
    dirs{d}=dirs{d}+c;
    ccs(I)=ccs(I)+1;
    if d>maxD
        maxD=d;
    end
    
end
for  I=1:maxD
    c=dirs{I};
    c(1)=c(end);
    t=(1:length(c))/length(c)*traces{I}.sampleRate/2000;
    semilogy(t,c+.001);%,colors{ mod( I, length(colors))+1});
    hold all
    dd=strrep(traces{I}.fileInfo.dirLabel,'_',' ');
    dLegend{I}=dd; %#ok<AGROW>
end
title('Multi Spectrum');
xlabel('Freq (kHz)');
ylabel('Current (pA)');
legend(dLegend)
figStart=figStart+1;

figure(figStart);
clf;

dLegend={};
dirs=cell(size(traces));
ccs=zeros(size(traces));
for  I=1:length(traces)
    dirs{I}=zeros( size(traces{I}.ffResponse));
    ccs(I)=0;
end
maxD=0;
for  I=1:length(traces)
    c=traces{I}.ffResponse;
    c(1)=c(end);
    d=traces{I}.fileInfo.dirNumber;
    dirs{d}=dirs{d}+c;
    ccs(I)=ccs(I)+1;
    if d>maxD
        maxD=d;
    end
    
end
for  I=1:maxD
    c=dirs{I};
    c(1)=c(end);
    t=(1:length(c))/length(c)*traces{I}.sampleRate/2000;
    semilogy(t,c+.001);%,colors{ mod( I, length(colors))+1});
    hold all
    dd=strrep(traces{I}.fileInfo.dirLabel,'_',' ');
    dLegend{I}=dd; %#ok<AGROW>
end
title('Short Spectrum');
xlabel('Freq (kHz)');
ylabel('Current (pA)');
legend(dLegend)
figStart=figStart+1;

end