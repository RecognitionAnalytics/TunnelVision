using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;


namespace TunnelVision.DataModel
{
    public class eNote
    {
        [Browsable(false)]
#if Travel
        //     [DatabaseGenerated(DatabaseGeneratedOption.None)]
#endif
        public int Id { get; set; }
        public DateTime NoteTime { get; set; }
        public byte[] Note { get; set; }

        [Browsable(false)]
        [NotMapped]
        public FlowDocument Document
        {
            get
            {
                FlowDocument d = new FlowDocument();
                if (Note != null)
                {
                    var content = new TextRange(d.ContentStart, d.ContentEnd);
                    MemoryStream ms = new MemoryStream(Note);
                    content.Load(ms, System.Windows.DataFormats.XamlPackage);
                }
                return d;
            }

        }

        [Browsable(false)]
        public string ShortNote
        {
            get;
            set;
        }

        public void ConvertString(string note)
        {
            ShortNote = note.Substring(0, Math.Min(note.Length, 100));

            FlowDocument document = new FlowDocument();
            document.SetValue(FlowDocument.TextAlignmentProperty, System.Windows.TextAlignment.Left);
            TextRange content = new TextRange(document.ContentStart, document.ContentEnd);
            content.Text = note;

            using (MemoryStream ms = new MemoryStream())
            {
                content.Save(ms, System.Windows.DataFormats.XamlPackage, true);
                Note = ms.ToArray();
            }
        }

        public eNote()
        {
            NoteTime = DateTime.Now;
        }

        public eNote(string text)
        {
            ConvertString(text);
            NoteTime = DateTime.Now;
        }

        public eNote(string text, FlowDocument doc)
        {
            this.NoteTime = DateTime.Now;

            ShortNote = text.Substring(0, Math.Min(text.Length, 100));
            using (MemoryStream ms = new MemoryStream())
            {
                // write XAML out to a MemoryStream
                TextRange tr = new TextRange(
                    doc.ContentStart,
                    doc.ContentEnd);
                ms.Seek(0, SeekOrigin.Begin);
                tr.Save(ms, System.Windows.DataFormats.XamlPackage, true);
                Note = ms.ToArray();
            }
        }

        public override string ToString()
        {
            return ShortNote;
        }

        public void Delete()
        {

            using (LabContext context = new LabContext())
            {

                var note = (from x in context.Notes where x.Id == this.Id select x).FirstOrDefault();
                if (note != null)
                    context.Notes.Remove(note);
                context.SaveChanges();
            }

        }

    }
}

