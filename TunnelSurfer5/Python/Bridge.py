import sys
import numpy as np
import struct
sys.path.append("C:\\Development\\TunnelSurfer5mySQL\\TunnelSurfer5\\bin\\Debug\\Python")

from DataTraces import DataTrace

print(sys.argv)
if (len(sys.argv)>1):
    filePath = sys.argv[1]
    outFile = sys.argv[2]
    controlFile = sys.argv[3].strip()
    concentrationMM= float(sys.argv[4].strip())
    numPores= int(sys.argv[5].strip())
else :
    filePath =  r"S:\Research\TunnelSurfer\Experiments\DNA\ResistorTest\Chip_01\M1_M4\New Trace_00031922.bDAQ|M4_M2"
    outFile =r"S:\Research\TunnelSurfer\Experiments\DNA\ResistorTest\Chip_01\M1_M4\New Trace_00031922\_1923_M4_M2"
    controlFile='' 
    concentrationMM=.05
    numPores=1


currentTrace = DataTrace(filePath, outFile,controlFile,concentrationMM,numPores )
stats=currentTrace.calcStatistics();

  
    
#print(vars(stats))
#with open(outFile + "_stats.txt", "w") as text_file:
#    text_file.write(str(vars(stats)))
   