c=10.^(-6:.2:1);
EstPoreLength=34e-9;
EstPoreDiameter=10e-9;
clear Gc
clear Gs
KSigma=10.5;
relativeMobility=1; %1 for KCl

for I=1:length(c)
    IonicConcentration=c(I);
    Gc(I)= 1e9* KSigma*IonicConcentration/(4*EstPoreLength/pi/ EstPoreDiameter^2+1/EstPoreDiameter);
    [~,Gs(I)]=IonicPoreRadiusSmeet(1e-9,EstPoreDiameter,EstPoreLength,KSigma,IonicConcentration,relativeMobility);
end
clf
% semilogx(c,Gc)
loglog(c,Gs)
hold all
loglog(c,Gc)
%
xlim([10^-6 5])
ylim([.05 500])
