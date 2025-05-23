function [IVparams,curveDict,figStart]=simmons2(directory,doPlots,figStart)
global dirSeperator

%directory where the IV curves are
%
% directory='C:\Users\bashc\Dropbox (ASU)\Data_box_ASU\20160229_NALDB34_Chip03_22cyc_HIM3pC_UVozone10each_200deg_1mMPB_pH7_modified\For Brain analysis\M1-M2\before_IV';
% doPlots=true;

%getting the area right is important.
IVparam.junctionLength = 4; %um
IVparam.junctionWidth = 80; %nm
gapSize=2; %nm

%the cols where the csv data is stored.  Change these if needed
voltageCol=6;
currentCol=3;

files = dir([directory dirSeperator '*.*']);
for I=1:length(files)
    fileNames{I}=files(I).name; %#ok<AGROW>
end
I=find(~cellfun(@isempty,strfind(lower(fileNames),'tunneling')));
fileNames=fileNames(I);
controlFiles=zeros(size(fileNames));
try
    controlFiles=(~cellfun(@isempty,strfind(lower(fileNames),'control'))) + (~cellfun(@isempty,strfind(lower(fileNames),'open')));
    [controlFiles,idx]=sort(controlFiles,'descend');
    controlFiles=find(controlFiles==1);
    fileNames=fileNames(idx);
catch mex
end

%your best estimates.  it is set for Pd with Al2O3 gap
potentialGap=3.02; %eV

%%%%%%%%%%%%%%%  end %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
DielectricConstant=1; %correction to alumina value (4e0/3nm) M.D. Groner , J.W. Elam , F.H. Fabreguette , S.M. George
jA=IVparam.junctionLength*1e-3*IVparam.junctionWidth; %um^2

%     starting             low      high     literature
x=[ [gapSize                1        6        gapSize ]; ... %gap
    [potentialGap           2       3.5      3.1     ]; ...%barrier
    [DielectricConstant     .85       1.2      1       ];...%dielectric
    [jA                   jA*.75     jA*10    jA      ];... %area correction %nm^2
    [0                   -0.1e-4     0.1e4    0       ]];%x offset

IVparam.Geo_JunctionArea=IVparam.junctionLength*1e-6*IVparam.junctionWidth*1e-9; %m^2



%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%Get background information
Conduct_Air=0;
CapAir=0;
Vair=[];
Iair=[];
Ia0=[];
Va0=[];
if isempty(controlFiles)==false
    [Ia0,Va0,dvdt]=LoadFile(directory,fileNames{controlFiles(1)},voltageCol,currentCol);
    [Iair,Vair,cUb,cLb,Conduct_Air]=ExtractData(Ia0,Va0);
    CapAir=(cUb-cLb)/(2*dvdt); %A/s
end
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

