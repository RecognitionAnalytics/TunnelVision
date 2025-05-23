# -*- coding: utf-8 -*-
"""
Created on Fri Apr 08 17:13:06 2016

@author: bashc
"""
#need to install quantities and neo.io
#pip install quantities
#pip install neo
#

import ntpath
import PeakFinders
import Denoising
import numpy as np
from math import floor
import scipy
from scipy import signal
from scipy.interpolate import interp1d
import matplotlib.pyplot as plt
import quantities as pq #https://pypi.python.org/pypi/quantities
from neo import io # download from https://pythonhosted.org/neo/io.html
import os
import fnmatch
from pprint import pprint
from matplotlib import interactive
interactive(False)

dirSeperator=os.path.sep

def path_leaf(path):
    head, tail = ntpath.split(path)
    return tail or ntpath.basename(head)
    
class TraceStatistics:
    def __init__(self, fullPath):
        self.filePath = fullPath
        self.fileName = path_leaf(fullPath)
        self.isControl=False
        self.isHumanGood= False
        self.topRef=0
        self.bttmRef=0
        self.concentrationMM= 0
        self.analyte =''
        self.tunnelMV= 0
        self.tun_grossBaseline = 0
        self.tun_range =0
        self.tun_std = 0
        self.traceTime =0
        self.tun_flatBaseline=0
        self.tun_noise=0
        self.tun_wNoise=0
        self.hasIonic=False
        
class DataTrace:
    def calcStatistics(self):
        t=self.tunnelCurrents()        
        self.stats.tun_grossBaseline = t.mean()
        self.stats.tun_range =t.max()-t.min()
        self.stats.tun_std = t.std()
        t=t-t.mean()
        self.stats.traceTime = t.times.max()-t.times.min()
#        plt.figure(1)
#        plt.plot(t.times[0:len(t):100], t[0:len(t):100])
#        plt.xlabel('time (s)')
#        plt.ylabel('Current (pA)')
#        plt.title('Raw')
#        plt.grid(True)
        
        print('Filtering')
        self._filterTraces(t)
        print('Peak Finding')
        self._findPeaks(t,100)
        
        w=1/(1+np.abs(self.wTrace)+np.asarray(self.stats.tun_grossBaseline))**.5
        self.stats.tun_flatBaseline=self.stats.tun_grossBaseline+np.sum(t[0:len(w)]*w)/sum(w)
        self.stats.tun_noise=2*np.mean(abs(t))
        self.stats.tun_wNoise=np.std(self.wTrace)*pq.pA
#        plt.show()
        return self.stats
        
    
    def estimatePower(self, specSize):
        if not hasattr(self,'powerSpec'):
            t=self.tunnelCurrents()
            f1,self.powerSpec = signal.welch(t, self.samplingRate, 'flattop', specSize, scaling='spectrum')
            f2,self.lPowerSpec = signal.welch(t[0:-1:100], self.samplingRate, 'flattop', specSize, scaling='spectrum')
            
#            plt.figure(3)
#            plt.plot(np.squeeze(f1),np.squeeze(np.log10(.001+ self.powerSpec)),np.squeeze(f2),np.squeeze(np.log10(.001+self.lPowerSpec)))
#            plt.xlabel('Frequency')
#            plt.ylabel('Current (pA)')
#            plt.title('Power spectrum')
#            plt.grid(True)
            
        return self.powerSpec
            
    def _filterTraces(self,t):    
        controlTrace=self._experiment.getNearestControl(self)
        self.estimatePower(1024)
        self.wTrace,wCoef, self.wImportance=Denoising.WienerScalart(t,self.samplingRate,controlTrace.estimatePower(1024),ControlWeight=1,NoiseMargin=45)
#        plt.figure(2)
#        plt.plot(t.times[0:len(self.wTrace):100],t[0:len(self.wTrace):100],t.times[0:len(self.wTrace):100], self.wTrace[0:len(self.wTrace):100])
#        plt.xlabel('time (s)')
#        plt.ylabel('Current (pA)')
#        plt.title('Denoised')
        plt.grid(True)
       
        
    def _findBackground(self,shortData):
        sTrace=Denoising.smooth(self.wTrace,200)
        sTrace=sTrace-np.mean(sTrace)
        peakFind=np.abs(sTrace-np.mean(sTrace[ [i for i,x in enumerate(sTrace) if x<2] ]))
        peakFind[[i for i,x in enumerate(peakFind) if x<25]]=0
        active_IDX=[i for i,x in enumerate(peakFind) if x!=0]
        peakFind[active_IDX]=1
        peakFind=scipy.ndimage.filters.gaussian_filter1d(peakFind,100)
        peakFind[[i for i,x in enumerate(peakFind) if x!=0]]=1
        
        idx=[i for i,x in enumerate(peakFind) if x==0]
        if len(idx)<1000:
        #        %basically assumes that the mean is the baseline
            if not idx:
                back=np.zeros(np.shape(shortData))+np.mean(shortData)
            else:
                if idx[0]!=1:
                    idx=np.append([0], idx)
                if idx[-1]!=len(shortData):
                    idx = np.append(idx,[len(shortData)])
                back=Denoising.smooth( shortData[idx] ,1000)
                back= interp1d(idx,back)(np.arange(0,len(shortData)))
        else:
