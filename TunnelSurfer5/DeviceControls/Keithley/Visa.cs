using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
#if Automation

namespace TunnelVision.DeviceControls.Keithley
{
    //TCPIP0::129.219.2.84::inst0::INSTR
    public class Visa
    {
#region DefaultProgram

        //        public static string DefaultProgram_B =
        //@"reset()
        //errorqueue.clear()
        //loadandrunscript myscript
        //function DCSweepVList(  numPoints, limitI, nplc)
        //    print(""Log:Making list"")
        //    sweepList = {}
        //    print(""Log:Making list"")
        //	
        //	
        //    print(""Log:reset"")
        //	reset()
        //	
        //	-- Configure the SMU
        //	smua.reset()
        //	smua.source.func					= smua.OUTPUT_DCVOLTS
        //	smua.source.limiti					= limitI
        //	smua.measure.nplc					= nplc
        //	smua.measure.delay					= 0
        //    print(""Log:buffers"")
        //	-- Prepare the Reading Buffers
        //	smua.nvbuffer1.clear()
        //	smua.nvbuffer1.collecttimestamps	= 1
        //	smua.nvbuffer2.clear()
        //	smua.nvbuffer2.collecttimestamps	= 1
        //    print(""Log:trigger"")
        //	-- Configure SMU Trigger Model for Sweep
        //	smua.trigger.source.listv(sweepList)
        //	smua.trigger.source.limiti			= limitI
        //	smua.trigger.measure.action			= smua.ENABLE
        //	smua.trigger.measure.iv(smua.nvbuffer1, smua.nvbuffer2)
        //	smua.trigger.endpulse.action		= smua.SOURCE_HOLD
        //	-- By setting the endsweep action to SOURCE_IDLE, the output will return
        //	-- to the bias level at the end of the sweep.
        //	smua.trigger.endsweep.action		= smua.SOURCE_IDLE
        //	smua.trigger.count					= numPoints
        //	smua.trigger.source.action			= smua.ENABLE
        //	-- Ready to begin the test
        //    print(""Log:start"")
        //	smua.source.output					= smua.OUTPUT_ON
        //	-- Start the trigger model execution
        //	smua.trigger.initiate()
        //	-- Wait until the sweep has completed
        //                print(""Log:wait for start"")
        //        while (smua.nvbuffer1.n==nil) do
        //            delay(.1)
        //        end
        //        print(""Time\tVoltage\tCurrent"")
        //        loopcount=0
        //        while (smua.nvbuffer1.n)<(numPoints-10) and loopcount<numPoints*2 do
        //            delay(.5)
        //            cCount = smua.nvbuffer1.n - 1
        //            print(smua.nvbuffer1.timestamps[cCount], smua.nvbuffer2[cCount], smua.nvbuffer1[cCount])
        //            loopcount = loopcount + 1
        //        end
        //	waitcomplete()
        //	print(""log:DataDump"")
        //print(smua.nvbuffer1.n)
        //format.data=format.ASCII
        //format.asciiprecision =6
        //printbuffer(1,smua.nvbuffer1.n,smua.nvbuffer1.timestamps, smua.nvbuffer2, smua.nvbuffer1)
        //	
        //	print(""done"")
        //	smua.source.output					= smua.OUTPUT_OFF
        //end
        //endscript";




        //        //waitcomplete()
        //        //smua.source.output					= smua.OUTPUT_OFF

        //        //-- Print the data back to the Console in tabular format
        //        //print(""Time\tVoltage\tCurrent"")
        //        //for x=1,smua.nvbuffer1.n do
        //        //    -- Voltage readings are in nvbuffer2.  Current readings are in nvbuffer1.
        //        //    print(smua.nvbuffer1.timestamps[x], smua.nvbuffer2[x], smua.nvbuffer1[x])
        //        //end
        //        //print(""Done"")



