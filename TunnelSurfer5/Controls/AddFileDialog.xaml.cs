using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace TunnelVision.Controls
{
    /// <summary>
    /// Interaction logic for AddFileDialog.xaml
    /// </summary>
    public partial class AddFileDialog : MetroWindow
    {
        public AddFileDialog()
        {
            InitializeComponent();

        }

        private string[] Filenames;
        private int cFilename = 0;
        string experimentName;
        string batchName;
        string chipName;
        string junctionName;
        public void ShowFilenames(string experimentName, string batchName, string chipName, string junctionName, string[] Filenames)
        {
            this.Filenames = Filenames;
            cFilename = 0;
            if (experimentName != "")
                this.experimentName = experimentName;
            if (batchName != "")
                this.batchName = batchName;
            if (chipName != "")
                this.chipName = chipName;
            if (junctionName != "")
                this.junctionName = junctionName;
            DisplayFile(Filenames[cFilename]);
        }

        private void DisplayFile(string filename)
        {

            var AllParse = DataModel.ExistingParse.ParsePeiString(Filenames[cFilename]);
            var fileParse = DataModel.ExistingParse.ParseFilenames(Filenames[cFilename]);

            if (filename.ToLower().Contains("control") == true || filename.ToLower().Contains("rinse") == true)
                isControl.IsChecked = true;

            bool _isIonic = (filename.ToLower().Contains("ionic") == true);
            bool _isIV = (filename.ToLower().Contains("iv") == true);

            isIonic.IsChecked = _isIonic;
            isSweep.IsChecked = _isIV;
            tbExperiment.Text = experimentName;

            tbFilename.Text = filename;
            try
            {
                tbBatchName.Text = AllParse["batch"];
            }
            catch
            {
                tbBatchName.Text = batchName;
            }
            try
            {
                tbChipName.Text = AllParse["chip"];
            }
            catch
            {
                tbChipName.Text = chipName;
            }
            try
            {
                tbALDCycles.Text = AllParse["cycles"];
            }
            catch
            {
                tbALDCycles.Text = "";
            }

            try
            {
                tbNotes.Text = AllParse["notes"];
            }
            catch
            {
                tbNotes.Text = "";
            }

            tbExperiment.Text = "DNA";

            try
            {
                tbTopReference.Text = fileParse["TopRef"];
                if (tbTopReference.Text == "-1000")
                    tbTopReference.Text = "float";
                if (tbTopReference.Text == "-2000")
                    tbTopReference.Text = "none";
                if (tbTopReference.Text == "-3000")
                    tbTopReference.Text = "sweep";
            }
            catch
            {
                if (_isIonic && _isIV)
                    tbTopReference.Text = "sweep";
                else
                    if (_isIV)
                        tbTopReference.Text = "none";
                    else
                        tbTopReference.Text = "float";
            }


            try
            {
                tbBttmRef.Text = fileParse["bttmRef"];
                if (tbBttmRef.Text == "-1000")
                    tbBttmRef.Text = "float";
                if (tbBttmRef.Text == "-2000")
                    tbBttmRef.Text = "none";
                if (tbBttmRef.Text == "-3000")
                    tbBttmRef.Text = "sweep";
            }
            catch
            {
                if (_isIonic && _isIV)
                    tbBttmRef.Text = "0";
                else
                    if (_isIV)
                        tbBttmRef.Text = "none";
                    else
                        tbBttmRef.Text = "float";
            }

            try
            {
                tbAnalyte.Text = fileParse["analyte"];
            }
            catch
            {
                tbAnalyte.Text = "";
            }

            try
            {
                tbConcentration.Text = fileParse["concentration"];
            }
            catch
            {
                tbConcentration.Text = "";
            }

            try
            {
                tbBuffer.Text = fileParse["buffer"];
            }
            catch
            {
                tbBuffer.Text = "PB";
            }

            try
            {
                tbTunnel.Text = fileParse["tunnelvoltage"];
                if (tbTunnel.Text == "-1000")
                    tbTunnel.Text = "float";
                if (tbTunnel.Text == "-2000")
                    tbTunnel.Text = "none";
                if (tbTunnel.Text == "-3000")
                    tbTunnel.Text = "sweep";
            }
            catch
            {
                if (_isIonic && _isIV)
                    tbTunnel.Text = "float";
                else
                    if (_isIV)
                        tbTunnel.Text = "sweep";
                    else
                        tbTunnel.Text = "float";
            }

            try
            {
                tbJunction.Text = fileParse["junction"];
            }
            catch
            {
                tbJunction.Text = junctionName;
            }


            try
            {
                tbTraceName.Text = fileParse["name"];
            }
            catch
            {
                tbTraceName.Text = "";
            }


            try
            {
                tbDrillMethod.Text = AllParse["drillChunk"];
            }
            catch
            {
                tbDrillMethod.Text = "";
            }


        }


        private void AddOrReplace(ref Dictionary<string, string> dict, string key, string value)
        {
            if (dict.ContainsKey(key) == true)
            {
                dict[key] = value;
            }
            else
                dict.Add(key, value);
        }

        private string VoltageTextToNumber(string text)
        {
            return DataModel.ExistingParse.ConvertVoltageToDouble(text).ToString();
            //text = text.ToLower();
            //if (text == "float")
            //    return "-1000";
            //else
            //    if (text == "none")
            //        return "-2000";
            //    else
            //        if (text == "sweep")
            //            return "-3000";
            //return text;
        }

        private void SaveFile(string filename)
        {
            var AllParse = DataModel.ExistingParse.ParsePeiString(Filenames[cFilename]);
            var fileParse = DataModel.ExistingParse.ParseFilenames(Filenames[cFilename]);

            AddOrReplace(ref AllParse, "batch", tbBatchName.Text);
            AddOrReplace(ref AllParse, "chip", tbChipName.Text);
            AddOrReplace(ref AllParse, "cycles", tbALDCycles.Text);
            AddOrReplace(ref AllParse, "notes", tbNotes.Text);
            AddOrReplace(ref AllParse, "drillChunk", tbDrillMethod.Text);
            AddOrReplace(ref fileParse, "isControl", isControl.IsChecked.ToString());
            AddOrReplace(ref fileParse, "experiment", tbExperiment.Text);
            AddOrReplace(ref fileParse, "TopRef", VoltageTextToNumber(tbTopReference.Text));
            AddOrReplace(ref fileParse, "bttmRef", VoltageTextToNumber(tbBttmRef.Text));
            AddOrReplace(ref fileParse, "tunnelvoltage", VoltageTextToNumber(tbTunnel.Text));
            AddOrReplace(ref fileParse, "analyte", tbAnalyte.Text);
            AddOrReplace(ref fileParse, "concentration", tbConcentration.Text);
            AddOrReplace(ref fileParse, "buffer", tbBuffer.Text);
            AddOrReplace(ref fileParse, "junction", tbJunction.Text);
            AddOrReplace(ref fileParse, "name", tbTraceName.Text);
            AddOrReplace(ref fileParse, "isIonic", isIonic.IsChecked.ToString());
            AddOrReplace(ref fileParse, "isIV", isSweep.IsChecked.ToString());

            var trace=   DataModel.ExistingParse.AddFile(tbExperiment.Text, filename, fileParse, AllParse,false );

            MessageBox.Show("File is converting on server, please wait 1-2 minutes.");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            tbMessage.Text = "Saving, please wait.";
            SaveFile(Filenames[cFilename]);
            tbMessage.Text = "New Item";
            cFilename++;
            if (cFilename < Filenames.Length)
                DisplayFile(Filenames[cFilename]);
            else
                this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            cFilename++;
            if (cFilename < Filenames.Length)
                DisplayFile(Filenames[cFilename]);
            else
                this.Close();
        }
    }
}
