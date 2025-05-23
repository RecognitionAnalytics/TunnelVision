# -*- coding: utf-8 -*-
"""
@author: jrosenstein
"""


import numpy as np
import time
import bdaq
#from Tkinter import *
from matplotlib import pyplot as plt
from scipy import signal
from scipy import integrate
from scipy import interpolate
import socket
import sys, traceback
import struct
import os
print(os.getcwd())

# define bias point
defaultVZero=1.4

def SendMessage(connection,message):
    n=str(len(message)).zfill(3)    
    print('len %s' % n)
    connection.sendall(n.encode('utf-8'))
    connection.sendall(message.encode('utf-8'))
    print ( 'sent "%s"' % message)

def GetMessage(connection):
        print("get")
        data = connection.recv(3).decode('utf-8')
        print(data)
        if len(data)==0:
            return ""
        n=int(data)
        data = connection.recv(n).decode('utf-8')
        print( 'received "%s"' % data)
        return data   

def ZeroVoltages():
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=0.0
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=0.0
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=0.0
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=0.0
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=0.0
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=0.0
    bdaq.ChangeDACValues(bdaq.cal["biasvoltages"])

def OneVoltages():
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=0.1
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=0.1
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=0.1
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=0.1
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=0.1
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=0.1
    bdaq.ChangeDACValues(bdaq.cal["biasvoltages"])

def TwoVoltages():
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=0.2
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=0.2
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=0.2
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=0.2
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=0.2
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=0.2
    bdaq.ChangeDACValues(bdaq.cal["biasvoltages"])

def ThreeVoltages():
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=0.3
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=0.3
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=0.3
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=0.3
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=0.3
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=0.3
    bdaq.ChangeDACValues(bdaq.cal["biasvoltages"])
    
def SpinVoltageUp():
    for i in np.arange(0,defaultVZero+.02,.02):
        print(i)
        bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=i
        bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=i
        bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=i
        bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=i
        bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=i
        bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=i
        bdaq.ChangeDACValues(bdaq.cal["biasvoltages"])

def SpinVoltageDown():
    for i in np.arange(defaultVZero,0,-.02):
        print(i)
        bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=i
        bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=i
        bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=i
        bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=i
        bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=i
        bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=i
        bdaq.ChangeDACValues(bdaq.cal["biasvoltages"])
    ZeroVoltages()
    
def SetVoltages(Bias1, Bias2, Clamp0,Clamp1, Clamp2, Clamp3):
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=Bias1
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=Bias2
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=Clamp0-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP0"]]
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=Clamp1-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP1"]]
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=Clamp2-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP2"]]
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=Clamp3-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP3"]]
    bdaq.ChangeDACValues(bdaq.cal["biasvoltages"])
    
# ===================================
def NullCurrentOffsets():
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=defaultVZero
    bdaq.ChangeDACValues(bdaq.cal["biasvoltages"])
    time.sleep(0.1)
    bdaq.NullCurrentOffsetsBA()
    time.sleep(0.1)
    bdaq.SaveConfig()

def NullVoltageOffsets():
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=defaultVZero
    
    bdaq.cal["biascorrections"][bdaq.DACs["VBIAS1"]]=0
    bdaq.cal["biascorrections"][bdaq.DACs["VBIAS2"]]=0
    bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP0"]] = bdaq.FindZeroCurrentVoltageBA(bdaq.DACs["VBIAS1"],bdaq.inputs["CH0"],1)
    time.sleep(0.1)
    bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP1"]] = bdaq.FindZeroCurrentVoltageBA(bdaq.DACs["VBIAS2"],bdaq.inputs["CH1"],1)
    time.sleep(0.1)
    bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP2"]] = bdaq.FindZeroCurrentVoltageBA(bdaq.DACs["VBIAS2"],bdaq.inputs["CH2"],1)
    time.sleep(0.1)
    bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP3"]] = bdaq.FindZeroCurrentVoltageBA(bdaq.DACs["VBIAS2"],bdaq.inputs["CH3"],1)
    time.sleep(0.1)
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=defaultVZero
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=defaultVZero-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP0"]]
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=defaultVZero-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP1"]]
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=defaultVZero-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP2"]]
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=defaultVZero-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP3"]]
    
    bdaq.SaveConfig()

    bdaq.ChangeDACValues(bdaq.cal["biasvoltages"])
    time.sleep(0.1)
    bdaq.NullCurrentOffsetsBA()
    time.sleep(0.1)
    bdaq.SaveConfig()
    

