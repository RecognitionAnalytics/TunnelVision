using System;
using System.Collections.Generic;
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
using System.Windows.Threading;
using TunnelVision.Controls.TreeViewModel;

namespace TunnelVision.Controls.ViewControl
{
    /// <summary>
    /// Interaction logic for DataReviewView.xaml
    /// </summary>
    public partial class DataReviewView : UserControl
    {
        public DataReviewView()
        {
            InitializeComponent();
            StartTime.Tick += StartTime_Tick;
            ProcessedTime.Tick += ProcessedTime_Tick;
            StartTime.Interval = new TimeSpan(0, 0, 0, 0, 200);
            ProcessedTime.Interval = new TimeSpan(0, 0, 0, 0, 500);
            YateToolbar.Editor = NewNote;
        }



        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void ProcessedTime_Tick(object sender, EventArgs e)
        {
            ProcessedTime.IsEnabled = false;
            List<string> p = new List<string>();
            foreach (var s in lbProcessedData2.SelectedItems)
                p.Add(s.ToString());
            theGraph.DisplayProcessed(p.ToArray());
        }
        private void lbProcessedData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ProcessedTime.Start();
            ProcessedTime.IsEnabled = true;
        }

        private void btnSaveNote_Click2(object sender, RoutedEventArgs e)
        {

        }

        private DataModel.DataTrace CurrentTrace;
        private void HandleDataContext()
        {
            if (DataContext != null)
            {
                CurrentTrace = (DataModel.DataTrace)((DataTraceVM)DataContext).DataTrace;
                theGraph.CurrentTrace = CurrentTrace;
                if (CurrentTrace.Note != null)
                {
                    NewNote.Document = CurrentTrace.Note.Document;
                }
            }
        }

        private void StartTime_Tick(object sender, EventArgs e)
        {
            StartTime.IsEnabled = false;
            HandleDataContext();
        }

        DispatcherTimer StartTime = new DispatcherTimer();
        DispatcherTimer ProcessedTime = new DispatcherTimer();
        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

            StartTime.Start();
            StartTime.IsEnabled = true;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

            StartTime.Start();
            StartTime.IsEnabled = true;

        }

        private void btnNewMeasurement_Click(object sender, RoutedEventArgs e)
        {
            ((DataTraceVM)DataContext).RepeatMeasurementRequested();
        }

        DataModel.Fileserver.FileTrace soundTrace = null;
        private void btnPlaySound_Click(object sender, RoutedEventArgs e)
        {
            List<string> p = new List<string>();
            foreach (var s in lbProcessedData2.SelectedItems)
                p.Add(s.ToString());
            if (p.Count == 0)
            {
                soundTrace = ((DataTraceVM)DataContext).DataTrace.Current;
                soundTrace.PlayAsSound();
            }
            else
            {
                soundTrace = ((DataTraceVM)DataContext).DataTrace.GetProcessed(p[0]);
                soundTrace.PlayAsSound();
            }
        }

        private void btnStopSound_Click(object sender, RoutedEventArgs e)
        {
            if (soundTrace != null)
                soundTrace.StopSound();
        }

        private void btnFlipReferences_Click(object sender, RoutedEventArgs e)
        {
            DataModel.LabContext context = new DataModel.LabContext();
            {
                var data = ((DataTraceVM)DataContext).DataTrace.Chip;
                var traces = (from x in context.Traces where x.Chip.Id == data.Id select x);
                foreach (var trace in traces)
                {
                    var top = trace.TopReference_mV;
                    trace.TopReference_mV = trace.BottomReference_mV;
                    trace.BottomReference_mV = top;
                }
                context.SaveChanges();

                var trace2 = (from x in context.Traces where x.Id == ((DataTraceVM)DataContext).DataTrace.Id select x).FirstOrDefault();

                DataContext = new DataTraceVM(trace2);
                //((DataTraceVM)DataContext).DataTrace = trace2;
            }
        }

      
    }
}
