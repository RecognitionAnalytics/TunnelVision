using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision
{
    public class AppLogger
    {
        public static void LogError(Exception ex)
        {
            try
            {
                if (Directory.Exists(@"C:\temp") == false)
                    Directory.CreateDirectory(@"C:\temp");

                string error = DateTime.Now.ToShortTimeString() + "::" + ex.Message + "\n" + ex.StackTrace + "\n";
                try
                {
                    error += ex.InnerException.Message + "\n";
                }
                catch
                { }

                File.AppendAllText(@"c:\temp\errorLog.txt", error);

                System.Diagnostics.Debug.Print(ex.Message);
                System.Diagnostics.Debug.Print(ex.StackTrace);
                try
                {
                    System.Diagnostics.Debug.Print(ex.InnerException.Message);
                }
                catch
                { }
            }
            catch
            {
               // System.Windows.Forms.MessageBox.Show("Cannot create log file");
            }
        }
    }
}
