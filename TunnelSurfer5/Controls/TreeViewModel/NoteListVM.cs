using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.Controls.TreeViewModel
{
    public class NoteListVM
    {
        public DataModel.iHasNote Parent { get; private set; }
        public DataModel.eNote Note { get; set; }
        public YATE.YATEditor Editor { get; set; }
        public ObservableCollection<DataModel.eNote> Notes { get; set; }

        public void SaveNote()
        {
            DataModel.eNote note = new DataModel.eNote(Editor.PlainText, Editor.Document);
            note.Id = this.Note.Id;
            Parent.SaveNote( note );
        }

        public NoteListVM(DataModel.iHasNote parent, List<DataModel.eNote> notes)
        {
            Parent = parent;
            Notes = new ObservableCollection<DataModel.eNote>();
            foreach (var note in notes)
            {
                Notes.Add(note);
            }
            if (notes != null && notes.Count > 0)
                Note = notes.Last();
        }

        public NoteListVM(DataModel.iHasNote parent, DataModel.eNote note)
        {
            Parent = parent;
            Notes = new ObservableCollection<DataModel.eNote>();
            Note = note;
        }
        public NoteListVM() { }
    }
}
