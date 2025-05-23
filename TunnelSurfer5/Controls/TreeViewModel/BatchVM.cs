using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TunnelVision.DataModel;

namespace TunnelVision.Controls.TreeViewModel
{

    public class BatchVM : TreeNodeVM
    {
        public override Experiment Experiment()
        {
            return this.theBatch.Experiment;
        }
        public override Batch Batch()
        {
            return theBatch;
        }


        protected override void DoListOpen(bool value)
        {
            if (value == true)
            {
                Children.Clear();
                foreach (var chip in theBatch.Chips)
                {
                    Children.Add(new ChipVM(chip));
                }
                Properties.Settings.Default["DefaultBatch"] = this.ToString();
            }

            base.DoListOpen(value);
        }



        public DataModel.Batch theBatch { get; set; }


        public BatchVM(DataModel.Batch batch) : base(batch)
        {
            this.theBatch = batch;
            OpenList = ((string)Properties.Settings.Default["DefaultBatch"] == batch.ToString());
            MenuButtons.AddDataTraceButton = Visibility.Collapsed;
            MenuButtons.AddIonicButton = Visibility.Collapsed;
            MenuButtons.AddJunctionButton = Visibility.Collapsed;
            MenuButtons.AddTunnelButton = Visibility.Collapsed;
            MenuButtons.BadButton = Visibility.Collapsed;
            MenuButtons.UploadButton = Visibility.Collapsed;
            MenuButtons.GoodButton = Visibility.Collapsed;
            MenuButtons.BrokenButton = Visibility.Collapsed;

        }

        protected override void DoClick()
        {
            Column1.Add(new NoteListVM(this.theBatch, this.theBatch.Notes));
            Column2.Add(BatchStatistics(null));
            Column2.Add(GapStatistics(null));
        }

        private ColumnChartViewModel BatchStatistics(ColumnChartViewModel colChart)
        {
            string timeTrace = App.ConvertFilePaths(theBatch.Folder + "\\BatchStats.png");
            if (File.Exists(timeTrace) == false)
            {
                if (colChart == null)
                    colChart = new ColumnChartViewModel { Title = "Batch statistics", DarkLayout = true };
                colChart.SaveFilename = theBatch.Folder + "\\BatchStats.png";
                return BatchStatistics_DelayLoadRequested(colChart, null, null, null);
            }
            else
            {
                colChart = new ColumnChartViewModel { Title = "Batch statistics", DarkLayout = true };
                colChart.ExampleImage = timeTrace;
                colChart.ChartOwner = theBatch;
                colChart.ShowLoadScreen = true;
                colChart.DelayLoadRequested += BatchStatistics_DelayLoadRequested;
                return colChart;
            }
        }

        private ColumnChartViewModel BatchStatistics_DelayLoadRequested(ColumnChartViewModel colChart, object Owner, object param1, object param2)
        {
            var chartData = new ObservableCollection<CategoryColumn>();
            chartData.Add(new CategoryColumn { Category = "Delivered", Number = theBatch.NumberOfDeliveredChips });
            chartData.Add(new CategoryColumn { Category = "Drilled", Number = theBatch.NumberOfDrilledChips });
            chartData.Add(new CategoryColumn { Category = "Run", Number = (from x in theBatch.Chips where x.QCStep > QualityControlStep.Drilled select x).Count() });
            chartData.Add(new CategoryColumn { Category = "RT good", Number = (from x in theBatch.Chips select x).Count() });
            colChart.AddSeries(theBatch.DrillMethod, chartData);
            colChart.ShowLoadScreen = false;
            return colChart;
        }

        private ColumnChartViewModel GapStatistics(ColumnChartViewModel colChart)
        {
            string timeTrace = App.ConvertFilePaths(theBatch.Folder + "\\GapStats.png");
            if (File.Exists(timeTrace) == false)
            {
                if (colChart == null)
                    colChart = new ColumnChartViewModel { Title = "Gap statistics", DarkLayout = true };
                colChart.SaveFilename = theBatch.Folder + "\\GapStats.png";
                return GapStatistics_DelayLoadRequested(colChart, null, null, null);
            }
            else
            {
                colChart = new ColumnChartViewModel { Title = "Batch statistics", DarkLayout = true };
                colChart.ExampleImage = timeTrace;
                colChart.ChartOwner = theBatch;
                colChart.ShowLoadScreen = true;
                colChart.DelayLoadRequested += GapStatistics_DelayLoadRequested;
                return colChart;
            }
        }

        private ColumnChartViewModel GapStatistics_DelayLoadRequested(ColumnChartViewModel colChart, object Owner, object param1, object param2)
        {
            foreach (var chip in theBatch.Chips)
            {
                var chartData = new ObservableCollection<CategoryColumn>();
                chartData.Add(new CategoryColumn { Category = "Delivered", Number = (float)chip.JunctionDelivered });
                chartData.Add(new CategoryColumn { Category = "Drilled", Number = (float)chip.JunctionDrilled });

                colChart.AddSeries(chip.ChipName, chartData);
            }
            colChart.ShowLoadScreen = false;
            return colChart;

        }

        public override void Delete()
        {
            theBatch.Delete();
            RequestDataTreeRefresh();
        }
    }
}
