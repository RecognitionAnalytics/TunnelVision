# -*- coding: utf-8 -*-
"""
Created on Sat Apr 16 16:36:03 2016

@author: bashc
"""
import sys
sys.path.append("C:\\Development\\TunnelSurfer5mySQL\\TunnelSurfer5\\bin\\Debug\\Python")
import numpy as np
from scipy.interpolate import interp1d
import matplotlib.pyplot as plt
import quantities as pq #https://pypi.python.org/pypi/quantities
from scipy.odr import *
from DataTraces import *
import csv


class IVCurve(Trace):
       
    def conditionHash(self):
        return   str(self.isIonic)
        
    def calcStatistics(self):
        controlTrace=None
            
        I=self.Currents()
        V=self.Voltages()
        (Current,Volts,currentUpper,currentLower,self.stats.Conductance)= self._ExtractGoodSection(I,V,.75)
      
        self.stats.dvdt=np.mean(np.abs( np.diff(V)))/self.sampleRate
        self.stats.Current_Shift=(currentUpper+currentLower)/2
        self.stats.Capacitance=(np.abs(currentUpper-currentLower)/2)/self.stats.dvdt
        self.stats.Capacitance.units=pq.nF
        #%correct for parrallel parasitic capacitance and leakage
        if  (not controlTrace):
            pt=(5.37235092786435e-12,0)
            self.stats.Capacitance_Corrected=self.stats.Capacitance
        else:
            pt=(controlTrace.Conductance,0)
            self.stats.Capacitance_Corrected=self.stats.Capacitance-controlTrace.stats.Capacitance
            
        Current_Corrected=Current-np.polyval(pt,Volts)*Current.units;
        
        self._finishStats(Current,Current_Corrected,Volts)

        return Trace.calcStatistics(self)
    
    def _ExtractGoodSection(self,I,V,voltageCutoff=.5): #[I,V,cU,cL,slope]=
        In=I-np.mean(I)
        
        voltageLimit =.95*np.max(V)
        idx=[i for i,x in enumerate(V) if np.abs(x)<voltageLimit]
        Vn=V[idx]
        In=In[idx]

        idx=  [i for i,x in enumerate(np.diff(Vn)) if x>0]
        idx=  [x for x in idx if np.abs(Vn[x])<voltageCutoff*voltageLimit]
        Vu=Vn[idx]
        Iu=In[idx]
        pU=np.polyfit(Vu,Iu,1)
        
        idx=  [i for i,x in enumerate(np.diff(Vn)) if x<0]
        idx=  [x for x in idx if np.abs(Vn[x])<voltageCutoff*voltageLimit]
        Vl=Vn[idx]
        Il=In[idx]
        pL=np.polyfit(Vl,Il,1)
        
        
        Il=Il-pL[1]*Il.units
        
        cU=pU[1]*Il.units
        cL=pL[1]*Il.units
        slope=(pL[0]*I.units)/(1*V.units)
        slope.units=1/pq.ohm
        idx=np.argsort(Vl)
        Vn=Vl[idx]
        In=Il[idx]
        return In[2:-2],Vn[2:-2],cU,cL,slope
        
    def _abstractLoadFile(self):
        #Keithley output varies and must be parsed, and can vary in the number of lines in the header.  Weisi output is set, but no header
        dataGrid=[]
        header=[]
        lastRow=[]
        with open(self.filePath, 'rb') as csvfile:
            dialect = csv.Sniffer().sniff(csvfile.read(1024))
            csvfile.seek(0)
            spamreader = csv.reader(csvfile, dialect)
            colnum = 0
            for row in spamreader:
                if len(row)>2 and is_number(row[0]) and is_number(row[1]):
                    dataGrid.append(row)
                    colnum=max( (colnum , len(row)))
                    if not header:
                        if not ( not lastRow):
                            header=lastRow
                lastRow=row
        cols=[] 
        for i in range(0,colnum):
            col=np.zeros(len(dataGrid))
            cols.append(col)
        for i in range(0,len(dataGrid)):
            row = dataGrid[i]
            for j in range(0,len(row)):
                cols[j][i]=float(row[j])
        self.stats.traceTime = max(cols[0])-min(cols[0])
        self.sampleRate = np.mean( np.diff(cols[0])) *  pq.second
        self._drivingVoltage = cols[2] * pq.mV    
        self._current = cols[3] * pq.nanoamp
        self._isLoaded=True
        
        
    def __init__(self,fullPath,outFile,controlFile,concentrationMM,numPores ):
        Trace.__init__(self,fullPath,outFile,controlFile,concentrationMM,numPores)
        self.outFile =outFile
        self.Conductance =0
                

        
