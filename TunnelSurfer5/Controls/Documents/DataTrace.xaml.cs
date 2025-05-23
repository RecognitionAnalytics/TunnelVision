using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TunnelVision
{
    /// <summary>
    /// Interaction logic for DataTrace.xaml
    /// </summary>
    public partial class DataTrace : UserControl, INotifyPropertyChanged
    {
        #region Housework
        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

       
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
           
        }

        public DataTrace()
        {
            InitializeComponent();
            
            //worker_ProcessedData.DoWork += worker_ProcessedData_DoWork;
            //worker_ProcessedData.RunWorkerCompleted += worker_ProcessedData_RunWorkerCompleted;
            
            //dispatcherTimer2.Tick += dispatcherTimer2_Tick;
        }


        #endregion


      


      


        //public void SetData(DataModel.DataTrace trace)
        //{
        //    _NewData = true;

        //    CommonExtensions.DoEvents();
        //    if (CurrentTrace != null)
        //        CurrentTrace.ClearMemory();

        //    CurrentTrace = trace;

        //    //if (CurrentTrace.Id == 0)
        //    //{
        //      ViewPanel.Visibility = System.Windows.Visibility.Collapsed;
        //    AutomationPanel.Visibility = System.Windows.Visibility.Visible;
        //    //}
        //    //else
        //    //{
        //    //    ViewPanel.Visibility = System.Windows.Visibility.Visible;
        //    //    AutomationPanel.Visibility = System.Windows.Visibility.Collapsed;
        //    //}

        //    if (CurrentTrace.Id == -1)
        //    {
        //        spYate.Visibility = System.Windows.Visibility.Collapsed;
        //    }
        //    if (CurrentTrace.Note != null)
        //    {
        //        NewNote.Document = CurrentTrace.Note.Document;
        //        NewNote2.Document = CurrentTrace.Note.Document;
        //    }
        //    NotifyPropertyChanged("CurrentTrace");
        //    RequestedProcessed.Clear();

        //    tbAnalyte.Text = CurrentTrace.Analyte;
        //    tbConcentration.Text = CurrentTrace.Concentration_mM.ToString();

        //    tbTraceName.Text = CurrentTrace.TraceName;

        //    tbTopVoltage.Text = CurrentTrace.TopRef_mV_ToString;
        //    tbTraceVoltage.Text = CurrentTrace.Tunnel_mV_ToString;
        //    tbBottomVoltage.Text = CurrentTrace.BottomRef_mV_ToString;

        //    try
        //    {
        //        if (CurrentTrace.Buffer != null)
        //            tbBuffer.Text = CurrentTrace.Buffer.ToString();
        //    }
        //    catch { }
        //    tbRunTime.Text = (Math.Round(CurrentTrace.RunTime / 60, 1)).ToString();
        //    tbSampleRate.Text = (CurrentTrace.SampleRate / 1000).ToString();

        //    chart1.Series.Clear();
        //    chartHist.Series.Clear();

        //    if (File.Exists(App.ConvertFilePaths(CurrentTrace.ProcessedFile + "_ExampleGraph.png")) == true && CurrentTrace.Id != -1)
        //    {
        //        hostImage.Visibility = Visibility.Visible;
        //        host.Visibility = Visibility.Hidden;
        //        CopyButtons.Visibility = Visibility.Collapsed;
        //        ShowData.Visibility = Visibility.Visible;
        //        FakeChart1.Source = new BitmapImage(new Uri(App.ConvertFilePaths(CurrentTrace.ProcessedFile + "_ExampleGraph.png"), UriKind.Absolute));
        //        App.DoneDrawing = true;
        //    }
        //    else
        //    {
        //        ShowData_Click(this, null);
        //    }


        //    if (CurrentTrace != null && CurrentTrace.ProcessedData != null)
        //    {
        //        List<string> Items = new List<string>();
        //        foreach (var s in CurrentTrace.ProcessedData)
        //        {
        //            Items.Add(s);
        //        }
        //        ProcessedData = Items.ToArray();
        //    }

        //}

       



        //System.Windows.Threading.DispatcherTimer dispatcherTimer2 = new System.Windows.Threading.DispatcherTimer();
        

       
      

       

        //#region ProcessedData
        //private readonly BackgroundWorker worker_ProcessedData = new BackgroundWorker();

        //private List<string> RequestedProcessed = new List<string>();
        //private void worker_ProcessedData_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    var t = CurrentTrace.Time;
        //    for (int i = 0; i < RequestedProcessed.Count; i++)
        //    {
        //        try
        //        {
        //            string itemS = RequestedProcessed[i].ToString();
        //            if (itemS.ToLower() == "raw")
        //            {
        //                var c = CurrentTrace.Current;
        //            }
        //            else
        //            {
        //                var v = CurrentTrace.GetProcessed(itemS);
        //            }
        //        }
        //        catch { }
        //    }

        //}

        //private void worker_ProcessedData_RunWorkerCompleted(object sender,
        //                               RunWorkerCompletedEventArgs e)
        //{
        //    if (CancelRequest == false)
        //    {
        //        dispatcherTimer2.IsEnabled = false;
        //        // RequestedProcessed.Reverse();
        //        List<string> nList = new List<string>(RequestedProcessed);
        //        foreach (var item in nList)
        //        {
        //            string itemS = item.ToString();
        //            try
        //            {
        //                if (itemS.ToLower() == "raw")
        //                    CheckAndAddSeriesToGraph("Raw", "pA", CurrentTrace.Time, CurrentTrace.Current);
        //                else if (itemS.ToLower() == "voltage")
        //                {
        //                    CheckAndAddSeriesToGraph(itemS, "mV", CurrentTrace.Time, CurrentTrace.GetProcessed(itemS));
        //                }
        //                else
        //                {
        //                    CheckAndAddSeriesToGraph(itemS, "pA", CurrentTrace.Time, CurrentTrace.GetProcessed(itemS));
        //                }
        //            }
        //            catch { }
        //        }
        //        if (RequestedProcessed.Count > 1)
        //        {
        //            foreach (var s in chart1.Series)
        //                s.IsVisibleInLegend = true;
        //        }
        //        DrawHistogram();
        //        LoadBarTimer.IsEnabled = false;
        //    }
        //    else
        //    {
        //        CancelRequest = false;
        //        worker_ProcessedData.RunWorkerAsync();
        //    }
        //}

        //void dispatcherTimer2_Tick(object sender, EventArgs e)
        //{
        //    dispatcherTimer2.IsEnabled = false;
        //    dispatcherTimer2.Stop();

        //    List<string> selected = new List<string>();
        //    ClearSeriesFromGraph();
        //    RequestedProcessed.Clear();
        //    if (ActiveProcessed != null)
        //    {
        //        foreach (var item in ActiveProcessed.SelectedItems)
        //        {
        //            RequestedProcessed.Add(item.ToString());
        //        }
        //    }
        //    else
        //    {
        //        LoadBarTimer.IsEnabled = true;
        //        ShowData_Click(this, null);
        //        return;
        //    }

        //    LoadBarTimer.Start();
        //    if (worker_ProcessedData.IsBusy == false)
        //        worker_ProcessedData.RunWorkerAsync();

        //}
        //private bool CancelRequest = false;
        //ListBox ActiveProcessed = null;
        //private void lbProcessedData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    ActiveProcessed = (ListBox)sender;
        //    List<string> selected = new List<string>();
        //    foreach (var item in ActiveProcessed.SelectedItems)
        //    {
        //        selected.Add(item.ToString());
        //    }

        //    hostImage.Visibility = Visibility.Hidden;
        //    host.Visibility = Visibility.Visible;
        //    CopyButtons.Visibility = Visibility.Visible;
        //    ShowData.Visibility = Visibility.Collapsed;

        //    if (selected.Count > 0)
        //    {
        //        if (worker_ProcessedData.IsBusy == true)
        //            CancelRequest = true;
        //        // worker2.CancelAsync();
        //        var a = new HashSet<string>(selected).SetEquals(RequestedProcessed);
        //        if (a == false)
        //        {
        //            dispatcherTimer2.Interval = new TimeSpan(0, 0, 0, 0, 500);
        //            dispatcherTimer2.IsEnabled = true;
        //            dispatcherTimer2.Start();
        //        }
        //    }
        //}
        //#endregion

   
       

      
      
      


       
    }
}