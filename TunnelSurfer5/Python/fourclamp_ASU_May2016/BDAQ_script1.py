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
import socket
import sys


def SendMessage(sock,message):
    n=str(len(message)).zfill(3)    
    sock.sendall(n.encode('utf-8'))
    sock.sendall(message.encode('utf-8'))
    print ( 'sent "%s"' % message)

def GetMessage(sock):
    data = sock.recv(3).decode('utf-8')
    if len(data)==0:
        return ""
    n=int(data)
    data = sock.recv(n).decode('utf-8')
    print( 'received "%s"' % data)
    return data   



def ZeroVoltages():
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=0
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=0
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=0
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=0
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=0
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=0

def OneVoltages():
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=0.1
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=0.1
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=0.1
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=0.1
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=0.1
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=0.1

def TwoVoltages():
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=0.2
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=0.2
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=0.2
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=0.2
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=0.2
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=0.2

def ThreeVoltages():
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=0.3
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=0.3
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=0.3
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=0.3
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=0.3
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=0.3
    
def SetVoltages(Bias1, Bias2, Clamp0,Clamp1, Clamp2, Clamp3):
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=Bias1
    bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=Bias2
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=Clamp0
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=Clamp1
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=Clamp2
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=Clamp3
    
    
# ===================================
def NullCurrentOffsets():
   # tkSimpleDialog.askstring("Null Currents","Remove all connections to board and then press Ok:")
    bdaq.ChangeDACValues(bdaq.cal["biasvoltages"])
    time.sleep(0.1)
    bdaq.NullCurrentOffsets()
    time.sleep(0.1)
    bdaq.SaveConfig()

def NullVoltageOffsets():
    #tkSimpleDialog.askstring("Null Currents","Plug high impedience circuit into board and then press Ok:")
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]] = bdaq.FindZeroCurrentVoltage(bdaq.DACs["VCLAMP0"],bdaq.inputs["CH0"],1)
    time.sleep(0.1)
    #bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]] = bdaq.FindZeroCurrentVoltage(bdaq.DACs["VCLAMP1"],bdaq.inputs["CH1"],1)
    #time.sleep(0.1)
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]] = bdaq.FindZeroCurrentVoltage(bdaq.DACs["VCLAMP2"],bdaq.inputs["CH2"],1)
    time.sleep(0.1)
    bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]] = bdaq.FindZeroCurrentVoltage(bdaq.DACs["VCLAMP3"],bdaq.inputs["CH3"],1)
    time.sleep(0.1)
    bdaq.SaveConfig()

def RunConstant():
    # constant bias
    constantprogram = bdaq.program_setbias(bdaq.cal["biasvoltages"])
    bdaq.LoadMM(constantprogram)            # load program into MM
    # soft reset to initialize FIFO
    bdaq.SoftReset()
    time.sleep(0.1)
    # start waveform and acquire data
    bdaq.StartMMandADCFIFO()               # start waveform and acquisition
    time.sleep(2)                         # pause for data buffer to fill
    rawdata,xferbytes = bdaq.GetDataByTimeNet(8,1,sock2)         # retrieve data
    # to scale this to current, one would use:
    return bdaq.ScaletoIData(rawdata,bdaq.cal)    
    
    
def RunSweep(sweepShape,sweepsteptime):    
    if (sweepShape=='SawIon'):
        # sweep
        sweepchannel = bdaq.DACs["VBIAS1"]
        sweeprange = 0.2
        sweepvoltages = list(np.ones(10)*bdaq.cal["biasvoltages"][sweepchannel]) + list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel],bdaq.cal["biasvoltages"][sweepchannel]+sweeprange,num=20))+list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel]+sweeprange,bdaq.cal["biasvoltages"][sweepchannel]-sweeprange,num=40))+list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel]-sweeprange,bdaq.cal["biasvoltages"][sweepchannel],num=20)) + list(np.ones(10)*bdaq.cal["biasvoltages"][sweepchannel])
        #sweepsteptime = 0.08
        sweepprogram = bdaq.program_waveform(bdaq.cal["biasvoltages"],sweepchannel,sweepvoltages,sweepsteptime)    
    if (sweepShape=='SawTunnel'):
        # sweep
        sweepchannel = bdaq.DACs["VBIAS2"]
        sweeprange = 0.2
        sweepvoltages = list(np.ones(10)*bdaq.cal["biasvoltages"][sweepchannel]) + list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel],bdaq.cal["biasvoltages"][sweepchannel]+sweeprange,num=20))+list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel]+sweeprange,bdaq.cal["biasvoltages"][sweepchannel]-sweeprange,num=40))+list(np.linspace(bdaq.cal["biasvoltages"][sweepchannel]-sweeprange,bdaq.cal["biasvoltages"][sweepchannel],num=20)) + list(np.ones(10)*bdaq.cal["biasvoltages"][sweepchannel])
        #sweepsteptime = 0.08
        sweepprogram = bdaq.program_waveform(bdaq.cal["biasvoltages"],sweepchannel,sweepvoltages,sweepsteptime)
    if (sweepShape=='SquareIon'):
        # step
        stepchannel = bdaq.DACs["VBIAS1"]
        #steptime = 0.5
        stepsize = 0.5
        stepvoltages = [bdaq.cal["biasvoltages"][sweepchannel],bdaq.cal["biasvoltages"][sweepchannel]+stepsize,bdaq.cal["biasvoltages"][sweepchannel]-stepsize,bdaq.cal["biasvoltages"][sweepchannel]]
        sweepprogram = bdaq.program_waveform(bdaq.cal["biasvoltages"],stepchannel,stepvoltages,sweepsteptime)
    if (sweepShape=='SquareTunnel'):
        # step
        stepchannel = bdaq.DACs["VBIAS2"]
        #steptime = 0.5
        stepsize = 0.5
        stepvoltages = [bdaq.cal["biasvoltages"][sweepchannel],bdaq.cal["biasvoltages"][sweepchannel]+stepsize,bdaq.cal["biasvoltages"][sweepchannel]-stepsize,bdaq.cal["biasvoltages"][sweepchannel]]
        sweepprogram = bdaq.program_waveform(bdaq.cal["biasvoltages"],stepchannel,stepvoltages,sweepsteptime)

    bdaq.LoadMM(sweepprogram)                # load program into MM
    # soft reset to initialize FIFO
    bdaq.SoftReset()
    time.sleep(0.1)
    # start waveform and acquire data
    bdaq.StartMMandADCFIFO()               # start waveform and acquisition
    time.sleep(2)                         # pause for data buffer to fill
    rawdata,xferbytes = bdaq.GetDataByTimeNet(8,1,sock2)         # retrieve data
    # to scale this to current, one would use:
    return bdaq.ScaletoIData(rawdata,bdaq.cal)


