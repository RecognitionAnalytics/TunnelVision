function [dirNames,firstTime,fileDictionary]=OrganizeFiles(topDirectory)
global dirSeperator
files =rdir( topDirectory, [dirSeperator '*.abf'],true);

dirNames=cell(size(files));
dirLabels=cell(size(files));
ccControl=1;
%get all the active directories and their names
for I=1:length(files)
    [p,name,e] = fileparts(files{I});
    [p2,name2] = fileparts(p);
    dirNames{I}=p;
    dirLabels{I}=name2;
end
%cut down the list
[dirNames,ia]=unique(dirNames);
dirLabels=dirLabels(ia);
fileTimes=[];
cc=1;
for K=1:length(dirNames)
    %check if there is a keithley log file with this abf file
    files = dir([dirNames{K} dirSeperator '*.abf']);
    for J=1:length(files)
        fileStruct.dirNumber=K;
        fileStruct.dirLabel=dirLabels{K};
        fileStruct.fileName=files(J).name;
        fileStruct.fileTime=datenum(files(J).date);
        fileTimes(cc)=fileStruct.fileTime;
        fileStruct = ParseFilename(fileStruct,[dirNames{K} dirSeperator files(J).name]);
        fileDictionary(cc)=fileStruct;
        cc=cc+1;
    end
end

[fileTimes, idx]=sort(fileTimes);
fileDictionary=fileDictionary(idx);


for K=length(fileDictionary):-1:1
    
    if fileDictionary(K).isControl==false
        fileDictionary(K).controlFile=-1;
        for J=K-1:-1:1
            if fileDictionary(J).isControl==true && fileDictionary(J).tunnelMV==fileDictionary(K).tunnelMV && fileDictionary(J).ionicMV==fileDictionary(K).ionicMV
                fileDictionary(K).controlFile=J;
                break
            end
        end
    else
        fileDictionary(K).controlFile=K;
    end
end

firstTime=fileTimes(1);
end