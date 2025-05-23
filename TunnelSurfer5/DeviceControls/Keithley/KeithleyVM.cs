using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using TunnelVision.Controls.TreeViewModel;

namespace TunnelVision.DeviceControls.Keithley
{
    public class KeithleyVM : BaseDeviceVM
    {

        public KeithleyVM() : base()
        {
            TraceScripts = new string[]
                        {
                       "Constant",
                       "Sweep Ionic",
                       "Sweep Tunnel"
                        };
            TraceScript = "Sweep Tunnel";

            TraceVoltages = new string[] { "floating", "-100", "-50", "0", "50", "100", "150", "200", "250", "300", "350", "400", "450", "500", "550" };
            IonicVoltages = new string[] { "floating", "-100", "-50", "0", "50", "100", "150", "200", "250", "300", "350", "400", "450", "500", "550" };

            Sensitivities = new string[] { "100pA", "1nA", "100nA", "1uA", "1mA" };
            Intervals = new string[] { "10", "20", "50", "100" };
        }

        public string[] Sensitivities
        {
            get; set;
        }

        private string _Sensititivity = "100pA";
        public string Sensitivity
        {
            get { return _Sensititivity; }
            set
            {
                _Sensititivity = value;
                NotifyPropertyChanged("Sensitivity");
            }
        }

        private string _ServerAddress = (string)Properties.Settings.Default["KeithleyAddress"];
        public string ServerAddress
        {
            get { return _ServerAddress; }
            set
            {
                _ServerAddress = value;
                Properties.Settings.Default["KeithleyAddress"] = value;
                NotifyPropertyChanged("ServerAddress");
            }
        }

        private string _ServerFile = (string)Properties.Settings.Default["KeithleyFile"];
        public string ServerFile
        {
            get { return _ServerFile; }
            set
            {
                _ServerFile = value;
                Properties.Settings.Default["KeithleyFile"] = value;
                NotifyPropertyChanged("ServerFile");
            }
        }

        public string[] Intervals
        {
            get; set;
        }

        private string _Interval = "50";
        public string Interval
        {
            get { return _Interval; }
            set
            {
                _Interval = value;
                NotifyPropertyChanged("Interval");
            }
        }


        public string TraceName
        {
            get { return DataTraceVM.TraceName; }
            set
            {
                DataTraceVM.TraceName = value;
                NotifyPropertyChanged("TraceName");
            }
        }

        private double _VBias0 = 450;
        public double VBias0
        {
            get { return _VBias0; }
            set
            {
                _VBias0 = value;
                NotifyPropertyChanged("VBias0");
            }
        }

        private double _VBias1 = 350;
        public double VBias1
        {
            get { return _VBias1; }
            set
            {
                _VBias1 = value;
                NotifyPropertyChanged("VBias1");
            }
        }

        private double _VClamp0 = 0;
        public double VClamp0
        {
            get { return _VClamp0; }
            set
            {
                _VClamp0 = value;
                NotifyPropertyChanged("VClamp0");
            }
        }

        private double _VClamp1 = 0;
        public double VClamp1
        {
            get { return _VClamp1; }
            set
            {
                _VClamp1 = value;
                NotifyPropertyChanged("VClamp1");
            }
        }

        #region Visa Handling
#if Automation
       static Visa visa=null;

        protected override void DoRun()
        {
            try
            {
                Saved = false;
                if (visa == null)
                {
                    visa = new Visa();
                    var resources = visa.DeviceNames();
                    visa.ConnectToDevice((string)Properties.Settings.Default["KeithleyAddress"]);
                }

                theGraph.ShowPreview(false);
                theGraph.ClearSeriesFromGraph();

                theGraph.CheckAndAddSeriesToGraph("Raw", "pA", null, null);

                visa.VisaPointDelivered += visa_VisaPointDelivered;
                visa.VisaDataCompleted += DataCompleted;

                var amplitude = .3;


                if (TraceType == NewDataTraceVM.TraceTypeEnum.Ionic)
                    amplitude = Math.Max(VBias0, VClamp0);
                else
                    amplitude = Math.Max(VBias1, VClamp1);
                double limiti = 100e-10;
                double speed = double.Parse(Interval);
                switch (Sensitivity)
                {
                    case "100pA":
                        limiti = 100e-12;
                        break;
                    case "1nA":
                        limiti = 1e-9;
                        break;
                    case "100nA":
                        limiti = 100e-9;
                        break;
                    case "1uA":
                        limiti = 100e-6;
                        break;
                    case "1mA":
                        limiti = 100e-3;
                        break;
                }



                visa.RunProgramBackground(amplitude / 1000, speed, limiti);
            }
            catch
            {
                MessageBox.Show("Please turn Keithley off and then on and try again.");
            }
        }

#endif
        void DataCompleted(List<double> time, List<double> voltage, List<double> current)
        {
            if (Saved == false)
            {
                Saved = true;
                theGraph.ClearSeriesFromGraph();

                theGraph.CheckAndAddSeriesToGraph("Raw", "pA", voltage.ToArray(), current.ToArray(), true);

                _Time = time;
                _Voltage = voltage;
                _Current = current;
                Save(new DataModel.eNote(this.Editor.PlainText, this.Editor.Document));

                DataTraceVM.RequestDataTreeRefresh();
            }
        }

