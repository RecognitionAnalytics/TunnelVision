using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
namespace TunnelVision.DataModel
{
    public class CalendarGoals
    {
         [Browsable(false)]
#if Travel
        //    [DatabaseGenerated(DatabaseGeneratedOption.None)]
#endif
        public int Id { get; set; }
        public DateTime DueDate { get; set; }
        public string Goal { get; set; }

        public override string ToString()
        {
            return DueDate.ToShortDateString() + " " + Goal;
        }

        public void Delete()
        {
            using (LabContext context = new LabContext())
            {
                var note = (from x in context.CalendarEvents where x.Id == this.Id select x).FirstOrDefault();
                if (note != null)
                    context.CalendarEvents.Remove(note);
                context.SaveChanges();
            }
        }
    }
}