for K=1:length(fileNames)
    
    IVparam.file=fileNames{K};
    
    IonicConcentration='1'; %mM
    buffer='air';
    if isempty(strfind(lower(  IVparam.file),'mm'))==false
        parts=strsplit(  IVparam.file,{' ','_','-'});
        for I=1:length(parts)
            if isempty(strfind(lower(parts{I}),'mm'))==false
                p=parts{I};
                parts2=strsplit(lower(p),'mm');
                IonicConcentration =parts2{1};
                if isempty(strfind(lower(parts{I}),'kcl'))==false
                    buffer='KCl';
                else
                    buffer='PB';
                end
            end
        end
    end
    
    
    [I0,V0,dvdt]=LoadFile(directory,IVparam.file,voltageCol,currentCol);
    [Current,Volts,cU,cL,Conduct]=ExtractData(I0,V0);
    
    IV_entry.buffer = ['B_' buffer '_' IonicConcentration];
    IV_entry.filename =IVparam.file;
    IV_entry.fit=polyfit(Volts,Current,4);
    curveDict.(IV_entry.buffer)=IV_entry;
    
    IVparam.Current_Shift=(cU+cL)/2;
    %make a plot of the raw data
    IVparam.Conductance=Conduct;
    IVparam.Capacitance=(cU-cL)/2/dvdt;
    %correct for parrallel parasitic capacitance
    IVparam.Capacitance_Corrected=IVparam.Capacitance-CapAir;
    
    if isempty(Vair)==false
        Current_Corrected=Current-polyval([Conduct_Air 0],Volts);
    else
        pt=[5.37235092786435e-12,0];
        Current_Corrected=Current-polyval(pt,Volts);
    end
    
    try
        [NonLinearX,LeastSquaresX]=SimmonsFit(Volts,Current_Corrected,x);
    catch mex
        NonLinearX=zeros(size(x))+1;
        LeastSquaresX=NonLinearX;
    end
    area=NonLinearX(4)*1e-12;%m^2
    area2=LeastSquaresX(4)*1e-12;%m^2
    w=area*1e18/max([IVparam.junctionWidth IVparam.junctionLength*1000]);
    w2=area2*1e18/max([IVparam.junctionWidth IVparam.junctionLength*1000]);
    
    IVparam.NL_Gap=NonLinearX(1);
    IVparam.LS_Gap=LeastSquaresX(1);
    
    IVparam.NL_Potential=NonLinearX(2);
    IVparam.LS_Potential=LeastSquaresX(2);
    
    IVparam.NL_Dielectric=NonLinearX(3)*NonLinearX(1)*4/3;
    IVparam.LS_Dielectric=LeastSquaresX(3)*LeastSquaresX(1)*4/3;
    
    IVparam.NL_Tunneling_Area=area*1e12;
    IVparam.LS_Tunneling_Area=area2*1e12;
    
    IVparam.NL_Nanowire_Width=w;
    IVparam.LS_Nanowire_Width=w2;
    
    
    d=(NonLinearX(1)*1e-9);
    %     KK=NonLinearX(3)*NonLinearX(1)*4/3;
    %     KI=(DielectricConstant*4/3*gapSize);
    %     eKe0_=KI*8.854e-12;
    %     eKe0=KK*8.854e-12;
    %     areaCap_=IVparams.Capacitance*gapSize*1e-9/eKe0_; %m^2
    %     areaCap=IVparams.Capacitance*d/eKe0; %m^2
    %     idealCap= eKe0_*IVparams.junctionArea/(gapSize*1e-9)*1e15;
    %     idealCap2= pi* eKe0_*IVparams.junctionLength*(1e-6)/log( (gapSize*1e-9) / (4e-6*IVparams.junctionWidth*1e-9))*1e15;
    %     HeightN=2*d*exp(-1*pi*eKe0/(IVparams.Capacitance_Corrected/(IVparams.junctionLength*1e-6)))*1e12;%nm
    sigma=(IVparam.Conductance-Conduct_Air)*area/d;
    
    %     IVparams.Geo_JunctionArea=junctionArea;
    IVparam.Geo_JunctionGap=( gapSize);
    
    IVparam.Measured_Capacitance=(IVparam.Capacitance*1e12);
    IVparam.Measured_Resistivity=(1/sigma);
    IVparam.MinArea=min([area area2]);
    if doPlots && sum( controlFiles ==K)==0
        
        simCurrent{1} = simmonsFormula2(NonLinearX,Volts)*1e-12/(area*1e4);
        simCurrent{2} = simmonsFormula2(LeastSquaresX,Volts)*1e-12/(area*1e4);
        simCurrent{3} = simmonsFormula2(x(:,4),Volts)*1e-12/(IVparam.Geo_JunctionArea*1e4);
        
        [figStart]=PlotRoutine(IVparam,I0,V0,Ia0,Va0,Iair,Vair,Volts, Current,Current_Corrected ,simCurrent,figStart);
    end
    IVparams(K)=IVparam;
end
end

function [figStart]=PlotRoutine(IVparam,I0,V0,Ia0,Va0,Iair,Vair, Volts, Current,Current_Corrected,simCurrent ,figStart)