        //        public static string DefaultProgram =
        //           @"reset()
        //errorqueue.clear()
        //loadandrunscript myscript
        //function AC_Waveform_Sweep(Vrms, numCycles, frequency, limitI)
        //	reset()
        //
        //	-- Generate the source values
        //	local Vpp				= Vrms 
        //	local sourceValues		= {} 
        //	local pointsPerCycle	= 7200 / frequency
        //	local numDataPoints		= pointsPerCycle * numCycles
        //
        //	for i=1, numDataPoints do
        //		sourceValues[i]		= (Vpp * math.sin(i * 2 * math.pi / pointsPerCycle))
        //	end
        //
        //	-- Configure the SMU ranges
        //	smua.reset()
        //	smua.source.settling		= smua.SETTLE_FAST_POLARITY
        //	smua.source.autorangev		= smua.AUTORANGE_OFF
        //	smua.source.autorangei		= smua.AUTORANGE_OFF
        //	smua.source.rangev			= Vpp
        //	smua.source.limiti			= limitI
        //
        //	smua.measure.autorangev		= smua.AUTORANGE_OFF
        //	smua.measure.autorangei		= smua.AUTORANGE_OFF
        //	smua.measure.autozero		= smua.AUTOZERO_OFF
        //	-- Voltage will be measured on the same range as the source range
        //	smua.measure.rangei			= limitI
        //	smua.measure.nplc			= 0.001
        //
        //	-- Prepare the Reading Buffers
        //	smua.nvbuffer1.clear()
        //	smua.nvbuffer1.collecttimestamps	= 1
        //	smua.nvbuffer2.clear()
        //	smua.nvbuffer2.collecttimestamps	= 1
        //
        //	-- Configure the trigger model
        //	--============================
        //	
        //	-- Timer 1 controls the time between source points
        //	trigger.timer[1].delay = (1 / 7200)
        //	trigger.timer[1].passthrough = true
        //	trigger.timer[1].stimulus = smua.trigger.ARMED_EVENT_ID
        //	trigger.timer[1].count = numDataPoints - 1
        //
        //	-- Configure the SMU trigger model
        //	smua.trigger.source.listv(sourceValues)
        //	smua.trigger.source.limiti		= limitI
        //	smua.trigger.measure.action		= smua.ENABLE
        //	smua.trigger.measure.iv(smua.nvbuffer1, smua.nvbuffer2)
        //	smua.trigger.endpulse.action	= smua.SOURCE_HOLD
        //	smua.trigger.endsweep.action	= smua.SOURCE_IDLE
        //	smua.trigger.count				= numDataPoints
        //	smua.trigger.arm.stimulus		= 0
        //	smua.trigger.source.stimulus	= trigger.timer[1].EVENT_ID
        //	smua.trigger.measure.stimulus	= 0
        //	smua.trigger.endpulse.stimulus	= 0
        //	smua.trigger.source.action		= smua.ENABLE
        //	-- Ready to begin the test
        //
        //	smua.source.output					= smua.OUTPUT_ON
        //	-- Start the trigger model execution
        //	smua.trigger.initiate()
        //	-- Wait until the sweep has completed
        //	waitcomplete()
        //	smua.source.output					= smua.OUTPUT_OFF
        //
        //	-- Print the data back to the Console in tabular format
        //	print(""Time\tVoltage\tCurrent"")
        //	for x=1,smua.nvbuffer1.n do
        //		-- Voltage readings are in nvbuffer2.  Current readings are in nvbuffer1.
        //		print(smua.nvbuffer1.timestamps[x], smua.nvbuffer2[x], smua.nvbuffer1[x])
        //	end
        //end
        //endscript";

        //        public static string DefaultProgram__2 =
        //            @"reset()
        //errorqueue.clear()
        //loadandrunscript myscript
        //function AC_Waveform_Sweep(Vrms, numCycles, frequency, limitI)
        //	reset()
        //	local basePoints = 2000.0
        //	local Vpp				= Vrms * math.sqrt(2)
        //	local sourceValues		= {} 
        //	local pointsPerCycle	= basePoints / frequency
        //	local numDataPoints		= pointsPerCycle * numCycles
        //	for i=1, numDataPoints do
        //		sourceValues[i]		= (Vpp * math.sin(i * 2 * math.pi / pointsPerCycle))
        //	end
        //	smua.reset()
        //	smua.source.settling		= smua.SETTLE_FAST_POLARITY
        //	smua.source.autorangev		= smua.AUTORANGE_OFF
        //	smua.source.autorangei		= smua.AUTORANGE_OFF
        //	smua.source.rangev			= Vpp
        //	smua.source.settling		= smua.SETTLE_FAST_ALL
        //	smua.source.delay =0
        //	
        //	smua.measure.autorangev		= smua.AUTORANGE_OFF
        //	smua.measure.autorangei		= smua.AUTORANGE_OFF
        //	smua.measure.autozero		= smua.AUTOZERO_OFF
        //	smua.measure.nplc			= 0.001
        //	smua.nvbuffer1.clear()
        //	smua.nvbuffer1.collecttimestamps	= 1
        //	smua.nvbuffer2.clear()
        //	smua.nvbuffer2.collecttimestamps	= 1
        //	trigger.timer[1].delay = (1.0 / basePoints)
        //	trigger.timer[1].passthrough = true
        //	trigger.timer[1].stimulus = smua.trigger.ARMED_EVENT_ID
        //	trigger.timer[1].count = numDataPoints - 1
        //	smua.trigger.source.listv(sourceValues)
        //	smua.trigger.source.limiti		= limitI
        //	smua.trigger.measure.action		= smua.ENABLE
        //	smua.trigger.measure.iv(smua.nvbuffer1, smua.nvbuffer2)
        //	smua.trigger.endpulse.action	= smua.SOURCE_HOLD
        //	smua.trigger.endsweep.action	= smua.SOURCE_IDLE
        //	smua.trigger.count				= numDataPoints
        //	smua.trigger.arm.stimulus		= 0
        //	smua.trigger.source.stimulus	= trigger.timer[1].EVENT_ID
        //	smua.trigger.measure.stimulus	= 0
        //	smua.trigger.endpulse.stimulus	= 0
        //	smua.trigger.source.action		= smua.ENABLE
        //	smua.source.output					= smua.OUTPUT_ON
        //	smua.trigger.initiate()
        //	delay(.5)
        //	print(""Time\tVoltage\tCurrent"")
        //	while smua.nvbuffer1.n<(numDataPoints-10) and loopcount<numDataPoints*2 do
        //		delay(.5)
        //		cCount = smua.nvbuffer1.n - 1
        //		print(smua.nvbuffer1.timestamps[cCount], smua.nvbuffer2[cCount], smua.nvbuffer1[cCount])
        //		loopcount = loopcount + 1
        //	end
        //	waitcomplete()
        //	print(""Time\tVoltage\tCurrent"")
        //	for x=1,smua.nvbuffer1.n do
        //		-- Voltage readings are in nvbuffer2.  Current readings are in nvbuffer1.
        //		print(smua.nvbuffer1.timestamps[x], smua.nvbuffer2[x], smua.nvbuffer1[x])
        //	end
        //	print(""done"")
        //	smua.source.output					= smua.OUTPUT_OFF
        //end
        //
        //
        //endscript";

