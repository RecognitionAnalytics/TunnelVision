# -*- coding: utf-8 -*-
"""
Created on Mon Apr 11 08:46:30 2016

@author: bashc
"""

from ExperimentMain import path_leaf,dirSeperator
    
class DataTrace:
    def calcStatistics(self):
        t=self.tunnelCurrents()        
        self.tun_grossBaseline = t.mean()
        self.tun_range =t.max()-t.min()
        self.tun_std = t.std()
        t=t-t.mean()
        self.traceTime = t.times.max()-t.times.min()
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
        
        w=1/(1+np.abs(self.wTrace)+np.asarray(self.tun_grossBaseline))**.5
        self.tun_flatBaseline=self.tun_grossBaseline+np.sum(t[0:len(w)]*w)/sum(w)
        self.tun_noise=2*np.mean(abs(t))
        self.tun_wNoise=np.std(self.wTrace)
        plt.show()
        
    
    def estimatePower(self, specSize):
        if not hasattr(self,'powerSpec'):
            t=self.tunnelCurrents()
            f1,self.powerSpec = signal.welch(t, self.samplingRate, 'flattop', specSize, scaling='spectrum')
            f2,self.lPowerSpec = signal.welch(t[0:-1:100], self.samplingRate, 'flattop', specSize, scaling='spectrum')
            plt.figure(3)
            plt.plot(np.squeeze(f1),np.squeeze(np.log10(.001+ self.powerSpec)),np.squeeze(f2),np.squeeze(np.log10(.001+self.lPowerSpec)))
            plt.xlabel('Frequency')
            plt.ylabel('Current (pA)')
            plt.title('Power spectrum')
            plt.grid(True)
            
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
        plt.figure(4)
        plt.plot(t.times[0:-1:100],t[0:-1:100],t.times[0:len(background):100], background[0:len(background):100])
        plt.xlabel('time (s)')
        plt.ylabel('Current (pA)')
        plt.title('Background')
        plt.grid(True)
        
        
        t=t-background*pq.pA

        thresh=np.min((threshold, np.std(t)))
        
        print('Median')
#        TV=Denoising.TV_smooth(t, .001)
        TV=Denoising.Median_smooth(t,401)
        plt.figure(5)
        plt.plot(t.times[0:-1:100],t[0:-1:100],t.times[0:len(TV):100], TV[0:len(TV):100])
        plt.xlabel('time (s)')
        plt.ylabel('Current (pA)')
        plt.title('Median')
        plt.grid(True)
       
        
        peakFind=np.zeros(np.shape(background))
        peakFind[[i for i,x in enumerate(TV) if x>thresh]]=1
        peakFind[active_IDX]=1
        peakFind=scipy.ndimage.filters.gaussian_filter1d(peakFind,100)
        peakIDX= [i for i,x in enumerate(peakFind) if x!=0]
        peakFind[peakIDX]=1
        self.numberRegions=len([ x for x in np.diff(peakFind) if x==1])
        peakFind=[]
        self.justPeaks=t[ peakIDX ]
        print('Peak detect')
        maxPeaks, minPeaks =PeakFinders.peakdetect(t)
        self.maxPeaks=[x[1] for x in maxPeaks if np.abs(x[1])>threshold]
        self.backFlatTunnelCurrents=t
        self.TV=TV
        
        
    def conditionHash(self):
        return  str(self.topRef) + '_' + str(self.tunnelMV) +  '_' + str(self.bttmRef)
        
    def ionicMV(self):
        return self.topRef-self.bttmRef
    
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
             self._drivingVoltages = np.asarray(self.fileData.segments[0].analogsignals[1])
             self._tunnelCurrents = self.fileData.segments[0].analogsignals[0]
             self._tunnelCurrents=self._tunnelCurrents[0:int(len(self._tunnelCurrents)/5)]
             self.samplingRate =float( self.fileData.segments[0].analogsignals[1].sampling_rate)
             if len(self.fileData.segments[0].analogsignals)>2:
                 self.hasIonic = True
                 self._ionicCurrents =np.asarray( self.fileData.segments[0].analogsignals[2])
             else:
                 self.hasIonic=False
                
    def _loadHeader(self):
        if self.fileType.lower() == '.abf':
             self.fileRef = io.AxonIO(filename=self.filePath)
             header=self.fileRef.read_header()
             self.fileDate = header['uFileStartDate']
             self.startTime= header['uFileStartTimeMS']
             s=float(self.fileDate)/100;
             s=long((s-int(s))*100)
             self.sortTime = s*1000000000+self.startTime

             
        
    def _parseFilename(self,fullFileName):
        pParts=fullFileName.split(dirSeperator)
        filename =(path_leaf(fullFileName).split(".")[0]) 
        sParts=filename.split('_')
        self.fileType=ntpath.splitext(fullFileName)[-1]
        self.isControl=not (not [x for x in pParts if ('control' in x.lower() or 'rinse' in x.lower())])
        
        self.isHumanGood= filename.lower().endswith('_g')
        if self.isHumanGood:
            sParts= [x for x in sParts if x is not 'g']

        top = [x for x in sParts if 'top' in x.lower()]
        if not (not top):
            self.topRef= self._makeNumber(top[0].lower().replace('top',''))
            sParts=[x for x in sParts if top[0] not in x]
        bttmRef = [x for x in sParts if 'bttm' in x.lower()]
        if not (not bttmRef):
            self.bttmRef= self._makeNumber(bttmRef[0].lower().replace('bttm',''))
            sParts=[x for x in sParts if bttmRef[0] not in x]
            
        concentration = [x for x in sParts if ( ('mm' in x.lower()) and (not 'mv' in x.lower()))]
        if (not concentration):
            concentration = [x for x in sParts if ( ('nm' in x.lower()) and (not 'mv' in x.lower()))]
            if not (not concentration):
                cParts=concentration[0].lower().split('nm')              
                self.concentrationMM= float(cParts[0]);
                self.analyte=cParts[1];
                sParts=[x for x in sParts if concentration[0] not in x]
        else:
            cParts=concentration[0].lower().split('mm')              
            self.concentrationMM=  float(cParts[0]);
            self.analyte=cParts[1];
            sParts=[x for x in sParts if concentration[0] not in x]
             
        tunnel = [x for x in sParts if 'mv' in x.lower()]
        if not (not top):
            self.tunnelMV= self._makeNumber(tunnel[0].lower())
            sParts=[x for x in sParts if tunnel[0] not in x]

    def __init__(self, fullPath, experiment):
        self._experiment=experiment
        self.filePath = fullPath
        self._isLoaded=False
        self.fileType=''
        self.isControl=False
        self.isHumanGood= False
        self.topRef=0
        self.bttmRef=0
        self.concentrationMM= 0
        self.analyte =''
        self.tunnelMV= 0
        self.hasIonic=False
        self.sortTime =0
        self._parseFilename(fullPath)
        self._loadHeader()