figure(figStart);
clf
hold all;
str1=sprintf('Gap=%0.2f | %0.2f   nm\n',IVparam.NL_Gap,IVparam.LS_Gap);
str1=sprintf('%s Potential=%0.2f | %0.2f   eV\n',str1, IVparam.NL_Potential,IVparam.LS_Potential);
str1=sprintf('%s Dielectric=%0.2f | %0.2f   e0\n',str1, IVparam.NL_Dielectric,IVparam.LS_Dielectric);
str1=sprintf('%s Tunneling Area=%0.2f | %0.2fum^2\n',str1,  IVparam.NL_Tunneling_Area,IVparam.LS_Tunneling_Area);
% str1=sprintf('%s Nanowire Width=%0.2f | %0.2f   nm\n',str1,  w ,w2 );
%jA=IVparam.junctionLength*1e-3*; %u
str1=sprintf('%s Resitivity=%0.2e ohm*m\n',str1,IVparam.Measured_Resistivity);
str1=sprintf('%s ---------------------------\n',str1);
str1=sprintf('%s Geo. Width:%0.2f nm\n' ,str1, IVparam.junctionWidth);
% str2=sprintf('%s Geo. Area:%0.2f um^2\n',str2, junctionArea*1e12);
str1=sprintf('%s Geo. Gap:%0.2f nm\n',str1,  IVparam.Geo_JunctionGap);
% str2=sprintf('%s Geo. Capacitance:%0.2f | %0.2f fF\n',str2,idealCap,idealCap2);

% str2=sprintf('%s Capacitance=%0.2f pF\n',str2,CapT*1e12);
% str2=sprintf('%s Cap. Area=%0.2f um^2\n',str2,areaCap*1e12);



plot(Volts*1000,Current/(IVparam.MinArea*1e4),'b')
% plotLoopless( );
b=ylim;
plotLoopless( Volts*1000,simCurrent{1},'r');
plotLoopless( Volts*1000,simCurrent{2},'m');
plotLoopless( Volts*1000,simCurrent{3},'k');
ylim(b)

xlabel('Voltage (mV)');
ylabel('Current Density (A/cm^2)');
legend({'Data','Nonlinear','Linear','Literature'},'Location','southoutside','Orientation','horizontal')
[~,f]=fileparts(IVparam.file);
title( strrep(f,'_',' ') );
clear f
text(1,.2,str1,'HorizontalAlignment','right','Units','normalized')
% text(0.02,.75,str2,'HorizontalAlignment','left','Units','normalized')

handaxes3 = axes('Position', [0.18 .72 0.2 0.2]);
set(handaxes3, 'Box','off')
plot(V0*1000,(I0-IVparam.Current_Shift)*1e12);
if isempty(Vair)==false
    hold all
    plot(Va0*1000,(Ia0 -IVparam.Current_Shift)*1e12);
    hold off
    %     legend({'Data', 'Parasitic'});
end
% xlabel('Voltage (mV)');
%  ylabel('Current (pA)');
% [~,f]=fileparts( IVparam.file);
% title( ['Raw:'  strrep(f,'_',' ')] );

str1=   sprintf(' Capacitance:%0.2f pF\n',  IVparam.Capacitance);
str1=   sprintf('%s Conductance:%0.2f pS\n',str1, IVparam.Conductance*1e12);
if isempty(Vair)==false
    hold all;
    plotLoopless( Volts*1000,Current*1e12,'b');
    plotLoopless( Vair*1000,Iair*1e12,'g');
    hold off;
    
    %     str1=   sprintf('%s Parasitic capacitance:%0.2f pF\n',str1, CapAir*1e12 );
    %     str1=   sprintf('%s Parasitic conductance:%0.2f pS\n',str1, 1e12*Conduct_Air);
    %     text(1,.2,str1,'HorizontalAlignment','right','Units','normalized')
    %     legend({'Data','Parasitic'});
else
    hold all
    plot(Volts,Current_Corrected);
    %     text(1,.2,str1,'HorizontalAlignment','right','Units','normalized')
end


figStart=figStart+1;
end


