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

namespace TunnelVision
{
    /// <summary>
    /// Interaction logic for AddBatchWindow.xaml
    /// </summary>
    public partial class AddChipWindow : MetroWindow
    {
        public AddChipWindow()
        {
            InitializeComponent();
        }

        private DataModel.Experiment experiment;
        public DataModel.Experiment Experiment
        {
            get { return experiment; }
            set
            {
                experiment = value;

            }
        }

        public DataModel.Batch Batch { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            using (DataModel.LabContext labContext = new DataModel.LabContext())
            {
                var exp = (from x in labContext.Experiments where x.Id == experiment.Id select x).First();
                var batch = (from x in exp.Batches where x.Id == Batch.Id select x).First();
                var note = new DataModel.eNote { NoteTime = DateTime.Now };
                note.ConvertString(Notes.Text);
                int numPores = 3;

                int.TryParse(numberPores.Text, out numPores);
                int nJunctions = 3;
                int.TryParse(JunctionChips.Text, out nJunctions);
                int cycles = 22;
                int.TryParse(NumberCycles.Text, out cycles);


                string chipName = ChipName.Text;
                string dir = batch.Folder + "\\" + chipName;
                try
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                catch
                { }
                var chip = new DataModel.Chip
                    {
                        ChipName = chipName,
                        DrillMethod = ((ListBoxItem)DrillMethod.SelectedItem).Content.ToString(),
                        NumberOfJunctions = nJunctions,
                        NumberPores = nJunctions,
                        Batch = batch,
                        Experiment = exp,
                        ALDCycles = cycles,
                        ALDMaterial = ((ListBoxItem)ALDMaterials.SelectedItem).Content.ToString(),
                        JunctionMaterial = ((ListBoxItem)JunctionMaterials.SelectedItem).Content.ToString(),
                        GoodChip = DataModel.UserRating.Unrated,
                        PoreSize = 10,
                        Folder = dir
                    };

                var junctions = new List<DataModel.Junction>();
                for (int j = 0; j < nJunctions; j++)
                {
                    string jName = "M" + (j+2  ).ToString() + "_M1";
                    junctions.Add(
                        new DataModel.Junction
                        {
                            Chip = chip,
                            Experiment = exp,
                            JunctionName = jName,
                            Folder = dir + "\\" + jName
                        });
                    try
                    {
                        System.IO.Directory.CreateDirectory(dir + "\\" + jName);
                    }
                    catch
                    { }
                }
                chip.Junctions = junctions;
                batch.Chips.Add(chip);
               
                labContext.SaveChanges();
                this.Close();
            }
        }
    }
}
