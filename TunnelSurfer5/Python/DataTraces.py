# -*- coding: utf-8 -*-
"""
Created on Sat Apr 16 16:38:19 2016

@author: bashc
"""
import sys
sys.path.append("C:\\Development\\TunnelSurfer5mySQL\\TunnelSurfer5\\bin\\Debug\\Python")

import ntpath
import PeakFinders
import Denoising
import sys
import numpy as np
from math import floor
import scipy
from scipy import signal
from scipy.interpolate import interp1d
import quantities as pq #https://pypi.python.org/pypi/quantities
from neo import io # download from https://pythonhosted.org/neo/io.html
import os
from datetime import datetime, date, timedelta
import struct
import os.path
import time as timeD 
import json
import re
import csv
from scipy.signal import butter, lfilter, freqz
from matplotlib import pyplot as plt

 
dirSeperator = os.path.sep

def path_leaf(path):
    head, tail = ntpath.split(path)
    return tail or ntpath.basename(head)
    
def is_number(s):
    try:
        float(s)
        return True
    except ValueError:
        return False    

def butter_lowpass(cutoff, fs, order=2):
    nyq = 0.5 * fs
    normal_cutoff = cutoff / nyq
    b, a = butter(order, normal_cutoff, btype='low', analog=False)
    return b, a

def butter_lowpass_filter(data, cutoff, fs, order=2):
    b, a = butter_lowpass(cutoff, fs, order=order)
    y = lfilter(b, a, data)
    return y
    
class TraceStatistics():
    def __init__(self, fullPath):
        self.filePath = fullPath
        self.fileName = path_leaf(fullPath)
        self.isControl = False
        self.topRef = 0
        self.bttmRef = 0
        self.sampleRate = 0
        self.concentrationMM = 0
        self.analyte = ''
        self.tunnelMV = 0
        self.traceTime = 0
        self.hasIonic = False
        self.ionicMV = 0
        self.otherInfo = ''

       
