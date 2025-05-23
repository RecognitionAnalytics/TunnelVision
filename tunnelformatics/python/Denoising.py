# -*- coding: utf-8 -*-
"""
Created on Sat Apr 09 19:01:55 2016

@author: bashc
"""
import numpy as np
from math import floor
import scipy
import sys
from scipy import fft, ifft
from scipy.signal import medfilt
from skimage.restoration import denoise_tv_bregman, denoise_bilateral
from scipy import sparse
from scipy.sparse import linalg


#%               RECURSIVE DOUBLE CUSUM ALGORITHM
#% (Detection of abrupt changes in the mean ; optimal for Gaussian signals)
#%
#%           Inputs :
#%             x    : signal samples
#%             delta: most likely jump to be detected
#%             h    : threshold for the detection test
#%
#%           Outputs :
#%             mc   : piecewise constant segmented signal
#%             kd   : detection times (in samples)
#%             krmv : estimated change times (in samples)

def cusum(x,delta,h):
#    %% Algo initialization
    Nd=0 #;           %detection number
    kd=np.zeros(len(x))#;   %detection time (in samples)
    N=len(x)
    krmv=np.zeros(len(x))#; %estimated change time (in samples)
    k0=0#;        %initial sample
    k=1#;         %current sample
    m=np.zeros(N)
    v=np.zeros(N)
    sp=np.zeros(N)
    Sp=np.zeros(N)
    gp= np.zeros(N)
    sn=np.zeros(N)
    Sn=np.zeros(N)
    gn=np.zeros(N)
    
    m[0]=x[0]
    m[k0]=x[k0]#; %mean value estimation
    v[k0]=0#;     %variance estimation
    sp[k0]=0#; %instantaneous log-likelihood ratio for positive jumps
    Sp[k0]=0#; %cumulated sum for positive jumps
    gp[k0]=0#; %decision function for positive jumps
    sn[k0]=0#; %instantaneous log-likelihood ratio for negative jumps
    Sn[k0]=0#; %cumulated sum for negative jumps
    gn[k0]=0#; %decision function for negative jumps
    
#    %% Global Loop
    sums=x[0]
    counts=1
    while k<len(x):
        if k % 10000==0:
            sys.stdout.write('.' + str(np.round(k,2)) + '.')
#        %current sample
        
#        %mean and variance estimation (from initial to current sample)
        sums=sums+x[k]
        counts=counts+1
        m[k]=sums/counts
        if (k-k0)>2000:
            v[k]=np.var(x[(k-2000):k])
        else:
            v[k]=np.var(x[k0:k])
            
        if v[k]==0:
            v[k]=70
            
#        %instantaneous log-likelihood ratios
        sp[k]=delta/v[k]*(x[k]-m[k]-delta/2)
        sn[k]=-delta/v[k]*(x[k]-m[k]+delta/2)
#        %cumulated sums
        Sp[k]=Sp[k-1]+sp[k]
        Sn[k]=Sn[k-1]+sn[k]
#        %decision functions
        gp[k]=np.max((gp[k-1]+sp[k],0))
        gn[k]=np.max((gn[k-1]+sn[k],0))
#        %abrupt change detection test
        if (gp[k]>h or gn[k]>h):
#            %detection number and detection time update
            Nd=Nd+1
            kd[Nd]=k
#            %change time estimation
            kmin=np.argmin(Sn[k0:k])
            krmv[Nd]=kmin+k0
            if gp[k]>h:
                kmin=np.argmin(Sp[k0:k])
                krmv[Nd]=kmin+k0
#            %algorithm reinitialization
            k0=k
            m[k0]=x[k0]
            v[k0]=0
            sp[k0]=0
            Sp[k0]=0
            gp[k0]=0
            sn[k0]=0
            Sn[k0]=0
            gn[k0]=0
            sums=0
            counts=0
        k=k+1            
        
#    %% Piecewise constant segmented signal
    k=k-1
    
    if Nd==0:
        mc=np.mean(x)*np.ones(k)
    else:
      if Nd==1:
        mc=m[krmv[0]]*np.ones(krmv[0]) #% m(k)*ones(1,k-krmv(1))]
      else:
        mc=np.zeros(k)
        mc[0:krmv[0]]=m[krmv[0]]
        cc=krmv[0]+1
        for ii in range(1,Nd):
            cc2=cc+(krmv[ii]-krmv[ii-1])
            mc[cc:cc2]= m[krmv[ii]] 
            cc=cc2+1
        mc[cc:]= m[k]
    idx=[i for i,xx in enumerate(mc) if xx==0]
    if not (not idx):
        if idx[-1]==len(mc):
            mc[idx[0:-2]]=mc[np.asarray(idx[0:-2])+1]
            mc[-1]=mc[0]
        else:
            mc[idx]=mc[np.asarray(idx)+1]
    krmv=    krmv[0:(Nd+1)].astype(int)
    return mc,m[krmv],krmv

