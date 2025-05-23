using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.DataModel.Fileserver
{
    public class FileDataTrace
    {
        public int Id { get; set; }
        public int OwnerID { get; set; }
        public virtual List<FileTrace> Traces { get; set; }
        public List<string> TraceNames { get; set; }
        public Dictionary<string, string> Header { get; set; }

        public static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public static List<string> AvailableTraces(Trace trace)
        {
            try
            {
                //using (FileContext context = new FileContext())
                {
                    //  var xx = (from x in context.FileDataTraces where x.OwnerID == trace.Id select x).FirstOrDefault();
                    // if (xx == null || xx.TraceNames == null)
                    {
                        if (trace.Folder == null)
                            return null;
                        string DataDir =App.ConvertFilePaths( trace.Folder);
                        //if (Directory.Exists(DataDir) == false)
                        //{
                        //    DataDir = DataDir.ToLower().Replace(@"s:\research\tunnelsurfer", App.DataDirectory);
                        //}
                        string ProcessedDir =App.ConvertFilePaths( trace.ProcessedFile );
                        //if (Directory.Exists(ProcessedDir) == false)
                        //{
                        //    ProcessedDir = ProcessedDir.ToLower().Replace(@"s:\research\tunnelsurfer", App.DataDirectory);
                        //}

                        string[] Files = Directory.GetFiles(DataDir,"*.json");
                        Files = (from x in Files where x.ToLower().Contains(ProcessedDir.ToLower()) == true select x).ToArray();
                        //var traces = (from x in Files
                        //              where (x.ToLower().Contains("e2") == false && x.ToLower().Contains("h2") == false
                        //                  && x.ToLower().Contains("levels") == false && x.ToLower().Contains("spec") == false
                        //                  && x.ToLower().Contains("times") == false && x.ToLower().Contains("header") == false && x.ToLower().Contains(".png") == false && x.ToLower().Contains(".bdaq") == false)
                        //              select x.ToLower().Replace(ProcessedDir.ToLower() + "_", "").Replace(".bin", "").Replace(".abf", "").Replace(".bbf", "").Replace(".bsn", "")).ToList();

                     //   var traces = (from x in Files where (x.Contains(trace.Junction.JunctionName) || x.Contains("Raw") || x.Contains("Smooth")) select x).ToList();
                     var    traces = (from x in Files select x).ToList();
                        if (traces.Count == 0)
                        {
                             traces = (from x in Files   select x).ToList();

                            //Files = Directory.GetFiles(Path.GetDirectoryName(ProcessedDir));
                            //Files = (from x in Files where x.ToLower().Contains(ProcessedDir.ToLower()) == true select x).ToArray();
                            //traces.AddRange((from x in Files
                            //                 where (x.ToLower().Contains("e2") == false && x.ToLower().Contains("h2") == false
                            //                     && x.ToLower().Contains("levels") == false && x.ToLower().Contains("spec") == false
                            //                     && x.ToLower().Contains("times") == false && x.ToLower().Contains(".txt") == false && x.ToLower().Contains(".png") == false && x.ToLower().Contains(".bdaq") == false)
                            //                 select x.ToLower().Replace(ProcessedDir.ToLower() + "_", "").Replace(".bin", "").Replace(".abf", "").Replace(".bbf", "")));
                        }

                         //traces = (from x in traces
                         //              where (x.ToLower().Contains("peakmask") == false && x.ToLower().Contains("shortfiltered") == false
                         //                 && x.ToLower().Contains("shortraw") == false  )
                         //              select x.ToLower()).ToList();

                        var tTraces = new List<string>();
                        foreach (string s in traces)
                            tTraces.Add(UppercaseFirst(s.Replace(ProcessedDir + "_" ,"").Replace(ProcessedDir , "").Replace(".json","")));

                       

                        string[] Ordered = new string[] { "Voltage", "TV", "Cusum", "Wiener", "Background_Removed", "Smooth", "Raw" };
                        foreach (string s in Ordered)
                        {
                            if (tTraces.Contains(s))
                            {
                                tTraces.Remove(s);
                                tTraces.Insert(0, s);
                            }
                        }

                        if (tTraces.Contains("Peakmask") == true)
                            tTraces.Remove("Peakmask");

                        return tTraces;
                    }


                    // return xx.TraceNames;
                }
            }
            catch
            {
                return null;

            }
        }

        public static Dictionary<string, string> ReadHeader(string filename)
        {
            string text = System.IO.File.ReadAllText(App.ConvertFilePaths( filename));
            string[] lines = text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> Header = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                try
                {
                    string[] parts = line.Split('=');
                    Header.Add(parts[0].Trim().ToLower(), parts[1].Trim());
                }
                catch { }
            }
            return Header;

        }


    }
}
