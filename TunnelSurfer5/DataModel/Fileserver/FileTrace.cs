using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TunnelVision.DataModel.Fileserver
{
    public class FileTrace
    {
        public string TraceName { get; set; }
        public double[] Trace { get; set; }

        //private int FT_TYPE = 0;
        //private double _Scale = 1;
        //private double _Offset = 1;

        public string Filename { get; set; }
        public double SampleRate { get; set; }

        //public long Length
        //{
        //    get
        //    {
        //        if (FT_TYPE == 0)
        //            return _TraceD.Length;
        //        if (FT_TYPE == 1)
        //            return _TraceS.Length;
        //        return _TraceU.Length;
        //    }
        //}

        //public void WriteChartWithTime(System.Windows.Forms.DataVisualization.Charting.Series series, int step)
        //{
        //    if (FT_TYPE == 0)
        //    {
        //        for (long i = 0; i < _TraceD.Length; i += step)
        //        {
        //            series.Points.AddXY(i/ SampleRate, _TraceD[i]);

        //            if ((i % 1000000) == 0)
        //            {
        //                series.Points.ResumeUpdates();
        //                CommonExtensions.DoEvents();
        //                series.Points.SuspendUpdates();
        //            }
        //        }
        //    }
        //    if (FT_TYPE == 1)
        //    {
        //        for (long i = 0; i < _TraceD.Length; i += step)
        //        {
        //            series.Points.AddXY(i / SampleRate, _TraceS[i]*_Scale-_Offset);

        //            if ((i % 1000000) == 0)
        //            {
        //                series.Points.ResumeUpdates();
        //                CommonExtensions.DoEvents();
        //                series.Points.SuspendUpdates();
        //            }
        //        }
        //    }

        //    if (FT_TYPE == 2)
        //    {
        //        for (long i = 0; i < _TraceD.Length; i += step)
        //        {
        //            series.Points.AddXY(i / SampleRate, (_TraceU[i] -_Offset)/_Scale);

        //            if ((i % 1000000) == 0)
        //            {
        //                series.Points.ResumeUpdates();
        //                CommonExtensions.DoEvents();
        //                series.Points.SuspendUpdates();
        //            }
        //        }
        //    }
        //}


        //public double  this[int key]
        //{
        //    get
        //    {
        //        if (FT_TYPE == 0)
        //            return _TraceD[key];
        //        if (FT_TYPE == 1)
        //            return _TraceS[key]*_Scale-_Offset;
        //        return (_TraceU[key] -_Offset)/_Scale;

        //    }
        //}

        public double GetTime(double i)
        {
            return i / SampleRate;
        }

        public double GetTime(int i)
        {
            return i / SampleRate;
        }

        public static void SaveIVTextFile(string file, List<double> times, List<double> voltages, List<double> currents)
        {
            string data = "";
            for (int i = 0; i < times.Count; i++)
            {
                data += times[i] + "," + voltages[i] + "," + currents[i] + "\n";
            }

            File.WriteAllText(file, data);
        }

        public static void SaveIVBinaryFile(DataModel.Trace trace, List<double> times, List<double> voltages, List<double> currents)
        {
            string file = trace.ProcessedFile;

            File.WriteAllText(file + "_header.txt", file);

            using (BinaryWriter b = new BinaryWriter(File.Open(file + "_Voltage.bin", FileMode.Create)))
            {
                b.Write((Int32)1);
                b.Write((Int32)voltages.Count);
                // Use foreach and write all 12 integers.
                foreach (double i in voltages)
                {
                    b.Write(i);
                }
            }

            using (BinaryWriter b = new BinaryWriter(File.Open(file + "_Raw.bin", FileMode.Create)))
            {
                b.Write((Int32)1);
                b.Write((Int32)currents.Count);
                // Use foreach and write all 12 integers.
                foreach (double i in currents)
                {
                    b.Write(i);
                }
            }
        }

        private static List<FileTrace> OpenBinaryBin(string filename)
        {
            List<FileTrace> Datas = new List<FileTrace>();

            using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
                   (FileOptions)0x20000000 | FileOptions.WriteThrough & FileOptions.SequentialScan))
            {
                using (BinaryReader binReader = new BinaryReader(fileStream))
                {
                    try
                    {
                        // Return to the beginning of the stream.
                        binReader.BaseStream.Position = 0;
                        int nTraces = binReader.ReadInt32();
                        int samples = binReader.ReadInt32();

                        FileTrace _data = new FileTrace { Filename = filename, SampleRate = 20000, Trace = null };

                        byte[] buffer = null;
                        for (int j = 0; j < nTraces; j++)
                        {

                            try
                            {
                                if (j == (nTraces - 1) && j != 0)
                                {
                                    _data.Trace = new double[samples];
                                    binReader.Read(buffer, 0, buffer.Length - 8);
                                    Buffer.BlockCopy(buffer, 0, _data.Trace, 0, buffer.Length);
                                }
                                else
                                {
                                    _data.Trace = new double[samples];
                                    if (buffer == null)
                                        buffer = new byte[Buffer.ByteLength(_data.Trace)];
                                    binReader.Read(buffer, 0, buffer.Length);
                                    Buffer.BlockCopy(buffer, 0, _data.Trace, 0, buffer.Length);
                                    //_data[i] = binReader.ReadDouble();
                                }
                            }
                            catch (System.ArgumentException ex)
                            {
                                int fSamples = 0;
                                try
                                {
                                    for (int i = 0; i < samples; i++)
                                    {
                                        _data.Trace[i] = binReader.ReadDouble();
                                        fSamples = i;
                                    }
                                }
                                catch (Exception ex2)
                                {
                                    double[] data2 = new double[fSamples - 1];
                                    Buffer.BlockCopy(_data.Trace, 0, data2, 0, fSamples - 1);
                                    _data.Trace = data2;
                                    System.Diagnostics.Debug.Print(ex2.Message);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.Print(ex.Message);
                            }
                            Datas.Add(_data);
                        }
                    }
                    catch //(EndOfStreamException e2)
                    {

                    }
                }
            }
            return Datas;
        }




        public static List<FileTrace> OpenBinary(string filename)
        {
            if (Path.GetExtension(filename).ToLower() == ".bin")
            {
                return OpenBinaryBin(filename);
            }

            return null;
        }

        public static FileTrace OpenBinary(Trace trace, string label)
        {
            try
            {
                string dataLocation = trace.ProcessedFile;
                dataLocation = App.ConvertFilePaths(dataLocation);

                string[] Files = System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(dataLocation), "*.json");

                string filename = (from x in Files where x.ToLower().Contains(label.ToLower()) select x).FirstOrDefault();
                if (filename == null)
                    return null;

                if (File.Exists(filename))
                    return ReadJSON(filename)[0];

                //if (File.Exists(filename))
                //    return OpenBinary(filename)[0];

                //string filename2 = dataLocation + "_" + label + ".bbf";
                //if (File.Exists(filename2))
                //{
                //    return ReadBBF(filename2)[0];
                //}

                //filename2 = dataLocation + "_" + label + ".abf";
                //if (File.Exists(filename2))
                //{
                //    ABFLoader a = new ABFLoader();
                //    var t = a.ReadABF(filename2);
                //    return (new FileTrace { Filename = filename2, TraceName = label, SampleRate = 20000, Trace = t });

                //}

                //if (label.ToLower() == "voltage")
                //{
                //    label = "raw";
                //    dataLocation = trace.ProcessedFile;
                //    dataLocation = App.ConvertFilePaths(dataLocation);
                //    filename = dataLocation + "_" + label + ".bin";
                //    if (File.Exists(filename))
                //    {
                //        var traces = OpenBinary(filename);
                //        if (traces.Count > 1)
                //            return traces[1];
                //    }

                //    filename2 = dataLocation + "_" + label + ".bbf";
                //    if (File.Exists(filename2))
                //    {
                //        var traces = ReadBBF(filename2);
                //        if (traces.Count > 1)
                //            return traces[1];
                //    }
                //}
            }
            catch (Exception ex)
            {
            }

            return null;
        }

        public static string[] GetSimplfied(Trace trace)
        {
            string dataLocation = trace.ProcessedFile;
            string file = Path.GetFileNameWithoutExtension(dataLocation);
            dataLocation = Path.GetDirectoryName(App.ConvertFilePaths(dataLocation));
            if (Directory.Exists(dataLocation))
                return Directory.GetFiles(dataLocation, file + "*S.bin");

            return null;
        }

        public static FileTrace ConvertABF(string filename, string outFile)
        {
            ABFLoader a = new ABFLoader();
            var t = a.ReadABF(filename);
            return (new FileTrace { Filename = filename, TraceName = "", SampleRate = 20000, Trace = t });
            //Thread.Sleep(200);
        }

        public static List<FileTrace> ReadBBF_Binary(string filename)
        {
            List<FileTrace> traces = new List<FileTrace>();
            using (var output = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read)))
            {
                var nChannels = output.ReadInt32();
                var nPoints = output.ReadInt32();
                var sampleRate = output.ReadDouble();

                uint fileday = output.ReadUInt32();
                uint filetime = output.ReadUInt32();

                var space = output.ReadBytes(512);

                var dataformat = output.ReadInt32();
                string[] channelname = new string[nChannels];
                string[] channelunit = new string[nChannels];
                double[] channelunitscale = new double[nChannels];
                double[] channelscale = new double[nChannels];
                double[] channeloffset = new double[nChannels];
                double[] channelvoltage = new double[nChannels];

                for (int i = 0; i < nChannels; i++)
                {
                    output.ReadInt32();
                    output.ReadInt32();
                    output.ReadDouble();
                    output.ReadDouble();
                    output.ReadDouble();

                    var label = output.ReadBytes(50);
                    var unit = output.ReadBytes(5);

                    channelname[i] = System.Text.Encoding.ASCII.GetString(label).Trim();
                    channelunit[i] = System.Text.Encoding.ASCII.GetString(unit).Trim();

                    channelunitscale[i] = output.ReadDouble();
                    channelscale[i] = output.ReadDouble();
                    channeloffset[i] = output.ReadDouble();
                    channelvoltage[i] = output.ReadDouble();

                    var data = new double[nPoints];
                    for (int j = 0; j < nPoints; j++)
                    {
                        data[j] = output.ReadInt16() * channelscale[i] + channeloffset[i];
                    }

                    var fileTrace = new FileTrace { Filename = filename, SampleRate = sampleRate, Trace = data, TraceName = channelname[i] };
                    traces.Add(fileTrace);
                }
            }
            return traces;
        }

        public static List<FileTrace> ReadBBF(string filename)
        {
            List<FileTrace> channels = new List<FileTrace>();
            using (var bInput = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read)))
            {

                string jHeader = bInput.ReadString();
                BBF_Header header = (BBF_Header)Newtonsoft.Json.JsonConvert.DeserializeObject(jHeader, typeof(BBF_Header));


                for (int j = 0; j < header.numberChannels; j++)
                {
                    bInput.BaseStream.Seek(header.Channels[j].Address, SeekOrigin.Begin);
                    double[] data = new double[header.Channels[j].NumberPoints];

                    if (header.Dataformat == BBF_FileFormat.FLAC_ShortChunk)
                    {
                        double scale = header.Channels[j].Scale;
                        double offset = header.Channels[j].Offset;

                        byte[] buffer = null;

                        buffer = bInput.ReadBytes((int)(header.Channels[j].ByteLength));
                        byte[] data2 = null;
                        using (MemoryStream m = new MemoryStream(buffer))
                        {
                            FlacBox.WaveOverFlacStream flacStream = new FlacBox.WaveOverFlacStream(m, FlacBox.WaveOverFlacStreamMode.Decode);
                            data2 = new byte[header.Channels[j].NumberPoints * 2 + 44];
                            flacStream.Read(data2, 0, data2.Length);
                        }
                        short[] sBuffer = new short[header.Channels[j].NumberPoints];
                        Buffer.BlockCopy(data2, 44, sBuffer, 0, data2.Length - 44);

                        for (long i = 0; i < header.Channels[j].NumberPoints; i++)
                        {
                            data[i] = sBuffer[i] * scale - offset;
                        }
                    }


                    if (header.Dataformat == BBF_FileFormat.ShortChunk)
                    {
                        double scale = header.Channels[j].Scale;
                        double offset = header.Channels[j].Offset;
                        for (long i = 0; i < header.Channels[j].NumberPoints; i++)
                        {
                            data[i] = bInput.ReadInt16() * scale - offset;
                        }
                    }
                    if (header.Dataformat == BBF_FileFormat.doubleChunk)
                    {
                        for (long i = 0; i < header.Channels[j].NumberPoints; i++)
                        {
                            data[i] = bInput.ReadDouble();
                        }
                    }
                    if (header.Dataformat == BBF_FileFormat.uShortChunk)
                    {
                        double scale = header.Channels[j].Scale;
                        double offset = header.Channels[j].Offset;
                        for (long i = 0; i < header.Channels[j].NumberPoints; i++)
                        {
                            data[i] = (bInput.ReadUInt16() - offset) * scale;
                        }
                    }

                    FileTrace ft = new FileTrace { Filename = filename, SampleRate = header.Channels[j].SampleRate, Trace = data, TraceName = header.Channels[j].Name };
                    channels.Add(ft);
                }
            }
            return channels;
        }

        private static object FlacLock = new object();

        public static void SaveFLAC(string filename, List<string> channelName, string experimentFile, Dictionary<string, string> pyHeader, double voltage, List<short[]> data, uint FileDate, uint FileTime_ms, double samplingRate, double[] scales, double[] offsets)
        {
            filename =System.IO.Path.GetDirectoryName(filename) + "\\" +  System.IO.Path.GetFileNameWithoutExtension(filename);

            WriteJSON(filename + ".json", experimentFile, voltage, FileDate, FileTime_ms, samplingRate,
                           data,
                           channelName , pyHeader, scales, offsets);
            SaveFLAC(filename + ".flac", data);
        }

        public static void SaveBBFFLAC(string filename, string experimentPath, List<double[]> current, List<string> ChannelNames, double samplingRate)
        {
            lock (FlacLock)
            {
                uint FileDate = (uint)(DateTime.Now.Year * 100 * 100 + DateTime.Now.Month * 100 + DateTime.Now.Day);
                uint FileTime_ms = (uint)(DateTime.Now.Hour * 60 * 60 * 1000 + DateTime.Now.Minute * 60 * 1000 + DateTime.Now.Second * 1000);
                using (var output = new BinaryWriter(File.Open(@"c:\temp\temp_Raw" + Thread.CurrentThread.ManagedThreadId + ".bbf", FileMode.Create, FileAccess.Write)))
                {
                    BBF_FileFormat fileFormat = BBF_FileFormat.FLAC_ShortChunk;
                    int numChannels = current.Count;

                    DataModel.Fileserver.BBF_Header header = new BBF_Header
                    {
                        numberChannels = numChannels,
                        Dataformat = fileFormat,
                        FileDate = FileDate,
                        FileTime_ms = FileTime_ms,
                        numberPoints = current[0].Length,
                        SampleRate = samplingRate,
                        experimentPath = experimentPath,
                        otherInformation = " "
                    };

                    header.Channels = new List<BBF_ChannelHeader>();
                    for (int j = 0; j < numChannels; j++)
                    {
                        string filelabel = ChannelNames[j];
                        string unit = "pA";
                        if (j != 0)
                        {
                            filelabel = "Voltage";
                            unit = "mV";
                        }
                        double[] data = current[j];
                        double max = data.Max() + double.Epsilon;
                        double min = data.Min() - double.Epsilon;
                        double offset = (max + min) / 2;
                        double scale = ((max - (max + min) / 2)) / (.8 * Int16.MaxValue);

                        DataModel.Fileserver.BBF_ChannelHeader channelHeader = new BBF_ChannelHeader
                        {
                            Name = filelabel,
                            Unit = unit,
                            Offset = offset,
                            Scale = scale,
                            SingleVoltage = false,
                            Voltage = 0,
                            NumberPoints = data.Length,
                            SampleRate = samplingRate,
                            Address = long.MaxValue,
                            ByteLength = long.MaxValue
                        };

                        header.Channels.Add(channelHeader);
                    }
                    string jHeader = Newtonsoft.Json.JsonConvert.SerializeObject(header) + " ".PadRight(50);

                    output.Write(jHeader);

                    for (int j = 0; j < numChannels; j++)
                    {
                        output.Flush();
                        header.Channels[j].Address = output.BaseStream.Position;
                        EricOulashin.WAVFile wav = new EricOulashin.WAVFile();
                        wav.Create(@"c:\temp\data" + Thread.CurrentThread.ManagedThreadId + ".wav", false, 48000, 16);
                        //byte[] buffer = new byte[Buffer.ByteLength(current)];
                        //Buffer.BlockCopy(current, 0, buffer, 0, buffer.Length);
                        double[] dataCurrent = current[j];
                        double max = dataCurrent.Max() + double.Epsilon;
                        double min = dataCurrent.Min() - double.Epsilon;
                        double offset = (max + min) / 2;
                        double scale = ((max - (max + min) / 2)) / (Int16.MaxValue * .8);

                        for (long i = 0; i < dataCurrent.Length; i++)
                        {
                            wav.AddSample_16bit((Int16)((dataCurrent[i] - offset) / scale));
                        }
                        wav.Close();
                        FlacBox.WaveOverFlacStream flacStream = new FlacBox.WaveOverFlacStream(new FlacBox.FlacWriter(File.Open(@"c:\temp\test" + Thread.CurrentThread.ManagedThreadId + ".flac", FileMode.Create)), 3);
                        var data = File.ReadAllBytes("c:\\temp\\data" + Thread.CurrentThread.ManagedThreadId + ".wav");
                        flacStream.Write(data, 0, data.Length);
                        flacStream.Flush();
                        flacStream.Close();
                        data = null;
                        flacStream = null;

                        data = File.ReadAllBytes(@"c:\temp\test" + Thread.CurrentThread.ManagedThreadId + ".flac");
                        header.Channels[j].ByteLength = data.Length;
                        output.Write(data);
                        data = null;
                        output.Flush();
                    }

                    output.Seek(0, SeekOrigin.Begin);
                    string jHeader2 = Newtonsoft.Json.JsonConvert.SerializeObject(header).PadRight(jHeader.Length);
                    output.Write(jHeader2);

                }
                if (File.Exists(filename))
                    File.Delete(filename);

                File.Move(@"c:\temp\temp_raw" + Thread.CurrentThread.ManagedThreadId + ".bbf", filename);
            }
        }

        public static void SaveBBFFLAC(string filename, string experimentPath, short[,] current, uint FileDate, uint FileTime_ms, double samplingRate, double[] scales, double[] offsets)
        {
            lock (FlacLock)
            {
                using (var output = new BinaryWriter(File.Open(@"c:\temp\temp_Raw" + Thread.CurrentThread.ManagedThreadId + ".bbf", FileMode.Create, FileAccess.Write)))
                {
                    BBF_FileFormat fileFormat = BBF_FileFormat.FLAC_ShortChunk;

                    double sum = 0;
                    for (int i = 0; i < current.GetLength(0); i++)
                    {
                        sum += current[i, 1];
                    }
                    double shortMean = sum / current.GetLength(0);
                    sum = 0;
                    for (int i = 0; i < current.GetLength(0); i++)
                    {
                        sum += Math.Pow(shortMean - current[i, 1], 2);
                    }
                    double shortSTD = Math.Sqrt(sum / current.GetLength(0));

                    int numChannels = current.GetLength(1);
                    if (shortSTD < 4)
                    {
                        numChannels = 1;
                    }

                    DataModel.Fileserver.BBF_Header header = new BBF_Header
                    {
                        numberChannels = numChannels,
                        Dataformat = fileFormat,
                        FileDate = FileDate,
                        FileTime_ms = FileTime_ms,
                        numberPoints = current.GetLength(0),
                        SampleRate = samplingRate,
                        experimentPath = experimentPath,
                        otherInformation = " "
                    };

                    header.Channels = new List<BBF_ChannelHeader>();
                    for (int j = 0; j < numChannels; j++)
                    {
                        string filelabel = "Current";
                        string unit = "pA";
                        if (j != 0)
                        {
                            filelabel = "Voltage";
                            unit = "mV";
                        }

                        var scaleV = 1d;
                        var offsetV = 1d;
                        if (current.GetLength(1) > 0)
                        {
                            scaleV = scales[1];
                            offsetV = offsets[1];
                        }

                        DataModel.Fileserver.BBF_ChannelHeader channelHeader = new BBF_ChannelHeader
                        {
                            Name = filelabel,
                            Unit = unit,
                            Offset = offsets[j],
                            Scale = scales[j],
                            SingleVoltage = (numChannels == 1),
                            Voltage = shortMean * scaleV - offsetV,
                            NumberPoints = current.GetLength(0),
                            SampleRate = samplingRate,
                            Address = long.MaxValue,
                            ByteLength = long.MaxValue
                        };

                        header.Channels.Add(channelHeader);
                    }
                    string jHeader = Newtonsoft.Json.JsonConvert.SerializeObject(header) + " ".PadRight(50);

                    output.Write(jHeader);

                    for (int j = 0; j < numChannels; j++)
                    {
                        output.Flush();
                        header.Channels[j].Address = output.BaseStream.Position;
                        EricOulashin.WAVFile wav = new EricOulashin.WAVFile();
                        wav.Create(@"c:\temp\data" + Thread.CurrentThread.ManagedThreadId + ".wav", false, 48000, 16);
                        //byte[] buffer = new byte[Buffer.ByteLength(current)];
                        //Buffer.BlockCopy(current, 0, buffer, 0, buffer.Length);
                        for (long i = 0; i < current.GetLength(0); i++)
                        {
                            wav.AddSample_16bit(current[i, j]);
                        }
                        wav.Close();
                        FlacBox.WaveOverFlacStream flacStream = new FlacBox.WaveOverFlacStream(new FlacBox.FlacWriter(File.Open(@"c:\temp\test" + Thread.CurrentThread.ManagedThreadId + ".flac", FileMode.Create)), 3);
                        var data = File.ReadAllBytes("c:\\temp\\data" + Thread.CurrentThread.ManagedThreadId + ".wav");
                        flacStream.Write(data, 0, data.Length);
                        flacStream.Flush();
                        flacStream.Close();
                        data = null;
                        flacStream = null;
                        //flacStream = new FlacBox.WaveOverFlacStream(new FlacBox.FlacReader(File.Open(@"c:\temp\test.flac", FileMode.Open),false));
                        //var data2 = new byte[ flacStream.Length ];
                        //flacStream.Read(data2, 0, data2.Length);

                        // for (long i = 0; i < current.GetLength(0); i++)
                        {
                            data = File.ReadAllBytes(@"c:\temp\test" + Thread.CurrentThread.ManagedThreadId + ".flac");
                            header.Channels[j].ByteLength = data.Length;
                            output.Write(data);
                        }
                        data = null;
                        output.Flush();
                    }

                    output.Seek(0, SeekOrigin.Begin);
                    string jHeader2 = Newtonsoft.Json.JsonConvert.SerializeObject(header).PadRight(jHeader.Length);
                    output.Write(jHeader2);

                }
                if (File.Exists(filename))
                    File.Delete(filename);

                File.Move(@"c:\temp\temp_raw" + Thread.CurrentThread.ManagedThreadId + ".bbf", filename);
            }
        }

        public static void SaveBBFFLAC(string filename, string channelName, string experimentPath, short[] current, uint FileDate, uint FileTime_ms, double samplingRate, double[] scales, double[] offsets)
        {
            lock (FlacLock)
            {
                using (var output = new BinaryWriter(File.Open(@"c:\temp\temp_Raw" + Thread.CurrentThread.ManagedThreadId + ".bbf", FileMode.Create, FileAccess.Write)))
                {
                    BBF_FileFormat fileFormat = BBF_FileFormat.FLAC_ShortChunk;


                    int numChannels = 1;

                    DataModel.Fileserver.BBF_Header header = new BBF_Header
                    {
                        numberChannels = numChannels,
                        Dataformat = fileFormat,
                        FileDate = FileDate,
                        FileTime_ms = FileTime_ms,
                        numberPoints = current.GetLength(0),
                        SampleRate = samplingRate,
                        experimentPath = experimentPath,
                        otherInformation = " "
                    };

                    header.Channels = new List<BBF_ChannelHeader>();
                    for (int j = 0; j < numChannels; j++)
                    {
                        string filelabel = channelName;
                        string unit = "pA";

                        DataModel.Fileserver.BBF_ChannelHeader channelHeader = new BBF_ChannelHeader
                        {
                            Name = filelabel,
                            Unit = unit,
                            Offset = offsets[j],
                            Scale = scales[j],
                            SingleVoltage = (numChannels == 1),
                            Voltage = 0,
                            NumberPoints = current.GetLength(0),
                            SampleRate = samplingRate,
                            Address = long.MaxValue,
                            ByteLength = long.MaxValue
                        };

                        header.Channels.Add(channelHeader);
                    }
                    string jHeader = Newtonsoft.Json.JsonConvert.SerializeObject(header) + " ".PadRight(50);

                    output.Write(jHeader);

                    for (int j = 0; j < numChannels; j++)
                    {
                        output.Flush();
                        header.Channels[j].Address = output.BaseStream.Position;
                        EricOulashin.WAVFile wav = new EricOulashin.WAVFile();
                        wav.Create(@"c:\temp\data" + Thread.CurrentThread.ManagedThreadId + ".wav", false, 48000, 16);
                        //byte[] buffer = new byte[Buffer.ByteLength(current)];
                        //Buffer.BlockCopy(current, 0, buffer, 0, buffer.Length);
                        for (long i = 0; i < current.GetLength(0); i++)
                        {
                            wav.AddSample_16bit(current[i]);
                        }
                        wav.Close();
                        FlacBox.WaveOverFlacStream flacStream = new FlacBox.WaveOverFlacStream(new FlacBox.FlacWriter(File.Open(@"c:\temp\test" + Thread.CurrentThread.ManagedThreadId + ".flac", FileMode.Create)), 3);
                        var data = File.ReadAllBytes("c:\\temp\\data" + Thread.CurrentThread.ManagedThreadId + ".wav");
                        flacStream.Write(data, 0, data.Length);
                        flacStream.Flush();
                        flacStream.Close();
                        data = null;
                        flacStream = null;
                        //flacStream = new FlacBox.WaveOverFlacStream(new FlacBox.FlacReader(File.Open(@"c:\temp\test.flac", FileMode.Open),false));
                        //var data2 = new byte[ flacStream.Length ];
                        //flacStream.Read(data2, 0, data2.Length);

                        // for (long i = 0; i < current.GetLength(0); i++)
                        {
                            data = File.ReadAllBytes(@"c:\temp\test" + Thread.CurrentThread.ManagedThreadId + ".flac");
                            header.Channels[j].ByteLength = data.Length;
                            output.Write(data);
                        }
                        data = null;
                        output.Flush();
                    }

                    output.Seek(0, SeekOrigin.Begin);
                    string jHeader2 = Newtonsoft.Json.JsonConvert.SerializeObject(header).PadRight(jHeader.Length);
                    output.Write(jHeader2);

                }
                if (File.Exists(filename))
                    File.Delete(filename);

                File.Move(@"c:\temp\temp_raw" + Thread.CurrentThread.ManagedThreadId + ".bbf", filename);
            }
        }

        public static void SaveFLAC(string filename, List<short[]> Currents)
        {
            //  lock (FlacLock)
            {
                for (int j = 0; j < Currents.Count; j++)
                {
                    var current = Currents[j];
                    EricOulashin.WAVFile wav = new EricOulashin.WAVFile();
                    wav.Create(@"c:\temp\data" + Thread.CurrentThread.ManagedThreadId + ".wav", false, 48000, 16);

                    for (long i = 0; i < current.GetLength(0); i++)
                    {
                        wav.AddSample_16bit(current[i]);
                    }
                    wav.Close();

                    string fileout = @"c:\temp\test" + Thread.CurrentThread.ManagedThreadId + ".flac";

                    FlacBox.WaveOverFlacStream flacStream = new FlacBox.WaveOverFlacStream(new FlacBox.FlacWriter(File.Open(fileout, FileMode.Create)), 3);
                    var data = File.ReadAllBytes("c:\\temp\\data" + Thread.CurrentThread.ManagedThreadId + ".wav");
                    flacStream.Write(data, 0, data.Length);
                    flacStream.Flush();
                    flacStream.Close();

                    Thread.Sleep(100);

                    File.Move(fileout, Path.GetDirectoryName(filename) + "\\" + Path.GetFileNameWithoutExtension(filename) + "_" + j.ToString() + ".flac");

                    File.Delete("c:\\temp\\data" + Thread.CurrentThread.ManagedThreadId + ".wav");
                }
            }
        }

        public static double[] ReadFLAC(string filename, int j, BBF_Header2 header)
        {
            double[] data = new double[header.Channels[j].NumberPoints];

            if (header.Dataformat == BBF_FileFormat.FLAC_ShortChunk)
            {
                double scale = header.Channels[j].Scale;
                double offset = header.Channels[j].Offset;

                byte[] buffer = null;

                using (var bInput = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read)))
                {
                    buffer = bInput.ReadBytes((int)bInput.BaseStream.Length);
                }

                byte[] data2 = null;
                using (MemoryStream m = new MemoryStream(buffer))
                {
                    FlacBox.WaveOverFlacStream flacStream = new FlacBox.WaveOverFlacStream(m, FlacBox.WaveOverFlacStreamMode.Decode);
                    data2 = new byte[header.Channels[j].NumberPoints * 2 + 44];
                    flacStream.Read(data2, 0, data2.Length);
                }
                short[] sBuffer = new short[header.Channels[j].NumberPoints];
                Buffer.BlockCopy(data2, 44, sBuffer, 0, data2.Length - 44);

                for (long i = 0; i < header.Channels[j].NumberPoints; i++)
                {
                    data[i] = sBuffer[i] * scale - offset;
                }
            }
            return data;
        }

        public static void WriteJSON(
            string filename, string experimentPath,
            double voltage, uint FileDate, uint FileTime_ms, double samplingRate,
            List<short[]> current,
            List<string> ChannelNames,
            Dictionary<string, string> pyHeader,
            double[] scales, double[] offsets
            )
        {
            using (var output = new BinaryWriter(File.Open(filename, FileMode.Create, FileAccess.Write)))
            {
                BBF_FileFormat fileFormat = BBF_FileFormat.FLAC_ShortChunk;

                int numChannels = current.Count;

                DataModel.Fileserver.BBF_Header2 header = new BBF_Header2
                {
                    numberChannels = numChannels,
                    Dataformat = fileFormat,
                    FileDate = FileDate,
                    FileTime_ms = FileTime_ms,
                    numberPoints = current[0].GetLength(0),
                    SampleRate = samplingRate,
                    experimentPath = experimentPath,
                    otherInformation = " ",
                    PyHeader = pyHeader,
                    Voltage = voltage
                };

                header.Channels = new List<BBF_ChannelHeader>();
                for (int j = 0; j < numChannels; j++)
                {
                    string filelabel = ChannelNames[j];
                    string unit = "pA";

                    DataModel.Fileserver.BBF_ChannelHeader channelHeader = new BBF_ChannelHeader
                    {
                        Name = filelabel,
                        Unit = unit,
                        Offset = offsets[j],
                        Scale = scales[j],
                        SingleVoltage = (numChannels == 1),
                        Voltage = voltage,
                        NumberPoints = current[j].GetLength(0),
                        SampleRate = samplingRate,
                        Address = 0,
                        ByteLength = 0
                    };

                    header.Channels.Add(channelHeader);
                }
                string jHeader = Newtonsoft.Json.JsonConvert.SerializeObject(header) + " ".PadRight(50);

                output.Write(jHeader);
            }
        }

        public static List<FileTrace> ReadJSON(string filename)
        {
            List<FileTrace> channels = new List<FileTrace>();
            using (var bInput = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read)))
            {

                string jHeader = bInput.ReadString();
                BBF_Header2 header = (BBF_Header2)Newtonsoft.Json.JsonConvert.DeserializeObject(jHeader, typeof(BBF_Header2));


                for (int j = 0; j < header.numberChannels; j++)
                {
                    string flac = Path.GetDirectoryName(filename) + "\\" + Path.GetFileNameWithoutExtension(filename) + "_" + j.ToString() + ".flac";
                    var data = ReadFLAC(flac, j, header);
                    FileTrace ft = new FileTrace { Filename = filename, SampleRate = header.Channels[j].SampleRate, Trace = data, TraceName = header.Channels[j].Name };
                    channels.Add(ft);
                }
            }
            return channels;
        }

        public static void ConvertBSN_FLAC(string experimentFile, string binFile, string saveFile, string channelName, Dictionary<string, string> pyHeader)
        {
            using (FileStream fileStream = new FileStream(binFile, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
                      (FileOptions)0x20000000 | FileOptions.WriteThrough & FileOptions.SequentialScan))
            {
                using (BinaryReader binReader = new BinaryReader(fileStream))
                {
                    try
                    {
                        // Return to the beginning of the stream.
                        binReader.BaseStream.Position = 0;
                        int nTraces = binReader.ReadInt32();
                        int samples = binReader.ReadInt32();

                        double scale = binReader.ReadDouble();
                        double offset = binReader.ReadDouble();
                        double SampleRate = binReader.ReadDouble();
                        double voltage = binReader.ReadDouble();

                        short[] data = new short[samples];
                        byte[] buffer = new byte[Buffer.ByteLength(data)];

                        binReader.Read(buffer, 0, buffer.Length - 8);
                        Buffer.BlockCopy(buffer, 0, data, 0, buffer.Length);
                        buffer = null;

                        uint FileDate = (uint)(DateTime.Now.Year * 100 * 100 + DateTime.Now.Month * 100 + DateTime.Now.Day);
                        uint FileTime_ms = (uint)(DateTime.Now.Hour * 60 * 60 * 1000 + DateTime.Now.Minute * 60 * 1000 + DateTime.Now.Second * 1000);

                        WriteJSON(saveFile + ".json", experimentFile, voltage, FileDate, FileTime_ms, SampleRate,
                            new List<short[]> { data },
                            new List<string> { channelName }, pyHeader, new double[] { scale }, new double[] { offset });
                        SaveFLAC(saveFile + ".flac", new List<short[]> { data });
                        // SaveBBFFLAC(saveFile, saveName, processedFile + "\\" + saveName + ".bdaq", data, 0, 0, SampleRate, new double[] { scale }, new double[] { offset });
                    }
                    catch (Exception ex) //(EndOfStreamException e2)
                    {
                        System.Diagnostics.Debug.Print(ex.Message);
                    }
                }
            }

        }

        public static void ConvertBSN_BBF(string processedFile, string binFile, string saveFile, string saveName)
        {
            using (FileStream fileStream = new FileStream(binFile, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
                      (FileOptions)0x20000000 | FileOptions.WriteThrough & FileOptions.SequentialScan))
            {
                using (BinaryReader binReader = new BinaryReader(fileStream))
                {
                    try
                    {
                        // Return to the beginning of the stream.
                        binReader.BaseStream.Position = 0;
                        int nTraces = binReader.ReadInt32();
                        int samples = binReader.ReadInt32();

                        double scale = binReader.ReadDouble();
                        double offset = binReader.ReadDouble();
                        double SampleRate = binReader.ReadDouble();
                        double voltage = binReader.ReadDouble();

                        short[] data = new short[samples];
                        byte[] buffer = new byte[Buffer.ByteLength(data)];

                        binReader.Read(buffer, 0, buffer.Length - 8);
                        Buffer.BlockCopy(buffer, 0, data, 0, buffer.Length);
                        buffer = null;

                        SaveBBFFLAC(saveFile, saveName, processedFile + "\\" + saveName + ".bdaq", data, 0, 0, SampleRate, new double[] { scale }, new double[] { offset });
                    }
                    catch (Exception ex) //(EndOfStreamException e2)
                    {
                        System.Diagnostics.Debug.Print(ex.Message);
                    }
                }
            }

        }

        public static void ConvertBin(string binFile, string saveFile, string saveName)
        {
            FileTrace ft = OpenBinary(binFile)[0];


            var trace = ft.Trace;
            double sampleRate = 20000;
            double M = trace.Max();
            double mm = trace.Min();

            double offset = (M + mm) / 2;
            double scale = (short.MaxValue - 3) / Math.Max(M - offset, offset - mm);
            scale = 1 / scale;

            short[] data = new short[trace.Length];
            for (int i = 0; i < trace.Length; i++)
            {
                data[i] = (short)Math.Round((trace[i] - offset) / scale);
            }

            SaveBBFFLAC(saveFile, saveName, binFile, data, 0, 0, sampleRate, new double[] { scale }, new double[] { offset });
        }


        public static void ConvertBin(string baseBBFFile, string binFile, string saveFile, string saveName)
        {
            FileTrace ft = OpenBinary(binFile)[0];
            if (Path.GetExtension(baseBBFFile).ToLower().Contains(".bbf") == true)
            {
                using (var bInput = new BinaryReader(File.Open(baseBBFFile, FileMode.Open, FileAccess.Read)))
                {
                    string jHeader = bInput.ReadString();
                    BBF_Header header = (BBF_Header)Newtonsoft.Json.JsonConvert.DeserializeObject(jHeader, typeof(BBF_Header));

                    double sampleRate = header.SampleRate * ft.Trace.Length / header.Channels[0].NumberPoints;

                    short[] data = new short[ft.Trace.Length];
                    double offset = header.Channels[0].Offset;
                    double scale = header.Channels[0].Scale;
                    for (int i = 0; i < ft.Trace.Length; i++)
                    {
                        data[i] = (short)Math.Round((ft.Trace[i] - offset) / scale);
                    }

                    SaveBBFFLAC(saveFile, saveName, header.experimentPath, data, header.FileDate, header.FileTime_ms, sampleRate, new double[] { header.Channels[0].Scale }, new double[] { header.Channels[0].Offset });
                }
            }
            else
            {


                //double sampleRate = double.Parse(header["samplerate"]);

            }

        }

        public static void ConvertWeisiToBin(DataModel.Trace trace, string filename)
        {
            string Data = File.ReadAllText(filename);
            var lines = Data.Split('\n');
            List<double> times = new List<double>();
            List<double> voltages = new List<double>();
            List<double> currents = new List<double>();
            foreach (var line in lines)
            {
                try
                {
                    var cols = line.Split(' ', '\t');
                    times.Add(double.Parse(cols[0]));
                    voltages.Add(double.Parse(cols[2]));
                    currents.Add(double.Parse(cols[3].Trim()));
                }
                catch
                {
                    System.Diagnostics.Debug.Print("");
                }
            }
            SaveIVBinaryFile(trace, times, voltages, currents);
        }

        public static void ConvertKeithleyToBin(DataModel.Trace trace, string filename)
        {
            string Data = File.ReadAllText(filename);
            var lines = Data.Split('\n');
            List<double> times = new List<double>();
            List<double> voltages = new List<double>();
            List<double> currents = new List<double>();
            bool inHeader = true;
            int timeCol = 0;
            int voltageCol = 5;
            int currentCol = 2;
            foreach (var line in lines)
            {
                if (inHeader)
                {
                    if (line.Contains("Timestamp") && line.Contains("localnode"))
                    {
                        inHeader = false;
                        var cols = line.Split(' ', '\t');
                        for (int i = 0; i < cols.Length; i++)
                        {
                            if (cols.Contains("Timestamp"))
                            {
                                timeCol = i;
                            }
                            if (cols.Contains("Reading") && cols.Contains("nvbuffer1"))
                            {
                                currentCol = i;
                            }
                            if (cols.Contains("Reading") && cols.Contains("nvbuffer2"))
                            {
                                voltageCol = i;
                            }
                        }
                    }
                }
                else
                {
                    var cols = line.Split(' ', '\t');
                    times.Add(double.Parse(cols[0]));
                    voltages.Add(double.Parse(cols[2]));
                    currents.Add(double.Parse(cols[3]));
                }
            }
            SaveIVBinaryFile(trace, times, voltages, currents);
        }

        System.Media.SoundPlayer player = null;

        public void StopSound()
        {
            try
            {
                if (player != null)
                {
                    player.Stop();
                }
            }
            catch { }
        }

        public void PlayAsSound()
        {
            using (var bInput = new BinaryReader(File.Open(this.Filename, FileMode.Open, FileAccess.Read)))
            {

                string jHeader = bInput.ReadString();
                BBF_Header header = (BBF_Header)Newtonsoft.Json.JsonConvert.DeserializeObject(jHeader, typeof(BBF_Header));


                for (int j = 0; j < header.numberChannels; j++)
                {
                    bInput.BaseStream.Seek(header.Channels[j].Address, SeekOrigin.Begin);
                    double[] data = new double[header.Channels[j].NumberPoints];

                    if (header.Dataformat == BBF_FileFormat.FLAC_ShortChunk)
                    {
                        double scale = header.Channels[j].Scale;
                        double offset = header.Channels[j].Offset;

                        byte[] buffer = null;

                        buffer = bInput.ReadBytes((int)(header.Channels[j].ByteLength));

                        using (MemoryStream m = new MemoryStream(buffer))
                        {
                            FlacBox.WaveOverFlacStream flacStream = new FlacBox.WaveOverFlacStream(m, FlacBox.WaveOverFlacStreamMode.Decode);
                            player = new System.Media.SoundPlayer(flacStream);
                            player.Play();
                        }

                    }
                }
            }
        }
    }
}
