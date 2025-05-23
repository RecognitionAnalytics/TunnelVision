function [diameter, conductance]=IonicPoreRadiusSmeet(Gc,diameter,poreLength,KSigma,ionicStrength,relMobility)
%   poreLength=  34*1e-9;
%   ionicStrength=1e-1;
% relMobility=1;
% KSigma=10.5;
%   ionicStrength=10.^(-6.1:.1:0);
diameter=diameter*1e9;
l=poreLength*1e9;

sigma=1.5*interp1([-6 -5 -3 -1 1],[1 2 9 25 25],log(ionicStrength)/log(10),'spline')*1000;
% clf
% loglog(ionicStrength,sigma/1000);
% semilogy(log(ionicStrength)/log(10),sigma)
% ylim([1 300])
%   xlim([1e-6 1]);
A=pi/(4*l);
%  B=((7.616e-8)+(7.909e-8))*ionicStrength*1.60217662e-19 * 6.02214078e23/0.001 ;
B=KSigma*ionicStrength;
C=4*950*sigma*(7.616e-8)*relMobility;

conductance=A*diameter^2*(B+C/diameter);


% clf
% loglog(ionicStrength,G);
% semilogy(log(ionicStrength)/log(10),sigma)
% ylim([1 300])
%   xlim([5e-7 5]);

conductance=conductance*1e-9;


a=A*B;
b=A*C;
c=-1e9*Gc;
x=zeros([1 2]);
x(1)=(-1*b+(b*b-4*c*a)^.5)/2/a;
x(2)=(-1*b-(b*b-4*c*a)^.5)/2/a;

x(x<0)=[];
x(~isreal(x))=[];
diameter=min(real(x))*1e-9;


end