        //        public static string DefaultProgram3 =
        //            @"
        //reset()
        //errorqueue.clear()
        //loadandrunscript myscript
        //function DCSweepVLinear(start, stop, numPoints, limitI, msPerPoint)
        //	reset()
        //	
        //	-- Configure the SMU
        //	smua.reset()
        //
        //-- Prepare the Reading Buffers
        //	smua.nvbuffer1.clear()
        //	smua.nvbuffer1.collecttimestamps	= 1
        //	smua.nvbuffer2.clear()
        //	smua.nvbuffer2.collecttimestamps	= 1
        //
        //	 smua.source.delay = 0
        //        smua.sense = smua.SENSE_LOCAL
        //        smua.measure.nplc = .1
        //        smua.measure.autozero = smua.AUTOZERO_ONCE
        //        smua.measure.count = 1
        //        smua.measure.filter.enable = smua.FILTER_OFF
        //        smua.measure.filter.type = smua.FILTER_MOVING_AVG
        //        smua.measure.filter.count = 1
        //        smua.measure.delay = 0
        //        smua.measure.delayfactor = 1
        //        smua.measure.analogfilter = 0
        //        smua.source.func = smua.OUTPUT_DCVOLTS
        //        if smua.source.highc == smua.ENABLE then smua.source.highc = smua.DISABLE end
        //        smua.source.limiti = 1E-3
        //        smua.trigger.source.limiti = smua.LIMIT_AUTO
        //        smua.source.autorangev = 0
        //        smua.source.rangev = 2
        //        smua.trigger.source.listv({2E-3, 4E-3, 6E-3, 8E-3, 10E-3, 12E-3, 14E-3, 16E-3, 18E-3, 20E-3, 22E-3, 24E-3, 26E-3, 28E-3, 30E-3, 32E-3, 34E-3, 36E-3, 38E-3, 40E-3, 42E-3, 44E-3, 46E-3, 48E-3, 50E-3, 52E-3, 54E-3, 56E-3, 58E-3, 60E-3, 62E-3, 64E-3, 66E-3, 68E-3, 70E-3, 72E-3, 74E-3, 76E-3, 78E-3, 80E-3, 82E-3, 84E-3, 86E-3, 88E-3, 90E-3, 92E-3, 94E-3, 96E-3, 98E-3, 0.1, 0.102, 0.104, 0.106, 0.108, 0.11, 0.112, 0.114, 0.116, 0.118, 0.12, 0.122, 0.124, 0.126, 0.128, 0.13, 0.132, 0.134, 0.136, 0.138, 0.14, 0.142, 0.144, 0.146,
        //            0.148, 0.15, 0.152, 0.154, 0.156, 0.158, 0.16, 0.162, 0.164, 0.166, 0.168, 0.17, 0.172, 0.174, 0.176, 0.178, 0.18, 0.182, 0.184, 0.186, 0.188, 0.19, 0.192, 0.194, 0.196, 0.198, 0.2, 0.202, 0.204, 0.206, 0.208, 0.21, 0.212, 0.214, 0.216, 0.218, 0.22, 0.222, 0.224, 0.226, 0.228, 0.23, 0.232, 0.234, 0.236, 0.238, 0.24, 0.242, 0.244, 0.246, 0.248, 0.25, 0.252, 0.254, 0.256, 0.258, 0.26, 0.262, 0.264, 0.266, 0.268, 0.27, 0.272, 0.274, 0.276, 0.278, 0.28, 0.282, 0.284, 0.286, 0.288, 0.29, 0.292, 0.294,
        //            0.296, 0.298, 0.3, 0.302, 0.304, 0.306, 0.308, 0.31, 0.312, 0.314, 0.316, 0.318, 0.32, 0.322, 0.324, 0.326, 0.328, 0.33, 0.332, 0.334, 0.336, 0.338, 0.34, 0.342, 0.344, 0.346, 0.348, 0.35, 0.352, 0.354, 0.356, 0.358, 0.36, 0.362, 0.364, 0.366, 0.368, 0.37, 0.372, 0.374, 0.376, 0.378, 0.38, 0.382, 0.384, 0.386, 0.388, 0.39, 0.392, 0.394, 0.396, 0.398, 0.4, 0.402, 0.404, 0.406, 0.408, 0.41, 0.412, 0.414, 0.416, 0.418, 0.42, 0.422, 0.424, 0.426, 0.428, 0.43, 0.432, 0.434, 0.436, 0.438, 0.44, 0.442,
        //            0.444, 0.446, 0.448, 0.45, 0.452, 0.454, 0.456, 0.458, 0.46, 0.462, 0.464, 0.466, 0.468, 0.47, 0.472, 0.474, 0.476, 0.478, 0.48, 0.482, 0.484, 0.486, 0.488, 0.49, 0.492, 0.494, 0.496, 0.498, 0.5, 0.498, 0.496, 0.494, 0.492, 0.49, 0.488, 0.486, 0.484, 0.482, 0.48, 0.478, 0.476, 0.474, 0.472, 0.47, 0.468, 0.466, 0.464, 0.462, 0.46, 0.458, 0.456, 0.454, 0.452, 0.45, 0.448, 0.446, 0.444, 0.442, 0.44, 0.438, 0.436, 0.434, 0.432, 0.43, 0.428, 0.426, 0.424, 0.422, 0.42, 0.418, 0.416, 0.414, 0.412, 0.41,
        //            0.408, 0.406, 0.404, 0.402, 0.4, 0.398, 0.396, 0.394, 0.392, 0.39, 0.388, 0.386, 0.384, 0.382, 0.38, 0.378, 0.376, 0.374, 0.372, 0.37, 0.368, 0.366, 0.364, 0.362, 0.36, 0.358, 0.356, 0.354, 0.352, 0.35, 0.348, 0.346, 0.344, 0.342, 0.34, 0.338, 0.336, 0.334, 0.332, 0.33, 0.328, 0.326, 0.324, 0.322, 0.32, 0.318, 0.316, 0.314, 0.312, 0.31, 0.308, 0.306, 0.304, 0.302, 0.3, 0.298, 0.296, 0.294, 0.292, 0.29, 0.288, 0.286, 0.284, 0.282, 0.28, 0.278, 0.276, 0.274, 0.272, 0.27, 0.268, 0.266, 0.264, 0.262,
        //            0.26, 0.258, 0.256, 0.254, 0.252, 0.25, 0.248, 0.246, 0.244, 0.242, 0.24, 0.238, 0.236, 0.234, 0.232, 0.23, 0.228, 0.226, 0.224, 0.222, 0.22, 0.218, 0.216, 0.214, 0.212, 0.21, 0.208, 0.206, 0.204, 0.202, 0.2, 0.198, 0.196, 0.194, 0.192, 0.19, 0.188, 0.186, 0.184, 0.182, 0.18, 0.178, 0.176, 0.174, 0.172, 0.17, 0.168, 0.166, 0.164, 0.162, 0.16, 0.158, 0.156, 0.154, 0.152, 0.15, 0.148, 0.146, 0.144, 0.142, 0.14, 0.138, 0.136, 0.134, 0.132, 0.13, 0.128, 0.126, 0.124, 0.122, 0.12, 0.118, 0.116, 0.114,
        //            0.112, 0.11, 0.108, 0.106, 0.104, 0.102, 0.1, 98E-3, 96E-3, 94E-3, 92E-3, 90E-3, 88E-3, 86E-3, 84E-3, 82E-3, 80E-3, 78E-3, 76E-3, 74E-3, 72E-3, 70E-3, 68E-3, 66E-3, 64E-3, 62E-3, 60E-3, 58E-3, 56E-3, 54E-3, 52E-3, 50E-3, 48E-3, 46E-3, 44E-3, 42E-3, 40E-3, 38E-3, 36E-3, 34E-3, 32E-3, 30E-3, 28E-3, 26E-3, 24E-3, 22E-3, 20E-3, 18E-3, 16E-3, 14E-3, 12E-3, 10E-3, 8E-3, 6E-3, 4E-3, 2E-3, 0, -2E-3, -4E-3, -6E-3, -8E-3, -10E-3, -12E-3, -14E-3, -16E-3, -18E-3, -20E-3, -22E-3, -24E-3, -26E-3, -28E-3, -30E-3,
        //            -32E-3, -34E-3, -36E-3, -38E-3, -40E-3, -42E-3, -44E-3, -46E-3, -48E-3, -50E-3, -52E-3, -54E-3, -56E-3, -58E-3, -60E-3, -62E-3, -64E-3, -66E-3, -68E-3, -70E-3, -72E-3, -74E-3, -76E-3, -78E-3, -80E-3, -82E-3, -84E-3, -86E-3, -88E-3, -90E-3, -92E-3, -94E-3, -96E-3, -98E-3, -0.1, -0.102, -0.104, -0.106, -0.108, -0.11, -0.112, -0.114, -0.116, -0.118, -0.12, -0.122, -0.124, -0.126, -0.128, -0.13, -0.132, -0.134, -0.136, -0.138, -0.14, -0.142, -0.144, -0.146, -0.148, -0.15, -0.152, -0.154, -0.156, -0.158,
        //            -0.16, -0.162, -0.164, -0.166, -0.168, -0.17, -0.172, -0.174, -0.176, -0.178, -0.18, -0.182, -0.184, -0.186, -0.188, -0.19, -0.192, -0.194, -0.196, -0.198, -0.2, -0.202, -0.204, -0.206, -0.208, -0.21, -0.212, -0.214, -0.216, -0.218, -0.22, -0.222, -0.224, -0.226, -0.228, -0.23, -0.232, -0.234, -0.236, -0.238, -0.24, -0.242, -0.244, -0.246, -0.248, -0.25, -0.252, -0.254, -0.256, -0.258, -0.26, -0.262, -0.264, -0.266, -0.268, -0.27, -0.272, -0.274, -0.276, -0.278, -0.28, -0.282, -0.284, -0.286, -0.288,
        //            -0.29, -0.292, -0.294, -0.296, -0.298, -0.3, -0.302, -0.304, -0.306, -0.308, -0.31, -0.312, -0.314, -0.316, -0.318, -0.32, -0.322, -0.324, -0.326, -0.328, -0.33, -0.332, -0.334, -0.336, -0.338, -0.34, -0.342, -0.344, -0.346, -0.348, -0.35, -0.352, -0.354, -0.356, -0.358, -0.36, -0.362, -0.364, -0.366, -0.368, -0.37, -0.372, -0.374, -0.376, -0.378, -0.38, -0.382, -0.384, -0.386, -0.388, -0.39, -0.392, -0.394, -0.396, -0.398, -0.4, -0.402, -0.404, -0.406, -0.408, -0.41, -0.412, -0.414, -0.416, -0.418,
        //            -0.42, -0.422, -0.424, -0.426, -0.428, -0.43, -0.432, -0.434, -0.436, -0.438, -0.44, -0.442, -0.444, -0.446, -0.448, -0.45, -0.452, -0.454, -0.456, -0.458, -0.46, -0.462, -0.464, -0.466, -0.468, -0.47, -0.472, -0.474, -0.476, -0.478, -0.48, -0.482, -0.484, -0.486, -0.488, -0.49, -0.492, -0.494, -0.496, -0.498, -0.5, -0.498, -0.496, -0.494, -0.492, -0.49, -0.488, -0.486, -0.484, -0.482, -0.48, -0.478, -0.476, -0.474, -0.472, -0.47, -0.468, -0.466, -0.464, -0.462, -0.46, -0.458, -0.456, -0.454, -0.452,
        //            -0.45, -0.448, -0.446, -0.444, -0.442, -0.44, -0.438, -0.436, -0.434, -0.432, -0.43, -0.428, -0.426, -0.424, -0.422, -0.42, -0.418, -0.416, -0.414, -0.412, -0.41, -0.408, -0.406, -0.404, -0.402, -0.4, -0.398, -0.396, -0.394, -0.392, -0.39, -0.388, -0.386, -0.384, -0.382, -0.38, -0.378, -0.376, -0.374, -0.372, -0.37, -0.368, -0.366, -0.364, -0.362, -0.36, -0.358, -0.356, -0.354, -0.352, -0.35, -0.348, -0.346, -0.344, -0.342, -0.34, -0.338, -0.336, -0.334, -0.332, -0.33, -0.328, -0.326, -0.324, -0.322,
        //            -0.32, -0.318, -0.316, -0.314, -0.312, -0.31, -0.308, -0.306, -0.304, -0.302, -0.3, -0.298, -0.296, -0.294, -0.292, -0.29, -0.288, -0.286, -0.284, -0.282, -0.28, -0.278, -0.276, -0.274, -0.272, -0.27, -0.268, -0.266, -0.264, -0.262, -0.26, -0.258, -0.256, -0.254, -0.252, -0.25, -0.248, -0.246, -0.244, -0.242, -0.24, -0.238, -0.236, -0.234, -0.232, -0.23, -0.228, -0.226, -0.224, -0.222, -0.22, -0.218, -0.216, -0.214, -0.212, -0.21, -0.208, -0.206, -0.204, -0.202, -0.2, -0.198, -0.196, -0.194, -0.192,
        //            -0.19, -0.188, -0.186, -0.184, -0.182, -0.18, -0.178, -0.176, -0.174, -0.172, -0.17, -0.168, -0.166, -0.164, -0.162, -0.16, -0.158, -0.156, -0.154, -0.152, -0.15, -0.148, -0.146, -0.144, -0.142, -0.14, -0.138, -0.136, -0.134, -0.132, -0.13, -0.128, -0.126, -0.124, -0.122, -0.12, -0.118, -0.116, -0.114, -0.112, -0.11, -0.108, -0.106, -0.104, -0.102, -0.1, -98E-3, -96E-3, -94E-3, -92E-3, -90E-3, -88E-3, -86E-3, -84E-3, -82E-3, -80E-3, -78E-3, -76E-3, -74E-3, -72E-3, -70E-3, -68E-3, -66E-3, -64E-3,
        //        -62E-3, -60E-3, -58E-3, -56E-3, -54E-3, -52E-3, -50E-3, -48E-3, -46E-3, -44E-3, -42E-3, -40E-3, -38E-3, -36E-3, -34E-3, -32E-3, -30E-3, -28E-3, -26E-3, -24E-3, -22E-3, -20E-3, -18E-3, -16E-3, -14E-3, -12E-3, -10E-3, -8E-3, -6E-3, -4E-3})
        //        smua.measure.autorangei = 0
        //        smua.measure.rangei = 100E-12
        //        smua.measure.autorangev = smua.source.autorangev
        //        if (smua.measure.autorangev == 0) then
        //            smua.measure.rangev = smua.source.rangev
        //        end
        //      
        //	-- Configure SMU Trigger Model for Sweep
        //	
        //	
        //	smua.trigger.measure.iv(smua.nvbuffer1, smua.nvbuffer2)
        //	smua.trigger.endpulse.action		= smua.SOURCE_HOLD
        //	-- By setting the endsweep action to SOURCE_IDLE, the output will return
        //	-- to the bias level at the end of the sweep.
        //	smua.trigger.endsweep.action		= smua.SOURCE_IDLE
        //	-- smua.trigger.count					= numPoints
        //	smua.trigger.source.action			= smua.ENABLE
        //	-- Ready to begin the test
        //
        //	smua.source.output					= smua.OUTPUT_ON
        //	-- Start the trigger model execution
        //	smua.trigger.initiate()
        //	-- Wait until the sweep has completed
        //	
        //	
        //	delay(1)
        //	-- Print the data back to the Console in tabular format
        //    print(""Time\tVoltage\tCurrent"")
        //    loopcount = 0
        //    lastCount =1
        //    while loopcount<1000*numPoints do
        //        delay(.1)
        //	    cCount = smua.nvbuffer1.n - 1
        //	    if (cCount>lastCount) then
        //  	        for x=lastCount,cCount do
        //		        -- Voltage readings are in nvbuffer2.  Current readings are in nvbuffer1.
        //		        print(smua.nvbuffer1.timestamps[x], smua.nvbuffer2[x], smua.nvbuffer1[x])
        //            end
        //  	        lastCount=cCount+1
        //	    end
        //    loopcount = loopcount + 1
        //	end
        //	smua.source.output					= smua.OUTPUT_OFF
        //	print(""Done"")
        //end
        //
        //endscript";
        //        public static string DefaultProgram1 =
        //@"
        //reset()
        //errorqueue.clear()
        //loadandrunscript myscript
        //
        //function AC_Waveform_Sweep(V, numCycles, frequency, limitI, waveform)
        //	reset()
        //
        //	-- Generate the source values
        //	local Vpp				= V /1000
        //	local sourceValues		= {} 
        //    local bPoints = math.abs( 7200  / math.log(limitI)*3 )
        //	local pointsPerCycle	=bPoints / frequency
        //	local numDataPoints		= pointsPerCycle * numCycles
        //
        //    if waveform==1 then
        //	    for i=1, numDataPoints do
        //		    sourceValues[i]		= (Vpp * math.sin(i * 2 * math.pi / pointsPerCycle))
        //	    end
        //    else
        //        for i=1, numDataPoints do
        //		        sourceValues[i]=0
        //	    end
        //        for k=0,6 do
        //            C=8/math.pi/math.pi*Vpp*math.pow(-1,k)/(2*k+1)/(2*k+1)
        //            B=2*math.pi*frequency*(2*k+1)/pointsPerCycle
        //	        for i=1, numDataPoints do
        //                v=i*B
        //		        sourceValues[i]	=sourceValues[i]+ (C* math.sin(v))
        //	        end
        //        end
        //    end
        //	-- Configure the SMU ranges
        //	smua.reset()
        //	smua.measure.autorangev		= smua.AUTORANGE_OFF
        //	smua.measure.autorangei		= smua.AUTORANGE_OFF
        //    smua.measure.autozero = smua.AUTOZERO_OFF
        //    smua.sense = smua.SENSE_LOCAL
        //	smua.source.settling		= smua.SETTLE_FAST_ALL
        //	smua.source.rangev			= Vpp
        //	smua.measure.rangei			= limitI
        //	smua.source.limiti			= limitI
        //	smua.measure.nplc			= 0.001
        //    smua.source.delay =0
        //    smua.measure.interval =1/7200
        //    
        //	-- Ready to begin the test
        //	smua.source.output					= smua.OUTPUT_ON
        //	print(""Time\tVoltage\tCurrent"")
        //    timer.reset()
        //    for l_j = 1,numDataPoints do
        //	    smua.source.levelv =sourceValues[l_j]	
        //        delay(1/7200)
        //        cur,volt= smua.measure.iv()
        //        time = timer.measure.t() 
        //        print(time,volt,cur)
        //	end
        //    smua.source.output					= smua.OUTPUT_OFF
        //    print(""Done"")
        //end
        //endscript
        //";

