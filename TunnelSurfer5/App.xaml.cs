using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using TunnelVision.DataModel;

namespace TunnelVision
{
    public class SQLiteConnectionFactory : System.Data.Entity.Infrastructure.IDbConnectionFactory
    {
        public System.Data.Common.DbConnection CreateConnection(string connectionString)
        {
            return new System.Data.SQLite.SQLiteConnection(connectionString);
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool ForceHighQuality = false;
        public static bool DoneDrawing = false;


        public static string DataDirectory = "s:\\research\\tunnelsurfer";
#if Travel
        public static string ConnectionString = "";// = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=C:\Users\bri\Desktop\TunnelVision\TunnelSurfer5\Data\TravelServer.mdf;Integrated Security=True;Connect Timeout=30";
                                                   //  public static   System.Data.SQLite.SQLiteConnection SQLConnection;
#else
        public static string ConnectionString = @"server=10.212.28.42;port=3307;database=tunnelsurfer4;uid=test;password=12Dnadna;Convert Zero Datetime=True;";
#endif
        public static string DropBoxFolder = "";
        private static void FindDropbox()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(
                                           Environment.SpecialFolder.LocalApplicationData);
                if (File.Exists(System.IO.Path.Combine(appDataPath, "Dropbox\\info.json")))
                {
                    string json = File.ReadAllText(System.IO.Path.Combine(appDataPath, "Dropbox\\info.json"));
                    var lines = json.Split('\n', '{', ',');
                    var line = (from x in lines where x.Contains("path") select x).ToList();



                    string pPath = line.First();
                    if (line.Count > 1)
                    {
                        var l2 = (from x in line where x.Contains("ASU") select x).FirstOrDefault();
                        if (l2 != null)
                            pPath = l2;
                    }

                    lines = pPath.Split(':');
                    DropBoxFolder = (lines[1] + ":" + lines[2]).Replace("\"", "").Replace("\\\\", "\\") + "\\TunnelVision";

                }
                else
                {

                    string dbPath = System.IO.Path.Combine(appDataPath, "\\Dropbox\\host.db");
                    string[] lines = System.IO.File.ReadAllLines(dbPath);
                    byte[] dbBase64Text = Convert.FromBase64String(lines[1]);
                    DropBoxFolder = System.Text.ASCIIEncoding.ASCII.GetString(dbBase64Text) + "\\TunnelVision";
                }
            }
            catch
            {
                DropBoxFolder = "";
            }
        }

