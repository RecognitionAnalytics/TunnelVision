function [I,V,cU,cL,slope]=ExtractData(I,V)

I=I-mean(I);

voltageLimit =.95*max(V);
idx=find(abs(V)<voltageLimit);
idx=idx(idx>.05*length(idx));
V=V(idx);
I=I(idx);
p=polyfit(V,I,1);
p=polyval(p,V);

idx=find(I>p);
Vu=V(idx);
Iu=I(idx);
p2=polyfit(Vu,Iu,3);
Iu=Iu-p2(4);
cU=p2(4);



idx=find(diff(V)<0);
Vl=V(idx);
Il=I(idx);
voltageLimit =.5*voltageLimit;
v=Vl(abs(Vl)<voltageLimit);
i=Il(abs(Vl)<voltageLimit);

f=fit(v,i,'poly1','Robust','Bisquare');

Il=Il-f.p2;


cL=f.p2;
slope=f.p1;

[V,idx]=sort(Vl);
I=Il(idx);
% V=Vl;
V=V(2:(end-2));
I=I(2:(end-2));
end