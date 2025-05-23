# -*- coding: utf-8 -*-
"""
Created on Wed May 04 22:57:54 2016

@author: bashc
"""
import sys
sys.path.append("C:\\Development\\TunnelSurfer5mySQL\\TunnelSurfer5\\bin\\Debug\\Python")
from neo import io # download from https://pythonhosted.org/neo/io.html
import numpy as np
import sys
import struct
import cPickle

print(sys.argv)

#filePath = 'S:\\Research\\Stacked Junctions\\Results\\20160224_NALDB34_Chip02_22cyc_HIM3pC_UVozone10each_200deg_PB_Modification\\For Brain analysis\\01_PolyA50Biotin\\M1_M3G_TopG_Bttm0mV_P350mV_100nMPolyA50Biotin_0001.abf' #sys.argv[2]
#outFile = 'C:\\temp\\control00' #sys.argv[3]

filePath = sys.argv[1]
outFile = sys.argv[2]
print(filePath)
print(outFile)

fileRef = io.AxonIO(filename=filePath)
header=fileRef.read_header()
try:
    cPickle.dump( header, open( outFile+ "Header.p", "wb" ) )
except  ValueError:
    print("error")    
fileDate = header['uFileStartDate']
startTime= header['uFileStartTimeMS']
s=float(fileDate)/100;
s=long((s-int(s))*100)
sortTime = s*1000000000+startTime
fileDate = fileDate
hr=int( startTime/1000/60/60)
mn=int(startTime/1000/60 - hr*60)
sec=int( startTime/1000 - hr*60*60-mn*60)
startTime = str(hr) + ':' + str(mn) + '::' + str(sec)
 
fileRef = io.AxonIO(filename=filePath)
fileData = fileRef.read_block(lazy=False, cascade=True)
_drivingVoltage = fileData.segments[0].analogsignals[1]

m=np.mean(_drivingVoltage)
if m>1:
    _drivingVoltage =_drivingVoltage /10

if np.std(_drivingVoltage)<1:
    _drivingVoltage =(m,m)
 
_current = fileData.segments[0].analogsignals[0]

samplingRate =float( fileData.segments[0].analogsignals[1].sampling_rate)
if len(fileData.segments[0].analogsignals)>2:
  hasIonic = True
  _ionicCurrents =np.asarray( fileData.segments[0].analogsignals[2])
else:
  hasIonic=False
  meanVoltage = np.mean( _drivingVoltage )
  
print("save header")
with open(outFile + "_header.txt", "w") as text_file:
    text_file.write("SampleRate= %s \n" % samplingRate)
    text_file.write("SortTime= %s \n" % sortTime)
    text_file.write("fileDate= %s \n" % fileDate)
    text_file.write("startTime= %s \n" % startTime)
    text_file.write("tunnelVoltage= %s \n" % m)
    text_file.write("hasIonic= %s \n" % hasIonic)
    text_file.write("nPoints= %s \n" % len(_current))
    text_file.write("nVPoints= %s \n" % len(_drivingVoltage))
    
print("save raw")    
out_file = open(outFile + "_Raw.bin","wb")
l= [1, len(_current)]
s=struct.pack('i'*2,*l)
s = struct.pack('d'*len(_current), *_current)
out_file.write(s)
out_file.close()

print("save voltage")
out_file = open(outFile + "_Voltage.bin","wb")
l= [1, len(_drivingVoltage)]
s=struct.pack('i'*2,*l)
s = struct.pack('d'*len(_drivingVoltage), *_drivingVoltage)
out_file.write(s)
out_file.close()


if hasIonic:
    print("ionic")
    out_file = open(outFile + "_Ionic.bin","wb")
    l= [1, len(_ionicCurrents)]
    s=struct.pack('i'*2,*l)
    s = struct.pack('d'*len(_ionicCurrents), *_ionicCurrents)
    out_file.write(s)
    out_file.close()
