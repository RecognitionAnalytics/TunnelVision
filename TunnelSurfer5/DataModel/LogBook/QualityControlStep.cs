using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.DataModel
{
    public enum QualityControlStep:int
    {
        Delivered = 0, Drilled = 1, Wet = 2, CMP = 4, RIE = 5, Etch = 6, RT = 7,
    }
}
