# -*- coding: utf-8 -*-
"""
Created on Sat Apr 16 16:38:19 2016

@author: bashc
"""
import ntpath
import PeakFinders
import Denoising
import sys
import numpy as np
from math import floor
import scipy
from scipy import signal
from scipy.interpolate import interp1d
import matplotlib.pyplot as plt
import quantities as pq #https://pypi.python.org/pypi/quantities
from neo import io # download from https://pythonhosted.org/neo/io.html
import neo
import os
import fnmatch
import datetime
#from pprint import pprint

dirSeperator=os.path.sep

def path_leaf(path):
    head, tail = ntpath.split(path)
    return tail or ntpath.basename(head)
    
def is_number(s):
    try:
        float(s)
        return True
    except ValueError:
        return False    

class TraceStatistics():
    def __init__(self, fullPath):
        self.filePath = fullPath
        self.fileName = path_leaf(fullPath)
        self.isControl=False
        self.topRef=0
        self.bttmRef=0
        self.sampleRate=0
        self.concentrationMM= 0
        self.analyte =''
        self.tunnelMV= 0
        self.traceTime =0
        self.hasIonic=False
        self.ionicMV=0
        self.otherInfo=''

#from numpy import mean, size, zeros, where, transpose
#from numpy.random import normal
#from matplotlib.pyplot import hist
#from scipy import linspace
#import array
#
#from matplotlib.pyplot import figure,  plot, xlabel, ylabel,  title, show, savefig, hist
        
class Trace:
    def conditionHash(self):
        return  str(self.stats.topRef) + '_' + str(self.stats.tunnelMV) +  '_' + str(self.stats.bttmRef)
        
    def junctionHash(self):
        if hasattr(self.stats, 'firstJunction'):
            m1=self.stats.firstJunction.replace('g','').replace('G','')
        else:
            m1='  '
            
        if hasattr(self.stats, 'secondJunction'):
            m2=self.stats.secondJunction.replace('g','').replace('G','')
        else:
            m2='  '
        return m1+m2

    def bayesian_blocks(self,t):
        """Bayesian Blocks Implementation
        https://jakevdp.github.io/blog/2012/09/12/dynamic-programming-in-python/
        By Jake Vanderplas.  License: BSD
        Based on algorithm outlined in http://adsabs.harvard.edu/abs/2012arXiv1207.5578S
    
        Parameters
        ----------
        t : ndarray, length N
            data to be histogrammed
    
        Returns
        -------
        bins : ndarray
            array containing the (N+1) bin edges
    
        Notes
        -----
        This is an incomplete implementation: it may fail for some
        datasets.  Alternate fitness functions and prior forms can
        be found in the paper listed above.
        """
        # copy and sort the array
        t = np.sort(t)
        N = t.size
    
        # create length-(N + 1) array of cell edges
        edges = np.concatenate([t[:1],
                                0.5 * (t[1:] + t[:-1]),
                                t[-1:]])
        block_length = t[-1] - edges
    
        # arrays needed for the iteration
        nn_vec = np.ones(N)
        best = np.zeros(N, dtype=float)
        last = np.zeros(N, dtype=int)
    
        #-----------------------------------------------------------------
        # Start with first data cell; add one cell at each iteration
        #-----------------------------------------------------------------
        for K in range(N):
            # Compute the width and count of the final bin for all possible
            # locations of the K^th changepoint
            width = block_length[:K + 1] - block_length[K + 1]
            count_vec = np.cumsum(nn_vec[:K + 1][::-1])[::-1]
    
            # evaluate fitness function for these possibilities
            fit_vec = count_vec * (np.log(count_vec) - np.log(width))
            fit_vec -= 4  # 4 comes from the prior on the number of changepoints
            fit_vec[1:] += best[:K]
    
            # find the max of the fitness: this is the K^th changepoint
            i_max = np.argmax(fit_vec)
            last[K] = i_max
            best[K] = fit_vec[i_max]
    
        #-----------------------------------------------------------------
        # Recover changepoints by iteratively peeling off the last block
        #-----------------------------------------------------------------
        change_points =  np.zeros(N, dtype=int)
        i_cp = N
        ind = N
        while True:
            i_cp -= 1
            change_points[i_cp] = ind
            if ind == 0:
                break
            ind = last[ind - 1]
        change_points = change_points[i_cp:]
    
        return edges[change_points]
    
    def autoHistBins(self,x):
        x_max = np.max(x)#http://toyoizumilab.brain.riken.jp/hideaki/res/histogram.html#Excel
        x_min = np.min(x)
        N_MIN = 20   #Minimum number of bins (integer)
                    #N_MIN must be more than 1 (N_MIN > 1).
        N_MAX = 500  #Maximum number of bins (integer)
        N = range(N_MIN,N_MAX) # #of Bins
        N = np.array(N)
        D = (x_max-x_min)/N    #Bin size vector
        C = np.zeros(shape=(np.size(D),1))
        
        #Computation of the cost function
        for i in xrange(np.size(N)):
            edges = scipy.linspace(x_min,x_max,N[i]+1) # Bin edges
            ki =np.histogram(x, bins=edges)[0]# plt.hist(x,edges) # Count # of events in bins
            ki = ki[0]    
            k = np.mean(ki) #Mean of event count
            v = np.sum((ki-k)**2)/N[i] #Variance of event count
            C[i] = (2*k-v)/((D[i])**2) #The cost Function
        #Optimal Bin Size Selection
        
        cmin = np.min(C)
        idx  = np.where(C==cmin)
        idx = int(idx[0])
