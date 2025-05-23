function [IVparams,curveDict, figStart]=IonicFitting(directory,EstPoreLengthNM,EstPoreDiameterNM,figStart,doPlots,searchString)
% directory='C:\Users\bashc\Dropbox (ASU)\Data_box_ASU\20160229_NALDB34_Chip03_22cyc_HIM3pC_UVozone10each_200deg_1mMPB_pH7_modified\For Brain analysis\M1-M2\before_IV';
% EstPoreLengthNM=50;
% EstPoreDiameterNM=10;
number_of_pores=1;
% figStart=1;
% doPlots=true;

EstPoreLength=EstPoreLengthNM*1e-9; %m
EstPoreDiameter=EstPoreDiameterNM*1e-9; %m

global dirSeperator
files = dir([directory dirSeperator '*.*']);
for I=1:length(files)
    fileNames{I}=files(I).name; %#ok<AGROW>
end
I=find(~cellfun(@isempty,strfind(lower(fileNames),searchString)));
fileNames=fileNames(I);
controlFiles=zeros(size(fileNames));
try
    controlFiles=~cellfun(@isempty,strfind(lower(fileNames),'control'));
    [controlFiles,idx]=sort(controlFiles,'descend');
    fileNames=fileNames(idx);
catch mex
end

%the cols where the data is seem to move around in csv files.  Change these if needed
voltageCol=6;
currentCol=3;

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
Gb=0;
CapBlocked=0;
Vblock=[];
Iblock=[];

