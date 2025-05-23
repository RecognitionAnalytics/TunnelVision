using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;

#if NETFX_CORE
using Windows.UI.Xaml;
#else

#endif

namespace TunnelVision
{
    public class ScatterChartViewModel : INotifyPropertyChanged
    {
        public enum ChartTypeEnum
        {
            Line, Histogram
        }

        public List<string> SelectionBrushes { get; set; }

        private object selectedPalette = null;
        public object SelectedPalette
        {
            get
            {
                return selectedPalette;
            }
            set
            {
                selectedPalette = value;
                NotifyPropertyChanged("SelectedPalette");
            }
        }

        private bool darkLayout = false;
        public bool DarkLayout
        {
            get
            {
                return darkLayout;
            }
            set
            {
                darkLayout = value;
                NotifyPropertyChanged("DarkLayout");
                NotifyPropertyChanged("Foreground");
                NotifyPropertyChanged("Background");
                NotifyPropertyChanged("MainBackground");
                NotifyPropertyChanged("MainForeground");
            }
        }

        public string Foreground
        {
            get
            {
                if (darkLayout)
                {
                    return "#FFEEEEEE";
                }
                return "#FF666666";
            }
        }
        public string MainForeground
        {
            get
            {
                if (darkLayout)
                {
                    return "#FFFFFFFF";
                }
                return "#FF666666";
            }
        }
        public string Background
        {
            get
            {
                if (darkLayout)
                {
                    return "#FF333333";
                }
                return "#FFF9F9F9";
            }
        }
        public string MainBackground
        {
            get
            {
                if (darkLayout)
                {
                    return "#FF000000";
                }
                return "#FFEFEFEF";
            }
        }


        private string selectedBrush = null;
        public string SelectedBrush
        {
            get
            {
                return selectedBrush;
            }
            set
            {
                selectedBrush = value;
                NotifyPropertyChanged("SelectedBrush");
            }
        }

        private ChartTypeEnum _ChartType = ChartTypeEnum.Line;
        public ChartTypeEnum ChartType
        {
            get
            {
                return _ChartType;
            }
            set
            {
                _ChartType = value;
                Plot();
            }
        }

        public ScatterChartViewModel()
        {
            SaveFilename = "";
            FontSizes = new List<double>();
            FontSizes.Add(9.0);
            FontSizes.Add(11.0);
            FontSizes.Add(13.0);
            FontSizes.Add(18.0);
            SelectedFontSize = 14.0f;

            Series = new ObservableCollection<ScatterSeriesData>();
        }

        private double[] DefaultX = null;
        public void AddSeries(string seriesName, double[] xData, double[] yData)
        {
            DefaultX = xData;
            Series.Add(new ScatterSeriesData() { SeriesDisplayName = seriesName, XItems = xData, YItems = yData, SeriesDescription = seriesName });
            if (Series.Count > 5)
            {
                MinHeight = 400;
            }
        }

        public void ExtendSeries(string seriesName, double[] xData, double[] yData)
        {
            DefaultX = xData;
            ScatterSeriesData theSeries = null;
            foreach (var line in Series)
            {
                if (line.SeriesDisplayName == seriesName)
                {
                    theSeries = line;
                    break;
                }
            }
            if (theSeries == null)
            {
                AddSeries(seriesName, xData, yData);
                return;
            }

            theSeries.XItems= theSeries.XItems.Concat(xData).ToArray();
            theSeries.YItems = theSeries.YItems.Concat(yData).ToArray();
        }

        public void AddSeries(string seriesName, DataModel.Fileserver.FileTrace xData, DataModel.Fileserver.FileTrace yData)
        {
            DefaultX = xData.Trace;
            Series.Add(new ScatterSeriesData() { SeriesDisplayName = seriesName, XItems = xData.Trace, YItems = yData.Trace, SeriesDescription = seriesName });
            if (Series.Count > 5)
            {
                MinHeight = 400;
            }
        }


