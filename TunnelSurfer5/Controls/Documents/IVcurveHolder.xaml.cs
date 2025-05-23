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
using TunnelVision.DataModel.Fileserver;

namespace TunnelVision
{
    /// <summary>
    /// Interaction logic for IVcurveHolder.xaml
    /// </summary>
    public partial class IVcurveHolder : UserControl
    {
        DispatcherTimer Delay = new DispatcherTimer();
        public DataModel.QualityControlStep ControlStep()
        {

            return (DataModel.QualityControlStep)lbQCStep.SelectedIndex;
        }
        ~IVcurveHolder()
        {
            if (currentTrace != null)
                currentTrace.ClearMemory();
        }
        public IVcurveHolder()
        {
            InitializeComponent();
            SetupChartProperties(chart1, "Voltage (mV)", "Current (pA)");
            SetupChartProperties(chart2, "Time(s)", "Current (pA)");

            lbDataRig.SelectedIndex = 1;
            lbQCStep.SelectedIndex = 1;
            Delay.Tick += Delay_Tick;
            Delay.Interval = new TimeSpan(0, 0, 1);
        }

        private void Delay_Tick(object sender, EventArgs e)
        {
            Delay.IsEnabled = false;
            var VM = (Controls.TreeViewModel.IVTraceVM)DataContext;
            if (VM != null)
            {
                currentTrace = VM.IVTrace;
                SetData(currentTrace);
            }
        }

        private DataModel.IVTrace currentTrace = null;
        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Delay.IsEnabled = true;
            Delay.Start();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Delay.IsEnabled = true;
            Delay.Start();
        }
        public void SetData(DataModel.IVTrace trace)
        {
            if (currentTrace != null)
                currentTrace.ClearMemory();

            currentTrace = trace;


            chart1.Series.Clear();

            var current = currentTrace.Current;

            tbTraceName.Text = trace.TraceName;
            tbNotes.Text = trace.Note.ShortNote;
            tbConcentration.Text = trace.Concentration_mM.ToString();
            tbBuffer.Text = trace.Buffer;

            var voltage = currentTrace.GetProcessed("Voltage");
            if (voltage == null)
            {
                voltage = currentTrace.GetProcessed("VoltageR");
                if (voltage != null)
                {
                    var mVolt = ((int)Math.Round(voltage.Trace.Max() / 100) * 100).ToString();
                    for (int i = 0; i < lbVoltage.Items.Count; i++)
                        if (mVolt == lbVoltage.Items[i].ToString())
                            lbVoltage.SelectedIndex = i;


                    string[] Files = currentTrace.GetSimplified();
                    foreach (string f in Files)
                    {
                        var cur = currentTrace.GetProcessed(f);
                        CheckAndAddSeriesToGraph(chart1, f, voltage.Trace, cur.Trace);
                    }
                    var currentFit = currentTrace.GetProcessed("CurveFit");
                    if (currentFit != null)
                        CheckAndAddSeriesToGraph(chart1, "Fit", voltage.Trace, currentFit.Trace);
                }
            }
            else            if (voltage != null)
            {
                var mVolt = ((int)Math.Round(voltage.Trace.Max() / 100) * 100).ToString();
                for (int i = 0; i < lbVoltage.Items.Count; i++)
                    if (mVolt == lbVoltage.Items[i].ToString())
                        lbVoltage.SelectedIndex = i;

                CheckAndAddSeriesToGraph(chart1, "Raw", voltage.Trace, current.Trace);

                var currentFit = currentTrace.GetProcessed("CurveFit");
                if (currentFit != null)
                    CheckAndAddSeriesToGraph(chart1, "Fit", voltage.Trace, currentFit.Trace);
            }
            else
            {
                lbVoltage.SelectedIndex = 2;
                lbSweep.SelectedIndex = 1;
                tbConcentration.Text = "100";
                tbBuffer.Text = "PB";
            }


            if (trace.GetType().ToString().ToLower().Contains("ionictrace"))
            {
                lbScripts.SelectedIndex = 0;
                DataModel.IonicTrace it = (DataModel.IonicTrace)trace;
                infoPanel1.Visibility = System.Windows.Visibility.Visible;
                infoPanel2.Visibility = System.Windows.Visibility.Visible;
                tbFitInfo1.Text = string.Format("Conductance  = {0:0.00} nS \nCapacitance = {1:0.00} nF \ndV/dt = {2:0.00} mV/s\n\n\nPore Size = {3:0} nm", it.Conductance * 1e9, it.Capacitance, it.dVdt,
                    it.AccessPoreSize);
                tbFitInfo2.Text = string.Format("Smeet data:\nExpected conductance {0:0.00} nS\nPore size = {1:0} nm", it.ExpectedSmeetConductance / 1000, it.SmeetPoreSize);
            }
            else
            {
                lbScripts.SelectedIndex = 1;
                DataModel.TunnelingTrace it = (DataModel.TunnelingTrace)trace;
                infoPanel1.Visibility = System.Windows.Visibility.Visible;
                infoPanel2.Visibility = System.Windows.Visibility.Collapsed;
                tbFitInfo1.Text = string.Format("Gap Size = {0:0} nm\nGap Potential = {1:0.0} eV \nDielectric Constant = {2:0.0} \nJunction Area = {3:0.0} square um",
                    it.GapSize, it.GapPotential, it.Dielectric, it.JunctionArea);
            }
        }