#            %extract the minimums and use those for the baseline
            steps=np.min((400, floor(len(idx)/2)))
            minX=np.zeros( steps+1)
            mins=np.zeros( steps+1)
            minX[0]=-.01
            mins[0]=np.mean(shortData[0:100])
            minX[steps]=len(shortData)+.01
            mins[steps]=np.mean(shortData[-100:-1])
            
            tt=shortData[idx]
            stp=int(len(tt)/steps)
            for I in range(0,int(steps)-1):
                I2=I*stp+1
                x=idx[I2:(I2+stp-2)]
                if not (not x):
                    t=tt[I2:(I2+stp-2)]
                    idx2=np.argmin(t);
                    mins[I+1]=np.mean(t[max([ 1 ,(idx2-100)]):min([len(t), (idx2+100)])])
                    minX[I+1]=x[idx2]
            
            back=Denoising.smooth(mins,15)
#            plt.figure(1)
#            plt.plot(np.arange(0,len(shortData))[0:-1:100],shortData[0:-1:100],minX,mins,minX,back)
#            plt.xlabel('time (s)')
#            plt.ylabel('Current (pA)')
#            plt.title('background')
#            plt.grid(True)
#            plt.show()
            back= interp1d(minX,back)(np.arange(0,len(shortData)))
        return (back,active_IDX)
            
    def _findPeaks(self,t, threshold):
        print('Background')
        background,active_IDX=self._findBackground(t)
#        plt.figure(4)
#        plt.plot(t.times[0:-1:100],t[0:-1:100],t.times[0:len(background):100], background[0:len(background):100])
#        plt.xlabel('time (s)')
#        plt.ylabel('Current (pA)')
#        plt.title('Background')
#        plt.grid(True)
        
        
        t=t-background*pq.pA

        thresh=np.min((threshold, np.std(t)))
        
        print('Median')
#        TV=Denoising.TV_smooth(t, .001)
#        TV=Denoising.Median_smooth(t,5)
        lmax = Denoising.tvdiplmax(t)
#        step = int(len(t)/10)
#        for i in range(0,10):
#            TV = Denoising.tvdip(np.asarray(t[(i*step):((i+1)*step)]),float(lmax*(1e-3)),1,1e-3,40)
        TV = Denoising.tvdip(np.asarray(t),float(lmax*(1e-3)),1,1e-3,40)[0]
