function [figStart]=GlobalPlots(traces,figStart)
resolution=1;
colors={'b','g','r','k','m','c'};
%do a time correct low resolution plot
times=[];
minTime = 1e25;
for I=1:length(traces)
    m=min(traces{I}.times);
    if (m<minTime)
        minTime = m;
    end
    times(I)=m;
end
[~, idx]=sort(times);
traces=traces(idx);

%convert all the times to minutes
for I=1:length(traces)
    t= traces{I}.times-minTime;
    traces{I}.times=t/60;
end




lastMax=0;
for I=1:length(traces)
    t= traces{I}.times-min(traces{I}.times)+lastMax;
    lastMax=max(t);
    traces{I}.timesC=t;
end

t=[];
for I=1:length(traces)
    t(I)=mean(traces{I}.volts(:));
end

if isnan(std(t)) || std(t)>2e-4
    
    figure(figStart);
    clf;
    lDir=-1;
    for  I=1:length(traces)
        c=traces{I}.currents(:)./traces{I}.volts(:);
        c(c<50)=50;
        t=traces{I}.timesC(1:100:end);
        c=c(1:100:end);
        d=traces{I}.fileInfo.dirNumber;
        plot(t,c,colors{ mod( d, length(colors))+1});
        hold all
        plot(t,traces{I}.volts(1:100:end),colors{ mod( d, length(colors))+1});
        if lDir~=d
            dd=strrep(traces{I}.fileInfo.dirLabel,'_',' ');
            if mod(d,2)==0
                text(t(1), min(c)-300, dd);
            else
                text(t(1), max(c)+300, dd);
            end
        end
        lDir=d;
    end
    title('Conductance');
    xlabel('Time (min)');
    ylabel('Conductance (pS)/Driving Volts(mV)');
    figStart=figStart+1;
end


figure(figStart);
clf;hold all
title('Raw');
xlabel('Time (min)');
ylabel('Current (pA)');
lDir=-1;
for  I=1:length(traces)
    t=traces{I}.timesC(1:100:end);
    c=traces{I}.currents(1:100:end);
    v=traces{I}.volts(1:100:end)*1000;
    d=traces{I}.fileInfo.dirNumber;
    plot(t,c,colors{ mod( d, length(colors))+1});
    hold all
    plot(t,v,colors{ mod( d, length(colors))+1});
    if lDir~=d
        dd=strrep(traces{I}.fileInfo.dirLabel,'_',' ');
        if mod(d,2)==0
            text(t(1), min(c)-300, dd);
        else
            text(t(1), max(c)+300, dd);
        end
    end
    lDir=d;
end
axes('Position',[0.6,0.7,0.3,0.2])
for  I=1:length(traces)
    t=traces{I}.times(1:400:end);
    c=traces{I}.currents(1:400:end);
    d=traces{I}.fileInfo.dirNumber;
    plot(t,c,colors{ mod( d, length(colors))+1});
    hold all
end
xlabel('Real Time (min)');

figStart=figStart+1;



% figure(figStart);
% clf;hold all
% title('Denoised');
% xlabel('Time (min)');
% ylabel('Current (pA)');
% lDir=-1;
% for  I=1:length(examples)
%     try
%         t=examples{I}.Times(1:100:end);
%         c=examples{I}.flat(1:100:end);
%         r=examples{I}.response(1:100:end);
%         t=t(1:length(c));
%         d=examples{I}.Dir;
%         plot(t,c,colors{ mod( d, length(colors))+1});
%         hold all
%         if lDir~=d
%             dd=strrep(dirLabels{d},'_',' ');
%             if mod(d,2)==0
%                 text(t(1), min(c)-300, dd);
%             else
%                 text(t(1), max(c)+300, dd);
%             end
%         end
%         lDir=d;
%     catch mex
%     end
% end
% figStart=figStart+1;

% figure(figStart);
% clf;hold all
% title('Subtracted');
% xlabel('Time (min)');
% ylabel('Current (pA)');
% lDir=-1;
% for  I=1:length(traces)
%     try
%         t=traces{I}.timesC(1:resolution:end);
%         c=traces{I}.bShortData(1:resolution:end);
%         f=traces{I}.flat(1:resolution:end);
%         tf=t(1:length(f));
%         t=t(1:length(c));
%         d=traces{I}.fileInfo.dirNumber;
%         plot(t,c,colors{ mod( d, length(colors))+1});
%         hold all
%         plot(tf,f,colors{ mod( d+1, length(colors))+1});
%         if lDir~=d
%              dd=strrep(traces{I}.fileInfo.dirLabel,'_',' ');
%             if mod(d,2)==0
%                 text(t(1), min(c)-300, dd);
%             else
%                 text(t(1), max(c)+300, dd);
%             end
%         end
%         lDir=d;
%     catch mex
%     end
% end
% figStart=figStart+1;




% figure(figStart);
% clf;hold all
% title('Resonance');
% xlabel('Time (min)');
% ylabel('Current (pA)');
% lDir=-1;
% for  I=1:length(traces)
%     try
%         t=traces{I}.timesC(1:100:end);
%         c=smooth(traces{I}.response(1:100:end),150);
%
%         t=t(1:length(c));
%
%          d=traces{I}.fileInfo.dirNumber;
%         plot(t,c,colors{ mod( d, length(colors))+1});
%         hold all
%
%         if lDir~=d
%             dd=strrep(dirLabels{d},'_',' ');
%             if mod(d,2)==0
%                 text(t(1), min(c)-300, dd);
%             else
%                 text(t(1), max(c)+300, dd);
%             end
%         end
%         lDir=d;
%     catch mex
%     end
% end
% figStart=figStart+1;

figure(figStart);
clf;hold all
title('Denoised');
xlabel('Time (min)');
ylabel('Current (pA)');
lDir=-1;
for  I=1:length(traces)
    try
        t=traces{I}.timesC;
        c=traces{I}.bShortData;
        f=traces{I}.waveNoise;
        f2=traces{I}.TV;
        f=f((end-length(c)+1):end);
        f2=f2((end-length(c)+1):end);
        t=t(1:length(c));
        d=traces{I}.fileInfo.dirNumber;
        plot(t,c,colors{ mod( d, length(colors))+1});
        hold all
        plot(t,f,colors{ mod( d+1, length(colors))+1});
        plot(t,f2,colors{ mod( d+2, length(colors))+1});
        if lDir~=d
            dd=strrep(traces{I}.fileInfo.dirLabel,'_',' ');
            if mod(d,2)==0
                text(t(1), min(c)-300, dd);
            else
                text(t(1), max(c)+300, dd);
            end
        end
        lDir=d;
    catch mex
    end
end
axes('Position',[0.6,0.7,0.3,0.2])
cTime=0;
for  I=1:length(traces)
    try
        c=traces{I}.justPeaks;
        c2=traces{I}.JPTV;
        t=(1:length(c))/traces{I}.sampleRate/60+cTime;
        c=c(1:100:end);
        c2=c2(1:100:end);
        t=t(1:100:end);
        d=traces{I}.fileInfo.dirNumber;
        plot(t,c,colors{ mod( d, length(colors))+1});
        hold all
        plot(t,c2,colors{ mod( d+1, length(colors))+1});
        cTime=max(t);
    catch mex
    end
end
title('JustPeaks');
xlabel('Time (min)');
ylabel('Current (pA)');

figStart=figStart+1;
end