        private void ChartCursorSelected(Chart sender, ChartCursor e)
        {
            txtChartSelect.Text = e.X.ToString("F4") + ", " + e.Y.ToString("F4");
          //  Debug.WriteLine("Cursor Position: " + txtChartSelect.Text + " @ " + e.ChartArea.Name);

            PointF diff = sender.CursorsDiff();
            txtChartSelect.Text = diff.X.ToString("F4") + ", " + diff.Y.ToString("F4");
        }

        private void ChartCursorMoved(Chart sender, ChartCursor e)
        {
            txtChartValue.Text = e.X.ToString("F4") + ", " + e.Y.ToString("F4");
        }

       

        private void SetupChartProperties(Chart chart1, string xlabel, string ylabel)
        {
            //customize the X-Axis to properly display Time 
            //chart1.Customize += chart1_Customize;
            chart1.Series.Clear(); //first remove all series completely
            chart1.BackColor = System.Drawing.Color.FromArgb(54, 54, 54);
            chart1.ForeColor = System.Drawing.Color.White;
            chart1.ChartAreas[0].AxisX.LabelStyle.ForeColor = System.Drawing.Color.White;
            chart1.ChartAreas[0].AxisY.LabelStyle.ForeColor = System.Drawing.Color.White;
            //// Enable all elements
            chart1.ChartAreas[0].AxisX.MinorGrid.Enabled = true;
            chart1.ChartAreas[0].AxisX.MinorTickMark.Enabled = true;
            chart1.ChartAreas[0].AxisX.MinorTickMark.Interval = 50;
            chart1.ChartAreas[0].BackColor = System.Drawing.Color.FromArgb(44, 44, 44);
            chart1.ChartAreas[0].AxisX.LineColor = System.Drawing.Color.White;
            chart1.ChartAreas[0].AxisY.LineColor = System.Drawing.Color.White;
            chart1.ChartAreas[0].AxisY.Title = ylabel;
            chart1.ChartAreas[0].AxisX.Title = xlabel;
            chart1.ChartAreas[0].AxisY.TitleForeColor = System.Drawing.Color.White;
            chart1.ChartAreas[0].AxisX.TitleForeColor = System.Drawing.Color.White;
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "#.###";
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "#.###";
            // Set Grid lines and tick marks interval
            chart1.ChartAreas[0].AxisX.MajorGrid.Interval = 100;
            chart1.ChartAreas[0].AxisX.MajorTickMark.Interval = 100;
           
            //set legend position and properties as required
            chart1.Legends[0].LegendStyle = LegendStyle.Table;
            chart1.Legends[0].ForeColor = System.Drawing.Color.White;
            // Set table style if legend style is Table
            chart1.Legends[0].TableStyle = LegendTableStyle.Auto;

            // Set legend docking
            chart1.Legends[0].Docking = Docking.Top;

            // Set legend alignment
            chart1.Legends[0].Alignment = StringAlignment.Center;

            // Set Antialiasing mode
            //this can be set lower if there are any performance issues!
            chart1.AntiAliasing = AntiAliasingStyles.Text;
            chart1.TextAntiAliasingQuality = TextAntiAliasingQuality.Normal;
            chart1.EnableZoomAndPanControls(ChartCursorSelected, ChartCursorMoved,
                           zoomChanged,
                           new ChartOption()
                           {
                               ContextMenuAllowToHideSeries = true,
                               XAxisPrecision = 0,
                               YAxisPrecision = 2
                           });

            // Client interface BUG:
            // OnAxisViewChang* is only called on Cursor_MouseUp, 
            //  so the following events are never raised
            chart1.AxisViewChanging += OnAxisViewChanges;
            chart1.AxisViewChanged += OnAxisViewChanges;
        }
        private void zoomChanged(Chart sender)
        {
        }
        private void OnAxisViewChanges(object sender, ViewEventArgs viewEventArgs)
        {
           // Debug.Fail("Don't worry, this event is never raised.");
        }