def RunConstant(skipPoints,runTime):
    # constant bias
    constantprogram = bdaq.program_setbias(bdaq.cal["biasvoltages"])
    bdaq.LoadMM(constantprogram)         # load program into MM
    # soft reset to initialize FIFO
    bdaq.SoftReset()
    time.sleep(0.1)
    # start waveform and acquire data
    bdaq.StartMMandADCFIFO()             # start waveform and acquisition
    time.sleep(2)                        # pause for data buffer to fill
    bdaq.GetDataByTimeNet(True,skipPoints,runTime,1,sock2)         # retrieve data
    print("inServer")
    # to scale this to current, one would use:
     
    
    
def RunSweep(sweepShape,sweepsteptime,sweeprange):    
    if (sweepShape=='SawIon'):
        sweepchannel = bdaq.DACs["VBIAS1"]
        sweepvoltages = list(np.ones(10)*bdaq.cal["biasvoltages"][sweepchannel]) + list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel],bdaq.cal["biasvoltages"][sweepchannel]+sweeprange,num=20))+list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel]+sweeprange,bdaq.cal["biasvoltages"][sweepchannel]-sweeprange,num=40))+list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel]-sweeprange,bdaq.cal["biasvoltages"][sweepchannel],num=20)) + list(np.ones(10)*bdaq.cal["biasvoltages"][sweepchannel])
        sweepprogram = bdaq.program_waveform(bdaq.cal["biasvoltages"],sweepchannel,sweepvoltages,sweepsteptime)    
        plt.plot(sweepvoltages)
        plt.show()
    if (sweepShape=='SawTunnel'):
        sweepchannel = bdaq.DACs["VBIAS2"]
        stretch=1
        sweepvoltages = list(np.ones(10)*bdaq.cal["biasvoltages"][sweepchannel]) + list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel],bdaq.cal["biasvoltages"][sweepchannel]+sweeprange,num=20*stretch))+list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel]+sweeprange,bdaq.cal["biasvoltages"][sweepchannel]-sweeprange,num=40*stretch))+list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel]-sweeprange,bdaq.cal["biasvoltages"][sweepchannel],num=20*stretch)) + list(np.ones(10)*bdaq.cal["biasvoltages"][sweepchannel])
        #sweepvoltages = list(np.ones(10)*bdaq.cal["biasvoltages"][sweepchannel]) + list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel],bdaq.cal["biasvoltages"][sweepchannel]+sweeprange,num=20))+list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel]+sweeprange,bdaq.cal["biasvoltages"][sweepchannel]-sweeprange,num=40))+list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel]-sweeprange,bdaq.cal["biasvoltages"][sweepchannel],num=20)) + list(np.ones(10)*bdaq.cal["biasvoltages"][sweepchannel])
        sweepprogram = bdaq.program_waveform(bdaq.cal["biasvoltages"],sweepchannel,sweepvoltages,sweepsteptime)
        plt.plot(sweepvoltages)
        plt.show()
    if (sweepShape=='SquareIon'):
        stepchannel = bdaq.DACs["VBIAS1"]
        stepsize = 0.5
        sweepvoltages = [bdaq.cal["biasvoltages"][sweepchannel],bdaq.cal["biasvoltages"][sweepchannel]+stepsize,bdaq.cal["biasvoltages"][sweepchannel]-stepsize,bdaq.cal["biasvoltages"][sweepchannel]]
        sweepprogram = bdaq.program_waveform(bdaq.cal["biasvoltages"],stepchannel,sweepvoltages,sweepsteptime)
    if (sweepShape=='SquareTunnel'):
        stepchannel = bdaq.DACs["VBIAS2"]
        stepsize = 0.5
        sweepvoltages = [bdaq.cal["biasvoltages"][sweepchannel],bdaq.cal["biasvoltages"][sweepchannel]+stepsize,bdaq.cal["biasvoltages"][sweepchannel]-stepsize,bdaq.cal["biasvoltages"][sweepchannel]]
        sweepprogram = bdaq.program_waveform(bdaq.cal["biasvoltages"],stepchannel,sweepvoltages,sweepsteptime)

    bdaq.LoadMM(sweepprogram)                # load program into MM
    # soft reset to initialize FIFO
    bdaq.SoftReset()
    time.sleep(0.1)
    # start waveform and acquire data
    bdaq.StartMMandADCFIFO()               # start waveform and acquisition
    time.sleep(2)                         # pause for data buffer to fill
    xfertime=sweepsteptime*len(sweepvoltages)
    bdaq.GetDataByTimeNet(True,100,xfertime,1,sock2)         # retrieve data
    
    bdaq.SaveTempLogFile('tempSweep.bdaq','c:\\temp')
    iData,log_cal = bdaq.ReadLogFile('c:\\temp\\tempSweep.bdaq')
    #iData,xferbytes =bdaq.GetDataByTime(xfertime,1)
    print("plotting") 
    iData = bdaq.ScaletoIData(iData,bdaq.cal)
    #Plot(iData,bdaq.cal)  
    nPoints =np.shape(iData)[0]
    sweepCurrents = np.zeros((len(sweepvoltages), bdaq.cal['numchannels']))
    stepPoints =  nPoints/len(sweepvoltages) 
    for i in np.arange(0,len(sweepvoltages)):
        x=int(stepPoints*i+stepPoints/2)
        sweepCurrents[i,:]= np.mean(iData[(x-5):(x+5),:],axis=0)
    
    x=np.asarray(sweepvoltages)
    for c in range(bdaq.cal['numchannels']):
        y=sweepCurrents[:,c]*1e12
        out_file = open('c:\\temp\\temp_channel_' + str(c) + 'S.bin',"wb")
        l= [1, len(y)]
        s=struct.pack('i'*2,*l)
        out_file.write(s)
        trace = np.asarray(y)
        s=struct.pack('d'*len(trace), *trace)
        out_file.write(s)
        out_file.close()
        plt.plot(x,(y-np.mean(y)))

    plt.show()
    
    print("voltage")
    out_file = open('c:\\temp\\temp_VoltageR.bin',"wb")
    l= [1, len(sweepvoltages)]
    s=struct.pack('i'*2,*l)
    out_file.write(s)
    trace = np.asarray(sweepvoltages)
    s=struct.pack('d'*len(trace), *trace)
    out_file.write(s)
    out_file.close()
    
    

