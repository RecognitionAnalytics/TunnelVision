function [figStart]=PlotScatters(traces,figStart)

resolution=1000;

dLegend={};
gDataTable=[];
gLabels=[];
maxD=0;
for  I=1:length(traces)
    dataTable=zeros([resolution 8]);
    dataTable(:,1)=resampleData(traces{I}.flat,resolution);
    dataTable(:,2)=resampleData(traces{I}.response,resolution);
    dataTable(:,3)=resampleData(traces{I}.waveNoise,resolution);
    dataTable(:,4)=resampleData(traces{I}.coefs2(:,1),resolution);
    dataTable(:,5)=resampleData(traces{I}.coefs2(:,2),resolution);
    dataTable(:,6)=resampleData(traces{I}.coefs2(:,3),resolution);
    dataTable(:,7)=resampleData(traces{I}.TV,resolution);
    dataTable(:,8)=resampleData(traces{I}.bShortData,resolution);
    gDataTable=vertcat(gDataTable,dataTable);
    gLabels=vertcat(gLabels,zeros([resolution,1])+traces{I}.fileInfo.dirNumber);
    if traces{I}.fileInfo.dirNumber>maxD
        maxD=traces{I}.fileInfo.dirNumber;
    end
end
gDataTable(isnan(gDataTable))=0;
gDataTable=bsxfun(@minus, gDataTable, mean(gDataTable));
gDataTable=bsxfun(@times, gDataTable, 1./std(gDataTable));
gDataTable(isnan(gDataTable))=0;


[~,score] = pca(gDataTable);
figure(figStart);
clf;hold all;
for  I=1:maxD
    idx=find(gLabels==I);
    if isempty(idx)==false
        %         scatter(score(idx,1),score(idx,2),2,colors{ mod( I, length(colors))+1});
        plot(score(idx,1),score(idx,2),'.');
        dd=strrep(traces{I}.fileInfo.dirLabel,'_',' ');
        dLegend{I}=dd; %#ok<AGROW>
    end
end
title('PCA scatter');
xlabel('PCA 1');
ylabel('PCA 2');
legend(dLegend)
figStart=figStart+1;

figure(figStart);
clf;hold all;
for  I=1:maxD
    idx=find(gLabels==I);
    if isempty(idx)==false
        plot(score(idx,3),score(idx,4),'.');
        dd=strrep(traces{I}.fileInfo.dirLabel,'_',' ');
        dLegend{I}=dd; %#ok<AGROW>
    end
end
title('PCA scatter2');
xlabel('PCA 3');
ylabel('PCA 4');
legend(dLegend)
figStart=figStart+1;

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

dLegend={};
gDataTable=[];
gLabels=[];
maxD=0;
for  I=1:length(traces)
    idx=traces{I}.justPeaksIDX;
    dataTable=zeros([resolution 8]);
    dataTable(:,1)=resampleData(traces{I}.flat(idx),resolution);
    try
        dataTable(:,2)=resampleData(traces{I}.response(idx),resolution);
        dataTable(:,3)=resampleData(traces{I}.waveNoise(idx),resolution);
    catch mex
    end
    dataTable(:,4)=resampleData(traces{I}.TV(idx),resolution);
    dataTable(:,5)=resampleData(traces{I}.bShortData(idx),resolution);
    t=idx;
    try
        dataTable(:,6)=resampleData(traces{I}.coefs2(t,1),resolution);
        dataTable(:,7)=resampleData(traces{I}.coefs2(t,2),resolution);
        dataTable(:,8)=resampleData(traces{I}.coefs2(t,3),resolution);
    catch mex
    end
    
    gDataTable=vertcat(gDataTable,dataTable);
    gLabels=vertcat(gLabels,zeros([resolution,1])+traces{I}.fileInfo.dirNumber);
    if traces{I}.fileInfo.dirNumber>maxD
        maxD=traces{I}.fileInfo.dirNumber;
    end
end
gDataTable(isnan(gDataTable))=0;
gDataTable=bsxfun(@minus, gDataTable, mean(gDataTable));
gDataTable=bsxfun(@times, gDataTable, 1./std(gDataTable));
gDataTable(isnan(gDataTable))=0;
[~,score] = pca(gDataTable);
figure(figStart);
clf;hold all;
for  I=1:maxD
    idx=find(gLabels==I);
    if isempty(idx)==false
        %         scatter(score(idx,1),score(idx,2),2,colors{ mod( I, length(colors))+1});
        plot(score(idx,1),score(idx,2),'.');
        dd=strrep(traces{I}.fileInfo.dirLabel,'_',' ');
        dLegend{I}=dd; %#ok<AGROW>
    end
end
title('JP PCA scatter');
xlabel('PCA 1');
ylabel('PCA 2');
legend(dLegend)
figStart=figStart+1;

figure(figStart);
clf;hold all;
for  I=1:maxD
    idx=find(gLabels==I);
    if isempty(idx)==false
        plot(score(idx,3),score(idx,4),'.');
        dd=strrep(traces{I}.fileInfo.dirLabel,'_',' ');
        dLegend{I}=dd; %#ok<AGROW>
    end
end
title('JP PCA scatter2');
xlabel('PCA 3');
ylabel('PCA 4');
legend(dLegend)
figStart=figStart+1;
end

function nData=resampleData(data,resolution)

nData=zeros([ resolution 1]);
step = floor(length(data)/resolution);
cc=1;
for I=1:step:length(data)-step
    t=data(I:(I+step));
    nData(cc)=mean(t);%+ max(t)*.25;
    cc=cc+1;
end
if length(nData)>resolution
    nData=nData(1:resolution);
end
end