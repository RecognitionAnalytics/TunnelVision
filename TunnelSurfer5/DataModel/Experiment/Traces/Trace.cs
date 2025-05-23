using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.DataModel
{
    public class Trace : iRateable
    {
        [Browsable(false)]
#if Travel
    //    [DatabaseGenerated(DatabaseGeneratedOption.None)]
#endif
        public int Id { get; set; }



        public override string ToString()
        {
            return TraceName;
        }
        [Browsable(false)]
        public virtual Chip Chip { get; set; }
        [Browsable(false)]
        public virtual Junction Junction { get; set; }
        public double RunTime { get; set; }
        [ReadOnly(true)]
        public string Filename { get; set; }
        public string TraceName { get; set; }
        public string Buffer { get; set; }
        public double BufferConcentration_mM { get; set; }
        public double Concentration_mM { get; set; }
        public double Conductance { get; set; }
        public string Condition { get; set; }
        public double Gain { get; set; }
        public double FilterkHz { get; set; }
        public int NumberPores { get; set; }


        [NotMapped]
        public double Format_Ionic_mV
        {
            get
            {
                double top = 0;
                if (TopReference_mV != -1000 && TopReference_mV != -2000 && TopReference_mV != -3000)
                    top = TopReference_mV;

                double bottom = 0;
                if (BottomReference_mV != -1000 && BottomReference_mV != -2000 && BottomReference_mV != -3000)
                    bottom = BottomReference_mV;


                return (bottom - top);
            }
        }
        [NotMapped]
        public double Format_Tunnel_mV
        {
            get
            {

                double tun = 0;
                if (Tunnel_mV_Top != -1000 && Tunnel_mV_Top != -2000 && Tunnel_mV_Top != -3000)
                    tun = Tunnel_mV_Top;
                return tun;
            }
        }

        [NotMapped]
        public string TopRef_mV_ToString
        {
            get
            {
                return ExistingParse.ConvertVoltageFromDouble(TopReference_mV);
            }
        }
        [NotMapped]
        public string BottomRef_mV_ToString
        {
            get
            {
                return ExistingParse.ConvertVoltageFromDouble(BottomReference_mV);
            }
        }
        [NotMapped]
        public string Tunnel_Top_mV_ToString
        {
            get
            {
                return ExistingParse.ConvertVoltageFromDouble(Tunnel_mV_Top);
            }
        }

        [NotMapped]
        public string Tunnel_Bottom_mV_ToString
        {
            get
            {
                return ExistingParse.ConvertVoltageFromDouble(Tunnel_mV_Bottom);
            }
        }

        public double TopReference_mV { get; set; }
        public double BottomReference_mV { get; set; }
        public double Tunnel_mV_Top { get; set; }
        public double Tunnel_mV_Bottom { get; set; }

        [Browsable(false)]
        public double ExpectedConductance { get; set; }
        public double SampleRate { get; set; }
        [ReadOnly(true)]
        public string Folder { get; set; }
        public bool HasIonic { get; set; }
        public string OtherInfo { get; set; }
        public string ProcessedFile { get; set; }

        public bool IsControl { get; set; }
        [ReadOnly(true)]
        public virtual eNote Note { get; set; }
        [Browsable(false)]
        public string ControlFile { get; set; }

        public virtual void SaveNote(eNote note)
        {
            using (LabContext context = new LabContext())
            {
                if (note.Id == 0)
                {
                    var exp = context.Traces.First(x => x.Id == Id);
                    exp.Note = note;
                }
                else
                {
                    var dNote = context.Notes.First(x => x.Id == note.Id);
                    dNote.Note = note.Note;
                    dNote.ShortNote = note.ShortNote;
                    dNote.NoteTime = note.NoteTime;
                }
                context.SaveChanges();
            }
        }

        [ReadOnly(true)]
        public DateTime DateAcquired { get; set; }
        [ReadOnly(true)]
        public virtual TSUsers GeneratingUser { get; set; }

        public virtual QualityControlStep QCStep { get; set; }

        public UserRating GoodTrace { get; set; }
        public UserRating MachineRating { get; set; }
        public string DataRig { get; set; }

        //[Browsable(false)]
        //[NotMapped]
        //private double[] _Time;

        [Browsable(false)]
        [NotMapped]
        public string RawFilename
        {
            get
            {
                return this.ProcessedFile + "_Raw.bin";
            }
        }

        public double[,] GetDataSegment(double minTime, double maxTime, out string[] Headers)
        {
            if (_Current == null)
            {
                OpenData();
            }

            if (_Current == null)
            {
                Headers = null;
                return null;
            }

            int minI = (int)Math.Floor(minTime * SampleRate);
            int maxI = (int)Math.Ceiling(maxTime * SampleRate);
            if (minI < 0)
                minI = 0;
            if (maxI > _Current.Trace.Length)
                maxI = _Current.Trace.Length;

            int numberSeries = 2 + Datas.Keys.Count;
            Headers = new string[numberSeries];
            Headers[0] = "Time(s)";
            Headers[1] = "Current(pA)";

            int cc = 2;
            List<Fileserver.FileTrace> _datas = new List<Fileserver.FileTrace>();
            foreach (KeyValuePair<string, Fileserver.FileTrace> kvp in Datas)
            {
                Headers[cc] = kvp.Key;
                _datas.Add(kvp.Value);
                cc = cc + 1;
            }

            double[,] outData = new double[(maxI - minI), numberSeries];

            cc = 0;
            for (int i = minI; i < maxI; i++)
            {
                try
                {
                    outData[cc, 0] = _Current.GetTime(i);
                    outData[cc, 1] = _Current.Trace[i];
                    for (int j = 0; j < _datas.Count; j++)
                    {
                        if (_datas[j].Trace.Length > i)
                            outData[cc, 2 + j] = _datas[j].Trace[i];
                    }
                    cc++;
                }
                catch
                {
                    break;
                }
            }
            return outData;
        }

        public double GetTraceValue(double time)
        {
            try
            {
                if (_Current == null)
                {
                    OpenData();
                }
                int minI = (int)Math.Floor(time * SampleRate);
                return _Current.Trace[minI];
            }
            catch
            {
                return 0;
            }
        }

        [Browsable(false)]
        [NotMapped]
        private DataModel.Fileserver.FileTrace _Current;

        [Browsable(false)]
        [NotMapped]
        public DataModel.Fileserver.FileTrace Current
        {
            get
            {
                if (_Current == null)
                {
                    OpenData();
                }
                return _Current;
            }
            set
            {
                _Current = value;
            }
        }

        public void ClearMemory()
        {
            _Current = null;
            // _Time = null;
            Datas.Clear();
        }

        [NotMapped]
        protected List<string> _ProcessedData;
        [NotMapped]
        [Browsable(false)]
        public List<string> ProcessedData
        {
            get
            {
                if (_ProcessedData == null)
                {
                    OpenData();

                }

                return _ProcessedData;

            }

        }

        [NotMapped]
        [Browsable(false)]
        private Dictionary<string, Fileserver.FileTrace> Datas = new Dictionary<string, Fileserver.FileTrace>();

        [NotMapped]
        [Browsable(false)]
        public Fileserver.FileTrace ShortCurrent
        {
            get
            {
                return DataModel.Fileserver.FileTrace.OpenBinary(this, "Short");
            }
        }


        public List<Fileserver.FileTrace> GetHistograms(string histogram)
        {
            return DataModel.Fileserver.FileHistogram.GetHistograms(this, histogram);
        }

        public string[] GetSimplified()
        {
            var fs = DataModel.Fileserver.FileTrace.GetSimplfied(this);
            string[] Files = new string[fs.Length];
            string proFile = Path.GetFileNameWithoutExtension(this.ProcessedFile)+"_";
            for (int i = 0; i < fs.Length; i++)
            {
                Files[i] = Path.GetFileNameWithoutExtension( fs[i]).Replace(proFile, "");
            }
            return Files;
        }

        public Fileserver.FileTrace GetProcessed(string DataLabel)
        {

            if (_Current == null)
                OpenData();

         //   if (DataLabel.ToLower() == "raw")
           //     return _Current;

            if (DataLabel == "BackgroundRemoved")
                DataLabel = "Background_Removed";

            if (Datas.ContainsKey(DataLabel.ToLower()))
                return Datas[DataLabel.ToLower()];
            try
            {
                var binData = DataModel.Fileserver.FileTrace.OpenBinary(this, DataLabel);
                Datas.Add(DataLabel.ToLower(), binData);
                return binData;
            }
            catch
            {
                return null;
            }
        }

        public Dictionary<string, string> GetHeader()
        {
            return DataModel.Fileserver.FileDataTrace.ReadHeader(ProcessedFile + "_header.txt");
        }

        private void OpenData()
        {
            _ProcessedData = DataModel.Fileserver.FileDataTrace.AvailableTraces(this);
            if (_ProcessedData == null)
                return;

            _Current = DataModel.Fileserver.FileTrace.OpenBinary(this, "smooth");
            if (_Current==null && _ProcessedData.Count>0)
            {
                _Current = DataModel.Fileserver.FileTrace.OpenBinary(this, _ProcessedData[0].ToLower());
                if (_Current ==null)
                    _Current = DataModel.Fileserver.FileTrace.OpenBinary(this, "ionic");
            }
        }


        public virtual void RateItem(UserRating rating)
        {

            using (LabContext context = new LabContext())
            {
                var trace = (from x in context.Traces where x.Id == this.Id select x).First();
                trace.GoodTrace = rating;
                context.SaveChanges();
            }

        }

        public virtual void Delete()
        {
            try
            {
                Note.Delete();
            }
            catch
            {
                using (LabContext context = new LabContext())
                {
                    int id = Note.Id;
                    Note = null;
                    context.SaveChanges();
                    var note = (from x in context.Notes where x.Id == id select x).FirstOrDefault();
                    if (note != null)
                        context.Notes.Remove(note);
                }
            }
        }
    }
}
