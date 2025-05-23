import sys
#sys.path.append("C:\\Development\\TunnelSurfer5\\TunnelSurfer5mySQL\\TunnelSurfer5\\bin\\Debug")
sys.path.append("C:\\Development\\TunnelSurfer5mySQL\\TunnelSurfer5\\bin\\Debug\\Python")

from IVCurves import tunnelIV,ionicIV

if __name__ == '__main__':
    print(sys.argv)
    if (len(sys.argv)>1):
        filePath = sys.argv[1]
        outFile = sys.argv[2]
        controlFile = sys.argv[3].strip()
        concentrationMM= float(sys.argv[4].strip())
        numPores= int(sys.argv[5].strip())        
    else :
        filePath =  "S:\\Research\\Stacked Junctions\\Results\\20160229_NALDB34_Chip03_22cyc_HIM3pC_UVozone10each_200deg_1mMPB_pH7_modified\\20160304_NALDB34_Chip03_22cyc_HIM3pC_M1_M2G_TunnelingIV_Air_3_Sweeprate_30mVpers_350mV"
        outFile ='c:\\temp\\testIV'
        controlFile=''
        concentrationMM=100
        numPores=3
    
    if "ionic" in filePath.lower():
        iv=ionicIV(filePath,outFile,controlFile,concentrationMM,numPores )
    else:
        iv=tunnelIV(filePath,outFile,controlFile,concentrationMM,numPores )
        
    
    stats=iv.calcStatistics();
#print(vars(stats))
#with open(outFile + "_stats.txt", "w") as text_file:
#    text_file.write(str(vars(stats)))
   