for K=1:length(fileNames)
    try
        file=fileNames{K};
        global dt
        [I0,V0,dvdt,dt]=LoadFile(directory,file,voltageCol,currentCol);
        IVparam.file=file;
        %%%%% just a couple of defaults
        %Ionic concentrations.
        IonicConcentration=1000; %mM
        %KSigma=10.5;  % S/m %KCl 1M
        KSigma=9.5; % S/m % pb buffer 1M
        %relativeMobility=1; %1 for KCl
        relativeMobility=0.450; %#ok<NASGU> %PB phosphate
        %http://web.med.unsw.edu.au/phbsoft/mobility_listings.htm
        buffer='KCl';
        if isempty(strfind(lower(file),'mm'))==false
            parts=strsplit(file,{' ','_','-'});
            for I=1:length(parts)
                if isempty(strfind(lower(parts{I}),'mm'))==false
                    p=parts{I};
                    IonicConcentration = sscanf(p, '%g', 1)/1000;
                    if isempty(strfind(lower(parts{I}),'kcl'))==false
                        KSigma=10.5;  % S/m %KCl 1M
                        relativeMobility=1; %1 for KCl
                        buffer='KCl';
                    else
                        KSigma=9.5; % S/m % pb buffer 1M
                        relativeMobility=0.450; %PB phosphate
                        buffer='PB';
                    end
                end
            end
        end
        if isempty(strfind(lower(file),'numpores'))==false
            parts=strsplit(file,{' ','_','-'});
            for I=1:length(parts)
                if isempty(strfind(lower(parts{I}),'numpores'))==false
                    number_of_pores = sscanf(parts{I}, '%g', 1)/1000;
                end
            end
        end
        
        
        
        IVparam.IonicConcentration=IonicConcentration;
        IVparam.KSigma=KSigma;
        IVparam.buffer=buffer;
        [Current,Volts,cU,cL,Conductance]=ExtractData(I0,V0);
        IVparam.Conductance=Conductance;
        %determine the capacitance of the trace
        CapT=(cU-cL)/2/dvdt;
        %estimate the resistance from the solution
        IVparam.Est_Solution_Resistance=1/(  KSigma*IonicConcentration^.5/30/(4*(2e-2)/pi/ (EstPoreDiameterNM)^2));
        
        %record the fit parameters of the curve
        try
            curveDict.(['B_' buffer '_' strrep(num2str(IonicConcentration),'.','_')])=polyfit(V0,I0,4);
        catch mex
            
        end
        try
            %model the time evolution of the circuit if there is a
            %capacitor and resistor in parallel with resistance in series
            [SimCurrent,cP]=CalcResistance(V0,I0,CapT*1e9,IVparam.Est_Solution_Resistance*1e-9,(1/Conductance)*1e-9);
            IVparam.Model_Pore_Conductance =1/cP(3);
            IVparam.Model_Pore_Capacitance =cP(1);
            IVparam.Model_Solution_Resistance =cP(2);
            
        catch mex
            disp(mex)
            IVparam.Model_Pore_Conductance =(1/Conductance);
            IVparam.Model_Pore_Capacitance =CapT;
            IVparam.Model_Solution_Resistance =IVparam.Est_Solution_Resistance;
        end
        IVparam.Capacitance=CapT;
        
        m=max(Volts)*.9;
        Current=Current(abs(Volts)<m);
        Volts=Volts(abs(Volts)<m);
        
        %if control then convert all into the control names ones
        if controlFiles(K)==1
            try
                IVparam.IonicConcentration_Blocked=IonicConcentration; %mM
                Ia0=I0;
                Va0= V0;
                Iblock=Current;
                Vblock=Volts;
                IVparam.Conductance_Blocked=Conductance;
                m=max(Vblock)*.9;
                Iblock=Iblock(abs(Vblock)<m);
                Vblock=Vblock(abs(Vblock)<m);
                
                IVparam.Capacitance_Blocked=CapT;
                IVparam.PoreSize_Blocked=IonicPoreRadius(IVparam.Conductance_Blocked,EstPoreLength,KSigma,C_ionsControl);
            catch mex
            end
        end
        
        %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        IVparam.Est_Pore_Conduct= number_of_pores* KSigma*IonicConcentration/(4*EstPoreLength/pi/ EstPoreDiameter^2+1/EstPoreDiameter);
        IVparam.number_of_pores=number_of_pores;
        %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        IVparam.Calculated_PoreSize=IonicPoreRadius(Conductance/number_of_pores,EstPoreLength,KSigma,IonicConcentration);
        IVparam.Calculated_Model_PoreSize=IonicPoreRadius(1e-9/IVparam.Model_Pore_Conductance/number_of_pores,EstPoreLength,KSigma,IonicConcentration);
        %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        if isempty(Vblock)==false
            IVparam.Calculated_PoreSize_Corrected=IonicPoreRadius(Conductance/number_of_pores-IVparam.Conductance_Blocked,EstPoreLength,KSigma,IonicConcentration);
            IVparam.Calculated_Model_PoreSize_Corrected=IonicPoreRadius(1e-9/IVparam.Model_Pore_Resistance/number_of_pores-IVparam.Conductance_Blocked,EstPoreLength,KSigma,IonicConcentration);
            
            [IVparam.Calculated_PoreSize_Smeet_Corrected , IVparam.Est_SmeetConductance]=IonicPoreRadiusSmeet(Conductance/number_of_pores-IVparam.Conductance_Blocked,EstPoreDiameter,EstPoreLength,KSigma,IonicConcentration,relativeMobility);
            IVparam.Calculated_Model_PoreSize_Smeet_Corrected=IonicPoreRadiusSmeet(1e-9/IVparam.Model_Pore_Resistance/number_of_pores-IVparam.Conductance_Blocked,EstPoreDiameter,EstPoreLength,KSigma,IonicConcentration,relativeMobility);
            IVparam.Capacitance_Corrected=CapT-IVparam.Capacitance_Blocked;
        else
            IVparam.Calculated_PoreSize_Smeet=IonicPoreRadiusSmeet(Conductance/number_of_pores,EstPoreDiameter,EstPoreLength,KSigma,IonicConcentration,relativeMobility);
            [IVparam.Calculated_Model_PoreSize_Smeet, IVparam.Est_SmeetConductance]=IonicPoreRadiusSmeet((1e-9/IVparam.Model_Pore_Conductance)/number_of_pores,EstPoreDiameter,EstPoreLength,KSigma,IonicConcentration,relativeMobility);
        end
        
        if doPlots
            [figStart]=PlotRoutine(Volts, Current,I0,V0, Vblock, Iblock, IVparam,SimCurrent,figStart);
        end
        drawnow
        IVparams(K)=IVparam; %#ok<AGROW>
    catch mex
    end
end
end

function [figStart]=PlotRoutine(Volts, Current,I0,V0, VoltsBlocked, CurrentBlocked, IVparam,SimCurrent,figStart)
figure(figStart);
clf; hold all;

