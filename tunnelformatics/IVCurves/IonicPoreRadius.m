function d=IonicPoreRadius(Gc,poreLength,KSigma,ionicStrength)
poreLength=poreLength/3;
a=4*poreLength/pi;
b=1;
c=-1*KSigma*ionicStrength/Gc;
x=zeros([1 2]);
x(1)=(-1*b+(b*b-4*c*a)^.5)/2/a;
x(2)=(-1*b-(b*b-4*c*a)^.5)/2/a;
x=1./x;
x(x<0)=[];
x(~isreal(x))=[];
d=min(real(x));


end