class Trace:
    def calcStatistics(self):
        return self.stats
        
    def Currents(self):
        if not self._isLoaded:
            self._loadFile()
        return self._current

    def Voltages(self):
        if not self._isLoaded:
            self._loadFile()
        return self._drivingVoltage           
        
    def _makeNumber(self,text):
        try:
            return float(text.lower().replace('g','0').replace('p','').replace('n','-').replace('mv',''))
        except (ValueError):
            return 0
        
    def _loadFile(self):
        self._abstractLoadFile()
        try:
            self.stats.sampleRate = self.sampleRate        
        except (Exception):
            self.stats.sampleRate = 20000
            self.sampleRate = 20000
        
    def _loadHeader(self):
        self.sortTime = datetime.datetime.fromtimestamp(os.path.getmtime(self.filePath))
    
    def OpenBinary(self, filename):
        with open(filename, "rb") as f:
            struct_fmt = '=2i' # int[5], float, byte[255]
            struct_len = struct.calcsize(struct_fmt)
            struct_unpack = struct.Struct(struct_fmt).unpack_from
            data = f.read(struct_len)
            s = struct_unpack(data)
            nTraces = s[0]
            lenTrace = s[1]
            struct_fmt = '=' + str(lenTrace) + 'd' # int[5], float, byte[255]
            struct_len = struct.calcsize(struct_fmt)
            struct_unpack = struct.Struct(struct_fmt).unpack_from
            results = []
            for I in range(0,nTraces):
                data = f.read(struct_len)
                if not data: break
                s = struct_unpack(data)
                results.append(s)
            return results

    def SaveShortBinaryD(self, fileName,samplerate,voltage, trace):
        trace = np.asarray(trace)
        M = np.max(trace)
        mm = np.min(trace)
        
        offset = (M + mm) / -2
        scale = (M - offset) / 32768
        
        trace2 = ((trace + offset) / scale).astype(np.int16)

        out_file = open(fileName,"wb")
        l = [1, len(trace2)]
        s = struct.pack('i' * 2,*l)
        out_file.write(s)
        l = [scale, offset, samplerate,voltage]
        s = struct.pack('d' * 4,*l)
        out_file.write(s)
        
        step = 3000
        for i in range(0,len(trace2),step):
            tt = trace2[i:(i + step)]
            s = struct.pack('h' * len(tt), *tt)
            out_file.write(s)
            
        out_file.close() 
         
    def SaveShortBinary(self, fileName,scale,offset,samplerate,voltage, trace):
        scale2 = 1 / scale
        offset2 = (32768 - offset) / scale
        out_file = open(fileName,"wb")
        l = [1, len(trace)]
        s = struct.pack('i' * 2,*l)
        out_file.write(s)
        l = [scale2, offset2, samplerate,voltage]
        s = struct.pack('d' * 4,*l)
        out_file.write(s)
        
        step = 3000
        for i in range(0,len(trace),step):
            tt = (np.float64(trace[i:(i + step)]) - offset) / scale
            tt = ((tt + offset2) / scale2) .astype(np.int16)
            s = struct.pack('h' * len(tt), *tt)
            out_file.write(s)
            
        out_file.close() 

    def OpenShortBinary(self, fileName):
        with open(fileName, "rb") as f:
            struct_fmt = '=2i' # int[5], float, byte[255]
            struct_len = struct.calcsize(struct_fmt)
            struct_unpack = struct.Struct(struct_fmt).unpack_from
            data = f.read(struct_len)
            s = struct_unpack(data)
            nTraces = s[0]
            lenTrace = s[1]

            struct_fmt = '=4d' 
            struct_len = struct.calcsize(struct_fmt)
            struct_unpack = struct.Struct(struct_fmt).unpack_from
            data = f.read(struct_len)
            s = struct_unpack(data)
            
            scale2 = s[0]
            offset2 = s[1]
            samplerate= s[2]
            voltage= s[3]

            struct_fmt = '=' + str(lenTrace) + 'h' # int[5], float, byte[255]
            struct_len = struct.calcsize(struct_fmt)
            struct_unpack = struct.Struct(struct_fmt).unpack_from
            results = []
            for I in range(0,nTraces):
                data = f.read(struct_len)
                if not data: break
                data = struct_unpack(data)
                data=np.asarray(data)*scale2 + offset2
                results.append(data)
        return [scale2,offset2,samplerate,voltage, results]
    #def OpenBBF(self,filename):
    #    with open(filename, 'rb') as f:
    #        nChannels = struct.unpack('i', f.read(4))[0]
    #        nPoints =struct.unpack('i', f.read(4))[0]
        
    #        self.stats.sampleRate = struct.unpack('d',f.read(8))[0]
            
    #        fileday = struct.unpack('I', f.read(4))[0]
    #        filetime_ms =struct.unpack('I', f.read(4))[0]
        
    #        reserved = f.read(512)
    #        reserved = reserved.decode('ascii').strip()
            
    #        dataFormat =struct.unpack('i', f.read(4))[0]
            
    #        channelname=[]
    #        channelunit=[]
    #        channelunitscale=[]
    #        channelscale =[]
    #        channeloffset =[]
    #        channelvoltage =[]
    #        for i in range(nChannels):
    #            c = f.read(50)
    #            c = c.decode('ascii').strip()
    #            channelname.append(c)
                
    #            c= f.read(5)
    #            channelunit.append ( c.decode('ascii').strip())
                
    #            channelunitscale.append( struct.unpack('d',f.read(8))[0])
    #            channelscale.append( struct.unpack('d',f.read(8))[0])
    #            channeloffset.append( struct.unpack('d',f.read(8))[0])
    #            channelvoltage.append( struct.unpack('d',f.read(8))[0])
                
    #        if dataFormat == 1:
    #            data = np.fromfile(f,dtype=np.int16,count=nPoints)
    #            data=data.reshape(len(data)/nChannels,nChannels)
    #            dataF= np.zeros(np.shape(data))
    #            for c in range(nChannels):
    #                dataF[:,c]=data[:,c]*channelscale[c]-channeloffset[c]
                
    #        if dataFormat == 0:
    #            data = np.fromfile(f,dtype=np.uint16,count=nPoints)
    #            data = data.reshape(len(data)/nChannels,nChannels)
    #            dataF= np.zeros(np.shape(data))
    #            for c in range(nChannels):
    #                dataF[:,c]=(data[:,c]-channeloffset[c])/channelscale[c]
                    
    #    self._current = dataF

    #def SaveBBF(self,fileName, tracename,unit,unitScale,samplerate, *Traces):
    #    with open(fileName, 'wb') as f:
            
    #        trace = Traces[0]
    #        l= [len(Traces), len(trace)]
    #        s=struct.pack('i'*2,*l)
    #        f.write(s)

    #        f.write( struct.pack('d', samplerate))
            
    #        f.write( struct.pack('I', self.fileDate))
    #        f.write(struct.pack('I', self.startTime))
            
    #        s= 'Reserved'.ljust(512).encode('ascii')
    #        f.write( bytearray( s ) )
    #        f.write( struct.pack('i', 1))
            
    #        for x in range(len(Traces)):
    #            trace = Traces[x]
    #            s= tracename.ljust(50).encode('ascii')
    #            f.write( bytearray( s ) )
                
    #            s= unit.ljust(5).encode('ascii')
    #            f.write( bytearray( s ) )
                
    #            m=np.mean( trace )
    #            t=trace-m;
    #            s=np.max( (np.min(t)*-1, np.max(t))) /32767
    #            f.write( struct.pack('d',unitScale))
    #            f.write( struct.pack('d',s))
    #            f.write( struct.pack('d',m))
    #            f.write( struct.pack('d', np.mean( self._drivingVoltage)))
                
               
    #        data = np.zeros(l,np.int16)
    #        for x in range(len(Traces)):
    #            trace = Traces[x]
    #            m=np.mean( trace )
    #            t=trace-m;
    #            s=np.max( (np.min(t)*-1, np.max(t))) /32767
    #            data[x,:]=np.int16( t/s )
                
    #        data=np.squeeze( data.reshape(1,np.size(data)))
    #        trace = np.asarray(data)
    #        s=struct.pack('i'*len(trace), *trace)
    #        f.write(s)


    def SaveBinary(self, fileName, *Traces):
        
        out_file = open(fileName,"wb")
        trace = Traces[0]
        l = [len(Traces), len(trace)]
        s = struct.pack('i' * 2,*l)
        out_file.write(s) 
        
        for count, thing in enumerate(Traces):
            trace = np.asarray(thing)
            for i in range(0,len(trace),350):
                tt = trace[i:(i + 350)]
                s = struct.pack('d' * len(tt), *tt)
                out_file.write(s)
            
        out_file.close()

    def SaveStats(self,filename):
        f = self.stats
        
        j = dict((name, getattr(f, name)) for name in dir(f) if not name.startswith('__'))
        directory = os.path.dirname(filename)
        if not os.path.exists(directory):
            os.makedirs(directory)
        with open(filename, "w") as text_file:
            for x,y in j.items(): 
                text_file.write(str(x) + "=" + str(y) + "\n")
                
    def __init__(self,fullPath, outPath, controlFile,concentrationMM,numPores):
        self._isLoaded = False
        self.outPath = outPath
        directory = os.path.dirname(outPath)
        if not os.path.exists(directory):
            os.makedirs(directory)
        self.controlFile = controlFile
        self.filePath = fullPath
        self.fileName = path_leaf(fullPath)
        self.stats = TraceStatistics(fullPath)
        self.fileType = ntpath.splitext(self.filePath)[-1]
        self.isControl = False
        self.stats.concentrationMM = concentrationMM
        self.stats.number_of_pores = numPores
        self.stats.sampleRate = 0
        self.sortTime = 0
        self._loadHeader()
          
