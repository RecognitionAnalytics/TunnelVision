using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using TunnelVision.DataModel;

namespace TunnelVision.Controls.TreeViewModel
{
    public class ExperimentVM : TreeNodeVM
    {
        public override Experiment Experiment()
        {
            return theExperiment;
        }
        public DataModel.Experiment theExperiment { get; set; }

        public ExperimentVM(DataModel.Experiment experiment) : base(experiment)
        {
            this.theExperiment = experiment;
            OpenList = ((string)Properties.Settings.Default["DefaultExperiment"] == this.ToString());

            MenuButtons.AddChipButton = Visibility.Collapsed;
            MenuButtons.AddDataTraceButton = Visibility.Collapsed;
            MenuButtons.AddIonicButton = Visibility.Collapsed;
            MenuButtons.AddJunctionButton = Visibility.Collapsed;
            MenuButtons.AddTunnelButton = Visibility.Collapsed;
            MenuButtons.BadButton = Visibility.Collapsed;
            MenuButtons.UploadButton = Visibility.Collapsed;
            MenuButtons.GoodButton = Visibility.Collapsed;
            MenuButtons.BrokenButton = Visibility.Collapsed;
        }

        public void ByDate()
        {
            Children.Clear();

            foreach (var batch in theExperiment.Batches)
            {
                Children.Add(new BatchVM(batch));
            }

            Properties.Settings.Default["DefaultExperiment"] = this.ToString();
        }
        public void ByBatch()
        {
            DoListOpen(true);
        }

        protected override void DoListOpen(bool value)
        {
            if (value == true)
            {
                Children.Clear();

                foreach (var batch in theExperiment.Batches)
                {
                    Children.Add(new BatchVM(batch));
                }

                Properties.Settings.Default["DefaultExperiment"] = this.ToString();
            }

            base.DoListOpen(value);
        }
        protected override void DoClick()
        {
            Column1.Add(new NoteListVM(this.theExperiment, this.theExperiment.Notes));
            Column2.Add(ChipStats(null));
            Column2.Add(DrillStatistics(null));
        }


        #region Plots

        private ColumnChartViewModel ChipStats(ColumnChartViewModel colChart)
        {

            string timeTrace = App.ConvertFilePaths(theExperiment.Folder + "\\ChipStats.png");
            if (File.Exists(timeTrace) == false || true)
            {
                if (colChart == null)
                    colChart = new ColumnChartViewModel { Title = "Chip statistics", DarkLayout = true };
                colChart.SaveFilename = theExperiment.Folder + "\\ChipStats.png";
                return ChipStats_DelayLoadRequested(colChart, null, null, null);
            }
            else
            {
                colChart = new ColumnChartViewModel { Title = "Chip statistics", DarkLayout = true };
                colChart.ExampleImage = timeTrace;
                colChart.ChartOwner = theExperiment;
                colChart.ShowLoadScreen = true;
                colChart.DelayLoadRequested += ChipStats_DelayLoadRequested;
                return colChart;
            }
        }


        private ColumnChartViewModel ChipStats_DelayLoadRequested(ColumnChartViewModel colChart, object Owner, object param1, object param2)
        {
#if Travel
            using (System.Data.SQLite.SQLiteConnection connection = new System.Data.SQLite.SQLiteConnection(App.ConnectionString))
            {
#else
                  using (MySql.Data.MySqlClient.MySqlConnection connection = new MySql.Data.MySqlClient.MySqlConnection(App.ConnectionString))
            {
#endif
                DataTable dt= selectQuery(connection, "select Id,BatchName,NumberOfDeliveredChips from Batches where Experiment_Id=" + theExperiment.Id);
                List<long> batchID = new List<long>();
                List<int> batchN = new List<int>();
                List<string> batchNames = new List<string>();
                List<int> Delivered = new List<int>();
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        string batchName = row["BatchName"].ToString();
                        batchNames.Add(batchName);
#if Travel
                        batchID.Add((long)row["Id"]);
#else
                        batchID.Add((int)row["Id"]);
#endif
                        batchN.Add( int.Parse("0" + Regex.Match(batchName, @"\d+").Value) );
                        Delivered.Add((int)row["NumberOfDeliveredChips"]);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print("");
                    }
                }


                var ranked = batchN.Select((val, index) =>
                                  new { Value = val, idx = index })
                          .OrderBy(pair => pair.Value)
                          .ToList();

                string[] xValues = new string[batchN.Count];
                double[] numChips = new double[batchN.Count];
                double[] numDrilled = new double[batchN.Count];
                double[] numGood = new double[batchN.Count];

                int cc = 0;
                foreach (var i in ranked)
                {
                    xValues[cc] = batchNames[i.idx];
                    numChips[cc] = Delivered[i.idx];
                    DataTable d2t = selectQuery(connection, "select Id, GoodChip from Chips where Batch_Id=" + batchID[i.idx]);
                    int goodChip = 0;
                    int rtChip = 0;
                    foreach (DataRow row in d2t.Rows)
                    {
                        if ((int)row[1] == 3)
                        {
                            goodChip++;
                            DataTable d3t = selectQuery(connection, "select count(*) from DataTraces where GoodTrace=3 and Chip_Id=" + row[0]);
                            if ((long)d3t.Rows[0][0] > 2)
                                rtChip++;
                        }
                    }
                    numDrilled[cc] =goodChip;
                    numGood[cc] = rtChip;
                    cc++;
                }

                List<double[]> lines = new List<double[]>();
                lines.Add(numChips);
                lines.Add(numDrilled);
                lines.Add(numGood);

                colChart.Series.Clear();
                for (int i = 0; i < numChips.Length; i++)
                {
                    var chartData = new System.Collections.ObjectModel.ObservableCollection<CategoryColumn>();
                    chartData.Add(new CategoryColumn { Category = "Number Chips", Number = (float)numChips[i] });
                    chartData.Add(new CategoryColumn { Category = "Number Drilled", Number = (float)numDrilled[i] });
                    chartData.Add(new CategoryColumn { Category = "Number Good", Number = (float)numGood[i] });
                    colChart.AddSeries(xValues[i], chartData);
                }
                colChart.ShowLoadScreen = false;
                return colChart;
            }
        }