def ScaleCalibration():
    SendMessage(connection,str( bdaq.cal["numchannels"]));
    for c in range(bdaq.cal["numchannels"]):
       time.sleep(0.2)
       t=str( bdaq.cal["in_os"][c] )
       SendMessage(connection, t)
       time.sleep(0.2)
       t=str( bdaq.cal["in_gain"][c])
       SendMessage(connection, t)
    t=str(bdaq.cal["samplerate"])
    SendMessage(connection, t)

def Plot(log_Idata,log_cal):
    t = np.arange(0,log_Idata.shape[0])/log_cal["samplerate"]

    for c in range(log_cal["numchannels"]):
        print(str(bdaq.inputs_reverse[c]) + " Mean = " + str(np.mean(log_Idata[int(0.5*log_cal["samplerate"])::,c]*1e9)) + "nA")
        plt.plot(t,log_Idata[:,c]*1e9,label=(str(bdaq.inputs_reverse[c])))
        plt.title("channel "+str(c))
        plt.ylabel("nA")
        plt.xlabel("sec")
        #plt.axis([0,t[len(t)-1],-10,10])
    plt.legend(loc=1)    
    plt.show()
    
    
    
    for c in range(log_cal["numchannels"]):
        TraceForPSD = log_Idata[int(0.5*log_cal["samplerate"])::,c]    # remove initial transient
        #TraceForPSD = TraceForPSD - np.mean(TraceForPSD)   # subtract mean
        f,PSD = signal.welch(TraceForPSD,log_cal["samplerate"],nperseg=2**17)
        RMS = np.sqrt(integrate.cumtrapz(PSD,f))
    
        plt.loglog(f[1::],RMS*1e12,label=(str(bdaq.inputs_reverse[c])))
        plt.xlabel('Hz')
        plt.ylabel('pArms')
        #plt.title("channel "+str(c))
        plt.grid(True,which="both")
    plt.legend(loc=2)    
    plt.show()


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


bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=defaultVZero
bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=defaultVZero
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=defaultVZero-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP0"]]
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=defaultVZero-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP1"]]
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=defaultVZero-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP2"]]
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=defaultVZero-bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP3"]]
 
    
# initialize hardware
try:    
    bdaq.BootFourClamp(bdaq.cal["biasvoltages"])
except (Exception):    
    bdaq.Disconnect()
    time.sleep(2)
    bdaq.BootFourClamp(bdaq.cal["biasvoltages"])
    print("Already open")
    
bdaq.SoftReset()

# Bind the socket to the address given on the command line
sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_address = ('127.0.0.1', 10000)
print ( 'starting up on %s port %s' % server_address)
sock.bind(server_address)
sock.listen(1)

sock2 = socket.socket(socket.AF_INET,socket.SOCK_DGRAM) # UDP