class DataTrace(Trace):
    def calcStatistics(self):
        t = self.Currents()   
        #v = self.Voltages()
        self.stats.nPoints = len(t)

        self.SaveStats(self.outPath + "_header.txt")
        self.RegularHist(t,"E2_Raw")    
        sTrace = Denoising.smooth(t,300)
        self.SaveShortBinaryD(self.outPath + "_Smooth.bsn",self.sampleRate,np.mean(self._drivingVoltage), sTrace)
        sTrace = None
        
        self.stats.tun_grossBaseline = t.mean()
        self.stats.tun_range = t.max() - t.min()
        self.stats.tun_std = t.std()
        t = t - t.mean()
        try:
            self.stats.traceTime = t.times.max() - t.times.min()
        except:
            self.stats.traceTime = len(t) / self.sampleRate
        self.SaveStats(self.outPath + "_header.txt")
        #return Trace.calcStatistics(self)
        sys.stdout.write('.wFilter.\n')
       
        self._filterTraces(t)
        t = self._findPeaks(t,100)
        sys.stdout.write('.baselineStats.\n')
        self.SaveStats(self.outPath + "_header.txt")
        w = 1 / (np.abs(1 + np.abs(np.abs(self.wTrace) + np.asarray(self.stats.tun_grossBaseline)))) ** .5
        L = np.min((len(w),len(t)))
        self.stats.tun_flatBaseline = self.stats.tun_grossBaseline + np.sum(t[0:L] * w[0:L]) / sum(w[0:L])
        self.stats.tun_noise = 2 * np.mean(abs(t))
        self.stats.tun_wNoise = np.std(self.wTrace) * pq.pA
