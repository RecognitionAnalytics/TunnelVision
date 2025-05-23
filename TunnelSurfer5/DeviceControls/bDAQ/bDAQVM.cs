using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using TunnelVision.Controls.TreeViewModel;
using TunnelVision.Controls.ViewControl;

namespace TunnelVision.DeviceControls.bDAQ
{
    public class bDAQVM : BaseDeviceVM
    {

        public bDAQVM()
            : base()
        {
            TraceScripts = new string[]
                        {
                       "Constant",
                       "Sweep Ionic",
                       "Sweep Tunnel"
                        };
            TraceScript = "Constant";

            TraceVoltages = new string[] { "-100", "-50", "0", "50", "100", "150", "200", "250", "300", "350", "400", "450", "500", "550" };
            IonicVoltages = new string[] { "-100", "-50", "0", "50", "100", "150", "200", "250", "300", "350", "400", "450", "500", "550" };
            DisplayChannels = new int[] { 0, 1, 2, 3 };
            Junctions = new string[] { "Ionic", "M2_M1", "M3_M1", "M4_M1" };
        }

        public override string ToString()
        {
            return "bDAQ";
        }

        public override void KillDevice()
        {
            if (_BDAQ == null)
                _BDAQ = new DeviceControls.bDAQ.bDAQ();
            _BDAQ.BDAQ_KillServer();
            base.KillDevice();
        }

        private List<string> runJunctions = new List<string>();
        private List<int> runChannels = new List<int>();
        protected override void DoRun()
        {
            runJunctions.Clear();
            runJunctions.AddRange(Junctions);
            runChannels.Clear();
            runChannels.AddRange(DisplayChannels);
            if (DisplayChannels.Length == 0)
            {
                for (int i = 0; i < runJunctions.Count; i++)
                    runChannels.Add(i);
                DisplayChannels = runChannels.ToArray();
            }


            string[] ChannelNames = new string[6];
            for (int i = 0; i < ChannelNames.Length; i++)
                ChannelNames[i] = "Skipped";
            ChannelNames[0] = "IonicBias";
            ChannelNames[1] = "TunnelBias";

            for (int i = 0; i < runChannels.Count; i++)
                ChannelNames[runChannels[i] + 2] = Junctions[i];

            if (_BDAQ == null)
                _BDAQ = new DeviceControls.bDAQ.bDAQ();
            theGraph.ShowPreview(false);
            theGraph.ClearSeriesFromGraph();
            foreach (var i in runChannels)
            {
                theGraph.CheckAndAddSeriesToGraph("Clamp" + i, "pA", null, null);
            }

            theGraph.theChart.ChartAreas[0].AxisX.Minimum = bDAQtime;
            theGraph.theChart.ChartAreas[0].AxisX.Maximum = bDAQtime + .2;


            bDAQtime = 0;
            _BDAQ.ArrayAcquired += _BDAQ_ArrayAcquired1;
            _BDAQ.bDAQDone += _BDAQ_bDAQDone;
            _BDAQ.SetServer(_ServerAddress);
            _BDAQ.SetChannelNames(ChannelNames);
            if (TraceScript == "Constant")
            {
                OutFile = @"C:\temp\temp.bDAQ";

                _BDAQ.RunConstant(RunTime * 60, VBias0 / 1000, VBias1 / 1000, VClamp0 / 1000, VClamp1 / 1000, VClamp2 / 1000, VClamp3 / 1000);
            }
            TraceFinished = false;

            if (TraceScript == "Sweep Ionic" || TraceScript == null)
            {
                OutFile = @"C:\temp\temp.bDAQ";
                _BDAQ.RunSweep(OutFile, bDAQ.SweepType.SawIon, VBias0 / 1000);
            }

            if (TraceScript == "Sweep Tunnel")
            {
                OutFile = @"C:\temp\temp.bDAQ";
                _BDAQ.RunSweep(OutFile, bDAQ.SweepType.SawTunnel, VBias1 / 1000);
            }
        }

