using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TunnelVision.Controls.TreeViewModel
{
    public class IVFolderVM :  TreeNodeVM
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
            if (DataModelItem.GetType() == typeof(ChipVM))
                return ((ChipVM)DataModelItem).Chip();

            try
            {
                return Junction().Chip;
            }
            catch
            {
                return null;
            }
        }
        public override DataModel.Junction Junction()
        {
            if (DataModelItem.GetType()==typeof(JunctionVM))
                return ((JunctionVM)DataModelItem).Junction();

            return null;
        }

        protected override void DoListOpen(bool value)
        {
            foreach (var curve in Curves)
            {
                Children.Add(curve);
            }
            base.DoListOpen(value);
        }

        public ObservableCollection<IVTraceVM> Curves { get; set; }

        private string FolderName;
        public override string ToString()
        {
            return FolderName;
        }

        public IVFolderVM(object parent, List<DataModel.IonicTrace> traces):base(parent)
        {
           
            Curves = new ObservableCollection<IVTraceVM>();
            foreach (var curve in traces)
                Curves.Add(new IVTraceVM((DataModel.IVTrace) curve ));
            FolderName = "Ionic IV";
            MenuButtons.AddTunnelButton = Visibility.Collapsed;
            MenuButtons.BadButton = Visibility.Collapsed;
            MenuButtons.UploadButton = Visibility.Collapsed;
            MenuButtons.GoodButton = Visibility.Collapsed;
            MenuButtons.BrokenButton = Visibility.Collapsed;
        }

        public IVFolderVM(object parent, List<DataModel.TunnelingTrace> traces) : base(parent)
        {
          
            Curves = new ObservableCollection<IVTraceVM>();
            foreach (var curve in traces)
                Curves.Add(new IVTraceVM((DataModel.IVTrace)curve));
            FolderName = "Tunneling IV";
            MenuButtons.BadButton = Visibility.Collapsed;
            MenuButtons.UploadButton = Visibility.Collapsed;
            MenuButtons.GoodButton = Visibility.Collapsed;
            MenuButtons.BrokenButton = Visibility.Collapsed;
        }

        public IVFolderVM(object parent, List<DataModel.IVTrace> traces) : base(parent)
        {
            FolderName = "IV Traces";
            Curves = new ObservableCollection<IVTraceVM>();
            foreach (var curve in traces)
                Curves.Add(new IVTraceVM((DataModel.IVTrace)curve));
            MenuButtons.BadButton = Visibility.Collapsed;
            MenuButtons.UploadButton = Visibility.Collapsed;
            MenuButtons.GoodButton = Visibility.Collapsed;
            MenuButtons.BrokenButton = Visibility.Collapsed;
        }

        protected override void DoClick()
        {
            PlotCurves();
        }

        #region Plots

        private void PlotCurves()
        {
            ScatterChartViewModel sChart = new ScatterChartViewModel { ChartType = ScatterChartViewModel.ChartTypeEnum.Line, Title = "Tunneling Curves", XLabel = "Voltage (mV)", YLabel = "Current (pA)" };

            foreach (var curve in Curves)
            {
                if (curve.DataModelItem.GetType() == typeof(DataModel.TunnelingTrace) || curve.DataModelItem.GetType().BaseType == typeof(DataModel.TunnelingTrace))
                {
                    double[] xValues = ((DataModel.IVTrace) curve.DataModelItem).GetProcessed("Voltage").Trace;
                    double[] yValues = ((DataModel.IVTrace)curve.DataModelItem).Current.Trace;
                    sChart.AddSeries(((DataModel.IVTrace)curve.DataModelItem).TraceName, xValues, yValues);
                }
            }
            if (sChart.Series != null && sChart.Series.Count > 0)
                Column2.Add(sChart);

            sChart = new ScatterChartViewModel { ChartType = ScatterChartViewModel.ChartTypeEnum.Line, Title = "Ionic Curves", XLabel = "Voltage (mV)", YLabel = "Current (pA)" };

            foreach (var curve in Curves)
            {
                if (curve.DataModelItem.GetType() == typeof(DataModel.IonicTrace) || curve.DataModelItem.GetType().BaseType == typeof(DataModel.IonicTrace))
                {
                    double[] xValues =((DataModel.IVTrace) curve.DataModelItem).GetProcessed("Voltage").Trace;
                    double[] yValues = ((DataModel.IVTrace)curve.DataModelItem).Current.Trace;
                    sChart.AddSeries(((DataModel.IVTrace)curve.DataModelItem).TraceName, xValues, yValues);
                }
            }
            if (sChart.Series != null && sChart.Series.Count > 0)
                Column1.Add(sChart);
        }

        #endregion
    }
}
