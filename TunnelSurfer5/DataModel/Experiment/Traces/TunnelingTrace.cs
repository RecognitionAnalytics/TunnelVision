using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.DataModel
{
    public class TunnelingTrace : IVTrace
    {

        public double ExpectedGapSize { get; set; }
        [ReadOnly(true)]
        public double GapSize { get; set; }
        [ReadOnly(true)]
        public double GapPotential { get; set; }
        [ReadOnly(true)]
        public double Dielectric { get; set; }
        [ReadOnly(true)]
        public double JunctionArea { get; set; }
        [ReadOnly(true)]
        public double[] Fit { get; set; }

        public override void RateItem(UserRating rating)
        {
            using (LabContext context = new LabContext())
            {

                var trace = (from x in context.TunnelIV where x.Id == this.Id select x).FirstOrDefault();
                if (trace != null)
                {
                    trace.GoodTrace = rating;

                    context.SaveChanges();
                }
            }
        }

        public override void Delete()
        {
            base.Delete();
            using (LabContext context = new LabContext())
            {
                var trace = (from x in context.TunnelIV where x.Id == this.Id select x).FirstOrDefault();
                if (trace != null)
                    context.TunnelIV.Remove(trace);
                context.SaveChanges();
            }
        }
    }
}
