using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.DataModel.Fileserver
{
    public enum BBF_FileFormat
    {
        ShortChunk,
        ShortInterleaved,
        FLAC_ShortChunk,
        FLAC_uShortChunk,
        uShortChunk,
        uShortInterleaved,
        doubleChunk,
        doubleInterleaved
    }

    public class BBF_Header2
    {
        public int numberChannels { get; set; }
        public int numberPoints { get; set; }
        public double SampleRate { get; set; }

        public uint FileDate { get; set; }
        public uint FileTime_ms { get; set; }
        public BBF_FileFormat Dataformat { get; set; }

        public string experimentPath { get; set; }
        public string otherInformation { get; set; }
        public double Voltage { get; set; }
        public Dictionary<string, string > PyHeader { get; set; }

        public List<BBF_ChannelHeader> Channels { get; set; }
    }

    public class BBF_Header
    {
        public int numberChannels { get; set; }
        public int numberPoints { get; set; }
        public double SampleRate { get; set; }

        public uint FileDate { get; set; }
        public uint FileTime_ms { get; set; }
        public BBF_FileFormat Dataformat { get; set; }

        public string experimentPath { get; set; }
        public string otherInformation { get; set; }

        public List<BBF_ChannelHeader> Channels { get; set; }
    }

    public class BBF_ChannelHeader
    {
        public string Name { get; set; }
        public double SampleRate { get; set; }
        public string Unit { get; set; }
        public double Scale { get; set; }
        public double Offset { get; set; }
        public bool SingleVoltage { get; set; }
        public double Voltage { get; set; }
        public bool isYChannel { get; set; }
        public string XChannel { get; set; }
        public long NumberPoints { get; set; }
        public long Address { get; set; }
        public long ByteLength { get; set; }
    }
    
}
