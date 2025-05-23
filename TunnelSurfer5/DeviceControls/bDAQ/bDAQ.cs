using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TunnelVision.DeviceControls.bDAQ
{
    public delegate void ArrayAquiredEvent(Int16[,] dataChunk);
    public delegate void TimedArrayAquiredEvent(double timePerSample, double[,] CalibrationGrid);
    public delegate void bDAQDoneEvent(string process);

    public class UDPServer
    {
        public event ArrayAquiredEvent ArrayAcquired;
        public bool EmergencyStop = false;
        public void Close()
        {
            EmergencyStop = true;
            try
            {
                _Listener.Close();
                Thread.Sleep(100);
                if (messageThread.IsAlive)
                {
                    messageThread.Abort();
                }
            }
            catch
            {
                if (messageThread != null)
                    messageThread.Abort();
            }
        }

        private void MessagePump()
        {
            while (EmergencyStop == false)
            {
                try
                {
                    byte[] receive_byte_array = _Listener.Receive(ref groupEP);
                    Console.WriteLine("Received a broadcast from {0}", groupEP.ToString());

                    short[,] data = new short[receive_byte_array.Length / 2 / numberChannels, numberChannels];
                    Buffer.BlockCopy(receive_byte_array, 0, data, 0, receive_byte_array.Length);
                    if (ArrayAcquired != null)
                    {
                        ArrayAcquired(data);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogError(ex);
                }
            }

            _Listener.Close();
        }

        UdpClient _Listener;
        IPEndPoint groupEP;
        int numberChannels;
        Thread messageThread;

        public bool IsAlive()
        {
            if (messageThread == null)
                return false;
            return messageThread.IsAlive;
        }

        public void RunServer(string address, int numberChannels, int port)
        {
            try
            {
                this.numberChannels = numberChannels;
                if (messageThread != null)
                {
                    if (messageThread.IsAlive)
                        return;
                }
                IPAddress ipAd = IPAddress.Parse(address);
                // use local m/c IP address, and 
                // use the same in the client
                _Listener = new UdpClient(port);
                groupEP = new IPEndPoint(IPAddress.Any, port);
                /* Start Listeneting at the specified port */


                System.Diagnostics.Debug.Print("The server is running at port " + port);
                System.Diagnostics.Debug.Print("The local End point is  :" + groupEP.Address);
                System.Diagnostics.Debug.Print("Waiting for a connection.....");

                messageThread = new Thread(delegate () { MessagePump(); });
                messageThread.Start();

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print("Error..... " + e.StackTrace);
            }
        }
    }


    public class MessageServer
    {
        public event ArrayAquiredEvent ArrayAcquired;
        public bool EmergencyStop = false;

        public Queue<string> LatestMessage = new Queue<string>();

        UTF8Encoding asen = new UTF8Encoding();
        public void Close()
        {
            EmergencyStop = true;
            sock.Disconnect(false);
            Thread.Sleep(100);
            if (messageThread.IsAlive)
            {
                messageThread.Abort();
            }

        }
        public string QueryMessage(string message)
        {

            SendMessage(message);
            while (LatestMessage.Count == 0 && EmergencyStop == false)
                Thread.Sleep(100);

            Thread.Sleep(100);
            if (LatestMessage.Count > 0)
                return LatestMessage.Dequeue();
            else
                return "";
        }

        public void SendMessage(string message)
        {
            var n = message.Length.ToString().PadLeft(3, '0');
            sock.Send(asen.GetBytes(n + message));

        }
        public string GetMessage()
        {
            byte[] b = new byte[3];

            int k = sock.Receive(b);
            System.Diagnostics.Debug.Print("Recieved...");
            string data = asen.GetString(b);
            System.Diagnostics.Debug.Print(data);
            data = data.Trim();
            if (data == "byt")
            {
                //b = new byte[10];
                //k = sock.Receive(b);
                //int numBytes = int.Parse(asen.GetString(b));
                b = new byte[3];
                k = sock.Receive(b);
                int numChannels = int.Parse(asen.GetString(b));
                int channelBlock = 2 * numChannels;
                b = new byte[channelBlock];
                bool isEnd = false;
                int nSteps = 1000;
                Int16[,] values = new Int16[nSteps, numChannels];
                int nRows = 1;
                while (isEnd == false)
                {
                    try
                    {
                        k = sock.Receive(b);

                        //nRows = (int)Math.Floor((double)k / channelBlock);

                        if (k == 3)
                        {
                            data = asen.GetString(b, 0, 3);
                            if (data == "end")
                            {
                                isEnd = true;
                                break;
                            }
                        }


                        Buffer.BlockCopy(b, 0, values, (nRows - 1) * channelBlock, channelBlock);

                        if (nRows % nSteps == 0)
                        {
                            if (ArrayAcquired != null)
                                ArrayAcquired(values);
                        }

                        nRows = (nRows % nSteps + 1);
                        //if (k % channelBlock != 0)
                        //{
                        //    overflow = k - nRows * channelBlock;
                        //}
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError(ex);
                    }
                }
                return "Array";
            }
            else
            {
                k = int.Parse(data);
                b = new byte[k];
                k = sock.Receive(b);
                data = asen.GetString(b);
                return data;
            }
        }
        private void MessagePump()
        {
            sock = _Listener.AcceptSocket();
            System.Diagnostics.Debug.Print("Connection accepted from " + sock.RemoteEndPoint);

            while (EmergencyStop == false)
            {
                try
                {
                    var data = GetMessage();
                    LatestMessage.Enqueue(data);
                    if (data != "")
                    {
                        if (data.Contains("ready"))
                        {
                            SendMessage(data);
                        }
                        //  SendMessage(data);
                        if (data.Trim() == "Done")
                            break;
                        System.Diagnostics.Debug.Print("\nSent Acknowledgement");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogError(ex);
                }
            }
            /* clean up */
            sock.Close();
            _Listener.Stop();
        }

        Socket sock;
        TcpListener _Listener;
        Thread messageThread;

        public bool IsAlive()
        {
            if (messageThread == null)
                return false;
            return messageThread.IsAlive;
        }

        public void RunServer(string address, int port)
        {
            try
            {
                if (messageThread != null)
                {
                    if (messageThread.IsAlive)
                        return;
                }
                IPAddress ipAd = IPAddress.Parse(address);
                // use local m/c IP address, and 
                // use the same in the client
                _Listener = new TcpListener(ipAd, port);
                /* Start Listeneting at the specified port */
                _Listener.Start();

                System.Diagnostics.Debug.Print("The server is running at port " + port);
                System.Diagnostics.Debug.Print("The local End point is  :" + _Listener.LocalEndpoint);
                System.Diagnostics.Debug.Print("Waiting for a connection.....");

                messageThread = new Thread(delegate () { MessagePump(); });
                messageThread.Start();

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print("Error..... " + e.StackTrace);
            }
        }
    }

    public class MessageClient
    {
        public event ArrayAquiredEvent ArrayAcquired;
        public bool EmergencyStop = false;
        TcpClient client = new TcpClient();
        NetworkStream clientStream;
        public Queue<string> LatestMessage = new Queue<string>();

        UTF8Encoding asen = new UTF8Encoding();
        ~MessageClient()
        {
            System.Diagnostics.Debug.Print("Disposed");

        }

        public void Close()
        {
            EmergencyStop = true;
            client.Close();
            Thread.Sleep(100);
            if (messageThread.IsAlive)
            {
                messageThread.Abort();
            }
        }

        public string QueryMessage(string message)
        {

            SendMessage(message);
            return GetMessage();
        }

        public void SendMessage(string message)
        {

            var n = message.Length.ToString().PadLeft(3, '0');
            //  sock.Send(asen.GetBytes(n + message));
            byte[] ba = asen.GetBytes(n + message);
            Console.WriteLine("Transmitting.....");
            if (clientStream != null)
                clientStream.Write(ba, 0, ba.Length);

        }
        public string GetMessage()
        {
            while (LatestMessage.Count == 0 && EmergencyStop == false)
                Thread.Sleep(100);

            Thread.Sleep(100);
            if (LatestMessage.Count > 0)
                return LatestMessage.Dequeue();
            else
                return "";
        }
        private string _GetMessage()
        {
            byte[] b = new byte[3];

            // clientStream.ReadTimeout= 3000;
            int k = clientStream.Read(b, 0, 3);
            if (k == 0)
                return "";
            System.Diagnostics.Debug.Print("Recieved...");
            string data = asen.GetString(b);
            System.Diagnostics.Debug.Print(data);
            data = data.Trim();
            if (data == "byt")
            {
                //b = new byte[10];
                //k = sock.Receive(b);
                //int numBytes = int.Parse(asen.GetString(b));
                b = new byte[3];
                k = clientStream.Read(b, 0, b.Length);
                int numChannels = int.Parse(asen.GetString(b));
                int channelBlock = 2 * numChannels;
                b = new byte[channelBlock];
                bool isEnd = false;
                int nSteps = 1000;
                Int16[,] values = new Int16[nSteps, numChannels];
                int nRows = 1;
                while (isEnd == false)
                {
                    try
                    {
                        k = clientStream.Read(b, 0, b.Length);

                        //nRows = (int)Math.Floor((double)k / channelBlock);

                        if (k == 3)
                        {
                            data = asen.GetString(b, 0, 3);
                            if (data == "end")
                            {
                                isEnd = true;
                                break;
                            }
                        }


                        Buffer.BlockCopy(b, 0, values, (nRows - 1) * channelBlock, channelBlock);

                        if (nRows % nSteps == 0)
                        {
                            if (ArrayAcquired != null)
                                ArrayAcquired(values);
                        }

                        nRows = (nRows % nSteps + 1);
                        //if (k % channelBlock != 0)
                        //{
                        //    overflow = k - nRows * channelBlock;
                        //}
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError(ex);
                    }
                }
                return "Array";
            }
            else
            {
                k = int.Parse(data);
                b = new byte[k];
                k = clientStream.Read(b, 0, b.Length);
                data = asen.GetString(b);
                return data;
            }
        }
        private void MessagePump()
        {
            while (EmergencyStop == false)
            {
                try
                {
                    var data = _GetMessage();
                    LatestMessage.Enqueue(data);
                    if (data != "")
                    {
                        System.Diagnostics.Debug.Print(data);
                        if (data.Trim() == "Done")
                            break;
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogError(ex);
                    EmergencyStop = true;
                }
            }
            /* clean up */
            client.Close();
        }
        Thread messageThread;

        public bool IsAlive()
        {
            if (messageThread == null)
                return false;
            return messageThread.IsAlive;
        }

        public void RunClient(string address, int port)
        {
            try
            {
                if (messageThread != null)
                {
                    if (messageThread.IsAlive)
                        return;
                }

                // use local m/c IP address, and 
                // use the same in the client
                client.Connect(address, port);
                clientStream = client.GetStream();
                /* Start Listeneting at the specified port */
                string check = _GetMessage();
                System.Diagnostics.Debug.Print(check);

                System.Diagnostics.Debug.Print("The client is running at port " + port);

                messageThread = new Thread(delegate () { MessagePump(); });
                messageThread.Start();

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print("Error..... " + e.StackTrace);

                throw new Exception("No Server");
            }
        }

    }

    public class bDAQ
    {
        private static MessageClient CommandServer = null;
        private static UDPServer DataServer;

        public bDAQ()
        {

            StartCommand();
        }

        private string ServerAddress = "127.0.0.1::10000";
        public void SetServer(string serverAddress)
        {
            ServerAddress = serverAddress;
        }
        public void BDAQ_KillServer()
        {
            try
            {
                Close();
                Thread.Sleep(10);
                IsCalibrated = true;
                if (CommandServer == null || CommandServer.IsAlive() == false)
                {
                    CommandServer = new MessageClient();
                    CommandServer.RunClient("127.0.0.1", 10000);
                }
                CommandServer.SendMessage("SuperDone");
                Thread.Sleep(3000);
                exeProcess.Close();
            }
            catch { }
        }

        public void Close()
        {
            try
            {
                CommandServer.SendMessage("Done");
                Thread.Sleep(100);
                CommandServer.EmergencyStop = true;

                // DataServer.SendMessage("Done");
                Thread.Sleep(100);
                DataServer.EmergencyStop = true;

                Thread.Sleep(100);
                CommandServer.Close();
                DataServer.Close();
            }
            catch { }
        }

        static Process exeProcess = null;
        private bool IsRunning()
        {
            if (exeProcess == null)
                return false;

            try
            {
                Process.GetProcessById(exeProcess.Id);
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }
        private static void Run_bDAQ()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = @"python.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory + @"Python\fourclamp_ASU_May2016\";
            startInfo.Arguments = AppDomain.CurrentDomain.BaseDirectory + @"Python\fourclamp_ASU_May2016\BDAQ_server.py ";

            exeProcess = Process.Start(startInfo);
        }

        bool IsCalibrated = false;

        private void StartCommand()
        {
            //if (IsRunning() == false)
            //{
            //  //  Run_bDAQ();
            //}
            if (CommandServer == null || CommandServer.IsAlive() == false)
            {
                CommandServer = new MessageClient();
                try
                {
                    string[] parts = ServerAddress.Split(':');
                    CommandServer.RunClient(parts[0], int.Parse(parts[2]));
                }
                catch
                {
                    try
                    {
                        string[] parts = ServerAddress.Split(':');
                        CommandServer.RunClient(parts[0], int.Parse(parts[1]));
                    }
                    catch (Exception ex)
                    {
                        System.Windows.Forms.MessageBox.Show("Unable to connect to python server.  Please start server or shut it down and start it again.");
                        throw ex;
                    }

                }
            }
            RunCalibration();
        }

        public void ForceCalibration()
        {
            IsCalibrated = false;
            StartCommand();

          //  OpenUDP();

         //   RunCalibration();
        }

        private void RunCalibration()
        {
            if (IsCalibrated == false)
            {

                var ret = System.Windows.Forms.MessageBox.Show("Please remove all connections to the box, and then click Yes", "Calibration", System.Windows.Forms.MessageBoxButtons.YesNo);
                if (ret == System.Windows.Forms.DialogResult.Yes)
                {
                    CommandServer.QueryMessage("NullI");

                    ret = System.Windows.Forms.MessageBox.Show("Please connect resister network and then click Yes", "Calibration", System.Windows.Forms.MessageBoxButtons.YesNo);
                    if (ret == System.Windows.Forms.DialogResult.Yes)
                    {
                        CommandServer.QueryMessage("NullV");

                        SpinDown();
                        System.Windows.Forms.MessageBox.Show("Device is now ready for use.  Please connect chip and click Yes", "Calibration", System.Windows.Forms.MessageBoxButtons.YesNo);
                        SpinUp();
                    }
                }
                ChannelCalibration();
                IsCalibrated = true;
            }


        }

        public event TimedArrayAquiredEvent ArrayAcquired;


        List<short[,]> UDPArrays = new List<short[,]>();
        object UPDLock = new object();
        int chunkCount = 0;
        public Tuple<int, short[,]> GetAcquired()
        {
            lock (UPDLock)
            {
                if (UDPArrays.Count > 0)
                {
                    short[,] data = UDPArrays[UDPArrays.Count - 1];
                    UDPArrays.Clear();
                    return new Tuple<int, short[,]>(chunkCount, data);
                }
            }
            return null;
        }
        private void DataServer_ArrayAcquired(short[,] dataChunk)
        {

            if (chunkCount == 0)
            {
                if (ArrayAcquired != null)
                {
                    ArrayAcquired(SkipPoints / SampleRate, ChannelCalibrationGrid);
                }
            }


            lock (UPDLock)
            {
                UDPArrays.Add(dataChunk);
                chunkCount += dataChunk.GetLength(0);
            }

        }

        public event bDAQDoneEvent bDAQDone;
        int NumberChannels = 4;

        double[,] ChannelCalibrationGrid;
        double SampleRate;
        int SkipPoints = 1000;
        private void ChannelCalibration()
        {
            string Channels = CommandServer.QueryMessage("Calibration");
            NumberChannels = int.Parse(Channels);
            ChannelCalibrationGrid = new double[2, NumberChannels];
            for (int i = 0; i < NumberChannels; i++)
            {
                string temp = CommandServer.GetMessage();
                ChannelCalibrationGrid[0, i] = double.Parse(temp);
                temp = CommandServer.GetMessage();
                ChannelCalibrationGrid[1, i] = double.Parse(temp);
            }
            string temp2 = CommandServer.GetMessage();
            SampleRate = double.Parse(temp2);

            CommandServer.GetMessage();
        }

        public enum SweepType { SawIon, SawTunnel };
        public void RunSweep(string filename, SweepType sweep, double amplitude)
        {
            StartCommand();

            OpenUDP();
            var messageThread = new Thread(delegate ()
            {
                try
                {
                    if (CommandServer.IsAlive() == false)
                    {
                        StartCommand();
                    }

                    chunkCount = 0;
                    SetVoltages(0, 0, 0, 0, 0, 0);
                    RunSweep(sweep, amplitude);
                    SaveTempBinFile(@"C:\temp\temp.bDAQ");
                    CloseClient();
                }
                catch { }
                if (bDAQDone != null)
                    bDAQDone("sweep");


            });
            messageThread.Start();
        }

        public void RunConstant(double runTime, double VBias1, double VBias2, double VClamp0, double VClamp1, double VClamp2, double VClamp3)
        {
            StartCommand();

            OpenUDP();
            var messageThread = new Thread(delegate ()
            {
                try
                {
                    if (CommandServer.IsAlive() == false)
                    {
                        StartCommand();
                    }
                    ChannelNames(cNames);
                    SetVoltages(VBias1, VBias2, VClamp0, VClamp1, VClamp2, VClamp3);
                    chunkCount = 0;
                    RunConstant(SkipPoints, SampleRate, runTime);
                    SaveTempBinFile(@"C:\temp\temp.bDAQ");
                    Thread.Sleep(1000);
                    try
                    {
                        CloseClient();
                    }
                    catch { }
                    if (bDAQDone != null)
                        bDAQDone("constant");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                }
            });
            messageThread.Start();
        }

        public void RunZero()
        {
            StartCommand();

            if (CommandServer.IsAlive() == false)
            {
                RunCalibration();
            }
            SpinDown();
            CloseClient();
        }

        public void RunVoltage()
        {
            StartCommand();

            if (CommandServer.IsAlive() == false)
            {
                RunCalibration();
            }
            SpinUp();
            CloseClient();
        }

        public void SetChannelNames(string[] names)
        {
            cNames = names;
        }

        #region Network codes
        private void SpinUp()
        {
            CommandServer.QueryMessage("SpinUp");
        }

        private void SpinDown()
        {
            CommandServer.QueryMessage("SpinDown");
        }

        private void OpenUDP()
        {
            DataServer = new UDPServer();
            DataServer.RunServer("127.0.0.1", NumberChannels, 10001);//restart each time to allow the number of channels to be checked.
            DataServer.ArrayAcquired += DataServer_ArrayAcquired;
        }
        private void CloseClient()
        {
            try
            {
                DataServer.Close();
                CommandServer.SendMessage("Done");
            }
            catch
            {
                CommandServer.SendMessage("Done");
            }
        }
        private void SetVoltages(double VBias1, double VBias2, double VClamp0, double VClamp1, double VClamp2, double VClamp3)
        {
            CommandServer.SendMessage("SetVoltages");

            CommandServer.SendMessage(VBias1.ToString());
            CommandServer.SendMessage(VBias2.ToString());
            CommandServer.SendMessage(VClamp0.ToString());
            CommandServer.SendMessage(VClamp1.ToString());
            CommandServer.SendMessage(VClamp2.ToString());
            CommandServer.QueryMessage(VClamp3.ToString());
        }

        private string[] cNames = new string[4];
        private void ChannelNames(string[] Names)
        {
            CommandServer.SendMessage("ChannelNames");
            for (int i = 0; i < Names.Length - 1; i++)
                CommandServer.SendMessage(Names[i]);
            string ret = CommandServer.QueryMessage(Names[Names.Length - 1]);
            System.Diagnostics.Debug.Print(ret);
        }

        private void RunConstant(int skipPoints, double sampleRate, double runTimeSec)
        {
            CommandServer.SendMessage("Constant");
            CommandServer.SendMessage(SkipPoints.ToString());
            CommandServer.SendMessage(SampleRate.ToString());
            try
            {
                string ret = "";
                while (ret.Contains("Trace:Done") == false)
                    ret = CommandServer.QueryMessage(runTimeSec.ToString());
                System.Diagnostics.Debug.Print(ret);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
            }

        }

        private void RunSweep(SweepType sweep, double amplitude)
        {
            if (sweep == SweepType.SawIon)
            {
                CommandServer.SendMessage("Sweep");
                CommandServer.SendMessage("SawIon");
                CommandServer.SendMessage("0.08");
                string reply = CommandServer.QueryMessage(amplitude.ToString());
                System.Diagnostics.Debug.Print(reply);
            }
            else
            {
                if (sweep == SweepType.SawTunnel)
                {
                    CommandServer.SendMessage("Sweep");
                    CommandServer.SendMessage("SawTunnel");
                    CommandServer.SendMessage("0.08");
                    string reply = CommandServer.QueryMessage(amplitude.ToString());
                    System.Diagnostics.Debug.Print(reply);
                }
                else
                {
                    CommandServer.QueryMessage("SweepSquare");
                }
            }
        }

        private void SaveTempFile(string filename)
        {
            SaveTempFile(Path.GetDirectoryName(filename), Path.GetFileName(filename));
        }

        private void SaveTempFile(string folder, string filename)
        {
            CommandServer.SendMessage("SaveTempFile");
            CommandServer.SendMessage(folder);
            CommandServer.QueryMessage(filename);
        }

        private void SaveTempBinFile(string filename)
        {
            SaveTempBinFile(Path.GetDirectoryName(filename), Path.GetFileName(filename));
        }

        private void SaveTempBinFile(string folder, string filename)
        {
            CommandServer.SendMessage("SaveTempBinFile");
            CommandServer.SendMessage(folder);
            string ret = "";
            while (ret.Contains("SaveTemp:Done") == false)
                ret = CommandServer.QueryMessage(Path.GetFileNameWithoutExtension(filename));
            System.Diagnostics.Debug.Print(ret);
        }

        private void SaveFile(string filename)
        {
            SaveFile(Path.GetDirectoryName(filename), Path.GetFileName(filename));
        }
        private void SaveFile(string folder, string filename)
        {
            CommandServer.SendMessage("Save");
            CommandServer.SendMessage(folder);
            CommandServer.QueryMessage(filename);
        }
        #endregion
    }
}