#        optD = D[idx]
        
        return scipy.linspace(x_min,x_max,int(N[idx]*1.5+1))
        
    def plotHistogram(self,x, title, xlabel, ylabel):
#        edges=self.bayesian_blocks(x)
        
        edges=self.autoHistBins(x)
        plt.figure()
        plt.hist(x,bins=edges)
        plt.title(title)
        plt.ylabel(ylabel)
        plt.xlabel(xlabel)
#        savefig('Hist.png')         
#        fig = plt.figure()
#        plt.plot(D,C,'.b',optD,cmin,'*r')
#        savefig('Fobj.png')
    
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
        return float(text.lower().replace('g','0').replace('p','').replace('n','-').replace('mv',''))

    def _parseFilename(self):
        pParts=self.filePath.split(dirSeperator)
        filename =(path_leaf(self.filePath).split(".")[0]) 
        sParts=filename.split('_')
        if is_number(sParts[0])==False:
            self.stats.firstJunction=sParts[0]
            self.stats.secondJunction=sParts[1]
        else:
            self.stats.firstJunction=''
            self.stats.secondJunction=''
            
        self.fileType=ntpath.splitext(self.filePath)[-1]
        self.isControl=not (not [x for x in pParts if ('control' in x.lower() or 'rinse' in x.lower())])
        self.stats.isControl =self.isControl
        
        self.isIonic = ('ion' in filename.lower()) 
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
            self.concentrationMM=self.stats.concentrationMM
            self.analyte = self.stats.analyte
            sParts=[x for x in sParts if concentration[0] not in x]
            
        self.stats.ionicMV=self.stats.topRef-self.stats.bttmRef     
        
        tunnel = [x for x in sParts if 'mv' in x.lower()]
        if not (not top):
            self.stats.tunnelMV= self._makeNumber(tunnel[0].lower())*pq.mV
            sParts=[x for x in sParts if tunnel[0] not in x]

        if self.stats.firstJunction=='':
            junctions = fnmatch.filter(sParts, 'M?M*') #MxMx or MxMxG
            if not (not junctions):
                junctions=junctions[0]
                self.stats.firstJunction=junctions[0:2]
                self.stats.secondJunction=junctions[2:]
            junctions = fnmatch.filter(sParts, 'M??M*') #MxGMx
            if not (not junctions):
                junctions=junctions[0]
                self.stats.firstJunction=junctions[0:3]
                self.stats.secondJunction=junctions[3:]
            
        if is_number(sParts[-1]):
            sParts=[x for x in sParts if sParts[-1] not in x]
            
        numPores = [x for x in sParts if 'numpores' in x.lower()]
        if not (not numPores):
            self.stats.number_of_pores = self._makeNumber(numPores[0])
            sParts=[x for x in sParts if numPores[0] not in x]
            
        self.stats.otherInfo ='_'.join('{}'.format(k) for k in sParts) 
    
    def _loadFile(self):
        self._abstractLoadFile()
        self.stats.sampleRate=self.sampleRate        
        
    def _loadHeader(self):
        self.sortTime=datetime.datetime.fromtimestamp(os.path.getmtime(self.filePath))
        
    def __init__(self,fullPath,experiment):
        self._experiment=experiment
        self._isLoaded=False
        self.filePath = fullPath
        self.fileName = path_leaf(fullPath)
        self.stats=TraceStatistics(fullPath)
        self.isControl=False
        self.concentrationMM= 0
        self.analyte =''
        self.sampleRate =0
        self.sortTime =0
        self._parseFilename()
        self._loadHeader()


            
