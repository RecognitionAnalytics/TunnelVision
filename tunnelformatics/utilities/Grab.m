function [grabs]=Grab(traces)
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

grabs.t =  [];
grabs.D=[];


lastMax=0;
for I=1:length(traces)
    t= traces{I}.times-min(traces{I}.times)+lastMax;
    lastMax=max(t);
    traces{I}.timesC=t;
end
l=xlim;
for  I=1:length(traces)
    t=traces{I}.timesC;
    c=traces{I}.bShortData;
    t=t(1:length(c));
    
    if t(1)<l(1) && t(end)>l(2)
        sI=find(t>l(1));
        eI=find(t>l(2));
        grabs.t =  t(sI:eI);
        grabs.D =  c(sI:eI);
        break;
    end
end
figure
plot((grabs.t-min(grabs.t))*60,grabs.D);
end