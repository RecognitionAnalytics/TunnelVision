function []=CopyToClipboard(examples, controlTrace, dirLabels, figStart)

%     t=globalExample(isnan(globalExample)==false);
%     if isempty(t)==false
%         stats{K,1}=dirLabels{K};
%         stats{K,2}=length(t)*100/sampleRate;
%         stats{K,3}=mean(t);
%         stats{K,4}=t(end);
%         stats{K,5}=std(t);
%         stats{K,6}=std(globalFlat);
%         stats{K,7}=std(peakTrace);
%         stats{K,8}=length(peakCollection);
%         stats{K,9}=length(x2);
%         stats{K,10}=mean(x2);
%         stats{K,11}=length(peakMags);
%         stats{K,12}=mean(peakMags);
%         stats{K,13}=mean(lengths)/sampleRate*1000;
%         stats{K,14}=mean(llengths)/sampleRate*1000;
cols={'name','total length(s)','baseline(pA)','settled baseline(pA)','noise(pA)' ...
    ,'flattened noise (pA)','limited noise(pA)','number useable peaks', ...
    '#cheap levels','cheap level average(pA)', 'spike count','spike height(pA)',...
    'cheap lengths (ms)','long cheap levels(ms)'};


t=sprintf('%s\t',cols{:});
t=[t sprintf('\n')];
for I=1:size(stats,1)
    t=[t  sprintf('%s\t',stats{I,1})];
    t=[t  sprintf('%d\t',stats{I,2:end})];
    t=[t  sprintf('\n')];
end

%paste into excel
%clipboard('copy',t);


f=figure( 'Position',[100 100 752 250]);clf
uitable('parent', f, 'data',stats,'ColumnName',cols, 'Position', [25 50 700 200]);
end