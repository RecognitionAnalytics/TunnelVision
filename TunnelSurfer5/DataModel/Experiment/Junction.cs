using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;


namespace TunnelVision.DataModel
{
    public class Junction : iRateable, iHasNote
    {
        public override string ToString()
        {
            return JunctionName;
        }

        public string OtherInfo { get; set; }

        [Browsable(false)]
#if Travel
        //     [DatabaseGenerated(DatabaseGeneratedOption.None)]
#endif
        public int Id { get; set; }

        [Browsable(false)]
        public virtual List<DataTrace> Traces { get; set; }

        [Browsable(false)]
        public virtual List<TunnelingTrace> TunnelIV { get; set; }

        [Browsable(false)]
        public virtual List<IonicTrace> IonicIV { get; set; }

        [Browsable(false)]
        public virtual List<AdditionalFile> AdditionalFiles { get; set; }

        [ReadOnly(true)]
        public virtual eNote Note { get; set; }

        [ReadOnly(true)]
        public virtual Experiment Experiment { get; set; }

        [ReadOnly(true)]
        public virtual Chip Chip { get; set; }

        [ReadOnly(true)]
        public string Folder { get; set; }

        public double GapSize { get; set; }

        // public Dictionary<string, string> OtherInfo { get; set; }

        public string JunctionName { get; set; }

        public UserRating GoodJunction { get; set; }

        [NotMapped]
        public QualityControlStep QCStep
        {
            get
            {
                QualityControlStep qc;
                try
                {
                    if (Traces != null && Traces.Count > 0)
                        qc = (from x in Traces select x.QCStep).Max();
                    else
                        qc = QualityControlStep.Delivered;
                }
                catch
                {
                    qc = 0;
                }
                return qc;
            }
        }

        public void RateItem(UserRating rating)
        {
            using (LabContext context = new LabContext())
            {
                var trace = (from x in context.Junctions where x.Id == this.Id select x).First();
                trace.GoodJunction = rating;

                context.SaveChanges();
            }

        }