        public void AddSeries(string seriesName, double[] yData)
        {
            if (DefaultX == null)
            {
                var x = new double[yData.Length];
                for (int i = 0; i < x.Length; i++)
                    x[i] = i;

                Series.Add(new ScatterSeriesData() { SeriesDisplayName = seriesName, XItems = x, YItems = yData, SeriesDescription = seriesName });
            }
            else
                Series.Add(new ScatterSeriesData() { SeriesDisplayName = seriesName, XItems = DefaultX, YItems = yData, SeriesDescription = seriesName });
            if (Series.Count > 5)
            {
                MinHeight = 800;
            }
        }

        private int _MinHeight = 250;
        public int MinHeight
        {
            get { return _MinHeight; }
            set
            {
                _MinHeight = value;
                NotifyPropertyChanged("MinHeight");
            }
        }
        private bool isLegendVisible = true;
        public bool IsLegendVisible
        {
            get
            {
                return isLegendVisible;
            }
            set
            {
                isLegendVisible = value;
                NotifyPropertyChanged("IsLegendVisible");
            }
        }

        private bool isTitleVisible = true;
        public bool IsTitleVisible
        {
            get
            {
                return isTitleVisible;
            }
            set
            {
                isTitleVisible = value;
                NotifyPropertyChanged("IsTitleVisible");
            }
        }

        public string Title { get; set; }
        public string XLabel { get; set; }
        public string YLabel { get; set; }
        public List<double> FontSizes { get; set; }
        private float fontSize = 11.0f;
        public float SelectedFontSize
        {
            get
            {
                return fontSize;
            }
            set
            {
                fontSize = value;
                NotifyPropertyChanged("SelectedFontSize");
                NotifyPropertyChanged("SelectedFontSizeString");
            }
        }

        public delegate void DelayLoadRequestEvent(ScatterChartViewModel sChart, object Owner, object param1, object param2);
        public event DelayLoadRequestEvent DelayLoadRequested;

        private ICommand _RequestFullDataCommand;
        public ICommand RequestFullDataCommand
        {
            get
            {
                return _RequestFullDataCommand ?? (_RequestFullDataCommand = new Controls.Converters.CommandHandler(() => RequestFullData(), _canExecute));
            }
        }
        private bool _canExecute = true;

        public void RequestFullData()
        {
            if (DelayLoadRequested != null)
                DelayLoadRequested(this, ChartOwner, OwnerParam1,OwnerParam2);
        }

        public bool ShowLoadScreen
        {
            set
            {
                if (value)
                {
                    ShowGraph = Visibility.Collapsed;
                    LoadScreen = Visibility.Visible;
                }
                else
                {
                    ExampleImage = "";
                    CommonExtensions.DoEvents();
                    LoadScreen = Visibility.Collapsed;
                    ShowGraph = Visibility.Visible;
                    Plot();
                }
            }
        }
        public object ChartOwner { get; set; }
        public object OwnerParam1 { get; set; }
        public object OwnerParam2 { get; set; }
        private string _ExampleImage = "";
        public string ExampleImage
        {
            get { return _ExampleImage; }
            set
            {
                _ExampleImage = value;
                NotifyPropertyChanged("ExampleImage");
            }
        }

       
        public string SaveFilename { get; set; }
        private Visibility _LoadScreen = Visibility.Collapsed;
        public Visibility LoadScreen
        {
            get { return _LoadScreen; }
            private set
            {
                _LoadScreen = value;
                NotifyPropertyChanged("LoadScreen");
                //   PropertyChanged(this, new PropertyChangedEventArgs("LoadScreen"));
            }
        }
        private Visibility _ShowGraph = Visibility.Visible;
        public Visibility ShowGraph
        {
            get { return _ShowGraph; }
            private set
            {
                _ShowGraph = value;
                NotifyPropertyChanged("ShowGraph");
                // PropertyChanged(this, new PropertyChangedEventArgs("ShowGraph"));
            }
        }

        public ObservableCollection<ScatterSeriesData> Series
        {
            get;
            set;
        }

        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string ToolTipFormat
        {
            get
            {
                return "{0} in series '{2}' has value '{1}' ({3:P2})";
            }
        }

        private Chart theChart;