        public void CheckAndAddSeriesToGraph(Chart chart1, string SeriesName, double[] xValues, double[] yValues, SeriesChartType chartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line)
        {
            foreach (Series se in chart1.Series)
            {
                if (se.Name == SeriesName)
                {
                    return; //already exists
                }
            }
            Series s = chart1.Series.Add(SeriesName);
            s.ChartType = chartType;
            s.BorderColor = System.Drawing.Color.FromArgb(180, 26, 59, 105);
            s.BorderWidth = 2; // show a THICK line for high visibility, can be reduced for high volume data points to be better visible
            s.ShadowOffset = 1;
            s.IsVisibleInLegend = false;
            //s.IsValueShownAsLabel = true;                       
            s.LegendText = SeriesName;
            s.LegendToolTip = SeriesName;
            s.Points.SuspendUpdates();

            int step = 1;
            try
            {
                for (int i = 0; i < xValues.Length; i += step)
                {
                    double y = yValues[i]; double x = xValues[i];
                    if (double.IsInfinity(y) || double.IsNaN(y) || Math.Abs(y) > 1e9)
                        y = 0;
                    if (double.IsInfinity(x) || double.IsNaN(x) || Math.Abs(x) > 1e9)
                        x = 0;

                    s.Points.AddXY(x, y);

                    if ((i % 100000) == 0)
                    {
                        CommonExtensions.DoEvents();
                    }
                }
            }
            catch { }
            s.Points.ResumeUpdates();

        }

        public void CheckAndAddSeriesToGraph(Chart chart1, string SeriesName, DataModel.Fileserver.FileTrace trace, SeriesChartType chartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line)
        {
            foreach (Series se in chart1.Series)
            {
                if (se.Name == SeriesName)
                {
                    return; //already exists
                }
            }
            Series s = chart1.Series.Add(SeriesName);
            s.ChartType = chartType;
            s.BorderColor = System.Drawing.Color.FromArgb(180, 26, 59, 105);
            s.BorderWidth = 2; // show a THICK line for high visibility, can be reduced for high volume data points to be better visible
            s.ShadowOffset = 1;
            s.IsVisibleInLegend = false;
            //s.IsValueShownAsLabel = true;                       
            s.LegendText = SeriesName;
            s.LegendToolTip = SeriesName;
            s.Points.SuspendUpdates();

            int step = 1;
            try
            {
                for (int i = 0; i < trace.Trace.Length; i += step)
                {
                    double y = trace.GetTime(i); double x = trace.Trace[i];
                    if (double.IsInfinity(y) || double.IsNaN(y) || Math.Abs(y) > 1e9)
                        y = 0;
                    if (double.IsInfinity(x) || double.IsNaN(x) || Math.Abs(x) > 1e9)
                        x = 0;

                    s.Points.AddXY(x, y);

                    if ((i % 100000) == 0)
                    {
                        CommonExtensions.DoEvents();
                    }
                }
            }
            catch { }
            s.Points.ResumeUpdates();

        }