def tvdiplmax(y):
    """Calculate the value of lambda so that if lambda >= lambdamax, the TVD
    functional solved by TVDIP is minimized by the trivial constant solution
    x = mean(y). This can then be used to determine a useful range of values
    of lambda, for example.
    Args:
        y: Original signal to denoise, size N x 1.
    Returns:
        lambdamax: Value of lambda at which x = mean(y) is the output of the
            TVDIP function.
    """

    N = y.size
    M = N - 1

    # Construct sparse operator matrices
    I1 = sparse.eye(M)
    O1 = sparse.dia_matrix((M, 1))
    D = sparse.hstack([I1, O1]) - sparse.hstack([O1, I1])

    DDT = D.dot(D.conj().T)
    Dy = D.dot(y)

    lambdamax = np.absolute(linalg.spsolve(DDT, Dy)).max(0)

    return lambdamax


def tvdip(y, lambdas, display=1, stoptol=1e-3, maxiter=60,z=None):
    """Performs discrete total variation denoising (TVD) using a primal-dual
    interior-point solver. It minimizes the following discrete functional:
    E=(1/2)||y-x||_2^2+lambda*||Dx||_1
    over the variable x, given the input signal y, according to each value of
    the regularization parametero lambda > 0. D is the first difference matrix.
    Uses hot-restarts from each value of lambda to speed up convergence for
    subsequent values: best use of the feature is made by ensuring that the
    chosen lambda values are close to each other.
    Args:
        y: Original signal to denoise, size N x 1.
        lambdas: A vector of positive regularization parameters, size L x 1.
            TVD will be applied to each value in the vector.
        display: (Optional) Set to 0 to turn off progress display, 1 to turn
            on. Defaults to 1.
        stoptol: (Optional) Precision as determined by duality gap tolerance,
            if not specified defaults to 1e-3.
        maxiter: (Optional) Maximum interior-point iterations, if not specified
            defaults to 60.
    Returns:
        x: Denoised output signal for each value of lambda, size N x L.
        E: Objective functional at minimum for each lamvda, size L x 1.
        s: Optimization result, 1 = solved, 0 = maximum iterations
            exceeded before reaching duality gap tolerance, size L x 1.
        lambdamax: Maximum value of lambda for the given y. If
            lambda >= lambdamax, the output is the trivial constant solution
            x = mean(y).
    Example:
        >>> import numpy as np
        >>> import tvdip as tv
        >>> # Find the value of lambda greater than which the TVD solution is
        >>> # just the mean.
        >>> lmax = tv.tvdiplmax(y)
        >>> # Perform TV denoising for lambda across a range of values up to a
        >>> # small fraction of the maximum found above.
        >>> lratio = np.array([1e-4, 1e-3, 1e-2, 1e-1])
        >>> x, E, status, l_max = tv.tvdip(y, lmax*lratio, True, 1e-3)
        >>> plot(x[:,0])
    """

    # Search tuning parameters
    ALPHA = 0.01    # Backtracking linesearch parameter (0,0.5]
    BETA = 0.5      # Backtracking linesearch parameter (0,1)
    MAXLSITER = 20  # Max iterations of backtracking linesearch
    MU = 2          # t update

    N = y.size  # Length of input signal y
    M = N - 1   # Size of Dx

    # Construct sparse operator matrices
    I1 = sparse.eye(M)
    O1 = sparse.dia_matrix((M, 1))
    D = sparse.hstack([I1, O1]) - sparse.hstack([O1, I1])

    DDT = D.dot(D.conj().T)
    Dy = D.dot(y)

    # Find max value of lambda
    lambdamax = (np.absolute(linalg.spsolve(DDT, Dy))).max(0)

    if display:
        print "lambda_max=%5.2e" % lambdamax

    x = np.zeros(N)
    E = 0

    # Optimization variables set up once at the start