#        plt.figure(5)
#        plt.plot(t.times[0:-1:100],t[0:-1:100],t.times[0:len(TV):100], TV[0:len(TV):100])
#        plt.xlabel('time (s)')
#        plt.ylabel('Current (pA)')
#        plt.title('Median')
#        plt.grid(True)
       
        
        peakFind=np.zeros(np.shape(background))
        peakFind[[i for i,x in enumerate(TV) if x>thresh]]=1
        peakFind[active_IDX]=1
        peakFind=scipy.ndimage.filters.gaussian_filter1d(peakFind,100)
        peakIDX= [i for i,x in enumerate(peakFind) if x!=0]
        peakFind[peakIDX]=1
        self.stats.numberRegions=len([ x for x in np.diff(peakFind) if x==1])
        peakFind=[]
        self.justPeaks=t[ peakIDX ]
        print('Peak detect')
        maxPeaks, minPeaks =PeakFinders.peakdetect(t)
        self.maxPeaks=[x[1] for x in maxPeaks if np.abs(x[1])>threshold]
        self.backFlatTunnelCurrents=t
        self.TV=TV
        
        
    def conditionHash(self):
        return  str(self.stats.topRef) + '_' + str(self.stats.tunnelMV) +  '_' + str(self.stats.bttmRef)
        
    def ionicMV(self):
        return self.stats.topRef-self.stats.bttmRef
    
    def ionicCurrents(self):
        if not self._isLoaded:
            self._loadFile()
        return self._ionicCurrents

    def tunnelCurrents(self):
        if not self._isLoaded:
            self._loadFile()
        return self._tunnelCurrents

    def tunnelVoltages(self):
        if not self._isLoaded:
            self._loadFile()
        return self._drivingVoltages            
        
    def _makeNumber(self,text):
        return float(text.lower().replace('g','0').replace('p','').replace('n','-').replace('mv',''))

    def _loadFile(self):
        if self.fileType.lower() == '.abf':
             self._isLoaded=True
             self.fileData = self.fileRef.read_block(lazy=False, cascade=True)
             self._drivingVoltages = self.fileData.segments[0].analogsignals[1]
             if np.mean(self._drivingVoltages)>1:
                 self._drivingVoltages =self._drivingVoltages /10
                 
             self._tunnelCurrents = self.fileData.segments[0].analogsignals[0]
             self._tunnelCurrents=self._tunnelCurrents[0:int(len(self._tunnelCurrents)/3)]
             self.samplingRate =float( self.fileData.segments[0].analogsignals[1].sampling_rate)
             if len(self.fileData.segments[0].analogsignals)>2:
                 self.hasIonic = True
                 self._ionicCurrents =np.asarray( self.fileData.segments[0].analogsignals[2])
             else:
                 self.hasIonic=False
             self.stats.hasIonic=self.hasIonic
             self.stats.meanVoltage = np.mean( self._drivingVoltages )
                
    def _loadHeader(self):
        if self.fileType.lower() == '.abf':
             self.fileRef = io.AxonIO(filename=self.filePath)
             header=self.fileRef.read_header()
             self.fileDate = header['uFileStartDate']
             self.startTime= header['uFileStartTimeMS']
             s=float(self.fileDate)/100;
             s=long((s-int(s))*100)
             self.sortTime = s*1000000000+self.startTime
             self.stats.fileDate = self.fileDate
             self.stats.startTime = self.startTime

             
        
    def _parseFilename(self,fullFileName):
        pParts=fullFileName.split(dirSeperator)
        filename =(path_leaf(fullFileName).split(".")[0]) 
        sParts=filename.split('_')
        self.fileType=ntpath.splitext(fullFileName)[-1]
        self.isControl=not (not [x for x in pParts if ('control' in x.lower() or 'rinse' in x.lower())])
        self.stats.isControl =self.isControl
        self.stats.isHumanGood= filename.lower().endswith('_g')
        if self.stats.isHumanGood:
            sParts= [x for x in sParts if x is not 'g']

        top = [x for x in sParts if 'top' in x.lower()]
        if not (not top):
            self.stats.topRef= self._makeNumber(top[0].lower().replace('top',''))*pq.mV
            sParts=[x for x in sParts if top[0] not in x]
        bttmRef = [x for x in sParts if 'bttm' in x.lower()]
        if not (not bttmRef):
            self.stats.bttmRef= self._makeNumber(bttmRef[0].lower().replace('bttm',''))*pq.mV
            sParts=[x for x in sParts if bttmRef[0] not in x]
            
        concentration = [x for x in sParts if ( ('mm' in x.lower()) and (not 'mv' in x.lower()))]
        if (not concentration):
            concentration = [x for x in sParts if ( ('nm' in x.lower()) and (not 'mv' in x.lower()))]
            if not (not concentration):
                cParts=concentration[0].lower().split('nm')              
                self.stats.concentrationMM= float(cParts[0])/1000*pq.mM;
                self.stats.analyte=cParts[1];
                sParts=[x for x in sParts if concentration[0] not in x]
        else:
            cParts=concentration[0].lower().split('mm')              
            self.stats.concentrationMM=  float(cParts[0])*pq.mM;
            self.stats.analyte=cParts[1];
            sParts=[x for x in sParts if concentration[0] not in x]
             
        tunnel = [x for x in sParts if 'mv' in x.lower()]
        if not (not top):
            self.stats.tunnelMV= self._makeNumber(tunnel[0].lower())*pq.mV
            sParts=[x for x in sParts if tunnel[0] not in x]
            
    def clearMemory(self):
        self._isLoaded=False        
        self.wTrace=[]
        self.wImportance=[]
        self.maxPeaks=[]
        self.lPowerSpec=[]
        self.justPeak=[]
        self._tunnelCurrents=[]
        self._backFlatTunnelCurrents=[]
        self.TV=[]
        self._drivingVoltages=[]
        self.fileData=[]
        self.fileRef=[]
                
        
    def __init__(self, fullPath, experiment):
        self._experiment=experiment
        self.filePath = fullPath
        self._isLoaded=False
        self.fileType=''
        self.isControl=False
        self.stats= TraceStatistics(fullPath)
        self.sortTime =0
        self._parseFilename(fullPath)
        self._loadHeader()
        
