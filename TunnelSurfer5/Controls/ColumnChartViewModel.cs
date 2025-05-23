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
    public class ColumnChartViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> ChartTypes { get; set; }
        public List<string> SelectionBrushes { get; set; }

        private string selectedChartType = null;
        public string SelectedChartType
        {
            get
            {
                return selectedChartType;
            }
            set
            {
                selectedChartType = value;
                NotifyPropertyChanged("SelectedChartType");
            }
        }

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

        public int MinHeight
        {
            get; set;
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

        public ColumnChartViewModel()
        {
            MinHeight = 300;
            SaveFilename = "";
            ChartTypes = new ObservableCollection<string>();
            //ChartTypes.Add("All");            
            ChartTypes.Add("Column");
            ChartTypes.Add("StackedColumn");
            //ChartTypes.Add("Bar");
            //ChartTypes.Add("StackedBar");
            //ChartTypes.Add("Pie");
            //ChartTypes.Add("Doughnut");
            //ChartTypes.Add("Gauge");            
            SelectedChartType = ChartTypes[0];
            FontSizes = new List<double>();
            FontSizes.Add(9.0);
            FontSizes.Add(11.0);
            FontSizes.Add(13.0);
            FontSizes.Add(18.0);
            SelectedFontSize = 11.0;


            Series = new ObservableCollection<SeriesData>();
        }

        public delegate ColumnChartViewModel DelayLoadRequestEvent(ColumnChartViewModel sChart, object Owner, object param1, object param2);
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
                DelayLoadRequested(this, ChartOwner, OwnerParam1, OwnerParam2);
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



        public void AddSeries(string seriesName, ObservableCollection<CategoryColumn> data)
        {
            Series.Add(new SeriesData() { SeriesDisplayName = seriesName, Items = data, SeriesDescription = seriesName });
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
        private double fontSize = 11.0;
        public double SelectedFontSize
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

        public string SelectedFontSizeString
        {
            get
            {
                return SelectedFontSize.ToString() + "px";
            }
        }

        public ObservableCollection<SeriesData> Series
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
            //customize the X-Axis to properly display Time 
            //chart1.Customize += chart1_Customize;
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
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "#.###";
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "##";

            chart1.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorTickMark.Enabled = false;
            // chart1.ChartAreas[0].AxisY.MajorTickMark.Interval = 1;
            chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            // chart1.ChartAreas[0].AxisY.Interval = 1;
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
        private void zoomChanged(Chart sender)
        {
        }
        private void OnAxisViewChanges(object sender, ViewEventArgs viewEventArgs)
        {
          //  Debug.Fail("Don't worry, this event is never raised.");
        }

        private string _SelectedPoint = "";
        public string SelectedPoint
        {
            get { return _SelectedPoint; }
        }

        private string _CurrentPoint = "";
        public string CurrentPoint
        {
            get { return _CurrentPoint; }
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
                _CurrentPoint = e.X.ToString("F4") + ", " + e.Y.ToString("F4");
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }
        

        public void Plot()
        {
            PlotBarHistogram();
            if (SaveFilename != "")
            {
                CommonExtensions.DoEvents();
                CommonExtensions.DoEvents();
                try
                {
                    theChart.SaveImage(App.ConvertFilePaths(SaveFilename), ChartImageFormat.Png);
                }
                catch { }
            }
        }
        private  void PlotBarHistogram()
        {
            if (theChart == null)
                return;
            theChart.Series.Clear();
            var title = new Title(this.Title, Docking.Top, new System.Drawing.Font("Verdana", 14), Color.White);
            theChart.Titles.Clear();
            theChart.Titles.Add(title);
            foreach (var series in this.Series)
            {
                try
                {
                    Series s = theChart.Series.Add(series.SeriesDisplayName + "");
                    s.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
                    s["DrawingStyle"] = "LightToDark";
                    //s.BorderColor = System.Drawing.Color.FromArgb(180, 26, 59, 105);
                    s.BorderWidth = 2;
                    s.ShadowOffset = 2;
                    s.IsVisibleInLegend = true;
                    s.XValueType = ChartValueType.Auto;

                    for (int i = 0; i < series.Items.Count; i++)
                    {
                        double number = series.Items[i].Number;
                        if (double.IsNaN(number) || double.IsInfinity(number))
                            s.Points.AddXY(series.Items[i].Category, 0);
                        else
                            s.Points.AddXY(series.Items[i].Category, number);
                    }
                }
                catch { }
            }
        }

    }

    public class DelegateCommand : ICommand
    {
        private readonly Predicate<object> _canExecute;
        private readonly Action<object> _execute;

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        public DelegateCommand(Action<object> execute,
                       Predicate<object> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            return _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }

    public class SeriesData
    {
        public string SeriesDisplayName { get; set; }

        public string SeriesDescription { get; set; }

        public ObservableCollection<CategoryColumn> Items { get; set; }
    }

    public class CategoryColumn : INotifyPropertyChanged
    {
        public string Category { get; set; }

        private double _number = 0;
        public double Number
        {
            get
            {
                return _number;
            }
            set
            {
                _number = value;
                if (PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Number"));
                }
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}