#    if not z:
#        z = np.zeros(M)
    mu1 = np.ones(M)
    mu2 = np.ones(M)

    # Work through each value of lambda, with hot-restart on optimization
    # variables
#    for idx, l in enumerate(lambdas):
    l=lambdas
    t = 1e-10
    step = np.inf
    f1 = z - l
    f2 = -z - l

    # Main optimization loop
    s= 1
    if display:
        print "Solving for lambda={0:5.2e}, lambda/lambda_max={1:5.2e}".format(l, l/lambdamax)
        print "Iter# primal    Dual    Gap"

    for iters in xrange(maxiter):
        if iters % 10==0:
            sys.stdout.write('.')
        DTz = (z.conj().T * D).conj().T
        DDTz = D.dot(DTz)
       
        w = Dy - (mu1 - mu2)

        # Calculate objectives and primal-dual gap
        pobj1 = 0.5*w.conj().T.dot(linalg.spsolve(DDT,w))+l*(np.sum(mu1+mu2))
        pobj2 = 0.5*DTz.conj().T.dot(DTz)+l*np.sum(np.absolute(Dy-DDTz))
        pobj = np.minimum(pobj1, pobj2)
        dobj = -0.5*DTz.conj().T.dot(DTz) + Dy.conj().T.dot(z)
        gap = pobj - dobj
        if display:
            print "{:5d} {:7.2e} {:7.2e} {:7.2e}".format(iters, pobj, dobj,gap)

        # Test duality gap stopping criterion
        if gap <= stoptol:
            s = 1
            break

        if step >= 0.2:
            t = np.maximum(2*M*MU/gap, 1.2*t)

        # Do Newton step
        rz = DDTz - w
        Sdata = (mu1/f1 + mu2/f2)
        S = DDT-sparse.csc_matrix((Sdata.reshape(Sdata.size),
                                  (np.arange(M), np.arange(M))))
        r = -DDTz + Dy + (1/t)/f1 - (1/t)/f2
        dz = linalg.spsolve(S, r)
        dmu1 = -(mu1+((1/t)+dz*mu1)/f1)
        dmu2 = -(mu2+((1/t)-dz*mu2)/f2)

        resDual = rz.copy()
        resCent = np.vstack((-mu1*f1-1/t, -mu2*f2-1/t))
        residual = np.vstack((resDual, resCent))

        # Perform backtracking linesearch
        negIdx1 = dmu1 < 0
        negIdx2 = dmu2 < 0
        step = 1
        if np.any(negIdx1):
            step = np.minimum(step,
                              0.99*(-mu1[negIdx1]/dmu1[negIdx1]).min(0))
        if np.any(negIdx2):
            step = np.minimum(step,
                              0.99*(-mu2[negIdx2]/dmu2[negIdx2]).min(0))

        for _ in xrange(MAXLSITER):
            newz = z + step*dz
            newmu1 = mu1 + step*dmu1
            newmu2 = mu2 + step*dmu2
            newf1 = newz - l
            newf2 = -newz - l

            # Update residuals
            newResDual = DDT.dot(newz) - Dy + newmu1 - newmu2
            newResCent = np.vstack((-newmu1*newf1-1/t, -newmu2*newf2-1/t))
            newResidual = np.vstack((newResDual, newResCent))

            if (np.maximum(newf1.max(0), newf2.max(0)) < 0
                and (scipy.linalg.norm(newResidual) <=
                    (1-ALPHA*step)*scipy.linalg.norm(residual))):
                break

            step = BETA * step

        # Update primal and dual optimization parameters
        z = newz
        mu1 = newmu1
        mu2 = newmu2
        f1 = newf1
        f2 = newf2

        x = (y-D.conj().T.dot(z))#.reshape(x.shape[0])
        
        E = 0.5*np.sum((y-x)**2)+l*np.sum(np.absolute(D.dot(x)))

        # We may have a close solution that does not satisfy the duality gap
        if iters >= maxiter:
            s = 0

    if display:
        if s:
            print("Solved to precision of duality gap %5.2e") % gap
        else:
            print("Max iterations exceeded - solution may be inaccurate")

    return x, E, s, lambdamax
    
def Median_smooth(x, kSize):
    return medfilt(x,kSize)

def TV_smooth(x, weight):
    x2=np.matlib.repmat(x, 2, 1)
    mn=np.mean(x)
    x2=x2-mn
    m= np.max( np.abs( x2))
    x2=x2/m
    x2= denoise_tv_bregman(x2, weight=weight)[1,:]*m+mn
    return x2
