using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.DataModel
{
    public class DataTrace : Trace
    {

        public string Analyte { get; set; }

        public double WeinerNoise { get; set; }
        public double MeanVoltage { get; set; }


        public double Baseline { get; set; }
        public double Noise { get; set; }

        public double FlattenedBaseline { get; set; }
        public double Tunnel_Range { get; set; }

        public override void RateItem(UserRating rating)
        {
            using (LabContext context = new LabContext())
            {
                var trace = (from x in context.Traces where x.Id == this.Id select x).FirstOrDefault();
                if (trace != null)
                {
                    trace.GoodTrace = rating;
                    context.SaveChanges();
                }
            }
        }


        public DataTrace() : base()
        {

        }

        public DataTrace(string filename)
        {
            this.Filename = filename;
            this.ProcessedFile = @"c:\temp\temp_" + System.IO.Path.GetFileNameWithoutExtension(filename);
            Current = Fileserver.FileTrace.ConvertABF(filename, this.ProcessedFile);
            _ProcessedData = new List<string>() { "Raw" };
            this.Folder = @"c:\temp";
            this.Id = -1;
        }

        public override void Delete()
        {
            base.Delete();
            try
            {
                using (LabContext context = new LabContext())
                {
                    var trace = (from x in context.Traces where x.Id == this.Id select x).FirstOrDefault();
                    if (trace != null)
                        context.Traces.Remove(trace);
                    context.SaveChanges();
                }
            }
            catch
            {
                using (LabContext context = new LabContext())
                {
                    var trace = (from x in context.Traces where x.Id == this.Id select x).FirstOrDefault();
                    if (trace != null)
                        context.Traces.Remove(trace);
                    context.SaveChanges();
                }
            }
        }

        [ReadOnly(true)]
        public DateTime DateProcessed { get; set; }

        public void ReadHeader(Dictionary<string, string> header)
        {
            try
            {
                if (header.ContainsKey("samplerate") == true)
                {
                    this.SampleRate = double.Parse(header["samplerate"]);

                    this.HasIonic = (header["hasionic"].ToLower() == "true");
                    this.DataRig = "Axon";

                    string date = header["filedate"];
                    string[] tParts = header["starttime"].Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                    try
                    {
                        DateTime dt = new DateTime(int.Parse(date.Substring(0, 4)), int.Parse(date.Substring(4, 2)), int.Parse(date.Substring(6, 2)),
                            int.Parse(tParts[0]), int.Parse(tParts[1]), int.Parse(tParts[2]));
                        this.DateAcquired = dt;
                    }
                    catch { }
                    try
                    {
                        if (header.ContainsKey("tun_wnoise") == true)
                        {
                            this.WeinerNoise = double.Parse(header["tun_wnoise"].Trim().Split(' ')[0]);// :5.57823209556 pA
                            this.MeanVoltage = double.Parse(header["meanvoltage"].Trim().Split(' ')[0]);
                            this.Noise = double.Parse(header["tun_noise"].Trim().Split(' ')[0]);
                            this.Baseline = double.Parse(header["tun_grossbaseline"].Trim().Split(' ')[0]);
                            this.FlattenedBaseline = double.Parse(header["tun_flatbaseline"].Trim().Split(' ')[0]);
                            this.Tunnel_Range = double.Parse(header["tun_range"].Trim().Split(' ')[0]);
                        }
                    }
                    catch { }
                    try
                    {

                        if (header.ContainsKey("tun_wnoise") == true)
                        {
                            this.RunTime = double.Parse(header["tracetime"].Trim().Split(' ')[0]);
                        }
                        else
                        {
                            this.RunTime = double.Parse(header["npoints"]) / this.SampleRate;
                        }

                    }
                    catch
                    {
                        if (header.ContainsKey("npoints") == true)
                        {
                            this.RunTime = double.Parse(header["npoints"]) / this.SampleRate;
                        }
                    }
                }
                else
                {
                    this.SampleRate = 20000;
                    this.HasIonic = false;
                    this.DataRig = "Axon";
                    this.DateAcquired = DateTime.Now;
                }

            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
                //System.Windows.Forms.MessageBox.Show(trace.Folder + "\\" + trace.TraceName + "_header.txt");
                //  System.Windows.Forms.MessageBox.Show(ex.Message);
                // System.Windows.Forms.MessageBox.Show(ex.StackTrace);
            }
        }

        [NotMapped]
        [Browsable(false)]
        public string BigCondition
        {
            get
            {
                return Math.Round(Concentration_mM, 6) + Analyte + Math.Round(BufferConcentration_mM, 6) + Buffer + Condition;
            }
        }
    }
}