class DataTrace(Trace):
    def calcStatistics(self):
        t=self.Currents()      
       
        sTrace=Denoising.smooth(t,200)
        seg = neo.Segment(name='Smoothed')
        self.fileData.segments.append(seg)
        anasig = neo.AnalogSignal(sTrace, units='pA', t_start=0*pq.s, sampling_rate=self.samplingRate/pq.sec)
        seg.analogsignals.append(anasig) 
        sTrace=None
        
        self.stats.tun_grossBaseline = t.mean()
        self.stats.tun_range =t.max()-t.min()
        self.stats.tun_std = t.std()
        t=t-t.mean()
        self.stats.traceTime = t.times.max()-t.times.min()
#      
        sys.stdout.write('.wFilter.')
        self._filterTraces(t)
        self._findPeaks(t,100)
        
        w=1/(1+np.abs(self.wTrace)+np.asarray(self.stats.tun_grossBaseline))**.5
        self.stats.tun_flatBaseline=self.stats.tun_grossBaseline+np.sum(t[0:len(w)]*w)/sum(w)
        self.stats.tun_noise=2*np.mean(abs(t))
        self.stats.tun_wNoise=np.std(self.wTrace)*pq.pA
#        plt.show()
        ivCurve=self._experiment.getTunnelIVStats(self)
        if not (not ivCurve):
            self.stats.expected_grossBaseline=ivCurve.GetCurveValue(self.stats.meanVoltage)
            self.stats.leakage_current=self.stats.tun_grossBaseline- self.stats.expected_grossBaseline
        return Trace.calcStatistics(self)
        
    
    def estimatePower(self, specSize):
        if not hasattr(self,'powerSpec'):
            t=self.Currents()
            sys.stdout.write('.')
            f1,self.powerSpec = signal.welch(t, self.samplingRate, 'flattop', specSize, scaling='spectrum')
            sys.stdout.write('.')
            f2,self.lPowerSpec = signal.welch(t[0:-1:100], self.samplingRate, 'flattop', specSize, scaling='spectrum')
            
            seg = neo.Segment(name='PowerspecShort')
            self.fileData.segments.append(seg)
            anasig = neo.AnalogSignal(self.powerSpec , units='pA', t_start=0*pq.s, sampling_rate=np.mean(np.diff(f1)) *pq.hertz)
            seg.analogsignals.append(anasig) 
            
            seg = neo.Segment(name='PowerspecLong')
            self.fileData.segments.append(seg)
            anasig = neo.AnalogSignal(self.lPowerSpec , units='pA', t_start=0*pq.s,  sampling_rate=np.mean(np.diff(f1)) *pq.hertz)
            seg.analogsignals.append(anasig) 

#            plt.figure(3)
#            plt.plot(np.squeeze(f1),np.squeeze(np.log10(.001+ self.powerSpec)),np.squeeze(f2),np.squeeze(np.log10(.001+self.lPowerSpec)))
#            plt.xlabel('Frequency')
#            plt.ylabel('Current (pA)')
#            plt.title('Power spectrum')
#            plt.grid(True)
            
        return self.powerSpec
            
    def _filterTraces(self,t):    
        controlTrace=self._experiment.getNearestControl(self)
        sys.stdout.write('.')
        self.estimatePower(1024)
        sys.stdout.write('.')
        self.wTrace,wCoef, self.wImportance=Denoising.WienerScalart(t,self.samplingRate,controlTrace.estimatePower(1024),ControlWeight=1,NoiseMargin=45)
        seg = neo.Segment(name='Wiener')
        self.fileData.segments.append(seg)
        anasig = neo.AnalogSignal(self.wTrace , units='pA', t_start=0*pq.s,  sampling_rate=self.samplingRate/pq.sec)
        seg.analogsignals.append(anasig) 
