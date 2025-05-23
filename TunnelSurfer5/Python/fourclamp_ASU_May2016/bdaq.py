# -*- coding: utf-8 -*-
"""
@author: jrosenstein
"""

import numpy as np
import ok
import time
import bdaq
import json
import os
from datetime import datetime
import re
import struct
from matplotlib import pyplot as plt


#############################################################################
# hardware calibration constants

xfersize = 2*1024*1024

cal = {}
cal["system"] = "BDAQ fourclamp v2 2016"
#cal["currentdatetime"] = datetime.now()
cal["ch0_10M"] = False
cal["downsampleratio"] = 1
cal["numchannels"] = 4
cal["samplerate"] = 100e6/16/cal["downsampleratio"]/cal["numchannels"]*2
cal["in_os"] = list(np.zeros(cal["numchannels"],dtype=np.float64))
cal["in_bits"] = 16
cal["in_vfs"] = 2
cal["in_transimpedance"] = 100e6*2*0.2
cal["in_gain"] = list(np.ones(cal["numchannels"],dtype=np.float64) * (2**cal["in_bits"]) / cal["in_vfs"] * cal["in_transimpedance"])

cal["out_vfs"] = list(np.ones(cal["numchannels"],dtype=np.float64)*1.0)
cal["out_vos"] = list(np.zeros(cal["numchannels"],dtype=np.float64))

# individual channel offset calibration
cal["in_os"][0] = 0
cal["in_os"][1] = 0
cal["in_os"][2] = 0
cal["in_os"][3] = 0

# individual channel gain calibration
cal["in_gain"][0] *= (1/4.0)
cal["in_gain"][1] *= (1/4.0)
cal["in_gain"][2] *= (1/4.0)
cal["in_gain"][3] *= (1/4.0)

if(bdaq.cal["ch0_10M"]):
    cal["in_gain"][0] *= 0.1


cal["biasvoltages"] = list(np.empty(8))
cal["biascorrections"] =list(np.empty(8))
#############################################################################
# register addresses and control bit locations

epaddr ={    "WireInTEST1" :                 0x00,
             "WireInADCMUX" :                0x01,
             "WireInGeneral" :               0x02,
             "WireInScan" :                  0x03,
             "WireInReset" :                 0x04,
             "WireInMM" :                    0x05,
             "WireInBDAQ" :                  0x06,
             "WireInDemodConfig" :           0x07,
             "WireInBDAQmuxselect" :         0x08,
             "WireInBDAQscanselect" :        0x09,
             "WireInBDAQdownsampleratio" :   0x0A,
             "WireOutTEST1" :                0x20,
             "WireOutADCFIFOCOUNT" :         0x21,
             "WireOutMMFIFOCOUNT" :          0x22,
             "TriggerInDACMM" :              0x40,
             "TrigOutTEST1" :                0x60,
             "WireOutDeviceId" :             0x3e,
             "WireOutDeviceVersion" :        0x36,
             "WireOutDemodValue" :           0x37,
             "WireOutHWAverages" :           0x38,
             "PipeInVECTOR" :                0x80,
             "PipeInDACMM" :                 0x81,
             "PipeInHOLDSECONDS" :           0x82,
             "PipeOutADC" :                  0xA0,
             "PipeOutMMCURRENTSTATE" :       0xA1,
        }

epbitshift = {  "SCANSEQUENCE" :    4,
                "INPUTSELECT1" :    16,
                "INPUTSELECT2" :    20, 
                }

epbit = {    "GLOBALRESET" :            1<<0,
             "ADCFIFORESET" :           1<<1,
             "MMRESET" :                1<<2,
             "DAC_enable_pin" :         1<<3,
             "ENABLEADCFIFOWRITE" :     1<<4,
             "ADCDATAREADY" :           1<<0,
             "MM_RUN" :                 1<<0,
             "MM_CLKSELECT" :           1<<1,
             "MM_FORCETRIGGER" :        1<<2,
             "BDAQ_INPUTSELECT1" :      0xF<<epbitshift["INPUTSELECT1"],
             "BDAQ_INPUTSELECT2" :      0xF<<epbitshift["INPUTSELECT2"],
             "BDAQ_SCAN_ALL_INPUTS" :   1<<24,
             "HWDOWNSAMPLE" :           1<<25,
             "BDAQ_MUX_ALL" :           0x0000FFFF,     
             "GAINSELECT" :             1<<27,
        }

inputselect = { "ADC_A" :   0,
                "ADC_B" :   1,
                "ADC_C" :   2,
                "ADC_D" :   3,
                "ADC_E" :   4,
                "ADC_F" :   5,
                "ADC_G" :   6,
                "ADC_H" :   7,
                "MMADDR" :  8,
                "HWDEMOD" : 9,
                }


#############################################################################
# MM definitions

CMD_NULL               = 0
CMD_Delay			= 1
CMD_WaitForTrigger	= 2
CMD_SendToADCSPI		= 4
CMD_SendToDAC		= 5
CMD_Idle			= 7
CMD_Restart			= 8
CMD_GoTo			= 9
CMD_RegisterSet		= 10
CMD_RegisterAdd		= 11
CMD_RegisterSubtract	= 12
CMD_RegisterToDAC		= 15
CMD_CalculateMean		= 17
CMD_QueueRegisterForDAC = 18
CMD_LDAC			= 19
CMD_SetFlags            = 22
CMD_RegistersToFourClamp = 23

