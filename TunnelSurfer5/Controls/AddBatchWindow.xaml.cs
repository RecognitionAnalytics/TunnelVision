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
    public partial class AddBatchWindow : MetroWindow
    {
        public AddBatchWindow()
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            using (DataModel.LabContext labContext = new DataModel.LabContext())
            {
                var exp = (from x in labContext.Experiments where x.Id == experiment.Id select x).First();
                var note = new DataModel.eNote { NoteTime = DateTime.Now };
                note.ConvertString(Notes.Text);

                int nChips = 10;
                int.TryParse(NumberChips.Text, out nChips);
                int nJunctions = 3;
                int.TryParse(JunctionChips.Text, out nJunctions);
                int cycles = 22;
                int.TryParse(NumberCycles.Text, out cycles);

                string dir = "S:\\Research\\TunnelSurfer\\Experiments\\" + exp.ExperimentName + "\\" + BatchName.Text;
                var batch = new DataModel.Batch
                {
                    Experiment = exp,
                    BatchName = BatchName.Text,
                    Notes = new List<DataModel.eNote> { note },
                    DeliveryDate = DateTime.Now,

                    ALDMaterial = ((ListBoxItem)ALDMaterials.SelectedItem).Content.ToString(),
                    ALDCycles = cycles,
                    JunctionMaterial = ((ListBoxItem)JunctionMaterials.SelectedItem).Content.ToString(),
                    DrillMethod = ((ListBoxItem)DrillMethod.SelectedItem).Content.ToString(),
                    NumberOfDeliveredChips = nChips,
                    ManufactorDate = ManufactorDate.DisplayDate,
                    Folder = dir
                };
                
                try
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                catch
                { }

                var chips = new List<DataModel.Chip>();
                for (int i = 0; i < nChips; i++)
                {
                    string chipName ="Chip_" + (i+1).ToString().PadLeft(2, '0');
                    dir = "S:\\Research\\TunnelSurfer\\Experiments\\" + exp.ExperimentName + "\\" + BatchName.Text + "\\" + chipName ;
                    try
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                    catch
                    { }
                    var chip = new DataModel.Chip
                        {
                            ChipName = chipName ,
                            DrillMethod = ((ListBoxItem)DrillMethod.SelectedItem).Content.ToString(),
                            NumberOfJunctions = nJunctions,
                            NumberPores = nJunctions,
                            Batch = batch,
                            Experiment = exp,
                            ALDCycles=cycles,
                            ALDMaterial = ((ListBoxItem)ALDMaterials.SelectedItem).Content.ToString(),
                            JunctionMaterial = ((ListBoxItem)JunctionMaterials.SelectedItem).Content.ToString(),
                            GoodChip= DataModel.UserRating.Unrated,
                            PoreSize=10,
                            Folder  =dir
                        };

                    var junctions = new List<DataModel.Junction>();
                    for (int j = 0; j < nJunctions; j++)
                    {
                        string jName ="M" + (j+1).ToString() + "_M4";
                        junctions.Add(
                            new DataModel.Junction
                            {
                                Chip = chip,
                                Experiment = exp,
                                JunctionName = jName,
                                Folder = dir + "\\" +  jName
                            });
                        try
                        {
                            System.IO.Directory.CreateDirectory(dir + "\\" + jName);
                        }
                        catch
                        { }
                    }
                    chip.Junctions = junctions;
                    chips.Add(chip);
                }
                batch.Chips = chips;
                exp.Batches.Add(batch);
                labContext.SaveChanges();
                this.Close();
            }
        }
    }
}