        void visa_VisaPointDelivered(double time, double voltage, double current)
        {
            theGraph.AddSeriesPointsToGraph("Raw", voltage, current);
        }

        #endregion
        List<double> _Time;
        List<double> _Voltage;
        List<double> _Current;
        bool Saved = false;
        private DataModel.TunnelingTrace SaveTunnelBinary(DataModel.Chip chip, DataModel.Junction junction, NewDataTraceVM DataTraceVM)
        {

            DataModel.TunnelingTrace tunnel = new DataModel.TunnelingTrace
            {
                Chip = chip,
                Junction = junction,
                Folder = junction.Folder
            };

            tunnel = (DataModel.TunnelingTrace)DataTraceVM.SaveTrace(tunnel);
            tunnel.Analyte = DataTraceVM.Analyte;

            tunnel.BottomReference_mV = VBias0;
            tunnel.TopReference_mV = VClamp0;
            tunnel.HasIonic = false;
            tunnel.DataRig = "Keithley";
            tunnel.Tunnel_mV_Top = VClamp1;
            tunnel.Tunnel_mV_Bottom = VBias1;
            tunnel.Condition = DataModel.ExistingParse.ConditionString(tunnel);

            string traceprocessedFile = tunnel.Folder + "\\" + tunnel.TraceName + tunnel.Id.ToString().PadLeft(4, '0');
            string tracefilename = traceprocessedFile + ".bbf";


            tunnel.Filename = tracefilename;
            tunnel.ProcessedFile = traceprocessedFile;

            return tunnel;
        }

        private DataModel.IonicTrace SaveIonicBinary(DataModel.Chip chip, DataModel.Junction junction, NewDataTraceVM DataTraceVM)
        {
            DataModel.IonicTrace ionic = new DataModel.IonicTrace
            {
                Chip = chip,
                Folder = chip.Folder
            };
            ionic = (DataModel.IonicTrace)DataTraceVM.SaveTrace(ionic);
            ionic.Analyte = DataTraceVM.Analyte;
            if (junction != null)
                ionic.Junction = junction;

            ionic.HasIonic = true;

            ionic.DataRig = "Keithley";
            ionic.TopReference_mV = VBias0;
            ionic.BottomReference_mV = VClamp0;

            ionic.Tunnel_mV_Top = VClamp1;
            ionic.Tunnel_mV_Bottom = VBias1;


            ionic.Condition = DataModel.ExistingParse.ConditionString(ionic);

            string traceprocessedFile = ionic.Folder + "\\" + ionic.TraceName + ionic.Id.ToString().PadLeft(4, '0');
            string tracefilename = traceprocessedFile + ".bbf";


            ionic.Filename = tracefilename;
            ionic.ProcessedFile = traceprocessedFile;

            return ionic;
        }