superDone =False
while superDone ==False:
    print("Waiting for client")
    #temp=sock.gettimeout()
    #sock.settimeout(.51)
    connection, client_address = sock.accept()
    #sock.settimeout(temp)
    message = 'Server is ready:Done'
    print ( 'sending "%s"' % message)
    SendMessage(connection,message)
    time.sleep(0.1)
    
    try:
        done=False
        while done==False:
            data=GetMessage(connection)
            if(data=='SuperDone'):
                done=True
                superDone=True
            if(data=='Done'):
                done=True
                SendMessage(connection,"Done")
            if (data =='NullI'):
                bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=defaultVZero
                bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=defaultVZero
                bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=defaultVZero
                bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=defaultVZero
                bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=defaultVZero
                bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=defaultVZero
                NullCurrentOffsets()
                SendMessage(connection,"NulledI:Done")
            if (data=='NullV'):
                bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=defaultVZero
                bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=defaultVZero
                bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=defaultVZero
                bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=defaultVZero
                bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=defaultVZero
                bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=defaultVZero
                NullVoltageOffsets()
                SendMessage(connection,"NulledV:Done")
            if (data=='ChannelNames'):
                bdaq.cal["channelnames"]=list(np.zeros(6,np.dtype(np.str)))
                bdaq.cal["channelnames"][0]=GetMessage(connection)
                bdaq.cal["channelnames"][1]=GetMessage(connection)
                bdaq.cal["channelnames"][2]=GetMessage(connection)
                bdaq.cal["channelnames"][3]=GetMessage(connection)
                bdaq.cal["channelnames"][4]=GetMessage(connection)
                bdaq.cal["channelnames"][5]=GetMessage(connection)
                SendMessage(connection,"ChannelNames:Done")                
            if (data=='Calibration'):
                ScaleCalibration()
                SendMessage(connection,"Calibration:Done")
            if (data=='Constant'):
                skipPoints =int(GetMessage(connection))
                sampleRate = float(GetMessage(connection))
                runTime=float(GetMessage(connection))
                bdaq.cal["samplerate"]=sampleRate
                RunConstant(skipPoints,runTime)
                SendMessage(connection,"Trace:Done")
            if (data=='Sweep'):
                iData=[]
                SweepType=GetMessage(connection)
                SweepSpeed=float(GetMessage(connection))   
                SweepAmp=float(GetMessage(connection))
                RunSweep(SweepType,SweepSpeed,SweepAmp)
                SendMessage(connection,"Sweep:Done")
            if (data=='Save'):
                dirname = GetMessage(connection)
                logname = GetMessage(connection)
                writelogfilename = bdaq.SaveLogFile(iData,logname,dirname)
            if (data=='SaveTempFile'):
                dirname = GetMessage(connection)
                logname = GetMessage(connection)
                writelogfilename = bdaq.SaveTempLogFile(logname,dirname)                
                SendMessage(connection,"SaveTemp:Done")        
            if (data=='SaveTempBinFile'):
                dirname = GetMessage(connection)
                logname = GetMessage(connection)
                writelogfilename = bdaq.SaveTempBinFile(logname,dirname)                
                SendMessage(connection,"SaveTemp:Done")        
            if (data=='Read'):
                readlogfilename = GetMessage(connection)
                iData,log_cal = bdaq.ReadLogFile(readlogfilename)
                iData = bdaq.ScaletoIData(iData,log_cal)
                SendMessage(connection,"read:Done")        
            if (data =='Plot'):
                Plot(iData,bdaq.cal)
                SendMessage(connection,"plot:Done")
            if (data =='ZeroVoltages'):
                ZeroVoltages()
                SendMessage(connection,'ZeroVoltages:Done')
            if (data =='OneVoltages'):
                ZeroVoltages()
                SendMessage(connection,'OneVoltages:Done')
            if (data =='TwoVoltages'):
                ZeroVoltages()
                SendMessage(connection,'TwoVoltages:Done')
            if (data =='ThreeVoltages'):
                ZeroVoltages()
                SendMessage(connection,'ThreeVoltages:Done')
            if (data =='SpinUp'):
                SpinVoltageUp()
                SendMessage(connection,'SpinUp:Done')
            if (data =='SpinDown'):
                SpinVoltageDown()
                SendMessage(connection,'SpinDown:Done')
            if (data=='SetVoltages'):
                Bias1 =float( GetMessage(connection) )+defaultVZero
                Bias2 =float( GetMessage(connection) )+defaultVZero
                Clamp0 =float( GetMessage(connection) )+defaultVZero
                Clamp1 =float( GetMessage(connection) )+defaultVZero
                Clamp2 =float( GetMessage(connection) )+defaultVZero
                Clamp3 =float( GetMessage(connection) )+defaultVZero
                SetVoltages(Bias1, Bias2, Clamp0,Clamp1, Clamp2, Clamp3)
                SendMessage(connection,'SetVoltages:Done')
                

        SendMessage(connection,'Closing connection')
    except ConnectionResetError as err:
        print("connection error: {0}".format(err))      
    except TypeError  as err:
        print("connection error: {0}".format(err))      
    except ValueError  as err:
        print("ValueError error: {0}".format(err))      
    except NameError  as err:
        print("NameError error: {0}".format(err))     
    except:
        print("Unexpected error:", sys.exc_info()[0])        
    time.sleep(2)
    connection.close()

# disconnect
bdaq.Disconnect()
