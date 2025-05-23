using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace TunnelVision.Controls.TreeViewModel
{

    public class IVTraceVM : TreeNodeVM

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
            return IVTrace.Junction;
        }

        public DataModel.IVTrace IVTrace { get; set; }

        public IVTraceVM(DataModel.IVTrace trace) : base(trace)
        {
            this.IVTrace = (DataModel.IVTrace)trace;
            RateItem(trace.GoodTrace);
            MeasurementTools = new string[] { "NIDAQmx", "Keithley", "OpalKelly" };
            SweepScripts = new string[] { "Ionic", "Tunneling", "Big Ionic", "Big Tunneling" };
            CurrentLimits = new string[] { "100pA", "1nA", "100nA", "1uA", "1mA" };


            Analyte = trace.Analyte;

            FitInfo = "";
            TraceVoltages = new string[] { "none", "float", "sweep", "-300", "-250", "-200", "-150", "-50", "0", "50", "100", "150", "200", "250", "300", "350", "400", "450", "500", "550" };

            SweepVoltages = new string[] { "100", "200", "300", "400", "500" };
            SweepSpeeds = new string[] { "10", "50", "100" };

            var enumQC = Enum.GetValues(typeof(DataModel.QualityControlStep));
            QCSteps = new string[enumQC.Length];
            int cc = 0;
            foreach (var e in enumQC)
            {
                QCSteps[cc] = e.ToString();
                cc++;
            }

            MenuButtons.BrokenButton = Visibility.Collapsed;
        }

        public string[] SweepScripts
        {
            get;
            set;
        }

        public string[] TraceVoltages { get; private set; }

        public string[] SweepVoltages { get; private set; }

        public double Concentration
        {
            get { return IVTrace.Concentration_mM; }
            set
            {
                IVTrace.Concentration_mM = value;
                NotifyPropertyChanged("Concentration");
            }
        }

        public string Analyte
        {
            get { return IVTrace.Analyte; }
            set
            {
                IVTrace.Analyte = value;
                NotifyPropertyChanged("Analyte");
            }
        }

        public string Buffer
        {
            get { return IVTrace.Buffer; }
            set
            {
                IVTrace.Buffer = value;
                NotifyPropertyChanged("Buffer");
            }
        }

        public string FitInfo { get; set; }

        public string[] SweepSpeeds
        { get; set; }

        public string[] CurrentLimits { get; set; }

        public string[] QCSteps
        {
            get;
            set;
        }

        public string[] MeasurementTools
        {
            get;
            set;
        }

        public string TraceName
        {
            get { return IVTrace.TraceName; }
            set
            {
                IVTrace.TraceName = value;
                NotifyPropertyChanged("TraceName");
            }
        }

        public string Notes
        {
            get
            {
                if (IVTrace.Note != null)
                    return IVTrace.Note.ShortNote;
                else
                    return "";
            }
            set
            {

            }
        }

        public string TopReference
        {
            get
            {
                return IVTrace.TopRef_mV_ToString;
            }
            set
            {
                IVTrace.TopReference_mV = DataModel.ExistingParse.ConvertVoltageToDouble(value);
                NotifyPropertyChanged("TopReference");
            }
        }

        public string BottomReference
        {
            get
            {
                return IVTrace.BottomRef_mV_ToString;
            }
            set
            {
                IVTrace.BottomReference_mV = DataModel.ExistingParse.ConvertVoltageToDouble(value);
                NotifyPropertyChanged("BottomReference");
            }
        }

        public string TunnelVoltage
        {
            get
            {
                return IVTrace.Tunnel_Top_mV_ToString;
            }
            set
            {
                IVTrace.Tunnel_mV_Top = DataModel.ExistingParse.ConvertVoltageToDouble(value);
                NotifyPropertyChanged("TunnelVoltage");
            }
        }

        public string SampleRate
        {
            get { return IVTrace.SampleRate.ToString(); }

            set
            {
                IVTrace.SampleRate = double.Parse(value);
                NotifyPropertyChanged("SampleRate");
            }
        }

        public string RunTime
        {
            get { return IVTrace.RunTime.ToString(); }

            set
            {
                IVTrace.RunTime = double.Parse(value);
                NotifyPropertyChanged("RunTime");
            }
        }
    }
}