def Plot(log_Idata,log_cal):
    t = np.arange(0,log_Idata.shape[0])/log_cal["samplerate"]

    for c in range(log_cal["numchannels"]):
        print(str(bdaq.inputs_reverse[c]) + " Mean = " + str(np.mean(log_Idata[int(0.5*log_cal["samplerate"])::,c]*1e9)) + "nA")
        plt.plot(t,log_Idata[:,c]*1e9,label=(str(bdaq.inputs_reverse[c])))
        plt.title("channel "+str(c))
        plt.ylabel("nA")
        plt.xlabel("sec")
        plt.axis([0,t[len(t)-1],-10,10])
    plt.legend(loc=1)    
    plt.show()
    
    
    
    for c in range(log_cal["numchannels"]):
        TraceForPSD = log_Idata[int(0.5*log_cal["samplerate"])::,c]    # remove initial transient
        TraceForPSD = TraceForPSD - np.mean(TraceForPSD)   # subtract mean
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

# ===================================
# define bias point

bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]]=0
bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]]=0
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]]=0
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]]=0
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]]=0
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]]=0
    
    
# initialize hardware
bdaq.BootFourClamp(bdaq.cal["biasvoltages"])

bdaq.SoftReset()

# Create a TCP/IP socket
sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

# Connect the socket to the port on the server given by the caller
server_address = ('127.0.0.1', 10000)
print ( 'connecting to %s port %s' % server_address)
sock.connect(server_address)
message = 'Device is ready:Done'
print ( 'sending "%s"' % message)
SendMessage(sock,message)

time.sleep(0.1)
data=GetMessage(sock)



sock2 = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

# Connect the socket to the port on the server given by the caller
server_address = ('127.0.0.1', 10001)
print ( 'connecting to %s port %s' % server_address)
sock2.connect(server_address)
message = 'Data is ready:Done'
print ( 'sending "%s"' % message)
SendMessage(sock2,message)

try:

    done=False
    # ===================================
    while done==False:
        data=GetMessage(sock)
        if(data=='Done'):
            done=True
        if (data =='NullI'):
            NullCurrentOffsets()
            SendMessage(sock,"NulledI:Done")
        if (data=='NullV'):
            NullVoltageOffsets()
            SendMessage(sock,"NulledV:Done")
        if (data=='Constant'):
            RunConstant()
            SendMessage(sock,"Trace:Done")
        if (data=='Sweep'):
            iData=[]
            SweepType=GetMessage(sock)
            SweepSpeed=float(GetMessage(sock))                
            iData=RunSweep(SweepType,SweepSpeed)
            SendMessage(sock,"Sweep:Done")
        if (data=='Save'):
            dirname = GetMessage(sock)
            logname = GetMessage(sock)
            writelogfilename = bdaq.SaveLogFile(iData,logname,dirname)
            SendMessage(sock,"log:Done")        
        if (data=='Read'):
            readlogfilename = GetMessage(sock)
            iData,log_cal = bdaq.ReadLogFile(readlogfilename)
            iData = bdaq.ScaletoIData(iData,log_cal)
            SendMessage(sock,"read:Done")        
        if (data =='Plot'):
            Plot(iData,bdaq.cal)
            SendMessage(sock,"plot:Done")
        if (data =='ZeroVoltages'):
            ZeroVoltages()
            SendMessage(sock,'ZeroVoltages:Done')
        if (data =='OneVoltages'):
            ZeroVoltages()
            SendMessage(sock,'OneVoltages:Done')
        if (data =='TwoVoltages'):
            ZeroVoltages()
            SendMessage(sock,'TwoVoltages:Done')
        if (data =='ThreeVoltages'):
            ZeroVoltages()
            SendMessage(sock,'ThreeVoltages:Done')
        if (data=='SetVoltages'):
            Bias1 =float( GetMessage(sock) )
            Bias2 =float( GetMessage(sock) )
            Clamp0 =float( GetMessage(sock) )
            Clamp1 =float( GetMessage(sock) )
            Clamp2 =float( GetMessage(sock) )
            Clamp3 =float( GetMessage(sock) )
            SetVoltages(Bias1, Bias2, Clamp0,Clamp1, Clamp2, Clamp3)
            SendMessage(sock,'SetVoltages:Done')
    
    SendMessage(sock,'Device Shutting down')
except (ConnectionResetError,TypeError,ValueError):
    print('connectionerror')
time.sleep(2)
sock.close()
sock2.close()
        

# disconnect
bdaq.Disconnect()