#        plt.show()
        self.SaveStats(self.outPath + "_header.txt")

        return Trace.calcStatistics(self)
        
    
    def estimatePower(self, specSize):
        if not hasattr(self,'powerSpec'):
            t = self.Currents()
            sys.stdout.write('.')
            f1,self.powerSpec = signal.welch(t, self.sampleRate, 'flattop', specSize, scaling='spectrum')
            sys.stdout.write('.')
            f2,self.lPowerSpec = signal.welch(t[0:-1:100], self.sampleRate, 'flattop', specSize, scaling='spectrum')
            
            self.SaveBinary(self.outPath + "_PowerspecShort_N.bin", f1, self.powerSpec)
            self.SaveBinary(self.outPath + "_PowerspecLong_N.bin", f1, self.powerSpec)
        return self.powerSpec
            
    def _filterTraces(self,t):    
        
        sys.stdout.write('.')
        self.estimatePower(1024)
        if self.controlFile != "":
            controlTrace = np.asarray(self.OpenBinary(self.controlFile + "_PowerspecShort_N.bin")[1])
        else: 
            controlTrace = self.powerSpec
        sys.stdout.write('.')
        self.wTrace,wCoef, self.wImportance = Denoising.WienerScalart(t,self.sampleRate,controlTrace,ControlWeight=1,NoiseMargin=45)
        #self.SaveBinary(self.outPath + "_Wiener.bin", self.wTrace)
        self.SaveShortBinaryD(self.outPath + "_Wiener.bsn",self.sampleRate,np.mean(self._drivingVoltage), self.wTrace)
    
    def _findBackground(self,wTrace2,shortData):
        sys.stdout.write('.smooth1.\n')
        sTrace = Denoising.smooth(wTrace2,200)
        sTrace = sTrace - np.mean(sTrace)
        peakFind = np.abs(sTrace - np.mean(sTrace[[i for i,x in enumerate(sTrace) if x < 2]]))
        peakFind[[i for i,x in enumerate(peakFind) if x < 25]] = 0
        active_IDX = [i for i,x in enumerate(peakFind) if x != 0]
        peakFind[active_IDX] = 1
        sys.stdout.write('.guass.\n')
        peakFind = scipy.ndimage.filters.gaussian_filter1d(peakFind,100)
        peakFind[[i for i,x in enumerate(peakFind) if x != 0]] = 1
        
        idx = [i for i,x in enumerate(peakFind) if x == 0]
        self.peakMask = [i for i,x in enumerate(peakFind) if x != 0]
        sys.stdout.write('.enumerate.\n')
        if len(idx) < 1000:
        #        %basically assumes that the mean is the baseline
            if (not idx) or (len(idx) < 400):
                back = np.zeros(np.shape(shortData)) * pq.pA + np.mean(shortData)
            else:
                if idx[0] != 1:
                    idx = np.append([0], idx)
                if idx[-1] != len(shortData):
                    idx = np.append(idx,[len(shortData) - 1])
                back = Denoising.smooth(shortData[idx] ,np.min([np.floor(len(idx) * .85), 1000]))
                back = interp1d(idx,back)(np.arange(0,len(shortData)))
        else:
