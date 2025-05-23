
% EXAMPLE:     
% [data,cal] = readBDAQlogfile('logfiles\testing_20160520_171904.bdaq');
% disp(cal);    % print calibration parameters
% scaledI = BDAQScaletoIData(data,cal);
% plot(scaledI(:,1));   % plot first channel

function [log_rawdata,log_cal] = readBDAQlogfile(readlogfilename)

    % this function uses the jsonlab library  http://iso2mesh.sourceforge.net/cgi-bin/index.cgi?jsonlab
    addpath('jsonlab')
    
    % open log file
    fid = fopen(readlogfilename,'rb');
    
    % extract header struct
    rawheaderdata = fread(fid,10000,'uint8=>char')';    % hard coded maximum header size
    myheader = cell2mat(regexp(rawheaderdata,'{.*?}<data_begins>','match'));
    headerlen = length(myheader);
    log_cal = loadjson(myheader(1:(headerlen-13)));
    
    % read the raw data stream
    fseek(fid,headerlen,'bof');
    log_rawdata = fread(fid,'uint16');
    log_rawdata = reshape(log_rawdata,log_cal.numchannels,length(log_rawdata)/log_cal.numchannels)';
    
    % close log file
    fclose(fid);
    
    
    
  