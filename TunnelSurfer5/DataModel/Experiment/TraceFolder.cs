using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.DataModel
{
    public class TraceFolder
    {
        public string FolderName { get; set; }
        public override string ToString()
        {
            return FolderName;
        }

        [Browsable(false)]
        public List<Trace> Traces { get; set; }

        List<TraceFolder> _Folders;
        public List<TraceFolder> Folders
        {
            get
            {
                return _Folders;
            }
            set
            {
                _Folders = value;
                foreach (var f in _Folders)
                    f.Parent = this;

            }
        }

        public TraceFolder Parent { get; set; }

        public Junction Junction
        {
            get;
            set;
        }
       
    }
}