        private DataModel.DataTrace SaveDatabase(DataModel.Chip chip, DataModel.Junction junction, NewDataTraceVM DataTraceVM)
        {


            DataModel.DataTrace tunnel = new DataModel.DataTrace
            {
                Chip = chip,
                Junction = junction,
                Folder = junction.Folder
            };

            tunnel = (DataModel.DataTrace)DataTraceVM.SaveTrace(tunnel);
            tunnel.Analyte = DataTraceVM.Analyte;
            tunnel.BottomReference_mV = VClamp0;
            tunnel.TopReference_mV = VBias0;
            tunnel.HasIonic = false;
            tunnel.DataRig = "Keithley";
            tunnel.Tunnel_mV_Top = VClamp1;
            tunnel.Tunnel_mV_Bottom = VBias1;
            tunnel.Condition = DataModel.ExistingParse.ConditionString(tunnel);

            string traceprocessedFile = tunnel.Folder + "\\" + tunnel.TraceName + tunnel.Id.ToString().PadLeft(4, '0');
            string tracefilename = traceprocessedFile + ".bbf";


            tunnel.Filename = tracefilename;
            tunnel.ProcessedFile = traceprocessedFile;

            return tunnel;
        }

        public override void Save(DataModel.eNote newNote)
        {
            var CurrentTrace = DataTraceVM.DataTrace;
            CurrentTrace.TraceName = TraceName;
            using (DataModel.LabContext lContext = new DataModel.LabContext())
            {
                var chip = (from x in lContext.Chips where x.Id == CurrentTrace.Chip.Id select x).FirstOrDefault();
                DataModel.Junction junction = null;
                if (CurrentTrace.Junction != null)
                    junction = (from x in chip.Junctions where x.Id == CurrentTrace.Junction.Id select x).FirstOrDefault();


                if (TraceType == NewDataTraceVM.TraceTypeEnum.Constant)
                {
                    var trace = SaveDatabase(chip, junction, DataTraceVM);
                    trace.Note = newNote;
                    junction.Traces.Add((DataModel.DataTrace)trace);
                    DataModel.Fileserver.FileTrace.SaveBBFFLAC(trace.ProcessedFile + "_raw.bbf", DataTraceVM.ExperimentPath, new List<double[]> { _Current.ToArray(), _Voltage.ToArray(), _Time.ToArray() },
                        new List<string> { "Current", "Voltages", "Time" }, 10 / (_Time[10] - _Time[0]));
                }
                if (TraceType == NewDataTraceVM.TraceTypeEnum.Tunneling)
                {
                    var trace = SaveTunnelBinary(chip, junction, DataTraceVM);
                    trace.Note = newNote;

                    junction.TunnelIV.Add((DataModel.TunnelingTrace)trace);
                    DataModel.Fileserver.FileTrace.SaveBBFFLAC(trace.ProcessedFile + "_raw.bbf", DataTraceVM.ExperimentPath, new List<double[]> { _Current.ToArray(), _Voltage.ToArray(), _Time.ToArray() },
                         new List<string> { "Current", "Voltages", "Time" }, 10 / (_Time[10] - _Time[0]));
                }
                if (TraceType == NewDataTraceVM.TraceTypeEnum.Ionic)
                {
                    var trace = SaveIonicBinary(chip, junction, DataTraceVM);
                    trace.Note = newNote;
                    chip.IonicIV.Add((DataModel.IonicTrace)trace);

                    DataModel.Fileserver.FileTrace.SaveBBFFLAC(trace.ProcessedFile + "_raw.bbf", DataTraceVM.ExperimentPath, new List<double[]> { _Current.ToArray(), _Voltage.ToArray(), _Time.ToArray() },
                         new List<string> { "Current", "Voltages", "Time" }, 10 / (_Time[10] - _Time[0]));
                }
                lContext.SaveChanges();
            }



            //if (DataTraceVM != null)
            //    DataTraceVM.RequestionDataTreeRefresh();
            try
            {
                var parts = TraceName.Split('_');
                if (Regex.IsMatch(parts.Last(), @"^\d+$") == true)
                {

                    int filenumber = int.Parse(string.Join(string.Empty, Regex.Matches(parts.Last(), @"-?\d+").OfType<Match>().Select(m => m.Value))) + 1;
                    parts[parts.Length - 1] = filenumber.ToString().PadLeft(4, '0');
                    TraceName = string.Join("_", parts);

                }
            }
            catch { }

        }

        public void ExportFile(string filename)
        {
            string text = "Voltage, Current, Time";
            for (int i = 0; i < _Current.Count; i++)
            {
                text += _Voltage[i] + "," + _Current[i] + "," + _Time[i] + "\n";
            }
            File.WriteAllText(filename, text);
        }

        public override string ToString()
        {
            return "Keithley";
        }
    }
}
