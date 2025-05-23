using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace TunnelVision.DataModel
{
    public class ExistingParse
    {
        #region Utilities
        private static int FormatNumber(string number)
        {
            if (number.ToLower() == "g")
                return 0;
            number = number.ToLower().Replace("p", "").Replace("n", "-");
            return int.Parse(string.Join(string.Empty, Regex.Matches(number, @"-?\d+").OfType<Match>().Select(m => m.Value)));
        }

        private static DateTime StripTime(Dictionary<string, string> Header)
        {
            try
            {
                string date = Header["fileDate"];
                string[] tParts = Header["startTime"].Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                DateTime dt = new DateTime(int.Parse(date.Substring(0, 4)), int.Parse(date.Substring(4, 2)), int.Parse(date.Substring(6, 2)),
                    int.Parse(tParts[0]), int.Parse(tParts[1]), int.Parse(tParts[2]));
                return dt;
            }
            catch
            {
                return DateTime.Now;
            }
        }

        private static double StripUnit(string item)
        {
            return double.Parse(item.Trim().Split(' ')[0]);
        }


        public static double ConvertVoltageToDouble(string voltage)
        {

            if (voltage.ToLower() == "float")
                return -1000;
            if (voltage.ToLower() == "none")
                return -2000;
            if (voltage.ToLower() == "sweep")
                return -3000;

            double value = 0;
            double.TryParse(voltage, out value);
            return value;
        }

        public static string ConvertVoltageFromDouble(double voltage)
        {

            if (voltage == -1000)
                return "float";
            if (voltage == -2000)
                return ("none");

            if (voltage == -3000)
                return ("sweep");

            return voltage.ToString();
        }

        public static string FileControl(string condition, Junction junction)
        {
            string controlFile = "";


            try
            {
                controlFile = (from x in junction.Traces where (x.Condition == condition && x.IsControl == true) select (x.ProcessedFile).ToString()).First();
            }
            catch
            {
                try
                {
                    controlFile = (from x in junction.Traces where x.IsControl == true select (x.ProcessedFile).ToString()).First();
                }
                catch
                {

                }
            }
            return controlFile;
        }

        public static string ConditionString(DataModel.Trace trace)
        {
            double top = 0;

            if (trace.TopReference_mV != -1000 && trace.TopReference_mV != -2000 && trace.TopReference_mV != -3000)
                top = trace.TopReference_mV;

            double bottom = 0;
            if (trace.BottomReference_mV != -1000 && trace.BottomReference_mV != -2000 && trace.BottomReference_mV != -3000)
                bottom = trace.BottomReference_mV;

            double tun = 0;
            if (trace.Tunnel_mV_Top != -1000 && trace.Tunnel_mV_Top != -2000 && trace.Tunnel_mV_Top != -3000)
                tun = trace.Tunnel_mV_Top;

            double bot = 0;
            if (trace.Tunnel_mV_Bottom != -1000 && trace.Tunnel_mV_Bottom != -2000 && trace.Tunnel_mV_Bottom != -3000)
                bot = trace.Tunnel_mV_Bottom;

            string con = "";
            if (trace.Concentration_mM < .001)
                con = Math.Round(trace.Concentration_mM * 1000d * 1000d, 0).ToString() + "nM";
            else
            {
                if (trace.Concentration_mM < 1)
                    con = Math.Round(trace.Concentration_mM * 1000d, 0).ToString() + "uM";
                else
                    con = Math.Round(trace.Concentration_mM, 0).ToString() + "mM";
            }
            return "IO_" + (bottom - top).ToString().PadLeft(4, '0') + "_Tun_" + (tun - bot).ToString().PadLeft(4, '0');
            //"Top_" + ExistingParse.ConvertVoltageFromDouble(trace.TopReference_mV).PadLeft(4, ' ')
            //+ "_Btm_" + ExistingParse.ConvertVoltageFromDouble(trace.BottomReference_mV).PadLeft(4, ' ')
            //+ "_Tun_" + ExistingParse.ConvertVoltageFromDouble(trace.Tunnel_mV).PadLeft(4, ' ');
        }
        #endregion

        #region Path_Parsers
        public static Dictionary<string, string> ParseYananFilenames(string filename)
        {
            Dictionary<string, string> ParsedParts = new Dictionary<string, string>();
            ParsedParts.Add("file", filename);
            string file = Path.GetFileNameWithoutExtension(filename);
            string dir = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(filename));

            List<string> sParts = new List<string>(); sParts.AddRange(file.Split('_'));

            var fileNumber = "0000";

            if (sParts[sParts.Count - 1].ToLower() == "g")
                sParts.RemoveAt(sParts.Count - 1);
            if (sParts[sParts.Count - 1].All(Char.IsDigit))
            {
                fileNumber = sParts[sParts.Count - 1];
                sParts.RemoveAt(sParts.Count - 1);
            }

            try
            {
                string top = (from x in sParts where x.ToLower().Contains("top") select x).First();
                sParts.Remove(top);
                top = FormatNumber(top.ToLower().Replace("top", "").Replace("mv", "")).ToString();
                ParsedParts.Add("TopRef", top);
            }
            catch
            {
                for (int i = 0; i < sParts.Count; i++)
                {
                    if (sParts[i].ToLower().Contains("ref") == true)
                    {
                        string top = sParts[i + 1];
                        sParts.Remove(sParts[i]);
                        sParts.Remove(top);
                        top = FormatNumber(top.ToLower().Replace("top", "").Replace("mv", "")).ToString();
                        ParsedParts.Add("TopRef", top);
                        break;
                    }
                }
                // ParsedParts.Add("TopRef", "0");
            }

            try
            {
                string top = (from x in sParts where x.ToLower().Contains("bttm") select x).First();
                sParts.Remove(top);
                top = FormatNumber(top.ToLower().Replace("bttm", "").Replace("mv", "")).ToString();
                ParsedParts.Add("bttmRef", top);
            }
            catch
            {
                ParsedParts.Add("bttmRef", "-2000");
            }

            bool hasBeads = false;
            try
            {
                string top = (from x in sParts where x.ToLower().Contains("bead") select x).First();

                if (top.Length == 4 || top.Length == 5)
                {
                    sParts.Remove(top);
                    hasBeads = true;
                }

            }
            catch
            {

            }

            try
            {
                string top = (from x in sParts where x.ToLower().Contains("pb") select x).First();
                double c = 1;
                try
                {
                    c = FormatNumber(top.ToLower().Replace("pb", ""));
                }
                catch
                {
                    for (int i = 0; i < sParts.Count; i++)
                    {
                        if (sParts[i].ToLower().Contains("pb") == true)
                        {
                            string top2 = sParts[i - 1];
                            sParts.Remove(sParts[i]);
                            sParts.Remove(top2);
                            top = top2 + "PB";
                            c = FormatNumber(top.ToLower().Replace("pb", ""));
                            break;
                        }
                    }
                }

                sParts.Remove(top);

                if (top.ToLower().Contains("nm"))
                    c = c / 1000d;
                if (top.ToLower().Contains("pm"))
                    c = c / 1000d / 1000d;

                ParsedParts.Add("analyte", "PB");
                ParsedParts.Add("buffer", "PB");
                ParsedParts.Add("concentration", c.ToString());
            }
            catch
            {

            }

            try
            {
                string top = (from x in sParts where x.ToLower().Contains("kcl") select x).First();

                double c = 1;
                try
                {
                    c = FormatNumber(top.ToLower().Replace("kcl", ""));
                }
                catch
                {
                    for (int i = 0; i < sParts.Count; i++)
                    {
                        if (sParts[i].ToLower().Contains("kcl") == true)
                        {
                            string top2 = sParts[i - 1];
                            sParts.Remove(sParts[i]);
                            sParts.Remove(top2);
                            top = top2 + "KCl";
                            c = FormatNumber(top.ToLower().Replace("kcl", ""));
                            break;
                        }
                    }
                }

                sParts.Remove(top);

                if (top.ToLower().Contains("nm"))
                    c = c / 1000d;

                ParsedParts.Add("analyte", "KCl");
                ParsedParts.Add("buffer", "KCl");
                ParsedParts.Add("concentration", c.ToString());

            }
            catch
            {

            }


            string[] analytes = new string[]{
                "avb3","avb1","a4b1","int" };
            foreach (var ana in analytes)
            {
                try
                {
                    string top = (from x in sParts where x.ToLower().Contains(ana) select x).First();
                    double c = 1;
                    try
                    {
                        c = FormatNumber(top.ToLower().Replace(ana, ""));
                    }
                    catch
                    {
                        for (int i = 0; i < sParts.Count; i++)
                        {
                            if (sParts[i].ToLower().Contains(ana) == true)
                            {
                                string top2 = sParts[i - 1];
                                sParts.Remove(sParts[i]);
                                sParts.Remove(top2);
                                top = top2 + ana;
                                c = FormatNumber(top.ToLower().Replace(ana, ""));
                                break;
                            }
                        }
                    }

                    sParts.Remove(top);

                    if (top.ToLower().Contains("nm"))
                        c = c / 1000d;
                    if (top.ToLower().Contains("pm"))
                        c = c / 1000d / 1000d;
                    if (top.ToLower().Contains("fm"))
                        c = c / 1000d / 1000d / 1000d;

                    ParsedParts.Add("analyte", ana);
                    ParsedParts.Add("buffer", "PB");
                    ParsedParts.Add("concentration", c.ToString());
                }
                catch
                {

                }
            }

            try
            {
                string top = (from x in sParts where x.ToLower().Contains("mm") select x).First();
                sParts.Remove(top);
                double c = FormatNumber(top.ToLower().Split(new string[] { "mm" }, StringSplitOptions.RemoveEmptyEntries)[0]);
                if (top.ToLower().Contains("nm"))
                    c = c / 1000d;

                if (ParsedParts.ContainsKey("analyte") == true)
                {
                    ParsedParts.Add("buffer", ParsedParts["analyte"]);
                    ParsedParts.Remove("analyte");
                }
                var pTop = top.Split(new string[] { "mM" }, StringSplitOptions.RemoveEmptyEntries);
                top = pTop[pTop.Length - 1];
                ParsedParts.Add("analyte", top);
                ParsedParts.Add("concentration", c.ToString());
            }
            catch
            {

            }



            try
            {
                string top = (from x in sParts where x.ToLower().Contains("nm") select x).First();
                sParts.Remove(top);
                double c = FormatNumber(top.ToLower().Split(new string[] { "nm" }, StringSplitOptions.RemoveEmptyEntries)[0]);
                if (top.ToLower().Contains("nm"))
                    c = c / 1000d;
                if (ParsedParts.ContainsKey("analyte") == true)
                {
                    ParsedParts.Add("buffer", ParsedParts["analyte"]);
                    ParsedParts.Remove("analyte");
                }
                var pTop = top.Split(new string[] { "nM" }, StringSplitOptions.RemoveEmptyEntries);
                top = pTop[pTop.Length - 1];
                ParsedParts.Add("analyte", top);
                ParsedParts.Add("concentration", c.ToString());
            }
            catch
            {

            }


            if (ParsedParts.ContainsKey("buffer") == false)
                ParsedParts.Add("buffer", "PB");
            if (ParsedParts.ContainsKey("analyte") == false)
                ParsedParts.Add("analyte", "PB");
            if (ParsedParts.ContainsKey("concentration") == false)
                ParsedParts.Add("concentration", "100");

            if (hasBeads)
            {
                ParsedParts["analyte"] = ParsedParts["analyte"] + "_beads";
            }
            try
            {
                string top = (from x in sParts where x.ToLower().Contains("mv") select x).First();
                sParts.Remove(top);
                top = FormatNumber(top.ToLower().Replace("mv", "")).ToString();
                if (filename.ToLower().Contains("m1g"))
                {
                    ParsedParts.Add("tunnelvoltagebottom", "0");
                    ParsedParts.Add("tunnelvoltage", top);
                }
                else
                {
                    if (filename.ToLower().Contains("m2g") || filename.ToLower().Contains("m3g") || filename.ToLower().Contains("m4g"))
                    {
                        ParsedParts.Add("tunnelvoltagebottom", top);
                        ParsedParts.Add("tunnelvoltage", "0");
                    }
                    else
                    {
                        ParsedParts.Add("tunnelvoltagebottom", "0");
                        ParsedParts.Add("tunnelvoltage", top);
                    }
                }
            }
            catch
            {
                for (int i = 0; i < sParts.Count; i++)
                {
                    if (sParts[i].ToLower().Contains("bias") == true)
                    {
                        try
                        {
                            string top2 = sParts[i + 1];
                            sParts.Remove(sParts[i]);
                            sParts.Remove(top2);
                            sParts.Remove("mV");
                            ParsedParts.Add("tunnelvoltage", FormatNumber(top2.ToLower()).ToString());
                        }
                        catch { }
                        break;
                    }
                }
            }

            var notes = (from x in sParts where x.ToLower().StartsWith("m") select x).ToArray();
            string junction = "";
            foreach (string s in notes)
            {
                sParts.Remove(s);
                junction += s + "_";
            }
            junction = junction.Trim();
            if (junction.Trim() != "")
            {
                if (junction.EndsWith("__"))
                    junction = junction.Substring(0, junction.Length - 2);
                if (junction.EndsWith("_"))
                    junction = junction.Substring(0, junction.Length - 1);

            }
            ParsedParts.Add("junction", junction.ToString());


            string remain = "";
            foreach (string s in sParts)
            {
                remain += s + "_";
            }

            remain = remain.Replace("LP_5KHz_", "");

            try
            {
                string name = dir + "_" + remain + "_" + fileNumber;
                if (name.ToLower().Contains(ParsedParts["analyte"].ToLower()) == false)
                    name = ParsedParts["analyte"] + name;
                name = name.Replace(junction, "").Replace(junction.Replace("_", ""), "").Replace("__", "_");
                ParsedParts["name"] = name;
            }
            catch
            {
                ParsedParts["name"] = remain + "_" + fileNumber;
            }

            return ParsedParts;
        }

        public static Dictionary<string, string> ParseFilenames(string filename)
        {

            if (filename.ToLower().Contains("integrin"))
            {
                return ParseYananFilenames(filename);
            }

            Dictionary<string, string> ParsedParts = new Dictionary<string, string>();
            ParsedParts.Add("file", filename);
            string file = Path.GetFileNameWithoutExtension(filename);
            string dir = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(filename));

            List<string> sParts = new List<string>(); sParts.AddRange(file.Split('_'));

            var fileNumber = "0000";

            if (sParts[sParts.Count - 1].ToLower() == "g")
                sParts.RemoveAt(sParts.Count - 1);
            if (sParts[sParts.Count - 1].All(Char.IsDigit))
            {
                fileNumber = sParts[sParts.Count - 1];
                sParts.RemoveAt(sParts.Count - 1);
            }

            try
            {
                string top = (from x in sParts where x.ToLower().Contains("top") select x).First();
                sParts.Remove(top);
                top = FormatNumber(top.ToLower().Replace("top", "").Replace("mv", "")).ToString();
                ParsedParts.Add("TopRef", top);
            }
            catch
            {
                // ParsedParts.Add("TopRef", "0");
            }

            try
            {
                string top = (from x in sParts where x.ToLower().Contains("bttm") select x).First();
                sParts.Remove(top);
                top = FormatNumber(top.ToLower().Replace("bttm", "").Replace("mv", "")).ToString();
                ParsedParts.Add("bttmRef", top);
            }
            catch
            {
                //   ParsedParts.Add("bttmRef", "0");
            }

            bool hasBeads = false;
            try
            {
                string top = (from x in sParts where x.ToLower().Contains("bead") select x).First();

                if (top.Length == 4 || top.Length == 5)
                {
                    sParts.Remove(top);
                    hasBeads = true;
                }

            }
            catch
            {

            }

            try
            {
                string top = (from x in sParts where x.ToLower().Contains("pb") select x).First();
                sParts.Remove(top);
                double c = FormatNumber(top.ToLower().Replace("pb", ""));
                if (top.ToLower().Contains("nm"))
                    c = c / 1000d;

                ParsedParts.Add("analyte", "PB");
                ParsedParts.Add("buffer", "PB");
                ParsedParts.Add("concentration", c.ToString());
            }
            catch
            {

            }

            try
            {
                string top = (from x in sParts where x.ToLower().Contains("kcl") select x).First();
                sParts.Remove(top);
                double c = FormatNumber(top.ToLower().Replace("kcl", ""));
                if (top.ToLower().Contains("nm"))
                    c = c / 1000d;
                ParsedParts.Add("analyte", "KCl");
                ParsedParts.Add("buffer", "KCl");
                ParsedParts.Add("concentration", c.ToString());
            }
            catch
            {

            }


            try
            {
                string top = (from x in sParts where x.ToLower().Contains("mm") select x).First();
                sParts.Remove(top);
                double c = FormatNumber(top.ToLower().Split(new string[] { "mm" }, StringSplitOptions.RemoveEmptyEntries)[0]);
                if (top.ToLower().Contains("nm"))
                    c = c / 1000d;

                if (ParsedParts.ContainsKey("analyte") == true)
                {
                    ParsedParts.Add("buffer", ParsedParts["analyte"]);
                    ParsedParts.Remove("analyte");
                }
                var pTop = top.Split(new string[] { "mM" }, StringSplitOptions.RemoveEmptyEntries);
                top = pTop[pTop.Length - 1];
                ParsedParts.Add("analyte", top);
                ParsedParts.Add("concentration", c.ToString());
            }
            catch
            {

            }

            try
            {
                string top = (from x in sParts where x.ToLower().Contains("um") select x).First();
                sParts.Remove(top);
                double c = FormatNumber(top.ToLower().Split(new string[] { "um" }, StringSplitOptions.RemoveEmptyEntries)[0]);
                if (top.ToLower().Contains("um"))
                    c = c / 1000d;
                if (ParsedParts.ContainsKey("analyte") == true)
                {
                    ParsedParts.Add("buffer", ParsedParts["analyte"]);
                    ParsedParts.Remove("analyte");
                }
                var pTop = top.Split(new string[] { "uM" }, StringSplitOptions.RemoveEmptyEntries);
                top = pTop[pTop.Length - 1];
                ParsedParts.Add("analyte", top);
                ParsedParts.Add("concentration", c.ToString());
            }
            catch
            {

            }

            try
            {
                string top = (from x in sParts where x.ToLower().Contains("nm") select x).First();
                sParts.Remove(top);
                double c = FormatNumber(top.ToLower().Split(new string[] { "nm" }, StringSplitOptions.RemoveEmptyEntries)[0]);
                if (top.ToLower().Contains("nm"))
                    c = c / 1000d / 1000d;
                if (ParsedParts.ContainsKey("analyte") == true)
                {
                    ParsedParts.Add("buffer", ParsedParts["analyte"]);
                    ParsedParts.Remove("analyte");
                }
                var pTop = top.Split(new string[] { "nM" }, StringSplitOptions.RemoveEmptyEntries);
                top = pTop[pTop.Length - 1];
                ParsedParts.Add("analyte", top);
                ParsedParts.Add("concentration", c.ToString());
            }
            catch
            {

            }


            if (ParsedParts.ContainsKey("buffer") == false)
                ParsedParts.Add("buffer", "PB");
            if (ParsedParts.ContainsKey("analyte") == false)
                ParsedParts.Add("analyte", "PB");
            if (ParsedParts.ContainsKey("concentration") == false)
                ParsedParts.Add("concentration", "100");

            if (hasBeads)
            {
                ParsedParts["analyte"] = ParsedParts["analyte"] + "_beads";
            }
            try
            {
                string top = (from x in sParts where x.ToLower().Contains("mv") select x).First();
                sParts.Remove(top);
                top = FormatNumber(top.ToLower().Replace("mv", "")).ToString();
                if (filename.ToLower().Contains("m1g"))
                {
                    ParsedParts.Add("tunnelvoltagebottom", "0");
                    ParsedParts.Add("tunnelvoltage", top);
                }
                else
                {
                    if (filename.ToLower().Contains("m2g") || filename.ToLower().Contains("m3g") || filename.ToLower().Contains("m4g"))
                    {
                        ParsedParts.Add("tunnelvoltagebottom", top);
                        ParsedParts.Add("tunnelvoltage", "0");
                    }
                    else
                    {
                        ParsedParts.Add("tunnelvoltagebottom", "0");
                        ParsedParts.Add("tunnelvoltage", top);
                    }
                }
            }
            catch
            {
                //ParsedParts.Add("tunnelvoltage", "0");
            }

            var notes = (from x in sParts where x.ToLower().StartsWith("m") select x).ToArray();
            string junction = "";
            foreach (string s in notes)
            {
                sParts.Remove(s);
                junction += s + "_";
            }
            junction = junction.Trim();
            if (junction.Trim() != "")
            {
                if (junction.EndsWith("__"))
                    junction = junction.Substring(0, junction.Length - 2);
                if (junction.EndsWith("_"))
                    junction = junction.Substring(0, junction.Length - 1);
            }
            junction = junction.Replace("g", "").Replace("G", "");

            if (junction.Length == 4 && junction.Contains("_") == false)
                junction = junction.Substring(0, 2) + "_" + junction.Substring(2);

            ParsedParts.Add("junction", junction);


            string remain = "";
            foreach (string s in sParts)
            {
                remain += s + "_";
            }

            try
            {
                if (dir.Contains("NAL"))
                {
                    string name = ParsedParts["concentration"] + "mM" + ParsedParts["analyte"] + "_" + fileNumber;
                    name = name.Replace(junction, "").Replace(junction.Replace("_", ""), "").Replace("__", "_");
                    if (name.Contains("2015") || name.Contains("2016"))
                        System.Diagnostics.Debug.Print("");
                    ParsedParts["name"] = name;
                }
                else
                {
                    string name = dir + remain + "_" + fileNumber;
                    name = name.Replace(junction, "").Replace(junction.Replace("_", ""), "").Replace("__", "_");
                    if (name.Contains("2015") || name.Contains("2016"))
                        System.Diagnostics.Debug.Print("");
                    ParsedParts["name"] = name;
                }
            }
            catch
            {
                ParsedParts["name"] = remain + "_" + fileNumber;
            }

            return ParsedParts;
        }

        public static Dictionary<string, string> ParsePeiString(string directoryPath)
        {

            Dictionary<string, string> ParsedParts = new Dictionary<string, string>();

            var pParts = new List<string>();
            pParts.AddRange(directoryPath.Split('\\'));


            string batchType = null;
            string batchChunk = null;
            try
            {
                batchChunk = (from x in pParts where x.ToLower().Contains("nald") select x).First();
                batchType = "nal";
            }
            catch { }
            if (batchChunk == null)
            {
                try
                {
                    batchChunk = (from x in pParts where x.Contains("ibm") select x).First();
                    batchType = "ibm";
                }
                catch { }
            }

            if (batchChunk == null)
            {
                try
                {
                    batchChunk = (from x in pParts where x.Contains("norcada") select x).First();
                    batchType = "norcada";
                }
                catch { }
            }
            if (batchChunk == null)
            {
                try
                {
                    batchChunk = (from x in pParts where x.Contains("asu") select x).First();
                    batchType = "asu";
                }
                catch { }
            }

            ParsedParts.Add("batchChunk", batchChunk);


            var sParts = new List<string>();
            sParts.AddRange(batchChunk.Split('_'));

            string date = null;
            DateTime measureDate;
            try
            {
                date = (from x in sParts where x.StartsWith("20") select x).First();
                sParts.Remove(date);
                measureDate = new DateTime(int.Parse(date.Substring(0, 4)), int.Parse(date.Substring(4, 2)), int.Parse(date.Substring(6, 2)));
            }
            catch
            {
                measureDate = DateTime.Now;
            }
            ParsedParts.Add("measureDate", measureDate.ToString());

            string batch = null;
            try
            {
                batch = (from x in sParts where x.ToLower().Contains(batchType) select x).First();
                sParts.Remove(batch);
            }
            catch { }

            ParsedParts.Add("batch", batch);

            string chip = null;
            try
            {
                chip = (from x in sParts where x.ToLower().Contains("chip") select x).First();
                sParts.Remove(chip);
            }
            catch { }
            ParsedParts.Add("chip", chip);

            string ald = "Al2O3";
            try
            {
                ald = (from x in sParts where x.ToLower().Contains("ald") select x).First();

                sParts.Remove(ald);
            }
            catch { }

            ParsedParts.Add("ald", ald);

            int cycles = 20;
            try
            {
                var c = (from x in sParts where x.ToLower().Contains("cyc") select x).First();

                sParts.Remove(c);
                c = c.ToLower().Replace("cyc", "");
                cycles = int.Parse(c);
            }
            catch
            {

            }
            ParsedParts.Add("cycles", cycles.ToString());

            string drillChunk = null;
            try
            {
                drillChunk = (from x in sParts where x.ToLower().Contains("him") select x).First();
                if (drillChunk != null)
                    sParts.Remove(drillChunk);
            }
            catch { }



            if (drillChunk == null)
            {
                try
                {
                    drillChunk = (from x in sParts where x.ToLower().Contains("fib") select x).First();
                    sParts.Remove(drillChunk);
                }
                catch { }
            }
            if (drillChunk == null)
            {
                try
                {
                    drillChunk = (from x in sParts where x.ToLower().Contains("rie") select x).First();
                    sParts.Remove(drillChunk);
                }
                catch { }
            }
            if (drillChunk == null)
            {
                try
                {
                    drillChunk = (from x in sParts where x.ToLower().Contains("tem") select x).First();
                    sParts.Remove(drillChunk);
                }
                catch { }
            }
            if (drillChunk == null)
                drillChunk = "RIE";
            ParsedParts.Add("drillChunk", drillChunk.ToString());

            string notes = "";
            foreach (string s in sParts)
                notes += " " + s;
            ParsedParts.Add("notes", notes.ToString());
            return ParsedParts;
        }

        public static Dictionary<string, string> ParseYananString(string directoryPath)
        {

            Dictionary<string, string> ParsedParts = new Dictionary<string, string>();

            var pParts = new List<string>();
            pParts.AddRange(directoryPath.Split('\\'));


            string batchType = null;
            string batchChunk = null;
            try
            {
                batchChunk = (from x in pParts where x.ToLower().Contains("nald") select x).First();
                batchType = "nal";
            }
            catch { }
            if (batchChunk == null)
            {
                try
                {
                    batchChunk = (from x in pParts where x.Contains("ibm") select x).First();
                    batchType = "ibm";
                }
                catch { }
            }

            if (batchChunk == null)
            {
                try
                {
                    batchChunk = (from x in pParts where x.Contains("norcada") select x).First();
                    batchType = "norcada";
                }
                catch { }
            }
            if (batchChunk == null)
            {
                try
                {
                    batchChunk = (from x in pParts where x.Contains("asu") select x).First();
                    batchType = "asu";
                }
                catch { }
            }

            if (batchChunk == null)
            {
                try
                {
                    batchChunk = (from x in pParts where x.Contains("FIB") select x).First();
                    batchType = "other";
                }
                catch { }
            }


            ParsedParts.Add("batchChunk", batchChunk);


            var sParts = new List<string>();
            sParts.AddRange(batchChunk.Split('_'));

            string date = null;
            DateTime measureDate;
            try
            {
                date = (from x in sParts where x.StartsWith("20") select x).First();
                sParts.Remove(date);
                measureDate = new DateTime(int.Parse(date.Substring(0, 4)), int.Parse(date.Substring(4, 2)), int.Parse(date.Substring(6, 2)));
            }
            catch
            {
                measureDate = DateTime.Now;
            }
            ParsedParts.Add("measureDate", measureDate.ToString());

            string batch = null;
            try
            {
                batch = (from x in sParts where x.ToLower().Contains(batchType) select x).First();
                sParts.Remove(batch);
            }
            catch { }

            ParsedParts.Add("batch", batch);

            string chip = null;
            try
            {
                chip = (from x in sParts where x.ToLower().Contains("chip") select x).First();
                sParts.Remove(chip);
            }
            catch { }
            ParsedParts.Add("chip", chip);

            string ald = "Al2O3";
            try
            {
                ald = (from x in sParts where x.ToLower().Contains("ald") || x.ToLower().Contains("al2o") select x).First();

                sParts.Remove(ald);
            }
            catch { }

            ParsedParts.Add("ald", ald);

            int cycles = 20;
            try
            {
                var c = (from x in sParts where x.ToLower().Contains("cyc") select x).First();

                sParts.Remove(c);
                c = c.ToLower().Replace("cyc", "");
                cycles = int.Parse(c);
            }
            catch
            {

            }
            ParsedParts.Add("cycles", cycles.ToString());

            string drillChunk = null;
            try
            {
                drillChunk = (from x in sParts where x.ToLower().Contains("him") select x).First();
                if (drillChunk != null)
                    sParts.Remove(drillChunk);
            }
            catch { }



            if (drillChunk == null)
            {
                try
                {
                    drillChunk = (from x in sParts where x.ToLower().Contains("fib") select x).First();
                    sParts.Remove(drillChunk);
                }
                catch { }
            }
            if (drillChunk == null)
            {
                try
                {
                    drillChunk = (from x in sParts where x.ToLower().Contains("rie") select x).First();
                    sParts.Remove(drillChunk);
                }
                catch { }
            }
            if (drillChunk == null)
            {
                try
                {
                    drillChunk = (from x in sParts where x.ToLower().Contains("tem") select x).First();
                    sParts.Remove(drillChunk);
                }
                catch { }
            }
            if (drillChunk == null)
                drillChunk = "RIE";
            ParsedParts.Add("drillChunk", drillChunk.ToString());

            string notes = "";
            foreach (string s in sParts)
                notes += " " + s;
            ParsedParts.Add("notes", notes.ToString());
            return ParsedParts;
        }

        #endregion

        #region Database
        private static Tuple<Batch, Chip> ParseToDatabase(DataModel.Experiment experiment, Dictionary<string, string> parse)
        {
            using (LabContext context = new LabContext())
            {

                var exp = (from x in context.Experiments where x.Id == experiment.Id select x).First();
                var batchNames = (from x in exp.Batches select x.BatchName).ToArray();

                var chipNumber = 2;
                try
                {
                    chipNumber = int.Parse("0" + Regex.Match(parse["chip"], @"\d+").Value);
                }
                catch { }
                Batch batchTable;
                //if (parse["batch"]==null || parse["batch"].Trim()=="")
                //{
                //   parse["batch"]= "NALDB31";
                //}
                string dir = exp.Folder + "\\" + parse["batch"];
                DateTime measuredDate = DateTime.Parse(parse["measureDate"]);

                if (batchNames.Contains(parse["batch"]))
                {
                    batchTable = (from x in exp.Batches where x.BatchName == parse["batch"] select x).First();
                    if (batchTable.NumberOfDeliveredChips < chipNumber)
                        batchTable.NumberOfDeliveredChips = chipNumber;
                }
                else
                {
                    batchTable = new Batch
                    {
                        BatchName = parse["batch"],
                        DeliveryDate = measuredDate,
                        JunctionMaterial = "Pd",
                        Notes = new List<eNote> { new eNote(parse["notes"]) },
                        ALDMaterial = "Al2O3",
                        DrillMethod = parse["drillChunk"],
                        ALDCycles = int.Parse(parse["cycles"]),
                        Experiment = exp,
                        Folder = dir,
                        ManufactorDate = measuredDate,
                        NumberOfDeliveredChips = chipNumber,
                        Chips = new List<Chip>(),
                        AdditionalFiles = new List<AdditionalFile>(),
                        OtherInfo = ""
                    };

                    try
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                    catch
                    { }

                    exp.Batches.Add(batchTable);
                    context.SaveChanges();
                }

                if (parse["chip"] == null)
                {
                    parse.Remove("chip");
                    if (batchTable.Chips.Count == 0)
                        parse["chip"] = "Chip01";
                    else
                        parse["chip"] = batchTable.Chips[batchTable.Chips.Count - 1].ChipName;
                }

                var ChipNames = (from x in batchTable.Chips select x.ChipName).ToArray();

                Chip chipTable;
                string chipDir = dir + "\\" + parse["chip"];

                if (ChipNames.Contains(parse["chip"]))
                {
                    chipTable = (from x in batchTable.Chips where x.ChipName == parse["chip"] select x).First();
                }
                else
                {
                    chipTable = new Chip
                    {
                        ALDCycles = int.Parse(parse["cycles"]),
                        ALDMaterial = "Al2O3",
                        Batch = batchTable,
                        ChipName = parse["chip"],
                        DrillMethod = parse["drillChunk"],
                        Experiment = exp,
                        Folder = chipDir,
                        GoodChip = UserRating.Good,
                        JunctionMaterial = "Al2O3",
                        Note = new eNote(parse["notes"]),
                        NumberOfJunctions = 3,
                        NumberPores = 3,
                        PoreSize = 10,
                        Junctions = new List<Junction>()
                    };
                    batchTable.Chips.Add(chipTable);
                    batchTable.NumberOfDeliveredChips++;
                    context.SaveChanges();
                }
                try
                {
                    System.IO.Directory.CreateDirectory(chipDir);
                }
                catch
                { }

                context.SaveChanges();
                return new Tuple<Batch, Chip>(batchTable, chipTable);
            }
        }

        private static Tuple<Batch, Chip, Dictionary<string, string>> ParseDirectoryString(Experiment experiment, string directoryPath)
        {
            Dictionary<string, string> parse;

            if (directoryPath.ToLower().Contains("integrin") == true)
                parse = ExistingParse.ParseYananString(directoryPath);
            else
                parse = ExistingParse.ParsePeiString(directoryPath);
            var t = ParseToDatabase(experiment, parse);
            return new Tuple<Batch, Chip, Dictionary<string, string>>(t.Item1, t.Item2, parse);
        }

        private static Junction AddJunction(Dictionary<string, string> parse, string filename, Chip chipTable)
        {
            using (LabContext context = new LabContext())
            {
                chipTable = (from x in context.Chips where x.Id == chipTable.Id select x).First();
                var junctionNames = (from x in chipTable.Junctions select x.JunctionName).ToArray();

                var exp = chipTable.Experiment;

                Junction junction;

                if (junctionNames.Contains(parse["junction"]))
                {
                    junction = (from x in chipTable.Junctions where x.JunctionName == parse["junction"] select x).First();
                }
                else
                {
                    string junctionDir = chipTable.Folder + "\\" + parse["junction"];
                    try
                    {
                        System.IO.Directory.CreateDirectory(junctionDir);
                    }
                    catch
                    { }

                    junction = new DataModel.Junction
                    {
                        Chip = chipTable,
                        Experiment = exp,
                        Folder = junctionDir,
                        GapSize = 2,
                        GoodJunction = UserRating.Good,
                        Note = new eNote(),
                        JunctionName = parse["junction"],
                        Traces = new List<DataTrace>()

                    };
                    chipTable.Junctions.Add(junction);
                }

                context.SaveChanges();
                return junction;
            }

        }

        #endregion

        //private static byte[] GetBinaryFile(string filename)
        //{
        //    byte[] bytes;
        //    using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read))
        //    {
        //        bytes = new byte[file.Length];
        //        file.Read(bytes, 0, (int)file.Length);
        //    }
        //    return bytes;
        //}

        #region pythonBridge
        private static void RunPython(DataTrace trace)
        {
            var files = Directory.GetFiles(trace.Folder, "*" + trace.TraceName + "*_header.txt");

            foreach (var file in files)
            {
                string contents = File.ReadAllText(file);
                if (contents.Contains(trace.Filename))
                {
                    trace.ProcessedFile = file.ToLower().Replace("_header.txt", "");

                }
            }

            string processedFile = trace.ProcessedFile;

            if (File.Exists(trace.ProcessedFile + "_Background_Removed.bbf") == false)
            // if (File.Exists(trace.ProcessedFile + "_header.txt") == false)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.FileName = @"C:\WinPython-64bit-2.7.10.3\python-2.7.10.amd64\python.exe";
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.WorkingDirectory = @"C:\WinPython-64bit-2.7.10.3\python-2.7.10.amd64";
                if (AppDomain.CurrentDomain.BaseDirectory.ToLower().Contains("dropbox") == true)
                {
                    startInfo.Arguments = @"C:\Development\TunnelVision\python\Bridge.py """ + trace.Filename +
                        @""" """ + trace.ProcessedFile + @""" """ + trace.ControlFile + @""" " + trace.Concentration_mM + " " + trace.NumberPores;
                }
                else
                {
                    startInfo.Arguments = AppDomain.CurrentDomain.BaseDirectory + @"\python\Bridge.py """ + trace.Filename +
                            @""" """ + trace.ProcessedFile + @""" """ + trace.ControlFile + @""" " + trace.Concentration_mM + " " + trace.NumberPores;
                }


                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;

                try
                {
                    //  Start the process with the info we specified.
                    //  Call WaitForExit and then the using-statement will close.
                    using (Process exeProcess = Process.Start(startInfo))
                    {
                        using (StreamReader readerSTD = exeProcess.StandardOutput)
                        {
                            using (StreamReader readerError = exeProcess.StandardError)
                            {
                                string result2 = "";
                                while (exeProcess.HasExited == false)
                                {
                                    // CommonExtensions.DoEvents();
                                    result2 = readerSTD.ReadToEnd();
                                    System.Diagnostics.Debug.Print(result2);

                                }
                                string result = readerError.ReadToEnd();
                                System.Diagnostics.Debug.Print(result);
                                if (result.Trim().Length > 0 && result.Contains("RuntimeWarning") == false && result.Contains("UserWarning") == false)
                                    System.Diagnostics.Debug.Print(result);
                                //    System.Windows.Forms.MessageBox.Show( result);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                    //System.Windows.Forms.MessageBox.Show(trace.Folder + "\\" + trace.TraceName + "_header.txt");
                    //   System.Windows.Forms.MessageBox.Show(ex.Message);
                    //System.Windows.Forms.MessageBox.Show(ex.StackTrace);
                }
            }

            var Header = DataModel.Fileserver.FileDataTrace.ReadHeader(trace.ProcessedFile + "_header.txt");
            string[] binFiles = new string[] { "_Smooth", "_Wiener", "_Background_Removed", "_cusum", "_TV", "_ShortFiltered", "_ShortRaw" };
            foreach (string file in binFiles)
            {
                try
                {
                    if (File.Exists(trace.ProcessedFile + file + ".bin") == true)
                    {
                        DataModel.Fileserver.FileTrace.ConvertBin(trace.ProcessedFile + "_raw.bbf", trace.ProcessedFile + file + ".bin", trace.ProcessedFile + file + ".bbf", file);
                        File.Delete(trace.ProcessedFile + file + ".bin");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print("");
                }
            }



            using (LabContext context = new LabContext())
            {
                trace = (from x in context.Traces where x.Id == trace.Id select x).FirstOrDefault();
                trace.ProcessedFile = processedFile;
                try
                {
                    if (Header.ContainsKey("samplerate") == true)
                    {
                        trace.SampleRate = double.Parse(Header["samplerate"]);

                        trace.HasIonic = (Header["hasionic"].ToLower() == "true");
                        trace.DataRig = "Axon";

                        string date = Header["filedate"];
                        string[] tParts = Header["starttime"].Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                        DateTime dt = new DateTime(int.Parse(date.Substring(0, 4)), int.Parse(date.Substring(4, 2)), int.Parse(date.Substring(6, 2)),
                            int.Parse(tParts[0]), int.Parse(tParts[1]), int.Parse(tParts[2]));
                        trace.DateAcquired = dt;
                        try
                        {
                            if (Header.ContainsKey("tun_wnoise") == true)
                            {
                                trace.WeinerNoise = double.Parse(Header["tun_wnoise"].Trim().Split(' ')[0]);// :5.57823209556 pA
                                trace.MeanVoltage = double.Parse(Header["meanvoltage"].Trim().Split(' ')[0]);
                                trace.Noise = double.Parse(Header["tun_noise"].Trim().Split(' ')[0]);
                                trace.Baseline = double.Parse(Header["tun_grossbaseline"].Trim().Split(' ')[0]);
                                trace.FlattenedBaseline = double.Parse(Header["tun_flatbaseline"].Trim().Split(' ')[0]);
                                trace.Tunnel_Range = double.Parse(Header["tun_range"].Trim().Split(' ')[0]);
                            }
                        }
                        catch { }
                        try
                        {

                            if (Header.ContainsKey("tun_wnoise") == true)
                            {
                                trace.RunTime = double.Parse(Header["tracetime"].Trim().Split(' ')[0]);
                            }
                            else
                            {
                                trace.RunTime = double.Parse(Header["npoints"]) / trace.SampleRate;
                            }

                        }
                        catch
                        {
                            if (Header.ContainsKey("npoints") == true)
                            {
                                trace.RunTime = double.Parse(Header["npoints"]) / trace.SampleRate;
                            }
                        }
                    }
                    else
                    {
                        trace.SampleRate = 20000;
                        trace.HasIonic = false;
                        trace.DataRig = "Axon";
                        trace.DateAcquired = DateTime.Now;
                    }

                }
                catch (Exception ex)
                {
                    AppLogger.LogError(ex);
                    //System.Windows.Forms.MessageBox.Show(trace.Folder + "\\" + trace.TraceName + "_header.txt");
                    //  System.Windows.Forms.MessageBox.Show(ex.Message);
                    // System.Windows.Forms.MessageBox.Show(ex.StackTrace);
                }
                context.SaveChanges();
            }

        }

        private static Dictionary<string, string> RunPythonIV(Trace trace)
        {

            var files = Directory.GetFiles(trace.Folder, "*" + trace.TraceName + "*_header.txt");

            foreach (var file in files)
            {
                string contents = File.ReadAllText(file);
                if (contents.Contains(trace.Filename))
                {
                    trace.ProcessedFile = file.ToLower().Replace("_header.txt", "");

                }
            }

            string processedFile = trace.ProcessedFile;

            if (File.Exists(trace.ProcessedFile + "_header.txt") == false)
            {

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.FileName = @"C:\WinPython-64bit-2.7.10.3\python-2.7.10.amd64\python.exe";
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.WorkingDirectory = @"C:\WinPython-64bit-2.7.10.3\python-2.7.10.amd64";
                if (AppDomain.CurrentDomain.BaseDirectory.ToLower().Contains("dropbox") == true)
                {
                    startInfo.Arguments = @"C:\Development\TunnelVision\TunnelSurfer5\Python\BridgeIV.py """ + trace.Filename +
                           @""" """ + trace.ProcessedFile + @""" """ + trace.ControlFile + @""" " + trace.Concentration_mM + " " + trace.NumberPores;
                }
                else
                {
                    startInfo.Arguments = AppDomain.CurrentDomain.BaseDirectory + @"\python\BridgeIV.py """ + trace.Filename +
                            @""" """ + trace.ProcessedFile + @""" """ + trace.ControlFile + @""" " + trace.Concentration_mM + " " + trace.NumberPores;
                }
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;

                try
                {

                    //  Start the process with the info we specified.
                    //  Call WaitForExit and then the using-statement will close.
                    string outString = "";
                    using (Process exeProcess = Process.Start(startInfo))
                    {
                        using (StreamReader readerSTD = exeProcess.StandardOutput)
                        {
                            using (StreamReader readerError = exeProcess.StandardError)
                            {

                                while (exeProcess.HasExited == false)
                                {
                                    CommonExtensions.DoEvents();
                                    string result2 = readerSTD.ReadToEnd();
                                    outString += result2;
                                }
                                System.Diagnostics.Debug.Print(outString);
                                string result = readerError.ReadToEnd();
                                if (result.Trim().Length > 0 && result.Contains("RuntimeWarning") == false)
                                    System.Windows.Forms.MessageBox.Show(outString + "\n" + result);
                                System.Diagnostics.Debug.Print(result);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                    // Log error.
                }
            }

            string text = System.IO.File.ReadAllText(trace.ProcessedFile + "_header.txt");
            string[] lines = text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> Header = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                try
                {
                    string[] parts = line.Split('=');
                    Header.Add(parts[0].Trim(), parts[1].Trim());
                }
                catch { }
            }
            Header.Add("ProcessedFile", processedFile);
            return Header;


        }

        private static void PreprocessFile(object traceObj)
        {
            try
            {
                DataTrace trace = (DataTrace)traceObj;
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.FileName = @"C:\WinPython-64bit-2.7.10.3\python-2.7.10.amd64\python.exe";
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.WorkingDirectory = @"C:\WinPython-64bit-2.7.10.3\python-2.7.10.amd64";

                if (AppDomain.CurrentDomain.BaseDirectory.ToLower().Contains("dropbox") == true)
                {
                    startInfo.Arguments = @"C:\Development\TunnelVision\python\ABFtoBinary.py """ + trace.Filename +
                        @""" """ + trace.ProcessedFile;
                }
                else
                {
                    startInfo.Arguments = AppDomain.CurrentDomain.BaseDirectory + @"\python\ABFtoBinary.py """ + trace.Filename +
                        @""" """ + trace.ProcessedFile;
                }

                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;

                try
                {
                    //  Start the process with the info we specified.
                    //  Call WaitForExit and then the using-statement will close.
                    using (Process exeProcess = Process.Start(startInfo))
                    {
                        using (StreamReader readerSTD = exeProcess.StandardOutput)
                        {
                            using (StreamReader readerError = exeProcess.StandardError)
                            {
                                string result2 = "";
                                while (exeProcess.HasExited == false)
                                {
                                    // CommonExtensions.DoEvents();
                                    result2 = readerSTD.ReadToEnd();
                                    System.Diagnostics.Debug.Print(result2);
                                }
                                string result = readerError.ReadToEnd();
                                System.Diagnostics.Debug.Print(result);
                                //if (result.Trim().Length > 0)
                                //    System.Windows.Forms.MessageBox.Show(result2 + "\n" + result);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                    // Log error.
                }

                //if (File.Exists(@"\\biofs\smb\Research\TunnelSurfer\Queue\_newDT_" + trace.Id.ToString() + " .abf") == false)
                //{
                //    File.Copy(trace.Filename, @"\\biofs\smb\Research\TunnelSurfer\Queue\_newDT_" + trace.Id.ToString() + " .abf");
                //}

            }
            catch (Exception mex)
            {
                System.Diagnostics.Debug.Print(mex.Message);
            }
        }
        #endregion

        #region AddFiles
        private static DataModel.Trace AddFile(Experiment experiment, string filename, Dictionary<string, string> traceParse, Dictionary<string, string> directoryParse, bool runPython)
        {
            var uppers = ParseToDatabase(experiment, directoryParse);

            Junction junction = AddJunction(traceParse, filename, uppers.Item2);

            return AddFile(experiment, filename, uppers.Item1, uppers.Item2, junction, traceParse, runPython);

        }

        private static DataModel.Trace AddFile(Experiment experiment, string filename, Batch theBatch = null, Chip theChip = null, Junction theJunction = null, Dictionary<string, string> traceParse = null, bool runPython = true)
        {
            Batch batch = theBatch;
            Chip chip = theChip;
            if (batch == null)
            {
                Tuple<Batch, Chip, Dictionary<string, string>> Tables = ParseDirectoryString(experiment, filename);
                if (theChip == null)
                    chip = Tables.Item2;
                batch = Tables.Item1;
            }

            if (traceParse == null)
                traceParse = ExistingParse.ParseFilenames(filename);

            Junction junction = theJunction;
            if (theJunction == null)
                junction = AddJunction(traceParse, filename, chip);

            DateTime measuredDate = DateTime.Now;

            if (traceParse.ContainsKey("bttmRef") == false)
                traceParse.Add("bttmRef", "-2000");

            if (traceParse.ContainsKey("TopRef") == false)
                traceParse.Add("TopRef", "-2000");

            if (traceParse.ContainsKey("tunnelvoltage") == false)
                traceParse.Add("tunnelvoltage", "-2000");

            if (traceParse.ContainsKey("tunnelvoltagebottom") == false)
                traceParse.Add("tunnelvoltagebottom", "0");

            DataTrace trace = null;
            try
            {
                trace = new DataTrace
                {
                    Analyte = traceParse["analyte"],
                    Buffer = traceParse["buffer"],
                    Concentration_mM = float.Parse(traceParse["concentration"]),
                    DateAcquired = measuredDate,
                    Filename = traceParse["file"],
                    GoodTrace = UserRating.Good,
                    MachineRating = UserRating.Unrated,
                    QCStep = QualityControlStep.RT,
                    DataRig = "axon",
                    HasIonic = false,
                    TraceName = traceParse["name"],
                    BottomReference_mV = double.Parse(traceParse["bttmRef"]),
                    TopReference_mV = double.Parse(traceParse["TopRef"]),
                    Tunnel_mV_Top = double.Parse(traceParse["tunnelvoltage"]),
                    Tunnel_mV_Bottom = double.Parse(traceParse["tunnelvoltagebottom"]),
                    Folder = junction.Folder,
                    Note = new eNote(""),
                    IsControl = (traceParse["file"].ToLower().Contains("control") == true || traceParse["file"].ToLower().Contains("rinse") == true),
                    OtherInfo = "",
                    NumberPores = 3
                    //OtherInfo = new Dictionary<string, string>()
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
            }

            trace.Condition = ExistingParse.ConditionString(trace);
            using (LabContext context = new LabContext())
            {
                chip = (from x in context.Chips where x.Id == chip.Id select x).First();
                junction = (from x in chip.Junctions where x.JunctionName == junction.JunctionName select x).First();
                string controlFile = "";
                if (trace.IsControl == false)
                {
                    try
                    {
                        var control = (from x in junction.Traces where (x.Condition == trace.Condition && x.IsControl == true) select x).FirstOrDefault();
                        if (control != null)
                        {
                            if (trace.BufferConcentration_mM == 0)
                                trace.BufferConcentration_mM = control.Concentration_mM;
                            controlFile = control.ProcessedFile;
                        }
                        else
                        {
                            control = (from x in junction.Traces where x.IsControl == true select x).FirstOrDefault();
                            if (control != null)
                            {
                                if (trace.BufferConcentration_mM == 0)
                                    trace.BufferConcentration_mM = control.Concentration_mM;
                                controlFile = control.ProcessedFile;
                            }
                            else
                            {
                                control = (from x in junction.Traces where (x.Analyte.ToLower() == "pb" || x.Analyte.ToLower() == "kcl") select x).FirstOrDefault();
                                if (control != null)
                                {
                                    if (trace.BufferConcentration_mM == 0)
                                        trace.BufferConcentration_mM = control.Concentration_mM;
                                    controlFile = control.ProcessedFile;
                                }
                            }
                        }
                    }
                    catch
                    {


                    }
                }

                trace.ControlFile = controlFile;
                if (trace.BufferConcentration_mM == 0)
                    trace.BufferConcentration_mM = trace.Concentration_mM;

                var cTrace = (from x in junction.Traces where x.Filename == trace.Filename select x).FirstOrDefault();

                if (cTrace == null)
                {
                    trace.Chip = chip;
                    trace.Junction = junction;

                    junction.Traces.Add(trace);
                    context.SaveChanges();
                    // trace.TraceName += "_" + trace.Id.ToString().PadLeft(4, '0');
                   // context.SaveChanges();

                    trace.ProcessedFile = trace.Folder + "\\" + trace.TraceName + trace.Id.ToString().PadLeft(4, '0');
                    trace.ProcessedFile = trace.ProcessedFile + "\\_" + trace.Id;
                    trace.Folder = System.IO.Path.GetDirectoryName(trace.ProcessedFile);
                    context.SaveChanges();

                    if (Directory.Exists(trace.Folder) == false)
                        Directory.CreateDirectory(trace.Folder);

                    if (Path.GetExtension(trace.Filename).ToLower() == ".abf" && File.Exists(trace.ProcessedFile + "_raw.json") == false)
                        DataModel.Fileserver.ABFLoader.ConvertABF_FLAC(trace.Filename, trace.Folder, trace.ProcessedFile + "_raw.json");
                    //    DataModel.Fileserver.ABFLoader.ConvertABF_BBF(trace.Filename, " ", trace.ProcessedFile + "_raw.bbf");

                    // File.Copy(trace.Filename, trace.ProcessedFile + "_Raw.bbf");
                }
                else
                    trace = cTrace;
            }
            try
            {
                System.IO.File.Copy(trace.Filename, System.IO.Path.GetDirectoryName(trace.Folder) + "\\" + trace.TraceName + "_" + trace.Id.ToString() + ".abf");
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
            if (File.Exists(@"C:\WinPython-64bit-2.7.10.3\python-2.7.10.amd64\python.exe") && runPython)
                RunPython(trace);
            else
            {

            }
            return trace;
        }

        private static DataModel.Trace AddIVFile(Experiment experiment, string filename, Dictionary<string, string> traceParse, Dictionary<string, string> directoryParse, bool runPython)
        {
            var uppers = ParseToDatabase(experiment, directoryParse);
            var chip = uppers.Item2;
            var batch = uppers.Item1;

            Junction junction = AddJunction(traceParse, filename, chip);

            return AddIVFile(experiment, filename, uppers.Item1, uppers.Item2, junction, traceParse, runPython);
        }

        private static DataModel.Trace AddIonic(Dictionary<string, string> traceParse, string folder, DateTime measuredDate, string TraceName, Chip chip, Junction junction, bool runPython = true)
        {
            var ivTrace = new IonicTrace
            {
                Buffer = traceParse["buffer"],
                Concentration_mM = float.Parse(traceParse["concentration"]),
                DateAcquired = measuredDate,
                Filename = traceParse["file"],
                GoodTrace = UserRating.Good,
                MachineRating = UserRating.Unrated,
                QCStep = QualityControlStep.Wet,
                DataRig = "axon",
                Folder = folder,
                HasIonic = true,
                TraceName = TraceName,
                Note = new eNote(""),
                IsControl = (traceParse["file"].ToLower().Contains("control") == true),
                OtherInfo = "",
                NumberPores = 3
                //OtherInfo = new Dictionary<string, string>()

            };
            ivTrace.Condition = ExistingParse.ConditionString(ivTrace);


            using (LabContext context = new LabContext())
            {
                chip = (from x in context.Chips where x.Id == chip.Id select x).First();

                var IVTry = (from x in context.IonicIV where x.Filename == ivTrace.Filename select x).FirstOrDefault();
                //if (IVTry != null)
                //{
                //    ivTrace = IVTry;

                //}
                //else
                {
                    if (junction != null)
                    {
                        junction = (from x in chip.Junctions where x.JunctionName == junction.JunctionName select x).First();
                        ivTrace.Junction = junction;
                        ivTrace.Chip = chip;
                        junction.IonicIV.Add(ivTrace);

                    }
                    else
                    {
                        ivTrace.Chip = chip;
                        chip.IonicIV.Add(ivTrace);
                    }
                    if (ivTrace.BufferConcentration_mM == 0)
                        ivTrace.BufferConcentration_mM = ivTrace.Concentration_mM;

                    context.SaveChanges();

                    ivTrace.ProcessedFile = ivTrace.Folder + "\\" + ivTrace.TraceName + "_" + ivTrace.Id.ToString().PadLeft(4, '0'); ;
                    context.SaveChanges();
                }
            }

            if (File.Exists(@"C:\WinPython-64bit-2.7.10.3\python-2.7.10.amd64\python.exe") && runPython)
            {
                var header = RunPythonIV(ivTrace);

                using (LabContext context = new LabContext())
                {
                    ivTrace = (from x in context.IonicIV where x.Id == ivTrace.Id select x).First();
                    ivTrace.ProcessedFile = header["ProcessedFile"];
                    ivTrace.ExpectedConductance = StripUnit(header["Theory_Pore_Conduct_Access"]);
                    ivTrace.RunTime = StripUnit(header["traceTime"]);
                    ivTrace.Capacitance = StripUnit(header["model_Capacitance"]);
                    ivTrace.AccessPoreSize = StripUnit(header["Calculated_Access_PoreSize_Model"]);
                    ivTrace.SmeetPoreSize = StripUnit(header["Calculated_Smeet_PoreSize_Model"]);
                    ivTrace.ExpectedSmeetConductance = StripUnit(header["Theory_SmeetConductance_Model"]);
                    ivTrace.NumberPores = (int)StripUnit(header["number_of_pores"]);
                    ivTrace.VoltageOffset = StripUnit(header["model_VoltageOffset"]);

                    ivTrace.ExpectedConductance = StripUnit(header["model_PoreConductance"]);
                    ivTrace.dVdt = StripUnit(header["dvdt"]);
                    ivTrace.Asymmetry = StripUnit(header["CurveSymmetry"]);
                    ivTrace.CurrentOffset = StripUnit(header["model_CurrentOffset"]);
                    ivTrace.Conductance = StripUnit(header["Conductance"]);
                    context.SaveChanges();
                }
            }
            else
            {
                if (Path.GetExtension(ivTrace.Filename) == "")
                    DataModel.Fileserver.FileTrace.ConvertWeisiToBin(ivTrace, ivTrace.Filename);
                else
                {
                    if (Path.GetExtension(ivTrace.Filename).ToLower() == ".csv")
                    {
                        DataModel.Fileserver.FileTrace.ConvertKeithleyToBin(ivTrace, ivTrace.Filename);
                    }
                }
                //using (LabContext context = new LabContext())
                //{
                //    ivTrace = (from x in context.IonicIV where x.Id == ivTrace.Id select x).First();
                //    string filename = Path.GetFileNameWithoutExtension(ivTrace.Filename) + "_" + ivTrace.Id + Path.GetExtension(ivTrace.Filename);
                //    string outFilename = App.ConvertFilePaths(@"s:\Research\TunnelSurfer\Queue\" + filename);
                //    System.IO.File.Copy(ivTrace.Filename, outFilename);
                //    ivTrace.Filename = @"s:\Research\TunnelSurfer\Queue\" + filename;
                //    context.SaveChanges();
                //}
            }
            return ivTrace;
        }

        private static DataModel.Trace AddTunnelTrace(Dictionary<string, string> traceParse, string folder, DateTime measuredDate, string TraceName, Chip chip, Junction junction, bool runPython = true)
        {
            if (traceParse.ContainsKey("bttmRef") == false)
            {
                traceParse.Add("bttmRef", "-2000");
            }
            if (traceParse.ContainsKey("TopRef") == false)
            {
                traceParse.Add("TopRef", "-2000");
            }
            if (traceParse.ContainsKey("tunnelvoltage") == false)
            {
                traceParse.Add("tunnelvoltage", "-3000");
            }
            var ivTrace = new TunnelingTrace
            {
                Buffer = traceParse["buffer"],
                Concentration_mM = float.Parse(traceParse["concentration"]),
                DateAcquired = measuredDate,
                Filename = traceParse["file"],
                GoodTrace = UserRating.Good,
                MachineRating = UserRating.Unrated,
                QCStep = QualityControlStep.Wet,
                DataRig = "axon",
                Folder = folder,
                HasIonic = false,
                TraceName = TraceName,
                Note = new eNote(""),
                IsControl = (traceParse["file"].ToLower().Contains("control") == true),
                BottomReference_mV = double.Parse(traceParse["bttmRef"]),
                TopReference_mV = double.Parse(traceParse["TopRef"]),
                Tunnel_mV_Top = double.Parse(traceParse["tunnelvoltage"]),
                Tunnel_mV_Bottom = double.Parse(traceParse["tunnelvoltagebottom"]),
                Analyte = traceParse["analyte"],
                OtherInfo = "",
                NumberPores = 3
                //OtherInfo = new Dictionary<string, string>()
            };
            try
            {
                if (traceParse.ContainsKey("isControl"))
                    ivTrace.IsControl = (traceParse["isControl"] == true.ToString());
                if (traceParse.ContainsKey("notes"))
                    ivTrace.Note = new eNote(traceParse["notes"]);
            }
            catch { }

            ivTrace.Condition = ExistingParse.ConditionString(ivTrace);
            ivTrace.ExpectedGapSize = 2;
            ivTrace.HasIonic = false;

            try
            {
                using (LabContext context = new LabContext())
                {
                    chip = (from x in context.Chips where x.Id == chip.Id select x).First();

                    var IVTry = (from x in context.TunnelIV where x.Filename == ivTrace.Filename select x).FirstOrDefault();

                    {
                        junction = (from x in chip.Junctions where x.JunctionName == junction.JunctionName select x).First();
                        ivTrace.Junction = junction;
                        ivTrace.Chip = chip;
                        junction.TunnelIV.Add(ivTrace);
                        if (ivTrace.BufferConcentration_mM == 0)
                            ivTrace.BufferConcentration_mM = ivTrace.Concentration_mM;
                        context.SaveChanges();

                        ivTrace.ProcessedFile = ivTrace.Folder + "\\" + ivTrace.TraceName + "_" + ivTrace.Id.ToString().PadLeft(4, '0'); ;
                        context.SaveChanges();
                    }
                }


            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
            }

            try
            {
                if (File.Exists(@"C:\WinPython-64bit-2.7.10.3\python-2.7.10.amd64\python.exe") && runPython)
                {
                    var header = RunPythonIV(ivTrace);

                    using (LabContext context = new LabContext())
                    {
                        ivTrace = (from x in context.TunnelIV where x.Id == ivTrace.Id select x).First();
                        ivTrace.ProcessedFile = header["ProcessedFile"];
                        ivTrace.GapSize = StripUnit(header["calc_Gap"]);
                        ivTrace.Fit = (from x in header["fitParam"].Split(new string[] { "[", "]", " " }, StringSplitOptions.RemoveEmptyEntries) select double.Parse(x)).ToArray();

                        try
                        {
                            ivTrace.ExpectedConductance = StripUnit(header["Conductance"]);
                            ivTrace.RunTime = StripUnit(header["traceTime"]);
                            ivTrace.Capacitance = StripUnit(header["Capacitance_Corrected"]);
                            ivTrace.VoltageOffset = StripUnit(header["calc_voltsOffset"]);
                            ivTrace.CurrentOffset = StripUnit(header["Current_Shift"]);
                            ivTrace.dVdt = StripUnit(header["dvdt"]);
                            ivTrace.Conductance = StripUnit(header["Conductance"]);
                            ivTrace.SampleRate = StripUnit(header["sampleRate"]);
                            ivTrace.Dielectric = StripUnit(header["calc_dielectric"]);
                            ivTrace.GapPotential = StripUnit(header["calc_potentialGap"]);
                        }
                        catch { }
                        ivTrace.JunctionArea = StripUnit(header["calc_area"]);
                        context.SaveChanges();
                    }
                }
                else
                {
                    if (Path.GetExtension(ivTrace.Filename) == "")
                        DataModel.Fileserver.FileTrace.ConvertWeisiToBin(ivTrace, ivTrace.Filename);
                    else
                    {
                        if (Path.GetExtension(ivTrace.Filename).ToLower() == ".csv")
                        {
                            DataModel.Fileserver.FileTrace.ConvertKeithleyToBin(ivTrace, ivTrace.Filename);
                        }
                    }
                    //using (LabContext context = new LabContext())
                    //{
                    //    ivTrace = (from x in context.TunnelIV where x.Id == ivTrace.Id select x).First();

                    //    string filename = Path.GetFileNameWithoutExtension(ivTrace.Filename) + "_" + ivTrace.Id + Path.GetExtension(ivTrace.Filename);
                    //    string outFilename = App.ConvertFilePaths(@"s:\Research\TunnelSurfer\Queue\" + filename);
                    //    System.IO.File.Copy(ivTrace.Filename, outFilename);
                    //    ivTrace.Filename = @"s:\Research\TunnelSurfer\Queue\" + filename;
                    //    context.SaveChanges();
                    //}
                }
            }
            catch { }

            return ivTrace;
        }

        private static DataModel.Trace AddIVFile(Experiment experiment, string filename, Batch batch = null, Chip chip = null, Junction junction = null, Dictionary<string, string> traceParse = null, bool runPython = true)
        {
            Dictionary<string, string> dirParse = new Dictionary<string, string>();
            if (batch == null)
            {
                Tuple<Batch, Chip, Dictionary<string, string>> Tables = ParseDirectoryString(experiment, filename);
                if (chip == null)
                    chip = Tables.Item2;
                batch = Tables.Item1;
                dirParse = Tables.Item3;
            }

            if (traceParse == null)
                traceParse = ExistingParse.ParseFilenames(filename);

            if (junction == null && traceParse["junction"] != "")
                junction = AddJunction(traceParse, filename, chip);


            DateTime measuredDate = DateTime.Now;


            string ivName = traceParse["name"];
            Dictionary<string, string> parse = ExistingParse.ParsePeiString(filename);
            try
            {
                ivName = ivName.Replace(parse["batchChunk"], "");
            }
            catch { }

            ivName = ivName.Replace(batch.BatchName, "").Replace(chip.ChipName, "");

            if (junction != null)
                ivName = ivName.Replace(junction.JunctionName, "");

            try
            {
                ivName = ivName.Replace(parse["cycles"] + "cyc", "");
                var p = parse["batchChunk"].Split('_');
                ivName = ivName.Replace(p[0], "");
            }
            catch { }

            if (ivName.StartsWith("201") == true)
                ivName = ivName.Substring(9);

            ivName = ivName.Replace("__", "").Replace("__", "");
            string folder = "";
            if (junction != null)
                folder = junction.Folder;
            else
                folder = chip.Folder;

            if ((filename.ToLower().Contains("ionic") || filename.ToLower().Contains("ion")) && filename.ToLower().Contains("tunnel") == false)
            {
                return AddIonic(traceParse, folder, measuredDate, ivName, chip, junction, runPython);
            }
            else
            {
                return AddTunnelTrace(traceParse, folder, measuredDate, ivName, chip, junction, runPython);
            }
        }

        public static DataModel.Trace AddFile(string Experiment, string filename, Dictionary<string, string> traceParse, Dictionary<string, string> directoryParse, bool runPython)
        {
            Experiment exp;
            using (LabContext context = new LabContext())
            {
                exp = (from x in context.Experiments where x.ExperimentName == Experiment select x).First();
            }

            if (traceParse.ContainsKey("isIV") && traceParse["isIV"] == "True")
                return AddIVFile(exp, filename, traceParse, directoryParse, runPython);
            else
                return AddFile(exp, filename, traceParse, directoryParse, runPython);
        }

        public static void AddFile(int dbIndex, string filename)
        {
            using (LabContext context = new LabContext())
            {
                var trace = (from x in context.Traces where x.Id == dbIndex select x).FirstOrDefault();
                if (trace != null)
                {
                    trace.Filename = filename;
                    if (File.Exists(trace.RawFilename) == false)
                        PreprocessFile(trace);
                    RunPython(trace);
                }
            }
        }

        #endregion

        private static bool isControl(string filename)
        {
            filename = filename.ToLower();
            return filename.Contains("control") || filename.Contains("rinse");
        }

        private static bool isBuffer(string filename)
        {
            filename = filename.ToLower();
            return filename.Contains("pb") || filename.Contains("kcl");
        }

        private static void AddDirectory(Experiment experiment, string directoryPath, bool runPython = true)
        {

            string[] datafiles = Directory.GetFiles(directoryPath, "*.abf", SearchOption.AllDirectories);
            List<string> sortDataFiles = new List<string>();
            sortDataFiles.AddRange((from x in datafiles where ExistingParse.isControl(x) select x));
            sortDataFiles.AddRange((from x in datafiles where ExistingParse.isBuffer(x) select x));
            sortDataFiles.AddRange((from x in datafiles where x.ToLower().Contains("control") == false select x));
            datafiles = (from x in sortDataFiles where x.ToLower().Contains("brian") == false select x).ToArray();

            foreach (var filename in datafiles)
            {
                if (filename.Contains(".abf"))
                {
                    var Tables = ParseDirectoryString(experiment, filename);
                    var traceParse = ExistingParse.ParseFilenames(filename);
                    var junction = AddJunction(traceParse, filename, Tables.Item2);
                }
            }

            foreach (var filename in datafiles)
            {
                if (isControl(filename) || isBuffer(filename))
                {
                    CommonExtensions.DoEvents();
                    if (filename.Contains(".abf"))
                    {
                        try
                        {
                            var Tables = ParseDirectoryString(experiment, filename);
                            AddFile(experiment, filename, Tables.Item1, Tables.Item2, null, null, runPython);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.Print(ex.Message);
                        }
                    }
                }
            }

            // foreach (var filename in datafiles)
            Parallel.ForEach(datafiles, new ParallelOptions { MaxDegreeOfParallelism = 7 }, filename =>
            {

                //CommonExtensions.DoEvents();
                System.Diagnostics.Debug.Print(string.Format("Processing {0} on thread {1}", filename, Thread.CurrentThread.ManagedThreadId));
                if (filename.ToLower().Contains("control") == false && filename.ToLower().Contains("rinse") == false)
                {
                    System.Diagnostics.Debug.Print("");
                }
                if (filename.Contains(".abf"))
                {
                    try
                    {
                        var Tables = ParseDirectoryString(experiment, filename);
                        AddFile(experiment, filename, Tables.Item1, Tables.Item2, null, null, runPython);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print(ex.Message);
                    }
                }
            }
           );

            string[] IVfiles = Directory.GetFiles(directoryPath, "*iv*", SearchOption.AllDirectories);


            foreach (string s in IVfiles)
            {

                try
                {
                    var Tables = ParseDirectoryString(experiment, s);
                    AddIVFile(experiment, s, Tables.Item1, Tables.Item2, null, null, runPython);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                }
            }
        }

        public static void ParseExperimentFolder(Experiment experiment, string folderName, bool runPython = true)
        {
            if (Directory.Exists(folderName))
            {
                AddDirectory(experiment, folderName, runPython);
            }
            else if (File.Exists(folderName))
            {
                ParseExperimentFile(experiment, folderName, runPython);
            }
        }

        private static void ParseExperimentFile(Experiment experiment, string fileName, bool runPython = true)
        {
            if (Path.GetExtension(fileName).ToLower().Contains("abf") == true)
                AddFile(experiment, fileName, null, null, null, null, runPython);
            else
                AddIVFile(experiment, fileName, null, null, null, null, runPython);
        }
    }
}
