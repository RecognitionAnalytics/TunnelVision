using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.Controls.TreeViewModel
{
    public class NewDataTraceVM : DataTraceVM
    {
       public  static NewDataTraceVM LastTrace = null;

        public NewDataTraceVM(TraceTypeEnum traceType, DataModel.Trace trace) : base(trace)
        {
            TraceType = traceType;
        }

        public NewDataTraceVM(TraceTypeEnum traceType, DataModel.Trace trace, string ExtraInfo) : base(trace, ExtraInfo)
        {
            TraceType = traceType;
        }

        public enum TraceTypeEnum
        { Constant, Ionic, Tunneling }

        private TraceTypeEnum _TraceType = TraceTypeEnum.Tunneling;
        public TraceTypeEnum TraceType
        {
            get { return _TraceType; }
            set
            {
                _TraceType = value;
                NotifyPropertyChanged("TraceType");
            }
        }

        private DeviceControls.BaseDeviceVM _dataRig;
        public DeviceControls.BaseDeviceVM theDataRig
        {
            get { return _dataRig; }
            set
            {
                _dataRig = value;
                _dataRig.TraceType = (TraceType);
                _dataRig.SetDataTraceVM(this);
                NotifyPropertyChanged("theDataRig");
            }
        }


        public ViewControl.GraphAndHist Graph
        {
            get;
            set;
        }

        public void DoRun()
        {
            string filename = DataTrace.Junction.Folder + "\\" + DataTrace.TraceName;

            _dataRig.StartRun(filename, Graph);
        }

        public void DoSave()
        {

        }

        public DataModel.Trace SaveTrace( DataModel.Trace trace)
        {
            LastTrace = this;

            trace.Buffer = this.Buffer;
            trace.BufferConcentration_mM = this.BufferConcentration;
            trace.Concentration_mM = this.Concentration;
            trace.OtherInfo = "";
            trace.IsControl = (this.TraceName.Contains("control") == true);
            trace.TraceName = this.TraceName;
            trace.DateAcquired = DateTime.Now;
            trace.NumberPores =int.Parse( this.NumberPores );
            return trace;
        }
    }
}
