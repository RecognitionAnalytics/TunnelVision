

[filename, pathname, filterindex] = uigetfile(['*.bdaq'], 'pick a log file');


[data,cal] = readBDAQlogfile(fullfile(pathname, filename));
disp(cal);    % print calibration parameters
scaledI = BDAQScaletoIData(data,cal);


downsampleratio = 1
for c=1:cal.numchannels
    scaledI_resampled(:,c) = resample(scaledI(:,c),1,downsampleratio);
end




t = (1:size(scaledI_resampled,1)) ./ cal.samplerate .* downsampleratio;


% plot traces
figure;
for c=1:cal.numchannels
    subplot(cal.numchannels,1,c)
    plot(t,scaledI_resampled(:,c)'.*1e9);   % plot each channel
    xlabel('seconds');
    ylabel('nA');
    title('Time Domain');
    hold on;
end

% plot noise
figure;
for c=1:cal.numchannels
    fftpoints = 2^16;
    [outputpsd,f] = pwelch(scaledI(:,c)-mean(scaledI(:,c)),[],floor(fftpoints*0.5),fftpoints,cal.samplerate);
    inputrms = sqrt(cumtrapz(f,outputpsd)) ./ cal.in_gain(c);
    
    loglog(f,inputrms.*1e12);
    xlabel('Hz');
    ylabel('pArms');
    title('Integrated Power');
    grid on;
    
    hold on;
end
