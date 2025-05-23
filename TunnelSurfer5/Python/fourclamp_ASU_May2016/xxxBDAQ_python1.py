# -*- coding: utf-8 -*-
"""
@author: jrosenstein
"""


import numpy as np
import time
import bdaq

import matplotlib as mpl
from matplotlib import pyplot as plt
from scipy import signal
from scipy import integrate
mpl.rcParams['agg.path.chunksize'] = 10000

def movingaverage(interval, window_size):
    window = np.ones(int(window_size))/float(window_size)
    return np.convolve(interval, window, 'same')
# =====================================
# load pre-saved configuration

bdaq.LoadConfig()
bdaq.cal["in_gain"] = list(np.ones(bdaq.cal["numchannels"],dtype=np.float64) * (2**bdaq.cal["in_bits"]) / bdaq.cal["in_vfs"] * bdaq.cal["in_transimpedance"])
bdaq.cal["in_gain"][0] *= (1/4.0)*425/500
bdaq.cal["in_gain"][1] *= (1/4.0)*425/500
bdaq.cal["in_gain"][2] *= (1/4.0)*425/500
bdaq.cal["in_gain"][3] *= (1/4.0)*425/500
bdaq.cal["downsampleratio"] = 1
bdaq.cal["samplerate"] = 100e6/16/bdaq.cal["downsampleratio"]/bdaq.cal["numchannels"]*2
bdaq.SaveConfig()

# ===================================
# define bias point

# null offsets and save configuration
newbiaspoint = True
defaultVZero=1.45
if(newbiaspoint):
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=defaultVZero

# ===================================

# initialize hardware

bdaq.BootFourClamp(bdaq.cal["biasvoltages"])

bdaq.SoftReset()
time.sleep(0.1)

# ===================================

# null offsets and save configuration
nullIoffset = False

if(nullIoffset):
    bdaq.ChangeDACValues(bdaq.cal["biasvoltages"])
    time.sleep(0.1)
    bdaq.NullCurrentOffsetsBA()
    time.sleep(0.1)
    bdaq.SaveConfig()
    
# =====================================

nullVoffset = True

if(nullVoffset):
    bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP0"]] = bdaq.FindZeroCurrentVoltageBA(bdaq.DACs["VBIAS1"],bdaq.inputs["CH0"],1)
    time.sleep(0.1)
    bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP1"]] = bdaq.FindZeroCurrentVoltageBA(bdaq.DACs["VBIAS2"],bdaq.inputs["CH1"],1)
    time.sleep(0.1)
    bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP2"]] = bdaq.FindZeroCurrentVoltageBA(bdaq.DACs["VBIAS2"],bdaq.inputs["CH2"],1)
    time.sleep(0.1)
    bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP3"]]= bdaq.FindZeroCurrentVoltageBA(bdaq.DACs["VBIAS2"],bdaq.inputs["CH3"],1)
    time.sleep(0.1)
    bdaq.SaveConfig()


#%% =====================================

# define voltage protocols
bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=defaultVZero
bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=defaultVZero
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=defaultVZero-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP0"]]
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=defaultVZero-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP1"]]
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=defaultVZero-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP2"]]
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=defaultVZero-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP3"]]


bdaq.ChangeDACValues(bdaq.cal["biasvoltages"])
time.sleep(0.1)
bdaq.NullCurrentOffsetsBA()
time.sleep(0.1)
bdaq.SaveConfig()
# constant bias
constantprogram = bdaq.program_setbias(bdaq.cal["biasvoltages"])

# sweep
sweepchannel = bdaq.DACs["VCLAMP0"]
sweeprange = 0.5
sweepvoltages = list(np.ones(10)*bdaq.cal["biasvoltages"][sweepchannel]) + list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel],bdaq.cal["biasvoltages"][sweepchannel]+sweeprange,num=20))+list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel]+sweeprange,bdaq.cal["biasvoltages"][sweepchannel]-sweeprange,num=40))+list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel]-sweeprange,1.0,num=20))
sweepsteptime = 0.01
sweepprogram = bdaq.program_waveform(bdaq.cal["biasvoltages"],sweepchannel,sweepvoltages,sweepsteptime)

