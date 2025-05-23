function [finalX,LX]=SimmonsFit(volts,current,x)

opts = statset('nlinfit');
opts.RobustWgtFun = 'bisquare';
opts.Robust='on';
opts.FunValCheck='on';

current=current/1e-12; %convert to pA

m=max(volts)*.75;
vt=volts(volts<m);
it=current(volts<m);
m=min(volts)*.9;
it=it(vt>m);
vt=vt(vt>m);

%  figure(4);clf
%  plot(vt,it);hold all
%  test=simmonsFormula2(x(:,1),vt);
%  plot(vt,test)
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
options = optimoptions('lsqcurvefit','Algorithm','levenberg-marquardt','TolFun',1e-8);
for KK=1:2
    x(:,1) = lsqcurvefit(@simmonsFormula2,x(:,1),vt,it,x(:,2),x(:,3),options);
end

LX=x(:,1);
% test=simmonsFormula(x(:,1),vt);
% idx=find(abs(vt)>.1);
x(:,1)= x(:,1)*1.05;
%
x0=x;
try
    x(:,1)=nlinfit(vt,it,@simmonsFormula2,x(:,1),opts);
catch mex
    m=max(volts)*0;
    vt=volts(volts<m);
    it=current(volts<m);
    try
        x=nlinfit(vt,it,@simmonsFormula2,x(:,1),opts);
    catch mex
        x=x0;
    end
end
finalX=x(:,1);
finalX(4)=abs(finalX(4));

end