        /////////////////////////////////////////////////////////////////////////
        [NotMapped]
        public double RunTime
        {
            get
            {
                return (from x in Traces select x.RunTime).Sum();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public List<TraceFolder> Folders
        {
            get
            {
                var t = new List<TraceFolder>();

                try
                {
                    var tTraces = (Traces.OrderBy(x => x.DateAcquired));

                    var analytes = (from x in tTraces select x.Analyte).Distinct();
                    var allConditions = new List<string>();
                    foreach (var analyte in analytes)
                    {
                        var conditions = (from x in tTraces where x.Analyte == analyte select x.Condition).Distinct();
                        allConditions.AddRange(conditions);
                    }
                    allConditions = allConditions.Distinct().ToList();

                    //var t2 = new List<Trace>();
                    //t2.AddRange(TunnelIV);
                    //t2.AddRange(IonicIV);
                    //t2.AddRange(Chip.IonicIV);
                    //  var ionicFolder = new TraceFolder { FolderName = "Traces", Traces = t2, Junction = this };
                    //var c = new ConditionFolder { Junction = this,  FolderName = "IV Curves", Folders = new List<TraceFolder> { ionicFolder } };
                    // t.Add(c);


                    foreach (var cond in allConditions)
                    {
                        var condAnalytes = (from x in tTraces where x.Condition == cond select x.Analyte).Distinct().ToList();

                        var t3 = new List<TraceFolder>();
                        foreach (var ana in condAnalytes)
                        {
                            t3.Add(new TraceFolder
                            {
                                FolderName = ana,
                                Junction = this,
                                Traces = new List<Trace>((from x in tTraces where (x.Analyte == ana && x.Condition == cond) select x))
                            }
                            );
                        }

                        t.Add(new TraceFolder { Junction = this, FolderName = cond, Folders = t3 });
                    }


                }
                catch
                {

                }
                return t;
            }
        }

        [NotMapped]
        [Browsable(false)]
        public List<DataTrace> TimedTraces
        {
            get
            {
                return (Traces.OrderBy(x => x.DateAcquired)).ToList();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public List<TraceFolder> AnalyteFolders
        {
            get
            {
                var t = new List<TraceFolder>();

                try
                {
                    var tTraces = (Traces.OrderBy(x => x.DateAcquired));

                    var conditions = (from x in tTraces select x.Condition).Distinct();
                    var allAnalytes = new List<string>();
                    foreach (var condition in conditions)
                    {
                        var analyte = (from x in tTraces where x.Condition == condition select x.Analyte).Distinct();
                        allAnalytes.AddRange(analyte);
                    }
                    allAnalytes = allAnalytes.Distinct().ToList();

                    foreach (var ana in allAnalytes)
                    {
                        var condAnalytes = (from x in tTraces where x.Analyte == ana select x.Condition).Distinct().ToList();

                        var aTraces = (from x in tTraces where x.Analyte == ana select x).ToList();

                        var t3 = new List<TraceFolder>();
                        foreach (var cond in condAnalytes)
                        {
                            var fTraces = (from x in aTraces where x.Condition == cond select x).ToList();
                            t3.Add(new TraceFolder
                            {
                                FolderName = cond,
                                Junction = this,
                                Traces = new List<Trace>(fTraces)
                            }
                            );
                        }

                        t.Add(new TraceFolder { Junction = this, FolderName = ana, Folders = t3 });
                    }


                }
                catch
                {

                }
                return t;
            }
        }

        [NotMapped]
        [Browsable(false)]
        public List<TraceFolder> TimeOnly
        {
            get
            {
                var t = new List<TraceFolder>();

                try
                {
                    var tTraces = (Traces.OrderBy(x => x.DateAcquired));
                    var t4 = new List<TraceFolder>();
                    var t5 = new List<Trace>();
                    foreach (var ttt in tTraces)
                        t5.Add((Trace)ttt);
                    t4.Add(new TraceFolder { FolderName = "Time", Junction = this, Traces = t5 });
                    t.Add(new TraceFolder { Junction = this, FolderName = "Time Ordered", Folders = t4 });
                }
                catch
                {

                }
                return t;
            }
        }


        [NotMapped]
        public double GapSizeDelivered
        {
            get
            {
                try
                {
                    return (from x in TunnelIV where x.QCStep == QualityControlStep.Delivered select x.GapSize).Average();
                }
                catch
                {
                    return 0;
                }
            }
        }

        [NotMapped]
        public double GapSizeDrilled
        {
            get
            {
                try
                {
                    return (from x in TunnelIV where x.QCStep > QualityControlStep.Delivered select x.GapSize).Average();
                }
                catch
                {
                    return 0;
                }
            }
        }

        [NotMapped]
        [Browsable(false)]
        public Dictionary<string, Fileserver.FileTrace> TunnelTraces
        {
            get
            {
                TunnelingTrace deliverTrace = null;
                Fileserver.FileTrace xValues = null;
                try
                {
                    deliverTrace = (from x in TunnelIV where x.QCStep == QualityControlStep.Delivered select x).FirstOrDefault();
                    if (deliverTrace != null)
                        xValues = deliverTrace.GetProcessed("Voltage");
                }
                catch
                {

                }

                TunnelingTrace drilledTrace = null;
                try
                {
                    drilledTrace = (from x in TunnelIV where x.QCStep == QualityControlStep.Drilled select x).FirstOrDefault();
                    if (xValues == null && drilledTrace != null)
                        xValues = drilledTrace.GetProcessed("Voltage");
                }
                catch
                {

                }

                TunnelingTrace wetTrace = null;
                try
                {
                    wetTrace = (from x in TunnelIV where x.QCStep > QualityControlStep.Drilled select x).FirstOrDefault();
                    if (xValues == null && wetTrace != null)
                        xValues = wetTrace.GetProcessed("Voltage");
                }
                catch
                {

                }
                if (xValues == null)
                {
                    try
                    {
                        var tracesA = new Dictionary<string, Fileserver.FileTrace>();
                        xValues = TunnelIV[0].GetProcessed("Voltage");
                        if (xValues != null)
                        {
                            tracesA.Add("Voltage", xValues);
                            foreach (var x in TunnelIV)
                            {
                                try
                                {
                                    tracesA.Add(x.TraceName, x.Current);
                                }
                                catch { }
                            }
                        }
                        return tracesA;
                    }
                    catch
                    {
                        return null;
                    }
                }

                var traces = new Dictionary<string, Fileserver.FileTrace>();
                if (xValues != null)
                {
                    traces.Add("Voltage", xValues);
                    if (deliverTrace != null)
                        traces.Add("Delivered", deliverTrace.Current);
                    if (drilledTrace != null)
                        traces.Add("Drilled", drilledTrace.Current);
                    if (wetTrace != null)
                        traces.Add("Wet", wetTrace.Current);
                }
                return traces;
            }
        }

        [NotMapped]
        [Browsable(false)]
        public Dictionary<string, Fileserver.FileTrace> IonicTraces
        {
            get
            {
                IonicTrace deliverTrace = null;
                Fileserver.FileTrace xValues = null;
                try
                {
                    deliverTrace = (from x in IonicIV where x.QCStep == QualityControlStep.Delivered select x).FirstOrDefault();
                    if (deliverTrace != null)
                        xValues = deliverTrace.GetProcessed("Voltage");
                }
                catch
                {

                }

                IonicTrace drilledTrace = null;
                try
                {
                    drilledTrace = (from x in IonicIV where x.QCStep == QualityControlStep.Drilled select x).FirstOrDefault();
                    if (xValues == null && drilledTrace != null)
                        xValues = drilledTrace.GetProcessed("Voltage");
                }
                catch
                {

                }

                IonicTrace wetTrace = null;
                try
                {
                    wetTrace = (from x in IonicIV where x.QCStep > QualityControlStep.Drilled select x).FirstOrDefault();
                    if (xValues == null && wetTrace != null)
                        xValues = wetTrace.GetProcessed("Voltage");
                }
                catch
                {

                }
                if (xValues == null)
                {
                    try
                    {
                        var tracesA = new Dictionary<string, Fileserver.FileTrace>();
                        xValues = IonicIV[0].GetProcessed("Voltage");
                        if (xValues != null)
                        {
                            tracesA.Add("Voltage", xValues);
                            foreach (var x in IonicIV)
                            {
                                try
                                {
                                    tracesA.Add(x.TraceName, x.Current);
                                }
                                catch { }
                            }
                        }
                        return tracesA;
                    }
                    catch
                    {
                        return null;
                    }
                }

                var traces = new Dictionary<string, Fileserver.FileTrace>();
                if (xValues != null)
                {
                    traces.Add("Voltage", xValues);
                    if (deliverTrace != null)
                        traces.Add("Delivered", deliverTrace.Current);
                    if (drilledTrace != null)
                        traces.Add("Drilled", drilledTrace.Current);
                    if (wetTrace != null)
                        traces.Add("Wet", wetTrace.Current);
                }
                return traces;
            }
        }


        public void SaveNote(eNote note)
        {
            using (LabContext context = new LabContext())
            {
                if (note.Id == 0)
                {
                    var junction = context.Junctions.First(x => x.Id == Id);
                    junction.Note = (note);
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

        [NotMapped]
        [Browsable(false)]
        public bool isSelected
        {
            get
            {
                return ((string)Properties.Settings.Default["DefaultJunction"] == JunctionName);
            }
        }

        public  void Delete()
        {
           
                using (LabContext context = new LabContext())
                {
                    try
                    {
                        if (Note != null)
                            Note.Delete();
                    }
                    catch
                    {
                        int id = Note.Id;
                        Note = null;
                        context.SaveChanges();
                        var note = (from x in context.Notes where x.Id == id select x).FirstOrDefault();
                        if (note != null)
                            context.Notes.Remove(note);
                    }
    
                    for (int i = 0; i < AdditionalFiles.Count; i++)
                        AdditionalFiles[i].Delete();
                    for (int i = 0; i < Traces.Count; i++)
                        Traces[i].Delete();
                    for (int i = 0; i < IonicIV.Count; i++)
                        IonicIV[i].Delete();
                    for (int i = 0; i < TunnelIV.Count; i++)
                        TunnelIV[i].Delete();

                    var trace = (from x in context.Junctions where x.Id == this.Id select x).FirstOrDefault();
                    if (trace != null)
                        context.Junctions.Remove(trace);
                    context.SaveChanges();
                }
           
        }
    }




}
