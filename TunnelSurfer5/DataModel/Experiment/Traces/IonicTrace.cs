using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.DataModel
{
    public class IonicTrace : IVTrace
    {
        [ReadOnly(true)]
        public double SmeetPoreSize { get; set; }
        [ReadOnly(true)]
        public double AccessPoreSize { get; set; }
        [Browsable(false)]
        public double ExpectedSmeetConductance { get; set; }

       

        public override void RateItem(UserRating rating)
        {
            using (LabContext context = new LabContext())
            {
                var trace = (from x in context.IonicIV where x.Id == this.Id select x).First();
                trace.GoodTrace = rating;

                context.SaveChanges();
            }

        }

        public override void Delete()
        {
            base.Delete();
            try
            {
                using (LabContext context = new LabContext())
                {
                    var trace = (from x in context.IonicIV where x.Id == this.Id select x).FirstOrDefault();
                    if (trace.Junction != null && trace.Junction.IonicIV != null && trace.Junction.IonicIV.Contains(trace))
                    {
                        trace.Junction.IonicIV.Remove(trace);
                        context.SaveChanges();
                    }
                    if (trace.Chip.IonicIV != null && trace.Chip.IonicIV.Contains(trace))
                    {
                        trace.Chip.IonicIV.Remove(trace);
                        context.SaveChanges();
                    }
                    if (trace != null)
                        context.IonicIV.Remove(trace);
                    context.SaveChanges();
                }
            }
            catch { }
        }
    }
}
