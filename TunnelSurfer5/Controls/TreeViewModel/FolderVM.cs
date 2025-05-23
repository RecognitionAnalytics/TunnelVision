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
    public class FolderVM :  TreeNodeVM
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
            return Condition.Junction;
        }

        public DataModel.TraceFolder Condition { get; set; }
       
        public FolderVM(DataModel.TraceFolder condition):base(condition)
        {
            Condition = condition;
            OpenList = ((string)Properties.Settings.Default["DefaultFolder"] == condition.FolderName);

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

                foreach (var folder in Condition.Folders)
                {
                    Children.Add(new TraceFolderVM(folder));
                }
                ListOpen = Visibility.Visible;
                Properties.Settings.Default["DefaultFolder"] = Condition.FolderName;
            }

            base.DoListOpen(value);
        }

        #region Plots
        protected override void DoClick()
        {

            ScatterChartViewModel sChart;
            string timeTrace = App.ConvertFilePaths(Condition.Junction.Folder + "\\" + Condition.FolderName + "TimeTrace.png");
            if (File.Exists(timeTrace) == false)
            {
                sChart = JunctionVM.TimeTrace(Condition.Junction, Condition.FolderName);
                sChart.SaveFilename = Condition.Junction.Folder + "\\" + Condition.FolderName + "TimeTrace.png";
                //  sChart.SaveQualityChartImage(sChart.ExampleImage);
            }
            else
            {
                sChart = new ScatterChartViewModel { Title = "Time Trace", XLabel = "Time (s)", YLabel = "Current (pA)", ChartType = ScatterChartViewModel.ChartTypeEnum.Line };
                sChart.ExampleImage = timeTrace;
                sChart.ChartOwner = Condition.Junction;
                sChart.ShowLoadScreen = true;
                sChart.DelayLoadRequested += SChart_DelayLoadRequested;
            }
            Column1.Add(sChart);

            var plots = JunctionVM.AnalytePlots(Condition.Junction, Condition.FolderName);
       
            try
            {
                Column1.Add(plots[0]);
            }
            catch { }
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
