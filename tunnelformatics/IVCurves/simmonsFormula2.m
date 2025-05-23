function J=simmonsFormula2(p,V)




e=1.60217657e-19; % C
h=  4.135667516e-15;% eV*s
me=9.10938e-31; %kg
diE=8.8541878176e-12; %F/m

s=p(1)*10;
phi0=p(2);
phiE=phi0*e;
K=mod((abs(p(3))*4/3*s),8) ;
area=abs(p(4));
V=V+p(5);
Vn=abs(V);


% eV=Vn*e;
% sE=s*1e-10;
% lambda=e^2*log(2)/(8*pi*diE*K*sE);
% s1=1.2*lambda*sE/phiE;
% s2=(phiE-5.6*lambda)*(sE./eV);
% s2(Vn<phi0)=sE*(1-9.2*lambda./(3*phiE+4*lambda-2*eV(Vn<phi0)))+s1;
% dS=s2-s1;
%
%
% p1=(eV./(2*sE)).*(s1+s2);
% p2=(1.15*lambda*sE./(s2-s1));
% p3=log(abs(s2.*(sE-s1)./(s1*(sE-s2+1e-12))));
% phiI=(phiE-p1-p2.*p3)/e;
% dS=dS*1e10;
s1=6/(K*phi0);
s2=(phi0*K*s-28)./(K*Vn);
s2(Vn<phi0)=s*(1-46./(3*phi0*K*s+20-2*Vn(Vn<phi0)*K*s))+s1;

dS=s2-s1;

p1=(5.75/K./(s2-s1));
p2=(Vn./(2*s)).*(s1+s2);
p3=log(abs(s2.*(s-s1)./(s1*(s-s2+1e-12))));
phiI=phi0-p2-p1.*p3;

beta=1;
J0=e./(2*pi*h*(beta).^2)*1e16; % to A/cm^2/V
J= J0./dS.^2;
A=(4*pi*beta/h)*(2*me*.75/e)^.5*1e-10;%to angstroms and V

p1=phiI.*exp(-1*A*dS.*abs(phiI).^.5);
p2=(phiI-Vn).*exp(-1*A*dS.*(abs(phiI)+Vn).^.5);
J=J.*(p1-p2)*1e4*area.*sign(V);


end


% J0=e^2/(2*pi*h*dS^2);
% K=4*pi*dS/h*(2*me*e)^.5;
% V1=phi0;
% V2=phi0+abs(V);
% J=( V1.*exp(-1*K*V1.^.5) - V2.*exp( -K*V2.^.5));
% figure(3);clf;plot(J)
% J=p(6)*J0*J;
% figure(3);clf;plot(J)



%
%
% dS=p(1)*1e-9;
% phi0=p(2);
% E=abs(p(3));
% alpha=p(4);
% beta=p(5);
%
% J0=e/(2*pi*h*(beta*dS)^2);
% A=-(4*pi*beta*dS/h)*(2*me)^.5;
% J=( phi0*exp( A*alpha*phi0^.5) - (phi0+abs(V)).*exp(A*alpha*(phi0 + abs(V)).^.5) );
% figure(3);clf;plot(J)
% J=p(6)*J0*sign(V).*J;
% figure(3);clf;plot(J)
%
%  lambda = e^2*log(2)/(8*pi*E*diE*dS);
%  s1=1.2*lambda*dS/phi0;
%  s2=(dS*( 1-9.2*lambda./(3*phi0+4*lambda-2*abs(V)))+s1);
%
%  p1=abs(V).*(s2-s1)/(2*dS);
%  p2=1.15.*lambda.*dS./(s2-s1);
%
%  p3=abs(s2.*(dS-s1)./( s1.*(dS-s2)));
%  p3(isinf(p3))=1;
%  p3=log(p3);
%  phi0=phi0-p1-p2.*p3;
%
%
% J=p(6)*J0*sign(V).*( phi0.*exp( A*alpha*phi0.^.5) - (phi0+abs(V)).*exp(A*alpha*(phi0 + abs(V)).^.5) );
%
%
% J(isnan(J))=0;
% J(isinf(J))=0;