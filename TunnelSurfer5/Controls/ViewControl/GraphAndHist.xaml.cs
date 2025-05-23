using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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

namespace TunnelVision.Controls.ViewControl
{
    /// <summary>
    /// Interaction logic for GraphAndHist.xaml
    /// </summary>
    public partial class GraphAndHist : UserControl, INotifyPropertyChanged
    {

        protected void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public GraphAndHist()
        {
            InitializeComponent();

            DataContext = this;
        }

        #region Size
        private void host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeGraphs();

        }

        private void hostTime_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeGraphs();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeGraphs();
        }

        private void ResizeGraphs()
        {
            var size = CommonExtensions.GetElementPixelSize(host);
            chart1.Width = (int)size.Width;
            chart1.Height = (int)size.Height;
            size = CommonExtensions.GetElementPixelSize(hostTime);
            chartHist.Width = (int)size.Width;
            chartHist.Height = (int)size.Height;
        }
        #endregion

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetupChartProperties(chart1);
            SetupChartProperties(chartHist);
            chartHist.ChartAreas[0].AxisY.Title = "Normalized Freq.";
            chartHist.ChartAreas[0].AxisX.Title = "Current (pA)";

            Random rand = new Random();
            var n = 1000;
            double[] xValues = new double[n];
            double[] yValues = new double[n];
            for (int i = 0; i < n; i++)
            {
                yValues[i] = 100 * (0.5 - rand.NextDouble()) + 25;
                xValues[i] = i / 20000d;
            }

            CheckAndAddSeriesToGraph("Control", "pA", xValues, yValues);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        System.Windows.Threading.DispatcherTimer dispatcherTimerHist = new System.Windows.Threading.DispatcherTimer();

        private DataModel.Trace _CurrentTrace;
        public DataModel.Trace CurrentTrace
        {
            get { return _CurrentTrace; }
            set
            {
                _CurrentTrace = value;
                NotifyPropertyChanged("CurrentTrace");
                if (File.Exists(App.ConvertFilePaths(CurrentTrace.ProcessedFile + "_ExampleGraph.png")) == true && CurrentTrace.Id != -1)
                {
                    hostImage.Visibility = Visibility.Visible;
                    host.Visibility = Visibility.Hidden;
                    CopyButtons.Visibility = Visibility.Collapsed;
                    ShowData.Visibility = Visibility.Visible;
                    FakeChart1.Source = new BitmapImage(new Uri(App.ConvertFilePaths(CurrentTrace.ProcessedFile + "_ExampleGraph.png"), UriKind.Absolute));
                    App.DoneDrawing = true;
                }
                else
                {
                    DisplayData();
                }
            }
        }
        private bool skipDisplay = false;
        private bool _NewData = false;

        #region LoadBar
        System.Windows.Threading.DispatcherTimer LoadBarTimer = new System.Windows.Threading.DispatcherTimer();
        private void LoadBarTimer_Tick(object sender, EventArgs e)
        {
            LoadingBar.Value = (LoadingBar.Value + 1) % 100;
        }
        #endregion

        public void DisplayProcessed(string[] Selected)
        {
            LoadBarTimer.Start();
            ClearSeriesFromGraph();
            _NewData = false;
            CommonExtensions.DoEvents();


            chart1.ChartAreas.Clear();
            var nAreas = (int)Math.Floor((1+Selected.Length) / 2d);
            if (nAreas == 0)
                nAreas = 1;
            for (int ii = 0; ii < nAreas; ii++)
            {
                var area = chart1.ChartAreas.Add(ii.ToString());
                FormatChartArea(area);
            }

            int i = 0;
            foreach (var s in Selected)
            {
                try
                {

                    var data = CurrentTrace.GetProcessed(s);
                  
                    CheckAndAddSeriesToGraph(s, "pA", data, i, ((int)Math.Floor(i/2d)).ToString());
                }
                catch { }
                i++;
            }

            LoadBarTimer.Stop();
            LoadBarTimer.IsEnabled = false;
            chart1.Invalidate();
            CommonExtensions.DoEvents();
            dispatcherTimerHist.Interval = new TimeSpan(0, 0, 0, 0, 40);
            dispatcherTimerHist.IsEnabled = true;
            dispatcherTimerHist.Start();

        }

        private void DisplayData()
        {
            var current = CurrentTrace.Current;
            hostImage.Visibility = Visibility.Hidden;
            host.Visibility = Visibility.Visible;
            CopyButtons.Visibility = Visibility.Visible;
            ShowData.Visibility = Visibility.Collapsed;

            LoadBarTimer.Start();
            CommonExtensions.DoEvents();
            try
            {
                if (CurrentTrace.Current == null)
                {
                    if (CurrentTrace.Id != 0)
                        MessageBox.Show("Data has not finished processing.  Please wait for server or check with Brian");
                    LoadBarTimer.IsEnabled = false;
                    chart1.Invalidate();
                    return;
                }
                if (skipDisplay == false)
                {

                    if (CurrentTrace != null)
                    {
                        _NewData = false;
                        ClearSeriesFromGraph();
                        CheckAndAddSeriesToGraph("Raw", "pA", CurrentTrace.Current);
                        dispatcherTimerHist.Interval = new TimeSpan(0, 0, 0, 0, 40);
                        dispatcherTimerHist.IsEnabled = true;
                        dispatcherTimerHist.Start();
                    }
                    LoadBarTimer.Stop();
                    LoadBarTimer.IsEnabled = false;
                    chart1.Invalidate();
                    CommonExtensions.DoEvents();
                }
            }
            catch { }
        }

        private void ShowData_Click(object sender, RoutedEventArgs e)
        {
            DisplayData();
        }
        public void ShowPreview(bool preview)
        {
            if (preview == false)
            {
                hostImage.Visibility = Visibility.Hidden;
                host.Visibility = Visibility.Visible;
                CopyButtons.Visibility = Visibility.Visible;
                ShowData.Visibility = Visibility.Collapsed;
            }
            else
            {
                hostImage.Visibility = Visibility.Visible;
                host.Visibility = Visibility.Hidden;
                CopyButtons.Visibility = Visibility.Collapsed;
                ShowData.Visibility = Visibility.Visible;
            }
        }

        #region Charting
        public Chart theChart { get { return chart1; } }

        private void chart1_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            dispatcherTimerHist.Interval = new TimeSpan(0, 0, 0, 0, 40);
            dispatcherTimerHist.IsEnabled = true;
            dispatcherTimerHist.Start();
        }

        private void ChartCursorSelected(Chart sender, ChartCursor e)
        {
            try
            {
                if (double.IsNaN(e.X) == false)
                {

                    PointF diff = sender.CursorsDiff();
                    txtChartSelect.Text = diff.X.ToString("F4") + ", " + diff.Y.ToString("F4");
                    if (CurrentTrace != null)
                        txtTraceValue.Text = diff.X.ToString("F4") + ", " + diff.Y.ToString("F4");

                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private void ChartCursorMoved(Chart sender, ChartCursor e)
        {
            try
            {
                txtChartValue.Text = e.X.ToString("F4") + ", " + e.Y.ToString("F4");
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private void FormatChartArea(ChartArea area)
        {
            area.AxisX.LabelStyle.ForeColor = System.Drawing.Color.White;
            area.AxisY.LabelStyle.ForeColor = System.Drawing.Color.White;
            //// Enable all elements
            //area.AxisX.MinorGrid.Enabled = true;
            //area.AxisX.MinorTickMark.Enabled = true;
            //area.AxisX.MinorTickMark.Interval = 1;
            area.BackColor = System.Drawing.Color.FromArgb(44, 44, 44);
            area.AxisX.LineColor = System.Drawing.Color.White;
            area.AxisY.LineColor = System.Drawing.Color.White;
            area.AxisY.Title = "Current (pA)";
            area.AxisX.Title = "Time (s)";
            area.AxisY.TitleForeColor = System.Drawing.Color.White;
            area.AxisX.TitleForeColor = System.Drawing.Color.White;
            area.AxisX.LabelStyle.Format = "#.####";
            area.AxisY.LabelStyle.Format = "#.###";
            area.AxisY.IsStartedFromZero = false;


            // Set Grid lines and tick marks interval
            //area.AxisX.MajorGrid.Interval = 30;
            //area.AxisX.MajorTickMark.Interval = 30;
        }

        private void SetupChartProperties(Chart chart1)
        {
            //customize the X-Axis to properly display Time 
            //chart1.Customize += chart1_Customize;
            chart1.Series.Clear(); //first remove all series completely
            chart1.BackColor = System.Drawing.Color.FromArgb(54, 54, 54);
            chart1.ForeColor = System.Drawing.Color.White;

            FormatChartArea(chart1.ChartAreas[0]);

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

        }

        private Dictionary<Series, DataModel.Fileserver.FileTrace> _Data = new Dictionary<Series, DataModel.Fileserver.FileTrace>();

        private void zoomChanged(Chart sender)
        {
            if (_Data != null)
            {
                var visibleArea = theChart.ChartAreas[0].GetChartVisibleAreaBoundary();

                var min = visibleArea.Left;
                var max = visibleArea.Right;
                foreach (Series se in chart1.Series)
                {
                    var yValues = _Data[se];
                    se.Points.SuspendUpdates();
                    se.Points.Clear();
                    se.Points.AddXY(0, yValues.Trace[0]);
                    var minI = (int)Math.Floor(min * yValues.SampleRate);
                    minI = Math.Max(minI, 0);
                    var maxI = (int)Math.Floor(max * yValues.SampleRate);
                    maxI = Math.Min(maxI, yValues.Trace.Length);
                    int step = (int)Math.Floor((maxI - minI) / 4000d);
                    if (step < 1)
                    {
                        minI = Math.Max(minI - 500, 0);
                        maxI = Math.Min(maxI + 500, yValues.Trace.Length);
                        step = 1;
                    }


                    for (long i = minI; i < maxI - step / 2; i += step)
                    {
                        try
                        {
                            if (step > 1)
                            {
                                double Mn = yValues.Trace[i];
                                double Mx = Mn;
                                for (long j = i; j < i + step; j++)
                                {
                                    double v = yValues.Trace[j];
                                    if (v < Mn)
                                        Mn = v;
                                    if (v > Mx)
                                        Mx = v;
                                }
                                se.Points.AddXY(yValues.GetTime(i), Mn);
                                se.Points.AddXY(yValues.GetTime(i), Mx);
                            }
                            else
                            {
                                se.Points.AddXY(yValues.GetTime(i), yValues.Trace[i]);
                            }
                        }
                        catch { }
                    }
                    se.Points.AddXY(yValues.GetTime(yValues.Trace.Length - 1), yValues.Trace[yValues.Trace.Length - 1]);
                    se.Points.ResumeUpdates();
                }
            }
        }

        private void OnAxisViewChanges(object sender, ViewEventArgs viewEventArgs)
        {
            // Debug.Fail("Don't worry, this event is never raised.");
        }

        public void CheckAndAddSeriesToGraph(string SeriesName, string Unit, double[] xValues, double[] yValues, bool HighResolution = false)
        {
            foreach (Series se in chart1.Series)
            {
                if (se.Name == SeriesName)
                {
                    return; //already exists
                }
            }
            Series s = chart1.Series.Add(SeriesName);
            s.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
            s.BorderColor = System.Drawing.Color.FromArgb(180, 26, 59, 105);
            s.BorderWidth = 1; // show a THICK line for high visibility, can be reduced for high volume data points to be better visible
            s.ShadowOffset = 1;
            s.IsVisibleInLegend = false;
            //s.IsValueShownAsLabel = true;                       
            s.LegendText = SeriesName + " (" + Unit + ")";
            s.LegendToolTip = SeriesName + " (" + Unit + ")";

            if (xValues != null)
            {
                s.Points.SuspendUpdates();
                int step = 1;
                if (fastGraph.IsChecked == true && App.ForceHighQuality == false && HighResolution == false)
                    step = 200;
                int maxL = Math.Min(xValues.Length, yValues.Length);
                for (long i = 0; i < maxL; i += step)
                {
                    double xValue = xValues[i];
                    double yValue = yValues[i];


                    if (double.IsInfinity(yValue) || double.IsNaN(yValue))
                        yValue = 0;
                    if (double.IsInfinity(xValue) || double.IsNaN(xValue))
                        xValue = 0;


                    if (yValue > 50000000)
                        yValue = 50000000;
                    if (yValue < -50000000)
                        yValue = -50000000;
                    if (xValue > 50000000)
                        xValue = 50000000;
                    if (xValue < -50000000)
                        xValue = -50000000;

                    s.Points.AddXY(xValue, yValue);

                    if ((i % 1000000) == 0)
                    {
                        s.Points.ResumeUpdates();

                        CommonExtensions.DoEvents();

                        s.Points.SuspendUpdates();
                    }
                    if (_NewData)
                        break;
                }
                s.Points.ResumeUpdates();
            }


            if (CurrentTrace != null && s.Points.Count > 5000 && File.Exists(App.ConvertFilePaths(CurrentTrace.ProcessedFile + "_ExampleGraph.png")) == false)
            {
                chart1.SaveImage(App.ConvertFilePaths(CurrentTrace.ProcessedFile + "_ExampleGraph.png"), ChartImageFormat.Png);
                App.DoneDrawing = true;
            }
            _NewData = false;
        }

        public void CheckAndAddSeriesToGraph(string SeriesName, string Unit, DataModel.Fileserver.FileTrace yValues, int count = 0, string chartArea = "")
        {
            foreach (Series se in chart1.Series)
            {
                if (se.Name == SeriesName)
                {
                    return; //already exists
                }
            }


            Series s = chart1.Series.Add(SeriesName);
            try
            {
                if (chartArea != "")
                    s.ChartArea = chartArea;
            }
            catch { }
            try
            {
                _Data.Add(s, yValues);
            }
            catch
            {
                _Data.Remove(s);
                _Data.Add(s, yValues);
            }

            s.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
            s.BorderColor = System.Drawing.Color.FromArgb(180, 26, 59, 105);
            s.BorderWidth = 1; // show a THICK line for high visibility, can be reduced for high volume data points to be better visible
            s.ShadowOffset = 1;
            s.IsVisibleInLegend = false;
            //s.IsValueShownAsLabel = true;                       
            s.LegendText = SeriesName + " (" + Unit + ")";
            s.LegendToolTip = SeriesName + " (" + Unit + ")";
            if (count % 2 == 0)
                s.YAxisType = AxisType.Primary;
            else
            {
                s.YAxisType = AxisType.Secondary;

            }

            if (yValues != null)
            {
                s.Points.SuspendUpdates();
                int step = (int)Math.Floor(yValues.Trace.Length / 4000d);
                //if (fastGraph.IsChecked == true && App.ForceHighQuality == false)
                //    step = 200;
                int maxL = Math.Min(yValues.Trace.Length, yValues.Trace.Length);
                for (long i = 0; i < maxL - step; i += step)
                {
                    double Mn = yValues.Trace[i];
                    double Mx = Mn;
                    for (long j = i; j < i + step; j++)
                    {
                        double v = yValues.Trace[j];
                        if (v < Mn)
                            Mn = v;
                        if (v > Mx)
                            Mx = v;
                    }
                    s.Points.AddXY(yValues.GetTime(i), Mn);
                    s.Points.AddXY(yValues.GetTime(i), Mx);

                    if ((i % 1000000) == 0)
                    {
                        s.Points.ResumeUpdates();

                        CommonExtensions.DoEvents();

                        s.Points.SuspendUpdates();
                    }
                    if (_NewData)
                        break;
                }
                s.Points.ResumeUpdates();
            }


            if (CurrentTrace != null && s.Points.Count > 5000 && File.Exists(App.ConvertFilePaths(CurrentTrace.ProcessedFile + "_ExampleGraph.png")) == false)
            {
                chart1.SaveImage(App.ConvertFilePaths(CurrentTrace.ProcessedFile + "_ExampleGraph.png"), ChartImageFormat.Png);
                App.DoneDrawing = true;
            }
            _NewData = false;

            chart1.EnableZoomAndPanControls(ChartCursorSelected, ChartCursorMoved,
                         zoomChanged,
                         new ChartOption()
                         {
                             ContextMenuAllowToHideSeries = true,
                             XAxisPrecision = 2,
                             YAxisPrecision = 2
                         });

            // Client interface BUG:
            // OnAxisViewChang* is only called on Cursor_MouseUp, 
            //  so the following events are never raised
            chart1.AxisViewChanging += OnAxisViewChanges;
            chart1.AxisViewChanged += OnAxisViewChanges;
        }

        //public void CheckAndAddHistToGraph(string SeriesName, string Unit, double[,] yValues, int column)
        //{
        //    foreach (Series se in chartHist.Series)
        //    {
        //        if (se.Name == SeriesName)
        //        {
        //            chartHist.Series.Remove(se);
        //            break;
        //        }
        //    }
        //    Series s = chartHist.Series.Add(SeriesName);
        //    s.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
        //    //s.BorderColor = System.Drawing.Color.FromArgb(180, 26, 59, 105);
        //    s.BorderWidth = 2;
        //    s.ShadowOffset = 2;
        //    s.IsVisibleInLegend = true;





        //    double A = 0;
        //    for (int i = 0; i < yValues.GetLength(0); i++)
        //    {
        //        A += yValues[i, column];
        //    }
        //    A = A / yValues.GetLength(0);
        //    double std = 0;
        //    for (int i = 0; i < yValues.GetLength(0); i++)
        //    {
        //        std += Math.Pow(yValues[i, column] - A, 2);
        //    }
        //    std = Math.Sqrt(std / yValues.GetLength(0));
        //    std = 3 * std / 15;
        //    double[] xValues = new double[30];
        //    double[] binValues = new double[30];
        //    for (int i = 0; i < xValues.Length; i++)
        //        xValues[i] = (i - 15) * std + A;
        //    for (int i = 0; i < yValues.GetLength(0); i++)
        //    {
        //        double v = Math.Round(((yValues[i, column] - A) / std) + 15);
        //        if (v >= 0 && v < 30)
        //            binValues[(int)v]++;
        //    }
        //    s.Points.SuspendUpdates();
        //    s.Points.AddXY(xValues[0], 0);
        //    s.Points.AddXY(xValues[0], binValues[0] / yValues.GetLength(0));
        //    for (int i = 1; i < xValues.Length; i++)
        //    {
        //        s.Points.AddXY(xValues[i], binValues[i - 1] / yValues.GetLength(0));
        //        s.Points.AddXY(xValues[i], binValues[i] / yValues.GetLength(0));
        //    }
        //    s.Points.AddXY(xValues[xValues.Length - 1], 0);
        //    s.Points.ResumeUpdates();

        //}

        public void ClearSeriesFromGraph()
        {
            //remove the series curve itself
            chart1.Series.Clear();
        }

        public void ClearCurveDataPointsFromGraph()
        {
            //clear only DATA points from curve, keeping it as is.
            foreach (Series s in chart1.Series)
            {
                s.Points.Clear();
            }
        }

        public void AddPointToLine(string strPinName, double dValueY, double dValueX)
        {
            // we don't want series to be drawn while adding points to it.
            //this can reduce flicker.
            chart1.Series.SuspendUpdates();

            chart1.Series[strPinName].Points.AddXY(dValueX, dValueY);
            chart1.Series.ResumeUpdates();
        }

        public void AddSeriesPointsToGraph(string SeriesName, double xValue, double yValue)
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
            if (double.IsInfinity(yValue) || double.IsNaN(yValue))
                yValue = 0;
            if (double.IsInfinity(xValue) || double.IsNaN(xValue))
                xValue = 0;


            if (yValue > 50000000)
                yValue = 50000000;
            if (yValue < -50000000)
                yValue = -50000000;
            if (xValue > 50000000)
                xValue = 50000000;
            if (xValue < -50000000)
                xValue = -50000000;


            s.Points.AddXY(xValue, yValue);

        }

        private void fastGraph_Checked(object sender, RoutedEventArgs e)
        {
            if (fastGraph.IsChecked == false)
            {
                DisplayData();
            }
        }

        double xRangeMin = -10;
        double xRangeMax = -10;
        private void DrawHistogram()
        {
            if (CurrentTrace != null)
            {
                double max;// = this.chart1.ChartAreas[0].AxisX.Maximum;
                double min;// = this.chart1.ChartAreas[0].AxisX.Minimum;

                var visibleArea = theChart.ChartAreas[0].GetChartVisibleAreaBoundary();// .AxisX.GetMinMax(out min, out max, true);

                min = visibleArea.Left;
                max = visibleArea.Right;
                if (max > min && max != min)
                {
                    chartHist.Series.Clear();
                    string[] headers;
                    double[,] data = CurrentTrace.GetDataSegment(min, max, out headers);
                    //for (int i = 0; i < lbProcessedData.SelectedItems.Count; i++)
                    //{
                    //    string colName = lbProcessedData.SelectedItems[i].ToString().ToLower();
                    //    if (colName == "raw")
                    //        colName = "current(pa)";
                    //    if (colName == "backgroundremoved")
                    //        colName = "background_removed";
                    //    if (data != null)
                    //    {
                    //        for (int j = 1; j < data.GetLength(1); j++)
                    //            if (colName == headers[j].ToLower())
                    //            {
                    //                CheckAndAddHistToGraph(headers[j], "pA", data, j);
                    //                break;
                    //            }
                    //    }
                    //}

                    //for (int i = 0; i < lbProcessedData2.SelectedItems.Count; i++)
                    //{
                    //    string colName = lbProcessedData2.SelectedItems[i].ToString().ToLower();
                    //    if (colName == "raw")
                    //        colName = "current(pa)";
                    //    if (colName == "backgroundremoved")
                    //        colName = "background_removed";
                    //    if (data != null)
                    //    {
                    //        for (int j = 1; j < data.GetLength(1); j++)
                    //            if (colName == headers[j].ToLower())
                    //            {
                    //                CheckAndAddHistToGraph(headers[j], "pA", data, j);
                    //                break;
                    //            }
                    //    }
                    //}
                }
                xRangeMin = min;
                xRangeMax = max;
            }
        }

        void dispatcherTimerHist_Tick(object sender, EventArgs e)
        {
            dispatcherTimerHist.IsEnabled = false;
            DrawHistogram();
        }

        #endregion

        #region Clipboard
        private readonly BackgroundWorker workerCopyToClipboard = new BackgroundWorker();

        string clip = "";
        private void workerCopyToClipboard_DoWork(object sender, DoWorkEventArgs e)
        {
            if (CurrentTrace != null)
            {

                double max;// = this.chart1.ChartAreas[0].AxisX.Maximum;
                double min;// = this.chart1.ChartAreas[0].AxisX.Minimum;

                var visibleArea = theChart.ChartAreas[0].GetChartVisibleAreaBoundary();// .AxisX.GetMinMax(out min, out max, true);

                min = visibleArea.Left;
                max = visibleArea.Right;
                string[] headers;
                double[,] data = CurrentTrace.GetDataSegment(min, max, out headers);

                clip = "";
                for (int i = 0; i < headers.Length; i++)
                    clip += headers[i] + "\t";
                clip += "\r\n";
                for (int i = 0; i < data.GetLength(0); i++)
                {
                    for (int j = 0; j < data.GetLength(1); j++)
                    {
                        clip += data[i, j] + "\t";
                    }
                    clip += "\r\n";
                }

            }
        }

        private void workerCopyToClipboard_RunWorkerCompleted(object sender,
                                       RunWorkerCompletedEventArgs e)
        {
            Clipboard.SetText(clip);
            clip = "";
            LoadBarTimer.IsEnabled = false;
        }

        private void doCopy_Click(object sender, RoutedEventArgs e)
        {
            LoadBarTimer.IsEnabled = true;
            workerCopyToClipboard.RunWorkerAsync();
        }
        #endregion

        private void doExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.SaveFileDialog folderDialog = new System.Windows.Forms.SaveFileDialog();
                System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
                if (result.ToString() == "OK")
                {
                    double max;// = this.chart1.ChartAreas[0].AxisX.Maximum;
                    double min;// = this.chart1.ChartAreas[0].AxisX.Minimum;

                    var visibleArea = theChart.ChartAreas[0].GetChartVisibleAreaBoundary();// .AxisX.GetMinMax(out min, out max, true);

                    min = visibleArea.Left;
                    max = visibleArea.Right;
                    string[] headers;
                    double[,] data = CurrentTrace.GetDataSegment(min, max, out headers);


                    string filePre = System.IO.Path.GetDirectoryName(folderDialog.FileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(folderDialog.FileName);

                    for (int j = 0; j < data.GetLength(1); j++)
                    {
                        string filename = filePre + headers[j] + ".raw";
                        using (BinaryWriter b = new BinaryWriter(File.Open(filename, FileMode.Create)))
                        {
                            for (int i = 0; i < data.GetLength(0); i++)
                            {
                                b.Write(data[i, j]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }


    }
}
