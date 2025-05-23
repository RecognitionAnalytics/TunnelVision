using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class NewIVTrace : UserControl, INotifyPropertyChanged
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

        private List<DeviceControls.BaseDeviceVM> MeasurementTools = new List<DeviceControls.BaseDeviceVM>();

        public NewIVTrace()
        {
            InitializeComponent();
            MeasurementTools.Add(new DeviceControls.AxomVM());
            MeasurementTools.Add(new DeviceControls.bDAQVM());
            MeasurementTools.Add(new DeviceControls.KeithleyVM());

            foreach (var mt in MeasurementTools)
                 mt.DataFinished += NewIVTrace_DataFinished;

            lbMeasureTool.ItemsSource = MeasurementTools;
        }

       
        #endregion



        #region Loading datacontext
        private NewIVTraceVM DataTraceVM;
        private DataModel.IVTrace _CurrentTrace;
        public DataModel.IVTrace CurrentTrace
        {
            get { return _CurrentTrace; }
            set
            {
                _CurrentTrace = value;
                NotifyPropertyChanged("CurrentTrace");
            }
        }

        private void HandleDataContext()
        {
            try
            {
                DataTraceVM = (NewIVTraceVM)DataContext;
                DataTraceVM.theDataRig = MeasurementTools[1];
                CurrentTrace = DataTraceVM.IVTrace;
                theGraph.CurrentTrace = CurrentTrace;
                if (CurrentTrace.Note != null)
                {
                    NewNote.Document = CurrentTrace.Note.Document;
                }
            }
            catch { }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            HandleDataContext();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            HandleDataContext();
        }
        #endregion


        #region Dirty code behind
        private void lbMeasureTool_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataTraceVM.theDataRig = (DeviceControls.BaseDeviceVM)lbMeasureTool.SelectedValue;
        }
        private void btnSaveNote_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentTrace != null)
                CurrentTrace.SaveNote(new DataModel.eNote(NewNote.PlainText, NewNote.Document));
        }
        private void lbTopVoltage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataTraceVM.TopReference = lbTopVoltage.SelectedValue.ToString();
        }

      

        private void lbSampleRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataTraceVM.SampleRate = lbSampleRate.SelectedValue.ToString();
        }

        private void lbTraceVoltage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataTraceVM.TunnelVoltage = lbTraceVoltage.SelectedValue.ToString();
        }

      
        #endregion

        private void btnStartData_Click(object sender, RoutedEventArgs e)
        {
            btnSave.IsEnabled = false;
            DataTraceVM.DoRun(this.theGraph);

            //DateTime dt = DateTime.Now;
            //string folder = @"\\biofs\smb\Research\TunnelSurfer\\experiments\\" + CurrentTrace.Chip.Experiment.ExperimentName + "\\" + CurrentTrace.Chip.Batch.BatchName + "\\" + CurrentTrace.Chip.ChipName + "\\" + CurrentTrace.Junction.JunctionName;

            //string file = dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + "_" + CurrentTrace.Chip.Experiment.ExperimentName + "_" + CurrentTrace.Chip.Batch.BatchName + "_" +
            //    CurrentTrace.Chip.ChipName + "_" + CurrentTrace.Junction.JunctionName;
            //file += "_" + tbConcentration.Text + "mM" + tbAnalyte.Text + "_" + tbBuffer.Text + "_" + tbTopVoltage.Text + "_" + tbBottomVoltage.Text + "_" + tbTraceVoltage.Text;


            //if (lbMeasureTool.SelectedValue.ToString().ToLower() == "axon")
            //{

            //}

            //if (lbMeasureTool.SelectedValue.ToString().ToLower() == "bdaq")
            //{



            //    btnStartData.IsEnabled = false;
            //}


        }

        #region SaveData

        string tempFile = "";
        private void NewIVTrace_DataFinished(string filename)
        {
            tempFile = filename;
            btnSave.Visibility = Visibility.Visible;
        }

        private DataModel.IonicTrace SaveDatabase(DataModel.Chip chip, string rawfilename)
        {
            DataModel.IonicTrace tunnel = new DataModel.IonicTrace
            {
                Chip = chip,
                BottomReference_mV = 0,
                TopReference_mV = DataModel.ExistingParse.ConvertVoltageToDouble(tbTopVoltage.Text),
                HasIonic = true,
                Analyte = tbBuffer.Text,
                Buffer = tbBuffer.Text,
                Concentration_mM = double.Parse(tbConcentration.Text),
                DataRig = lbMeasureTool.SelectedValue.ToString(),
                Note = new DataModel.eNote(NewNote.PlainText, NewNote.Document),
                OtherInfo = "",
                IsControl = (tbTraceName.Text.Contains("control") == true),
                TraceName = tbTraceName.Text,
                Tunnel_mV = DataModel.ExistingParse.ConvertVoltageToDouble(tbTraceVoltage.Text),
                DateAcquired = DateTime.Now,
                Filename = rawfilename,
                Folder = chip.Folder
            };

            tunnel.Condition = DataModel.ExistingParse.ConditionString(tunnel.TopReference_mV, tunnel.BottomReference_mV, tunnel.Tunnel_mV);

            return tunnel;
        }

        private DataModel.TunnelingTrace SaveDatabase(DataModel.Chip chip, DataModel.Junction junction, string rawfilename)
        {
            DataModel.TunnelingTrace tunnel = new DataModel.TunnelingTrace
            {
                Chip = chip,
                Junction = junction,
                BottomReference_mV = 0,
                TopReference_mV = DataModel.ExistingParse.ConvertVoltageToDouble(tbTopVoltage.Text),
                HasIonic = true,
                Analyte = tbBuffer.Text,
                Buffer = tbBuffer.Text,
                Concentration_mM = double.Parse(tbConcentration.Text),
                DataRig = lbMeasureTool.SelectedValue.ToString(),
                Note = new DataModel.eNote(NewNote.PlainText, NewNote.Document),
                OtherInfo = "",
                IsControl = (tbTraceName.Text.Contains("control") == true),
                TraceName = tbTraceName.Text,
                Tunnel_mV = DataModel.ExistingParse.ConvertVoltageToDouble(tbTraceVoltage.Text),
                DateAcquired = DateTime.Now,
                Filename = rawfilename,
                Folder = junction.Folder
            };

            tunnel.Condition = DataModel.ExistingParse.ConditionString(tunnel.TopReference_mV, tunnel.BottomReference_mV, tunnel.Tunnel_mV);

            return tunnel;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            DateTime dt = DateTime.Now;
            string file = dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + "_" + CurrentTrace.Chip.Experiment.ExperimentName + "_" + CurrentTrace.Chip.Batch.BatchName + "_" +
                  CurrentTrace.Chip.ChipName;

            file += "_" + tbConcentration.Text + "mM" + tbBuffer.Text + "_" + tbTraceName.Text;

            if (lbMeasureTool.SelectedValue.ToString().ToLower() == "bdaq")
            {
                file += ".bDAQ";
                //_BDAQ.SaveFile(CurrentTrace.Folder, file);

                using (DataModel.LabContext lContext = new DataModel.LabContext())
                {
                    var chip = (from x in lContext.Chips where x.Id == CurrentTrace.Chip.Id select x).FirstOrDefault();
                    if (DataTraceVM.theDataRig.TraceScript.ToLower().Contains("ionic") == true)
                    {
                        chip.IonicIV.Add(SaveDatabase(chip, file));
                    }
                    else
                    {
                        var junc = (from x in lContext.Junctions where x.Id == CurrentTrace.Junction.Id select x).FirstOrDefault();
                        junc.TunnelIV.Add(SaveDatabase(chip, junc, tempFile));
                    }
                    lContext.SaveChanges();
                }
            }
            else
            {
                file = CurrentTrace.Junction.Folder + "\\" + file;
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            DateTime dt = DateTime.Now;
            string file = dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + "_" + CurrentTrace.Chip.Experiment.ExperimentName + "_" + CurrentTrace.Chip.Batch.BatchName + "_" +
                  CurrentTrace.Chip.ChipName;

            file += "_" + tbConcentration.Text + "mM" + tbBuffer.Text + "_" + tbTraceName.Text;

            System.Windows.Forms.SaveFileDialog saveFile = new System.Windows.Forms.SaveFileDialog();
            saveFile.FileName = System.IO.Path.GetFileName(file);
            var ret = saveFile.ShowDialog();
            if (ret != System.Windows.Forms.DialogResult.Cancel)
            {
                if (lbMeasureTool.SelectedValue.ToString().ToLower() == "bdaq")
                {
                    file += ".bDAQ";
                    //_BDAQ.SaveFile(System.IO.Path.GetDirectoryName(saveFile.FileName), System.IO.Path.GetFileNameWithoutExtension(saveFile.FileName) + ".bdaq");
                }
            }
        }
        #endregion

       

        private void lbProcessedData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            DataTraceVM.theDataRig.KillDevice();
        }
    }
}