#        plt.figure(2)
#        plt.plot(t.times[0:len(self.wTrace):100],t[0:len(self.wTrace):100],t.times[0:len(self.wTrace):100], self.wTrace[0:len(self.wTrace):100])
#        plt.xlabel('time (s)')
#        plt.ylabel('Current (pA)')
#        plt.title('Denoised')
#        plt.grid(True)
       
        
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
            steps=int(np.min((400, floor(len(idx)/2))))
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
     
    def traceTVLambda(self):
        return Denoising.tvdiplmax(self.Currents())
        
    def _findPeaks(self,t, threshold):
        background,active_IDX=self._findBackground(t)
        t=t-background*pq.pA
        
        self.plotHistogram(t,'Signal -- Uncorrected Time','Current','Frequency')
        plt.show()
        plt.savefig('S:\\Research\\BrianAnalysis\\GitAnalysis\\Sig_' + self.fileName + '.pdf', bbox_inches='tight')
        
        seg = neo.Segment(name='Background')
        self.fileData.segments.append(seg)
        anasig = neo.AnalogSignal(t , units='pA', t_start=0*pq.s,  sampling_rate=self.samplingRate/pq.sec)
        seg.analogsignals.append(anasig)         

        cusumSignal,cusumLevels,cusumLevelTimes= Denoising.cusum(np.asarray(t),50,50)
        self.plotHistogram(cusumSignal,'Leveled -- Uncorrected Time','Current','Frequency')
        plt.show()     
        plt.savefig('S:\\Research\\BrianAnalysis\\GitAnalysis\\Lev_' + self.fileName + '.png', bbox_inches='tight')
        
        self.plotHistogram(np.abs( (cusumLevelTimes[1:] - cusumLevelTimes[0:-1]) ),'Level Times','Time','Frequency')
        plt.show() 
        plt.savefig('S:\\Research\\BrianAnalysis\\GitAnalysis\\LevTime_' + self.fileName + '.png', bbox_inches='tight')
        
        self.plotHistogram(cusumLevels,'Fast Levels','Current','Frequency')
        plt.show()
        plt.savefig('S:\\Research\\BrianAnalysis\\GitAnalysis\\Fast_' + self.fileName + '.png', bbox_inches='tight')
        
        
        seg = neo.Segment(name='CUSUM')
        self.fileData.segments.append(seg)
        anasig = neo.AnalogSignal(cusumSignal , units='pA', t_start=0*pq.s,  sampling_rate=self.samplingRate/pq.sec)
        seg.analogsignals.append(anasig) 
        
        seg = neo.Segment(name='CUSUM_Levels')
        self.fileData.segments.append(seg)
        anasig = neo.AnalogSignal(cusumLevels , units='pA', t_start=0*pq.s,  sampling_rate=self.samplingRate/pq.sec)
        seg.analogsignals.append(anasig) 
        
        seg = neo.Segment(name='CUSUM_Times')
        self.fileData.segments.append(seg)
        anasig = neo.AnalogSignal(cusumLevelTimes , units='s', t_start=0*pq.s,  sampling_rate=self.samplingRate/pq.sec)
        seg.analogsignals.append(anasig) 
        
        sys.stdout.write('.tvFilter.')
        TV = Denoising.tvdip(np.asarray(t),self._experiment.experimentLambda,0,1e-3,40,z=cusumSignal)[0]
        seg = neo.Segment(name='TV')
        self.fileData.segments.append(seg)
        anasig = neo.AnalogSignal(TV , units='pA', t_start=0*pq.s,  sampling_rate=self.samplingRate/pq.sec)
        seg.analogsignals.append(anasig) 
        
        plt.figure(5)
        plt.plot(t.times[0:-1:100],t[0:-1:100],
                 t.times[0:len(cusumSignal):100], cusumSignal[0:len(cusumSignal):100],
                 t.times[0:len(TV):100], TV[0:len(TV):100])
        plt.xlabel('time (s)')
        plt.ylabel('Current (pA)')
        plt.title(self.stats.fileName)
        plt.grid(True)
        plt.show()
        plt.savefig('S:\\Research\\BrianAnalysis\\GitAnalysis\\Raw_' + self.fileName + '.png', bbox_inches='tight')
        
        