% plotLoopless( Volts*1000,Current*1e9,'b');
plot(Volts*1000,Current*1e9,'b.')

str1=sprintf('---Experiment-----\n');
str1=sprintf('%s Conductance=%0.2f / %0.2f nS\n',str1,IVparam.Conductance*1e9,IVparam.Model_Pore_Conductance);
str1=sprintf('%s Capacitance=%0.2f / %0.2f nF\n',str1,IVparam.Capacitance*1e9,IVparam.Model_Pore_Capacitance);
str1=sprintf('%s*Calc Pore size=%0.2f / %0.2f nm\n',str1,IVparam.Calculated_PoreSize*1e9,IVparam.Calculated_Model_PoreSize*1e9 );

if isempty(VoltsBlocked)==false
    plotLoopless( VoltsBlocked*1000,CurrentBlocked*1e9,'g');
    str1=sprintf('%s*Corrected pore size=%0.2f nm\n',str1,IVparam.Calculated_PoreSize_Corrected*1e9);
    str1=sprintf('%s*Calc Smeet pore size=%0.2f nm\n',str1,IVparam.Calculated_PoreSize_Smeet_Corrected*1e9);
    %     str1=sprintf('%s Parasitic pore size=%0.2f nm\n',str1,d_blocked*1e9);
    %     str1=sprintf('%s Parasitic capacitance: %0.2f nF\n',str1,CapBlocked*1e9);
    %     str1=sprintf('%s Parasitic conductance: %0.2f nS\n',str1,1e9*Gb);
else
    str1=sprintf('%s*Calc Smeet pore size=%0.2f / %0.2f nm\n',str1,IVparam.Calculated_PoreSize_Smeet*1e9,IVparam.Calculated_Model_PoreSize_Smeet*1e9);
end


plot(V0*1000,I0*1e9,'b')
if exist('Va0','var')
    plot(Va0*1000,Ia0*1e9,'g')
end

b=xlim;
bY=ylim;
IE=Volts*IVparam.Est_Pore_Conduct*1e9;
VE=Volts*1000;
plot(VE,IE,'r');

IE=Volts* IVparam.Est_SmeetConductance*1e9;
VE=Volts*1000;
plot(VE,IE,'k');

plot(V0*1000,SimCurrent,'g');

xlim(b)
ylim(bY);
hold off;
[~,f]=fileparts(IVparam.file);
title( strrep(f,'_',' ') );
xlabel('Voltage (mV)');
ylabel('Current (nA)');


% str1=sprintf('%s---Parameters-----\n Thickness= %0.0f nm\n',str1,EstPoreLength*1e9);
% str1=sprintf('%s Diameter= %0.0f nm\n Number Pores= %d \n',str1,EstPoreDiameter*1e9,number_of_pores);
% str1=sprintf('%s Molarity= %0.0f mM\n',str1,IonicConcentration*1000);
% str1=sprintf('%s Conductivity= %0.1f S/m\n',str1,KSigma);

str1=sprintf('%s---Model-----\n',str1);% Conductance=%0.2f/%0.2f nS\n',str1, Est_Conduct*1e9, Gs*1e9);

str1=sprintf('%s S Cap:%0.2f nF\n S R1:%0.2f gOhm\n S R2:%0.2f gOhm\n',str1,IVparam.Model_Pore_Capacitance,1/IVparam.Model_Pore_Conductance,IVparam.Model_Solution_Resistance);
text(0.05,.75,str1,'HorizontalAlignment','left','Units','normalized')
%     text(min(Volts)*750,b(1)*.75,str1);
if isempty(VoltsBlocked)==false
    legend({'Pore','Blocked','Raw','Access Pore','Smeet pore','Equiv.'});
else
    legend({'Pore','Raw','Access Pore','Smeet pore','Equiv.'});
end

handaxes3 = axes('Position', [0.68 0.2 0.2 0.2]);
plotLoopless( Volts*1000,Current*1e9/IVparam.IonicConcentration,'b');
if isempty(VoltsBlocked)==false
    hold all
    plotLoopless( VoltsBlocked*1000,CurrentBlocked*1e9/IVparam.IonicConcentration_Blocked,'g');
end
set(handaxes3, 'Box','off')
%xlabel('mV')
ylabel('*nA at 1M')

figStart=figStart+1;

end
