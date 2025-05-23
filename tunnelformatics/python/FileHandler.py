# -*- coding: utf-8 -*-
"""
Created on Fri Apr 08 17:13:06 2016

@author: bashc
"""
import os
import ntpath
import fnmatch
from DataTrace import DataTrace
from matplotlib import interactive
interactive(False)

dirSeperator='\\'

def path_leaf(path):
    head, tail = ntpath.split(path)
    return tail or ntpath.basename(head)
    
       
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
f=e.dataTraces[10]

f.calcStatistics()