        //    smua.measure.filter.enable = smua.FILTER_OFF
        //smua.measure.filter.type = smua.FILTER_MOVING_AVG
        //smua.measure.filter.count = 1
#endregion

        public Visa()
        {

            VisaWorker.DoWork += VisaWorker_DoWork;
            VisaWorker.RunWorkerCompleted += VisaWorker_RunWorkerCompleted;
            VisaWorker.ProgressChanged += VisaWorker_ProgressChanged;

        }


        private string Program;

       // private MessageBasedSession mbSession = null;
      //  private ResourceManager rmSession = null;
        public string[] DeviceNames()
        {
            //using (var rmSession = new ResourceManager())
            //{
            //    return rmSession.Find("(TCPIP)?*").ToArray();
            //}
            return null;
        }

        public void ConnectToDevice(string connectionString)
        {
          //  rmSession = new ResourceManager();
         //   mbSession = (MessageBasedSession)rmSession.Open(connectionString);

        }

        private string LoadProgram(double amplitude, double sweepRate, double iRange)
        {
            double[] Sweep = new double[]{ 2E-3, 4E-3, 6E-3, 8E-3, 10E-3, 12E-3, 14E-3, 16E-3, 18E-3, 20E-3, 22E-3, 24E-3, 26E-3, 28E-3, 30E-3, 32E-3, 34E-3, 36E-3, 38E-3, 40E-3, 42E-3, 44E-3, 46E-3, 48E-3, 50E-3, 52E-3, 54E-3, 56E-3, 58E-3, 60E-3, 62E-3, 64E-3, 66E-3, 68E-3, 70E-3, 72E-3, 74E-3, 76E-3, 78E-3, 80E-3, 82E-3, 84E-3, 86E-3, 88E-3, 90E-3, 92E-3, 94E-3, 96E-3, 98E-3, 0.1, 0.102, 0.104, 0.106, 0.108, 0.11, 0.112, 0.114, 0.116, 0.118, 0.12, 0.122, 0.124, 0.126, 0.128, 0.13, 0.132, 0.134, 0.136, 0.138, 0.14, 0.142, 0.144, 0.146,
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

            List<string> sSweep = (from x in Sweep where Math.Abs(x) < amplitude select x.ToString()).ToList();
            string sweepList = "sweep1.trigger.source.listv({";
            for (int i = 0; i < sSweep.Count - 1; i++)
            {
                sweepList += sSweep[i] + ",";
                if ((i % 20) == 0)
                    sweepList += "\n";
            }

            sweepList += sSweep.Last() + "})";

            Program = File.ReadAllText((string)Properties.Settings.Default["KeithleyFile"]);

            Program = Program.Replace("sweep1.trigger.source.listv({})", sweepList);
            Program = Program.Replace("tpXXX", sweepRate.ToString());
            Program = Program.Replace("pwXXX", (sweepRate-1).ToString());
            Program = Program.Replace("nosXXX", (sSweep.Count).ToString());
            Program = Program.Replace("rangeiXXX", iRange.ToString());
          //  File.WriteAllText(@"C:\temp\t.txt", Program);
            string outString = "";
            foreach (string s in Program.Split('\n'))
            {
                if (s.Trim() != "")
                {
                    //mbSession.RawIO.Write(s);
                   // if (mbSession.ReadStatusByte() == Ivi.Visa.StatusByteFlags.MessageAvailable)
                    {
                   //     outString += (mbSession.RawIO.ReadString()) + "\n";
                    }
                }
            }

            string errors = outString + "\n" + CheckErrors();
            if (errors.Trim() != "0" && errors.Trim() != "")
            {
                MessageBox.Show(errors);
            }

            return outString;
        }

        List<double> Times = new List<double>();
        public List<double> dVoltages = new List<double>();
        List<double> Voltages = new List<double>();
        List<double> Currents = new List<double>();
        public void RunProgram(string Function)
        {
            //Program = DefaultProgram_B;
            //if (Program.Contains("sweepList = {}") == true)
            //{
            //    double amplitude = .3;
            //    dVoltages.Clear();
            //    for (int i = 0; i < 500; i++)
            //        dVoltages.Add(i / 500d * amplitude);
            //    for (int i = 500; i > -500; i--)
            //        dVoltages.Add(i / 500d * amplitude);
            //    for (int i = -500; i < 0; i++)
            //        dVoltages.Add(i / 500d * amplitude);
            //    string array = "sweepList = {";
            //    for (int i = 0; i < dVoltages.Count - 1; i++)
            //    {
            //        array += dVoltages[i] + ",";
            //        if ((i % 20) == 0)
            //            array += "\n";
            //    }
            //    array += dVoltages.Last() + "}  \n";
            //    Program = Program.Replace("sweepList = {}", array);
            //    string error = LoadProgram(Program);
            //    //mbSession.RawIO.Write(array);
            //    error += CheckErrors();
            //    mbSession.RawIO.Write("numPoints = " + dVoltages.Count + "\n");
            //    error += CheckErrors();
            //}


            ////  mbSession.RawIO.Write(@"print(""hello"")\n");

            //mbSession.RawIO.Write(Function);

            // string error2 = CheckErrors();
            Times = new List<double>();
            Voltages = new List<double>();
            Currents = new List<double>();
            Thread.Sleep(100);


            VisaWorker_DoWork(null, new DoWorkEventArgs(null));

            System.Diagnostics.Debug.Print(CheckErrors());


            if (VisaDataCompleted != null)
                VisaDataCompleted(Times, Voltages, Currents);

        }
        public void RunProgramBackground(double amplitude, double sweepRate, double iRange )
        {
            LoadProgram(amplitude, sweepRate, iRange);
          
            Times = new List<double>();
            Voltages = new List<double>();
            Currents = new List<double>();

            VisaWorker.WorkerReportsProgress = true;
            VisaWorker.RunWorkerAsync();

        }

        public string CheckErrors()
        {
            //mbSession.RawIO.Write("print(errorqueue.count)\n");
            //Thread.Sleep(100);
            //var t = mbSession.ReadStatusByte().ToString();
            //string outString = "";
            //if (t.Contains("MessageAvailable") == true)
            //{
            //    try
            //    {
            //        string tt = mbSession.RawIO.ReadString();
            //        int nErrors = (int)(double.Parse(tt));
            //        outString = nErrors + "\n";
            //        for (int i = 0; i < nErrors; i++)
            //        {
            //            mbSession.RawIO.Write("code,msg,sev,nod=errorqueue.next()\n");
            //            mbSession.RawIO.Write("print(code)\n");
            //            Thread.Sleep(100);
            //            int tt1 = (int)(double.Parse(mbSession.RawIO.ReadString()));
            //            mbSession.RawIO.Write("print(msg)\n");
            //            Thread.Sleep(100);
            //            string ttt = mbSession.RawIO.ReadString();
            //            outString += tt1 + "   " + ttt + "\n";
            //        }
            //    }
            //    catch { }

            //}
            //return outString;
            return null;
        }

        public string RunCommand(string command)
        {
           // mbSession.RawIO.Write(command);
            return "";
        }

        private string ReplaceCommonEscapeSequences(string s)
        {
            return s.Replace("\\n", "\n").Replace("\\r", "\r");
        }

        private string InsertCommonEscapeSequences(string s)
        {
            return s.Replace("\n", "\\n").Replace("\r", "\\r");
        }


        private readonly BackgroundWorker VisaWorker = new BackgroundWorker();

        private void VisaWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool isDone = false;
            int lastUpdate = 0;
            Times = new List<double>();
            Voltages = new List<double>();
            Currents = new List<double>();
            string dataLog = "";
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Reset();
            sw.Start();
            var tLimit = new TimeSpan(0,1,0);
            while (isDone == false && (sw.ElapsedMilliseconds < tLimit.TotalMilliseconds ) )
            {
                System.Diagnostics.Debug.Print(sw.ElapsedMilliseconds.ToString());
                Thread.Sleep(10);

                //if (mbSession.ReadStatusByte().ToString().Contains("MessageAvailable") == true)
                //{
                //    sw.Reset();
                //    tLimit = new TimeSpan(0, 0, 30);
                //    sw.Start();
                //    string outString = "";//(mbSession.RawIO.ReadString());
                //    string[] lines = outString.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                //    double[] v = new double[lines.Length];
                //    double[] c = new double[lines.Length];
                   
                //    for (int i = 0; i < lines.Length; i++)
                //    {
                //        if (lines[i].Contains("COMPLETE") == false)
                //        {
                //            bool vbuffer = (lines[i].ToLower().Contains("nvbuffer2") == true);
                //            if (lines[i].Contains("PTS-IN-BUFF") == true)
                //            {
                //                try
                //                {
                //                    string[] values = lines[i].Split(new string[] { "{" }, StringSplitOptions.RemoveEmptyEntries);
                //                    foreach (var value in values)
                //                    {
                //                        if (value.Contains("READINGS"))
                //                        {
                //                            string[] numbers = value.Split(new string[] { "}", "," }, StringSplitOptions.RemoveEmptyEntries);
                //                            if (vbuffer)
                //                            {

                //                                for (int j = 1; j < numbers.Length; j++)
                //                                {
                //                                    try
                //                                    {
                //                                        Voltages.Add(double.Parse(numbers[j]));
                //                                    }
                //                                    catch
                //                                    {
                //                                        Voltages.Add(Voltages[Voltages.Count - 1]);
                //                                    }
                //                                }
                //                                System.Diagnostics.Debug.Print(Currents.Max().ToString());
                //                                System.Diagnostics.Debug.Print("");

                //                            }
                //                            else
                //                            {

                //                                for (int j = 1; j < numbers.Length; j++)
                //                                {
                //                                    try
                //                                    {
                //                                        Currents.Add(double.Parse(numbers[j]) * 1e12);
                //                                    }
                //                                    catch
                //                                    {
                //                                        Currents.Add(Currents[Currents.Count - 1]);
                //                                    }
                //                                }
                //                                System.Diagnostics.Debug.Print(Currents.Max().ToString());
                //                                System.Diagnostics.Debug.Print("");

                //                            }
                //                        }

                //                        if (vbuffer && value.Contains("TIMESTAMPS"))
                //                        {
                //                            string[] numbers = value.Split(new string[] { "}", "," }, StringSplitOptions.RemoveEmptyEntries);
                //                            for (int j = 1; j < numbers.Length; j++)
                //                            {
                //                                try
                //                                {
                //                                    Times.Add(double.Parse(numbers[j]));
                //                                }
                //                                catch
                //                                {
                //                                    Times.Add(Times[Times.Count - 1]);
                //                                }
                //                            }

                //                        }


                //                    }
                //                    if (VisaWorker.IsBusy)
                //                    {
                //                        try
                //                        {
                //                            int n = Times.Count - 1;
                //                            for (int k = lastUpdate; k < n;k++ )
                //                                VisaWorker.ReportProgress(0, new Tuple<double, double, double>(Times[k], Voltages[k], Currents[k]));
                //                            lastUpdate = n+1;
                //                        }
                //                        catch (Exception ex)
                //                        {
                //                            System.Diagnostics.Debug.Print(ex.Message);
                //                        }
                //                    }
                //                }
                //                catch (Exception Exception)
                //                {
                //                    System.Diagnostics.Debug.Print(Exception.Message);
                //                }
                //                //}
                //            }
                //        }
                //        else
                //        {
                //            isDone = true;
                //        }
                //    }

                //    dataLog += outString + "\n";
                //}
            }

            for (int i = 0; i < 2; i++)
            {
                Thread.Sleep(500);

                //if (mbSession.ReadStatusByte().ToString().Contains("MessageAvailable") == true)
                //{
                //    string outString = (mbSession.RawIO.ReadString());
                //}
            }

            string error = CheckErrors();
            if (error.Trim() != "" && error.Trim()!="0")
                System.Diagnostics.Debug.Print(error);

        }



        private void VisaWorker_RunWorkerCompleted(object sender,
                                       RunWorkerCompletedEventArgs e)
        {
            if (VisaDataCompleted != null)
                VisaDataCompleted(Times, Voltages, Currents);
        }

        void VisaWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            var data = (Tuple<double, double, double>)e.UserState;

            if (VisaPointDelivered != null)
                VisaPointDelivered(data.Item1, data.Item2, data.Item3);
        }

        public delegate void VisaPointDeliveredEvent(double time, double voltage, double current);
        public event VisaPointDeliveredEvent VisaPointDelivered;

        public delegate void VisaDataCompletedEvent(List<double> time, List<double> voltage, List<double> current);
        public event VisaDataCompletedEvent VisaDataCompleted;
    }
}
#endif