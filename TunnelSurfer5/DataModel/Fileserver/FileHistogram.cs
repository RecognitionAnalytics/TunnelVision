using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TunnelVision.DataModel.Fileserver
{
    public class FileHistogram
    {
        public int Id { get; set; }
        public int OwnerID { get; set; }
        public string HistogramName { get; set; }
        public virtual List<FileTrace> Histograms { get; set; }

        

        public static List<FileTrace> GetHistograms(Trace trace, string histogram, string style = "")
        {
            string dataLocation = trace.ProcessedFile + "_" + histogram + ".bin";
            if (File.Exists(dataLocation))
            {
                return FileTrace.OpenBinary(dataLocation);
            }

            var ff2 = App.ConvertFilePaths(dataLocation);//.ToLower().Replace(@"s:\research\", "\\\\biofs.asurite.ad.asu.edu\\smb\\research\\");
            if (File.Exists(ff2) == true)
                return FileTrace.OpenBinary(ff2);

            ff2 = dataLocation.ToLower().Replace(@"s:\research\tunnelsurfer\experiments", App.DropBoxFolder);
            if (File.Exists(ff2) == true)
                return FileTrace.OpenBinary(ff2);

            return null;
        }
    }
}
