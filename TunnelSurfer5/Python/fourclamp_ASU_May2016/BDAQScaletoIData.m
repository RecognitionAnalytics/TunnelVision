

% uses a BDAQ calibration struct to calibrate a raw data stream for gain
% and offset
% 
% EXAMPLE:     
% [data,cal] = readBDAQlogfile('logfiles\testing_20160520_171904.bdaq');
% disp(cal);    % print calibration parameters
% scaledI = BDAQScaletoIData(data,cal);
% plot(scaledI(:,1));   % plot first channel

function scaledI = BDAQScaletoIData(rawdata,mycal)

    scaledI = zeros(size(rawdata));
    
    for c = 1:mycal.numchannels
        scaledI(:,c) = ( rawdata(:,c) - mycal.in_os(c) ) / mycal.in_gain(c);
    end    
    
