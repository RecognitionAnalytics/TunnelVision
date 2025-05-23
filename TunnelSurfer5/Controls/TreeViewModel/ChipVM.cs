using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace TunnelVision.Controls.TreeViewModel
{
     public class ChipVM : TreeNodeVM
    {
        public override DataModel.Experiment Experiment()
        {
            return Chip().Experiment;
        }
        public override DataModel.Batch Batch()
        {
            return Chip().Batch;
        }

        public override DataModel.Chip Chip()
        {
            return theChip;
        }

        public DataModel.Chip theChip { get; set; }
       
        public ChipVM(DataModel.Chip chip):base(chip)
        {
            this.theChip = chip;
            RateItem(chip.GoodChip);
            OpenList = ((string)Properties.Settings.Default["DefaultChip"] == this.ToString());

             MenuButtons.AddDataTraceButton = Visibility.Collapsed;
            MenuButtons.AddTunnelButton = Visibility.Collapsed;
            MenuButtons.UploadButton = Visibility.Collapsed;
        }

        protected override void DoListOpen(bool value)
        {
            if (value == true)
            {
                Children.Clear();
                if (theChip.IonicIV != null)
                {
                    if (theChip.IonicIV.Count > 0)
                    {
                        Children.Add(new IVFolderVM(theChip, theChip.IonicIV));
                    }
                }


                foreach (var chip in theChip.Junctions)
                {
                    Children.Add(new JunctionVM(chip));
                }
                Properties.Settings.Default["DefaultChip"] = this.ToString();
            }

            base.DoListOpen(value);
        }

        protected override void DoClick()
        {
            Column1.Add(new NoteListVM(this.theChip, this.theChip.Note));
            PlotStats(null);
        }

        #region Plotting
        ColumnChartViewModel _RunTime = null;
        ScatterChartViewModel _BestJunction = null;
        ScatterChartViewModel _CompareJunction = null;
        ScatterChartViewModel _IVJunction = null;
        ScatterChartViewModel _IonicJunction = null;
        ScatterChartViewModel _AllJunction = null;

        private ColumnChartViewModel PlotStats(ColumnChartViewModel colChart)
        {
            string timeTrace = App.ConvertFilePaths(theChip.Folder + "\\RunTime.png");
            if (File.Exists(timeTrace) == false)
            {

                //_RunTime = new ColumnChartViewModel { Title = "Run Time", DarkLayout = true };
                //_RunTime.SaveFilename = Chip.Folder + "\\PlotStats.png";
                PlotStatistics_DelayLoadRequested(null, null, null, null);
            }
            else
            {
                _RunTime = new ColumnChartViewModel { Title = "Run Time", DarkLayout = true };
                _RunTime.ExampleImage = App.ConvertFilePaths(theChip.Folder + "\\RunTime.png"); ;
                _RunTime.ChartOwner = theChip;
                _RunTime.ShowLoadScreen = true;
                _RunTime.DelayLoadRequested += PlotStatistics_DelayLoadRequested;

                _IVJunction = new ScatterChartViewModel { Title = "IV Curves", DarkLayout = true };
                _IVJunction.ExampleImage = App.ConvertFilePaths(theChip.Folder + "\\IVJunction.png"); ;
                _IVJunction.ChartOwner = theChip;
                _IVJunction.ShowLoadScreen = true;
                _IVJunction.DelayLoadRequested += _BestJunction_DelayLoadRequested;

                _BestJunction = new ScatterChartViewModel { Title = "Best IV Curves", DarkLayout = true };
                _BestJunction.ExampleImage = App.ConvertFilePaths(theChip.Folder + "\\BestJunction.png"); ;
                _BestJunction.ChartOwner = theChip;
                _BestJunction.ShowLoadScreen = true;
                _BestJunction.DelayLoadRequested += _BestJunction_DelayLoadRequested;

                _IonicJunction = new ScatterChartViewModel { Title = "Ionic Traces", DarkLayout = true };
                _IonicJunction.ExampleImage = App.ConvertFilePaths(theChip.Folder + "\\IonicJunction.png"); ;
                _IonicJunction.ChartOwner = theChip;
                _IonicJunction.ShowLoadScreen = true;
                _IonicJunction.DelayLoadRequested += _BestJunction_DelayLoadRequested;


                _CompareJunction = new ScatterChartViewModel { Title = "Compare Junctions", DarkLayout = true };
                _CompareJunction.ExampleImage = App.ConvertFilePaths(theChip.Folder + "\\CompareJunction.png"); ;
                _CompareJunction.ChartOwner = theChip;
                _CompareJunction.ShowLoadScreen = true;
                _CompareJunction.DelayLoadRequested += _BestJunction_DelayLoadRequested;
                // return _RunTime;
            }

            if (_RunTime != null)
                Column2.Add(_RunTime);

            if (_IVJunction != null)
                Column1.Add(_IVJunction);
            if (_IonicJunction != null)
                Column2.Add(_IonicJunction);

            if (_CompareJunction != null)
                Column1.Add(_CompareJunction);
            
            if (_BestJunction != null)
                Column2.Add(_BestJunction);
           

            return null;
        }

        private void _BestJunction_DelayLoadRequested(ScatterChartViewModel sChart, object Owner, object param1, object param2)
        {
            PlotStatistics_DelayLoadRequested(null, null, null, null);
        }

        private ColumnChartViewModel PlotStatistics_DelayLoadRequested(ColumnChartViewModel colChart, object Owner, object param1, object param2)
        {

            Dictionary<string, DataModel.Fileserver.FileTrace> bestJunction = new Dictionary<string, DataModel.Fileserver.FileTrace>();
            Dictionary<string, DataModel.Fileserver.FileTrace> IVJunction = new Dictionary<string, DataModel.Fileserver.FileTrace>();
            Dictionary<string, DataModel.Fileserver.FileTrace> IonicJunction = new Dictionary<string, DataModel.Fileserver.FileTrace>();
            double bRunTime = 0;
            int bIVCurves = 0;

            IonicJunction = theChip.IonicTraces;
            int bIOCurves = theChip.IonicIV.Count;
            double[] runTimes = new double[theChip.Junctions.Count];
            string[] runJunction = new string[theChip.Junctions.Count];
            int cc = 0;
            if (_CompareJunction == null)
            {
                _CompareJunction = new ScatterChartViewModel { ChartType = ScatterChartViewModel.ChartTypeEnum.Line, Title = "Compare Junctions", XLabel = "Voltage (mV)", YLabel = "Current (pA)" };
            }
            else
                _CompareJunction.Series.Clear();
           
            _CompareJunction.SaveFilename = theChip.Folder + "\\CompareJunction.png";
            _CompareJunction.ShowLoadScreen = false;
            DataModel.Fileserver.FileTrace xValues;
            foreach (var junction in theChip.Junctions)
            {
                var runTime = junction.RunTime;
                runTimes[cc] = runTime;
                runJunction[cc] = junction.JunctionName;
                var traces = junction.TunnelTraces;
                if (traces != null && traces.Count > 0)
                {
                    try
                    {
                        xValues = traces["Voltage"];
                        foreach (var kvp in traces)
                        {
                            if (kvp.Key != "Voltage")
                            {
                                var yValues = kvp.Value;
                                _CompareJunction.AddSeries(junction.JunctionName, xValues, yValues);
                                break;
                            }
                        }
                    }
                    catch { }
                }
                if (runTime > bRunTime)
                {
                    bRunTime = runTime;
                    bestJunction = traces;
                }
                if (junction.TunnelIV.Count > bIVCurves)
                {
                    bIVCurves = junction.TunnelIV.Count;
                    IVJunction = traces;
                }
                if (junction.IonicIV.Count > bIOCurves)
                {
                    bIOCurves = junction.IonicIV.Count;
                    IonicJunction = junction.IonicTraces;
                }
                cc++;
            }

            // if (bestJunction != null && bestJunction.Keys.Count > 1)
            {
                if (_BestJunction == null)
                {
                    _BestJunction = new ScatterChartViewModel { ChartType = ScatterChartViewModel.ChartTypeEnum.Line, Title = "Best IV Curves", XLabel = "Voltage (mV)", YLabel = "Current (pA)" };
                }
                else
                    _BestJunction.Series.Clear();
                _BestJunction.SaveFilename = theChip.Folder + "\\BestJunction.png";
                _BestJunction.ShowLoadScreen = false;
                if (bestJunction != null && bestJunction.Count > 0)
                {
                    try
                    {
                        xValues = bestJunction["Voltage"];
                        foreach (KeyValuePair<string, DataModel.Fileserver.FileTrace> kvp in bestJunction)
                        {
                            if (kvp.Key != "Voltage")
                            {
                                var yValues = kvp.Value;
                                _BestJunction.AddSeries(kvp.Key, xValues, yValues);
                            }
                        }
                    }
                    catch { }
                }


                // Column2.Add(sChart);
            }

            // if (IVJunction != null && IVJunction.Keys.Count > 0)
            {
                if (_IVJunction == null)
                    _IVJunction = new ScatterChartViewModel { ChartType = ScatterChartViewModel.ChartTypeEnum.Line, Title = "IV Curves", XLabel = "Voltage (mV)", YLabel = "Current (pA)" };
                else
                    _IVJunction.Series.Clear();
                if (IVJunction != null && IVJunction.Count > 0)
                {
                    try
                    {
                        xValues = IVJunction["Voltage"];
                        foreach (KeyValuePair<string, DataModel.Fileserver.FileTrace> kvp in IVJunction)
                        {
                            if (kvp.Key != "Voltage")
                            {
                                var yValues = kvp.Value;
                                _IVJunction.AddSeries(kvp.Key, xValues, yValues);
                            }
                        }
                    }
                    catch { }
                }
                _IVJunction.SaveFilename = theChip.Folder + "\\IVJunction.png";
                _IVJunction.ShowLoadScreen = false;
                // Column1.Add(sChart);

            }

            // if (IonicJunction != null && IonicJunction.Keys.Count > 0)
            {
                if (_IonicJunction == null)
                    _IonicJunction = new ScatterChartViewModel { ChartType = ScatterChartViewModel.ChartTypeEnum.Line, Title = "Ionic Curves", XLabel = "Voltage (mV)", YLabel = "Current (pA)" };
                else
                    _IonicJunction.Series.Clear();
                if (IonicJunction != null && IonicJunction.Count > 0)
                {
                    try
                    {
                        xValues = IonicJunction["Voltage"];
                        foreach (KeyValuePair<string, DataModel.Fileserver.FileTrace> kvp in IonicJunction)
                        {
                            if (kvp.Key != "Voltage")
                            {
                                var yValues = kvp.Value;
                                _IonicJunction.AddSeries(kvp.Key, xValues, yValues);
                            }
                        }
                    }
                    catch { }
                }
                _IonicJunction.SaveFilename = theChip.Folder + "\\IonicJunction.png";
                _IonicJunction.ShowLoadScreen = false;
                // Column1.Add(sChart);

            }

            try
            {
                if (_RunTime == null)
                    _RunTime = new ColumnChartViewModel { Title = "Run Time", DarkLayout = true };
                else
                    _RunTime.Series.Clear();
                _RunTime.SaveFilename = theChip.Folder + "\\RunTime.png";

                var chartData = new System.Collections.ObjectModel.ObservableCollection<CategoryColumn>();
                for (int i = 0; i < runJunction.Length; i++)
                    chartData.Add(new CategoryColumn { Category = runJunction[i], Number = runTimes[i] / 60 / 60 });

                _RunTime.AddSeries("Run Times", chartData);
                _RunTime.ShowLoadScreen = false;
            }
            catch { }
            //Column2.Add(_RunTime);
            return null;
        }
        #endregion

        public override void Delete()
        {
            theChip.Delete();
           // RequestDataTreeRefresh();
        }

        public override void ExportToRoche(string FolderName)
        {
            base.ExportToRoche(FolderName);
        }
    }
}
