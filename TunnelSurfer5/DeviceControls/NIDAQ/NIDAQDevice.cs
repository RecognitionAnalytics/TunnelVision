﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if  DAQ 
using NationalInstruments;
using NationalInstruments.DAQmx;
#endif

namespace TunnelVision.DeviceControls.NIDAQ
{
#if DAQ
    public class NIDAQDevice
    {
        public string[] DeviceNamesIn()
        {
            return DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External);

        }

        public string[] DeviceNamesOut()
        {
            return DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AO, PhysicalChannelAccess.External);
        }

        private NationalInstruments.DAQmx.Task myTask;
        private AnalogMultiChannelReader reader;
        private AnalogWaveform<double>[] data;

    #region voltageSweep
        private double[] voltageSweep = new double[]{2E-3, 4E-3, 6E-3, 8E-3, 10E-3, 12E-3, 14E-3, 16E-3, 18E-3, 20E-3, 22E-3, 24E-3, 26E-3, 28E-3, 30E-3, 32E-3, 34E-3, 36E-3, 38E-3, 40E-3, 42E-3, 44E-3, 46E-3, 48E-3, 50E-3, 52E-3, 54E-3, 56E-3, 58E-3, 60E-3, 62E-3, 64E-3, 66E-3, 68E-3, 70E-3, 72E-3, 74E-3, 76E-3, 78E-3, 80E-3, 82E-3, 84E-3, 86E-3, 88E-3, 90E-3, 92E-3, 94E-3, 96E-3, 98E-3, 0.1, 0.102, 0.104, 0.106, 0.108, 0.11, 0.112, 0.114, 0.116, 0.118, 0.12, 0.122, 0.124, 0.126, 0.128, 0.13, 0.132, 0.134, 0.136, 0.138, 0.14, 0.142, 0.144, 0.146,
            0.148, 0.15, 0.152, 0.154, 0.156, 0.158, 0.16, 0.162, 0.164, 0.166, 0.168, 0.17, 0.172, 0.174, 0.176, 0.178, 0.18, 0.182, 0.184, 0.186, 0.188, 0.19, 0.192, 0.194, 0.196, 0.198, 0.2, 0.202, 0.204, 0.206, 0.208, 0.21, 0.212, 0.214, 0.216, 0.218, 0.22, 0.222, 0.224, 0.226, 0.228, 0.23, 0.232, 0.234, 0.236, 0.238, 0.24, 0.242, 0.244, 0.246, 0.248, 0.25, 0.252, 0.254, 0.256, 0.258, 0.26, 0.262, 0.264, 0.266, 0.268, 0.27, 0.272, 0.274, 0.276, 0.278, 0.28, 0.282, 0.284, 0.286, 0.288, 0.29, 0.292, 0.294,
            0.296, 0.298, 0.3, 0.302, 0.304, 0.306, 0.308, 0.31, 0.312, 0.314, 0.316, 0.318, 0.32, 0.322, 0.324, 0.326, 0.328, 0.33, 0.332, 0.334, 0.336, 0.338, 0.34, 0.342, 0.344, 0.346, 0.348, 0.35, 0.352, 0.354, 0.356, 0.358, 0.36, 0.362, 0.364, 0.366, 0.368, 0.37, 0.372, 0.374, 0.376, 0.378, 0.38, 0.382, 0.384, 0.386, 0.388, 0.39, 0.392, 0.394, 0.396, 0.398, 0.4, 0.402, 0.404, 0.406, 0.408, 0.41, 0.412, 0.414, 0.416, 0.418, 0.42, 0.422, 0.424, 0.426, 0.428, 0.43, 0.432, 0.434, 0.436, 0.438, 0.44, 0.442,
            0.444, 0.446, 0.448, 0.45, 0.452, 0.454, 0.456, 0.458, 0.46, 0.462, 0.464, 0.466, 0.468, 0.47, 0.472, 0.474, 0.476, 0.478, 0.48, 0.482, 0.484, 0.486, 0.488, 0.49, 0.492, 0.494, 0.496, 0.498, 0.5, 0.498, 0.496, 0.494, 0.492, 0.49, 0.488, 0.486, 0.484, 0.482, 0.48, 0.478, 0.476, 0.474, 0.472, 0.47, 0.468, 0.466, 0.464, 0.462, 0.46, 0.458, 0.456, 0.454, 0.452, 0.45, 0.448, 0.446, 0.444, 0.442, 0.44, 0.438, 0.436, 0.434, 0.432, 0.43, 0.428, 0.426, 0.424, 0.422, 0.42, 0.418, 0.416, 0.414, 0.412, 0.41,
            0.408, 0.406, 0.404, 0.402, 0.4, 0.398, 0.396, 0.394, 0.392, 0.39, 0.388, 0.386, 0.384, 0.382, 0.38, 0.378, 0.376, 0.374, 0.372, 0.37, 0.368, 0.366, 0.364, 0.362, 0.36, 0.358, 0.356, 0.354, 0.352, 0.35, 0.348, 0.346, 0.344, 0.342, 0.34, 0.338, 0.336, 0.334, 0.332, 0.33, 0.328, 0.326, 0.324, 0.322, 0.32, 0.318, 0.316, 0.314, 0.312, 0.31, 0.308, 0.306, 0.304, 0.302, 0.3, 0.298, 0.296, 0.294, 0.292, 0.29, 0.288, 0.286, 0.284, 0.282, 0.28, 0.278, 0.276, 0.274, 0.272, 0.27, 0.268, 0.266, 0.264, 0.262,
            0.26, 0.258, 0.256, 0.254, 0.252, 0.25, 0.248, 0.246, 0.244, 0.242, 0.24, 0.238, 0.236, 0.234, 0.232, 0.23, 0.228, 0.226, 0.224, 0.222, 0.22, 0.218, 0.216, 0.214, 0.212, 0.21, 0.208, 0.206, 0.204, 0.202, 0.2, 0.198, 0.196, 0.194, 0.192, 0.19, 0.188, 0.186, 0.184, 0.182, 0.18, 0.178, 0.176, 0.174, 0.172, 0.17, 0.168, 0.166, 0.164, 0.162, 0.16, 0.158, 0.156, 0.154, 0.152, 0.15, 0.148, 0.146, 0.144, 0.142, 0.14, 0.138, 0.136, 0.134, 0.132, 0.13, 0.128, 0.126, 0.124, 0.122, 0.12, 0.118, 0.116, 0.114,
            0.112, 0.11, 0.108, 0.106, 0.104, 0.102, 0.1, 98E-3, 96E-3, 94E-3, 92E-3, 90E-3, 88E-3, 86E-3, 84E-3, 82E-3, 80E-3, 78E-3, 76E-3, 74E-3, 72E-3, 70E-3, 68E-3, 66E-3, 64E-3, 62E-3, 60E-3, 58E-3, 56E-3, 54E-3, 52E-3, 50E-3, 48E-3, 46E-3, 44E-3, 42E-3, 40E-3, 38E-3, 36E-3, 34E-3, 32E-3, 30E-3, 28E-3, 26E-3, 24E-3, 22E-3, 20E-3, 18E-3, 16E-3, 14E-3, 12E-3, 10E-3, 8E-3, 6E-3, 4E-3, 2E-3, 0, -2E-3, -4E-3, -6E-3, -8E-3, -10E-3, -12E-3, -14E-3, -16E-3, -18E-3, -20E-3, -22E-3, -24E-3, -26E-3, -28E-3, -30E-3,
            -32E-3, -34E-3, -36E-3, -38E-3, -40E-3, -42E-3, -44E-3, -46E-3, -48E-3, -50E-3, -52E-3, -54E-3, -56E-3, -58E-3, -60E-3, -62E-3, -64E-3, -66E-3, -68E-3, -70E-3, -72E-3, -74E-3, -76E-3, -78E-3, -80E-3, -82E-3, -84E-3, -86E-3, -88E-3, -90E-3, -92E-3, -94E-3, -96E-3, -98E-3, -0.1, -0.102, -0.104, -0.106, -0.108, -0.11, -0.112, -0.114, -0.116, -0.118, -0.12, -0.122, -0.124, -0.126, -0.128, -0.13, -0.132, -0.134, -0.136, -0.138, -0.14, -0.142, -0.144, -0.146, -0.148, -0.15, -0.152, -0.154, -0.156, -0.158,
            -0.16, -0.162, -0.164, -0.166, -0.168, -0.17, -0.172, -0.174, -0.176, -0.178, -0.18, -0.182, -0.184, -0.186, -0.188, -0.19, -0.192, -0.194, -0.196, -0.198, -0.2, -0.202, -0.204, -0.206, -0.208, -0.21, -0.212, -0.214, -0.216, -0.218, -0.22, -0.222, -0.224, -0.226, -0.228, -0.23, -0.232, -0.234, -0.236, -0.238, -0.24, -0.242, -0.244, -0.246, -0.248, -0.25, -0.252, -0.254, -0.256, -0.258, -0.26, -0.262, -0.264, -0.266, -0.268, -0.27, -0.272, -0.274, -0.276, -0.278, -0.28, -0.282, -0.284, -0.286, -0.288,
            -0.29, -0.292, -0.294, -0.296, -0.298, -0.3, -0.302, -0.304, -0.306, -0.308, -0.31, -0.312, -0.314, -0.316, -0.318, -0.32, -0.322, -0.324, -0.326, -0.328, -0.33, -0.332, -0.334, -0.336, -0.338, -0.34, -0.342, -0.344, -0.346, -0.348, -0.35, -0.352, -0.354, -0.356, -0.358, -0.36, -0.362, -0.364, -0.366, -0.368, -0.37, -0.372, -0.374, -0.376, -0.378, -0.38, -0.382, -0.384, -0.386, -0.388, -0.39, -0.392, -0.394, -0.396, -0.398, -0.4, -0.402, -0.404, -0.406, -0.408, -0.41, -0.412, -0.414, -0.416, -0.418,
            -0.42, -0.422, -0.424, -0.426, -0.428, -0.43, -0.432, -0.434, -0.436, -0.438, -0.44, -0.442, -0.444, -0.446, -0.448, -0.45, -0.452, -0.454, -0.456, -0.458, -0.46, -0.462, -0.464, -0.466, -0.468, -0.47, -0.472, -0.474, -0.476, -0.478, -0.48, -0.482, -0.484, -0.486, -0.488, -0.49, -0.492, -0.494, -0.496, -0.498, -0.5, -0.498, -0.496, -0.494, -0.492, -0.49, -0.488, -0.486, -0.484, -0.482, -0.48, -0.478, -0.476, -0.474, -0.472, -0.47, -0.468, -0.466, -0.464, -0.462, -0.46, -0.458, -0.456, -0.454, -0.452,
            -0.45, -0.448, -0.446, -0.444, -0.442, -0.44, -0.438, -0.436, -0.434, -0.432, -0.43, -0.428, -0.426, -0.424, -0.422, -0.42, -0.418, -0.416, -0.414, -0.412, -0.41, -0.408, -0.406, -0.404, -0.402, -0.4, -0.398, -0.396, -0.394, -0.392, -0.39, -0.388, -0.386, -0.384, -0.382, -0.38, -0.378, -0.376, -0.374, -0.372, -0.37, -0.368, -0.366, -0.364, -0.362, -0.36, -0.358, -0.356, -0.354, -0.352, -0.35, -0.348, -0.346, -0.344, -0.342, -0.34, -0.338, -0.336, -0.334, -0.332, -0.33, -0.328, -0.326, -0.324, -0.322,
            -0.32, -0.318, -0.316, -0.314, -0.312, -0.31, -0.308, -0.306, -0.304, -0.302, -0.3, -0.298, -0.296, -0.294, -0.292, -0.29, -0.288, -0.286, -0.284, -0.282, -0.28, -0.278, -0.276, -0.274, -0.272, -0.27, -0.268, -0.266, -0.264, -0.262, -0.26, -0.258, -0.256, -0.254, -0.252, -0.25, -0.248, -0.246, -0.244, -0.242, -0.24, -0.238, -0.236, -0.234, -0.232, -0.23, -0.228, -0.226, -0.224, -0.222, -0.22, -0.218, -0.216, -0.214, -0.212, -0.21, -0.208, -0.206, -0.204, -0.202, -0.2, -0.198, -0.196, -0.194, -0.192,
            -0.19, -0.188, -0.186, -0.184, -0.182, -0.18, -0.178, -0.176, -0.174, -0.172, -0.17, -0.168, -0.166, -0.164, -0.162, -0.16, -0.158, -0.156, -0.154, -0.152, -0.15, -0.148, -0.146, -0.144, -0.142, -0.14, -0.138, -0.136, -0.134, -0.132, -0.13, -0.128, -0.126, -0.124, -0.122, -0.12, -0.118, -0.116, -0.114, -0.112, -0.11, -0.108, -0.106, -0.104, -0.102, -0.1, -98E-3, -96E-3, -94E-3, -92E-3, -90E-3, -88E-3, -86E-3, -84E-3, -82E-3, -80E-3, -78E-3, -76E-3, -74E-3, -72E-3, -70E-3, -68E-3, -66E-3, -64E-3,
        -62E-3, -60E-3, -58E-3, -56E-3, -54E-3, -52E-3, -50E-3, -48E-3, -46E-3, -44E-3, -42E-3, -40E-3, -38E-3, -36E-3, -34E-3, -32E-3, -30E-3, -28E-3, -26E-3, -24E-3, -22E-3, -20E-3, -18E-3, -16E-3, -14E-3, -12E-3, -10E-3, -8E-3, -6E-3, -4E-3};
    #endregion

        List<double> Times = new List<double>();
        List<double> Voltages = new List<double>();
        List<double> Currents = new List<double>();
        public void RunSweep(string channelInName, string channelOutName, double sampleRate, double rangeI)
        {
            int numberSamples = voltageSweep.Length;
            double rangeV = voltageSweep.Max();
            // Create a new task
            myTask = new NationalInstruments.DAQmx.Task();

            // Create a channel
            myTask.AIChannels.CreateVoltageChannel(channelInName, "", (AITerminalConfiguration)(-1), rangeI * -1, rangeI, AIVoltageUnits.Volts);
            myTask.AOChannels.CreateVoltageChannel(channelOutName, "", -1 * rangeV, rangeV, AOVoltageUnits.Volts);

            // Configure timing specs    
            myTask.Timing.ConfigureSampleClock("", sampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, numberSamples);
            //myTask.Timing.ConfigureSampleClock("", fGen.ResultingSampleClockRate, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 1000);
            // Verify the task
            myTask.Control(TaskAction.Verify);

            AnalogSingleChannelWriter writer = new AnalogSingleChannelWriter(myTask.Stream);

            //write data to buffer
            writer.WriteMultiSample(false, voltageSweep);

            // Read the data
            reader = new AnalogMultiChannelReader(myTask.Stream);

            myTask.Start();
            data = reader.ReadWaveform(numberSamples);

            for (int i = 0; i < data.Length; i++)
                Times.Add(i);
            Voltages.AddRange(voltageSweep);
            Currents.AddRange(data[0].GetRawData());
            if (VisaDataCompleted != null)
                VisaDataCompleted(Times, Voltages, Currents);
        }

        public delegate void VisaPointDeliveredEvent(double time, double voltage, double current);
       // public event VisaPointDeliveredEvent VisaPointDelivered;

        public delegate void VisaDataCompletedEvent(List<double> time, List<double> voltage, List<double> current);
        public event VisaDataCompletedEvent VisaDataCompleted;
    }
#endif
}