        public void SetupChartProperties(Chart chart1)
        {
            theChart = chart1;

            chart1.Series.Clear(); //first remove all series completely
            chart1.BackColor = System.Drawing.Color.FromArgb(54, 54, 54);
            chart1.ForeColor = System.Drawing.Color.White;
            chart1.ChartAreas[0].AxisX.LabelStyle.ForeColor = System.Drawing.Color.White;
            chart1.ChartAreas[0].AxisY.LabelStyle.ForeColor = System.Drawing.Color.White;
            //// Enable all elements
            chart1.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisX.MinorTickMark.Enabled = false;
            // chart1.ChartAreas[0].AxisX.MinorTickMark.Interval = 15;
            chart1.ChartAreas[0].BackColor = System.Drawing.Color.FromArgb(44, 44, 44);
            chart1.ChartAreas[0].AxisX.LineColor = System.Drawing.Color.White;
            chart1.ChartAreas[0].AxisY.LineColor = System.Drawing.Color.White;
            chart1.ChartAreas[0].AxisY.Title = YLabel + " ";
            chart1.ChartAreas[0].AxisX.Title = XLabel + " ";
            chart1.ChartAreas[0].AxisY.TitleForeColor = System.Drawing.Color.White;
            chart1.ChartAreas[0].AxisX.TitleForeColor = System.Drawing.Color.White;
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "#.##";
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "##.#";

            chart1.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorTickMark.Enabled = false;
            //  chart1.ChartAreas[0].AxisY.MajorTickMark.Interval = 1;
            chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            //  chart1.ChartAreas[0].AxisY.Interval = 1;
            //set legend position and properties as required
            chart1.Legends[0].LegendStyle = LegendStyle.Table;
            chart1.Legends[0].ForeColor = System.Drawing.Color.White;
            // Set table style if legend style is Table
            chart1.Legends[0].TableStyle = LegendTableStyle.Auto;

            // Set legend docking
            chart1.Legends[0].Docking = Docking.Top;

            // Set legend alignment
            chart1.Legends[0].Alignment = System.Drawing.StringAlignment.Center;

            // Set Antialiasing mode
            //this can be set lower if there are any performance issues!
            chart1.AntiAliasing = AntiAliasingStyles.All;
            chart1.TextAntiAliasingQuality = TextAntiAliasingQuality.High;


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

        private void OnAxisViewChanges(object sender, ViewEventArgs viewEventArgs)
        {
           // Debug.Fail("Don't worry, this event is never raised.");
        }

        private string _SelectedPoint = "";
        public string SelectedPoint
        {
            get { return _SelectedPoint; }
        }

        private string _CurrentPoint = "";
        public string CurrentPoint
        {
            get
            {
                return _CurrentPoint;
            }
            set
            {
                _CurrentPoint = value;
                NotifyPropertyChanged("CurrentPoint");
            }
        }

        private void ChartCursorSelected(Chart sender, ChartCursor e)
        {
            try
            {
                if (double.IsNaN(e.X) == false)
                {
                    _SelectedPoint = e.X.ToString("F4") + ", " + e.Y.ToString("F4");
            //  Debug.WriteLine("Cursor Position: " + txtChartSelect.Text + " @ " + e.ChartArea.Name);

            PointF diff = sender.CursorsDiff();
            _SelectedPoint = diff.X.ToString("F4") + ", " + diff.Y.ToString("F4");
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
                CurrentPoint = e.X.ToString("F4") + ", " + e.Y.ToString("F4");
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private void zoomChanged(Chart sender)
        {
        }

        public void Plot()
        {
            if (theChart != null)
            {
                theChart.Series.Clear();
                var title = new Title(this.Title, Docking.Top, new System.Drawing.Font("Verdana", 14), Color.White);
                theChart.Titles.Clear();
                theChart.Titles.Add(title);

                if (_ChartType == ChartTypeEnum.Line)
                    PlotLines();
                else
                    PlotHistogram();

                if (SaveFilename != "")
                {
                    CommonExtensions.DoEvents();
                    CommonExtensions.DoEvents();
                    try
                    {
                        theChart.SaveImage(App.ConvertFilePaths(SaveFilename), ChartImageFormat.Png);
                    }
                    catch { }
                    //SaveQualityChartImage(_ExampleImage);
                }
            }
        }

        private void PlotLines()
        {
            theChart.Series.Clear();

            foreach (var line in Series)
            {
                if (line.XItems != null)
                {
                    Series s = null;
                    try
                    {
                        s = theChart.Series.Add(line.SeriesDisplayName);
                    }
                    catch
                    {
                        s = theChart.Series.Add(line.SeriesDisplayName + "_" + theChart.Series.Count);
                    }
                    s.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                    s.BorderColor = System.Drawing.Color.FromArgb(180, 26, 59, 105);
                    s.BorderWidth = 1;
                    s.ShadowOffset = 1;
                    s.IsVisibleInLegend = true;
                    s.LegendText = line.SeriesDisplayName;
                    s.LegendToolTip = line.SeriesDisplayName;
                    s.Points.SuspendUpdates();

                    for (int i = 0; i < line.XItems.Length; i++)
                    {

                        double number = line.YItems[i];
                        if (double.IsNaN(number) || double.IsInfinity(number))
                            s.Points.AddXY(line.XItems[i], 0);
                        else
                            s.Points.AddXY(line.XItems[i], number);


                    }
                    s.Points.ResumeUpdates();
                }
            }
        }

        private void PlotHistogram()
        {
            theChart.Series.Clear();
            foreach (var line in Series)
            {
                Series s = theChart.Series.Add(line.SeriesDisplayName);
                s.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;

                s.BorderWidth = 2;
                s.ShadowOffset = 2;
                s.IsVisibleInLegend = true;

                var binValues = line.XItems;
                var values = line.YItems;

                double sm = binValues.Sum();
                s.Points.SuspendUpdates();
                s.Points.AddXY(values[0], 0);
                double number = binValues[0] / sm;
                if (double.IsNaN(number) || double.IsInfinity(number))
                    number = 0;
                s.Points.AddXY(values[0], binValues[0] / sm);
                for (int i = 1; i < values.Length; i++)
                {
                    number = binValues[i] / sm;
                    if (double.IsNaN(number) || double.IsInfinity(number))
                        number = 0;

                    double number1 = binValues[i - 1] / sm;
                    if (double.IsNaN(number1) || double.IsInfinity(number1))
                        number1 = 0;


                    s.Points.AddXY(values[i], number1);
                    s.Points.AddXY(values[i], number);


                }
                s.Points.AddXY(values[values.Length - 1], 0);
                s.Points.ResumeUpdates();
            }
        }

        public void CopyVisible()
        {

            double max;// = this.chart1.ChartAreas[0].AxisX.Maximum;
            double min;// = this.chart1.ChartAreas[0].AxisX.Minimum;

            var visibleArea= theChart.ChartAreas[0].GetChartVisibleAreaBoundary();// .AxisX.GetMinMax(out min, out max, true);

            min = visibleArea.Left;
            max = visibleArea.Right;

            string clip = "X Values+\t";
            for (int i = 0; i < Series.Count; i++)
                clip += Series[i].SeriesDisplayName + "\t";
            clip += "\r\n";
            for (int i = 0; i < Series.Count; i++)
            {
                var t = Series[i];
                for (int j = 0; j < t.XItems.Length; j++)
                {
                    if (t.XItems[j] > min && t.XItems[j] < max)
                    {
                        if (j == 0)
                        {
                            clip += t.XItems[j] + "\t";
                            clip += t.YItems[j] + "\t";
                        }
                        //else 
                        //    clip += data[i, j] + "\t";
                    }
                }
                clip += "\r\n";
            }


        }



        System.Drawing.Font oldFont1 = new System.Drawing.Font("Trebuchet MS", 35F, System.Drawing.FontStyle.Bold);
        System.Drawing.Font oldFont2 = new System.Drawing.Font("Trebuchet MS", 15F, System.Drawing.FontStyle.Bold);
        System.Drawing.Font oldFont3 = new System.Drawing.Font("Trebuchet MS", 35F, System.Drawing.FontStyle.Bold);
        System.Drawing.Font oldLegendFont = new System.Drawing.Font("Trebuchet MS", 35F, System.Drawing.FontStyle.Bold);

        int oldLineWidth1;
        int oldLineWidth2;
        int oldLineWidth3;
        int oldLineWidth4;

        int oldWidth;
        int oldHeight;
        public void SaveQualityChartImage(string filename)
        {
            if (!(theChart.Series.Count > 0))
            {
                return;
            }

            if (theChart.Titles.Count > 0)
            {
                oldFont1 = theChart.Titles[0].Font;
            }
            oldFont2 = theChart.ChartAreas[0].AxisX.LabelStyle.Font;
            oldFont3 = theChart.ChartAreas[0].AxisX.TitleFont;
            //if (theChart.Legends.Count > 0)
            //{
            //    oldLegendFont = theChart.Legends["Legend"].Font;
            //}
            oldLineWidth1 = theChart.ChartAreas[0].AxisX.LineWidth;
            oldLineWidth2 = theChart.ChartAreas[0].AxisX.MajorTickMark.LineWidth;
            oldLineWidth3 = theChart.Series[0].BorderWidth;
            oldLineWidth4 = theChart.ChartAreas[0].AxisY.MajorGrid.LineWidth;
            oldWidth = theChart.Width;
            oldHeight = theChart.Height;

            SaveImage(filename);
        }

        private void SaveImage(string filename)
        {
            theChart.Visible = false;
            System.Drawing.Font chtFont = new System.Drawing.Font("Trebuchet MS", 35F, System.Drawing.FontStyle.Bold);
            System.Drawing.Font smallFont = new System.Drawing.Font("Trebuchet MS", 15F, System.Drawing.FontStyle.Bold);
            if (theChart.Titles.Count > 0)
            {
                theChart.Titles[0].Font = chtFont;
            }

            theChart.ChartAreas[0].AxisX.TitleFont = chtFont;
            theChart.ChartAreas[0].AxisX.LineWidth = 3;
            theChart.ChartAreas[0].AxisX.MajorGrid.LineWidth = 3;
            theChart.ChartAreas[0].AxisX.LabelStyle.Font = smallFont;
            theChart.ChartAreas[0].AxisX.MajorTickMark.LineWidth = 3;

            theChart.ChartAreas[0].AxisY.TitleFont = chtFont;
            theChart.ChartAreas[0].AxisY.LineWidth = 3;
            theChart.ChartAreas[0].AxisY.MajorGrid.LineWidth = 3;
            theChart.ChartAreas[0].AxisY.LabelStyle.Font = smallFont;
            theChart.ChartAreas[0].AxisY.MajorTickMark.LineWidth = 3;
            //if (theChart.Legends.Count > 0)
            //{
            //    theChart.Legends["Legend"].Font = smallFont;
            //}


            foreach (Series series in theChart.Series)
            {
                series.BorderWidth = 3;

            }

            theChart.Width = 900;
            theChart.Height = 600;


            theChart.SaveImage(App.ConvertFilePaths( filename), ChartImageFormat.Png);

            resetOldValues();

        }

        private void resetOldValues()
        {
            if (theChart.Titles.Count > 0)
            {
                theChart.Titles[0].Font = oldFont1;
            }

            theChart.ChartAreas[0].AxisX.TitleFont = oldFont3;
            theChart.ChartAreas[0].AxisX.LineWidth = oldLineWidth1;
            theChart.ChartAreas[0].AxisX.MajorGrid.LineWidth = oldLineWidth4;
            theChart.ChartAreas[0].AxisX.LabelStyle.Font = oldFont2;
            theChart.ChartAreas[0].AxisX.MajorTickMark.LineWidth = oldLineWidth2;

            theChart.ChartAreas[0].AxisY.TitleFont = oldFont3;
            theChart.ChartAreas[0].AxisY.LineWidth = oldLineWidth1;
            theChart.ChartAreas[0].AxisY.MajorGrid.LineWidth = oldLineWidth4;
            theChart.ChartAreas[0].AxisY.LabelStyle.Font = oldFont2;
            theChart.ChartAreas[0].AxisY.MajorTickMark.LineWidth = oldLineWidth2;
            //if (theChart.Legends.Count > 0)
            //{
            //    theChart.Legends["Legend"].Font = oldLegendFont;
            //}
            
            foreach (Series series in theChart.Series)
            {
                series.BorderWidth = oldLineWidth3;

            }

            theChart.Width = oldWidth;
            theChart.Height = oldHeight;
            theChart.Visible = true;
        }
    }


    

    public class ScatterSeriesData
    {
        public string SeriesDisplayName { get; set; }

        public string SeriesDescription { get; set; }

        public double[] XItems { get; set; }
        public double[] YItems { get; set; }
    }

}


