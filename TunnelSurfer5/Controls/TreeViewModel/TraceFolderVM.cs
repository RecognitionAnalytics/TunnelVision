using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TunnelVision.Controls.TreeViewModel
{
    public class TraceFolderVM : TreeNodeVM
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
        public override DataModel.Junction Junction()
        {
            return TraceFolder.Junction;
        }

        public DataModel.TraceFolder TraceFolder { get; set; }

        public TraceFolderVM(DataModel.TraceFolder traceFolder):base(traceFolder)
        {
            TraceFolder = traceFolder;
            OpenList = ((string)Properties.Settings.Default["TraceFolder"] == traceFolder.FolderName);

            MenuButtons.BadButton = Visibility.Collapsed;
            MenuButtons.UploadButton = Visibility.Collapsed;
            MenuButtons.GoodButton = Visibility.Collapsed;
            MenuButtons.BrokenButton = Visibility.Collapsed;
        }

        protected override void DoListOpen(bool value)
        {
            if (value == true)
            {
                Children.Clear();

                foreach (var trace in TraceFolder.Traces)
                {
                    Children.Add(new DataTraceVM(trace));
                }

                Properties.Settings.Default["TraceFolder"] = TraceFolder.FolderName;
            }

            base.DoListOpen(value);
        }

        #region plotting
        protected override void DoClick()
        {
            ScatterChartViewModel sChart;
            string timeTrace = App.ConvertFilePaths(TraceFolder.Junction.Folder + "\\" + TraceFolder.FolderName + "TimeTrace.png");
            if (File.Exists(timeTrace) == false)
            {

                sChart = JunctionVM.TimeTrace(TraceFolder.Junction, TraceFolder.FolderName);
                sChart.SaveFilename = TraceFolder.Junction.Folder + "\\" + TraceFolder.FolderName + "TimeTrace.png";
                //  sChart.SaveQualityChartImage(sChart.ExampleImage);
            }
            else
            {
                sChart = new ScatterChartViewModel { Title = "Time Trace", XLabel = "Time (s)", YLabel = "Current (pA)", ChartType = ScatterChartViewModel.ChartTypeEnum.Line };
                sChart.ExampleImage = timeTrace;
                sChart.ChartOwner = TraceFolder.Junction;
                sChart.ShowLoadScreen = true;
                sChart.DelayLoadRequested += SChart_DelayLoadRequested;
            }
            Column1.Add(sChart);




            List<ScatterChartViewModel> plots;
            if (TraceFolder.Parent != null)
                plots = JunctionVM.ConditionPlots(TraceFolder.Junction, TraceFolder.FolderName, TraceFolder.Parent.FolderName);
            else
                plots = JunctionVM.ConditionPlots(TraceFolder.Junction, TraceFolder.FolderName, "ana");

            Column1.Add(plots[0]);
            for (int i = 1; i < plots.Count; i++)
                Column2.Add(plots[i]);
        }





        private void SChart_DelayLoadRequested(ScatterChartViewModel sChart, object Owner, object param1, object param2)
        {
            JunctionVM.TimeTrace((DataModel.Junction)Owner, "", sChart);
            sChart.ShowLoadScreen = false;
        }

        #endregion

    }
}