        public static string ConvertFilePaths(string filepath)
        {
            var ff2 = filepath.ToLower().Replace(@"s:\research", DataDirectory);
            if (Directory.Exists(Path.GetDirectoryName(ff2)) == true)
                return ff2;


            if (Directory.Exists(@"s:\research\tunnelsurfer"))
                return filepath;


            ff2 = filepath.ToLower().Replace(@"s:\research\", "\\\\biofs.asurite.ad.asu.edu\\smb\\research\\");
            if (Directory.Exists("\\\\biofs.asurite.ad.asu.edu\\smb\\research\\") == true)
                return ff2;


            ff2 = filepath.ToLower().Replace(@"s:\research\tunnelsurfer\experiments", App.DropBoxFolder);
            if (Directory.Exists(App.DropBoxFolder))
                return ff2;

            return ff2;
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            //    DataModel.Fileserver.ABFLoader.ReadBBF(@"c:\temp\pTest.bbf");

            //DataModel.Fileserver.ABFLoader.ConvertABF_BBF(
            //    @"S:\Research\Stacked Junctions\Results\20160304_NALDB34_Chip03_22cyc_HIM3pC_Remodified_Repeated\03PolyT50_Beads\M1_M2G_TopG_Bttm0mV_P350mV_100nMPolyT50_Beads_0000.abf", "", @"c:\temp\te.bbf");

            //DataModel.Fileserver.FileTrace.ReadBBF(@"c:\temp\te.bbf");

            //   DataModel.Fileserver.FileTrace.ConvertBSN_FLAC("", @"S:\Research\TunnelSurfer\Experiments\Protein\NALDB_Reuse\Chip_01\M1_M4\New Trace_00092706_Ionic.bsn", @"S:\Research\TunnelSurfer\Experiments\Protein\NALDB_Reuse\Chip_01\M1_M4\New Trace_00092706_M1_M4_Full.bbf", "test");
            //   DataModel.Fileserver.FileTrace.ReadBBF(@"S:\Research\TunnelSurfer\Experiments\Protein\NALDB_Reuse\Chip_01\M1_M4\New Trace_00092706_M1_M4_Full.bbf");

            //DataModel.Fileserver.FileTrace.ConvertBSN_FLAC("", @"S:\Research\TunnelSurfer\Experiments\Protein\NALDB_Reuse\Chip_01\M1_M4\New Trace_00042691_M1_M4\_2691_M1_M4_Full.bsn",
            //   @"S:\Research\TunnelSurfer\Experiments\Protein\NALDB_Reuse\Chip_01\M1_M4\New Trace_00042691_M1_M4\_2691_M1_M4_Full", "test", null);
            //DataModel.Fileserver.FileTrace.ReadBBF2(@"S:\Research\TunnelSurfer\Experiments\Protein\NALDB_Reuse\Chip_01\M1_M4\New Trace_00042691_M1_M4\_2691_M1_M4_Full.bbf2");

            //TunnelVision.DataModel.Fileserver.ABFLoader.ConvertABF_FLAC(
            //    @"S:\Research\Stacked Junctions\Results\20160608_NALDBT01_Chip04_22cyc_HIM_R20nm_5pC\M1_M3G_TopG_Bttm0mV_P350mV_1mMPB_0000.abf", "",
            //    @"S:\Research\Stacked Junctions\Results\20160608_NALDBT01_Chip04_22cyc_HIM_R20nm_5pC\M1_M3G_TopG_Bttm0mV_P350mV_1mMPB_0000.flac");

            if (System.IO.Directory.Exists(@"s:\research\") == false)
            {
                DataDirectory = "\\\\biofs.asurite.ad.asu.edu\\smb\\research\\tunnelsurfer";
                if (System.IO.Directory.Exists("\\\\biofs.asurite.ad.asu.edu\\smb\\research\\") == false)
                {
                    System.Windows.Forms.MessageBox.Show("Please connect ASU VPN so file system is exposed.");
                    Application.Current.Shutdown();
                }
            }


            if (System.Environment.MachineName == "BIOD0455")
            {
                ExecuteExample();
            }
            FindDropbox();
#if Travel
#if MSDB
            ConnectionString = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=" + DropBoxFolder + @"\Database\TravelServer.mdf;Integrated Security=True;Connect Timeout=30";
#else
            ConnectionString = new System.Data.SQLite.SQLiteConnectionStringBuilder() { DataSource = DropBoxFolder + @"\Database\TunnelVision.sqlite3" }.ConnectionString;// @"Data Source=" + DropBoxFolder + @"\Database\TunnelVision.sqlite3";
                                                                                                                                                                          //  SQLConnection = new System.Data.SQLite.SQLiteConnection(ConnectionString);
#endif                                                                                                                                                                             //SQLConnection = new System.Data.SQLite.SQLiteConnection()

#else


#endif




            base.OnStartup(e);

        }

        protected override void OnExit(ExitEventArgs e)
        {

            //var xs = new System.Xml.Serialization.XmlSerializer(typeof(Dictionary<string,string>));
            //Dictionary<string, string> ints = new Dictionary<string,string>();

            //using (FileStream fs = new FileStream(@"C:\store.xml", FileMode.OpenOrCreate))
            //{
            //    xs.Serialize(fs, ints);
            //}

            base.OnExit(e);
        }

        //public static void ExecuteFileExample()
        //{
        //    string connectionString = "server=10.212.28.42;port=3307;database=fileserver3;uid=test;password=12Dnadna";

        //    using (MySqlConnection connection = new MySqlConnection(connectionString))
        //    {
        //        // Create database if not exists
        //        bool createDatabase = false;
        //        using (DataModel.Fileserver.FileContext contextDB = new DataModel.Fileserver.FileContext(connection, false))
        //        {
        //            createDatabase = contextDB.Database.CreateIfNotExists();
        //        }

        //        if (createDatabase)
        //        {
        //            connection.Open();
        //            MySqlTransaction transaction = connection.BeginTransaction();

        //            try
        //            {
        //                // DbConnection that is already opened
        //                using (DataModel.Fileserver.FileContext context = new DataModel.Fileserver.FileContext(connection, false))
        //                {

        //                    // Interception/SQL logging
        //                    context.Database.Log = (string message) => { Console.WriteLine(message); };

        //                    // Passing an existing transaction to the context
        //                    context.Database.UseTransaction(transaction);

        //                    // DbSet.AddRange
        //                    context.FakeData();
        //                }

        //                transaction.Commit();
        //            }
        //            catch
        //            {
        //                transaction.Rollback();
        //                throw;
        //            }
        //        }
        //    }
        //}

        public static void ExecuteExample()
        {
            string connectionString = "server=10.212.28.42;port=3307;database=tunnelsurfer4;uid=test;password=12Dnadna;Convert Zero Datetime=True;";

            //using (MySqlConnection connection = new MySqlConnection(connectionString))
            //{
            //    // Create database if not exists
            //    bool createDatabase = false;
            //    using (LabContext contextDB = new LabContext(connection, false))
            //    {
            //        createDatabase = contextDB.Database.CreateIfNotExists();
            //       // contextDB.Database.Create();

            //        var brians = (from x in contextDB.Traces where x.Filename.ToLower().Contains("brain") select x).ToList();
            //        for (int i = 0; i < brians.Count; i++)
            //        {
            //            contextDB.Traces.Remove(brians[i]);

            //        }
            //        contextDB.SaveChanges();

            //    }

            //    if (createDatabase)
            //    {
            //        connection.Open();
            //        MySqlTransaction transaction = connection.BeginTransaction();

            //        try
            //        {
            //            // DbConnection that is already opened
            //            using (LabContext context = new LabContext(connection, false))
            //            {

            //                // Interception/SQL logging
            //                context.Database.Log = (string message) => { Console.WriteLine(message); };

            //                // Passing an existing transaction to the context
            //                context.Database.UseTransaction(transaction);

            //                // DbSet.AddRange
            //                context.FakeData();
            //            }

            //            transaction.Commit();
            //        }
            //        catch
            //        {
            //            transaction.Rollback();
            //            throw;
            //        }
            //    }
            //}
        }

    }
}
