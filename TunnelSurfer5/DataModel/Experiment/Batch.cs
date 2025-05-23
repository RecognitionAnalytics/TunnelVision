using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace TunnelVision.DataModel
{
    public class Batch :iHasNote
    {
        public override string ToString()
        {
            return BatchName;
        }
        
        [Browsable(false)]
#if Travel
        //      [DatabaseGenerated(DatabaseGeneratedOption.None)]
#endif
        public int Id { get; set; }
        [Browsable(false)]
        public virtual List<Chip> Chips { get; set; }
        public string BatchName { get; set; }
        [ReadOnly(true)]
        public string Folder { get; set; }
        public string DrillMethod { get; set; }
        public string ALDMaterial { get; set; }
        public int ALDCycles { get; set; }
        public string JunctionMaterial { get; set; }
        public DateTime DeliveryDate { get; set; }
        public DateTime ManufactorDate { get; set; }
        public string OtherInfo { get; set; }
        [Browsable(false)]
        public virtual List<eNote> Notes { get; set; }

       // public Dictionary<string, string> OtherInfo { get; set; }

        public int NumberOfDeliveredChips
        {
            get;
            set;
        }


        [NotMapped]
        public int NumberOfDrilledChips
        {
            get
            {
                return (from x in Chips where (int)x.QCStep > (int)QualityControlStep.Delivered select x).Count();
            }
        }

        [NotMapped]
        public int NumberOfRTChips
        {
            get
            {
                return (from x in Chips where (int)x.QCStep == (int)QualityControlStep.RT select x).Count();
            }
        }

        [Browsable(false)]
        public virtual List<AdditionalFile> AdditionalFiles { get; set; }

        [ReadOnly(true)]
        public virtual Experiment Experiment { get; set; }



        ///////////////////////////////////////////////////////////////////////////////////////////
        // Plotting options

        public void Delete()
        {
            using (LabContext context = new LabContext())
            {
                for (int i = 0; i < Notes.Count; i++)
                    Notes[i].Delete();

                for (int i = 0; i < AdditionalFiles.Count; i++)
                    AdditionalFiles[i].Delete();

                for (int i = 0; i < Chips.Count; i++)
                    Chips[i].Delete();


                var trace = (from x in context.Batches where x.Id == this.Id select x).FirstOrDefault();
                if (trace != null)
                    context.Batches.Remove(trace);
                context.SaveChanges();
            }
        }




        public void SaveNote(eNote note)
        {
            using (LabContext context = new LabContext())
            {
                if (note.Id == 0)
                {
                    var exp = context.Batches.First(x => x.Id == Id);
                    exp.Notes.Add(note);
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

       
    }


}
