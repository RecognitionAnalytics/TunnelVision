using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TunnelVision
{
    public delegate void ItemSelectedEventHandler(DataExplorerView sender, object e);
    /// <summary>
    /// Interaction logic for DataExplorerView.xaml
    /// </summary>
    public partial class DataExplorerView : UserControl
    {
        private object CurrentDataObject = null;

        public event ItemSelectedEventHandler ItemSelected;

        public event EventHandler RefreshData;

        public DataExplorerView()
        {
            try
            {
                Experiments = new ObservableCollection<Controls.TreeViewModel.ExperimentVM>();
                DataContext = this;
                InitializeComponent();
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private DataModel.Experiment[] ExperimentsEF;

        public ObservableCollection<TunnelVision.Controls.TreeViewModel.ExperimentVM> Experiments { get; set; }

        public void SetData(DataModel.Experiment[] experiments, bool quiet = false)
        {
            try
            {
                ExperimentsEF = experiments;
                Experiments.Clear();
                foreach (var exp in experiments)
                {
                    try
                    {
                        var expVM = new Controls.TreeViewModel.ExperimentVM(exp);
                        Experiments.Add(expVM);
                        if (exp.ExperimentName == (string)Properties.Settings.Default["DefaultExperiment"] && quiet == false)
                        {
                            expVM.Clicked();
                            if (ItemSelected != null)
                            {
                                this.ItemSelected(this, expVM);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError(ex);
                    }
                }
            }
            catch { }
        }

        private void ExpandAll(Controls.TreeViewModel.TreeNodeVM item)
        {
            item.OpenList = true;
            item.Clicked();
            App.DoneDrawing = false;
            this.ItemSelected(this, item);

            for (int i = 0; i < 10; i++)
            {
                CommonExtensions.DoEvents();
            }
            if (item.GetType() == typeof(Controls.TreeViewModel.DataTraceVM))
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (App.DoneDrawing == false && sw.Elapsed < new TimeSpan(0, 0, 20))
                {
                    CommonExtensions.DoEvents();
                }
                sw.Stop();
            }
            else
            {
                if (item.Children != null)
                    for (int i = 0; i < item.Children.Count; i++)
                    {
                        try
                        {
                            ExpandAll((Controls.TreeViewModel.TreeNodeVM)item.Children[i]);
                        }
                        catch { }
                    }
            }
        }

        public static object GetPropValue(object src, string propName)
        {
            try
            {
                return src.GetType().GetProperty(propName).GetValue(src, null);
            }
            catch { }
            return null;
        }

        Brush b;

        private void LoadOld(DataModel.Experiment exp)
        {
            try
            {
                if (true)
                {

                  
                    exp = (from x in ExperimentsEF where x.ExperimentName.ToLower() == "protein" select x).FirstOrDefault();
                    if (exp.ExperimentName.ToLower() == "protein")
                    {
                        DataModel.ExistingParse.ParseExperimentFolder(exp, @"S:\Research\Stacked Junctions\Proteins", false);
                        DataModel.ExistingParse.ParseExperimentFolder(exp, @"C:\Users\bashc\Dropbox (Personal)\integrin data\Integrin binding data_HIM", false);
                        DataModel.ExistingParse.ParseExperimentFolder(exp, @"C:\Users\bashc\Dropbox (Personal)\integrin data\Integrin binding data_FIB", false);
                        

                    }

                    exp = (from x in ExperimentsEF where x.ExperimentName.ToLower() == "dna" select x).FirstOrDefault();
                    if (exp.ExperimentName == "DNA")
                    {
                        #region DNA
                        //try
                        {
                            DataModel.ExistingParse.ParseExperimentFolder(exp, @"S:\Research\Stacked Junctions\Results", false);
                        }
                        // catch { }


                        #endregion
                    }

                }
            }
            catch { }
        }

        private Controls.TreeViewModel.TreeNodeVM selectedItem = null;

        private void SendData(Controls.TreeViewModel.TreeNodeVM item)
        {
            try
            {
                if (item != null && selectedItem != item)
                {
                    item.Clicked();
                    selectedItem = item;
                    if (ItemSelected != null)
                    {
                        try
                        {
                            this.ItemSelected(this, selectedItem);
                        }
                        catch (Exception ex)
                        {
                            AppLogger.LogError(ex);
                        }
                    }
                    if (item.GetType() == typeof(Controls.TreeViewModel.ExperimentVM) && System.Environment.MachineName == "BIOD0455")
                    {
                       // LoadOld(((Controls.TreeViewModel.ExperimentVM)selectedItem).Experiment());
                    }
                }
            }
            catch { }
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var item = ((Controls.TreeViewModel.TreeNodeVM)((System.Windows.Controls.TextBlock)sender).DataContext);
                SendData(item);
                if (e.ClickCount == 2)
                {
                    try
                    {
                        item.DoubleClicked();
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError(ex);
                    }
                }
                System.Diagnostics.Debug.Print(sender.ToString());
            }
            catch { }

        }

        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                b = ((TextBlock)sender).Background;
                ((TextBlock)sender).Background = Brushes.DarkGreen;
                CurrentDataObject = ((TextBlock)sender).DataContext;
            }
            catch { }
        }

        private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                ((TextBlock)sender).Background = b;
            }
            catch { }
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                //try
                //{
                var item = ((Controls.TreeViewModel.TreeNodeVM)((System.Windows.Controls.StackPanel)sender).DataContext);
                SendData(item);
                if (e.ClickCount == 2)
                {
                    try
                    {
                        item.DoubleClicked();
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError(ex);
                    }
                }
                System.Diagnostics.Debug.Print(sender.ToString());
                //}
                //catch (Exception ex)
                //{
                //    AppLogger.LogError(ex);
                //}
            }
            catch { }
        }

        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                b = ((StackPanel)sender).Background;
                ((StackPanel)sender).Background = Brushes.DarkGreen;
                CurrentDataObject = ((StackPanel)sender).DataContext;
            }
            catch { }
        }

        private void StackPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                ((StackPanel)sender).Background = b;
            }
            catch { }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var t = (System.Windows.Controls.DataGrid)sender;
                SendData((Controls.TreeViewModel.TreeNodeVM)t.SelectedItem);
                System.Diagnostics.Debug.Print(sender.ToString());
            }
            catch { }
        }

        private void OnListViewItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDataObject != null)
            {
                var item = (Controls.TreeViewModel.TreeNodeVM)CurrentDataObject;
                item.Delete();
                SetData(Controls.TreeViewModel.MainViewVM.GetDataModel());
            }
            System.Diagnostics.Debug.Print("");
        }

        private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.Print("");
        }

        private void TextBlock_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.Print("");
        }

        private void btnByDate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnByBatch_Click(object sender, RoutedEventArgs e)
        {

        }

        private void GraphItem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDataObject != null)
            {
                var item = (Controls.TreeViewModel.TreeNodeVM)CurrentDataObject;

                App.ForceHighQuality = true;
                ExpandAll(item);
            }
            System.Diagnostics.Debug.Print("");
        }
    }
}

