using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.DataModel
{
    public class TSUsers
    {
         [Browsable(false)]
#if Travel
        //   [DatabaseGenerated(DatabaseGeneratedOption.None)]
#endif
        public int Id { get; set; }
        public string UserName { get; set; }
        public override string ToString()
        {
            return UserName;
        }
    }
}
