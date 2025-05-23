using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.DataModel.Fileserver
{
    public class ABFLoader
    {
        public const int BLOCKSIZE = 512;

        public static void ConvertABF_BBF_Binary(string filename, string outname)
        {
            long offset = 0;
            using (var reader = new BinaryReader(File.OpenRead(filename)))
            {
                ABF_FileInfo fi = ABFLoader.ByteToType<ABF_FileInfo>(reader);
                reader.BaseStream.Seek(fi.StringsSection.uBlockIndex * BLOCKSIZE, 0);
                byte[] buffer = reader.ReadBytes((int)fi.StringsSection.uBytes);
                var BigString = System.Text.Encoding.ASCII.GetString(buffer);
                List<ABF_ADCInfo> ADC_Infos = new List<ABF_ADCInfo>();
                for (int i = 0; i < fi.ADCSection.llNumEntries; i++)
                {
                    offset = fi.ADCSection.uBlockIndex * BLOCKSIZE + fi.ADCSection.uBytes * i;
                    reader.BaseStream.Seek(offset, 0);

                    ABF_ADCInfo AI = ABFLoader.ByteToType<ABF_ADCInfo>(reader);
                    ADC_Infos.Add(AI);
                }
                offset = fi.ProtocolSection.uBlockIndex * BLOCKSIZE;
                reader.BaseStream.Seek(offset, 0);
                ABF_ProtocolInfo ProtocolSec = ABFLoader.ByteToType<ABF_ProtocolInfo>(reader);

                var nADCNumChannels = fi.ADCSection.llNumEntries;
                var lActualAcqLength = fi.DataSection.llNumEntries;
                var lDataSectionPtr = fi.DataSection.uBlockIndex;
                var nNumPointsIgnored = 0;

                var fADCSampleInterval = ProtocolSec.fADCSequenceInterval / nADCNumChannels;

                var dataSz = 2;

                switch (fi.nDataFormat)
                {
                    case 0:
                        dataSz = 2;
                        break;
                    case 1:
                        dataSz = 4;
                        break;
                }


                var headOffset = lDataSectionPtr * BLOCKSIZE + nNumPointsIgnored * dataSz;
                var dSize = fADCSampleInterval * nADCNumChannels;

                var start = 0;
                long startPt = (int)Math.Floor(1e6 * start * (1 / fADCSampleInterval));

                startPt = (long)Math.Floor((double)startPt / nADCNumChannels) * nADCNumChannels;
                long dataPtsPerChan = lActualAcqLength / nADCNumChannels - (long)Math.Floor(1e6 * start / dSize);
                long dataPts = dataPtsPerChan * nADCNumChannels;
                var RunTime = 1e-6 * lActualAcqLength * fADCSampleInterval;
                offset = startPt * dataSz + headOffset;
                reader.BaseStream.Seek(offset, 0);

                short[,] current = null;
                if (dataSz == 2)
                {
                    current = new short[dataPtsPerChan, nADCNumChannels];
                    buffer = reader.ReadBytes(Buffer.ByteLength(current));
                    Buffer.BlockCopy(buffer, 0, current, 0, Buffer.ByteLength(buffer));
                }
                else
                {
                    var currentF = new float[dataPtsPerChan, nADCNumChannels];
                    buffer = reader.ReadBytes(Buffer.ByteLength(currentF));
                    Buffer.BlockCopy(buffer, 0, currentF, 0, Buffer.ByteLength(buffer));
                }


                for (int j = 0; j < current.GetLength(1); j++)
                {
                    string filelabel = "raw";
                    if (j != 0)
                        filelabel = "Voltage";
                    using (var output = new BinaryWriter(File.Open(@"c:\temp\temp_" + filelabel + ".bbf", FileMode.Create, FileAccess.Write)))
                    {
                        output.Write((Int32)1);
                        output.Write((Int32)current.GetLength(0));
                        output.Write((double)1d / (ProtocolSec.fADCSequenceInterval * 1e-6));

                        output.Write((UInt32)fi.uFileStartDate);
                        output.Write((UInt32)fi.uFileStartTimeMS);


                        byte[] bytes = new byte[512];
                        string s = "reserved  ".PadRight(512);
                        byte[] LogoDataBy = ASCIIEncoding.ASCII.GetBytes(s);
                        Buffer.BlockCopy(LogoDataBy, 0, bytes, 0, bytes.Length);
                        output.Write(bytes);


                        output.Write((Int32)1);//1 means signed short x/scale-offset//0 means unsigned short (x-offset)/scale//2 means double

                        output.Write((Int32)1);//individual trace type
                        output.Write((Int32)current.GetLength(0)); //individual trace length
                        output.Write((double)0);//reserved
                        output.Write((double)0);//reserved
                        output.Write((double)0);//reserved

                        if (j == 0)
                            s = "Current".PadRight(50);
                        else
                            s = "Voltage".PadRight(50);

                        bytes = new byte[50];
                        LogoDataBy = ASCIIEncoding.ASCII.GetBytes(s);
                        Buffer.BlockCopy(LogoDataBy, 0, bytes, 0, bytes.Length);
                        output.Write(bytes);

                        if (j == 0)
                            s = "pA".PadRight(5);
                        else
                            s = "mV".PadRight(5);

                        bytes = new byte[5];
                        LogoDataBy = ASCIIEncoding.ASCII.GetBytes(s);
                        Buffer.BlockCopy(LogoDataBy, 0, bytes, 0, bytes.Length);
                        output.Write(bytes);

                        if (j == 0)
                        {
                            output.Write((double)1e-12);
                        }
                        else
                        {
                            output.Write((double)1e-3);
                        }

                        var scale = (1 / (ADC_Infos[j].fInstrumentScaleFactor * ADC_Infos[j].fSignalGain * ADC_Infos[j].fADCProgrammableGain * 1) * 10 / 32768);
                        var offset2 = (ADC_Infos[j].fInstrumentOffset - ADC_Infos[j].fSignalOffset);
                        output.Write((double)scale);
                        output.Write((double)offset2);

                        if (j == 1)
                            output.Write((double)-1);
                        else
                        {
                            scale = (1 / (ADC_Infos[1].fInstrumentScaleFactor * ADC_Infos[1].fSignalGain * ADC_Infos[1].fADCProgrammableGain * 1) * 10 / 32768);
                            offset2 = (ADC_Infos[1].fInstrumentOffset - ADC_Infos[1].fSignalOffset);
                            output.Write((double)current[0, 1] * scale - offset2);
                        }
                        for (long i = 0; i < dataPtsPerChan; i++)
                        {

                            output.Write(current[i, j]);
                        }
                        output.Flush();
                    }

                    File.Move(@"c:\temp\temp_" + filelabel + ".bbf", outname + "_" + filelabel + ".bbf");
                }



                //using (var output = new BinaryReader(File.Open(@"c:\temp\temp_raw.bbf", FileMode.Open, FileAccess.Read)))
                //{
                //    var nChannels = output.ReadInt32();
                //    var nPoints = output.ReadInt32();
                //    var sampleRate = output.ReadDouble();

                //    uint fileday = output.ReadUInt32();
                //    uint filetime = output.ReadUInt32();

                //    string space = output.ReadString();
                //    string label = output.ReadString();

                //    var scaleType = output.ReadInt32();
                //    var scale = output.ReadDouble();
                //    var soffset = output.ReadDouble();

                //    var data = new short[nPoints];
                //    for (int i = 0; i < nPoints; i++)
                //    {
                //        data[i] = output.ReadInt16();
                //        if (data[i] != current[i, 0])
                //        {
                //            System.Diagnostics.Debug.Print("");
                //        }
                //    }

                //}

            }
        }

        public static void ConvertABF_BBF(string filename, string experimentPath, string outname)
        {
            long offset = 0;
            using (var reader = new BinaryReader(File.OpenRead(filename)))
            {
                ABF_FileInfo fi = ABFLoader.ByteToType<ABF_FileInfo>(reader);
                reader.BaseStream.Seek(fi.StringsSection.uBlockIndex * BLOCKSIZE, 0);
                byte[] buffer = reader.ReadBytes((int)fi.StringsSection.uBytes);
                var BigString = System.Text.Encoding.ASCII.GetString(buffer);
                List<ABF_ADCInfo> ADC_Infos = new List<ABF_ADCInfo>();
                for (int i = 0; i < fi.ADCSection.llNumEntries; i++)
                {
                    offset = fi.ADCSection.uBlockIndex * BLOCKSIZE + fi.ADCSection.uBytes * i;
                    reader.BaseStream.Seek(offset, 0);

                    ABF_ADCInfo AI = ABFLoader.ByteToType<ABF_ADCInfo>(reader);
                    ADC_Infos.Add(AI);
                }
                offset = fi.ProtocolSection.uBlockIndex * BLOCKSIZE;
                reader.BaseStream.Seek(offset, 0);
                ABF_ProtocolInfo ProtocolSec = ABFLoader.ByteToType<ABF_ProtocolInfo>(reader);

                var nADCNumChannels = fi.ADCSection.llNumEntries;
                var lActualAcqLength = fi.DataSection.llNumEntries;
                var lDataSectionPtr = fi.DataSection.uBlockIndex;
                var nNumPointsIgnored = 0;

                var fADCSampleInterval = ProtocolSec.fADCSequenceInterval / nADCNumChannels;

                var dataSz = 2;

                switch (fi.nDataFormat)
                {
                    case 0:
                        dataSz = 2;
                        break;
                    case 1:
                        dataSz = 4;
                        break;
                }


                var headOffset = lDataSectionPtr * BLOCKSIZE + nNumPointsIgnored * dataSz;
                var dSize = fADCSampleInterval * nADCNumChannels;

                var start = 0;
                long startPt = (int)Math.Floor(1e6 * start * (1 / fADCSampleInterval));

                startPt = (long)Math.Floor((double)startPt / nADCNumChannels) * nADCNumChannels;
                long dataPtsPerChan = lActualAcqLength / nADCNumChannels - (long)Math.Floor(1e6 * start / dSize);
                long dataPts = dataPtsPerChan * nADCNumChannels;
                var RunTime = 1e-6 * lActualAcqLength * fADCSampleInterval;
                offset = startPt * dataSz + headOffset;
                reader.BaseStream.Seek(offset, 0);

                short[,] current = null;
                if (dataSz == 2)
                {
                    current = new short[dataPtsPerChan, nADCNumChannels];
                    buffer = reader.ReadBytes(Buffer.ByteLength(current));
                    Buffer.BlockCopy(buffer, 0, current, 0, Buffer.ByteLength(buffer));
                }
                else
                {
                    var currentF = new float[dataPtsPerChan, nADCNumChannels];
                    buffer = reader.ReadBytes(Buffer.ByteLength(currentF));
                    Buffer.BlockCopy(buffer, 0, currentF, 0, Buffer.ByteLength(buffer));
                }

                var scales = new double[current.GetLength(0)];
                var offsets = new double[current.GetLength(0)];
                for (int j = 0; j < current.GetLength(1); j++)
                {
                    scales[j] = (1 / (ADC_Infos[j].fInstrumentScaleFactor * ADC_Infos[j].fSignalGain * ADC_Infos[j].fADCProgrammableGain * 1) * 10 / 32768);
                    offsets[j] = (ADC_Infos[j].fInstrumentOffset - ADC_Infos[j].fSignalOffset);
                }

                DataModel.Fileserver.FileTrace.SaveBBFFLAC(outname, experimentPath, current, fi.uFileStartDate, fi.uFileStartTimeMS, 1d / (ProtocolSec.fADCSequenceInterval * 1e-6), scales, offsets);

            }
        }

        public static void ConvertABF_FLAC(string filename, string experimentPath, string outname)
        {
            long offset = 0;
            using (var reader = new BinaryReader(File.OpenRead(filename)))
            {
                ABF_FileInfo fi = ABFLoader.ByteToType<ABF_FileInfo>(reader);
                reader.BaseStream.Seek(fi.StringsSection.uBlockIndex * BLOCKSIZE, 0);
                byte[] buffer = reader.ReadBytes((int)fi.StringsSection.uBytes);
                var BigString = System.Text.Encoding.ASCII.GetString(buffer);
                List<ABF_ADCInfo> ADC_Infos = new List<ABF_ADCInfo>();
                for (int i = 0; i < fi.ADCSection.llNumEntries; i++)
                {
                    offset = fi.ADCSection.uBlockIndex * BLOCKSIZE + fi.ADCSection.uBytes * i;
                    reader.BaseStream.Seek(offset, 0);

                    ABF_ADCInfo AI = ABFLoader.ByteToType<ABF_ADCInfo>(reader);
                    ADC_Infos.Add(AI);
                }
                offset = fi.ProtocolSection.uBlockIndex * BLOCKSIZE;
                reader.BaseStream.Seek(offset, 0);
                ABF_ProtocolInfo ProtocolSec = ABFLoader.ByteToType<ABF_ProtocolInfo>(reader);

                var nADCNumChannels = fi.ADCSection.llNumEntries;
                var lActualAcqLength = fi.DataSection.llNumEntries;
                var lDataSectionPtr = fi.DataSection.uBlockIndex;
                var nNumPointsIgnored = 0;

                var fADCSampleInterval = ProtocolSec.fADCSequenceInterval / nADCNumChannels;

                var dataSz = 2;

                switch (fi.nDataFormat)
                {
                    case 0:
                        dataSz = 2;
                        break;
                    case 1:
                        dataSz = 4;
                        break;
                }


                var headOffset = lDataSectionPtr * BLOCKSIZE + nNumPointsIgnored * dataSz;
                var dSize = fADCSampleInterval * nADCNumChannels;

                var start = 0;
                long startPt = (int)Math.Floor(1e6 * start * (1 / fADCSampleInterval));

                startPt = (long)Math.Floor((double)startPt / nADCNumChannels) * nADCNumChannels;
                long dataPtsPerChan = lActualAcqLength / nADCNumChannels - (long)Math.Floor(1e6 * start / dSize);
                long dataPts = dataPtsPerChan * nADCNumChannels;
                var RunTime = 1e-6 * lActualAcqLength * fADCSampleInterval;
                offset = startPt * dataSz + headOffset;
                reader.BaseStream.Seek(offset, 0);

                List<short[]> current = new List<short[]>();
                if (dataSz == 2)
                {
                   var currentB = new short[dataPtsPerChan, nADCNumChannels];
                    buffer = reader.ReadBytes(Buffer.ByteLength(currentB));
                    Buffer.BlockCopy(buffer, 0, currentB, 0, Buffer.ByteLength(buffer));

                    for (int i = 0; i < nADCNumChannels; i++)
                    {
                        short[] line = new short[dataPtsPerChan];
                        for (int j = 0; j < dataPtsPerChan; j++)
                            line[j] = currentB[j, i];
                        current.Add(line);
                    }
                }
                else
                {
                    var currentF = new float[dataPtsPerChan, nADCNumChannels];
                    buffer = reader.ReadBytes(Buffer.ByteLength(currentF));
                    Buffer.BlockCopy(buffer, 0, currentF, 0, Buffer.ByteLength(buffer));
                }

                var scales = new double[current.Count];
                var offsets = new double[current.Count];
                for (int j = 0; j < current.Count; j++)
                {
                    scales[j] = (1 / (ADC_Infos[j].fInstrumentScaleFactor * ADC_Infos[j].fSignalGain * ADC_Infos[j].fADCProgrammableGain * 1) * 10 / 32768);
                    offsets[j] = (ADC_Infos[j].fInstrumentOffset - ADC_Infos[j].fSignalOffset);
                }

                DataModel.Fileserver.FileTrace.SaveFLAC(outname, new List<string> { "current", "voltage" ,"ionic"} , experimentPath,null,0, current, fi.uFileStartDate, fi.uFileStartTimeMS, 1d / (ProtocolSec.fADCSequenceInterval * 1e-6), scales, offsets);

            }
        }

        public static short SwapInt16(short v)
        {

            return (short)(((v & 0xff) << 8) | ((v >> 8) & 0xff));

        }

        public double[] ReadABF(string filename)
        {
            long offset = 0;
            using (var reader = new BinaryReader(File.OpenRead(filename)))
            {
                ABF_FileInfo fi = ABFLoader.ByteToType<ABF_FileInfo>(reader);
                reader.BaseStream.Seek(fi.StringsSection.uBlockIndex * BLOCKSIZE, 0);
                byte[] buffer = reader.ReadBytes((int)fi.StringsSection.uBytes);
                var BigString = System.Text.Encoding.ASCII.GetString(buffer);
                List<ABF_ADCInfo> ADC_Infos = new List<ABF_ADCInfo>();
                for (int i = 0; i < fi.ADCSection.llNumEntries; i++)
                {
                    offset = fi.ADCSection.uBlockIndex * BLOCKSIZE + fi.ADCSection.uBytes * i;
                    reader.BaseStream.Seek(offset, 0);

                    ABF_ADCInfo AI = ABFLoader.ByteToType<ABF_ADCInfo>(reader);
                    ADC_Infos.Add(AI);
                }
                offset = fi.ProtocolSection.uBlockIndex * BLOCKSIZE;
                reader.BaseStream.Seek(offset, 0);
                ABF_ProtocolInfo ProtocolSec = ABFLoader.ByteToType<ABF_ProtocolInfo>(reader);

                var nADCNumChannels = fi.ADCSection.llNumEntries;
                var lActualAcqLength = fi.DataSection.llNumEntries;
                var lDataSectionPtr = fi.DataSection.uBlockIndex;
                var nNumPointsIgnored = 0;

                var fADCSampleInterval = ProtocolSec.fADCSequenceInterval / nADCNumChannels;

                var dataSz = 2;

                switch (fi.nDataFormat)
                {
                    case 0:
                        dataSz = 2;
                        break;
                    case 1:
                        dataSz = 4;
                        break;
                }


                var headOffset = lDataSectionPtr * BLOCKSIZE + nNumPointsIgnored * dataSz;
                var dSize = fADCSampleInterval * nADCNumChannels;

                var start = 0;
                long startPt = (int)Math.Floor(1e6 * start * (1 / fADCSampleInterval));

                startPt = (long)Math.Floor((double)startPt / nADCNumChannels) * nADCNumChannels;
                long dataPtsPerChan = lActualAcqLength / nADCNumChannels - (long)Math.Floor(1e6 * start / dSize);
                long dataPts = dataPtsPerChan * nADCNumChannels;
                var RunTime = 1e-6 * lActualAcqLength * fADCSampleInterval;
                offset = startPt * dataSz + headOffset;
                reader.BaseStream.Seek(offset, 0);

                float[,] currentF;
                if (dataSz == 2)
                {
                    short[,] current = new short[dataPtsPerChan, nADCNumChannels];
                    buffer = reader.ReadBytes(Buffer.ByteLength(current));
                    Buffer.BlockCopy(buffer, 0, current, 0, Buffer.ByteLength(buffer));
                    currentF = new float[dataPtsPerChan, nADCNumChannels];
                    for (int i = 0; i < nADCNumChannels; i++)
                    {
                        for (int j = 0; j < dataPtsPerChan; j++)
                        {
                            currentF[j, i] = current[j, i] / (ADC_Infos[i].fInstrumentScaleFactor * ADC_Infos[i].fSignalGain * ADC_Infos[i].fADCProgrammableGain * 1)
                                * 10 / 32768 + ADC_Infos[i].fInstrumentOffset - ADC_Infos[i].fSignalOffset;
                        }
                    }
                }
                else
                {
                    currentF = new float[dataPtsPerChan, nADCNumChannels];
                    buffer = reader.ReadBytes(Buffer.ByteLength(currentF));
                    Buffer.BlockCopy(buffer, 0, currentF, 0, Buffer.ByteLength(buffer));
                }

                double[] currentD = new double[dataPtsPerChan];

                for (long i = 0; i < dataPtsPerChan; i++)
                {

                    if (currentF[i, 0] == 0)
                        System.Diagnostics.Debug.Print("");
                    currentD[i] = currentF[i, 0];
                }

                return currentD;
            }
        }

        public short[] ReadABFShort(string filename)
        {
            long offset = 0;
            using (var reader = new BinaryReader(File.OpenRead(filename)))
            {
                ABF_FileInfo fi = ABFLoader.ByteToType<ABF_FileInfo>(reader);
                reader.BaseStream.Seek(fi.StringsSection.uBlockIndex * BLOCKSIZE, 0);
                byte[] buffer = reader.ReadBytes((int)fi.StringsSection.uBytes);
                var BigString = System.Text.Encoding.ASCII.GetString(buffer);
                List<ABF_ADCInfo> ADC_Infos = new List<ABF_ADCInfo>();
                for (int i = 0; i < fi.ADCSection.llNumEntries; i++)
                {
                    offset = fi.ADCSection.uBlockIndex * BLOCKSIZE + fi.ADCSection.uBytes * i;
                    reader.BaseStream.Seek(offset, 0);

                    ABF_ADCInfo AI = ABFLoader.ByteToType<ABF_ADCInfo>(reader);
                    ADC_Infos.Add(AI);
                }
                offset = fi.ProtocolSection.uBlockIndex * BLOCKSIZE;
                reader.BaseStream.Seek(offset, 0);
                ABF_ProtocolInfo ProtocolSec = ABFLoader.ByteToType<ABF_ProtocolInfo>(reader);

                var nADCNumChannels = fi.ADCSection.llNumEntries;
                var lActualAcqLength = fi.DataSection.llNumEntries;
                var lDataSectionPtr = fi.DataSection.uBlockIndex;
                var nNumPointsIgnored = 0;

                var fADCSampleInterval = ProtocolSec.fADCSequenceInterval / nADCNumChannels;

                var dataSz = 2;

                switch (fi.nDataFormat)
                {
                    case 0:
                        dataSz = 2;
                        break;
                    case 1:
                        dataSz = 4;
                        break;
                }


                var headOffset = lDataSectionPtr * BLOCKSIZE + nNumPointsIgnored * dataSz;
                var dSize = fADCSampleInterval * nADCNumChannels;

                var start = 0;
                long startPt = (int)Math.Floor(1e6 * start * (1 / fADCSampleInterval));

                startPt = (long)Math.Floor((double)startPt / nADCNumChannels) * nADCNumChannels;
                long dataPtsPerChan = lActualAcqLength / nADCNumChannels - (long)Math.Floor(1e6 * start / dSize);
                long dataPts = dataPtsPerChan * nADCNumChannels;
                var RunTime = 1e-6 * lActualAcqLength * fADCSampleInterval;
                offset = startPt * dataSz + headOffset;
                reader.BaseStream.Seek(offset, 0);

                short[,] current = null;
                if (dataSz == 2)
                {
                    current = new short[dataPtsPerChan, nADCNumChannels];
                    buffer = reader.ReadBytes(Buffer.ByteLength(current));
                    Buffer.BlockCopy(buffer, 0, current, 0, Buffer.ByteLength(buffer));
                }
                else
                {
                    var currentF = new float[dataPtsPerChan, nADCNumChannels];
                    buffer = reader.ReadBytes(Buffer.ByteLength(currentF));
                    Buffer.BlockCopy(buffer, 0, currentF, 0, Buffer.ByteLength(buffer));
                }

                short[] currentD = new short[dataPtsPerChan];

                for (long i = 0; i < dataPtsPerChan; i++)
                {
                    currentD[i] = current[i, 0];
                }

                return currentD;
            }
        }

        public static T ByteToType<T>(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ABF_Section
    {
        [FieldOffset(0)]
        public UInt32 uBlockIndex;            // ABF block number of the first entry
        [FieldOffset(4)]
        public UInt32 uBytes;                 // size in bytes of of each entry
        [FieldOffset(8)]
        public Int64 llNumEntries;           // number of entries in this section
    };


    //headPar={
    //        'fFileSignature',0,'*byte',[-1 -1 -1 -1];
    //        'fFileVersionNumber',4,'bit8=>int',[-1 -1 -1 -1];
    //        'uFileInfoSize',8,'uint32',-1;
    //        'lActualEpisodes',12,'uint32',-1;
    //        'uFileStartDate',16','uint32',-1;
    //        'uFileStartTimeMS',20,'uint32',-1;
    //        'uStopwatchTime',24,'uint32',-1;
    //        'nFileType',28,'int16',-1;
    //        'nDataFormat',30,'int16',-1;
    //        'nSimultaneousScan',32,'int16',-1;
    //        'nCRCEnable',34,'int16',-1;
    //        'uFileCRC',36,'uint32',-1;
    //        'FileGUID',40,'uint32',-1;
    //        'uCreatorVersion',56,'uint32',-1;
    //        'uCreatorNameIndex',60,'uint32',-1;
    //        'uModifierVersion',64,'uint32',-1;
    //        'uModifierNameIndex',68,'uint32',-1;
    //        'uProtocolPathIndex',72,'uint32',-1;
    //        };
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct ABF_FileInfo
    {
        [FieldOffset(0)]
        public fixed byte uFileSignature[4];
        [FieldOffset(4)]
        public uint uFileVersionNumber;
        [FieldOffset(8)]
        public uint uFileInfoSize;
        [FieldOffset(12)]
        public uint uActualEpisodes;
        [FieldOffset(16)]
        public uint uFileStartDate;
        [FieldOffset(20)]
        public uint uFileStartTimeMS;
        [FieldOffset(24)]
        public uint uStopwatchTime;
        [FieldOffset(28)]
        public short nFileType;
        [FieldOffset(30)]
        public short nDataFormat;
        [FieldOffset(32)]
        public short nSimultaneousScan;
        [FieldOffset(34)]
        public short nCRCEnable;
        [FieldOffset(36)]
        public uint uFileCRC;
        [FieldOffset(40)]
        public fixed byte FileGUID[16];
        [FieldOffset(56)]
        public uint uCreatorVersion;
        [FieldOffset(60)]
        public uint uCreatorNameIndex;
        [FieldOffset(64)]
        public uint uModifierVersion;
        [FieldOffset(68)]
        public uint uModifierNameIndex;
        [FieldOffset(72)]
        public uint uProtocolPathIndex;
        [FieldOffset(76)]
        public ABF_Section ProtocolSection;           // the protocol
        [FieldOffset(92)]
        public ABF_Section ADCSection;
        [FieldOffset(92 + 16)]
        public ABF_Section DACSection;                // one for each DAC channel
        [FieldOffset(92 + 2 * 16)]
        public ABF_Section EpochSection;              // one for each epoch
        [FieldOffset(92 + 3 * 16)]
        public ABF_Section ADCPerDACSection;          // one for each ADC for each DAC
        [FieldOffset(92 + 4 * 16)]
        public ABF_Section EpochPerDACSection;        // one for each epoch for each DAC
        [FieldOffset(92 + 5 * 16)]
        public ABF_Section UserListSection;           // one for each user list
        [FieldOffset(92 + 6 * 16)]
        public ABF_Section StatsRegionSection;        // one for each stats region
        [FieldOffset(92 + 7 * 16)]
        public ABF_Section MathSection;
        [FieldOffset(92 + 8 * 16)]
        public ABF_Section StringsSection;

        // ABF 1 sections ...
        [FieldOffset(92 + 9 * 16)]
        public ABF_Section DataSection;            // Data
        [FieldOffset(92 + 10 * 16)]
        public ABF_Section TagSection;             // Tags
        [FieldOffset(92 + 11 * 16)]
        public ABF_Section ScopeSection;           // Scope config
        [FieldOffset(92 + 12 * 16)]
        public ABF_Section DeltaSection;           // Deltas
        [FieldOffset(92 + 13 * 16)]
        public ABF_Section VoiceTagSection;        // Voice Tags
        [FieldOffset(92 + 14 * 16)]
        public ABF_Section SynbyteraySection;      // Synch Array
        [FieldOffset(92 + 15 * 16)]
        public ABF_Section AnnotationSection;      // Annotations
        [FieldOffset(92 + 16 * 16)]
        public ABF_Section StatsSection;           // Stats config
        [FieldOffset(92 + 17 * 16)]
        public fixed byte sUnused[148];     // size = 512 bytes
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct ABF_ProtocolInfo
    {
        [FieldOffset(0)]
        public short nOperationMode;
        [FieldOffset(2)]
        public float fADCSequenceInterval;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ABF_ProtocolInfo2
    {
        public bool bEnableFileCompression;
        public fixed byte sUnused1[3];
        public uint uFileCompressionRatio;

        public float fSynchTimeUnit;
        public float fSecondsPerRun;
        public long lNumSamplesPerEpisode;
        public long lPreTriggerSamples;
        public long lEpisodesPerRun;
        public long lRunsPerTrial;
        public long lNumberOfTrials;
        public short nAveragingMode;
        public short nUndoRunCount;
        public short nFirstEpisodeInRun;
        public float fTriggerThreshold;
        public short nTriggerSource;
        public short nTriggerAction;
        public short nTriggerPolarity;
        public float fScopeOutputInterval;
        public float fEpisodeStartToStart;
        public float fRunStartToStart;
        public long lAverageCount;
        public float fTrialStartToStart;
        public short nAutoTriggerStrategy;
        public float fFirstRunDelayS;

        public short nChannelStatsStrategy;
        public long lSamplesPerTrace;
        public long lStartDisplayNum;
        public long lFinishDisplayNum;
        public short nShowPNRawData;
        public float fStatisticsPeriod;
        public long lStatisticsMeasurements;
        public short nStatisticsSaveStrategy;

        public float fADCRange;
        public float fDACRange;
        public long lADCResolution;
        public long lDACResolution;

        public short nExperimentType;
        public short nManualInfoStrategy;
        public short nCommentsEnable;
        public long lFileCommentIndex;
        public short nAutoAnalyseEnable;
        public short nSignalType;

        public short nDigitalEnable;
        public short nActiveDACChannel;
        public short nDigitalHolding;
        public short nDigitalInterEpisode;
        public short nDigitalDACChannel;
        public short nDigitalTrainActiveLogic;

        public short nStatsEnable;
        public short nStatisticsClearStrategy;

        public short nLevelHysteresis;
        public long lTimeHysteresis;
        public short nAllowExternalTags;
        public short nAverageAlgorithm;
        public float fAverageWeighting;
        public short nUndoPromptStrategy;
        public short nTrialTriggerSource;
        public short nStatisticsDisplayStrategy;
        public short nExternalTagType;
        public short nScopeTriggerOut;

        public short nLTPType;
        public short nAlternateDACOutputState;
        public short nAlternateDigitalOutputState;

        public fixed float fCellID[3];

        public short nDigitizerADCs;
        public short nDigitizerDACs;
        public short nDigitizerTotalDigitalOuts;
        public short nDigitizerSynchDigitalOuts;
        public short nDigitizerType;

        public fixed byte sUnused[304];     // size = 512 bytes

        //ABF_ProtocolInfo()
        //{
        //   MEMSET_CTOR;
        //   STATIC_ASSERT( sizeof( ABF_ProtocolInfo ) == 512 );
        //}
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ABF_MathInfo
    {
        public short nMathEnable;
        public short nMathExpression;
        public uint uMathOperatorIndex;
        public uint uMathUnitsIndex;
        public float fMathUpperLimit;
        public float fMathLowerLimit;
        public fixed short nMathADCNum[2];
        public fixed byte sUnused[16];
        public fixed float fMathK[6];

        public fixed byte sUnused2[64];     // size = 128 bytes

        //ABF_MathInfo()
        //{
        //   MEMSET_CTOR;
        //   STATIC_ASSERT( sizeof( ABF_MathInfo ) == 128 );
        //}
    }




    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct ABF_ADCInfo
    {
        [FieldOffset(0)]
        public short nADCNum;
        [FieldOffset(2)]

        public short nTelegraphEnable;
        [FieldOffset(4)]

        public short nTelegraphInstrument;
        [FieldOffset(6)]

        public float fTelegraphAdditGain;
        [FieldOffset(10)]

        public float fTelegraphFilter;
        [FieldOffset(14)]

        public float fTelegraphMembraneCap;
        [FieldOffset(18)]

        public short nTelegraphMode;
        [FieldOffset(20)]

        public float fTelegraphAccessResistance;
        [FieldOffset(24)]

        public short nADCPtoLChannelMap;
        [FieldOffset(26)]

        public short nADCSamplingSeq;
        [FieldOffset(28)]

        public float fADCProgrammableGain;
        [FieldOffset(32)]

        public float fADCDisplayAmplification;
        [FieldOffset(36)]

        public float fADCDisplayOffset;
        [FieldOffset(40)]

        public float fInstrumentScaleFactor;
        [FieldOffset(44)]

        public float fInstrumentOffset;
        [FieldOffset(48)]

        public float fSignalGain;
        [FieldOffset(52)]

        public float fSignalOffset;
        [FieldOffset(56)]

        public float fSignalLowpassFilter;
        [FieldOffset(60)]

        public float fSignalHighpassFilter;
        [FieldOffset(64)]

        public byte nLowpassFilterType;
        [FieldOffset(65)]

        public byte nHighpassFilterType;
        [FieldOffset(66)]

        public float fPostProcessLowpassFilter;
        [FieldOffset(70)]

        public byte nPostProcessLowpassFilterType;
        [FieldOffset(71)]

        public bool bEnabledDuringPN;
        [FieldOffset(71)]

        public short nStatsChannelPolarity;
        [FieldOffset(72)]

        public Int32 lADCChannelNameIndex;
        [FieldOffset(76)]

        public Int32 lADCUnitsIndex;
        [FieldOffset(80)]

        public fixed byte sUnused[46];         // size = 128 bytes

        //ABF_ADCInfo()
        //{
        //   MEMSET_CTOR;
        //   STATIC_ASSERT( sizeof( ABF_ADCInfo ) == 128 );
        //}
    }

    public unsafe struct ABF_DACInfo
    {
        // The DAC this public unsafe struct is describing.
        public short nDACNum;

        public short nTelegraphDACScaleFactorEnable;
        public float fInstrumentHoldingLevel;

        public float fDACScaleFactor;
        public float fDACHoldingLevel;
        public float fDACCalibrationFactor;
        public float fDACCalibrationOffset;

        public long lDACChannelNameIndex;
        public long lDACChannelUnitsIndex;

        public long lDACFilePtr;
        public long lDACFileNumEpisodes;

        public short nWaveformEnable;
        public short nWaveformSource;
        public short nInterEpisodeLevel;

        public float fDACFileScale;
        public float fDACFileOffset;
        public long lDACFileEpisodeNum;
        public short nDACFileADCNum;

        public short nConditEnable;
        public long lConditNumPulses;
        public float fBaselineDuration;
        public float fBaselineLevel;
        public float fStepDuration;
        public float fStepLevel;
        public float fPostTrainPeriod;
        public float fPostTrainLevel;
        public short nMembTestEnable;

        public short nLeakSubtractType;
        public short nPNPolarity;
        public float fPNHoldingLevel;
        public short nPNNumADCChannels;
        public short nPNPosition;
        public short nPNNumPulses;
        public float fPNSettlingTime;
        public float fPNInterpulse;

        public short nLTPUsageOfDAC;
        public short nLTPPresynapticPulses;

        public long lDACFilePathIndex;

        public float fMembTestPreSettlingTimeMS;
        public float fMembTestPostSettlingTimeMS;

        public short nLeakSubtractADCIndex;

        public fixed byte sUnused[124];     // size = 256 bytes

        //ABF_DACInfo()
        //{
        //   MEMSET_CTOR;
        //   STATIC_ASSERT( sizeof( ABF_DACInfo ) == 256 );
        //}
    }

    public unsafe struct ABF_EpochInfoPerDAC
    {
        // The Epoch / DAC this public unsafe struct is describing.
        public short nEpochNum;
        public short nDACNum;

        // One full set of epochs (ABF_EPOCHCOUNT) for each DAC channel ...
        public short nEpochType;
        public float fEpochInitLevel;
        public float fEpochLevelInc;
        public long lEpochInitDuration;
        public long lEpochDurationInc;
        public long lEpochPulsePeriod;
        public long lEpochPulseWidth;

        public fixed byte sUnused[18];      // size = 48 bytes

        //ABF_EpochInfoPerDAC()
        //{
        //   MEMSET_CTOR;
        //   STATIC_ASSERT( sizeof( ABF_EpochInfoPerDAC ) == 48 );
        //}
    }

    public unsafe struct ABF_EpochInfo
    {
        // The Epoch this public unsafe struct is describing.
        public short nEpochNum;

        // Describes one epoch
        public short nDigitalValue;
        public short nDigitalTrainValue;
        public short nAlternateDigitalValue;
        public short nAlternateDigitalTrainValue;
        public bool bEpochCompression;   // Compress the data from this epoch using uFileCompressionRatio

        public fixed byte sUnused[21];      // size = 32 bytes

        //ABF_EpochInfo()
        //{
        //   MEMSET_CTOR;
        //   STATIC_ASSERT( sizeof( ABF_EpochInfo ) == 32 );
        //}
    }

    public unsafe struct ABF_StatsRegionInfo
    {
        // The stats region this public unsafe struct is describing.
        public short nRegionNum;
        public short nADCNum;

        public short nStatsActiveChannels;
        public short nStatsSearchRegionFlags;
        public short nStatsSelectedRegion;
        public short nStatsSmoothing;
        public short nStatsSmoothingEnable;
        public short nStatsBaseline;
        public long lStatsBaselineStart;
        public long lStatsBaselineEnd;

        // Describes one stats region
        public long lStatsMeasurements;
        public long lStatsStart;
        public long lStatsEnd;
        public short nRiseBottomPercentile;
        public short nRiseTopPercentile;
        public short nDecayBottomPercentile;
        public short nDecayTopPercentile;
        public short nStatsSearchMode;
        public short nStatsSearchDAC;
        public short nStatsBaselineDAC;

        public fixed byte sUnused[78];   // size = 128 bytes

        //    ABF_StatsRegionInfo()
        //    {
        //        MEMSET_CTOR;
        //% STATIC_ASSERT(sizeof(ABF_StatsRegionInfo) == 128);
        //    }
    }

    public unsafe struct ABF_UserListInfo
    {
        // The user list this public unsafe struct is describing.
        public short nListNum;

        // Describes one user list
        public short nULEnable;
        public short nULParamToVary;
        public short nULRepeat;
        public long lULParamValueListIndex;

        public fixed byte sUnused[52];   // size = 64 bytes

        //public  ABF_UserListInfo()
        //{
        //    STATIC_ASSERT(sizeof(ABF_UserListInfo) == 64);
        //}
    }


}
