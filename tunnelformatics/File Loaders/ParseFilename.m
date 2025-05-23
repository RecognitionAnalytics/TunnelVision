function [fileParse] = ParseFilename(fileParse,fullFileName)
global dirSeperator

[p,name,e] = fileparts(fullFileName);
fileParse.fileType=e;
pParts=strsplit(p,dirSeperator);

fileParse.isControl=false;
try
    I=find(~cellfun(@isempty,strfind(lower(pParts),'control')));
    if isempty(I)==false
        fileParse.isControl =true;
    end
catch mex
    try
        I=find(~cellfun(@isempty,strfind(lower(pParts),'rinse')));
        if isempty(I)==false
            fileParse.isControl =true;
        end
    catch mex
    end
end


I=find(~cellfun(@isempty,strfind(lower(pParts),'nald')));
pParts=strsplit(pParts{I},'_');

if isempty(str2num([pParts{1} ]))==false
    fileParse.experimentDate=pParts{1};
    pParts(1)=[];
end

try
    I=find(~cellfun(@isempty,strfind(lower(pParts),'nald')));
    fileParse.batch =pParts{I};
    pParts(I)=[];
catch mex
    fileParse.batch ='';
end

try
    I=find(~cellfun(@isempty,strfind(lower(pParts),'chip')));
    fileParse.chip =strrep(lower(pParts{I}),'chip','');
    pParts(I)=[];
catch mex
    fileParse.chip ='';
end

try
    I=find(~cellfun(@isempty,strfind(lower(pParts),'cyc')));
    fileParse.aldCycles =strrep(lower(pParts{I}),'cyc','');
    pParts(I)=[];
catch mex
    fileParse.aldCycles ='';
end

try
    I=find(~cellfun(@isempty,strfind(lower(pParts),'him')));
    fileParse.method ='HIM';
    fileParse.dose=strrep(lower(pParts{I}),'him','');
    pParts(I)=[];
catch mex
    try
        I=find(~cellfun(@isempty,strfind(lower(pParts),'fib')));
        fileParse.method =pParts{I};
        pParts(I)=[];
    catch mex
        try
            I=find(~cellfun(@isempty,strfind(lower(pParts),'rie')));
            fileParse.method =pParts{I};
            pParts(I)=[];
        catch mex
            fileParse.method ='';
        end
    end
end

fileParse.notes = sprintf('%s ',pParts{1:end});

parts=strsplit(name,'_');
if strcmp(parts{end},'g')
    parts(end)=[];
end
if isempty(str2num([parts{end} ]))==false
    parts(end)=[];
end
fileParse.junction=[parts{1} ' ' parts{2}];
try
    str='top';I=find(~cellfun(@isempty,strfind(lower(parts),str)));t=parts{   I};parts(I)=[];t=strrep(lower(t),str,'');
    t=strrep(t,'g','0');
    t=strrep(t,'p','');
    t=strrep(t,'n','-');
    t=strrep(t,'mv','');
    fileParse.topRef= str2num(t);
catch mex
    fileParse.topRef= 0;
end

try
    str='bttm';I=find(~cellfun(@isempty,strfind(lower(parts),str)));t=parts{   I};parts(I)=[];t=strrep(lower(t),str,'');
    t=strrep(t,'g','0');
    t=strrep(t,'p','');
    t=strrep(t,'n','-');
    t=strrep(t,'mv','');
    fileParse.bttmRef= str2num(t);
catch mex
    fileParse.bttmRef=0;
end

try
    try
        str='mm';I=find(~cellfun(@isempty,strfind(lower(parts),str)));t=parts{   I};parts(I)=[];
        parts2=strsplit(lower(t),'mm');
        fileParse.concentrationMM= str2num(parts2{1});
        fileParse.analyte=parts2{2};
        
    catch mex
        str='nm';I=find(~cellfun(@isempty,strfind(lower(parts),str)));t=parts{   I};parts(I)=[];
        parts2=strsplit(lower(t),'nm');
        fileParse.concentrationMM= str2num(parts2{1})/1000;
        fileParse.analyte=parts2{2};
    end
catch mex
    fileParse.concentrationMM= 0;
    fileParse.analyte='';
end

try
    str='mv';I=find(~cellfun(@isempty,strfind(lower(parts),str)));t=parts{   I};parts(I)=[];t=strrep(lower(t),str,'');
    t=strrep(t,'g','0');
    t=strrep(t,'p','');
    t=strrep(t,'n','-');
    fileParse.tunnelMV= str2num(t);
catch mex
    fileParse.tunnelMV= 0;
end
fileParse.ionicMV=abs(fileParse.topRef-fileParse.bttmRef);
end