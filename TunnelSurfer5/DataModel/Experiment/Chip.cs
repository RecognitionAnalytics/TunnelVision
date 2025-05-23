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
    public class Chip : iRateable,iHasNote
    {
        public override string ToString()
        {
            return ChipName;
        }
        [Browsable(false)]
#if Travel
        //  [DatabaseGenerated(DatabaseGeneratedOption.None)]
#endif
        public int Id { get; set; }
        [ReadOnly(true)]
        public virtual List<Junction> Junctions { get; set; }
        public string ChipName { get; set; }
        [Browsable(false)]
        public virtual eNote Note { get; set; }
        public string DrillMethod { get; set; }
        public string ALDMaterial { get; set; }
        public int ALDCycles { get; set; }
        public string JunctionMaterial { get; set; }
        public float PoreSize { get; set; }
        public int NumberPores { get; set; }
        public int NumberOfJunctions { get; set; }

        public virtual List<AdditionalFile> AdditionalFiles { get; set; }

        public virtual List<IonicTrace> IonicIV { get; set; }

        public string OtherInfo { get; set; }

        [NotMapped]
        public QualityControlStep QCStep
        {
            get
            {
                try
                {
                    return (from x in Junctions select x.QCStep).Max();
                }
                catch { }
                return 0;
            }
        }

        public UserRating GoodChip
        {
            get;
            set;
        }

        [ReadOnly(true)]
        public string Folder { get; set; }

        public void SaveNote(eNote note)
        {
            using (LabContext context = new LabContext())
            {
                if (note.Id == 0)
                {
                    var chip = context.Chips.First(x => x.Id == Id);
                    chip.Note = (note);
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

        public void RateItem(UserRating rating)
        {
            using (LabContext context = new LabContext())
            {
                var trace = (from x in context.Chips where x.Id == this.Id select x).First();
                trace.GoodChip = rating;

                context.SaveChanges();
            }

        }

        public void AddJunction(string junctionName)
        {
            using (DataModel.LabContext labContext = new DataModel.LabContext())
            {
                var chip = (from x in labContext.Chips where x.Id == Id select x).First();
                string dir = chip.Folder;
                chip.Junctions.Add(
                       new DataModel.Junction
                       {
                           Chip = chip,
                           Experiment = chip.Experiment,
                           JunctionName = junctionName,
                           Folder = dir + "\\" + junctionName

                       });

                try
                {
                    System.IO.Directory.CreateDirectory(dir + "\\" + junctionName);
                }
                catch
                { }
                labContext.SaveChanges();
            }
        }
        /// ///////////////////////////////////////////////////////////////////////////////

        [ReadOnly(true)]
        public virtual Batch Batch { get; set; }

        [ReadOnly(true)]
        public virtual Experiment Experiment { get; set; }


        /////////////////////////////////////////////////////////////////////////

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


        [NotMapped]
        public double JunctionDelivered
        {
            get
            {
                try
                {
                    return (from x in Junctions select x.GapSizeDelivered).Average();
                }
                catch
                {
                    return 0;
                }
            }
        }

        [NotMapped]
        public double JunctionDrilled
        {
            get
            {
                try
                {
                    return (from x in Junctions select x.GapSizeDrilled).Average();
                }
                catch
                {
                    return 0;
                }
            }
        }

        public void Delete()
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

                List<IonicTrace> items = new List<IonicTrace>();
                items.AddRange(IonicIV);
                foreach (var i in items)
                    i.Delete();

                for (int i = 0; i < Junctions.Count; i++)
                    Junctions[i].Delete();

                var trace = (from x in context.Chips where x.Id == this.Id select x).FirstOrDefault();
                if (trace != null)
                    context.Chips.Remove(trace);
                context.SaveChanges();
            }
        }
    }
}
