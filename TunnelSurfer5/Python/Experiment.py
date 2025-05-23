# -*- coding: utf-8 -*-
"""
Created on Fri Apr 08 17:13:06 2016

@author: bashc
"""
#need to install quantities and neo.io
#pip install quantities
#pip install neo
#


import numpy as np
import quantities as pq #https://pypi.python.org/pypi/quantities
from neo import io # download from https://pythonhosted.org/neo/io.html
import os
import sys
import fnmatch
from DataTraces import DataTrace
from IVCurves import tunnelIV,ionicIV
#from pprint import pprint
from matplotlib import interactive
interactive(False)
        
dirSeperator=os.path.sep
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
        self.afterExperimentIVs=[]
        self.beforeDrillingIVs=[]
        self.beforeIVs=[]
        if not (not self.afterExperimentIVFolder):
            for root, dirnames, filenames in os.walk(self.afterExperimentIVFolder):
                    for filename in filenames:
                        if 'ion' in filename.lower():
                            self.afterExperimentIVs.append(ionicIV(root + dirSeperator + filename, self)) 
                        else:
                            if 'tunnel' in filename.lower():
                                self.afterExperimentIVs.append(tunnelIV(root + dirSeperator + filename, self))    

        if not (not self.beforeDrillingIVFolder):
            for root, dirnames, filenames in os.walk(self.beforeDrillingIVFolder):
                    for filename in filenames:
                        if 'ion' in filename.lower():
                            self.beforeDrillingIVs.append(ionicIV(root + dirSeperator + filename, self)) 
                        else:
                            if 'tunnel' in filename.lower():
                                self.beforeDrillingIVs.append(tunnelIV(root + dirSeperator + filename, self))     
                                
        if not (not self.beforeExperimentIVFolder):
            for root, dirnames, filenames in os.walk(self.beforeExperimentIVFolder):
                    for filename in filenames:
                        if 'ion' in filename.lower():
                            self.beforeIVs.append(ionicIV(root + dirSeperator + filename, self)) 
                        else:
                            if 'tunnel' in filename.lower():
                                self.beforeIVs.append(tunnelIV(root + dirSeperator + filename, self))     
                                
        print(len(self.beforeIVs))
        
                
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
                
    def getTunnelIVStats(self,testTrace):
        junction = testTrace.junctionHash()
        pos = [x for x in self.beforeIVs if ( x.isIonic==False and x.junctionHash().lower()==junction.lower())]
        if not (not pos):
            return pos[0]
        else:
            pos=[x for x in self.beforeIVs if ( x.isIonic==False and x.isControl==False)]
            if not (not pos):
                return pos[0]
            else:
                return None
        
    def getNearestIVControl(self, isIonic):
        pos = [x for x in self.beforeIVs if ( x.isIonic and x.isControl)]
        if not (not pos):
            return pos[0]
        else:
            return None

    def formatTable(self,colNames, traces):
        table=''
        for i in range(0,len(traces)):
            stats=traces[i].stats
            temps=vars(stats)
            vals=[]
            for x in colNames:
                if x in temps.keys():
                    vals.append (temps[x])
                else:
                    vals.append('')
            table=table + ','.join('{}'.format(k) for k in vals)    + '\n'    
        return table   
        
    def runStatistics(self,outputFolder):
        tableColsIV=set([])
        if not (not self.beforeIVs):
            for i in range(0,len( self.beforeIVs)):        
                self.beforeIVs[i].calcStatistics()
                for t in vars(self.beforeIVs[i].stats).keys():
                    tableColsIV.add(t)
                
        if not (not self.beforeDrillingIVs):
            for i in range(0,len( self.beforeDrillingIVs)):        
                self.beforeDrillingIVs[i].calcStatistics()
                for t in vars(self.beforeDrillingIVs[i].stats).keys():
                    tableColsIV.add(t)
            
        if not (not self.afterExperimentIVs):
            for i in range(0,len( self.afterExperimentIVs)):        
                self.afterExperimentIVs[i].calcStatistics()            
                for t in vars(self.afterExperimentIVs[i].stats).keys():
                    tableColsIV.add(t)

        tableIV= ','.join('{}'.format(k) for k in tableColsIV)  + '\n'
        tableIV=tableIV+self.formatTable(tableColsIV,self.beforeIVs)  +                 self.formatTable(tableColsIV,self.beforeDrillingIVs)  +                 self.formatTable(tableColsIV,self.afterExperimentIVs)   
#        
        lambdas=[]
        for i in range(0,len( self.dataTraces)):
            if self.dataTraces[i].isControl==False:
                sys.stdout.write('.')
                tvlambda = self.dataTraces[i].traceTVLambda()
                lambdas.append(tvlambda)
                sys.stdout.write('.')
                self.dataTraces[i].stats.TV_lambda = tvlambda
                
                self.dataTraces[i].clearMemory()
                
              
        self.experimentLambda= np.mean(np.asarray(lambdas))*1e-3
        tableCols=set([])
        for i in range(0,len( self.dataTraces)):
#            print('Statistics for:' + str(i))
            sys.stdout.write('.')
            stats=self.dataTraces[i].calcStatistics()
            for t in vars(stats).keys():
                    tableCols.add(t)
            self.dataTraces[i].saveABF('S:\\Research\\BrianAnalysis\\GitAnalysis\\' + self.dataTraces[i].fileName)                    
            self.dataTraces[i].clearMemory()
            
        table= ','.join('{}'.format(k) for k in tableCols)  + '\n'
        table=table+self.formatTable(tableCols,self.dataTraces)  
        
        print(table)    
        
        with open(outputFolder + dirSeperator + 'experimentStats.csv', 'w') as csvfile:
            csvfile.write(tableIV + '\n\n')
            csvfile.write(table)
        
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
        
if __name__ == '__main__':

    #directoryPath= 'S:\\Research\\Stacked Junctions\\Results\\20160229_NALDB34_Chip03_22cyc_HIM3pC_UVozone10each_200deg_1mMPB_pH7_modified\\For Brain analysis\\M1-M2_2'
    directoryPath= 'S:\\Research\\Stacked Junctions\\Results\\20160224_NALDB34_Chip02_22cyc_HIM3pC_UVozone10each_200deg_PB_Modification\\For Brain analysis'
    print('Loading:\n'+directoryPath)
    e=Experiment(directoryPath)
    outputTable=e.runStatistics('S:\\Research\\BrianAnalysis\\GitAnalysis')

#print(f.calcStatistics())
