using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TunnelVision.DataModel;

namespace TunnelVision.Controls.TreeViewModel
{

    public class JunctionVM : TreeNodeVM
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
            return Junction().Chip;
        }
        public override Junction Junction()
        {
            return theJunction;
        }

        private enum JunctionListStyle { ByTime, ByCondition, ByAnalyte, ByGrid };
        private JunctionListStyle _ListStyle = JunctionListStyle.ByCondition;

        public DataModel.Junction theJunction { get; set; }

        public JunctionVM(DataModel.Junction junction) : base(junction)
        {
            theJunction = junction;
            RateItem(junction.GoodJunction);


            OpenList = ((string)Properties.Settings.Default["DefaultJunction"] == theJunction.JunctionName);

            MenuButtons.BrokenButton = Visibility.Collapsed;
        }

        protected override void DoListOpen(bool value)
        {
            if (value == true)
            {

                Children.Clear();
                if (theJunction.IonicIV.Count > 0)
                {
                    Children.Add(new IVFolderVM(theJunction, theJunction.IonicIV));
                }

                if (theJunction.TunnelIV.Count > 0)
                {
                    Children.Add(new IVFolderVM(theJunction, theJunction.TunnelIV));
                }

                List<DataModel.TraceFolder> folders = null;
                if (_ListStyle == JunctionListStyle.ByCondition)
                {
                    folders = theJunction.Folders;
                }
                if (_ListStyle == JunctionListStyle.ByAnalyte)
                {
                    folders = theJunction.AnalyteFolders;
                }
                if (_ListStyle == JunctionListStyle.ByTime)
                {
                    var traces = theJunction.TimedTraces;
                    foreach (var trace in traces)
                    {
                        Children.Add(new DataTraceVM(trace, trace.Condition));
                    }
                }
                if (_ListStyle == JunctionListStyle.ByGrid)
                {
                    var traces = theJunction.TimedTraces;
                    GridItems GI = new GridItems(traces);
                    Children.Add(GI);
                }
                if (folders != null && folders.Count > 1)
                {
                    foreach (var f in folders)
                    {
                        Children.Add(new FolderVM(f));
                    }
                }
                else
                {
                    if (folders != null && folders.Count > 0)
                    {
                        foreach (var f in folders[0].Folders)
                        {
                            Children.Add(new TraceFolderVM(f));
                        }
                    }
                }

                ListOpen = Visibility.Visible;
                Properties.Settings.Default["DefaultJunction"] = theJunction.JunctionName;
            }

            base.DoListOpen(value);
        }

        protected override void DoClick()
        {

            Column1.Clear();
            Column2.Clear();
            Column3.Clear();
            Column4.Clear();

            Column1.Add(new NoteListVM(this.theJunction, this.theJunction.Note));

            ScatterChartViewModel sChart;
            string timeTrace = App.ConvertFilePaths(theJunction.Folder + "\\TimeTrace.png");
            //theJunction.Folder.ToLower().Replace(@"s:\research\tunnelsurfer", App.DataDirectory) + "\\TimeTrace.png";// AppDomain.CurrentDomain.BaseDirectory + "icons\\TimeTrace.png";

            if (App.ForceHighQuality == true)
            {
                sChart = JunctionVM.TimeTrace(theJunction, "");
                sChart.SaveFilename = theJunction.Folder + "\\TimeTrace.png";
            }
            else
            {
                if (File.Exists(timeTrace) == false)
                {
                    timeTrace = AppDomain.CurrentDomain.BaseDirectory + "icons\\TimeTrace.png";
                    sChart = new ScatterChartViewModel { Title = "Time Trace", XLabel = "Time (s)", YLabel = "Current (pA)", ChartType = ScatterChartViewModel.ChartTypeEnum.Line };
                    sChart.ExampleImage = timeTrace;
                    sChart.ChartOwner = theJunction;
                    sChart.ShowLoadScreen = true;
                    sChart.DelayLoadRequested += SChart_DelayLoadRequested;

                    //sChart = JunctionVM.TimeTrace(theJunction, "");
                    //sChart.SaveFilename = theJunction.Folder + "\\TimeTrace.png";
                    ////  sChart.SaveQualityChartImage(sChart.ExampleImage);
                }
                else
                {
                    sChart = new ScatterChartViewModel { Title = "Time Trace", XLabel = "Time (s)", YLabel = "Current (pA)", ChartType = ScatterChartViewModel.ChartTypeEnum.Line };
                    sChart.ExampleImage = timeTrace;
                    sChart.ChartOwner = theJunction;
                    sChart.ShowLoadScreen = true;
                    sChart.DelayLoadRequested += SChart_DelayLoadRequested;
                }
            }

            Column2.Add(sChart);

            var plots = ConditionPlots(theJunction, "", "Junc");
            Column1.Add(plots[0]);
            for (int i = 1; i < plots.Count; i++)
                Column2.Add(plots[i]);

        }

        #region Plotting
        private void SChart_DelayLoadRequested(ScatterChartViewModel sChart, object Owner, object param1, object param2)
        {
            TimeTrace((DataModel.Junction)Owner, "", sChart);
            sChart.SaveFilename = theJunction.Folder + "\\TimeTrace.png";
            sChart.ShowLoadScreen = false;
        }




        private static ScatterChartViewModel ConditionHist(ScatterChartViewModel sChartRaw, List<DataModel.DataTrace> traces, List<string> conditionNames, string fileTag)
        {
            double sum = 0;

            foreach (var condition in conditionNames)
            {
                double[] xValues = null;
                double[] yValues = null;
                var anaTraces = (from x in traces where x.Condition == condition select x);
                double minX = 100000;
                double maxX = -100000;
                List<List<DataModel.Fileserver.FileTrace>> dTrace = new List<List<DataModel.Fileserver.FileTrace>>();
                foreach (var trace in anaTraces)
                {
                    try
                    {
                        var hist = trace.GetHistograms(fileTag);//"E2_Raw");
                        var s = hist[0].Trace.Sum() + hist[1].Trace.Sum();
                        if (!(double.IsNaN(s) || double.IsInfinity(s)))
                        {
                            sum += s;
                            dTrace.Add(hist);
                            if (hist[0].Trace[0] < minX)
                                minX = hist[0].Trace[0];
                            if (hist[0].Trace[hist[0].Trace.Length - 1] > maxX)
                                maxX = hist[0].Trace[hist[0].Trace.Length - 1];
                        }
                    }
                    catch { }
                }
                if (maxX != -100000)
                {
                    int n = (int)((maxX - minX) / 10);
                    xValues = new double[n];
                    yValues = new double[n];
                    for (int i = 0; i < n; i++)
                    {
                        xValues[i] = minX + i * 10;
                    }
                    int k; int j; int j2;
                    foreach (var hist in dTrace)
                    {
                        try
                        {
                            j = (int)((hist[0].Trace[0] - minX) / 10);
                            j2 = (int)((hist[0].Trace[hist[0].Trace.Length - 1] - minX) / 10);
                            for (k = 0; k < (j2 - j); k++)
                            {
                                yValues[j + k] += hist[1].Trace[k];
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.Print(ex.Message);
                        }
                    }
                    var FixLine = JunctionVM.FixLine(yValues, xValues);
                    sChartRaw.AddSeries(condition, FixLine.Item2, FixLine.Item1);
                }
            }

            if (sum > 0)
                return (sChartRaw);
            else
                return null;
        }

        private static void Condition_DelayLoadRequested(ScatterChartViewModel sChart, object Owner, object param1, object param2)
        {
            // TimeTrace((DataModel.Junction)Owner, "", sChart);
            ConditionHist(sChart, (List<DataModel.DataTrace>)Owner, (List<string>)param1, (string)param2);
            sChart.ShowLoadScreen = false;
        }

        public static List<ScatterChartViewModel> ConditionPlots(DataModel.Junction Junction, string analyteSearch = "", string title = "")
        {
            List<ScatterChartViewModel> plots = new List<ScatterChartViewModel>();

            List<DataModel.DataTrace> traces;
            string titleAdd = "";
            if (analyteSearch == "")
                traces = Junction.Traces;
            else
            {
                traces = (from x in Junction.Traces where x.Analyte == analyteSearch select x).ToList();
                titleAdd = "" + analyteSearch;
            }

            var conditionNames = (from x in traces select x.Condition).Distinct().ToList();

            try
            {
                ScatterChartViewModel sChartRaw = new ScatterChartViewModel { Title = "Raw Currents " + titleAdd, XLabel = "Current (pA)", YLabel = "norm. frequency", ChartType = ScatterChartViewModel.ChartTypeEnum.Histogram };
                string timeTrace = App.ConvertFilePaths(Junction.Folder + "\\" + analyteSearch + title + sChartRaw.Title + ".png");
                if (File.Exists(timeTrace) == false)
                {
                    var hist = ConditionHist(sChartRaw, traces, conditionNames, "E2_Raw");
                    sChartRaw.SaveFilename = Junction.Folder + "\\" + analyteSearch + title + sChartRaw.Title + ".png";
                    if (hist != null)
                        plots.Add(hist);
                }
                else
                {
                    sChartRaw.ExampleImage = timeTrace;
                    sChartRaw.ChartOwner = traces;
                    sChartRaw.OwnerParam1 = conditionNames;
                    sChartRaw.OwnerParam2 = "E2_Raw";
                    sChartRaw.ShowLoadScreen = true;
                    sChartRaw.DelayLoadRequested += Condition_DelayLoadRequested;
                    plots.Add(sChartRaw);
                }

                sChartRaw = new ScatterChartViewModel { Title = "Peak Levels " + titleAdd, XLabel = "Current (pA)", YLabel = "norm. frequency", ChartType = ScatterChartViewModel.ChartTypeEnum.Histogram };
                timeTrace = App.ConvertFilePaths(Junction.Folder + "\\" + analyteSearch + title + sChartRaw.Title + ".png");
                if (File.Exists(timeTrace) == false)
                {
                    var hist = ConditionHist(sChartRaw, traces, conditionNames, "E2_cusumLevels");
                    sChartRaw.SaveFilename = Junction.Folder + "\\" + analyteSearch + title + sChartRaw.Title + ".png";
                    if (hist != null)
                        plots.Add(hist);
                }
                else
                {
                    sChartRaw.ExampleImage = timeTrace;
                    sChartRaw.ChartOwner = traces;
                    sChartRaw.OwnerParam1 = conditionNames;
                    sChartRaw.OwnerParam2 = "E2_cusumLevels";
                    sChartRaw.ShowLoadScreen = true;
                    sChartRaw.DelayLoadRequested += Condition_DelayLoadRequested;
                    plots.Add(sChartRaw);
                }

                sChartRaw = new ScatterChartViewModel { Title = "Spike Levels " + titleAdd, XLabel = "Current (pA)", YLabel = "norm. frequency", ChartType = ScatterChartViewModel.ChartTypeEnum.Histogram };
                timeTrace = App.ConvertFilePaths(Junction.Folder + "\\" + analyteSearch + title + sChartRaw.Title + ".png");
                if (File.Exists(timeTrace) == false)
                {
                    var hist = ConditionHist(sChartRaw, traces, conditionNames, "E2_Spikes");
                    sChartRaw.SaveFilename = Junction.Folder + "\\" + analyteSearch + title + sChartRaw.Title + ".png";
                    if (hist != null)
                        plots.Add(hist);

                }
                else
                {
                    sChartRaw.ExampleImage = timeTrace;
                    sChartRaw.ChartOwner = traces;
                    sChartRaw.OwnerParam1 = conditionNames;
                    sChartRaw.OwnerParam2 = "E2_Spikes";
                    sChartRaw.ShowLoadScreen = true;
                    sChartRaw.DelayLoadRequested += Condition_DelayLoadRequested;
                    plots.Add(sChartRaw);
                }
            }
            catch { }
            return plots;
        }


        public static Tuple<double[], double[]> FixLine(double[] line, double[] xValues)
        {
            if (line == null)
                return null;
            double maxV = line.Max() * .01;
            int minI = 0;
            for (int i = 0; i < line.Length; i++)
                if (line[i] > maxV)
                {
                    minI = i;
                    break;
                }
            int maxI = line.Length;
            for (int i = minI + 1; i < line.Length; i++)
                if (line[i] > maxV)
                    maxI = i;

            double[] lineOut = new double[maxI - minI];
            double[] xOut = new double[lineOut.Length];
            int cc = 0;
            for (int i = minI; i < maxI; i++)
            {
                lineOut[cc] = line[i];
                xOut[cc] = xValues[i];
                cc++;
            }
            return new Tuple<double[], double[]>(xOut, lineOut);
        }

        private static ScatterChartViewModel AnalyteHist(ScatterChartViewModel sChartRaw, List<DataModel.DataTrace> traces, List<string> analyteNames, string fileTag)
        {
            double sum = 0;

            foreach (var ana in analyteNames)
            {
                try
                {
                    double[] xValues = null;
                    double[] yValues = null;
                    var anaTraces = (from x in traces where x.Analyte == ana select x);
                    double minX = 100000;
                    double maxX = -100000;
                    List<List<DataModel.Fileserver.FileTrace>> dTrace = new List<List<DataModel.Fileserver.FileTrace>>();
                    foreach (var trace in anaTraces)
                    {
                        try
                        {
                            var hist = trace.GetHistograms(fileTag);//"E2_Raw");
                            var s = hist[0].Trace.Sum() + hist[1].Trace.Sum();
                            if (!(double.IsNaN(s) || double.IsInfinity(s)))
                            {
                                sum += s;
                                dTrace.Add(hist);
                                if (hist[0].Trace[0] < minX)
                                    minX = hist[0].Trace[0];
                                if (hist[0].Trace[hist[0].Trace.Length - 1] > maxX)
                                    maxX = hist[0].Trace[hist[0].Trace.Length - 1];
                            }
                        }
                        catch { }
                    }

                    if (maxX != -100000)
                    {
                        int n = (int)((maxX - minX) / 10);
                        xValues = new double[n];
                        yValues = new double[n];
                        for (int i = 0; i < n; i++)
                        {
                            xValues[i] = minX + i * 10;
                        }
                        int k; int j; int j2;
                        foreach (var hist in dTrace)
                        {
                            try
                            {
                                j = (int)((hist[0].Trace[0] - minX) / 10);
                                j2 = (int)((hist[0].Trace[hist[0].Trace.Length - 1] - minX) / 10);
                                for (k = 0; k < (j2 - j); k++)
                                {
                                    yValues[j + k] += hist[1].Trace[k];
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.Print(ex.Message);
                            }
                        }
                        var FixLine = JunctionVM.FixLine(yValues, xValues);
                        sChartRaw.AddSeries(ana, FixLine.Item2, FixLine.Item1);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                }
            }

            if (sum > 0)
                return (sChartRaw);
            else
                return null;
        }

        private static void Analyte_DelayLoadRequested(ScatterChartViewModel sChart, object Owner, object param1, object param2)
        {

            AnalyteHist(sChart, (List<DataModel.DataTrace>)Owner, (List<string>)param1, (string)param2);
            sChart.ShowLoadScreen = false;
        }

        public static List<ScatterChartViewModel> AnalytePlots(DataModel.Junction Junction, string Condition, string title = "")
        {
            List<ScatterChartViewModel> plots = new List<ScatterChartViewModel>();

            List<DataModel.DataTrace> traces;

            traces = (from x in Junction.Traces where x.Condition == Condition select x).ToList();

            var analyteNames = (from x in traces select x.Analyte).Distinct().ToList();

            try
            {
                ScatterChartViewModel sChartRaw = new ScatterChartViewModel { Title = "Raw Currents " + Condition, XLabel = "Current (pA)", YLabel = "norm. frequency", ChartType = ScatterChartViewModel.ChartTypeEnum.Histogram };
                string timeTrace = App.ConvertFilePaths(Junction.Folder + "\\" + Condition + title + sChartRaw.Title + ".png");
                if (File.Exists(timeTrace) == false)
                {
                    var hist = AnalyteHist(sChartRaw, traces, analyteNames, "E2_Raw");
                    sChartRaw.SaveFilename = Junction.Folder + "\\" + Condition + title + sChartRaw.Title + ".png";
                    if (hist != null)
                        plots.Add(hist);
                }
                else
                {
                    sChartRaw.ExampleImage = timeTrace;
                    sChartRaw.ChartOwner = traces;
                    sChartRaw.OwnerParam1 = analyteNames;
                    sChartRaw.OwnerParam2 = "E2_Raw";
                    sChartRaw.ShowLoadScreen = true;
                    sChartRaw.DelayLoadRequested += Analyte_DelayLoadRequested;
                    plots.Add(sChartRaw);
                }

                sChartRaw = new ScatterChartViewModel { Title = "Peak Levels " + Condition, XLabel = "Current (pA)", YLabel = "norm. frequency", ChartType = ScatterChartViewModel.ChartTypeEnum.Histogram };
                timeTrace = App.ConvertFilePaths(Junction.Folder + "\\" + Condition + title + sChartRaw.Title + ".png");
                if (File.Exists(timeTrace) == false)
                {
                    var hist = AnalyteHist(sChartRaw, traces, analyteNames, "E2_cusumLevels");
                    sChartRaw.SaveFilename = Junction.Folder + "\\" + Condition + title + sChartRaw.Title + ".png";
                    if (hist != null)
                        plots.Add(hist);
                }
                else
                {
                    sChartRaw.ExampleImage = timeTrace;
                    sChartRaw.ChartOwner = traces;
                    sChartRaw.OwnerParam1 = analyteNames;
                    sChartRaw.OwnerParam2 = "E2_Raw";
                    sChartRaw.ShowLoadScreen = true;
                    sChartRaw.DelayLoadRequested += Analyte_DelayLoadRequested;
                    plots.Add(sChartRaw);
                }




                sChartRaw = new ScatterChartViewModel { Title = "Spike Levels " + Condition, XLabel = "Current (pA)", YLabel = "norm. frequency", ChartType = ScatterChartViewModel.ChartTypeEnum.Histogram };
                timeTrace = App.ConvertFilePaths(Junction.Folder + "\\" + Condition + title + sChartRaw.Title + ".png");
                if (File.Exists(timeTrace) == false)
                {
                    var hist = AnalyteHist(sChartRaw, traces, analyteNames, "E2_Spikes");
                    sChartRaw.SaveFilename = Junction.Folder + "\\" + Condition + title + sChartRaw.Title + ".png";
                    if (hist != null)
                        plots.Add(hist);
                }
                else
                {
                    sChartRaw.ExampleImage = timeTrace;
                    sChartRaw.ChartOwner = traces;
                    sChartRaw.OwnerParam1 = analyteNames;
                    sChartRaw.OwnerParam2 = "E2_Raw";
                    sChartRaw.ShowLoadScreen = true;
                    sChartRaw.DelayLoadRequested += Analyte_DelayLoadRequested;
                    plots.Add(sChartRaw);
                }



            }
            catch { }
            return plots;
        }

        public static ScatterChartViewModel TimeTrace(DataModel.Junction Junction, string search, ScatterChartViewModel sChart = null)
        {
            if (sChart == null)
                sChart = new ScatterChartViewModel { Title = "Time Trace", XLabel = "Time (s)", YLabel = "Current (pA)", ChartType = ScatterChartViewModel.ChartTypeEnum.Line };

            sChart.Series.Clear();

            List<DataModel.DataTrace> lines;
            if (search == "")
                lines = Junction.Traces.OrderBy(x => x.DateAcquired).ToList();
            else
                lines = (from x in Junction.Traces where (x.Condition == search || x.Analyte == search) select x).OrderBy(x => x.DateAcquired).ToList();

            double currentX = 0;
            long pointCount = 0;
            Dictionary<string, Tuple<string, List<double>, List<double>>> lineData = new Dictionary<string, Tuple<string, List<double>, List<double>>>();
            foreach (var line in lines)
            {
                try
                {
                    var l = line.ShortCurrent;
                    int step = 8;
                    if (l == null)
                    {
                        l = line.Current;
                        step = 20000;
                    }

                    var xData = new List<double>(); xData.AddRange(new double[(int)Math.Floor(l.Trace.Length / (double)step) - 2]);
                    var yData = new List<double>(); yData.AddRange(new double[xData.Count]);
                    int cc = 0;
                    double x = 0;
                    double y = 0;
                    for (int i = 0; i < l.Trace.Length; i += step)
                    {
                        if (cc < xData.Count)
                        {
                            x = currentX + l.GetTime(i);
                            y = l.Trace[i];
                            if (double.IsInfinity(y) || double.IsNaN(y))
                            {
                                y = 0;
                            }
                            xData[cc] = x;
                            yData[cc] = y;
                            cc++;
                            pointCount++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    string condition = line.BigCondition;
                    if (lineData.ContainsKey(condition))
                    {
                        var t = lineData[condition];
                        t.Item2.AddRange(xData);
                        t.Item3.AddRange(yData);
                    }
                    else
                    {
                        lineData.Add(condition, new Tuple<string, List<double>, List<double>>(line.TraceName, xData, yData));
                    }

                    currentX = x;
                }
                catch (Exception ex)
                {

                }

            }

            foreach (KeyValuePair<string, Tuple<string, List<double>, List<double>>> kvp in lineData)
            {
                sChart.AddSeries(kvp.Value.Item1, kvp.Value.Item2.ToArray(), kvp.Value.Item3.ToArray());
            }

            return sChart;
        }

        public static List<ScatterChartViewModel> StatsPlots(DataModel.Junction Junction, string search)
        {
            List<ScatterChartViewModel> plots = new List<ScatterChartViewModel>();


            List<DataModel.DataTrace> traces;
            if (search == "")
                traces = Junction.Traces;
            else
                traces = (from x in Junction.Traces where (x.Condition == search || x.Analyte == search) select x).ToList();

            try
            {
                ScatterChartViewModel sChartRaw = new ScatterChartViewModel { Title = "Raw Currents", XLabel = "Current (pA)", YLabel = "norm. frequency", ChartType = ScatterChartViewModel.ChartTypeEnum.Histogram };
                double sum = 0;
                foreach (var trace in traces)
                {
                    try
                    {
                        var hist = trace.GetHistograms("E2_Raw");

                        sum = hist[0].Trace.Sum() + hist[1].Trace.Sum();
                        if (double.IsNaN(sum) || double.IsInfinity(sum))
                        {

                            break;
                        }
                        sChartRaw.AddSeries(trace.TraceName, hist[1].Trace, hist[0].Trace);
                    }
                    catch { }
                }
                if (!(double.IsNaN(sum) || double.IsInfinity(sum)))
                    plots.Add(sChartRaw);
                sum = 0;

                sChartRaw = new ScatterChartViewModel { Title = "Peak Levels", XLabel = "Current (pA)", YLabel = "norm. frequency", ChartType = ScatterChartViewModel.ChartTypeEnum.Histogram };
                foreach (var trace in traces)
                {
                    try
                    {
                        var hist = trace.GetHistograms("E2_cusumLevels");

                        sum = hist[0].Trace.Sum() + hist[1].Trace.Sum();
                        if (double.IsNaN(sum) || double.IsInfinity(sum))
                        {

                            break;
                        }
                        sChartRaw.AddSeries(trace.TraceName, hist[1].Trace, hist[0].Trace);
                    }
                    catch { }
                }
                if (!(double.IsNaN(sum) || double.IsInfinity(sum)))
                    plots.Add(sChartRaw);

                sChartRaw = new ScatterChartViewModel { Title = "Spike Levels", XLabel = "Current (pA)", YLabel = "norm. frequency", ChartType = ScatterChartViewModel.ChartTypeEnum.Histogram };
                foreach (var trace in traces)
                {
                    try
                    {
                        var hist = trace.GetHistograms("E2_Spikes");

                        sum = hist[0].Trace.Sum() + hist[1].Trace.Sum();
                        if (double.IsNaN(sum) || double.IsInfinity(sum))
                        {

                            break;
                        }
                        sChartRaw.AddSeries(trace.TraceName, hist[1].Trace, hist[0].Trace);

                    }
                    catch { }
                }
                if (!(double.IsNaN(sum) || double.IsInfinity(sum)))
                    plots.Add(sChartRaw);

                //sChartRaw = new ScatterChartViewModel { Title = "Level Times", XLabel = "Time (S)", YLabel = "norm. frequency", ChartType = ScatterChartViewModel.ChartTypeEnum.Histogram };
                //foreach (var trace in traces)
                //{
                //    try
                //    {
                //        var hist = trace.GetHistograms("H2_CUMSUMTimes");

                //        sum = hist[0].Sum() + hist[1].Sum();
                //        if (double.IsNaN(sum) || double.IsInfinity(sum))
                //        {

                //            break;
                //        }
                //        sChartRaw.AddSeries(trace.TraceName, hist[1], hist[0]);

                //    }
                //    catch { }
                //}
                //if (!(double.IsNaN(sum) || double.IsInfinity(sum)))
                //    plots.Add(sChartRaw);
            }
            catch { }
            return plots;
        }

        #endregion

        #region ListStyles
        private ICommand ArrangeByTimeCommand;
        public ICommand ArrangeByTime
        {
            get
            {
                return ArrangeByTimeCommand ?? (ArrangeByTimeCommand = new Controls.Converters.CommandHandler(() => ArrangeByTime_Clicked(), true));
            }
        }
        public void ArrangeByTime_Clicked()
        {

            _ListStyle = JunctionListStyle.ByTime;
            OpenList = true;

        }

        private ICommand ArrangeByConditionCommand;
        public ICommand ArrangeByCondition
        {
            get
            {
                return ArrangeByConditionCommand ?? (ArrangeByConditionCommand = new Controls.Converters.CommandHandler(() => ArrangeByCondition_Clicked(), true));
            }
        }
        public void ArrangeByCondition_Clicked()
        {
            _ListStyle = JunctionListStyle.ByCondition;
            OpenList = true;
        }

        private ICommand ArrangeByAnalyteCommand;
        public ICommand ArrangeByAnalyte
        {
            get
            {
                return ArrangeByAnalyteCommand ?? (ArrangeByAnalyteCommand = new Controls.Converters.CommandHandler(() => ArrangeByAnalyte_Clicked(), true));
            }
        }
        public void ArrangeByAnalyte_Clicked()
        {
            _ListStyle = JunctionListStyle.ByAnalyte;
            OpenList = true;
        }


        private ICommand ArrangeByGridCommand;
        public ICommand ArrangeByGrid
        {
            get
            {
                return ArrangeByGridCommand ?? (ArrangeByGridCommand = new Controls.Converters.CommandHandler(() => ArrangeByGrid_Clicked(), true));
            }
        }
        public void ArrangeByGrid_Clicked()
        {
            _ListStyle = JunctionListStyle.ByGrid;
            OpenList = true;
        }
        #endregion

        public override void Delete()
        {
            theJunction.Delete();
            // RequestDataTreeRefresh();
        }

        private string ToCSV(string fileText, DataTable dt, string[] columnNames)
        {
            foreach (DataRow row in dt.Rows)
            {
                foreach (var col in columnNames)
                {
                    try
                    {
                        fileText += col + "," + row[col].ToString() + "\n";
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print("");
                    }
                }
            }
            return fileText;
        }
        public override void ExportToRoche(string FolderName)
        {
#if Travel
            using (System.Data.SQLite.SQLiteConnection connection = new System.Data.SQLite.SQLiteConnection(App.ConnectionString))
            {
#else
                  using (MySql.Data.MySqlClient.MySqlConnection connection = new MySql.Data.MySqlClient.MySqlConnection(App.ConnectionString))
            {
#endif
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                string ExperimentFile = "";
                DataTable dt = selectQuery(connection, "select  *  from Experiments where Experiments.Id= " + theJunction.Experiment.Id);

                string[] columnNames = dt.Columns.Cast<DataColumn>().Where(x => (x.ColumnName.Contains("Last") == false))
                                .Select(x => x.ColumnName)
                                .ToArray();
                ExperimentFile = ToCSV(ExperimentFile, dt, columnNames);

                dt = selectQuery(connection, "select NoteTime,ShortNote from enotes where Experiment_Id=" + theJunction.Experiment.Id);

                columnNames = dt.Columns.Cast<DataColumn>()
                               .Select(x => x.ColumnName)
                               .ToArray();
                ExperimentFile = ToCSV(ExperimentFile, dt, columnNames);
                File.WriteAllText(FolderName + "\\ExperimentHeader.csv", ExperimentFile);

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                string BatchFile = "";
                dt = selectQuery(connection, "select  *  from Batches where Id= " + theJunction.Chip.Batch.Id);

                columnNames = dt.Columns.Cast<DataColumn>().Where(x => (x.ColumnName.Contains("_Id") == false))
                               .Select(x => x.ColumnName)
                               .ToArray();
                BatchFile = ToCSV(BatchFile, dt, columnNames);

                dt = selectQuery(connection, "select NoteTime,ShortNote from enotes where Batch_Id=" + theJunction.Chip.Batch.Id);

                columnNames = dt.Columns.Cast<DataColumn>()
                               .Select(x => x.ColumnName)
                               .ToArray();
                BatchFile = ToCSV(BatchFile, dt, columnNames);
                File.WriteAllText(FolderName + "\\BatchHeader.csv", BatchFile);

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                string ChipFile = "";
                dt = selectQuery(connection, "select  *  from Chips where Id= " + theJunction.Chip.Id);

                columnNames = dt.Columns.Cast<DataColumn>().Where(x => (x.ColumnName.Contains("_Id") == false))
                               .Select(x => x.ColumnName)
                               .ToArray();
                ChipFile = ToCSV(ChipFile, dt, columnNames);

                ChipFile += "ControlTraces\n";
                foreach (var trace in theJunction.Chip.IonicIV)
                {
                    ChipFile += "" + trace.ProcessedFile + "," + trace.QCStep.ToString() + "\n";
                }

                dt = selectQuery(connection, "select eNotes.NoteTime, eNotes.ShortNote from eNotes Inner Join Chips on Chips.Note_Id = eNotes.Id where Chips.Id = " + theJunction.Chip.Id);
                columnNames = dt.Columns.Cast<DataColumn>()
                               .Select(x => x.ColumnName)
                               .ToArray();
                ChipFile = ToCSV(ChipFile, dt, columnNames);
                File.WriteAllText(FolderName + "\\ChipHeader.csv", ChipFile);

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                string JunctionFile = "";
                dt = selectQuery(connection, "select  *  from Junctions where Id= " + theJunction.Id);

                columnNames = dt.Columns.Cast<DataColumn>().Where(x => (x.ColumnName.Contains("_Id") == false))
                               .Select(x => x.ColumnName)
                               .ToArray();
                JunctionFile = ToCSV(JunctionFile, dt, columnNames);

                JunctionFile += "ControlTraces\n";
                foreach (var trace in theJunction.TunnelIV)
                {
                    JunctionFile += "" + trace.ProcessedFile + "," + trace.QCStep.ToString() + "," + trace.Condition + "\n";
                }

                if (theJunction.Note != null)
                {
                    JunctionFile += "NoteTime," + theJunction.Note.NoteTime + "\n";
                    JunctionFile += "ShortNote," + theJunction.Note.ShortNote + "\n";
                }


                File.WriteAllText(FolderName + "\\JunctionHeader.csv", JunctionFile);

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                dt = selectQuery(connection, "select  DataTraces.*  from DataTraces Inner Join Junctions on DataTraces.Junction_Id=Junctions.Id where Junctions.Id=" + theJunction.Id);

                columnNames = dt.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToArray();
                columnNames = (from x in columnNames where x.ToLower().Contains("_id") == false select x).ToArray();
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        var header = DataModel.Fileserver.FileDataTrace.ReadHeader(row["ProcessedFile"].ToString() + "_header.txt");
                        foreach (var col in columnNames)
                        {
                            if (header.ContainsKey(col))
                            {
                                header.Remove(col);
                            }
                            header.Add(col, row[col].ToString());
                        }
                        System.Diagnostics.Debug.Print("");
                        string dataFile = "";
                        foreach (KeyValuePair<string, string> kvp in header)
                        {
                            dataFile += kvp.Key + "," + kvp.Value + "\n";
                        }
                        string filename = Path.GetFileNameWithoutExtension(row["ProcessedFile"].ToString());
                        string rFilename = row["Filename"].ToString();
                        string outFile = FolderName + "\\" + filename + "_raw" + Path.GetExtension(rFilename);
                        dataFile += "RawFile," + outFile + "\n";
                        File.WriteAllText(FolderName + "\\" + filename + ".csv", dataFile);
                        File.Copy(App.ConvertFilePaths( rFilename),outFile);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print("");
                    }
                }

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                dt = selectQuery(connection, "select  TunnelTraces.*  from TunnelTraces Inner Join Junctions on TunnelTraces.Junction_Id=Junctions.Id where Junctions.Id=" + theJunction.Id);

                columnNames = dt.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToArray();
                columnNames = (from x in columnNames where x.ToLower().Contains("_id") == false select x).ToArray();
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        var header = DataModel.Fileserver.FileDataTrace.ReadHeader(row["ProcessedFile"].ToString() + "_header.txt");
                        foreach (var col in columnNames)
                        {
                            if (header.ContainsKey(col))
                            {
                                header.Remove(col);
                            }
                            header.Add(col, row[col].ToString());
                        }
                        System.Diagnostics.Debug.Print("");
                        string dataFile = "";
                        foreach (KeyValuePair<string, string> kvp in header)
                        {
                            dataFile += kvp.Key + "," + kvp.Value + "\n";
                        }
                        string filename = Path.GetFileNameWithoutExtension(row["ProcessedFile"].ToString());
                        string rFilename = row["Filename"].ToString();
                        string outFile = FolderName + "\\" + filename + "_raw" + Path.GetExtension(rFilename);
                        dataFile += "RawFile," + outFile + "\n";
                        File.WriteAllText(FolderName + "\\" + filename + ".csv", dataFile);
                        File.Copy(App.ConvertFilePaths(rFilename), outFile);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print("");
                    }
                }

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                dt = selectQuery(connection, "select IonicTraces.*  from IonicTraces Inner Join Junctions on IonicTraces.Junction_Id=Junctions.Id where Junctions.Id=" + theJunction.Id);

                columnNames = dt.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToArray();
                columnNames = (from x in columnNames where x.ToLower().Contains("_id") == false select x).ToArray();
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        var header = DataModel.Fileserver.FileDataTrace.ReadHeader(row["ProcessedFile"].ToString() + "_header.txt");
                        foreach (var col in columnNames)
                        {
                            if (header.ContainsKey(col))
                            {
                                header.Remove(col);
                            }
                            header.Add(col, row[col].ToString());
                        }
                        System.Diagnostics.Debug.Print("");
                        string dataFile = "";
                        foreach (KeyValuePair<string, string> kvp in header)
                        {
                            dataFile += kvp.Key + "," + kvp.Value + "\n";
                        }
                        string filename = Path.GetFileNameWithoutExtension(row["ProcessedFile"].ToString());
                        string rFilename = row["Filename"].ToString();
                        string outFile = FolderName + "\\" + filename + "_raw" + Path.GetExtension(rFilename);
                        dataFile += "RawFile," + outFile + "\n";
                        File.WriteAllText(FolderName + "\\" + filename + ".csv", dataFile);
                        File.Copy(App.ConvertFilePaths(rFilename), outFile);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print("");
                    }
                }
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                dt = selectQuery(connection, "select IonicTraces.*  from IonicTraces Inner Join Chips on IonicTraces.Chip_Id=Chips.Id where Chips.Id=" + theJunction.Chip.Id);

                columnNames = dt.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToArray();
                columnNames = (from x in columnNames where x.ToLower().Contains("_id") == false select x).ToArray();
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        var header = DataModel.Fileserver.FileDataTrace.ReadHeader(row["ProcessedFile"].ToString() + "_header.txt");
                        foreach (var col in columnNames)
                        {
                            if (header.ContainsKey(col))
                            {
                                header.Remove(col);
                            }
                            header.Add(col, row[col].ToString());
                        }
                        System.Diagnostics.Debug.Print("");
                        string dataFile = "";
                        foreach (KeyValuePair<string, string> kvp in header)
                        {
                            dataFile += kvp.Key + "," + kvp.Value + "\n";
                        }
                        string filename = Path.GetFileNameWithoutExtension(row["ProcessedFile"].ToString());
                        string rFilename = row["Filename"].ToString();
                        string outFile = FolderName + "\\" + filename + "_raw" + Path.GetExtension(rFilename);
                        dataFile += "RawFile," + outFile + "\n";
                        File.WriteAllText(FolderName + "\\" + filename + ".csv", dataFile);
                        File.Copy(App.ConvertFilePaths(rFilename), outFile);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print("");
                    }
                }
            }
        }
    }

    public class GridItems
    {
        public ObservableCollection<DataTraceVM> GItems { get; set; }
        public GridItems(List<DataModel.DataTrace> traces)
        {
            GItems = new ObservableCollection<DataTraceVM>();
            foreach (var trace in traces)
                GItems.Add(new DataTraceVM(trace));
        }

        //     <DataGrid.GroupStyle>
        //    <GroupStyle>
        //        <GroupStyle.HeaderTemplate>
        //            <DataTemplate>
        //                <StackPanel>
        //                    <TextBlock Text="{Binding Path=Name}" />
        //                </StackPanel>
        //            </DataTemplate>
        //        </GroupStyle.HeaderTemplate>
        //        <GroupStyle.ContainerStyle>
        //            <Style TargetType="{x:Type GroupItem}">
        //                <Setter Property="Template">
        //                    <Setter.Value>
        //                        <ControlTemplate TargetType="{x:Type GroupItem}">
        //                            <Expander>
        //                                <Expander.Header>
        //                                    <StackPanel Orientation="Horizontal">
        //                                      <TextBlock Text="{Binding Path=Name}" />
        //                                      <TextBlock Text="{Binding Path=ItemCount}"/>
        //                                      <TextBlock Text="Items"/>
        //                                    </StackPanel>
        //                                </Expander.Header>
        //                                <ItemsPresenter />
        //                            </Expander>
        //                        </ControlTemplate>
        //                    </Setter.Value>
        //                </Setter>
        //            </Style>
        //        </GroupStyle.ContainerStyle>
        //    </GroupStyle>
        //</DataGrid.GroupStyle>
    }
}
