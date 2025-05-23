function [I,V,dvdt,dt]=LoadFile(directory,filename,voltageCol,currentCol)

if isempty(strfind(filename,':'))==true
    fid = fopen([directory '\' filename],'r');
else
    fid = fopen(filename,'r');
end
if isempty(strfind(filename,'.csv'))
    C_data0 = textscan(fid,'%f %f %f %f');
    V =C_data0{3}/1000;
    I = C_data0{4} *1e-9;
else
    InputText=textscan(fid,'%s',7,'delimiter','\n');
    C_data0 = textscan(fid,'%f, %f, %f, %f, %f, %f,');
    V =C_data0{voltageCol};
    I = C_data0{currentCol} ;
end
fclose(fid);

dvdt=mean(abs(diff(V)))/mean(diff(C_data0{1}));

dt=mean(diff(C_data0{1}));
end