#        dTV=np.asarray([i for i,x in enumerate( np.abs(np.diff(TV))  ) if x>10])
#        mids=TV[ np.round( (dTV[1:]+ dTV[0:-1])/2) ]
#        mids=np.asarray([x for x in mids if x>20])
        dTV= np.abs(np.diff(TV)) 
        sys.stdout.write('.peakfindTV.')
        maxPeaks, minPeaks =PeakFinders.peakdetect(dTV)
        idxM=[x[0] for x in maxPeaks]
        idxm=[x[0] for x in minPeaks]
        idx=np.zeros(len(idxM) + len(idxm),dtype= np.int32)
        idx[:(len(idxM))]=idxM
        idx[len(idxM):]=idxm
        idx.sort()
        mids=TV[ np.round( (idx[1:]+ idx[0:-1])/2) ]
        mids=np.asarray([x for x in mids if x>20])
        
        self.plotHistogram(mids,'Slow Levels','Current','Frequency')
        plt.show()
        plt.savefig('S:\\Research\\BrianAnalysis\\GitAnalysis\\Slow_' + self.fileName + '.pdf', bbox_inches='tight')
        
        seg = neo.Segment(name='TV_Levels')
        self.fileData.segments.append(seg)
        anasig = neo.AnalogSignal(mids , units='pA', t_start=0*pq.s,  sampling_rate=self.samplingRate/pq.sec)
        seg.analogsignals.append(anasig) 
        
        seg = neo.Segment(name='TV_Times')
        self.fileData.segments.append(seg)
        anasig = neo.AnalogSignal(idx , units='s', t_start=0*pq.s,  sampling_rate=self.samplingRate/pq.sec)
        seg.analogsignals.append(anasig) 
#        thresh=np.min((threshold, np.std(t)))
#        peakFind=np.zeros(np.shape(background))
#        peakFind[[i for i,x in enumerate(TV) if x>thresh]]=1
#        peakFind[active_IDX]=1
#        peakFind=scipy.ndimage.filters.gaussian_filter1d(peakFind,100)
#        peakIDX= [i for i,x in enumerate(peakFind) if x!=0]
#        peakFind[peakIDX]=1
#        self.stats.numberRegions=len([ x for x in np.diff(peakFind) if x==1])
#        peakFind=[]
#        self.justPeaks=t[ peakIDX ]
        
        
        sys.stdout.write('.peakfind.')
        maxPeaks, minPeaks =PeakFinders.peakdetect(t)
        self.maxPeaks=[x[1] for x in maxPeaks if np.abs(x[1])>threshold]
        self.plotHistogram(self.maxPeaks,'Spike','Current','Frequency')
        plt.show()
        plt.savefig('S:\\Research\\BrianAnalysis\\GitAnalysis\\Spike_' + self.fileName + '.pdf', bbox_inches='tight')
        
        seg = neo.Segment(name='Peaks')
        self.fileData.segments.append(seg)
        anasig = neo.AnalogSignal(self.maxPeaks , units='pA', t_start=0*pq.s,  sampling_rate=self.samplingRate/pq.sec)
        seg.analogsignals.append(anasig) 
        return 1
        


    def saveABF(self,filename):
        w = io.NeoMatlabIO(filename=filename)
        w.write_block(self.fileData)
        
    def _abstractLoadFile(self):
        if self.fileType.lower() == '.abf':
             if not self.fileRef:
                self.fileRef = io.AxonIO(filename=self.filePath)
             self._isLoaded=True
             self.fileData = self.fileRef.read_block(lazy=False, cascade=True)
             self._drivingVoltage = self.fileData.segments[0].analogsignals[1]
             if np.mean(self._drivingVoltage)>1:
                 self._drivingVoltage =self._drivingVoltage /10
                 
             self._current = self.fileData.segments[0].analogsignals[0]
#             self._current=self._current[0:int(len(self._current)/10)]
             self.samplingRate =float( self.fileData.segments[0].analogsignals[1].sampling_rate)
             if len(self.fileData.segments[0].analogsignals)>2:
                 self.hasIonic = True
                 self._ionicCurrents =np.asarray( self.fileData.segments[0].analogsignals[2])
             else:
                 self.hasIonic=False
             self.stats.hasIonic=self.hasIonic
             self.stats.meanVoltage = np.mean( self._drivingVoltage )
                
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
             hr=int( self.startTime/1000/60/60)
             mn=int(self.startTime/1000/60 - hr*60)
             sec=int( self.startTime/1000 - hr*60*60-mn*60)
             self.stats.startTime = str(hr) + ':' + str(mn) + '::' + str(sec)

    def clearMemory(self):
        self._isLoaded=False        
        self.wTrace=[]
        self.wImportance=[]
        self.maxPeaks=[]
        self.lPowerSpec=[]
        self.justPeak=[]
        self._current=[]
        self._backFlatTunnelCurrents=[]
        self.TV=[]
        self._drivingVoltages=[]
        self.fileData=[]
        self.fileRef=[]
        
    def __init__(self, fullPath, experiment):
        Trace.__init__(self,fullPath,experiment)