        public void Calibrate()
        {
            if (_BDAQ == null)
                _BDAQ = new DeviceControls.bDAQ.bDAQ();
            else
                _BDAQ.ForceCalibration();
        }

        public void ZeroVoltages()
        {
            if (_BDAQ == null)
                _BDAQ = new DeviceControls.bDAQ.bDAQ();
            _BDAQ.RunZero();
        }
        public void StartVoltages()
        {
            if (_BDAQ == null)
                _BDAQ = new DeviceControls.bDAQ.bDAQ();
            _BDAQ.RunVoltage();
        }
        string OutFile;

        private void _BDAQ_ArrayAcquired1(double timePerSample, double[,] calibrationGrid)
        {
            if (!theGraph.Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                theGraph.Dispatcher.BeginInvoke(new TimedArrayAquiredEvent(_BDAQ_ArrayAcquired1), timePerSample, calibrationGrid);
                return;
            }
            while (TraceFinished == false)
            {
                CommonExtensions.DoEvents();
                var AcqArray = _BDAQ.GetAcquired();
                if (AcqArray != null)
                    AddSeriesPointsToGraph(AcqArray.Item1 * timePerSample, timePerSample, calibrationGrid, AcqArray.Item2);
            }
        }

        public static DeviceControls.bDAQ.bDAQ _BDAQ = null;
        private bool TraceFinished = false;

        private void _BDAQ_bDAQDone(string process)
        {

            if (!theGraph.Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                theGraph.Dispatcher.Invoke(new bDAQDoneEvent(_BDAQ_bDAQDone), process);
                return;
            }
            TraceFinished = true;
            SignalDataFinished(OutFile);
        }

        Stopwatch realTime = new Stopwatch();

        public void AddSeriesPointsToGraph(double timeStart, double timePerSample, double[,] calibrationGrid, short[,] xValues)
        {
            if (bDAQtime == 0)
            {
                theGraph.theChart.ChartAreas[0].AxisX.Minimum = bDAQtime;
                theGraph.theChart.ChartAreas[0].AxisX.Maximum = bDAQtime + 2000 * timePerSample;
                realTime.Reset();
                realTime.Start();
            }

            if (theGraph.theChart.Series[0].Points.Count > 2000)
            {
                theGraph.ClearSeriesFromGraph();
                foreach (var i in runChannels)
                {
                    theGraph.CheckAndAddSeriesToGraph("Clamp" + i, "pA", null, null);
                }
                theGraph.theChart.ChartAreas[0].AxisX.Minimum = bDAQtime;
                theGraph.theChart.ChartAreas[0].AxisX.Maximum = bDAQtime + 2000 * timePerSample;
            }
            for (int i = 0; i < DisplayChannels.Length; i++)
            {
                bDAQtime = timeStart;
                var s = theGraph.theChart.Series[i];
                s.Points.SuspendUpdates();
                for (int j = 0; j < xValues.GetLength(0); j++)
                {
                    try
                    {
                        var t = Math.Abs(xValues[j, DisplayChannels[i]]);// System.Net.IPAddress.HostToNetworkOrder(xValues[j, i]);
                        double d = 1e12 * (t - calibrationGrid[0, DisplayChannels[i]]) / calibrationGrid[1, DisplayChannels[i]];
                        s.Points.AddXY(bDAQtime, d);
                    }
                    catch { }
                    bDAQtime += timePerSample;
                }
                s.Points.ResumeUpdates();
            }
            theGraph.theChart.Invalidate();
            theGraph.InvalidateVisual();
            theGraph.UpdateLayout();

            CommonExtensions.DoEvents();
        }

        double bDAQtime = 0;

        protected override void HandleScriptChange()
        {
            //base.HandleScriptChange();
        }
        protected override void HandleTraceTypeChange()
        {
            switch (TraceType)
            {
                case Controls.TreeViewModel.NewDataTraceVM.TraceTypeEnum.Constant:
                    TraceScript = "Constant";
                    break;
                case Controls.TreeViewModel.NewDataTraceVM.TraceTypeEnum.Ionic:
                    TraceScript = "Sweep Ionic";
                    break;
                case Controls.TreeViewModel.NewDataTraceVM.TraceTypeEnum.Tunneling:
                    TraceScript = "Sweep Tunnel";
                    break;
            }
            base.HandleTraceTypeChange();
        }

