using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using TunnelVision.Controls.TreeViewModel;

namespace TunnelVision.Controls.ViewControl
{
    /// <summary>
    /// Interaction logic for NewTrace.xaml
    /// </summary>
    public partial class NewTrace : UserControl, INotifyPropertyChanged
    {
        #region Housekeeping
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {

        }
        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private List<string> MeasurementTools = new List<string>();

        public NewTrace()
        {
            InitializeComponent();
          

        }
        #endregion

        #region Loading datacontext
        private NewDataTraceVM DataTraceVM;
        private DataModel.DataTrace _CurrentTrace;
        public DataModel.DataTrace CurrentTrace
        {
            get { return _CurrentTrace; }
            set
            {
                _CurrentTrace = value;
                NotifyPropertyChanged("CurrentTrace");
            }
        }

        private Dictionary<string, DeviceControls.BaseDeviceVM> DeviceCatalog = new Dictionary<string, DeviceControls.BaseDeviceVM>();
        private void HandleDataContext()
        {
            try
            {
                DataTraceVM = (NewDataTraceVM)DataContext;
                var bDAQ = new DeviceControls.bDAQ.bDAQVM();

                DeviceCatalog.Add("Axon", new DeviceControls.Axon.AxonVM());
                DeviceCatalog.Add("bDAQ", bDAQ);
                DeviceCatalog.Add("Keithley", new DeviceControls.Keithley.KeithleyVM());
                MeasurementTools.Add("Axon");
                MeasurementTools.Add("bDAQ");
                MeasurementTools.Add("Keithley");

               // foreach (var mt in MeasurementTools)
               
                DataTraceVM.Graph = theGraph;
                DataTraceVM.theDataRig = bDAQ;
                lbMeasureTool.ItemsSource = MeasurementTools;
                lbMeasureTool.SelectedIndex = 1;
                
                CurrentTrace = DataTraceVM.DataTrace;
                theGraph.CurrentTrace = CurrentTrace;
                if (CurrentTrace.Note != null)
                {
                  //  NewNote.Document = CurrentTrace.Note.Document;
                }
            }
            catch { }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsLoaded)
              HandleDataContext();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            HandleDataContext();
            CommonExtensions.DoEvents();
            lbMeasureTool.SelectedIndex = 1;
        }
        #endregion

        #region Dirty code behind
        private void lbMeasureTool_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataTraceVM!=null)
                DataTraceVM.theDataRig = (DeviceControls.BaseDeviceVM)DeviceCatalog[lbMeasureTool.SelectedValue.ToString()];
        }
       
       
        #endregion
    }
}
