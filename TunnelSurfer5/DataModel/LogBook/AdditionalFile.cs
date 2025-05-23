using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
namespace TunnelVision.DataModel
{
    public class AdditionalFile
    {
         [Browsable(false)]
#if Travel
        //    [DatabaseGenerated(DatabaseGeneratedOption.None)]
#endif
        public int Id { get; set; }
        public string Title { get; set; }
        public string Filename { get; set; }
        public DateTime AddTime { get; set; }
        public string Notes { get; set; }

        public override string ToString()
        {
            return Filename;
        }

        public void Delete()
        {
            using (LabContext context = new LabContext())
            {
                var note = (from x in context.AdditionalFiles where x.Id == this.Id select x).FirstOrDefault();
                if (note != null)
                    context.AdditionalFiles.Remove(note);
                context.SaveChanges();
            }
        }
    }
}
