function [dataTrace]=FindPeaks(dataTrace,baselineRemovalMethod,shortData,wFiltered)

dataTrace.bShortData=[];
dataTrace.justPeaks=[];
dataTrace.peakMags=[];
dataTrace.flatPeakMags=[];
dataTrace.levelJumps=[];
dataTrace.levels=[];
dataTrace.justPeaksIDX=[];
dataTrace.allLevels=[];
dataTrace.JPTV=[];
dataTrace.TV=[];
dataTrace.baselineDrift=0;

%quick and dirty peak finder using the weiner filtered data to find the
%active areas.
halfWindow = 100;
sigma =  halfWindow/2;
impulse =exp(-1*( (-halfWindow:halfWindow) ./sigma).^2);
impulse=impulse/sum(impulse);
peakFind=abs(wFiltered-mean(wFiltered(wFiltered<2)));
peakFind(peakFind<25)=0;
peakFind(peakFind>=25)=1;
idxP=find(peakFind==1);
peakFind=conv(peakFind,impulse,'same');
peakFind(peakFind~=0)=1;

didx=diff(peakFind);
sIDX=find(didx==1);
eIDX=find(didx==-1);

idx=find(peakFind==0)';
if length(idx)<100
    disp(length(idx));
end
if length(idx)>100
    if baselineRemovalMethod==0 || length(idx)<1000
        %basically assumes that the mean is the baseline
        if idx(1)~=1
            idx=vertcat(1, idx);
        end
        if idx(end)~=length(shortData)
            idx = vertcat(idx,length(shortData));
        end
        t=smooth( shortData(idx) ,1000);
        t= interp1(idx,t,1:length(shortData));
    else
        %extract the minimums and use those for the baseline
        steps=min([400 floor(length(idx)/2)]);
        minX=zeros([1 steps+1]);
        mins=zeros([1 steps+1]);
        minX(1)=.99;
        mins(1)=mean(shortData(1:100));
        minX(steps+1)=length(shortData)+.01;
        mins(steps+1)=mean(shortData((end-100):end));
        
        tt=shortData(idx);
        step=floor(length(tt)/steps);
        for I=0:steps-2
            I2=I*step+1;
            x=idx(I2:(I2+step-2));
            if isempty(x)==false
                t=tt(I2:(I2+step-2));
                [v,idx2]=min(t);
                mins(I+2)=mean(t(max([ 1 (idx2-100)]):min([length(t) (idx2+100)])));
                try
                    minX(I+2)=x(idx2);
                catch mex
                    disp(mex)
                end
                
            end
        end
        
        t=smooth(mins);
        t= interp1(minX,t,1:length(shortData));
        
    end
    bShortData=shortData-t';
    dataTrace.bShortData=bShortData;
    dataTrace.baselineDrift=std(t);
    %     v=min(bShortdata);
    %     dst = MRF_denoise(bShortdata+v,  500, .2, 10)-v;
    %
    %TV for a better peak find as well as smoothing
    lmax = tvdiplmax(bShortData);
    x = tvdip(bShortData,lmax*(1e-3),1,1e-3,40);
    dataTrace.TV=x;
    
    thresh=min([100 std(bShortData)]);%?
    thresh=100;
    peakFind(:)=0;
    peakFind(x>thresh)=1;
    peakFind(idxP)=1; %#ok<FNDSB>
    %expand the peaks a little to make sure to get a little bit of baseline
    %and any small hickups
    peakFind=conv(peakFind,impulse,'same');
    peakFind(peakFind~=0)=1;
    
    idxP=find(peakFind~=0);
    justPeaks=bShortData(idxP);
    dataTrace.justPeaksIDX=idxP;
    
    %get all the spikes
    [~, dataTrace.peakMags] = peakfinder(smooth(bShortData,10),1,thresh,1);
    [~, dataTrace.flatPeakMags] = peakfinder(smooth(abs(wFiltered),25),1,2,1);
    
    try
        if isempty(justPeaks)==false
            %find the level regions in the justpeaks trace
            lmax = tvdiplmax(justPeaks);
            x = tvdip(justPeaks,lmax*(1e-4),1,1e-3,100);
            dx=(diff(x));
            [l1, dataTrace.levelJumps] = peakfinder(dx,1,5,1);
            [l2] = peakfinder(dx,1,-3,-1);
            l=[l1' l2'];
            l=sort(l);
            mids=floor((l(2:end) + l(1:(end-1)))/2);
            mids(mids<1)=[];
            dataTrace.JPTV=x;
            dataTrace.levels=x(mids);
            dataTrace.justPeaks=justPeaks;
            %better graphs
            dataTrace.allLevels=vertcat(dataTrace.levels, dataTrace.peakMags);
        end
    catch mex
        disp(mex);
    end
end

end