function [figStart]=exampleHist(examples,fieldname,fTitle,xAxis,figStart,doSemi)

figure(figStart);
clf;

lDir=-1;
c=[];
dirLegend={};

for  I=1:length(examples)
    d=examples{I}.fileInfo.dirNumber;
    
    t=examples{I}.(fieldname);
    t=reshape(t,[1 length(t)]);
    if lDir~=d && lDir~=-1
        c=[c t]; %#ok<NASGU>
        
        [y,x]=histcounts(t,'BinMethod','fd');
        x=x(1:(end-1));
        y=y/sum(y);
        idx=find(y>(max(y)*.01));
        idx=min(idx):max(idx);
        x=x(idx);
        y=y(idx);
        if doSemi
            semilogy(x,y+.001);
        else
            plot(x,y);
        end
        hold all
        dd=strrep(examples{I}.fileInfo.dirLabel,'_',' ');
        dirLegend{length( dirLegend)+1}=dd; %#ok<AGROW>
        c=[];
    else
        c=[c t];
    end
    lDir=d;
end
%fix this mess
if isempty(c)==false
    
    [y,x]=histcounts(t,'BinMethod','fd');
    x=x(1:(end-1));
    y=y/sum(y);
    idx=find(y>(max(y)*.01));
    idx=min(idx):max(idx);
    x=x(idx);
    y=y(idx);
    
    if doSemi
        semilogy(x,y+.001);
    else
        plot(x,y);
    end
    dd=strrep(examples{I}.fileInfo.dirLabel,'_',' ');
    dirLegend{length( dirLegend)+1}=dd; %#ok<AGROW>
    
    hold all
    
end
title(fTitle );
xlabel(xAxis);
ylabel('Frequency');
legend(dirLegend);
figStart=figStart+1;
end
