function [dataExample]=WaveletResponse(dataExample,shortData,output,controlC)

%resampling to 1/100 seems to be a good level for sensing features in the
%data, no rescale results in too small a window
shorterData=resample(shortData,1,100);
cc=0;
ffResponseW=zeros([1 512]);
for I=1:512:length(shorterData)-510
    t=abs(fft(shorterData(I:I+511)))';
    w=abs(max(abs(output(I:I+511))));
    ffResponseW=ffResponseW+t*w;
    cc=cc+w;
end
t2=ffResponseW(end:-1:(floor(end/2)+2));
ffResponseW=ffResponseW(1:floor(end/2));
ffResponseW(2:end)=(ffResponseW(2:end)+t2 )/2;
ffResponseW(1)=t2(1);


ffResponse=ffResponseW/cc;



%capture a number of different scales into one by changing the sampling
%scale over a number of orders of magnitude
wResponse=zeros([1 512]);
cc2=0;
for J=1:4
    if J~=1
        shorterData=resample(shortData,1,10^J);
    else
        shorterData=shortData;
    end
    cc=0;
    ffResponseW=zeros([1 512]);
    for I=1:512:length(shorterData)-510
        t=abs(fft(shorterData(I:I+511)))';
        w=abs(mean(shorterData(I:I+511)));
        ffResponseW=ffResponseW+t*w;
        cc=cc+w;
    end
    if cc~=0
        wResponse=wResponse+ffResponseW/cc;
        cc2=cc2+1;
    end
end
t2=wResponse(end:-1:(floor(end/2)+2));
wResponse=wResponse(1:floor(end/2));
wResponse(2:end)=(wResponse(2:end)+t2 )/2;
wResponse(1)=t2(1);



%this is a wavelet denoise, the wavelets are cleaned up according to the
%control noise, and then the inverse wavelet transform is performed
% sig = {shortData(1:floor(length(shortData)*.1)),controlC.dt};
sig = {shortData,controlC.dt};
cwtsig = cwtft(sig,'scales',controlC.scales,'wavelet',controlC.wav);

sigma=controlC.cfs;
response=zeros([1 size( cwtsig.cfs,2)]);
%the higher the alpha, the more denoised the trace is
alpha=.5;

for I=1:size(cwtsig.cfs,2)
    Yf = cwtsig.cfs(:,I);
    Pyf = abs(Yf).^2;
    
    W=((1-alpha)*Pyf-alpha*sigma)./Pyf;
    W(W<0)=0;
    spec = (W.*Yf);
    spec(isnan(W))=0;
    cwtsig.cfs(:,I)=spec;
    spec=abs(spec);
    %determine the dominate frequency
    response(I)=abs(sum(controlC.scales.*spec')/(.1+sum(spec)));
end
sigrec = icwtft(cwtsig,'signal',sig);

coefs=zeros([3 size(cwtsig.cfs,2)]);
%sample a few of the wavelet scales to get an approximate frequency
%response. Convert from imaginary
coefs(1,:)=abs(cwtsig.cfs(1,:));
coefs(2,:)=abs(cwtsig.cfs(5,:));
coefs(3,:)=abs(cwtsig.cfs(10,:));

dataExample.response=response';
dataExample.waveNoise=sigrec';
dataExample.coefs2=coefs';
dataExample.ffResponse=ffResponse;
dataExample.ffResponseW=wResponse;
end