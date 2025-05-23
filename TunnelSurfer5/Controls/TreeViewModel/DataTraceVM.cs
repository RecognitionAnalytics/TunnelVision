using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace TunnelVision.Controls.TreeViewModel
{



    public delegate void RepeatMeasurementEvent(DataTraceVM dataTrace);

    public class DataTraceVM : TreeNodeVM
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
            return DataTrace.Junction;
        }

        public string ExperimentPath
        {
            get
            {
                if (Junction() != null)
                {
                    return Experiment().ExperimentName + "_" + Batch().BatchName + "_" + Chip().ChipName + "_" + Junction().JunctionName + "_" + TraceName;
                }
                else
                {
                    return Experiment().ExperimentName + "_" + Batch().BatchName + "_" + Chip().ChipName + "_" + TraceName;
                }
            }

        }

        public event RepeatMeasurementEvent RepeatMeasurment;

        public DataModel.DataTrace DataTrace { get; set; }

        public string DateAcquired
        {
            get
            {
                return DataTrace.DateAcquired.ToShortTimeString();
            }
        }

        public string TraceName
        {
            get { return DataTrace.TraceName; }
            set
            {
                DataTrace.TraceName = value;
                NotifyPropertyChanged("TraceName");
            }
        }

        public double Concentration
        {
            get { return Math.Round(DataTrace.Concentration_mM, 8); }
            set
            {
                DataTrace.Concentration_mM = value;
                NotifyPropertyChanged("Concentration");
            }
        }

        public double BufferConcentration
        {
            get { return DataTrace.BufferConcentration_mM; }
            set
            {
                DataTrace.BufferConcentration_mM = value;
                NotifyPropertyChanged("BufferConcentration");
            }
        }

        public string Analyte
        {
            get { return DataTrace.Analyte; }
            set
            {
                DataTrace.Analyte = value;
                NotifyPropertyChanged("Analyte");
            }
        }

        public string Buffer
        {
            get { return DataTrace.Buffer; }
            set
            {
                DataTrace.Buffer = value;
                NotifyPropertyChanged("Buffer");
            }
        }

        public string BufferFull
        {
            get { return Math.Round( DataTrace.BufferConcentration_mM , 6) + " mM " + DataTrace.Buffer; }
            set
            {
                // DataTrace.Buffer = value;
                NotifyPropertyChanged("Buffer");
            }
        }

        private string _DisplayTitle = "";
        public string DisplayTitle
        {
            get { return _DisplayTitle; }
            set
            {
                _DisplayTitle = value;
                NotifyPropertyChanged("DisplayTitle");
            }
        }

        public string TopReference
        {
            get
            {
                return DataTrace.TopRef_mV_ToString;
            }
            set
            {
                DataTrace.TopReference_mV = DataModel.ExistingParse.ConvertVoltageToDouble(value);
                NotifyPropertyChanged("TopReference");
            }
        }

        private string _NumberPores = "3";
        public string NumberPores
        {
            get
            {
                return _NumberPores;
            }
            set
            {
                _NumberPores = value;
                NotifyPropertyChanged("NumberPores");
            }
        }
        public string BottomReference
        {
            get
            {
                return DataTrace.BottomRef_mV_ToString;
            }
            set
            {
                DataTrace.BottomReference_mV = DataModel.ExistingParse.ConvertVoltageToDouble(value);
                NotifyPropertyChanged("BottomReference");
            }
        }


        public string TunnelVoltageBot
        {
            get
            {
                return DataTrace.Tunnel_Bottom_mV_ToString;
            }
            set
            {
                DataTrace.Tunnel_mV_Bottom = DataModel.ExistingParse.ConvertVoltageToDouble(value);
                NotifyPropertyChanged("TunnelVoltageBot");
            }
        }
        public string TunnelVoltage
        {
            get
            {
                return DataTrace.Tunnel_Top_mV_ToString;
            }
            set
            {
                DataTrace.Tunnel_mV_Top = DataModel.ExistingParse.ConvertVoltageToDouble(value);
                NotifyPropertyChanged("TunnelVoltage");
            }
        }
        public string SampleRate
        {
            get { return DataTrace.SampleRate.ToString(); }

            set
            {
                DataTrace.SampleRate = double.Parse(value);
                NotifyPropertyChanged("SampleRate");
            }
        }

        public string RunTime
        {
            get { return Math.Round(DataTrace.RunTime / 60, 2).ToString(); }

            set
            {
                DataTrace.RunTime = double.Parse(value) * 60;
                NotifyPropertyChanged("RunTime");
            }
        }

        public List<string> ProcessedData
        {
            get { return DataTrace.ProcessedData; }
        }

        public DataTraceVM(DataModel.Trace trace) : base(trace)
        {
            _DisplayTitle = trace.ToString();
            this.DataTrace = (DataModel.DataTrace)trace;
            RateItem(trace.GoodTrace);

            MenuButtons.BrokenButton = Visibility.Collapsed;
        }

        public DataTraceVM(DataModel.Trace trace, string ExtraInfo) : base(trace)

        {
            _DisplayTitle = trace.ToString() + "          " + ExtraInfo;
            this.DataTrace = (DataModel.DataTrace)trace;
            RateItem(trace.GoodTrace);
            MenuButtons.BrokenButton = Visibility.Collapsed;
        }

        public void RepeatMeasurementRequested()
        {
            if (RepeatMeasurment != null)
                RepeatMeasurment(this);
        }

        public override void Delete()
        {
            DataTrace.Delete();
          //  RequestDataTreeRefresh();
        }

        public void WriteRocheHeader(string outputFile)
        {
            var header = DataTrace.GetHeader();

            PropertyInfo[] propertyInfos;
            propertyInfos = typeof(DataModel.DataTrace).GetProperties(BindingFlags.Public |
                                                          BindingFlags.Static);
            // sort properties by name
            Array.Sort(propertyInfos,
                    delegate (PropertyInfo propertyInfo1, PropertyInfo propertyInfo2)
                    { return propertyInfo1.Name.CompareTo(propertyInfo2.Name); });

            // write property names
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                string name =(propertyInfo.Name);
                if (header.ContainsKey(name))
                {
                    header.Remove(name);
                }
                header.Add(name, GetPropValue(DataTrace, name).ToString());
            }

        }

        public object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }
    }
}