#   
    
def Bilateral_smooth(x, sigma_range,spatial):
    x2=np.matlib.repmat(x, 2, 1)
    mn=np.mean(x)
    x2=x2-mn
    m= np.max( np.abs( x2))
    x2=x2/m
    x2=  denoise_bilateral(x2, sigma_range=sigma_range, sigma_spatial=spatial)[1,:]*m+mn
    return x2

def smooth(x,window_len=11,window='hanning'):
    """smooth the data using a window with requested size.
    
    This method is based on the convolution of a scaled window with the signal.
    The signal is prepared by introducing reflected copies of the signal 
    (with the window size) in both ends so that transient parts are minimized
    in the begining and end part of the output signal.
    
    input:
        x: the input signal 
        window_len: the dimension of the smoothing window; should be an odd integer
        window: the type of window from 'flat', 'hanning', 'hamming', 'bartlett', 'blackman'
            flat window will produce a moving average smoothing.

    output:
        the smoothed signal
        
    example:

    t=linspace(-2,2,0.1)
    x=sin(t)+randn(len(t))*0.1
    y=smooth(x)
    
    see also: 
    
    numpy.hanning, numpy.hamming, numpy.bartlett, numpy.blackman, numpy.convolve
    scipy.signal.lfilter
 
    TODO: the window parameter could be the window itself if an array instead of a string
    NOTE: length(output) != length(input), to correct this: return y[(window_len/2-1):-(window_len/2)] instead of just y.
    """ 
     
    if x.ndim != 1:
        raise ValueError, "smooth only accepts 1 dimension arrays."

    if x.size < window_len:
        raise ValueError, "Input vector needs to be bigger than window size."
        

    if window_len<3:
        return x
    
    
    if not window in ['flat', 'hanning', 'hamming', 'bartlett', 'blackman']:
        raise ValueError, "Window is on of 'flat', 'hanning', 'hamming', 'bartlett', 'blackman'"
    

    s=np.r_[x[window_len-1:0:-1],x,x[-1:-window_len:-1]]
    #print(len(s))
    if window == 'flat': #moving average
        w=np.ones(window_len,'d')
    else:
        w=eval('np.'+window+'(window_len)')
    
    y=np.convolve(w/w.sum(),s,mode='valid')
    gap=int((len(y)-len(x)-1)/2)
    return y[gap:(gap+len(x))]
    
    
def WienerScalart(signal,fs,N0,SP=.4,ControlWeight=1, alpha=.99,NoiseMargin=25):

    ## output=WIENERSCALART96(signal,fs,IS)
    ## Wiener filter based on tracking a priori SNR usingDecision-Directed 
    ## method, proposed by Scalart et al 96. In this method it is assumed that
    ## SNRpost=SNRprior +1. based on this the Wiener Filter can be adapted to a
    ## model like Ephraims model in which we have a gain function which is a
    ## function of a priori SNR and a priori SNR is being tracked using Decision

    
    W=len(N0)*2  #Window length is 25 ms
    wnd=scipy.signal.hamming(W) 
    
    NIS=0
    Y=_segment(signal,W,SP,wnd)  # This function chops the signal into frames
    
    YPhase=np.angle(Y[range( 0,int(len(Y)/2)),:])  #Noisy Speech Phase
    Y=abs(Y[ range(0,int(len(Y)/2)),:]) #Specrogram
    numberOfFrames=np.shape(Y)[1] 
    
    N=N0
    LambdaD=N*N #initial Noise Power Spectrum variance
#    alpha=.99  #used in smoothing xi (For Deciesion Directed method for estimation of A Priori SNR)
    NoiseCounter=0 
    NoiseLength=20 #This is a smoothing factor for the noise updating
    G=np.ones(np.shape(N)) #Initial Gain used in calculation of the new xi
    Gamma=G 
        
    X=np.zeros(np.shape(Y))  # Initialize X (memory allocation)
    X_dist=np.zeros(numberOfFrames)  # Initialize X (memory allocation)
    for i in range(numberOfFrames):
        if i % 100==0:
            sys.stdout.write('.')
        ################VAD and Noise Estimation START
        NoiseFlag, SpeechFlag, NoiseCounter, Dist=_vad(Y[:,i],N,NoiseCounter,NoiseMargin,8)
        if i<=NIS: # If initial silence ignore VAD
            SpeechFlag=0 
            NoiseCounter=100 
