
clear all
cT = abfload('\\biofs.asurite.ad.asu.edu\SMB\Research\BrianAnalysis\Stacked Junctions\20160301 Keithley ionic tests\2016_03_01_withKeithley_0002.abf','start',0);
cT=cT(:,1);

fid = fopen('\\biofs.asurite.ad.asu.edu\SMB\Research\BrianAnalysis\Stacked Junctions\20160301 Keithley ionic tests\Keitley\PeiKeithleyWithRefWithAxon2.csv','r');


% InputText=textscan(fid,'%s',7,'delimiter','\n');
C_data0 = textscan(fid,'%f, %f, %f');
T =C_data0{1};
I = C_data0{2} ;

X=(1:length(cT))/20000;

% T= T-T(1);
t=T/1000;
t=t+(X(end)-t(end));
clf
hold all

plot(X,smooth(cT,100)-mean(cT))
plot(t,I-smooth(I,10000))