AD5668FullScale = 2**16
AD5668Vfs = 5*0.5
AD5668Offset = 0
def GET_AD5668_CODE(DAC_VOLTAGE): return ((DAC_VOLTAGE+AD5668Offset)/AD5668Vfs*AD5668FullScale);

AD5668_WRITE_SINGLE = 0x0
AD5668_UPDATE_SINGLE = 0x1 
AD5668_WRITE_SINGLE_UPDATE_ALL = 0x2 
AD5668_WRITE_UPDATE_SINGLE = 0x3
AD5668_POWERON = 0x4
AD5668_SETUP_CLEAR = 0x5
AD5668_SETUP_LDAC = 0x6
AD5668_RESET = 0x7
AD5668_SETUP_REF = 0x8
AD5668_ALLDACS = 0xF
def REGISTERS_TO_FOURCLAMP(REG, ADDR, CMD): return ((np.uint32(REG)<<8) + (np.uint32(ADDR)<<12) + (np.uint32(CMD)<<16) + CMD_RegistersToFourClamp)

DelayFreq = 100000000/16 * 2**-6
def DELAYSEC(MYSECONDS): return ((np.uint32(MYSECONDS*DelayFreq)<<8) + CMD_Delay)

IDLE = CMD_Idle
RESTART = CMD_Restart;

def GOTO(GOTO_ADDR): return ((GOTO_ADDR<<8) + CMD_GoTo);

def REGISTERSET(REGISTER_NUMBER,VALUE): return ((np.uint32(REGISTER_NUMBER)<<8) + (np.uint32(VALUE)<<16) + CMD_RegisterSet)
def REGISTERADD(REGISTER_NUMBER,VALUE): return ((np.uint32(REGISTER_NUMBER)<<8) + (np.uint32(VALUE)<<16) + CMD_RegisterAdd)
def REGISTERSUBTRACT(REGISTER_NUMBER,VALUE): return ((np.uint32(REGISTER_NUMBER)<<8) + (np.uint32(VALUE)<<16) + CMD_RegisterSubtract)

def ADCSPIwrite(ADC_INSTRUCTION,ADC_DATA): return ((ADC_INSTRUCTION<<16) + (ADC_DATA<<8) + CMD_SendToADCSPI)

#############################################################################
# fourclamp DAC channel numbers

DACs = {    "VBIAS1" : 0,
            "VBIAS2" : 2,
            "VCLAMP0" : 6,
            "VCLAMP1" : 7,
            "VCLAMP2" : 3,
            "VCLAMP3" : 1,
            "unused1" : 4,
            "unused2" : 5,
        }

# default values
bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS1"]] = 0
bdaq.cal["biasvoltages"][bdaq.DACs["VBIAS2"]] = 0
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP0"]] = 0
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP1"]] = 0
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP2"]] = 0
bdaq.cal["biasvoltages"][bdaq.DACs["VCLAMP3"]] = 0       

bdaq.cal["biasvoltages"][bdaq.DACs["unused1"]] = 0       
bdaq.cal["biasvoltages"][bdaq.DACs["unused2"]] = 0       

bdaq.cal["biascorrections"][bdaq.DACs["VBIAS1"]] = 0
bdaq.cal["biascorrections"][bdaq.DACs["VBIAS2"]] = 0
bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP0"]] = 0
bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP1"]] = 0
bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP2"]] = 0
bdaq.cal["biascorrections"][bdaq.DACs["VCLAMP3"]] = 0    

inputs =    {   "CH0" :     0,
                "CH1" :     3,
                "CH2" :     2,
                "CH3" :     1,
            }

inputs_reverse  =    {    0 : "CH0",
                          1 : "CH3",
                          2 : "CH2",
                          3 : "CH1",
            }


#############################################################################

# handle for FPGA FrontPanel interface

xem = ok.okCFrontPanel()


#############################################################################

def ConfigureFPGA():
    
    # initialize the OK device
    #xem = ok.okCFrontPanel()
    
    if(xem.OpenBySerial("") != xem.NoError):
        raise Exception("Failed to open device.")
    
    # get the OK device details
    devInfo = ok.okTDeviceInfo()
    if(xem.GetDeviceInfo(devInfo) != xem.NoError):
        raise Exception("Failed to get device info.")
    else:
        print("Product: " + devInfo.productName)
        print("Serial Number: " + devInfo.serialNumber)
    
    # download the FPGA .bit file
    bitfilename = os.path.join(os.getcwd(),'bitfile_fpga.bit');
    if(xem.ConfigureFPGA(bitfilename) != xem.NoError):
        raise Exception("Failed to load FPGA .bit file")
    else:
        print("Successfully loaded " + bitfilename)


#############################################################################