        public void CheckAndAddSeriesToGraph(Chart chart1, string SeriesName, List<double> xValues, List<double> yValues, SeriesChartType chartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line)
        {
            foreach (Series se in chart1.Series)
            {
                if (se.Name == SeriesName)
                {
                    chart1.Series.Remove(se);
                    break;
                }
            }
            Series s = chart1.Series.Add(SeriesName);
            s.ChartType = chartType;
            s.BorderColor = System.Drawing.Color.FromArgb(180, 26, 59, 105);
            s.BorderWidth = 2; // show a THICK line for high visibility, can be reduced for high volume data points to be better visible
            s.ShadowOffset = 1;
            s.IsVisibleInLegend = false;
            //s.IsValueShownAsLabel = true;                       
            s.LegendText = SeriesName;
            s.LegendToolTip = SeriesName;
            s.Points.SuspendUpdates();

            int step = 1;

            for (int i = 0; i < xValues.Count; i += step)
            {
                double y = yValues[i]; double x = xValues[i];
                if (double.IsInfinity(y) || double.IsNaN(y) || Math.Abs(y) > 1e9)
                    y = 0;
                if (double.IsInfinity(x) || double.IsNaN(x) || Math.Abs(x) > 1e9)
                    x = 0;
                s.Points.AddXY(x, y);

                if ((i % 100000) == 0)
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                                      new Action(delegate { }));
                }
            }
            s.Points.ResumeUpdates();

        }

        public void AddSeriesPointsToGraph(Chart chart1, string SeriesName, double[] xValues, double[] yValues)
        {
            Series s = null;
            foreach (var se in chart1.Series)
            {
                if (se.Name == SeriesName)
                {
                    s = se;
                    break; ; //already exists
                }
            }

            if (s == null)
                return;

            s.Points.SuspendUpdates();
            int step = 1;
            for (int i = 0; i < xValues.Length; i += step)
            {
                double y = yValues[i]; double x = xValues[i];
                if (double.IsInfinity(y) || double.IsNaN(y) || Math.Abs(y) > 1e9)
                    y = 0;
                if (double.IsInfinity(x) || double.IsNaN(x) || Math.Abs(x) > 1e9)
                    x = 0;
                s.Points.AddXY(x, y);
            }
            s.Points.ResumeUpdates();
        }

        public void AddSeriesPointsToGraph(Chart chart1, string SeriesName, double x, double y)
        {
            Series s = null;
            foreach (var se in chart1.Series)
            {
                if (se.Name == SeriesName)
                {
                    s = se;
                    break; ; //already exists
                }
            }

            if (s == null)
                return;
            if (double.IsInfinity(y) || double.IsNaN(y) || Math.Abs(y) > 1e9)
                y = 0;
            if (double.IsInfinity(x) || double.IsNaN(x) || Math.Abs(x) > 1e9)
                x = 0;
            s.Points.AddXY(x, y);
        }


        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (cbCentered.IsChecked == true)
            {
                chart1.Series.Clear();

                var voltage = currentTrace.GetProcessed("CorrectedVolts");
                var currentFit = currentTrace.GetProcessed("Corrected");
                CheckAndAddSeriesToGraph(chart1, "Corrected", voltage.Trace, currentFit.Trace, SeriesChartType.Point);
            }
            else
                SetData(currentTrace);
        }

        Random r = new Random();
#if  DAQ 
        #region NIDAQ Handling

        private void RunNIDaQ()
        {
            DeviceControls.NIDAQDevice niDAQ = new DeviceControls.NIDAQDevice();
            string[] AIChannels = niDAQ.DeviceNamesIn();
            string[] AOChannels = niDAQ.DeviceNamesOut();
            niDAQ.VisaDataCompleted += DataCompleted;
            niDAQ.RunSweep(AIChannels[0], AOChannels[0], 1000, 1);
        }
        #endregion