//<HierarchicalDataTemplate.Triggers>
//                   <DataTrigger Binding="{Binding RelativeSource={RelativeSource AncestorType=TreeViewItem}, Path=IsExpanded}" Value="True">
//                       <Setter TargetName="isGood" Property="Source" Value="{Binding Source={StaticResource Icon_FolderOpen}, Mode=OneTime}"/>
//                   </DataTrigger>
//               </HierarchicalDataTemplate.Triggers>

//private void Trace_SelectionChanged(object sender, SelectionChangedEventArgs e)
//{
//    var item = ((Controls.TreeViewModel.iTV_Node)((System.Windows.Controls.ListView)sender).SelectedItem);
//    if (selectedItem != item && item !=null)
//    {
//        item.Clicked();
//        selectedItem = item;
//        if (ItemSelected != null)
//        {
//            this.ItemSelected(this, selectedItem);
//        }
//    }
//}

//private void IVTrace_SelectionChanged(object sender, SelectionChangedEventArgs e)
//{
//    var item = (((System.Windows.Controls.ListView)sender).SelectedItem);
//    //(Controls.TreeViewModel.iTV_Node)

//    //item.Clicked();
//    //selectedItem = item;
//    if (item != null)
//    {
//        this.ItemSelected(this, item);
//    }
//}

//private void Junction_SelectionChanged(object sender, SelectionChangedEventArgs e)
//{
//    var item = (((System.Windows.Controls.ListView)sender).SelectedItem);
//    //(Controls.TreeViewModel.iTV_Node)

//    //item.Clicked();
//    //selectedItem = item;
//    if (item != null)
//    {
//        this.ItemSelected(this, item);
//    }
//}



//private void TV_LabData_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
//{
//    System.Diagnostics.Debug.Print(sender.ToString());
//    return;
//    if (TV_LabData.SelectedItem.GetType().ToString().Contains("Batch") == true)
//    {

//        var obatch = (DataModel.Batch)TV_LabData.SelectedItem;
//        using (var context = new DataModel.LabContext())
//        {
//            var batch = (from x in context.Batches where x.Id == obatch.Id select x).FirstOrDefault();

//            //context.Batches.Remove(batch);


//            for (int k = 0; k < batch.Chips.ToList().Count; k++)
//            {
//                var chip = batch.Chips[k];

//                for (int i = 0; i < chip.Junctions.ToList().Count; i++)
//                {
//                    var junction = chip.Junctions[0];

//                    for (int j = 0; j < junction.Traces.ToList().Count; j++)
//                    {
//                        var trace = junction.Traces[0];
//                        if (trace.Note != null)
//                            context.Notes.Remove(trace.Note);
//                        //  context.SaveChanges();
//                        context.Traces.Remove(trace);
//                        //  context.SaveChanges();
//                    }

//                    for (int j = 0; j < junction.IonicIV.ToList().Count; j++)
//                    {
//                        var iv = junction.IonicIV[0];
//                        if (iv.Note != null)
//                            context.Notes.Remove(iv.Note);
//                        //  context.SaveChanges();
//                        context.IonicIV.Remove(iv);
//                        // context.SaveChanges();
//                    }

//                    for (int j = 0; j < junction.TunnelIV.ToList().Count; j++)
//                    {
//                        var iv = junction.TunnelIV[0];
//                        if (iv.Note != null)
//                            context.Notes.Remove(iv.Note);
//                        // context.SaveChanges();
//                        context.TunnelIV.Remove(iv);
//                        // context.SaveChanges();
//                    }
//                    if (junction.Note != null)
//                        context.Notes.Remove(junction.Note);
//                    // context.SaveChanges();
//                    context.Junctions.Remove(junction);
//                    // context.SaveChanges();
//                }
//                if (chip.Note != null)
//                    context.Notes.Remove(chip.Note);
//                //context.SaveChanges();
//                context.Chips.Remove(chip);
//                // context.SaveChanges();
//            }
//            for (int j = 0; j < batch.Notes.ToList().Count; j++)
//            {
//                var note = batch.Notes[0];
//                context.Notes.Remove(note);
//                //  context.SaveChanges();
//            }
//            context.Batches.Remove(batch);
//            context.SaveChanges();


//            //context.SaveChanges();

//        }

//    }

//}