class tunnelIV(IVCurve):
    def conditionHash(self):
        return  self.stats.firstJunction + self.stats.secondJunction + '_tunneling'
        
    def _simmonsFormula2(self,p,V):#dang guy (simmons) changed units every other line.  This is just to get everything kosher.  It would make sense to optimize this to nice units before trying to use it in a curve fit
          sys.stdout.write('.\n')
          s=p[0]
          s=s*pq.nanometer
          phi0=p[1]
          phi0=phi0*pq.eV
          
          K=p[2]
          K=(K*4/(3*pq.nanometer)*s)
          
          area=p[3]
          area=area*(pq.nanometer)**2
          
          VV=V-p[4]
          Vn=( np.abs(VV)*pq.volt)*pq.constants.e 
          Vn.units=pq.eV
        
          beta=1
          L=((pq.constants.e **2)*np.log(2)/(8*np.pi*K* pq.constants.electric_constant *s))
          L.units=pq.eV
          
          s1=1.2*L*s/phi0
          s1.units=pq.nm
          s2=s*(1-9.2*L/(3*phi0+4*L-(2*Vn)  ))+s1
          s3=(phi0-5.6*L)*(s/Vn)
          idx=[i for i,x in enumerate(Vn) if x>phi0]
          s2[idx]=s3[idx]
          ds=s2-s1
          
          p2=Vn*(s2+s1)/(2*s)
          p2.units=pq.eV
          phiI=phi0-p2-(1.15*L*s/(s2-s1))*np.log(.00001+ np.abs( s2*(s-s1)/(.0000001*pq.nm**2 + np.abs( s1*(s-s2)))))
          
          J0=pq.constants.e /(2*np.pi*pq.constants.Planck_constant_in_eV_s*((beta*ds)**2))
          J0.units=pq.A/pq.eV/(pq.cm)**2
          
          A=4*np.pi*beta*ds*((2*pq.constants.electron_mass)**.5)/pq.constants.Planck_constant_in_eV_s
          A.units=1/(pq.eV)**.5
          
          I=np.sign(VV)* area*J0*(phiI*np.exp(-1*A*(phiI**.5)) - (phiI+Vn)*np.exp(-A*((phiI+Vn)**.5)))
          I.units=pq.nA
          return I+p[5]*pq.nA
          
    def GetCurveValue(self,Volt):
        v=Volt
        v.units=pq.volt
        if hasattr(self,'fitParam'):
            return self._simmonsFormula2(self.fitParam,(float(v),0))   
        else:
            return 0*pq.pA
        
    def _finishStats(self,Current,Current_Corrected,Volts):
        junctionLength = 4 * pq.micron
        junctionWidth = 80 * pq.nanometer
        gapSize=2 * pq.nanometer

        #%your best estimates.  it is set for Pd with Al2O3 gap
        potentialGap=3.02 * pq.electron_volt

        DielectricConstant=1 # %correction to alumina value (4e0/3nm) M.D. Groner , J.W. Elam , F.H. Fabreguette , S.M. George
        geoArea=junctionLength*junctionWidth
        geoArea.units=(pq.nm)**2
        
        x=(float(gapSize),float( potentialGap) ,float(DielectricConstant) ,float(geoArea) ,0 ,0)
        linear = Model(self._simmonsFormula2)
        mydata = Data(Volts/1000, Current_Corrected)
        myodr = ODR(mydata, linear, beta0=x,maxit =10)
#        print('running')
        myoutput = myodr.run()
#        myoutput.pprint()
        
        x2=myoutput.beta
        x2error=myoutput.sd_beta
        self.stats.calc_Gap=x2[0]*gapSize.units
        self.stats.calc_potentialGap=x2[1] * potentialGap.units
        self.stats.calc_dielectric =( x2[2]*x2[0]*4/3 )*(pq.constants.electric_constant.units)
        self.stats.calc_area =x2[3] * (pq.nm)**2
        self.stats.calc_voltsOffset=x2[4]*pq.volt
        self.stats.fitParam=x2
        print('-')
        print('Gap  = ' + str(self.stats.calc_Gap) + ' / ' + str(gapSize))
        print('eV   = ' + str(self.stats.calc_potentialGap) + ' / ~3.10 ')
        print('Ek   = ' + str(self.stats.calc_dielectric) + ' / ~3')
        jWidth =self.stats.calc_area/junctionLength
        jWidth.units = pq.nm
        print('width = ' + str(jWidth) + ' / ' + str(junctionWidth))
                
        simI=self._simmonsFormula2(x2,np.asarray(Volts/1000))

        
        self.fitParam=x2
        
        self.SaveBinary(self.outFile + "_Voltage.bin",np.asarray(Volts/1000))
        self.SaveBinary(self.outFile + "_Raw.bin",Current)
        self.SaveBinary(self.outFile + "_CorrectedVolts.bin",np.asarray(Volts*1000))
        self.SaveBinary(self.outFile + "_Corrected.bin",Current_Corrected)
        self.SaveBinary(self.outFile + "_CurveFit.bin",simI)
        self.SaveStats(self.outFile + "_header.txt")
        
        
    def __init__(self,fullPath,outFile,controlFile,concentrationMM,numPores ):
        IVCurve.__init__(self,fullPath,outFile,controlFile,concentrationMM,numPores )
    