#        else: # Else Do VAD
#            NoiseFlag, SpeechFlag, NoiseCounter, Dist=vad(Y[:,i],N,NoiseCounter,3,8)  #Magnitude Spectrum Distance VAD
        X_dist[i]=Dist   
        
        if SpeechFlag==0: # If not Speech Update Noise Parameters
            N=(NoiseLength*N+Y[:,i] + ControlWeight*N0)/(NoiseLength+1+ControlWeight)  #Update and smooth noise mean
            LambdaD=(NoiseLength*LambdaD+(Y[:,i]**2+ ControlWeight*N0**2))/(+1+ControlWeight+NoiseLength)  #Update and smooth noise variance
        ###################VAD and Noise Estimation END
        
        gammaNew=(Y[:,i]**2)/LambdaD  #A postiriori SNR
        xi=alpha*(G**2)*Gamma+(1-alpha)*np.max(gammaNew-1,0)  #Decision Directed Method for A Priori SNR
        Gamma=gammaNew 
        
        G=(xi/(xi+1)) 
        
        X[:,i]=G*Y[:,i]  #Obtain the new Cleaned value
        
      
    output=_OverlapAddW(X,YPhase,W,SP*W,wnd)  #Overlap-add Synthesis of speech
    #output=filter(1,[1 -pre_emph],output)  #Undo the effect of Pre-emphasis
    return (output, Y, X_dist)
    
def WienerScalart96(signal,fs,IS):

    ## output=WIENERSCALART96(signal,fs,IS)
    ## Wiener filter based on tracking a priori SNR usingDecision-Directed 
    ## method, proposed by Scalart et al 96. In this method it is assumed that
    ## SNRpost=SNRprior +1. based on this the Wiener Filter can be adapted to a
    ## model like Ephraims model in which we have a gain function which is a
    ## function of a priori SNR and a priori SNR is being tracked using Decision
    ## Directed method. 
    ## Author: Esfandiar Zavarehei
    ## Created: MAR-05
    
    W=floor(.025*fs)  #Window length is 25 ms
    SP=.4  #Shift percentage is 40# (10ms) #Overlap-Add method works good with this value(.4)
    wnd=scipy.signal.hamming(W) 
    NoiseMargin=3
    #    pre_emph=0 
    #    signal=filter([1 -pre_emph],1,signal) 
    
    NIS=floor((IS*fs-W)/(SP*W) +1) #number of initial silence segments
    if NIS<=0:
        NIS=100
        
    Y=_segment(signal,W,SP,wnd)  # This function chops the signal into frames
    #Y=fft(y) 
    
    YPhase=np.angle(Y[range( 0,int(len(Y)/2)),:])  #Noisy Speech Phase
    Y=abs(Y[ range(0,int(len(Y)/2)),:]) #Specrogram
    numberOfFrames=np.shape(Y)[1] 
#    FreqResol=np.shape(Y)[0] 
    
    N=np.mean(Y[:,range(0,int(NIS))],axis=1)  #initial Noise Power Spectrum mean
    LambdaD=N*N #initial Noise Power Spectrum variance
    alpha=.99  #used in smoothing xi (For Deciesion Directed method for estimation of A Priori SNR)
    NoiseCounter=0 
    NoiseLength=9 #This is a smoothing factor for the noise updating
    G=np.ones(np.shape(N)) #Initial Gain used in calculation of the new xi
    Gamma=G 
        
    X=np.zeros(np.shape(Y))  # Initialize X (memory allocation)
    X_dist=np.zeros(numberOfFrames)  # Initialize X (memory allocation)
    for i in range(numberOfFrames):
        if i % 100==0:
            sys.stdout.write('.')
        ################VAD and Noise Estimation START
        NoiseFlag, SpeechFlag, NoiseCounter, Dist=_vad(Y[:,i],N,NoiseCounter,NoiseMargin,8)
        if i<=NIS: # If initial silence ignore VAD
            SpeechFlag=0 
            NoiseCounter=100 