def Initialize():

    # assert global reset
    xem.SetWireInValue(epaddr["WireInReset"], epbit["GLOBALRESET"], epbit["GLOBALRESET"])   
    xem.UpdateWireIns()
    
    # enable DAC
    xem.SetWireInValue(epaddr["WireInGeneral"], epbit["DAC_enable_pin"], epbit["DAC_enable_pin"] );    
    xem.UpdateWireIns()
    
    # select always-running adc_clkout
    # 0 = always running adc_clkin, 1 = ADC_FCO
    clockselect = 0
    xem.SetWireInValue(epaddr["WireInMM"], clockselect*epbit["MM_CLKSELECT"], epbit["MM_CLKSELECT"] );    
            
    # de-assert global reset
    xem.SetWireInValue(epaddr["WireInReset"], 0, epbit["GLOBALRESET"])   
    xem.UpdateWireIns()
    
    # initialize ADC AD9257
    # normal mode
    programADC = np.uint32([    ADCSPIwrite(0x08,0x03), # reset
                                ADCSPIwrite(0xFF,0x01), # update
                                ADCSPIwrite(0x08,0x00), # chip run
                                ADCSPIwrite(0xFF,0x01), # update
                                ADCSPIwrite(0x0B,0x00), # set internal clock divider to 1
                                ADCSPIwrite(0xFF,0x01), # update
                                ADCSPIwrite(0x05,0x0F), # select channels to be programmed
                                ADCSPIwrite(0xFF,0x01), # update
                                ADCSPIwrite(0x18,0x04), # set internal Vref = 2Vpp
                                ADCSPIwrite(0xFF,0x01), # update
                                ADCSPIwrite(0x16,0x02), # set output clock phase
                                ADCSPIwrite(0xFF,0x01), # update
                                ADCSPIwrite(0x14,0x00), # output mode offset binary
                                ADCSPIwrite(0xFF,0x01), # update
                                ADCSPIwrite(0x0D,0x00), # test mode off
                                ADCSPIwrite(0xFF,0x01), # update
                                IDLE
                            ])
    
    
    # load program into MM
    LoadMM(programADC)    
            
    # start MM
    xem.SetWireInValue(epaddr["WireInMM"], epbit["MM_RUN"], epbit["MM_RUN"] ) 
    xem.UpdateWireIns()


#############################################################################

def SoftReset():
    # soft reset (do not re-load FPGA)
    
    # reset and stop MM
    xem.SetWireInValue(epaddr["WireInReset"], epbit["MMRESET"], epbit["MMRESET"])
    xem.SetWireInValue(epaddr["WireInMM"], 0, epbit["MM_RUN"])
    xem.UpdateWireIns()
    
    time.sleep(0.01)    

    # de-assert MM reset  (it is still stopped)
    xem.SetWireInValue(epaddr["WireInReset"], 0, epbit["MMRESET"])
    xem.UpdateWireIns()

    time.sleep(0.01)    
        
    # select MM to run from frame clock to synchronize with ADC
    # 0 = always running adc_clkin, 1 = ADC_FCO
    clockselect = 1
    xem.SetWireInValue(epaddr["WireInMM"], clockselect*epbit["MM_CLKSELECT"], epbit["MM_CLKSELECT"] );    

    time.sleep(0.01)    
    
    # assert global reset and disable ADC FIFO
    xem.SetWireInValue(epaddr["WireInReset"], epbit["GLOBALRESET"], epbit["GLOBALRESET"])   
    xem.SetWireInValue(epaddr["WireInReset"], epbit["ADCFIFORESET"], epbit["ADCFIFORESET"])   
    xem.SetWireInValue(epaddr["WireInTEST1"], 0, epbit["ENABLEADCFIFOWRITE"] );    
    xem.UpdateWireIns()
        
    time.sleep(0.01)    
        
    # de-assert global reset and FIFO reset. writing is still disabled.
    xem.SetWireInValue(epaddr["WireInReset"], 0, epbit["GLOBALRESET"])   
    xem.SetWireInValue(epaddr["WireInReset"], 0, epbit["ADCFIFORESET"])   
    xem.UpdateWireIns()
    
    time.sleep(0.01)    
    
#############################################################################
    
def LoadMM(program):
    
    # reset and stop MM
    xem.SetWireInValue(epaddr["WireInReset"], epbit["MMRESET"], epbit["MMRESET"])
    xem.SetWireInValue(epaddr["WireInMM"], 0, epbit["MM_RUN"])
    xem.UpdateWireIns()
            
    # de-assert MM reset  (it is still stopped)
    xem.SetWireInValue(epaddr["WireInReset"], 0, epbit["MMRESET"])
    xem.UpdateWireIns()
    
    packet = bytearray(np.uint32(program).view(np.uint8))
    modu = np.mod(len(packet), 16)  # must send multiples of 16 bytes
    if modu>0:
        for x in range(modu,16):
            packet.append(0)
    bytes_sent = xem.WriteToPipeIn(epaddr["PipeInDACMM"], packet)

    if(bytes_sent<0):
        raise Exception("bytes_sent = " + str(bytes_sent))
    else:
        print("Bytes loaded into MM: " + str(bytes_sent))
    
    return bytes_sent
    
#############################################################################

def StartMMandADCFIFO():
    
    xem.SetWireInValue(epaddr["WireInMM"], epbit["MM_RUN"], epbit["MM_RUN"] ) 
    xem.SetWireInValue(epaddr["WireInTEST1"], epbit["ENABLEADCFIFOWRITE"], epbit["ENABLEADCFIFOWRITE"] );    

    xem.UpdateWireIns()
    
#############################################################################
    
def StartMM():
    
    xem.SetWireInValue(epaddr["WireInMM"], epbit["MM_RUN"], epbit["MM_RUN"] ) 

    xem.UpdateWireIns()

#############################################################################

