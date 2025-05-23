using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace TunnelVision.DeviceControls.bDAQ
{
    /// <summary>
    /// Interaction logic for bDAQView.xaml
    /// </summary>
    public partial class bDAQView : UserControl
    {
        public bDAQView()
        {
            InitializeComponent();
            DataContextChanged += BDAQView_DataContextChanged;


            //lbProcessedData.SelectedItems.Add(lbProcessedData.Items.GetItemAt(0));
            //lbProcessedData.SelectedItems.Add(lbProcessedData.Items.GetItemAt(2));
            //lbProcessedData.SelectedItems.Add(lbProcessedData.Items.GetItemAt(3));

            //lbJunctionData.SelectedItems.Add(lbJunctionData.Items.GetItemAt(0));
            //lbJunctionData.SelectedItems.Add(lbJunctionData.Items.GetItemAt(2));
            //lbJunctionData.SelectedItems.Add(lbJunctionData.Items.GetItemAt(3));

        }

        private void BDAQView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            theDataRig = (bDAQVM)DataContext;
            if (theDataRig!=null)
                theDataRig.DataFinished += NewTrace_DataFinished;
        }

        bDAQVM theDataRig = null;


        private void lbProcessedData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<int> items = new List<int>();

            var sItems = lbProcessedData.SelectedItems;
            for (int i = 0; i < lbProcessedData.Items.Count; i++)
            {
                if (sItems.Contains(lbProcessedData.Items[i]))
                    items.Add(i);
            }
            theDataRig.DisplayChannels = items.ToArray();
        }

        private void lbJunctionData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<string> items = new List<string>();
            foreach (var s in lbJunctionData.SelectedItems)
                items.Add(s.ToString());
            //theDataRig.AvailableJunctions = items.ToArray();
            theDataRig.Junctions = items.ToArray();
        }

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

        private void lbRunTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                theDataRig.RunTime = double.Parse(lbRunTime.SelectedValue.ToString());
            }
            catch { }
        }

        private void lbTraceVoltage1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            theDataRig.VClamp1 = double.Parse(lbTraceVoltage1.SelectedValue.ToString());
        }

        private void lbTraceVoltage2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            theDataRig.VClamp2 = double.Parse(lbTraceVoltage2.SelectedValue.ToString());
        }

        private void lbTraceVoltage3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            theDataRig.VClamp3 = double.Parse(lbTraceVoltage3.SelectedValue.ToString());
        }

        private bool _DataSaved = false;
        private void btnStartData_Click(object sender, RoutedEventArgs e)
        {
            _DataSaved = false;
            theDataRig.Editor = NewNote;
            btnStartData.IsEnabled = false;
            theDataRig.DataFinished += NewTrace_DataFinished;

            theDataRig.DataTraceVM.DoRun();

        }

        private void NewTrace_DataFinished(string filename)
        {
            //  btnSave.Visibility = Visibility.Visible;
            btnStartData.IsEnabled = true;
            if (_DataSaved == false)
            {
                _DataSaved = true;
                Thread.Sleep(100);
                theDataRig.Save(new DataModel.eNote(NewNote.PlainText, NewNote.Document));
            }
        }


        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (theDataRig.DisplayChannels.Length != theDataRig.Junctions.Length)
            {
                MessageBox.Show("Please select the same number of channels as junctions to perform assignment");
            }
            else 
                theDataRig.Save(new DataModel.eNote(NewNote.PlainText, NewNote.Document));
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
                theDataRig .ExportFile(System.IO.Path.GetDirectoryName(saveFile.FileName), System.IO.Path.GetFileNameWithoutExtension(saveFile.FileName) + ".bdaq");
            }
        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            theDataRig.KillDevice();
        }

        private void btnSetToZero_Click(object sender, RoutedEventArgs e)
        {
            theDataRig.ZeroVoltages();
        }

        private void btnStartVoltages_Click(object sender, RoutedEventArgs e)
        {
            theDataRig.StartVoltages();
        }

        private void btnCalibrate_Click(object sender, RoutedEventArgs e)
        {
            theDataRig.Calibrate();
        }
    }
}