        private string _ServerAddress = (string)Properties.Settings.Default["BDAQServer"];
        public string ServerAddress
        {
            get { return _ServerAddress; }
            set
            {
                _ServerAddress = value;
                Properties.Settings.Default["BDAQServer"] = value;
                NotifyPropertyChanged("ServerAddress");
            }
        }

        private string _ServerFile = (string)Properties.Settings.Default["BDAQFile"];
        public string ServerFile
        {
            get { return _ServerFile; }
            set
            {
                _ServerFile = value;
                Properties.Settings.Default["BDAQFile"] = value;
                NotifyPropertyChanged("ServerFile");
            }
        }

        public string TraceName
        {
            get
            {

                if (DataTraceVM != null)
                    return DataTraceVM.TraceName;
                else
                    return "";
            }
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

        private double _VClamp2 = 0;
        public double VClamp2
        {
            get { return _VClamp2; }
            set
            {
                _VClamp2 = value;
                NotifyPropertyChanged("VClamp2");
            }
        }

        private double _VClamp3 = 0;
        public double VClamp3
        {
            get { return _VClamp3; }
            set
            {
                _VClamp3 = value;
                NotifyPropertyChanged("VClamp3");
            }
        }

        private int[] _DisplayChannels;
        public int[] DisplayChannels
        {
            get { return _DisplayChannels; }
            set
            {
                _DisplayChannels = value;
                NotifyPropertyChanged("DisplayChannels");
            }
        }

        private string[] _Junctions;
        public string[] Junctions
        {
            get { return _Junctions; }
            set
            {
                _Junctions = value;
                NotifyPropertyChanged("Junctions");
            }
        }

        private string[] _AvailableJunctions;
        public string[] AvailableJunctions
        {
            get { return _AvailableJunctions; }
            set
            {
                _AvailableJunctions = value;
                NotifyPropertyChanged("AvailableJunctions");
            }
        }

        private DataModel.TunnelingTrace SaveTunnelBinary(int Channel, DataModel.Chip chip, DataModel.Junction junction, NewDataTraceVM DataTraceVM)
        {

            DataModel.TunnelingTrace tunnel = new DataModel.TunnelingTrace
            {
                Chip = chip,
                Junction = junction,
                Folder = junction.Folder
            };

            tunnel = (DataModel.TunnelingTrace)DataTraceVM.SaveTrace(tunnel);
            tunnel.Analyte = DataTraceVM.Analyte;

            List<double> voltages = new List<double>();
            voltages.Add(VClamp0);
            voltages.Add(VClamp1);
            voltages.Add(VClamp2);
            voltages.Add(VClamp3);



            tunnel.BottomReference_mV = VBias0;
            tunnel.TopReference_mV = VClamp0;
            tunnel.HasIonic = true;
            tunnel.DataRig = "bDAQ";
            tunnel.Tunnel_mV_Top = voltages[Channel];
            tunnel.Tunnel_mV_Bottom = VBias1;
            tunnel.Condition = DataModel.ExistingParse.ConditionString(tunnel);
            tunnel.TraceName = tunnel.TraceName + "_"+ junction.JunctionName;
            return tunnel;
        }

        private DataModel.IonicTrace SaveIonicBinary(int Channel, DataModel.Chip chip, DataModel.Junction junction, NewDataTraceVM DataTraceVM)
        {
            DataModel.IonicTrace ionic = new DataModel.IonicTrace
            {
                Chip = chip,
                Folder = chip.Folder
            };
            ionic = (DataModel.IonicTrace)DataTraceVM.SaveTrace(ionic);
            ionic.Analyte = DataTraceVM.Analyte;
            if (junction != null)
            {
                ionic.Junction = junction;
                ionic.TraceName = ionic.TraceName + "_"+ junction.JunctionName;
            }

            List<double> voltages = new List<double>();
            voltages.Add(VClamp0);
            voltages.Add(VClamp1);
            voltages.Add(VClamp2);
            voltages.Add(VClamp3);



            ionic.HasIonic = true;

            ionic.DataRig = "bDAQ";
            ionic.TopReference_mV = VBias0;
            ionic.BottomReference_mV = VClamp0;

            ionic.Tunnel_mV_Top = voltages[Channel];
            ionic.Tunnel_mV_Bottom = VBias1;


            ionic.Condition = DataModel.ExistingParse.ConditionString(ionic);

            return ionic;
        }

        private DataModel.DataTrace SaveDatabase(int Channel, DataModel.Chip chip, DataModel.Junction junction, NewDataTraceVM DataTraceVM)
        {

            DataModel.DataTrace tunnel = new DataModel.DataTrace
            {
                Chip = chip,
                Junction = junction,
                Folder = junction.Folder
            };

            tunnel = (DataModel.DataTrace)DataTraceVM.SaveTrace(tunnel);
            tunnel.Analyte = DataTraceVM.Analyte;

            List<double> voltages = new List<double>();
            voltages.Add(VClamp0);
            voltages.Add(VClamp1);
            voltages.Add(VClamp2);
            voltages.Add(VClamp3);

            tunnel.BottomReference_mV = VClamp0;
            tunnel.TopReference_mV = VBias0;
            tunnel.HasIonic = true;
            tunnel.DataRig = "bDAQ";
            tunnel.Tunnel_mV_Top = voltages[Channel];
            tunnel.Tunnel_mV_Bottom = VBias1;
            tunnel.Condition = DataModel.ExistingParse.ConditionString(tunnel);

            return tunnel;
        }

        public override void Save(DataModel.eNote newNote)
        {

            using (DataModel.LabContext lContext = new DataModel.LabContext())
            {

                var CurrentTrace = DataTraceVM.DataTrace;
                List<DataModel.Trace> traces = new List<DataModel.Trace>();

                var chip = (from x in lContext.Chips where x.Id == CurrentTrace.Chip.Id select x).FirstOrDefault();
                for (int i = 0; i < Junctions.Length; i++)
                {
                    string s = Junctions[i];
                    if (s != "Ionic")
                    {
                        int channel = DisplayChannels[i];
                        var junc = (from x in chip.Junctions where x.JunctionName == s select x).FirstOrDefault();
                        if (TraceType == NewDataTraceVM.TraceTypeEnum.Constant)
                        {
                            var trace = SaveDatabase(channel, chip, junc, DataTraceVM);
                            trace.Note = newNote;
                            traces.Add(trace);
                            junc.Traces.Add((DataModel.DataTrace)trace);
                        }
                        else if (TraceType == NewDataTraceVM.TraceTypeEnum.Tunneling)
                        {
                            var trace = SaveTunnelBinary(channel, chip, junc, DataTraceVM);
                            trace.Note = newNote;
                            traces.Add(trace);
                            junc.TunnelIV.Add((DataModel.TunnelingTrace)trace);
                        }
                        if (TraceType == NewDataTraceVM.TraceTypeEnum.Ionic)
                        {
                            var trace = SaveIonicBinary(channel, chip, junc, DataTraceVM);
                            trace.Note = newNote;
                            traces.Add(trace);
                            junc.IonicIV.Add((DataModel.IonicTrace)trace);
                        }

                    }
                    else
                    {
                        int channel = DisplayChannels[i];
                        var junc = (from x in chip.Junctions select x).FirstOrDefault();
                        if (TraceType == NewDataTraceVM.TraceTypeEnum.Constant)
                        {

                            var trace = SaveDatabase(channel, chip, junc, DataTraceVM);
                            trace.Note = newNote;
                            traces.Add(trace);
                            junc.Traces.Add((DataModel.DataTrace)trace);
                        }
                        else if (TraceType == NewDataTraceVM.TraceTypeEnum.Tunneling)
                        {
                            var trace = SaveTunnelBinary(channel, chip, junc, DataTraceVM);
                            trace.Note = newNote;
                            traces.Add(trace);
                            junc.TunnelIV.Add((DataModel.TunnelingTrace)trace);
                        }
                        if (TraceType == NewDataTraceVM.TraceTypeEnum.Ionic)
                        {
                            var trace = SaveIonicBinary(channel, chip, junc, DataTraceVM);
                            trace.Note = newNote;
                            traces.Add(trace);
                            junc.IonicIV.Add((DataModel.IonicTrace)trace);
                        }
                    }
                }
                lContext.SaveChanges();

              
                string tracefilename = traces[0].Folder + "\\" + traces[0].TraceName + traces[0].Id.ToString().PadLeft(4, '0') + ".bDAQ";
                string traceprocessedFile = traces[0].Folder + "\\" + traces[0].TraceName + traces[0].Id.ToString().PadLeft(4, '0');

                for (int i = 0; i < traces.Count; i++)
                {
                    traces[i].Filename = tracefilename;
                    traces[i].ProcessedFile = traceprocessedFile;
                    lContext.SaveChanges();
                }

                File.Copy(OutFile, tracefilename);
                string processedFolder = System.IO.Path.GetDirectoryName(OutFile);
                string processedFile = System.IO.Path.GetFileNameWithoutExtension(OutFile);
                try
                {
                    File.Move(processedFolder + "\\" + processedFile + "_header.txt", traceprocessedFile + "_header.txt");
                }
                catch
                {
                    File.Copy(processedFolder + "\\" + processedFile + "_header.txt", traceprocessedFile + "_header.txt");
                }
                List<string> files;
                if (TraceType != NewDataTraceVM.TraceTypeEnum.Constant)
                {
                    files = new List<string>();
                    files.AddRange(Directory.GetFiles(processedFolder, "*_Voltage*.bin"));
                    foreach (var file in files)
                    {
                        string outfile = traceprocessedFile + System.IO.Path.GetFileNameWithoutExtension(file).Replace(processedFile, "") + ".bin";
                        try
                        {
                            File.Move(file, outfile);
                        }
                        catch
                        {
                            File.Copy(file, outfile);
                        }
                    }

                    files = new List<string>(); files.AddRange(Directory.GetFiles(processedFolder, "*_Channel*S.bin"));
                    files.Sort();
                    for (int i = 0; i < runJunctions.Count; i++)
                    {
                        string file = files[runChannels[i]];
                        string outfile = traceprocessedFile + "_" + runJunctions[i] + "S.bin";
                        try
                        {
                            File.Move(file, outfile);
                        }
                        catch
                        {
                            File.Copy(file, outfile);
                        }
                    }

                }


                File.Copy(processedFolder + "\\" + processedFile + "_Raw.bin", traceprocessedFile + "_Raw.bin");
                 files = new List<string>(); files.AddRange(Directory.GetFiles(processedFolder, "*_Channel*.bin"));
                files.Sort();
                for (int i = 0; i < runJunctions.Count; i++)
                {
                    string file = files[runChannels[i]];
                    string outfile = traceprocessedFile + "_" + runJunctions[i] + ".bin";
                    try
                    {
                        File.Move(file, outfile);
                    }
                    catch
                    {
                        File.Copy(file, outfile);
                    }
                }

               
                if (DataTraceVM != null)
                    DataTraceVM.RequestDataTreeRefresh();
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
        }

        public void ExportFile(string directory, string filename)
        {

        }

        public override void SetDataTraceVM(NewDataTraceVM dataTraceVM)
        {
            base.SetDataTraceVM(dataTraceVM);
            List<string> junctions = new List<string>();
            junctions.Add("Ionic");
            using (DataModel.LabContext lContext = new DataModel.LabContext())
            {
                var CurrentTrace = dataTraceVM.DataTrace;
                var chip = (from x in lContext.Chips where x.Id == CurrentTrace.Chip.Id select x).FirstOrDefault();

                foreach (var junc in chip.Junctions)
                {
                    junctions.Add(junc.ToString());
                }
            }
            Junctions = junctions.ToArray();
            AvailableJunctions = junctions.ToArray();

        }
    }
}
