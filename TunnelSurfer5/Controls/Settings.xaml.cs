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
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : MetroWindow
    {
        public Settings()
        {
            InitializeComponent();
           
        }

        private DataModel.Experiment[] experiments;
        public DataModel.Experiment[] Experiments
        {
            get { return experiments; }
            set
            {
                var e = new List<DataModel.Experiment>();
                e.AddRange(value);
                var eN = new DataModel.Experiment{ 
                    Id=-1,
                    ExperimentName="New", 
                    CalendarEvents=new List<DataModel.CalendarGoals>()
                };
                e.Add(eN);
                experiments = e.ToArray();
                CurrentExperiment = eN;

                DataContext = new
                {
                    Experiments = experiments,
                    CurrentExperiment = eN,
                    NoteText = NoteText
                };
            }
        }

        private void AddExperiment_Click(object sender, RoutedEventArgs e)
        {
            using (DataModel.LabContext labContext= new DataModel.LabContext())
            {
                var note = new DataModel.eNote { NoteTime = DateTime.Now };
                note.ConvertString(Instructions.Text);
               
                if (CurrentExperiment.Id > -1)
                {
                    var exp = (from x in labContext.Experiments where x.Id == CurrentExperiment.Id select x).First();
                    exp.ExperimentName = NewExperiment.Text;
                    exp.Notes.Add(note);
                   
                }
                else{

                    var newExp = new DataModel.Experiment { ExperimentName = NewExperiment.Text,  Notes = new List<DataModel.eNote> { note } };
                    labContext.Experiments.Add(newExp);
                }
                labContext.SaveChanges();
            }
            using (DataModel.LabContext labContext = new DataModel.LabContext())
            {
                Experiments = labContext.Experiments.ToArray();
            }
            this.Close();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.Print(sender.ToString());
        }

        public DataModel.Experiment CurrentExperiment { get; set; }

        public string NoteText { get; set; }

        private void lstBxTask_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var list = (ListBox)sender;
            DataContext = new
            {
                Experiments = experiments,
                CurrentExperiment =(DataModel.Experiment) list.SelectedItems[0],
                NoteText = NoteText
            };

            if (CurrentExperiment.Id > -1)
                addText.Text = "Save Experiment";
            else
                addText.Text = "Add Experiment";
        }

        private void AddStep_Click(object sender, RoutedEventArgs e)
        {
           // CurrentExperiment.ControlSteps.Add((DataModel.QualityControlStep)controlSteps.SelectedItems[0]);
            DataContext = new
            {
                Experiments = experiments,
                CurrentExperiment = CurrentExperiment
            };
        }

        private void controlSteps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //DataModel.QualityControlStep qc = (DataModel.QualityControlStep)controlSteps.SelectedItems[0];
            //if (qc.Id ==0 )
            //{
            //    addQC.Text = "Add Step";
            //}
            //else
            //{
            //    addQC.Text = "Save Edit";
            //}
        }
    }
}