#endif
        private void StartIV_Click(object sender, RoutedEventArgs e)
        {
            chart1.Series.Clear();
            chart2.Series.Clear();
#if  DAQ 
            if (lbDataRig.SelectedItem.ToString().ToLower() == "keithley")
            {
               // RunVisaSweep();
            }
            if (lbDataRig.SelectedItem.ToString().ToLower() == "nidaqmx")
            {
                RunNIDaQ();
            }
#endif
        }





        void DataCompleted(List<double> time, List<double> voltage, List<double> current)
        {

        }

        void visa_VisaPointDelivered(double time, double voltage, double current)
        {

        }

        private void host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            chart1.Width = (int)CommonExtensions.GetElementPixelSize(host).Width;
        }

        private void hostTime_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            chart2.Width = (int)CommonExtensions.GetElementPixelSize(hostTime).Width;
        }

        private void btnSaveNote_Click(object sender, RoutedEventArgs e)
        {
            if (currentTrace != null)
                currentTrace.SaveNote(new DataModel.eNote(tbNotes.Text));
        }

        private void lbDataRig_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void lbTraceVoltage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            tbTraceVoltage.Text = lbTraceVoltage.SelectedValue.ToString();
        }

        private void lbTopVoltage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbTopVoltage.Text = lbTopVoltage.SelectedValue.ToString();

        }

        private void lbBottomVoltage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbBottomVoltage.Text = lbBottomVoltage.SelectedValue.ToString();
        }

        private void lbScripts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbScripts.SelectedValue != null)
            {
                string program = lbScripts.SelectedValue.ToString().ToLower();
                bool ionic = (program.Contains("ionic") == true);

                if (ionic)
                {
                    lbNumPores.Visibility = System.Windows.Visibility.Visible;
                    tbNumPores.Visibility = System.Windows.Visibility.Visible;
                    tbTopVoltage.Text = "sweep";
                    tbBottomVoltage.Text = "0";
                    tbTraceVoltage.Text = "float";
                    if (program.Contains("big"))
                        lbCurrentLimits.SelectedIndex = 3;
                    else
                        lbCurrentLimits.SelectedIndex = 2;
                }
                else
                {
                    lbNumPores.Visibility = System.Windows.Visibility.Hidden;
                    tbNumPores.Visibility = System.Windows.Visibility.Hidden;
                    tbTopVoltage.Text = "none";
                    tbBottomVoltage.Text = "none";
                    tbTraceVoltage.Text = "sweep";
                    if (program.Contains("big"))
                        lbCurrentLimits.SelectedIndex = 1;
                    else

                        lbCurrentLimits.SelectedIndex = 0;
                }
            }
        }

        private void btnCopyText_Click(object sender, RoutedEventArgs e)
        {
            if (currentTrace.Current != null)
            {
                var current = currentTrace.Current;
                var voltage = currentTrace.GetProcessed("Voltage");
                var currentFit = currentTrace.GetProcessed("CurveFit");

                var voltage2 = currentTrace.GetProcessed("CorrectedVolts");
                var currentFit2 = currentTrace.GetProcessed("Corrected");

                var outString = tbFitInfo1.Text + "\n" + tbFitInfo2.Text + "\n";
                outString += "Voltage\tCurrent\tCurveFit\tCorrectedVolts\tCorrected Current\n";
                for (int i = 0; i < current.Trace.Length; i++)
                {
                    outString += voltage.Trace[i] + "\t" + current.Trace[i] + "\t";
                    if (currentFit != null)
                        outString += currentFit.Trace[i] + "\t";
                    if (voltage2 != null && i < voltage2.Trace.Length)
                        outString += voltage2.Trace[i] + "\t" + currentFit2.Trace[i] + "\n";
                    else
                        outString += "\n";
                }
                Clipboard.SetText(outString);
            }
            else
            {
                var outString = "Voltage\tCurrent\n";
                var s = chart1.Series[0];
                for (int i = 0; i < s.Points.Count; i++)
                {
                    outString += s.Points[i].XValue + "\t" + s.Points[i].YValues + "\n";
                }
                Clipboard.SetText(outString);
            }
        }


    }
}

