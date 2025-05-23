using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TunnelVision.Controls.TreeViewModel;

namespace TunnelVision.DeviceControls.Axon
{
    public class AxonVM : BaseDeviceVM
    {
        public void startAxon(string file, int sampleRate, double runTime)
        {
            if (_Axon == null)
            {
                _Axon = new DeviceControls.Axon.Axon();
                _Axon.FileCreated += _Axon_FileCreated;
            }
            _Axon.RunAxon(file, false, sampleRate, (int)Math.Floor(runTime));

        }

        private string NewFilename = "";
        private void _Axon_FileCreated(string filename)
        {
            NewFilename = filename;
            Save(new DataModel.eNote(this.Editor.PlainText, this.Editor.Document));
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
            tunnel.DataRig = "Axon";
            tunnel.Tunnel_mV_Top = VClamp1;
            tunnel.Tunnel_mV_Bottom = VBias1;
            tunnel.Condition = DataModel.ExistingParse.ConditionString(tunnel);

            string traceprocessedFile = tunnel.Folder + "\\" + tunnel.TraceName + tunnel.Id.ToString().PadLeft(4, '0');
            string tracefilename = traceprocessedFile + ".json";


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

                    lContext.SaveChanges();

                    trace.ProcessedFile = trace.Folder + "\\" + trace.TraceName + trace.Id.ToString().PadLeft(4, '0');
                    trace.ProcessedFile = trace.ProcessedFile + "\\_" + trace.Id;
                    trace.Folder = System.IO.Path.GetDirectoryName(trace.ProcessedFile);
                    trace.Filename = System.IO.Path.GetDirectoryName(trace.Folder) + "\\" + trace.TraceName + "_" + trace.Id.ToString() + " .abf";

                    trace.Note = newNote;
                    junction.Traces.Add((DataModel.DataTrace)trace);

                    lContext.SaveChanges();

                    try
                    {
                        if (Directory.Exists(trace.Folder) == false)
                            Directory.CreateDirectory(trace.Folder);
                    }
                    catch { }

                    if (Path.GetExtension(trace.Filename).ToLower() == ".abf" )
                        DataModel.Fileserver.ABFLoader.ConvertABF_FLAC(trace.Filename, trace.Folder, trace.ProcessedFile + "_raw.json");

                   

                    //lContext.SaveChanges();
                    //string traceprocessedFile = trace.Folder + "\\" + trace.TraceName + trace.Id.ToString().PadLeft(4, '0');
                    //string tracefilename = traceprocessedFile + ".bbf";


                    //trace.Filename = tracefilename;
                    //trace.ProcessedFile = traceprocessedFile;

                    //File.Move(NewFilename, trace.ProcessedFile + "_Raw.abf");
                    //trace.Filename = trace.ProcessedFile + "_Raw.abf";
                }

                lContext.SaveChanges();
            }

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
            if (DataTraceVM != null)
                DataTraceVM.RequestDataTreeRefresh();
        }
        DeviceControls.Axon.Axon _Axon = null;

        protected override void DoRun()
        {
            startAxon(@"c:\temp\temp.abf", int.Parse(SampleRate), RunTime);
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

        private double _Gain = 1;
        public double Gain
        {
            get { return _Gain; }
            set
            {
                _Gain = value;
                NotifyPropertyChanged("Gain");
            }
        }

        private double _FilterKHz = 10;
        public double FilterKHz
        {
            get { return _FilterKHz; }
            set
            {
                _FilterKHz = value;
                NotifyPropertyChanged("FilterKHz");
            }
        }

        public override string ToString()
        {
            return "Axon";
        }
    }
}
