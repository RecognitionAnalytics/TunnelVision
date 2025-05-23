%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%%%%%                 user controlled                   %%%%%%%%%%
TopDirectory='S:\\Research\\Stacked Junctions\\Results\\20160229_NALDB34_Chip03_22cyc_HIM3pC_UVozone10each_200deg_1mMPB_pH7_modified\\For Brain analysis\\M1-M2'
doWavelet =true;
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
global dirSeperator
if ispc
    dirSeperator= '\';
else
    dirSeperator='/';
end
%just for convience with comparison
figStart=1;
baselineRemovalMethod=1;

%recursive file search to get all the abf files, directories and basic
%informations
[dirNames,firstTime,fileDictionary]=OrganizeFiles(TopDirectory);

IVCurves.before=false;
IVCurves.after=false;
if exist([TopDirectory dirSeperator 'before_IV'],'dir')~=0
    IVCurves.before=true;
    try
        [ IVCurves.beforeIonicIVs,  IVCurves.beforeIonicCurveDict, figStart]=IonicFitting([TopDirectory dirSeperator 'before_IV'],50,10,figStart,true,'ionic');
    catch mex
    end
    try
        [ IVCurves.beforeTunnelingIVs, IVCurves.beforeTunnelingCurveDict,figStart]=simmons2([TopDirectory dirSeperator 'before_IV'],true,figStart);
    catch mex
    end
end

if exist([TopDirectory dirSeperator 'after_IV'],'dir')~=0
    IVCurves.after=true;
    try
        [ IVCurves.afterIonicIVs,IVCurves.afterIonicCurveDict, figStart]=IonicFitting([TopDirectory dirSeperator 'after_IV'],50,10,figStart,true,'ionic');
    catch mex
    end
    try
        [ IVCurves.afterTunnelingIVs,IVCurves.afterTunnelingCurveDict,figStart]=simmons2([TopDirectory dirSeperator 'after_IV'],true,figStart);
    catch mex
    end
end



%get the frequency response of the control
[controlTraces]=LoadControl( dirNames , fileDictionary);

%load the files and do all the denoising
[traces]=DoFiltering(controlTraces,fileDictionary,dirNames,baselineRemovalMethod,doWavelet,firstTime);


figStart=GlobalPlots(traces,figStart);
figStart=PlotHistograms(traces, figStart);
figStart=PlotFreq(traces,figStart);
figStart=PlotScatters(traces,figStart);
%this is a problem, output to csv
textTable=DoStatistics(TopDirectory,traces, IVCurves);