def Disconnect():
    
    xem.Close()
    
    

#############################################################################

def Configure_FourClampAmplifier():

    # default
    inputsequence =  [inputselect["ADC_A"],inputselect["ADC_B"],inputselect["ADC_C"],inputselect["ADC_D"],inputselect["ADC_A"],inputselect["ADC_B"],inputselect["ADC_C"],inputselect["ADC_D"]]
    
    # enable scanning/interleaving of multiple channels
    xem.SetWireInValue(epaddr["WireInBDAQ"], epbit["BDAQ_SCAN_ALL_INPUTS"], epbit["BDAQ_SCAN_ALL_INPUTS"] ) 
    
    # configure scanned input sequence
    scansequence = 0
    for s in range(len(inputsequence)):
        scansequence += np.uint32(inputsequence[s])<<(s*epbitshift["SCANSEQUENCE"])
    
    xem.SetWireInValue(epaddr["WireInBDAQscanselect"], int(scansequence), 0xFFFFFFFF ) 
    
    # select non-multiplexed inputs
    #xem.SetWireInValue(EP_WireInBDAQ, SELECT_MMADDR<<EPBITSHIFT_BDAQ_INPUTSELECT2, EPBITS_BDAQ_INPUTSELECT2 ) 
    #xem.SetWireInValue(EP_WireInBDAQ, SELECT_ADC_A<<EPBITSHIFT_BDAQ_INPUTSELECT2, EPBITS_BDAQ_INPUTSELECT2 ) 

    # enable HW downsampling    
    xem.SetWireInValue(epaddr["WireInBDAQdownsampleratio"], bdaq.cal["downsampleratio"]-1, 0x0000FFFF ) 
    xem.SetWireInValue(epaddr["WireInBDAQ"], epbit["HWDOWNSAMPLE"], epbit["HWDOWNSAMPLE"] ) 
        
    # select AMP_OUT MUX position
    xem.SetWireInValue(epaddr["WireInBDAQmuxselect"], epbit["BDAQ_MUX_ALL"], epbit["BDAQ_MUX_ALL"] )     


    # select transimpedance gain
    if(bdaq.cal["ch0_10M"] == False):
        xem.SetWireInValue(epaddr["WireInBDAQ"], epbit["GAINSELECT"], epbit["GAINSELECT"] ) 
        
    xem.UpdateWireIns()

        
    
#############################################################################
UDP_IP = '127.0.0.1'
UDP_PORT =  10001

def SendMessage(sock, array,skipPoints):
    data = (np.uint8(array)).view(np.uint16).reshape(int( len(array)/2/bdaq.cal["numchannels"]),bdaq.cal["numchannels"])
    nPoints = int(np.floor(np.shape( data)[0]/skipPoints))*skipPoints
    passArray = np.zeros((int(nPoints/skipPoints),bdaq.cal["numchannels"]),dtype=np.uint16)
    for c in range(0,bdaq.cal["numchannels"]):
        passArray[:,c]=np.uint16( data[0:nPoints,c].reshape((-1,skipPoints)).mean(axis=1) )
    try:
        sock.sendto(passArray, (UDP_IP, UDP_PORT))
    except OSError as err:
        print("OSError: {0}".format(err)) 
    print ( 'sent "%s"' % str(nPoints))
    
#############################################################################  

def GetDataByTimeNet(save,skipPoints,xfertime,timeoutseconds,sock):
    
    xferbytes = 2*xfertime*bdaq.cal["samplerate"]*bdaq.cal["numchannels"]
    if (save==True):
        print("streamopen")
        streamFile = open ("c:\\temp\\BDAQStream.bBin", "wb")
        print("start data")
        data=GetDataNet(save,xferbytes,timeoutseconds,sock,streamFile,skipPoints)
        print("Done getdata")
        streamFile.flush()
        print("flushed")
        streamFile.close()
    else:
        data=GetDataNet(save,xferbytes,timeoutseconds,sock,0,skipPoints)

    time.sleep(1)
    print(data)
    return data
    
