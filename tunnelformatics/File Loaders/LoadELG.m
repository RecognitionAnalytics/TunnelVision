function [dataExample]=LoadELG(ionic,firstTime)

fs = dir(ionic);

fid = fopen(ionic,'r');
C_data0 = textscan(fid,'%f, %f, %f');
pTimes =C_data0{1}/1000;
I_Ionic = C_data0{2} ;

if isempty(pTimes)==false
    createTime=(fs.datenum -firstTime) *24*60*60 ;
    pTimes=(pTimes-pTimes(1))+createTime;
    ionicTraceTime=pTimes(end)-pTimes(1);
    I_Ionic=I_Ionic-smooth(I_Ionic,10000)-100;
    
    dataExample.file=name;
    dataExample.Currents=I_Ionic;
    dataExample.Times=pTimes;
    dataExample.Dir=K;
    
    
end
fclose(fid);
end