function [shortData,dataExample,sampleRate]=LoadABF(fileName,firstTime)



[shortData,~,h]= abfload(fileName,'start',0);

sampleRate =  h.dataPtsPerChan /abs( h.recTime(2)-h.recTime(1));
t = (num2str(h.uFileStartDate));
year=str2num(t(1:4)); %#ok<*ST2NM>
month=str2num(t(5:6));
day=str2num(t(7:end));
secF=h.uFileStartTimeMS/1000;
minF=secF/60;
hourF=floor(minF/60);
minF=floor(minF-60*floor(minF/60));
secF=secF-(hourF*60*60+minF*60);
fileTime = (datenum(year,month,day,hourF,minF,secF)-firstTime) *24*60*60 ;%+h.uFileStartTimeMS/(1000*60*60*24);
dataExample.fileTime=fileTime;
dataExample.realFileTime=datenum(year,month,day,hourF,minF,secF);
dataExample.timeStamp=datestr(dataExample.realFileTime);
%fileTime=(fileTimes(J) -firstTime) *24*60*60 ;
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%the first part is almost always useless
shortData=shortData( floor(.1*size(shortData,1)):end,:);

unitA=h.recChUnits{1};
unitV=h.recChUnits{2};

scales=h.fInstrumentScaleFactor;
vtrace=shortData(:,2)*scales(2)/10;
vtrace(vtrace<50)=vtrace(vtrace<50)+.001.*sign(vtrace(vtrace<50));

dataExample.volts=vtrace;

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

shortData = shortData(:,1);

pTimes=fileTime+(1:length(shortData))/sampleRate;


dataExample.times=pTimes';

end