# step
stepchannel = bdaq.DACs["VCLAMP2"]
steptime = 0.5
stepsize = 1
stepvoltages = [bdaq.cal["biasvoltages"][sweepchannel],bdaq.cal["biasvoltages"][sweepchannel]+stepsize,bdaq.cal["biasvoltages"][sweepchannel]-stepsize,bdaq.cal["biasvoltages"][sweepchannel]]
stepprogram = bdaq.program_waveform(bdaq.cal["biasvoltages"],stepchannel,stepvoltages,steptime)

bdaq.LoadMM(constantprogram)            # load program into MM
#bdaq.LoadMM(stepprogram)                # load program into MM
#bdaq.LoadMM(sweepprogram)                # load program into MM

# =====================================

# soft reset to initialize FIFO

bdaq.SoftReset()
time.sleep(0.1)

# =====================================

# start waveform and acquire data

bdaq.StartMMandADCFIFO()               # start waveform and acquisition

time.sleep(2)                         # pause for data buffer to fill
rawdata,xferbytes = bdaq.GetDataByTime(2,1)         # retrieve data

# to scale this to current, one would use:
Idata = bdaq.ScaletoIData(rawdata,bdaq.cal)


# ===================================
# save data to file

savelogfile = True

if(savelogfile):

    dirname = 'logfiles'
    logname = 'testing'
    
    writelogfilename = bdaq.SaveLogFile(rawdata,logname,dirname)


# ===================================
# read back data from log file

readlogfilename = writelogfilename
#readlogfilename = 'C:\\Users\\pshakya\\Desktop\\fourclamp\\BDAQ python\\logfiles\\testlogfile_20160519_131858.bdaq'

log_rawdata,log_cal = bdaq.ReadLogFile(readlogfilename)
log_Idata = bdaq.ScaletoIData(log_rawdata,log_cal)


plt.plot(1e12*movingaverage(log_Idata[int(0.5*bdaq.cal["samplerate"])::,0],100))    
plt.plot(1e12*movingaverage(log_Idata[int(0.5*bdaq.cal["samplerate"])::,1],100))    
plt.plot(1e12*movingaverage(log_Idata[int(0.5*bdaq.cal["samplerate"])::,2],100))    
plt.plot(1e12*movingaverage(log_Idata[int(0.5*bdaq.cal["samplerate"])::,3],100))    
plt.show()

# ===================================
# plot data

t = np.arange(0,log_Idata.shape[0])/log_cal["samplerate"]

#for c in range(log_cal["numchannels"]):
#    print(str(bdaq.inputs_reverse[c]) + " Mean = " + str(np.mean(log_Idata[int(0.5*log_cal["samplerate"])::,c]*1e12)) + "pA")
#    plt.plot(t,log_Idata[:,c]*1e12,label=(str(bdaq.inputs_reverse[c])))
#    plt.title("channel "+str(c))
#    plt.ylabel("pA")
#    plt.xlabel("sec")
#    plt.axis([0,t[len(t)-1],-20,20])

#plt.legend(loc=1)    
#plt.show()

#for c in range(log_cal["numchannels"]):
#    TraceForPSD = log_Idata[int(0.5*log_cal["samplerate"])::,c]    # remove initial transient
#    TraceForPSD = TraceForPSD - np.mean(TraceForPSD)   # subtract mean
#    f,PSD = signal.welch(TraceForPSD,log_cal["samplerate"],nperseg=2**17)
#    RMS = np.sqrt(integrate.cumtrapz(PSD,f))
    
#    plt.loglog(f,PSD*1e24)
#    plt.xlabel('Hz')
#    plt.ylabel('pA**2/Hz')
#    plt.title("channel "+str(c))
#    plt.grid(True,which="major",ls='-')
#    plt.show()


 #   plt.loglog(f[1::],RMS*1e12,label=(str(bdaq.inputs_reverse[c])))
 #   plt.xlabel('Hz')
 #   plt.ylabel('pArms')
    #plt.title("channel "+str(c))
 #   plt.grid(True,which="both")
#plt.legend(loc=2)    
#plt.show()





#
#for c in range(log_cal["numchannels"]):
#    plt.plot(Idata[:100e3,c]*1e9)
#    plt.title("channel "+str(c))
#    plt.ylabel("nA")
#    plt.xlabel("sec")
#    plt.grid(True)
#    plt.ylim(-12,12)   
#    plt.show()
#    print(np.mean(Idata[25e3:35e3,c]*1e9))


#%% ===================================

# disconnect
bdaq.Disconnect()
