function [controlTraces]=LoadControl( dirNames , fileDictionary)
global dirSeperator
dt = 0.05; %just bull numbers because we do not care about real values
s0 = 2*dt;
ds = 0.4875;
NbSc = 20;
wname = 'morl';
sca = {s0,ds,NbSc};
wave = {wname,[]};

controlTraces=cell(size(fileDictionary));
for K=1:length(fileDictionary)
    if fileDictionary(K).isControl==true
        fileName = [dirNames{fileDictionary(K).dirNumber} dirSeperator fileDictionary(K).fileName];
        [shortData,~,~]= abfload(fileName,'start',0);
        
        sig = { shortData(:,1),dt};
        clear shortData;
        cwtsig = cwtft(sig,'scales',sca,'wavelet',wave);
        
        cwtsig.cfs=mean(abs(cwtsig.cfs).^2,2);
        cwtsig.omega=mean(cwtsig.omega);
        cwtsig.trace=sig{1};
        controlTraces{K}=cwtsig;
    end
end


end