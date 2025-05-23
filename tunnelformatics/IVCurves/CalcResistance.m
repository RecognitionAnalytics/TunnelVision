function [SimCurrent,x]=CalcResistance(V0,I0,C,R1,R2)

opts = statset('nlinfit');
opts.RobustWgtFun = 'bisquare';
opts.Robust='on';
opts.FunValCheck='on';

p(1)=C;
p(2)=R1;
p(3)=R2;
p(4)=0;

lb(1)=.01;
lb(2)=.001;
lb(3)=.05;
lb(4)=-2;

ub(1)=15;
ub(2)=.5;
ub(3)=5;
ub(4)=2;

idx=find(diff(V0)<0);
%  idx=1:floor(idx(1)/2);
%   idx=1:(idx(1)+45);
idx=1:(idx(1)+15);
%   idx=1:length(V0);
%  idx=1:floor(idx(1)*1.5);
options = optimoptions('lsqcurvefit','Algorithm','levenberg-marquardt','TolFun',1e-12);
x = lsqcurvefit(@SimulateIonic,p,V0(idx),I0(idx)*1e9,lb,ub,options);
%  x = lsqcurvefit(@SimulateIonic,p,V0(idx),I0(idx)*1e9);
fprintf('Cap: %0.2f \n R1: %0.2f\n R2: %0.2f\n',x(1),x(2),x(3));

% figure(1)
% clf;hold all
%
% plot(V0,I0*1e9);

[SimCurrent]=SimulateIonic(x,V0);
% plot(V0,iC);
% x2=nlinfit(V0,I0,@SimulateIonic,x,opts);
% [iC]=SimulateIonic(x2,V0);
% plot(V0,iC);
% x(1)=4.7;
% x(2)=1;
% x(3)=1;
end