class ionicIV(IVCurve):
    def conditionHash(self):
        return  'ionic'
        
    def _SimulateIonic(self,Xs,V0):
        sys.stdout.write('.')
        C=Xs[0]*1e-9
        R1=(Xs[1])*1e9
        R2=(Xs[2])*1e9
        Rt=R1+R2
        V=V0+Xs[3]
        Q=0
        step=1
        dt=float(self.sampleRate)/step
        iC=np.zeros(len(V))
        for I in range(0,len(iC)):
            dqdt=(Q/C-V[I]*R2/Rt)/R2/(R2/Rt-1)
            Q=Q+dqdt*dt
            iC[I]=((R2*dqdt+V[I])/Rt) #manual convert to nA
           
        return (iC)*1e9+Xs[4]
          
    def _IonicPoreRadiusSmeet(self,Gc,diameter,poreLength,KSigma,ionicStrength,relMobility):
        poreLength=poreLength*30.0/50.0
#        sigma=1.5*interp1([-6 -5 -3 -1 1],[1 2 9 25 25],log(ionicStrength)/log(10),'spline')*1000
        sigma=1/1000*pq.coulomb/(pq.meter**2)* interp1d([-6, -5, -3 ,-1 ,1],[1, 2, 9, 25, 28])(np.log10(float(ionicStrength)) )
        
        A=np.pi/(4*poreLength)
        B=KSigma*float(ionicStrength)
        uk=(7.616e-8)*(pq.meter**2)/pq.volt/pq.second
        C=4*sigma*uk*relMobility
        conductance=A*diameter**2*(B+C/diameter)
        conductance.units=pq.pS
        
        Gc.units=pq.pS
        
        aa=A*B
        bb=A*C
        cc=-Gc
        x=np.zeros(2)
        x[0]=((-1*bb+(bb*bb-4*cc*aa)**.5)/2/aa).simplified
        x[1]=((-1*bb-(bb*bb-4*cc*aa)**.5)/2/aa).simplified
        
        x=[i for i in x if i>0 and np.isreal(i)]
        diameter=np.min(np.real(x))*1e9*pq.nm
        return (diameter, conductance)
          
    def GetCurveValue(self,Volt):
        return float(Volt)*self.stats.Conductance  
        
    def _IonicPoreRadiusAccess(self,Gc,poreLength,KSigma,ionicStrength):
        poreLength=poreLength/3
        aa=4*poreLength/np.pi
        bb=1
        cc=(-1*KSigma*float(ionicStrength)/Gc).simplified
        x=np.zeros(2)
        x[0]=((-1*bb+(bb*bb-4*cc*aa)**.5)/2/aa).simplified
        x[1]=((-1*bb-(bb*bb-4*cc*aa)**.5)/2/aa).simplified
        x=1/x
        x=[i for i in x if i>0 and np.isreal(i)]
        diameter=np.min(np.real(x))*1e9*pq.nm
        return diameter

        
    def _calcPores(self,KSigma,IonicConcentration, EstPoreLength,EstPoreDiameter,relativeMobility):
        number_of_pores=self.stats.number_of_pores
        self.stats.Theory_Pore_Conduct_Access= number_of_pores* KSigma*float(IonicConcentration)/(4*EstPoreLength/np.pi/ EstPoreDiameter**2+1/EstPoreDiameter)
        self.stats.Theory_Pore_Conduct_Access.units=pq.pS
        
        self.stats.Calculated_Access_PoreSize=self._IonicPoreRadiusAccess(self.stats.Conductance/number_of_pores,EstPoreLength,KSigma,IonicConcentration)
        self.stats.Calculated_Access_PoreSize_Model=self._IonicPoreRadiusAccess(self.stats.model_PoreConductance/number_of_pores,EstPoreLength,KSigma,IonicConcentration)
        
        (self.stats.Calculated_Smeet_PoreSize, self.stats.Theory_SmeetConductance)=self._IonicPoreRadiusSmeet((self.stats.Conductance-.2*pq.pS)/number_of_pores,EstPoreDiameter,EstPoreLength,KSigma,IonicConcentration,relativeMobility)
        (self.stats.Calculated_Smeet_PoreSize_Model, self.stats.Theory_SmeetConductance_Model)=self._IonicPoreRadiusSmeet((self.stats.model_PoreConductance-.2*pq.pS) /number_of_pores,EstPoreDiameter,EstPoreLength,KSigma,IonicConcentration,relativeMobility)
        print('-')
        print('Pore Smeet    = ' + str(self.stats.Calculated_Smeet_PoreSize_Model))
        print('Pore Access   = ' + str(self.stats.Calculated_Access_PoreSize_Model))

        
    def _fitCurve(self,V,I):
        CapT=self.stats.Capacitance_Corrected
        CapT.units=pq.farad
        t=np.asarray([float( CapT)*1e9,.25,float(self.stats.Estimated_Pore_Resistance_Slope)/1e9 ,0,0])

        idx=[i for i,x in enumerate(np.diff(V)) if x <0]
        idx=range(0,(idx[0]-3))        
        
        linear = Model(self._SimulateIonic)
        mydata = Data(V[idx],I[idx])
        myodr = ODR(mydata, linear, beta0=t,maxit =10)
        myoutput = myodr.run()
