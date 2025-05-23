using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TunnelVision.DeviceControls.Axon
{
    public delegate void FileCreatedEvent(string filename);
    public class Axon
    {
        public Axon()
        {
            fileWatcherTimer.Tick += fileWatcherTimer_Tick;
        }

        public event FileCreatedEvent FileCreated;

        private void SetFileFolder(string filename)
        {
            #region program
            string program = @"
SetTitleMatchMode, 1
DetectHiddenWindows, on
IfWinExist, AxoScope
	WinActivate, AxoScope
else 
{
	run, c:\Program Files (x86)\Molecular Devices\pCLAMP10.4\AxoScope.exe
	Sleep 1000
	ifWinExist ahk_class #32770
		send {Enter}
	Sleep 1000
	ifWinExist ahk_class #32770
		send {Enter}
	Sleep 1000
	ifWinExist ahk_class #32770
		send {Enter}
	Sleep 500
}

Send !FF
WinWaitActive, Set Data File Names
sleep, 1000
ControlSend, ComboBox1, c
sleep, 500
ControlSend, ComboBox1, o

sleep, 100

SelectFolder()
{
ControlGet, ListVar, List, , SysListView321,  Set Data File Names
Loop, Parse, ListVar, `n  ; Rows are delimited by linefeeds (`n).
{
    RowNumber := A_Index
    Loop, Parse, A_LoopField, %A_Tab%  ; Fields (columns) in each row are delimited by tabs (A_Tab).
		IfInString, A_LoopField, c:
       {
		   
			StringLeft, searchLetter, A_LoopField, 1
			
			ControlSend, SysListView321, %searchLetter%
			sleep, 10
			ControlSend, SysListView321, {Enter}
			sleep,200
			SelectFolder()
	    }
		IfInString, A_LoopField, Tunnel
        {
			ControlSend, SysListView321, Tun
			ControlSend, SysListView321,  {Enter}
	    }
}	
	
	
}

SelectFolder()
Control,Check,,Button6,Set Data File Names
Control,Uncheck,,Button5,Set Data File Names
SetKeyDelay, 1, 1
ControlClick, Edit2, Set Data File Names
sleep,100
ControlSend, Edit2, tempFile
ControlClick, Button2, Set Data File Names
sleep,500
IfWinExist, Set Data File Names
	ControlClick, Button2, Set Data File Names
";
            #endregion

            program = program.Replace("tempFile", filename);
            File.WriteAllText(@"C:\TunnelSurferTemp\FileFolder.ahk", program);
            var process = Process.Start(@"C:\TunnelSurferTemp\FileFolder.ahk");
            while (process.HasExited == false)
            {
                Thread.Sleep(100);
            }
        }

        private void SetProtocol(string protocolFile)
        {
            #region program
            string program = @"
SetTitleMatchMode, 1
DetectHiddenWindows, on
IfWinExist, AxoScope
	WinActivate, AxoScope
else 
{
	run, c:\Program Files (x86)\Molecular Devices\pCLAMP10.4\AxoScope.exe

	Sleep 1000
	ifWinExist ahk_class #32770
		send {Enter}

	Sleep 1000
	ifWinExist ahk_class #32770
		send {Enter}

	Sleep 1000
	ifWinExist ahk_class #32770
		send {Enter}
	
	Sleep 500
}
sleep, 200
Send !AO
WinWaitActive, Open Protocol
sleep, 100
SetKeyDelay, 1, 1
ControlSendRaw, Edit1, protocolFile
sleep,50
ControlClick, Button2, Open Protocol
sleep,100
IfWinExist, Open Protocol
	ControlClick, Button2, Open Protocol
";
            #endregion

            program = program.Replace("protocolFile", protocolFile);
            File.WriteAllText(@"C:\TunnelSurferTemp\ProtocolFile.ahk", program);
            var process = Process.Start(@"C:\TunnelSurferTemp\ProtocolFile.ahk");
            while (process.HasExited == false)
            {
                Thread.Sleep(100);
            }
        }
        
        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                Thread.Sleep(100);
                if (Path.GetExtension(e.FullPath).ToLower() == ".abf")
                {
                    while (CheckFiles(e.FullPath) == false)
                    {
                        Thread.Sleep(100);
                    }
                    if (FileCreated != null)
                        FileCreated(e.FullPath);
                }
            }
        }

       // FileSystemWatcher watcher;

        private string _Filename = "";
        private string _protocol = "";
        public void RunAxon(string tempFilename, bool lowNoise, int SampleRate, int minutes)
        {
            if (Path.GetFileNameWithoutExtension(tempFilename) != _Filename)
            {
                SetFileFolder(Path.GetFileNameWithoutExtension(tempFilename));
                _Filename = Path.GetFileNameWithoutExtension(tempFilename);
            }
            string protocolFile = @"s:\Research\TunnelSurfer\AxonScripts\export" + SampleRate + "_";
            if (lowNoise)
                protocolFile += "lowNoise_";
            else
                protocolFile += "whole_";

            protocolFile += minutes.ToString() + ".pro";

            if (protocolFile != _protocol)
            {
                SetProtocol(protocolFile);
                _protocol = protocolFile;
            }

           
            fileWatcherTimer.Start();
            fileWatcherTimer.IsEnabled = true;
          //  watcher = new FileSystemWatcher(@"C:\TunnelSurferTemp");
          //  watcher.Created += Watcher_Created;
          //  watcher.NotifyFilter = NotifyFilters.CreationTime ;
          //  watcher.Filter = "*.abf";
         //   watcher.EnableRaisingEvents = true;

            Process.Start(@"C:\TunnelSurferTemp\record.ahk");
        }

        System.Windows.Threading.DispatcherTimer fileWatcherTimer = new System.Windows.Threading.DispatcherTimer();

        void fileWatcherTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                string[] files = Directory.GetFiles(@"C:\TunnelSurferTemp","*.abf");
                if (files != null && files.Length > 0)
                {
                    fileWatcherTimer.IsEnabled = false;

                    foreach (var file in files)
                    {
                        if (CheckFiles(file))
                        {
                            if (FileCreated != null)
                                FileCreated(file);
                        }
                    }

                    fileWatcherTimer.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        public bool CheckFiles(string file)
        {
            return (File.Exists(Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file) + ".rsv") == false);
           
        }

    }
}
