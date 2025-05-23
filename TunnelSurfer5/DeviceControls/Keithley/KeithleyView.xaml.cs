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

namespace TunnelVision.DeviceControls.Keithley
{
    /// <summary>
    /// Interaction logic for bDAQView.xaml
    /// </summary>
    public partial class KeithleyView : UserControl
    {
        public KeithleyView()
        {
            InitializeComponent();
            DataContextChanged += BDAQView_DataContextChanged;
        }

        private void BDAQView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            theDataRig = (KeithleyVM)DataContext;
            if (theDataRig != null)
                theDataRig.DataFinished += NewTrace_DataFinished;
        }

        KeithleyVM theDataRig = null;

        private void lbTopVoltage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            theDataRig.VBias0 = double.Parse(lbTopVoltage.SelectedValue.ToString());
        }

        private void lbBottomVoltage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            theDataRig.VClamp0 = double.Parse(lbBottomVoltage.SelectedValue.ToString());
        }

        private void lbTraceVoltageBot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            theDataRig.VBias1 = double.Parse(lbTraceVoltageBot.SelectedValue.ToString());
        }
        private void lbTraceVoltage1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            theDataRig.VClamp1 = double.Parse(lbTraceVoltage1.SelectedValue.ToString());
        }

        private void lbTraceVoltage2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            theDataRig.VBias1 = double.Parse(lbTraceVoltage1.SelectedValue.ToString());
        }

        private void lbSensitivity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            theDataRig.Sensitivity = lbSensitivity.SelectedValue.ToString();
        }

        private void lbInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            theDataRig.Interval = lbSampleInterval.SelectedValue.ToString();
        }

        private void btnStartData_Click(object sender, RoutedEventArgs e)
        {
            theDataRig.Editor = NewNote;
            //btnSave.IsEnabled = false;
            theDataRig.DataFinished += NewTrace_DataFinished;
            theDataRig.DataTraceVM.DoRun();
        }

        private void NewTrace_DataFinished(string filename)
        {
            btnExport.Visibility = Visibility.Visible;
            btnExport.IsEnabled = true;
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            DateTime dt = DateTime.Now;
            var CurrentTrace = theDataRig.DataTraceVM.DataTrace;
            string file = dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + "_" + CurrentTrace.Chip.Experiment.ExperimentName + "_" + CurrentTrace.Chip.Batch.BatchName + "_" +
                  CurrentTrace.Chip.ChipName;

            file += "_" + theDataRig.DataTraceVM.Concentration + "mM" + theDataRig.DataTraceVM.Analyte + "_" + tbTraceName.Text;

            System.Windows.Forms.SaveFileDialog saveFile = new System.Windows.Forms.SaveFileDialog();
            saveFile.FileName = System.IO.Path.GetFileName(file);
            var ret = saveFile.ShowDialog();
            if (ret != System.Windows.Forms.DialogResult.Cancel)
            {
                file += ".bDAQ";
                theDataRig.ExportFile(saveFile.FileName);
            }
        }

       
    }
}
