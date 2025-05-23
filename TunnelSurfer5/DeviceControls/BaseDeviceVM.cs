using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TunnelVision.Controls.TreeViewModel;
using TunnelVision.Controls.ViewControl;

namespace TunnelVision.DeviceControls
{

    public delegate void DataFinishedEvent(string filename);

    public class BaseDeviceVM : INotifyPropertyChanged
    {
        public virtual void KillDevice()
        {

        }

        protected void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event DataFinishedEvent DataFinished;

        protected void SignalDataFinished(string outfile)
        {
            if (DataFinished != null)
                DataFinished(outfile);
        }

        public BaseDeviceVM()
        {
            TraceScripts = new string[]
                        {
                       "Whole cell",
                       "Capacative"
                        };


            TraceVoltages = new string[] { "none", "float", "0", "50", "100", "150", "200", "250", "300", "350", "400", "450", "500", "550" };
            IonicVoltages = new string[] { "none", "float", "0", "100", "150", "200", "250", "300", "350", "400", "450", "500", "550" };
            SampleRates = new string[] { "20", "100", "250" };
            RunTimes = new string[] { "1", "2", "4", "8" };
        }

        private string[] _TraceScripts;
        public string[] TraceScripts
        {
            get { return _TraceScripts; }
            set
            {
                _TraceScripts = value;
                NotifyPropertyChanged("TraceScripts");
            }
        }

        private string _TraceScript;
        public string TraceScript
        {
            get
            {
                return _TraceScript;
            }
            set
            {
                _TraceScript = value;
                HandleScriptChange();
                NotifyPropertyChanged("TraceScript");
            }
        }

        private bool _TunnelBold = false;
        public virtual bool TunnelBold
        {
            get { return _TunnelBold; }
            set
            {
                _TunnelBold = value;
                NotifyPropertyChanged("TunnelBold");
            }
        }

        private bool _IonicBold = false;
        public virtual bool IonicBold
        {
            get { return _IonicBold; }
            set
            {
                _IonicBold = value;
                NotifyPropertyChanged("IonicBold");
            }
        }

        private Controls.TreeViewModel.NewDataTraceVM.TraceTypeEnum _TraceType = Controls.TreeViewModel.NewDataTraceVM.TraceTypeEnum.Tunneling;
        public Controls.TreeViewModel.NewDataTraceVM.TraceTypeEnum TraceType
        {
            get { return _TraceType; }
            set
            {
                _TraceType = value;
                HandleTraceTypeChange();
                NotifyPropertyChanged("TraceType");

                if (_TraceType== NewDataTraceVM.TraceTypeEnum.Ionic)
                {
                    IonicBold = true;
                    TunnelBold = false;
                }
                else
                {
                    IonicBold = false;
                    TunnelBold = true;
                }
            }
        }
        protected virtual void HandleTraceTypeChange() { }

        protected virtual void HandleScriptChange()
        {
            if (_TraceScript.ToLower().Contains("constant"))
            {
                TraceType = NewDataTraceVM.TraceTypeEnum.Constant;
                return;
            }
            if (_TraceScript.ToLower().Contains("ionic"))
            {
                TraceType = NewDataTraceVM.TraceTypeEnum.Ionic;
            }
            else
                TraceType = NewDataTraceVM.TraceTypeEnum.Tunneling;

        }

        private string[] _TraceVoltages;
        public string[] TraceVoltages
        {
            get { return _TraceVoltages; }
            set
            {
                _TraceVoltages = value;
                NotifyPropertyChanged("TraceVoltages");
            }
        }

        private string[] _IonicVoltages;
        public string[] IonicVoltages
        {
            get { return _IonicVoltages; }
            set
            {
                _IonicVoltages = value;
                NotifyPropertyChanged("IonicVoltages");
            }
        }

        private string[] _RunTimes;
        public string[] RunTimes
        {
            get { return _RunTimes; }
            set
            {
                _RunTimes = value;
                NotifyPropertyChanged("RunTimes");
            }
        }

        private double _RunTime=.5;
        public double RunTime
        {
            get { return _RunTime; }
            set
            {
                _RunTime = value;
                NotifyPropertyChanged("RunTime");
            }
        }

        private string[] _SampleRates;
        public string[] SampleRates
        {
            get { return _SampleRates; }
            set
            {
                _SampleRates = value;
                NotifyPropertyChanged("SampleRates");
            }
        }

        private string _SampleRate="20";
        public string SampleRate
        {
            get { return _SampleRate; }
            set
            {
                _SampleRate = value;
                NotifyPropertyChanged("SampleRate");
            }
        }

        public YATE.YATEditor Editor;

        protected GraphAndHist theGraph;
        protected string Filename;
        public void StartRun(string filename, GraphAndHist graph)
        {
            theGraph = graph;
            Filename = filename;
            DoRun();
        }
        protected virtual void DoRun() { }

        public virtual void Save(DataModel.eNote newNote) { }

        public  NewDataTraceVM DataTraceVM = null;
        public virtual void SetDataTraceVM(NewDataTraceVM dataTraceVM)
        {
            this.DataTraceVM = dataTraceVM;
        }
    }
}