#############################################################################

    
def GetDataNet(save,xferbytes,timeoutseconds,sock,streamFile,skipPoints):
    
    # round down to multiples of transfer size
    xferbytes = int(bdaq.xfersize * ((xferbytes // bdaq.xfersize) + 1))
    
    actualxferbytes = 0;
    if (save==False):
        mydata = bytearray(int(xferbytes))
        loopdelay = 0.05
    else:
        loopdelay =.0156

    for d in range( xferbytes // bdaq.xfersize ):
        print("d = " + str(d))
        for i in range(1,int(np.floor(timeoutseconds/loopdelay))):
            xem.UpdateWireOuts()
            
            ADCfifocount = xem.GetWireOutValue(epaddr["WireOutADCFIFOCOUNT"])*8*1024
            #print("ADC fifo count = " + str(ADCfifocount))
            bytesavailable = 2**np.floor(np.log2(ADCfifocount))

            if(ADCfifocount==0):
                raise Exception("ADC FIFO returned zero. Likely setup or configuration error")
                
            if(bytesavailable>=(bdaq.xfersize+10000)):
                #xferlen = min(minimumxfer,4194304,bytesavailable)
                #xferlen = np.uint32(2**np.floor(np.log2(xferlen)))
    
                readbytes = bytearray(bdaq.xfersize)
                bytecount = xem.ReadFromBlockPipeOut(epaddr["PipeOutADC"], 512, readbytes)
        
                if(bytecount<0):
                    print("ERROR: ReadFromBlockPipeOut returned " + str(bytecount))
                else:
                    print("Transferred " + str(bytecount/1024) + " kB.");
                    actualxferbytes += bytecount;
                if (save==False):
                    mydata[ (d*bdaq.xfersize) : ((d+1)*bdaq.xfersize) ] = readbytes
                else:
                    print("Writing")
                    try:
                        streamFile.write(readbytes)
                    except OSError as err:
                            print("OSError: {0}".format(err))  
                        
                break
          
            time.sleep(loopdelay)

        SendMessage(sock,readbytes,skipPoints)
    if (save==False):
        return (np.uint8(mydata)).view(np.uint16).reshape(len(mydata)/2/bdaq.cal["numchannels"],bdaq.cal["numchannels"]), actualxferbytes
    else:
        return actualxferbytes

    
#############################################################################    
def GetDataByTime(xfertime,timeoutseconds):

    xferbytes = 2*xfertime*bdaq.cal["samplerate"]*bdaq.cal["numchannels"]

    return GetData(xferbytes,timeoutseconds)
    
#############################################################################

    
def GetData(xferbytes,timeoutseconds):
    
    loopdelay = 0.05
    
    # round down to multiples of transfer size
    xferbytes = int(bdaq.xfersize * ((xferbytes // bdaq.xfersize) + 1))
    actualxferbytes = 0;
    
    
    mydata = bytearray(int(xferbytes))
    
    
    for d in range( xferbytes // bdaq.xfersize ):
        print("d = " + str(d))
        for i in range(1,int(np.floor(timeoutseconds/loopdelay))):
            xem.UpdateWireOuts()
            
            ADCfifocount = xem.GetWireOutValue(epaddr["WireOutADCFIFOCOUNT"])*8*1024
            print("ADC fifo count = " + str(ADCfifocount))
            bytesavailable = 2**np.floor(np.log2(ADCfifocount))

            if(ADCfifocount==0):
                raise Exception("ADC FIFO returned zero. Likely setup or configuration error")
                
            if(bytesavailable>=(bdaq.xfersize+10000)):
                #xferlen = min(minimumxfer,4194304,bytesavailable)
                #xferlen = np.uint32(2**np.floor(np.log2(xferlen)))
    
                readbytes = bytearray(bdaq.xfersize)
                bytecount = xem.ReadFromBlockPipeOut(epaddr["PipeOutADC"], 512, readbytes)
        
                if(bytecount<0):
                    print("ERROR: ReadFromBlockPipeOut returned " + str(bytecount))
                else:
                    print("Transferred " + str(bytecount/1024) + " kB.");
                    actualxferbytes += bytecount;
        
                mydata[ (d*bdaq.xfersize) : ((d+1)*bdaq.xfersize) ] = readbytes
                break
          
            time.sleep(loopdelay)
       
    return (np.uint8(mydata)).view(np.uint16).reshape(len(mydata)/2/bdaq.cal["numchannels"],bdaq.cal["numchannels"]), actualxferbytes



#############################################################################

def ScaletoIData(rawdata,mycal):

    scaledI = np.empty(rawdata.shape,dtype=np.float64)
    
    for c in range(mycal["numchannels"]):
        scaledI[:,c] = ( np.float64(rawdata[:,c]) - mycal["in_os"][c] ) / mycal["in_gain"][c]
        
    return scaledI

#############################################################################
 
def ChangeDACValues(biaspoint):
    
    myprogram = program_setbias(biaspoint)
    
    # load program into MM and execute it
    LoadMM(myprogram)                
    StartMM()


#############################################################################
 
def program_setbias(biaspoint):
    
    # initialize bias voltages
    myprogram = [     REGISTERSET(1,GET_AD5668_CODE(biaspoint[DACs["unused1"]])),
                      REGISTERS_TO_FOURCLAMP(1,DACs["unused1"],AD5668_WRITE_SINGLE),
                      REGISTERSET(1,GET_AD5668_CODE(biaspoint[DACs["unused2"]])),
                      REGISTERS_TO_FOURCLAMP(1,DACs["unused2"],AD5668_WRITE_SINGLE),
                      REGISTERSET(1,GET_AD5668_CODE(biaspoint[DACs["VBIAS1"]])),
                      REGISTERS_TO_FOURCLAMP(1,DACs["VBIAS1"],AD5668_WRITE_SINGLE),
                      REGISTERSET(1,GET_AD5668_CODE(biaspoint[DACs["VBIAS2"]])),
                      REGISTERS_TO_FOURCLAMP(1,DACs["VBIAS2"],AD5668_WRITE_SINGLE),
                      REGISTERSET(1,GET_AD5668_CODE(biaspoint[DACs["VCLAMP0"]])),
                      REGISTERS_TO_FOURCLAMP(1,DACs["VCLAMP0"],AD5668_WRITE_SINGLE),
                      REGISTERSET(1,GET_AD5668_CODE(biaspoint[DACs["VCLAMP1"]])),
                      REGISTERS_TO_FOURCLAMP(1,DACs["VCLAMP1"],AD5668_WRITE_SINGLE),
                      REGISTERSET(1,GET_AD5668_CODE(biaspoint[DACs["VCLAMP2"]])),
                      REGISTERS_TO_FOURCLAMP(1,DACs["VCLAMP2"],AD5668_WRITE_SINGLE),
                      REGISTERSET(1,GET_AD5668_CODE(biaspoint[DACs["VCLAMP3"]])),
                      REGISTERS_TO_FOURCLAMP(1,DACs["VCLAMP3"],AD5668_WRITE_SINGLE_UPDATE_ALL)
                      ]

    # end
    myprogram = myprogram + [IDLE]
    
    if(len(myprogram)>1024):
        print("ERROR: program is too long")
        return -1
    
    return myprogram




    
#############################################################################
 
def program_waveform(biaspoint,sweepchannel,sweepvalues,steptime):
    
    # initialize bias voltages
    myprogram = [     REGISTERSET(1,GET_AD5668_CODE(np.single(biaspoint[DACs["VBIAS1"]]))),
                      REGISTERS_TO_FOURCLAMP(1,np.single(DACs["VBIAS1"]),AD5668_WRITE_SINGLE),
                      REGISTERSET(1,GET_AD5668_CODE(np.single(biaspoint[DACs["VBIAS2"]]))),
                      REGISTERS_TO_FOURCLAMP(1,np.single(DACs["VBIAS2"]),AD5668_WRITE_SINGLE),
                      REGISTERSET(1,GET_AD5668_CODE(np.single(biaspoint[DACs["VCLAMP0"]]))),
                      REGISTERS_TO_FOURCLAMP(1,np.single(DACs["VCLAMP0"]),AD5668_WRITE_SINGLE),
                      REGISTERSET(1,GET_AD5668_CODE(np.single(biaspoint[DACs["VCLAMP1"]]))),
                      REGISTERS_TO_FOURCLAMP(1,np.single(DACs["VCLAMP1"]),AD5668_WRITE_SINGLE),
                      REGISTERSET(1,GET_AD5668_CODE(np.single(biaspoint[DACs["VCLAMP2"]]))),
                      REGISTERS_TO_FOURCLAMP(1,np.single(DACs["VCLAMP2"]),AD5668_WRITE_SINGLE),
                      REGISTERSET(1,GET_AD5668_CODE(np.single(biaspoint[DACs["VCLAMP3"]]))),
                      REGISTERS_TO_FOURCLAMP(1,np.single(DACs["VCLAMP3"]),AD5668_WRITE_SINGLE_UPDATE_ALL)
                      ]
    
    # apply waveform
    for i in np.arange(len(sweepvalues)):
        myprogram = myprogram + [
                                REGISTERSET(1,GET_AD5668_CODE(np.single(sweepvalues[i]))),
                                REGISTERS_TO_FOURCLAMP(1,sweepchannel,AD5668_WRITE_UPDATE_SINGLE),
                                DELAYSEC(np.float64(steptime))
                                ]
        

    # end
    myprogram = myprogram + [IDLE]
    
    if(len(myprogram)>1024):
        print("ERROR: program is too long")
        return -1
    
    return myprogram


    
#############################################################################

def NullCurrentOffsets():

    # reset offsets to zero
    bdaq.cal_in_os = np.zeros(bdaq.cal["numchannels"],dtype=np.float64)

    SoftReset()
    time.sleep(0.1)
    
    nullprogram = [IDLE,IDLE]
    LoadMM(nullprogram)                # load program into MM
    
    StartMMandADCFIFO()               # start waveform and acquisition

    time.sleep(2)                         # pause for data buffer to fill
    rawdata,bytecount = GetDataByTime(2,1)         # retrieve data
    
    if(bytecount):
        # set each offset to the mean value, skipping the first 0.5 seconds
        for c in range(bdaq.cal["numchannels"]):
            t=rawdata[int(c+0.5*bdaq.cal["samplerate"])::,c]
            print(np.mean(t))
            bdaq.cal["in_os"][c] = np.mean(t)   #skip 0.5sec and average
        

def NullCurrentOffsetsBA():
    constantprogram = bdaq.program_setbias(bdaq.cal["biasvoltages"])
    # reset offsets to zero
    bdaq.LoadMM(constantprogram)            # load program into MM
    bdaq.SoftReset()
    time.sleep(0.1)
    bdaq.StartMMandADCFIFO()               # start waveform and acquisition
    time.sleep(2)                         # pause for data buffer to fill
    rawdata,xferbytes = bdaq.GetDataByTime(2,1)         # retrieve data
    if(xferbytes):
        # set each offset to the mean value, skipping the first 0.5 seconds
        for c in range(bdaq.cal["numchannels"]):
            t=rawdata[int(c+0.5*bdaq.cal["samplerate"])::,c]
            print(np.mean(t))
            bdaq.cal["in_os"][c] = np.mean(t)   #skip 0.5sec and average
            

def movingaverage(interval, window_size):
    window = np.ones(int(window_size))/float(window_size)
    return np.convolve(interval, window, 'same')
    
#############################################################################
def FindZeroCurrentVoltageBA(DACchannel,ADCchannel,polarity):
    
    # step
    steptime = 0.5
    stepsize = 0.4
    stepvoltages = [bdaq.cal["biasvoltages"][DACchannel],bdaq.cal["biasvoltages"][DACchannel]+stepsize,bdaq.cal["biasvoltages"][DACchannel]-stepsize,bdaq.cal["biasvoltages"][DACchannel]]
    stepprogram = bdaq.program_waveform(bdaq.cal["biasvoltages"],DACchannel,stepvoltages,steptime)
    print(stepvoltages)
    bdaq.LoadMM(stepprogram)            # load program into MM    
    
    # =====================================

    # soft reset to initialize FIFO
    
    bdaq.SoftReset()
    time.sleep(0.1)
    
    # =====================================
    
    # start waveform and acquire data
    
    bdaq.StartMMandADCFIFO()               # start waveform and acquisition
    
    time.sleep(2)                         # pause for data buffer to fill
    rawdata,xferbytes = bdaq.GetDataByTime(2,1)         # retrieve data
    
    Idata = bdaq.ScaletoIData(rawdata,bdaq.cal)
    
    bData = Idata[:,ADCchannel]    
    #downsample=20
    #bData=bData[int(0.5*bdaq.cal["samplerate"]):int(1.9*bdaq.cal["samplerate"])]
    #L=np.floor( len(bData)/downsample )*downsample
    #trace=bData[0:L].reshape((-1,downsample)).mean(axis=1)
    trace=movingaverage(bData[int(0.5*bdaq.cal["samplerate"]):int(1.9*bdaq.cal["samplerate"])],400)    
    plt.plot(trace)    
    plt.show()
    
    Ival1 = np.mean(Idata[int(0.7*bdaq.cal["samplerate"]):int(0.9*bdaq.cal["samplerate"]),ADCchannel])
    Ival2 = np.mean(Idata[int(1.2*bdaq.cal["samplerate"]):int(1.4*bdaq.cal["samplerate"]),ADCchannel])
    Ival3 = np.mean(Idata[int(1.7*bdaq.cal["samplerate"]):int(1.9*bdaq.cal["samplerate"]),ADCchannel])
    
    slope = (2*stepsize)/(Ival1-Ival2)
    
    vcorrection = Ival3 * slope * polarity*-1
    print("vcorrection",vcorrection)
    print("Ivals",Ival1,Ival2,Ival3)    
    print("slope",slope)
    if ( np.abs(vcorrection)<.4):
        return ( vcorrection)
    else:
        print("FindZeroCurrentVoltage failed to converge")
        return 0   
    
def FindZeroCurrentVoltage(DACchannel,ADCchannel,polarity):
    
    # step
    steptime = 0.5
    stepsize = 0.1
    stepvoltages = [bdaq.cal["biasvoltages"][DACchannel],bdaq.cal["biasvoltages"][DACchannel]+stepsize,bdaq.cal["biasvoltages"][DACchannel]-stepsize,bdaq.cal["biasvoltages"][DACchannel]]
    stepprogram = bdaq.program_waveform(bdaq.cal["biasvoltages"],DACchannel,stepvoltages,steptime)
    
    bdaq.LoadMM(stepprogram)            # load program into MM    
    
    # =====================================

    # soft reset to initialize FIFO
    
    bdaq.SoftReset()
    time.sleep(0.1)
    
    # =====================================
    
    # start waveform and acquire data
    
    bdaq.StartMMandADCFIFO()               # start waveform and acquisition
    
    time.sleep(2)                         # pause for data buffer to fill
    rawdata,xferbytes = bdaq.GetDataByTime(2,1)         # retrieve data
    
    Idata = bdaq.ScaletoIData(rawdata,bdaq.cal)
    
    
    Ival1 = np.mean(Idata[int(0.7*bdaq.cal["samplerate"]):int(0.9*bdaq.cal["samplerate"]),ADCchannel])
    Ival2 = np.mean(Idata[int(1.2*bdaq.cal["samplerate"]):int(1.4*bdaq.cal["samplerate"]),ADCchannel])
    Ival3 = np.mean(Idata[int(1.7*bdaq.cal["samplerate"]):int(1.9*bdaq.cal["samplerate"]),ADCchannel])
    
    slope = (2*stepsize)/(Ival1-Ival2)
    
    vcorrection = Ival3 * slope * polarity
    print("vcorrection",vcorrection)
    print("Ivals",Ival1,Ival2,Ival3)    
    print("slope",slope)
    if ( np.abs(vcorrection)<.1):
        
        return ( vcorrection)
    else:
        print("FindZeroCurrentVoltage failed to converge")
        return 0   
    
    
#############################################################################

def BootFourClamp(biasvoltages):
    print("BDAQ fourclamp boot sequence")
    
    bdaq.ConfigureFPGA()
    time.sleep(0.1)
    
    bdaq.Initialize()
    time.sleep(0.1)
    
    bdaq.Configure_FourClampAmplifier()
    time.sleep(0.1)
    
    bdaq.ChangeDACValues(biasvoltages)
    time.sleep(0.1)
    
    bdaq.SoftReset()
    time.sleep(0.1)
    

#############################################################################

def SaveConfig():
    
    settingsfilename = os.path.join(os.getcwd(),'setup.bdaq')

    with open(settingsfilename, 'w') as f:
        json.dump(bdaq.cal,f)
    
    
#############################################################################

def LoadConfig():
    
    settingsfilename = os.path.join(os.getcwd(),'setup.bdaq')

    try:
        with open(settingsfilename, 'r') as f:
            new_cal = json.load(f)
            bdaq.cal = new_cal
    except:
        print("No config file found.")
                
    
    
#############################################################################

def SaveLogFile(rawdata,logname,dirname):
    
    #####################################################################
    # save data to file
    # 
    # bdaq.cal[] is also saved as a JSON object as a text file header
    # 
    # rawdata should be the binary uint16 array returned by GetData
    #####################################################################
    
    #bdaq.cal["currentdatetime"] = list(datetime.now())
    
    #logname = logname + '_' + datetime.now().strftime("%Y%m%d_%H%M%S") + '.bdaq'
    writelogfilename = os.path.join(dirname,logname)
    os.makedirs(os.path.dirname(writelogfilename), exist_ok=True)
    
    with open(writelogfilename, 'w') as f:
        json.dump(bdaq.cal,f)
        f.write("<data_begins>")
    
    with open(writelogfilename, 'ab') as f:    
        f.write(rawdata)

    return writelogfilename
    
    
def SaveTempLogFile(logname,dirname):
    
    writelogfilename = os.path.join(dirname,logname)
    os.makedirs(os.path.dirname(writelogfilename), exist_ok=True)
    
    with open(writelogfilename, 'w') as f:
        json.dump(bdaq.cal,f)
        f.write("<data_begins>")
    
    with open(writelogfilename, 'ab') as f:    
        with open("c:\\temp\\BDAQStream.bBin", "rb") as f2:
            while True:
                chunk=f2.read(8192)
                if chunk:
                    f.write(chunk)
                else:
                    break

    return writelogfilename    

def SaveTempBinFile(logname,dirname):
    
    writelogfilename = os.path.join(dirname,logname)
    os.makedirs(os.path.dirname(writelogfilename), exist_ok=True)
    
    with open('C:\\temp\\temp.bDAQ', 'w') as f:
        json.dump(bdaq.cal,f)
        f.write("<data_begins>")
    print('read')
    with open('C:\\temp\\temp.bDAQ', 'ab') as f:    
        with open("c:\\temp\\BDAQStream.bBin", "rb") as f2:
            while True:
                chunk=f2.read(8192)
                if chunk:
                    f.write(chunk)
                else:
                    break

    iData,log_cal = bdaq.ReadLogFile('C:\\temp\\temp.bDAQ')
    
    f=log_cal
    print('header')
    j= log_cal#dict((name, getattr(f, name)) for name in dir(f) if not name.startswith('__'))
    with open(writelogfilename  + "_header.txt", "w") as text_file:
        for x,y in j.items(): 
            text_file.write( str(x) + "=" + str(y) + "\n" )    
    #print('channels')
    #downsample=500
    #for c in range(log_cal["numchannels"]):
    #    out_file = open(writelogfilename + "_Channel_" + str(c) + ".bin","wb")
    #    trace =iData[:,c];
    #    L=np.floor( len(trace)/downsample )*downsample
    #    trace=trace[0:L].reshape((-1,downsample)).mean(axis=1)
    #    trace = 1e12* (trace[int(0.5*bdaq.cal["samplerate"]):]  - log_cal["in_os"][c] ) / log_cal["in_gain"][c]
    #    l= [1, len(trace)]
    #    s=struct.pack('i'*2,*l)
    #    out_file.write(s)
    #    trace = np.asarray(trace)
    #    s=struct.pack('d'*len(trace), *trace)
    #    out_file.write(s)
    #    out_file.close()

    #traceAll=np.zeros(len(trace))        
    #for c in range(log_cal["numchannels"]):
    #    trace =iData[:,c];
    #    L=np.floor( len(trace)/downsample )*downsample
    #    traceAll=traceAll+ trace[0:L].reshape((-1,downsample)).mean(axis=1)[int(0.5*bdaq.cal["samplerate"]):]

    #trace = 1e12* (traceAll  - log_cal["in_os"][c] ) / log_cal["in_gain"][c]
    #out_file = open(writelogfilename + "_raw.bin","wb")
    #l= [1, len(trace)]
    #s=struct.pack('i'*2,*l)
    #out_file.write(s)
    #trace = np.asarray(trace)
    #s=struct.pack('d'*len(trace), *trace)
    #out_file.write(s)
    #out_file.close()
        
    return writelogfilename       
#############################################################################

def ReadLogFile(readlogfilename):
    
    #####################################################################
    # read data from a log file created by SaveLogFile
    #
    # returns the raw data and the calibration library.
    #
    # the signals can be converted to current using ScaletoIData
    #
    #####################################################################
    
    
    # read header
    with open(readlogfilename,'rb') as f:
        
        rawheaderdata = f.read(10000)   #maximum header size
    
        # locate calibration header (json object)
        headerMatchObj = re.match(b'{.*?}<data_begins>',rawheaderdata)
        
        # decode calibration header
        log_cal = json.loads(headerMatchObj.group(0)[:-13].decode('utf-8'))
        #print("logfile calibration header:",log_cal)
        
        headerlength = headerMatchObj.end()
        
        # read trace data from log file
        f.seek(headerlength)
        log_rawdata = np.fromfile(f,dtype=np.uint16)
        log_rawdata = log_rawdata.reshape(len(log_rawdata)/log_cal["numchannels"],log_cal["numchannels"])
        
    
    
    return log_rawdata, log_cal
    
#############################################################################
    