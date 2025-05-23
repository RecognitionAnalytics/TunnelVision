using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using TunnelVision.DataModel.Fileserver;

namespace TunnelVision.DataModel
{
    public class Experiment: iHasNote
    {
        public override string ToString()
        {
            return ExperimentName;
        }

        public void RateItem(UserRating rating)
        {

        }

        [Browsable(false)]
#if Travel
        //    [DatabaseGenerated(DatabaseGeneratedOption.None)]
#endif
        public int Id { get; set; }

        //  public Dictionary<string, string> OtherInfo { get; set; }

        [Browsable(false)]
        public virtual List<Batch> Batches { get; set; }

        public string ExperimentName { get; set; }

        [Browsable(false)]
        public virtual List<eNote> Notes { get; set; }

        [Browsable(false)]
        public virtual List<CalendarGoals> CalendarEvents { get; set; }

        [Browsable(false)]
        public virtual List<TSUsers> ExperimentUsers { get; set; }

        [Browsable(false)]
        public int LastNumberOfJunctions { get; set; }

        [Browsable(false)]
        public int LastNumberOfChips { get; set; }

        [ReadOnly(true)]
        public string Folder { get; set; }

        public void SaveNote(eNote note)
        {
            using (LabContext context = new LabContext())
            {
                if (note.Id == 0)
                {
                    var exp = context.Experiments.First(x => x.Id == Id);
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

        public void Save()
        {
            using (LabContext context = new LabContext())
            {
                var exp = context.Experiments.First(x => x.Id == Id);
                exp.Notes = Notes;
                //exp.Batches = Batches;

                exp.ExperimentName = ExperimentName;
                //exp.ControlSteps = ControlSteps;
                //exp.ExperimentUsers = ExperimentUsers;
                exp.LastNumberOfJunctions = LastNumberOfJunctions;
                exp.LastNumberOfChips = LastNumberOfChips;
                context.SaveChanges();
            }

        }

        public string OtherInfo { get; set; }
       
       
    }
}