        private ColumnChartViewModel DrillStatistics(ColumnChartViewModel colChart)
        {

            string timeTrace = App.ConvertFilePaths(theExperiment.Folder + "\\DrillStatistics.png");
            if (File.Exists(timeTrace) == false || true)
            {
                if (colChart == null)
                    colChart = new ColumnChartViewModel { Title = "Drill statistics", DarkLayout = true };
                colChart.SaveFilename = theExperiment.Folder + "\\DrillStatistics.png";
                return DrillStatistics_DelayLoadRequested(colChart, null, null, null);
            }
            else
            {
                colChart = new ColumnChartViewModel { Title = "Drill statistics", DarkLayout = true };
                colChart.ExampleImage = timeTrace;
                colChart.ChartOwner = theExperiment;
                colChart.ShowLoadScreen = true;
                colChart.DelayLoadRequested += DrillStatistics_DelayLoadRequested;
                return colChart;
            }
        }
        private ColumnChartViewModel DrillStatistics_DelayLoadRequested(ColumnChartViewModel colChart, object Owner, object param1, object param2)
        {
#if Travel
            using (System.Data.SQLite.SQLiteConnection connection = new System.Data.SQLite.SQLiteConnection(App.ConnectionString))
            {
#else
                  using (MySql.Data.MySqlClient.MySqlConnection connection = new MySql.Data.MySqlClient.MySqlConnection(App.ConnectionString))
            {
#endif
                DataTable dt = selectQuery(connection, "select Id,BatchName,NumberOfDeliveredChips from Batches where Experiment_Id=" + theExperiment.Id);
                List<long> batchID = new List<long>();
                List<int> batchN = new List<int>();
                List<string> batchNames = new List<string>();
                List<int> Delivered = new List<int>();
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        string batchName = row["BatchName"].ToString();
                        batchNames.Add(batchName);
#if Travel
                        batchID.Add((long)row["Id"]);
#else
                        batchID.Add((int)row["Id"]);
#endif
                        batchN.Add(int.Parse("0" + Regex.Match(batchName, @"\d+").Value));
                        Delivered.Add((int)row["NumberOfDeliveredChips"]);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print("");
                    }
                }

                var ranked = batchN.Select((val, index) =>
                                  new { Value = val, idx = index })
                          .OrderBy(pair => pair.Value)
                          .ToList();

                DataTable d2t = selectQuery(connection, "SELECT Distinct(DrillMethod) FROM Chips INNER JOIN Experiments ON Chips.Experiment_Id = Experiments.Id where Experiments.Id = " + theExperiment.Id);
                List<string> methods = new List<string>();
                foreach (DataRow row in d2t.Rows)
                    methods.Add(row[0].ToString());

                colChart.Series.Clear();
                foreach (var i in ranked)
                {
                    var chartData = new System.Collections.ObjectModel.ObservableCollection<CategoryColumn>();
                    foreach (var drill in methods)
                    {
                         d2t = selectQuery(connection, "SELECT GoodChip FROM Chips where DrillMethod='" + drill + "' and Batch_Id = " + batchID[i.idx]);
                        int goodChips = 0;
                        int chips = 1;
                        foreach (DataRow row in d2t.Rows)
                        {
                            if ((int)row[0] == 3)
                                goodChips++;
                            chips++;
                        }
                        chartData.Add(new CategoryColumn { Category = drill, Number = (float)(goodChips) });
                    }
                    colChart.AddSeries(batchNames[i.idx], chartData);
                }
                colChart.ShowLoadScreen = false;
                return colChart;
            }
        }
#endregion
    }
}
