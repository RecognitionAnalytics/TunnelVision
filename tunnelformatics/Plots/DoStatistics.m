function [textTable]=DoStatistics(topDirectory,traces,beforeTunnelingIVs,beforeIonicIVs,afterTunnelingIVs,afterIonicIVs)
global dirSeperator

eCols=fieldnames(traces{1}.fileInfo);
eIVCols{1}=(beforeTunnelingIVs);
eIVCols{2}=(beforeIonicIVs);
eIVCols{3}=(afterTunnelingIVs);
eIVCols{4}=(afterIonicIVs);
eColAll=eCols;
for I=1:length(eIVCols)
    eColAll=vertcat(eColAll, fieldnames(eIVCols{I}));
end
experiments=cell([1 length(eColAll)]);
% numbers=zeros([length(examples) 14]);
ee=traces{1};
eeFI=traces{1}.fileInfo;
cc=1;
for J=1:length(eCols);
    t=eeFI.(eCols{J});
    if ischar(t)==false
        if isnumeric(t) || islogical(t)
            t=num2str(t);
        else
            if isstruct(t)
                t='';
            end
        end
    end
    experiments{cc}=t;
    cc=cc+1;
end


for J=1:length(eIVCols);
    fNames=fieldnames(eIVCols{J});
    for K=1:length(fNames)
        t=eIVCols{J}.(fNames{K});
        if ischar(t)==false
            if isnumeric(t) || islogical(t)
                if numel(t)>1
                    t=num2str( mean( t(:)));
                else
                    t=num2str(t);
                end
            else
                if isstruct(t)
                    t='';
                end
            end
        end
        experiments{cc}=t;
        cc=cc+1;
    end
    
end


cols1=fieldnames(traces{1}.fileInfo);
cols2=fieldnames(traces{1});
cols2(find(~cellfun(@isempty,strfind(lower(cols2),'fileinfo'))))=[];
cols=vertcat(cols1, cols2);
stats=cell([length(traces) length(cols)]);
% numbers=zeros([length(examples) 14]);
for K=1:length(traces)
    ee=traces{K};
    eeFI=traces{K}.fileInfo;
    for J=1:length(cols1);
        t=eeFI.(cols1{J});
        if ischar(t)==false
            if isnumeric(t) || islogical(t)
                t=num2str(t);
            else
                if isstruct(t)
                    t='';
                end
            end
        end
        stats{K,J}=t;
    end
    
    for J=1:length(cols2);
        t=ee.(cols2{J});
        if ischar(t)==false
            if isnumeric(t) || islogical(t)
                if numel(t)>1
                    t=num2str( mean( t(:)));
                else
                    t=num2str(t);
                end
            else
                if isstruct(t)
                    t='';
                end
            end
        end
        
        stats{K,J+length(cols1)}=t;
    end
end

textTable=sprintf('%s,',cols{:});
textTable=[textTable sprintf('\n')];
for I=1:size(stats,1)
    textTable=[textTable  sprintf('%s,',stats{I,:})];
    textTable=[textTable  sprintf('\n')]; %#ok<AGROW>
end

%paste into excel
% clipboard('copy',textTable);
f=figure( 'Position',[100 100 752 250]);clf
uitable('parent', f, 'data',stats,'ColumnName',cols,'units','normalized', 'Position', [0 0 1 1]);


filename = [topDirectory dirSeperator 'experiment.csv'];
fileID = fopen(filename,'w');
fprintf(fileID,'%s',textTable);
fclose(fileID);

% figure(figStart);
% clf;hold all
% title('Baseline');
% xlabel('Directory');
% ylabel('log Current (pA)');
% scatter(numbers(:,1),log(numbers(:,2))/log(10));
% scatter(numbers(:,1),log(numbers(:,3))/log(10));
% y=ylim;
% for I=1:size(numbers,1)
%     ht = text(numbers(I,1),0.6*y(1)+0.4*y(2),dirLabels{numbers(I,1)});
%     set(ht,'Rotation',90)
% end
% figStart=figStart+1;
%
% figure(figStart);
% clf;
% scatter(numbers(:,1),log(numbers(:,4))/log(10));
% hold all
% scatter(numbers(:,1),log(numbers(:,5))/log(10));
% title('Noise');
% xlabel('Directory');
% ylabel('log Current (pA)');
% figStart=figStart+1;
%
% figure(figStart);
% clf;
% scatter(numbers(:,1),log(numbers(:,6))/log(10));
% hold all
% scatter(numbers(:,1),log(numbers(:,7))/log(10));
% scatter(numbers(:,1),log(numbers(:,8))/log(10));
% title('Frequency');
% xlabel('Directory');
% ylabel('Log Frequency (peaks/S)');
end