#            %extract the minimums and use those for the baseline
            steps = int(np.min((400, floor(len(idx) / 2))))
            minX = np.zeros(steps + 1)
            mins = np.zeros(steps + 1)
            minX[0] = -.01
            mins[0] = np.mean(shortData[0:100])
            minX[steps] = len(shortData) + .01
            mins[steps] = np.mean(shortData[-100:-1])
            
            tt = shortData[idx]
            stp = int(len(tt) / steps)
            for I in range(0,int(steps) - 1):
                I2 = I * stp + 1
                x = idx[I2:(I2 + stp - 2)]
                if not (not x):
                    t = tt[I2:(I2 + stp - 2)]
                    idx2 = np.argmin(t)
                    mins[I + 1] = np.mean(t[max([1 ,(idx2 - 100)]):min([len(t), (idx2 + 100)])])
                    minX[I + 1] = x[idx2]
                    
            sys.stdout.write('.smooth2.\n')
            back = Denoising.smooth(mins,15)
            sys.stdout.write('.interp1.\n')
            back = interp1d(minX,back)(np.arange(0,len(shortData)))
        return (back,active_IDX)
     
    def traceTVLambda(self):
        return Denoising.tvdiplmax(self.Currents())
    
    def RegularHist(self, t, fileTag):
        try:
            tmin = int(np.floor(np.min(t) / 10) * 10)
            tmax = int(np.ceil(np.max(t) / 10) * 10)
            if tmax == tmin:
                tmax = tmin + 10
            cBins = range(tmin,tmax,10)
            aValues = np.histogram(t,cBins,normed=False)[0]
            with open(self.outPath + "_" + fileTag + ".csv", 'wb') as csvfile:
                spamwriter = csv.writer(csvfile, delimiter=',',
                            quotechar='|', quoting=csv.QUOTE_MINIMAL)
                for i in range(0,len(aValues)):
                    spamwriter.writerow([cBins[i] , aValues[i]])
    
            #self.SaveBinary(self.outPath + "_" + fileTag + ".bin", cBins,
            #aValues)
        except (ValueError,TypeError,RuntimeError):
            print('hist error')
            
    def _findPeaks(self,t, threshold):
        #t=t[ np.floor(len(t)*.25):np.floor(len(t)*.4)]
        # Filter requirements.

        
        sys.stdout.write('.Background.\n')
        background,active_IDX = self._findBackground(self.wTrace,t)
       
        t = t - np.asarray(background) * pq.pA
        #self.SaveBinary(self.outPath + "_Background_Removed.bin", t)
        self.SaveShortBinaryD(self.outPath + "_Background_Removed.bsn",self.sampleRate,np.mean(self._drivingVoltage), t)
        self.RegularHist(t,"H2_BackHist")
        sys.stdout.write('.justpeaks.\n')
        #try:
            #justPeaksdenoised = self.wTrace[self.peakMask]
            #self.SaveBinary(self.outPath + "_ShortFiltered.bin",
            #justPeaksdenoised)
            #self.SaveShortBinaryD(self.outPath +
            #"_ShortFiltered.bsn",self.sampleRate,np.mean( self._drivingVoltage
            #), justPeaksdenoised)
            #justPeaks = t[self.peakMask]
            #self.SaveBinary(self.outPath + "_ShortRaw.bin", justPeaks)
            #self.SaveBinary(self.outPath + "_PeakMask.bin", self.peakMask)
        #except (ValueError,TypeError,RuntimeError):
        #    print('short error')
       
                
        
        sys.stdout.write('.CUSUM.\n')
        try:
            cusumSignal,cusumLevels,cusumLevelTimes = Denoising.cusum(np.asarray(t),50,50)
            self.RegularHist(cusumSignal,"H2_CUMSUMHist")
            
            cLT = np.asarray(np.abs((cusumLevelTimes[1:] - cusumLevelTimes[0:-1])))
            self.RegularHist(cLT,"H2_CUMSUMHist")
            self.RegularHist(cusumLevels,"E2_cusumLevels")
            
            #self.SaveBinary(self.outPath + "\\CUMSUMTimes.bin",cLT)
            #self.SaveBinary(self.outPath + "\\cusumLevels.bin", cusumLevels)
            #self.SaveBinary(self.outPath + "_cusum.bin", cusumSignal)
            self.SaveShortBinaryD(self.outPath + "_cusum.bsn",self.sampleRate,np.mean(self._drivingVoltage), cusumSignal)
        except (ValueError,TypeError,RuntimeError):
            print('CUSUM error')            
        
        sys.stdout.write('.tvFilter.\n')
        
        #try:
        #    lamb=self.traceTVLambda()*1e-3
        #    TV =
        #    Denoising.tvdip(np.asarray(t),lamb,0,1e-3,40,z=cusumSignal)[0]
        #    self.SaveBinary(self.outPath +"_TV.bin",TV)
        #    self.RegularHist(TV,"H2_TVHist")
            			
        #    dTV= np.abs(np.diff(TV))
        #    sys.stdout.write('.peakfindTV.\n')
        #    maxPeaks, minPeaks =PeakFinders.peakdetect(dTV)
        #    idxM=[x[0] for x in maxPeaks]
        #    idxm=[x[0] for x in minPeaks]
        #    idx=np.zeros(len(idxM) + len(idxm),dtype= np.int32)
        #    idx[:(len(idxM))]=idxM
        #    idx[len(idxM):]=idxm
        #    idx.sort()
        #    mids=TV[ np.round( (idx[1:]+ idx[0:-1])/2) ]
        #    mids=np.asarray([x for x in mids if x>20])
            			
        #    self.SaveBinary(self.outPath +"_TVLevels.bin",mids)
        #    self.RegularHist(mids,"H2_TVLevels")
            
        #    idx=idx/self.stats.sampleRate
        #    idx=idx[1:]-idx[0:-1]
        #    self.SaveBinary(self.outPath +"_TVTimes.bin",idx)
        #    self.RegularHist(idx,"H2_TVTimes")
        #except (ValueError,TypeError,RuntimeError):
        #    print('TV error')
        
        #try:
            #sys.stdout.write('.peakfind.\n')
            #maxPeaks, minPeaks = PeakFinders.peakdetect(t)
            #self.maxPeaks = [x[1] for x in maxPeaks if np.abs(x[1]) >
            #threshold]
            
            #self.SaveBinary(self.outPath + "_SpikeLevels.bin",self.maxPeaks)
            #self.SaveShortBinaryD(self.outPath +
            #"_SpikeLevels.bsn",self.sampleRate,np.mean( self._drivingVoltage
            #), self.maxPeaks)
            #self.RegularHist(self.maxPeaks,"E2_Spikes")
        #except (ValueError,TypeError,RuntimeError):
        #    print('Peakfind error')
        return t
        
       
    def _abstractLoadFile(self):
        if self.fileType.lower() == '.abf':
             if not self.fileRef:
                self.fileRef = io.AxonIO(filename=self.filePath)
             self._isLoaded = True
             self.fileData = self.fileRef.read_block(lazy=False, cascade=True)
             self._drivingVoltage = self.fileData.segments[0].analogsignals[1]
             if np.mean(self._drivingVoltage) > 1:
                 self._drivingVoltage = self._drivingVoltage / 10
                
             self.sampleRate = float(self.fileData.segments[0].analogsignals[1].sampling_rate)   
             self._current = self.fileData.segments[0].analogsignals[0]
             self.SaveShortBinaryD(self.outPath  + "_Raw.bsn",self.sampleRate,np.mean(self._drivingVoltage),  self._current)
             if (self.sampleRate>20000):  
                 lowPass = 20000
                 self._current = self.downSample(lowPass,0,self._current)
                 self.SaveShortBinaryD(self.outPath  + "_Low.bsn",lowPass,np.mean(self._drivingVoltage),  self._current)
                
             
