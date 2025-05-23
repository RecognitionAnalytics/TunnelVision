using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.DataModel
{
    public class IVTrace : Trace
    {
        public string Analyte { get; set; }
        public string IVMethod { get; set; }

        public double Capacitance { get; set; }
        [Browsable(false)]
        public double ControlConductance { get; set; }
        [Browsable(false)]
        public double ControlCapacitance { get; set; }
        [Browsable(false)]
        public double Chi2 { get; set; }
        [ReadOnly(true)]
        public double Asymmetry { get; set; }

        [ReadOnly(true)]
        public double VoltageOffset { get; set; }
        [Browsable(false)]
        public double CurrentOffset { get; set; }

        [Browsable(false)]
        public double dVdt { get; set; }


    }
}
