using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TunnelVision.Controls.ViewControl
{
    /// <summary>
    /// Interaction logic for ReportView.xaml
    /// </summary>
    public partial class ReportView : UserControl
    {
        public ReportView()
        {
            InitializeComponent();

            this.DataContextChanged += ReportView_DataContextChanged;
        }

        private void ReportView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }



        //public void SetObject(Controls.TreeViewModel.TreeNodeVM obj)
        //{
        //    tbLoading.Visibility = System.Windows.Visibility.Collapsed;
        //    Target = obj;

        //    if (obj == null)
        //        return;


        //    DataContext = null;
        //    DataContext = this;
        //}

        public Controls.TreeViewModel.TreeNodeVM Target { get; set; }


        #region Graphing
        private void host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var host = (System.Windows.Forms.Integration.WindowsFormsHost)sender;
            var chart = (System.Windows.Forms.DataVisualization.Charting.Chart)host.Child;
            var size = CommonExtensions.GetElementPixelSize(host);
            chart.Width = (int)size.Width;
            chart.Height = (int)size.Height;
        }

        private void host_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var host = (System.Windows.Forms.Integration.WindowsFormsHost)sender;
                var chart = (System.Windows.Forms.DataVisualization.Charting.Chart)host.Child;
                var colChart = (ColumnChartViewModel)host.DataContext;
                colChart.SetupChartProperties(chart);
                colChart.Plot();
            }
            catch { }
        }

        private void scatterHost_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var host = (System.Windows.Forms.Integration.WindowsFormsHost)sender;
                var chart = (System.Windows.Forms.DataVisualization.Charting.Chart)host.Child;
                var colChart = (ScatterChartViewModel)host.DataContext;
                colChart.SetupChartProperties(chart);
                colChart.Plot();
            }
            catch { }
        }

        private void ScatterCopy_Clicked(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            var chart = (ScatterChartViewModel)button.DataContext;
            chart.CopyVisible();
            //System.Diagnostics.Debug.Print(sender.ToString());
        }
        #endregion

        #region Notes
        private void NoteList_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.Print(sender.ToString());
        }

        private void NoteList_Loaded(object sender, RoutedEventArgs e)
        {
            var panel = (StackPanel)sender;
            var toolBar = (YATE.YATEExEditor)panel.Children[1];
            var editor = (YATE.YATEditor)panel.Children[2];
            toolBar.Editor = editor;

            var NoteVM = (Controls.TreeViewModel.NoteListVM)panel.DataContext;
            NoteVM.Editor = editor;
            if (NoteVM.Note != null)
                editor.Document = NoteVM.Note.Document;
            System.Diagnostics.Debug.Print(sender.ToString());
        }

        public void SaveNote()
        {
            //  DataModel.eNote note = new DataModel.eNote(NewNote.PlainText, NewNote.Document);

            // target.Notes.Add(note);
            //Target.SaveNote(note);
            //Target.Notes.Add(note);
            //SetObject(target);
            //lbNoteList.ItemsSource = null;
            //lbNoteList.ItemsSource = Target.Notes;
            //lbNoteList.InvalidateVisual();
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            var t = (System.Windows.Controls.Expander)sender;
            // var note = (from x in Target.Notes where t.Header.ToString() == x.ShortNote.ToString() select x).First();
            var flowDoc = (System.Windows.Controls.FlowDocumentReader)t.Content;
            flowDoc.Document = ((DataModel.eNote)t.DataContext).Document;
        }

        private void YateToolbar_SaveClicked(object sender, RoutedEventArgs e)
        {
            var noteVM = (TunnelVision.Controls.TreeViewModel.NoteListVM)((System.Windows.Controls.Button)sender).DataContext;
            noteVM.SaveNote();
            //NewNote.cmdSelectAll();
            //NewNote.cmdDelete();
        }

        #endregion
    }
}