#        else: # Else Do VAD
#            NoiseFlag, SpeechFlag, NoiseCounter, Dist=vad(Y[:,i],N,NoiseCounter,3,8)  #Magnitude Spectrum Distance VAD
        X_dist[i]=Dist   
        
        if SpeechFlag==0: # If not Speech Update Noise Parameters
            N=(NoiseLength*N+Y[:,i])/(NoiseLength+1)  #Update and smooth noise mean
            LambdaD=(NoiseLength*LambdaD+(Y[:,i]**2))/(1+NoiseLength)  #Update and smooth noise variance
        ###################VAD and Noise Estimation END
        
        gammaNew=(Y[:,i]**2)/LambdaD  #A postiriori SNR
        xi=alpha*(G**2)*Gamma+(1-alpha)*np.max(gammaNew-1,0)  #Decision Directed Method for A Priori SNR
        Gamma=gammaNew 
        
        G=(xi/(xi+1)) 
        
        X[:,i]=G*Y[:,i]  #Obtain the new Cleaned value
        
      
    output=_OverlapAdd2(X,YPhase,W,SP*W)  #Overlap-add Synthesis of speech
    #output=filter(1,[1 -pre_emph],output)  #Undo the effect of Pre-emphasis
    return (output, Y, X_dist)

def _OverlapAddW(X,YPhase,windowLen,ShiftLen,wnd):
    if floor(ShiftLen)!=ShiftLen:
        ShiftLen=floor(ShiftLen) 
    
    FreqRes, FrameNum=np.shape(X) 
    wnd2=wnd+1
    Spec=X*np.exp(1j*YPhase) 
        
    if np.mod(windowLen,2)==0: #if FreqResol is odd
        Spec=np.concatenate((Spec, np.flipud(np.conj(Spec[range(1,len(Spec)),:])))) 
    else:
        Spec=np.concatenate((Spec,np.flipud(np.conj(Spec[range(1,len(Spec)-1),:])))) 
        
    sig=np.zeros((FrameNum-1)*ShiftLen+windowLen )
    weight=np.zeros((FrameNum-1)*ShiftLen+windowLen )
    for i in range(FrameNum):
        start=int(i*ShiftLen)
        start=range(start,int(start+windowLen))
        spec=np.real(ifft(Spec[:,i],windowLen))  
        sig[start ]=(sig[start])+   spec*wnd2
        weight[start ]=(weight[start])+   wnd2
        if i % 100==0:
            sys.stdout.write('.')

    return sig /weight
    
def _OverlapAdd2(X,YPhase,windowLen,ShiftLen):
    if floor(ShiftLen)!=ShiftLen:
        ShiftLen=floor(ShiftLen) 
    
    FreqRes, FrameNum=np.shape(X) 
    
    Spec=X*np.exp(1j*YPhase) 
        
    if np.mod(windowLen,2)==0: #if FreqResol is odd
        Spec=np.concatenate((Spec, np.flipud(np.conj(Spec[range(1,len(Spec)),:])))) 
    else:
        Spec=np.concatenate((Spec,np.flipud(np.conj(Spec[range(1,len(Spec)-1),:])))) 
        
    sig=np.zeros((FrameNum-1)*ShiftLen+windowLen )
    
    for i in range(FrameNum):
        start=int(i*ShiftLen)
        start=range(start,int(start+windowLen))
        spec=np.real(ifft(Spec[:,i],windowLen))  
        sig[start ]=(sig[start])+   spec 
        if i % 100==0:
            sys.stdout.write('.')
    return sig 

def _segment(signal,W,SP,wnd):
    L=len(signal) 
    SP=floor(W*SP) 
    N=floor((L-W)/SP +1)  #number of segments
    
    Seg= np.zeros((W,N ),dtype=np.complex64)
    cc=0
    for I in range(0,len(signal)-int(W),int(SP)):
        Seg[:,cc]=fft(signal[range(I,int(I+W))]*wnd)
        cc=cc+1
    
    return Seg


def _vad(signal,noise,NoiseCounter,NoiseMargin,Hangover):
#    FreqResol=len(signal) 
    
    SpectralDist= 20*(np.log10(signal)-np.log10(noise)) 
    idx = [i for i,x in enumerate(SpectralDist) if x<0]
    SpectralDist[idx]=0 
    
    Dist=np.mean(SpectralDist)  
    if (Dist < NoiseMargin) :
        NoiseFlag=1  
        NoiseCounter=NoiseCounter+1 
    else:
        NoiseFlag=0 
        NoiseCounter=0 
    
    # Detect noise only periods and attenuate the signal     
    if (NoiseCounter > Hangover):
        SpeechFlag=0     
    else:
        SpeechFlag=1  
        
    return (NoiseFlag, SpeechFlag, NoiseCounter, Dist)
 
