%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%                 user controlled                   %%%%%%%%%%
%run DoAllPlots first
TopDirectory='\\BIOFS.asurite.ad.asu.edu\SMB\Research\Brian\Array_electrodes\2016_02_22 Damascene DNA';
sampleRate=20000;
desiredDir=2;
showFiltered=0;   %0 to remove filtered
controlDir='01_control';
baselineRemovalMethod=0;
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
clear controlDir

dirs=dir(TopDirectory);
dirNames={};
cc=1;
for I=3:length(dirs)
    if isempty(strfind(dirs(I).name,'.'))
        searchS=[TopDirectory '\' dirs(I).name '\*.abf']
        files = dir(searchS);
        if isempty(files)==false
            dirNames{cc}= dirs(I).name;
            if isempty(strfind(lower(dirs(I).name),'control'))==false %&& I~=3
                controlDir=dirNames{cc};
            end
            cc=cc+1;
        end
    end
end
clear dirs
dirLabels=sort(dirNames);
for I=1:length(dirLabels)
    dirNames{I}=[TopDirectory '\' dirLabels{I}]; %#ok<SAGROW>
    dirLabels{I}=strrep(dirLabels{I},'_',' ');
end

if exist('controlDir','var')
    files = dir([TopDirectory '\' controlDir '\*.abf']);
    controlTrace=[];
    for J=1:length(files)
        cT = abfload([TopDirectory '\' controlDir '\' files(J).name],'start',0);
        cT = cT(:,1);
        cT = cT(end*.1:end);
        cT=cT-mean(cT);
        controlTrace=[controlTrace cT']; %#ok<AGROW>
    end
    clear cT
    controlTrace=controlTrace';
end


files = dir([dirNames{desiredDir} '\*.abf']);
clear fileNames
for J=1:length(files)
    fileNames{J}=files(J).name; %#ok<SAGROW>
end
fileNames=sort(fileNames);

if isempty(strfind(lower(dirNames{desiredDir}),'control'))==false
    controlTrace=[];
    for J=1:length(files)
        cT = abfload([dirNames{desiredDir} '\' files(J).name],'start',0);
        cT = cT(:,1);
        cT = cT(end*.1:end);
        cT=cT-mean(cT);
        controlTrace=[controlTrace cT']; %#ok<AGROW>
    end
    clear cT
    controlTrace=controlTrace';
end


peakCollection={};
ccPC=1;

for J=1:length(fileNames)
    [shortData] = abfload([dirNames{desiredDir},'\',fileNames{J}],'start',0);
    shortData = shortData(:,1);
    shortData = shortData(floor(end*.1):end);
    %     shortData=shortData-mean(shortData(1:floor(.25*end)));
    
    
    if isempty(controlTrace)
        controlTrace=shortData(1:floor(.25*end));
    end
    signal=vertcat(controlTrace,shortData);
    output=WienerScalart96(signal,50000,length(controlTrace)/50000);
    clear signal
    output= output( length(controlTrace):end);
    if length(shortData)>length(output)
        shortData = shortData(1:length(output));
    end
    
    figure(10+J);clf;hold all
    shortDataS=smooth(shortData,100);
    plot((1:length(shortData))/sampleRate, shortDataS)
    if showFiltered==1
        t=output;
        %t(1:floor(end*.02))=0;
        plot((1:length(output))/sampleRate, smooth(t,50)*20)
        legend({'Raw','Filtered'});
    end
    xlabel('Time(s)');ylabel('Current (pA)');
    title('Raw data');
    
    background=[];
    backX=[];
    step=5000;
    ccB=1;
    background(ccB)=shortData(1);
    backX(ccB)=1;
    ccB=ccB+1;
    for I=2:step:length(shortData)-step
        t=shortData(I:(I+step-1));
        t2=output(I:(I+step-1));
        [v,idx]=min(t.*t2);
        background(ccB)=t(idx);
        backX(ccB)=I+idx-1;
        ccB=ccB+1;
    end
    background(ccB)=shortData(end);
    backX(ccB)=length(shortData);
    
    figure(20+J);clf;hold all
    t=interp1(backX,background,1:length(shortData));
    shortDataS=smooth(shortData-t',100);
    plot((1:length(shortData))/sampleRate, shortDataS)
    xlabel('Time(s)');ylabel('Current (pA)');
    title('Flat data');
    % ylim([-200 500])
    halfWindow = 100;
    sigma =  halfWindow/2;
    impulse =exp(-1*( (-halfWindow:halfWindow) ./sigma).^2);
    impulse=impulse/sum(impulse);
    peakFind=abs(output-mean(output(output<2)));
    peakFind(peakFind<3)=0;
    peakFind(peakFind>=3)=1;
    peakFind=conv(peakFind,impulse,'same');
    peakFind(peakFind~=0)=1;
    clear impulse
    clear sigma
    clear halfWindow
    
    
    idxP=find(peakFind~=0);
    
    didx=diff(peakFind);
    sIDX=find(didx==1);
    eIDX=find(didx==-1);
    clear didx
    justPeaks=shortData(idxP);
    idx=find(peakFind==0)';
    clear peakFind
    if length(idx)>100
        if baselineRemovalMethod==0 || length(idx)<1000
            if idx(1)~=1
                idx=[1 idx];
            end
            if idx(end)~=length(shortData)
                idx = [idx length(shortData)];
            end
            t=smooth( shortData(idx) ,1000);
            t= interp1(idx,t,1:length(shortData));
        else
            steps=1000;
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
                t=tt(I2:(I2+step-2));
                [v,idx2]=min(t);
                mins(I+2)=mean(t(max([ 1 (idx2-100)]):min([length(t) (idx2+100)])));
                minX(I+2)=x(idx2);
            end
            
            t=smooth(mins);
            t= interp1(minX,t,1:length(shortData));
            clear mins
            clear minX
        end
        
        justPeaks=justPeaks-t(idxP)';
        figure(30+J);clf;hold all
        plot((1:length(justPeaks))/sampleRate, justPeaks)
        lmax = tvdiplmax(justPeaks);
        x = tvdip(justPeaks,lmax*(1e-3),1,1e-3,10);
        plot((1:length(x))/sampleRate, x)
        xlabel('Time(s)');ylabel('Current (pA)');legend({'Cat Peaks','Leveled Peaks'});
        title('Concatenated Peaks');
        
        tt=shortData-t';
        
        for KK=1:min([length(eIDX) length(sIDX)])
            t=tt(sIDX(KK):eIDX(KK));
            if length(t)>2
                peakCollection{ccPC}=t;
                ccPC=ccPC+1;
            end
        end
        clear output;
        clear eIDX
        clear sIDX
        clear idx
        clear idx2
        clear idxP
        clear justPeaks
        clear t
        clear tt
        clear shortData
    end
    drawnow
end

clear lmax
clear ans
clear lengths
clear llengths
clear outMags
clear peakMag
clear peakMags
clear peakTrace2;
clear peakLoc
clear v
clear dx
clear bins
clear t
clear controlTrace
clear controlDir
clear fileNames
clear files
clear flatX
clear globalX
clear I
clear I2
clear J
clear K
clear KK
clear x
clear X
clear step
clear steps
clear peakTrace
clear globalExample
clear globalFlat
clear peakX
clear v
clear idx
clear idx2
clear idxP
clear I
clear t
clear x2
clear ccPC