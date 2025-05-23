using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.DataModel.Fileserver
{
    public class FileQueue
    {
        public int Id { get; set; }
        public int OwnerID { get; set; }
        public string Filename { get; set; }
        public byte[] Data { get; set; }
        public DateTime DateAcquired { get; set; }
    }
}