class Experiment:
    def _getParseChunk(self,chunks,search):
        chunk = [x for x in chunks if search in x.lower()]
        if not (not chunk):
            return ([x for x in chunks if chunk[0] not in x] , chunk[0])
        else:
            return (chunks,'')

    def _parseDirectory(self,directoryPath):
        pParts=directoryPath.split(dirSeperator)
        pParts,experimentName =self._getParseChunk(pParts,'_nal')
        sParts=experimentName.split('_')
        self.experimentName=experimentName
        sParts,self.batch = self._getParseChunk(sParts,'nal')
        try:
            self.experimentDate=int(sParts[0])
            sParts=[x for x in sParts if sParts[0] not in x]   
        except  ValueError:
            self.experimentDate=0
       
        sParts,self.chip = self._getParseChunk(sParts,'chip')
        sParts,self.ald = self._getParseChunk(sParts,'cyc')
        
        him = [x for x in sParts if 'him' in x.lower()]
        if not (not him):
            self.method='HIM'
            self.methodInfo = him[0].lower().replace('him','')
            sParts=[x for x in sParts if him[0] not in x]
        
        fib = [x for x in sParts if 'fib' in x.lower()]
        if not (not fib):
            self.method='FIB'
            self.methodInfo = fib[0].lower().replace('fib','')
            sParts=[x for x in sParts if fib[0] not in x]
        
        rie = [x for x in sParts if 'rie' in x.lower()]
        if not (not rie):
            self.method='RIE'
            self.methodInfo = rie[0].lower().replace('rie','')
            sParts=[x for x in sParts if rie[0] not in x]
        
        self.notes = ' '.join(sParts)

    def _getFiles(self):
        self.dataTraces=[]
        for root, dirnames, filenames in os.walk(self.fullPath):
            beforeDrilling = [x for x in dirnames if 'before_drilling_iv' in x.lower()]
            if not (not beforeDrilling):
                self.beforeDrillingIVFolder = (root + dirSeperator + beforeDrilling[0])
            beforeExperiment = [x for x in dirnames if 'before_iv' in x.lower()]
            if not (not beforeExperiment):
                self.beforeExperimentIVFolder = (root + dirSeperator + beforeExperiment[0])
            afterExperiment = [x for x in dirnames if 'after_iv' in x.lower()]
            if not (not afterExperiment):
                self.afterExperimentIVFolder = (root + dirSeperator + afterExperiment[0])    
            for filename in fnmatch.filter(filenames, '*.abf'):
                self.dataTraces.append(DataTrace(root + dirSeperator + filename, self))
        self.dataTraces.sort(key=lambda x: x.sortTime, reverse=False)
        
        
                
    def getNearestControl(self, testTrace):
        searchCondition = testTrace.conditionHash()
        searchTime = testTrace.sortTime
        pos = [x for x in self.dataTraces if (x.conditionHash()==searchCondition and x.sortTime<searchTime and x.isControl)]
        if not (not pos):
            return pos[-1]
        else:
            pos = [x for x in self.dataTraces if (x.sortTime<searchTime and x.isControl)]
            if not (not pos):
                return pos[-1]
            else:
                return testTrace
                
    def runStatistics(self):
        table=''
        for i in range(0,len( self.dataTraces)):
            stats=self.dataTraces[i].calcStatistics()
            if table=='':
                table=','.join('{}'.format(k) for k in vars(stats).keys())
            table=table + '\n' + ','.join('{}'.format(k) for k in vars(stats).values())
            print(table)
            self.dataTraces[i].clearMemory()
        return table
                
    def __init__(self, fullPath):
        self.fullPath=fullPath
        self.experimentName=''
        self.batch = ''
        self.experimentDate=0
        self.ald=''
        self.method=''
        self.methodInfo = ''
        self.notes =''
        self.dataTraces=[]
        self.beforeDrillingIVFolder = ''
        self.beforeExperimentIVFolder = ''
        self.afterExperimentIVFolder=''
        self._parseDirectory(fullPath)
        self._getFiles()
        
directoryPath= 'S:\\Research\\Stacked Junctions\\Results\\20160229_NALDB34_Chip03_22cyc_HIM3pC_UVozone10each_200deg_1mMPB_pH7_modified\\For Brain analysis\\M1-M2'
e=Experiment(directoryPath)
e.runStatistics()

#print(f.calcStatistics())
