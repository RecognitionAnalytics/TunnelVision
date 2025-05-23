function [iC]=SimulateIonic(p,V0)
C=p(1)*1e-9;
R1=p(2)*1e9;
R2=p(3)*1e9;
global dt
Rt=R1+R2;
Q=0;
iC=zeros(size(V0));
for I=2:length(V0)
    V=V0(I);
    dqdt=(Q/C-V*R2/Rt)/R2/(R2/Rt-1);
    Q=Q+dqdt*dt;
    try
        iC(I)=(R2*dqdt+V)/Rt*1e9;
    catch mex
        disp(mex)
    end
end
iC=iC+p(4);
end