#             self._current=self._current[0:int(len(self._current)/10)]
             
             if len(self.fileData.segments[0].analogsignals) > 2:
                 self.hasIonic = True
                 self._ionicCurrents = np.asarray(self.fileData.segments[0].analogsignals[2])
             else:
                 self.hasIonic = False
             self.stats.hasIonic = self.hasIonic
             self.stats.meanVoltage = np.mean(self._drivingVoltage)
             self.stats.sampleRate = self.sampleRate
             self.stats.sortTime = self.sortTime
             self.stats.fileDate = self.fileDate
        if '.bdaq' in self.fileType.lower():
             self._isLoaded = True
             self.sampleRate = float(self.header["samplerate"])
             self.stats.hasIonic = False
             self.stats.meanVoltage = self._drivingVoltage 
             self.stats.sampleRate = self.sampleRate
             self.stats.sortTime = self.sortTime
             self.stats.fileDate = self.fileDate
    
    def ScaletoIData(self,rawdata,mycal):
        scaledI = np.empty(rawdata.shape,dtype=np.float64)
        for c in range(mycal["numchannels"]):
            scaledI[:,c] = (np.float64(rawdata[:,c]) - mycal["in_os"][c]) / mycal["in_gain"][c]
        return scaledI

    def ReadLogFile(self,readlogfilename):
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
            log_rawdata = log_rawdata.reshape(len(log_rawdata) / log_cal["numchannels"],log_cal["numchannels"])
            t = log_rawdata[:24,0]
            if (t[0] == t[1]):
                step = len([x for x in t if x == t[0]])
                log_rawdata = log_rawdata[::step,:]
                log_cal["samplerate"] = float(log_cal["samplerate"]) / step 
            log_rawdata = log_rawdata[int(len(log_rawdata) * .03):int(len(log_rawdata) * .97),:]    
    
        return log_rawdata, log_cal
        
    def downSample(self,cutOff, channel,iData):
         fs = self.sampleRate     # sample rate, Hz
         if len(iData.shape)>1:
             if (fs>100000):
                 highCut = 60000
                 secs = len(iData) / fs # Number of seconds in signal X
                 samps = secs * highCut     # number of samples in 10k sample rate
                 step = int(np.floor(len(iData) / samps))
                 iData = iData[::step,channel]
             else:
                 highCut =fs
                 iData = iData[:,channel]
         else:
             if (fs>100000):
                 highCut = 60000
                 secs = len(iData) / fs # Number of seconds in signal X
                 samps = secs * highCut     # number of samples in 10k sample rate
                 step = int(np.floor(len(iData) / samps))
                 iData = iData[::step]
             else:
                 highCut =fs
         
         sys.stdout.write('.filter.\n')
         order = 6
         fs = highCut    # sample rate, Hz
        
         iData = butter_lowpass_filter(np.asarray(iData), cutOff / 2, fs, order)
         iData = iData[~np.isnan(iData)]
         iData = iData[~np.isnan(iData)]
         secs = len(iData) / fs # Number of seconds in signal X
         samps = secs * cutOff     # number of samples in 10k sample rate
         iData = scipy.signal.resample(iData, samps) * pq.pA

         return iData[int(len(iData) * .02):int(len(iData) * .98)]

    def LoadAndConvertBDAQ(self, filename):
         print('load bdaq')
         parts = filename.split('|')
         filename = parts[0]
         sPath = self.outPath.replace("_" + parts[1],"_")
             
         iData,log_cal = self.ReadLogFile(filename)
         #iData=(1e12* self.ScaletoIData(iData,log_cal))*pq.pA

         self.sampleRate = float(log_cal["samplerate"])
         try:
            channelNames = log_cal['channelnames']
         except:
            channelNames = ['Ionic','M2_M1','M3_M1','M4_M1'];
         channelNames = channelNames[2:]
         if parts[1] in channelNames:
             channel = [i for i,x in enumerate(channelNames) if x == parts[1]][0]
         else:
             channel = 0
         
         vBias = channel + 2
         if channel == 0:
             vChannel = 0
             vBias = 0
         else:
             vChannel = 1
             if channel > 1:
                 vBias = channel + 4
                 
         voltage = (log_cal['biascorrections'][vBias] + log_cal['biasvoltages'][vBias]) - log_cal['biasvoltages'][vChannel]         
         goodChannel = ([int(i) for i,x in enumerate(channelNames) if x != "Skipped"])
         for i in range(0,len(goodChannel)):
            j = goodChannel[i]
            if (os.path.isfile(sPath + channelNames[j] + "_Full.bsn") ==False ):
                print(sPath + channelNames[j] + "_Full.bsn")
                t = iData[:,j]
                print(str(((np.asarray(np.mean(t)) - log_cal["in_os"][j]) / log_cal["in_gain"][j])))
                
                self.SaveShortBinary(sPath + channelNames[j] + "_Full.bsn",log_cal["in_gain"][j],log_cal["in_os"][j],self.sampleRate, voltage ,t)
                print(str(((np.asarray((t[0])) - log_cal["in_os"][j]) / log_cal["in_gain"][j])))
         t = 0
         #log_cal["numchannels"]=len(channelNames)
         
         lowPass = 20000
         print('try ionic')
         try:
             if (("Ionic" in channelNames) and (os.path.isfile(sPath + "Ionic.bsn") == False) and (os.path.isfile(sPath + "Ionic.json") == False) ):
                 ionic = self.downSample(lowPass,0,iData)
                 voltage = (log_cal['biascorrections'][0] + log_cal['biasvoltages'][0]) - log_cal['biasvoltages'][0] 
                 ionic = ((np.asarray(ionic) - log_cal["in_os"][0]) / log_cal["in_gain"][0]) * pq.pA
                 self.SaveShortBinaryD(sPath + "Ionic.bsn",lowPass,voltage,  ionic)
                 ionic = 0
         except:
             print('bad ionic')

         print(str(channel))
         self._drivingVoltage = (log_cal['biascorrections'][vBias] + log_cal['biasvoltages'][vBias]) - log_cal['biasvoltages'][vChannel] 

         iData = self.downSample(lowPass,channel,iData)
         self._current = ((np.asarray(iData) - log_cal["in_os"][channel]) / log_cal["in_gain"][channel]) * 1e12 * pq.pA
         print(self.outPath + ".bsn")
         realFile = self.outPath + "_Raw.bsn"
         self.SaveShortBinaryD(realFile,lowPass,self._drivingVoltage, self._current) 
         iData=[]
         self.sampleRate = lowPass
         self.header = log_cal
         self.sortTime = os.path.getctime(filename)
         self.fileDate = datetime.fromtimestamp(self.sortTime)#.strftime('%Y-%m-%d %H:%M:%S')
         
         self.startTime = self.fileDate
         
         self.stats.fileDate = self.fileDate
         self.stats.startTime = self.fileDate.strftime('%H:%M::%S')# str(hr) + ':' + str(mn) + '::' + str(sec)
         tOut = self.outPath
         for i in range(0,len(goodChannel)):
            j = goodChannel[i]
            if (os.path.isfile(sPath +  channelNames[j] + "_Raw.bsn") ==False ):
                [scale3,offset3,samplerate,voltage, results]=self.OpenShortBinary(sPath + channelNames[j] + "_Full.bsn")
                self.sampleRate=samplerate
                results = self.downSample(lowPass,0,results[0])
                self.sampleRate=lowPass
                self._current =np.asarray( results)*1e12 * pq.pA
                print(self.outPath + ".bsn")
                self.SaveShortBinaryD(sPath +  channelNames[j] + "_Raw.bsn",lowPass,self._drivingVoltage, self._current) 
    
                self._isLoaded = True
                self.sampleRate = float(self.header["samplerate"])
                self.stats.hasIonic = False
                self.stats.meanVoltage = self._drivingVoltage 
                self.stats.sampleRate = self.sampleRate  
                self.stats.sortTime = self.sortTime
                self.stats.fileDate = self.fileDate
                self.outPath = sPath +  channelNames[j] 
                self.calcStatistics()
         self.outPath=tOut   
         [scale3,offset3,samplerate,voltage, results]=self.OpenShortBinary(realFile)
         self._current =np.asarray( results[0])* pq.pA
             
    def _loadHeader(self):
        if self.fileType.lower() == '.abf':
             self.fileRef = io.AxonIO(filename=self.filePath)
             header = self.fileRef.read_header()
             self.fileDate = header['uFileStartDate']
             self.startTime = header['uFileStartTimeMS']
             s = float(self.fileDate) / 100
             s = long((s - int(s)) * 100)
             self.sortTime = s * 1000000000 + self.startTime
             self.stats.fileDate = self.fileDate
             hr = int(self.startTime / 1000 / 60 / 60)
             mn = int(self.startTime / 1000 / 60 - hr * 60)
             sec = int(self.startTime / 1000 - hr * 60 * 60 - mn * 60)
             self.stats.startTime = str(hr) + ':' + str(mn) + '::' + str(sec)

        if '.bdaq' in self.fileType.lower():
             self.LoadAndConvertBDAQ(self.filePath)

    def clearMemory(self):
        self._isLoaded = False        
        self.wTrace = []
        self.wImportance = []
        self.maxPeaks = []
        self.lPowerSpec = []
        self.justPeak = []
        self._current = []
        self._backFlatTunnelCurrents = []
        self.TV = []
        self._drivingVoltages = []
        self.fileData = []
        self.fileRef = []
        
    def __init__(self, fullPath, outPath, controlFile,concentrationMM,numPores):
        Trace.__init__(self,fullPath, outPath, controlFile,concentrationMM,numPores)