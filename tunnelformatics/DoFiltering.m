function [traces]=DoFiltering(controlTraces,fileDictionary,dirNames,baselineRemovalMethod,doWavelet,firstTime)
global dirSeperator
traces=cell(size(fileDictionary));
for K=1:length(fileDictionary)
    fullName=[dirNames{fileDictionary(K).dirNumber} dirSeperator fileDictionary(K).fileName];
    
    %load the date, seperate into currents and voltages and get
    %the time stamp
    [shortData,dataTrace,sampleRate]=LoadABF(fullName,firstTime);
    %take off the background, not needed for the rest of the
    %calculations
    v=mean(shortData(floor(length(shortData)*.1):length(shortData)));
    shortData=shortData-v;
    dataTrace.background=v;
    dataTrace.sampleRate = sampleRate;
    dataTrace.fileInfo = fileDictionary(K);
    %do a wiener filter to determine areas where there is
    %action on the trace
    if fileDictionary(K).controlFile~=-1
        signal=vertcat(controlTraces{fileDictionary(K).controlFile}.trace ,shortData);
        wFiltered=WienerScalartBA(signal,sampleRate,length(controlTraces{fileDictionary(K).controlFile}.trace)/sampleRate);
        clear signal
        wFiltered= wFiltered( length(controlTraces{fileDictionary(K).controlFile}.trace):end);
    else
        %if there is no control trace, use the data trace to
        %determine the areas that are unusual
        cT=shortData(1:floor(length(shortData)*.1));
        signal=vertcat(cT,shortData);
        wFiltered=WienerScalartBA(signal,sampleRate,length(cT)/sampleRate);
        clear signal
        wFiltered= wFiltered( length(cT):end);
    end
    
    %the weiner algorythm tends to cut off the tail of the
    %trace
    if length(shortData)>length(wFiltered)
        shortData = shortData(1:length(wFiltered));
        dataTrace.times=dataTrace.times(1:length(wFiltered));
        dataTrace.volts=dataTrace.volts(1:length(wFiltered));
    end
    
    %do all the wavelet stuff, slow, slow slow
    if doWavelet==true
        dataTrace=WaveletResponse(dataTrace,shortData,wFiltered,controlTraces{fileDictionary(K).controlFile});
    end
    
    
    dataTrace.flat=wFiltered;
    
    %statistics
    w=1./(1+abs(v+wFiltered)).^.5;
    dataTrace.flatBaseline=v+sum(shortData(1:length(w)).*w)/sum(w);
    dataTrace.noise=2*mean(abs(shortData));
    dataTrace.stdnoise=std(shortData);
    dataTrace.flatNoise=std(wFiltered);
    
    % get the peak info
    dataTrace=FindPeaks(dataTrace,baselineRemovalMethod,shortData,wFiltered);
    dataTrace.currents=shortData;
    %record everything so it does not have to be done again.
    %     save([TopDirectory dirSeperator dirLabels{K} '_' fileNames{J} '.mat'],'dataExample');
    traces{K}=dataTrace;
    drawnow
end

end