#        myoutput.pprint()
        
        x2=myoutput.beta
#        x2error=myoutput.sd_beta
        
        self.stats.model_Capacitance=x2[0]*pq.nF
        self.stats.model_SolutionResistance=x2[1]*1e9 *pq.ohm
        self.stats.model_PoreConductance = 1/(x2[2]*1e9 *pq.ohm)
        self.stats.model_VoltageOffset =x2[3]*pq.volt
        self.stats.model_CurrentOffset =x2[4]*pq.nA
        self.stats.model_VoltageOffset.units=pq.mV
        self.fitParam=x2
        
    def _finishStats(self,Current,Current_Corrected,Volts):
        
        EstPoreLength=50*pq.nm #1e-9; %m
        EstPoreDiameter=10*pq.nm # *1e-9; %m
        
        self.stats.concentrationMM=self.stats.concentrationMM*pq.mM
        IonicConcentration=self.stats.concentrationMM.copy() #; %mM
        IonicConcentration.units=pq.molar
        
        KSigma=10.5*1e12*pq.siemens/pq.meter #quantities library gets ths wrong and returns picosiemens
        relativeMobility=1
        if hasattr(self.stats, 'analyte'):
            if self.stats.analyte.lower()=='pb':
                KSigma=9.5*1e12 *pq.siemens/pq.meter #; % S/m % pb buffer 1M
                relativeMobility=0.450 #; %#ok<NASGU> %PB phosphate #%http://web.med.unsw.edu.au/phbsoft/mobility_listings.htm
        else:
            self.stats.analyte='prob pb'
              
        if hasattr(self.stats, 'number_of_pores')==False:
            self.stats.number_of_pores   =1     
        
        Expected_Solution_Resistance=1/(  KSigma*float(IonicConcentration)*( np.pi*(1*pq.cm/2)**2)/(1*pq.cm))
        Expected_Solution_Resistance.units=pq.ohm
#        CapT*1e9,IVparam.Est_Solution_Resistance*1e-9,(1/Conductance)*1e-9
        Estimated_Pore_Resistance_Slope = 1/self.stats.Conductance
        Estimated_Pore_Resistance_Slope.units=pq.ohm
        self.stats.Estimated_Pore_Resistance_Slope=Estimated_Pore_Resistance_Slope
        
        V=self.Voltages()
        V.units=pq.volt
        I=self.Currents()
        I.units=pq.nA
        
        print(self.stats.fileName)
        self._fitCurve(V,I)
        self._calcPores(KSigma,IonicConcentration, EstPoreLength,EstPoreDiameter,relativeMobility)
            
        simI=self._SimulateIonic(self.fitParam,np.asarray(V))
        
        I=np.asarray(I)
        simI=np.asarray(simI)
        self.stats.FitError= np.sum( np.abs(simI-I))
        idxp=[i for i,x in enumerate(V) if x>0]
        idxn=[i for i,x in enumerate(V) if x<0]
        self.stats.CurveSymmetry = ( np.sum( np.abs(simI[idxp]-I[idxp]))- np.sum( np.abs(simI[idxn]-I[idxn]))) /self.stats.FitError
        
        self.SaveBinary(self.outFile + "_Voltage.bin",np.asarray(V*1000))
        self.SaveBinary(self.outFile + "_Raw.bin",I)
        self.SaveBinary(self.outFile + "_CorrectedVolts.bin",np.asarray(Volts*1000))
        self.SaveBinary(self.outFile + "_Corrected.bin",Current_Corrected)
        self.SaveBinary(self.outFile + "_CurveFit.bin",simI)
        self.SaveStats(self.outFile + "_header.txt")
        
        
    def __init__(self,fullPath,outFile,controlFile,concentrationMM,numPores ):
        IVCurve.__init__(self,fullPath,outFile,controlFile,concentrationMM,numPores )