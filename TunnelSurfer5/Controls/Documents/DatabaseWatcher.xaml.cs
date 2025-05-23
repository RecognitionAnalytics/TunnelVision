using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TunnelVision.Controls.Documents
{
    /// <summary>
    /// Interaction logic for DatabaseWatcher.xaml
    /// </summary>
    public partial class DatabaseWatcher : UserControl
    {
        public DatabaseWatcher()
        {
            InitializeComponent();
        }

        static Random random = new Random();

        public static IEnumerable<T> RandomPermutation<T>(IEnumerable<T> sequence)
        {
            T[] retArray = sequence.ToArray();


            for (int i = 0; i < retArray.Length - 1; i += 1)
            {
                int swapIndex = random.Next(i, retArray.Length);
                if (swapIndex != i)
                {
                    T temp = retArray[i];
                    retArray[i] = retArray[swapIndex];
                    retArray[swapIndex] = temp;
                }
            }

            return retArray;
        }

        //get python path from environtment variable
        private string GetPythonPath()
        {
            IDictionary environmentVariables = Environment.GetEnvironmentVariables();
            string pathVariable = environmentVariables["Path"] as string;
            if (pathVariable != null)
            {
                string[] allPaths = pathVariable.Split(';');
                foreach (var path in allPaths)
                {
                    string pythonPathFromEnv = path + "\\python.exe";
                    if (System.IO.File.Exists(pythonPathFromEnv))
                        return pythonPathFromEnv;
                }
            }
            return "";
        }

        private string PythonStep(string exe, string argCmd, DataModel.DataTrace trace)
        {

            string pyDir = AppDomain.CurrentDomain.BaseDirectory + "python";


            string processedfile = trace.ProcessedFile;

            if (processedfile.Contains("\\_" + trace.Id) == false)
                processedfile = processedfile + "\\_" + trace.Id;


            if (trace.Filename.ToLower().Contains("bdaq") == false)//bdaq files have 3-4 traces and require the junction label
            {
                // if (File.Exists(processedfile + "_header.txt") == true)
                //    return null;
                argCmd = argCmd.ToLower()
                   .Replace("%pydir%", pyDir)
                   .Replace("%filename%", "\"" + trace.Filename + "\"")
                   .Replace("%outfilename%", "\"" + processedfile + "\"")
                   .Replace("%controlfile%", "\"" + trace.ControlFile + "\"")
                   .Replace("%concentration%", "\"" + trace.Concentration_mM.ToString() + "\"")
                   .Replace("%numberpores%", "\"" + trace.NumberPores.ToString() + "\"");
            }
            else
            {

                //if (File.Exists(processedfile + "_" + trace.Junction.JunctionName + "_header.txt") == true)
                //     return null;

                string j_Name = trace.Junction.JunctionName;
                string[] parts = j_Name.Split('_');
                j_Name = parts[1] + "_" + parts[0];
                // processedfile = processedfile + "_" + trace.Junction.JunctionName;
                argCmd = argCmd.ToLower()
                    .Replace("%pydir%", pyDir)
                    .Replace("%filename%", "\"" + trace.Filename + "|" + j_Name + "\"")  ///extra information about which junction is desired 
                    .Replace("%outfilename%", "\"" + processedfile + "_" + j_Name + "\"")
                    .Replace("%controlfile%", "\"" + trace.ControlFile + "\"")
                    .Replace("%concentration%", "\"" + trace.Concentration_mM.ToString() + "\"")
                    .Replace("%numberpores%", "\"" + trace.NumberPores.ToString() + "\"");
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = exe;
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(exe);
            startInfo.Arguments = argCmd;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            System.Diagnostics.Debug.Print(argCmd);
            System.Diagnostics.Debug.Print(startInfo.ToString());
            Process exeProcess = Process.Start(startInfo);
            //  Debug.Print(exeProcess.ProcessName);

            using (StreamReader readerSTD = exeProcess.StandardOutput)
            {
                using (StreamReader readerError = exeProcess.StandardError)
                {
                    string result2 = "";

                    result2 = readerSTD.ReadToEnd();
                    System.Diagnostics.Debug.Print(result2);

                    string result = readerError.ReadToEnd();
                    System.Diagnostics.Debug.Print(result);
                }
            }

            return processedfile;
        }

        //string[] binFiles = new string[] { "_Smooth", "_Wiener", "_Background_Removed", "_cusum", "_TV", "_ShortFiltered", "_ShortRaw" };
        //foreach (string file in binFiles)
        //{
        //    try
        //    {
        //        if (File.Exists(processedfile + file + ".bin") == true)
        //        {
        //            if (trace.Filename.ToLower().Contains("bdaq"))
        //            {
        //                DataModel.Fileserver.FileTrace.ConvertBin(processedfile + file + ".bin", processedfile + file + ".bbf", file);
        //                File.Delete(processedfile + file + ".bin");
        //            }
        //            else
        //            {
        //                DataModel.Fileserver.FileTrace.ConvertBin(processedfile + "_raw.bbf", processedfile + file + ".bin", processedfile + file + ".bbf", file);
        //                File.Delete(processedfile + file + ".bin");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.Print("");
        //    }
        //}
        //try
        //{
        //    files = Directory.GetFiles(processedfile, "*_ionic.bsn");
        //    foreach (var j in files)
        //    {
        //        try
        //        {
        //            DataModel.Fileserver.FileTrace.ConvertBSN_FLAC(
        //                processedfile,
        //                j,
        //                System.IO.Path.GetDirectoryName(j) + "\\" + System.IO.Path.GetFileNameWithoutExtension(j) + ".bbf",
        //              System.IO.Path.GetFileNameWithoutExtension(j));
        //            File.Delete(j);
        //        }
        //        catch (Exception ex)
        //        {
        //            System.Diagnostics.Debug.Print(ex.Message);
        //        }
        //    }
        //}
        //catch (Exception ex)
        //{
        //    System.Diagnostics.Debug.Print(ex.Message);
        //}

        //files = Directory.GetFiles(System.IO.Path.GetDirectoryName(trace.ProcessedFile), System.IO.Path.GetFileNameWithoutExtension(trace.ProcessedFile) + "*.bsn");
        //foreach (var j in files)
        //{
        //    try
        //    {
        //        DataModel.Fileserver.FileTrace.ConvertBSN_FLAC(
        //            trace.ProcessedFile,
        //            j,
        //           System.IO.Path.GetDirectoryName(trace.ProcessedFile) + "\\" + System.IO.Path.GetFileNameWithoutExtension(j) + ".bbf",
        //            trace.Junction.JunctionName + "_Full");
        //        File.Delete(j);
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.Print(ex.Message);
        //    }
        //}

        private Dictionary<string, string> ConvertPythonFiles(string processedfile, DataModel.DataTrace trace)
        {
            Dictionary<string, string> Header = null;
            string[] files;
            try
            {
                files = Directory.GetFiles(System.IO.Path.GetDirectoryName(processedfile), "*header.txt");
                Header = DataModel.Fileserver.FileDataTrace.ReadHeader(files[0]);
                Header.Add("TraceName", trace.TraceName);
                Header.Add("Junction", trace.Junction.JunctionName);
                Header.Add("Chip", trace.Chip.ChipName);
                Header.Add("Batch", trace.Chip.Batch.BatchName);
                Header.Add("Experiment", trace.Chip.Experiment.ExperimentName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
            }

            files = Directory.GetFiles(System.IO.Path.GetDirectoryName(processedfile), "*.bsn");
            foreach (var j in files)
            {
                try
                {
                    DataModel.Fileserver.FileTrace.ConvertBSN_FLAC(
                        trace.Filename,
                        j,
                        System.IO.Path.GetDirectoryName(j) + "\\" + System.IO.Path.GetFileNameWithoutExtension(j),
                        System.IO.Path.GetFileNameWithoutExtension(j).Replace("_" + trace.Id + "_", ""),
                        Header);
                    Thread.Sleep(500);
                    File.Delete(j);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                }
            }


            return Header;
        }

        private void DoProcess(string exe, string argCmd, DataModel.DataTrace NewTrace)
        {
            using (DataModel.LabContext context = new DataModel.LabContext())
            {
                try
                {
                    int traceID = NewTrace.Id;
                    var trace = (from x in context.Traces where (x.Id == traceID) select x).ToList()[0];
                    trace.DateProcessed = DateTime.Now;
                    context.SaveChanges();
                    var processedFile = PythonStep(exe, argCmd, trace);
                    if (processedFile != null)
                    {
                        //trace keeps timing out??
                        trace = (from x in context.Traces where (x.Id == traceID) select x).ToList()[0];
                        System.Diagnostics.Debug.Print(" ");
                        var Header = ConvertPythonFiles(processedFile, trace);


                        try
                        {
                            var tTrace = (from x in context.Traces where x.Id == traceID select x).First();
                            tTrace.Folder = System.IO.Path.GetDirectoryName(processedFile);
                            tTrace.ProcessedFile = processedFile;
                            tTrace.ReadHeader(Header);
                            context.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.Print(ex.Message);
                        }
                    }
                }
                catch { }
            }

        }

        private void RunPython(string exe, string argCmd, List<DataModel.DataTrace> randomNews)
        {
            // var trace = randomNews[0];
            if (true)
            {
                Parallel.ForEach(randomNews, new ParallelOptions { MaxDegreeOfParallelism = 4 }, NewTrace =>
               {
                   try
                   {
                       DoProcess(exe, argCmd, NewTrace);
                   }
                   catch { }
               }

                );

            }
            else
            {
                foreach (var NewTrace in randomNews)
                {
                    try
                    {
                        DoProcess(exe, argCmd, NewTrace);
                    }
                    catch { }
                }
            }
        }

        private void PrepRun()
        {
            List<DataModel.DataTrace> randomNews = null;
            using (DataModel.LabContext context = new DataModel.LabContext())
            {

                // var news = (from x in context.Traces where (x.Filename.ToLower().Contains("bdaq") && x.DateProcessed.Year < 2016) orderby x.DateAcquired descending select x).ToList();
                // var news = (from x in context.Traces where (x.Filename.ToLower().Contains("bdaq")) orderby x.DateAcquired descending select x).ToList();
                var news = (from x in context.Traces where ( x.DateProcessed.Year < 2016) orderby x.DateAcquired descending select x).ToList();
                // randomNews = DatabaseWatcher.RandomPermutation<DataModel.DataTrace>(news).ToList();

                randomNews = new List<DataModel.DataTrace>();
                for (int i = 0; i < 5; i++)
                {
                    randomNews.Add(news[i]);
                }

                List<DataModel.DataTrace> sortDataFiles = new List<DataModel.DataTrace>();
                sortDataFiles.AddRange((from x in randomNews where x.IsControl == true select x));
                sortDataFiles.AddRange((from x in randomNews where (x.Analyte.ToLower() == "rinse" || x.TraceName.ToLower() == "rinse") select x));
                sortDataFiles.AddRange((from x in randomNews where (x.Analyte.ToLower() == "control" || x.TraceName.ToLower() == "control") select x));
                sortDataFiles.AddRange((from x in randomNews where (x.Analyte.ToLower() == "water" || x.TraceName.ToLower() == "water") select x));

                foreach (var n in sortDataFiles)
                {
                    randomNews.Remove(n);
                }

                string cmd = tbtraceExe.Text;

                string exe = cmd.Split(' ')[0];
                List<string> args = new List<string>(cmd.Split(' '));
                args.RemoveAt(0);
                string argCmd = string.Join(" ", args);
                if (cmd.ToLower().Contains("python"))
                {
                    exe = GetPythonPath();
                }
                else
                {
                    exe = cmd.Split(' ')[0];
                }
                if (sortDataFiles.Count > 0)
                    RunPython(exe, argCmd, sortDataFiles);
                RunPython(exe, argCmd, randomNews);
            }
        }

        System.Windows.Threading.DispatcherTimer WatchTimer = null;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (WatchTimer == null)
                WatchTimer = new System.Windows.Threading.DispatcherTimer();
            WatchTimer.Tick += WatchTimer_Tick;
            WatchTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            WatchTimer.IsEnabled = true;
            WatchTimer.Start();
        }

        private void WatchTimer_Tick(object sender, EventArgs e)
        {
            WatchTimer.IsEnabled = false;
            PrepRun();
            WatchTimer.IsEnabled = true;
        }
    }
}

