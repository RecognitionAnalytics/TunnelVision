using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TunnelVision.DataModel;
namespace TunnelVision
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        Controls.TreeViewModel.MainViewVM MainView;
        DeviceControls.DeviceControlVM DeviceView;
        public MainWindow()
        {

            InitializeComponent();

            DataViewer.ItemSelected += DataViewer_ItemSelected;
            DataViewer.RefreshData += DataViewer_RefreshData;
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;

            MainView = new Controls.TreeViewModel.MainViewVM();
            MainView.RefreshDataTree += MainView_RefreshDataTree;
            DataContext = MainView;

            DeviceView = new DeviceControls.DeviceControlVM();
        }

        private void MainView_RefreshDataTree()
        {
            DataViewer.SetData(MainView.SetupDataModel(),true);
        }

        void RefreshData()
        {
            DataViewer.SetData(MainView.SetupDataModel());
        }
        void DataViewer_RefreshData(object sender, EventArgs e)
        {
            RefreshData();
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        #region Startup

        System.Windows.Threading.DispatcherTimer dispatcherStart = new System.Windows.Threading.DispatcherTimer();
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (LabContext contextDB = new LabContext())
                {
                    var exp = (from x in contextDB.Experiments select x).ToArray();

                    if (exp.Length == 0)
                    {
                        contextDB.FakeData();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
                System.Windows.Forms.MessageBox.Show("Cannot connect to database.");
            }
            dispatcherStart.Tick += DispatcherStart_Tick;
            dispatcherStart.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            dispatcherStart.IsEnabled = true;
        }

        private void DispatcherStart_Tick(object sender, EventArgs e)
        {
            dispatcherStart.IsEnabled = false;
            RefreshData();
        }
        #endregion

        void DataViewer_ItemSelected(DataExplorerView sender, object e)
        {
            try
            {
                var currentItem = (Controls.TreeViewModel.TreeNodeVM)e;
                //PropertyViewer.SetObject(currentItem.DataModelItem);
                MainView.Document = null;
                CommonExtensions.DoEvents();
                MainView.Document = currentItem;

                if (e.GetType() == typeof(Controls.TreeViewModel.DataTraceVM))
                {
                    ((Controls.TreeViewModel.DataTraceVM)e).RepeatMeasurment += MainWindow_RepeatMeasurment;
                }

            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);

            }
        }

        private void MainWindow_RepeatMeasurment(Controls.TreeViewModel.DataTraceVM dataTrace)
        {
            var newTrace = new Controls.TreeViewModel.NewDataTraceVM(Controls.TreeViewModel.NewDataTraceVM.TraceTypeEnum.Constant, dataTrace.DataTrace);
            MainView.Document = newTrace;
        }

        #region FocusedElement

        /// <summary>
        /// FocusedElement Dependency Property
        /// </summary>
        public static readonly DependencyProperty FocusedElementProperty =
            DependencyProperty.Register("FocusedElement", typeof(string), typeof(MainWindow),
                new FrameworkPropertyMetadata((IInputElement)null));

        /// <summary>
        /// Gets or sets the FocusedElement property.  This dependency property 
        /// indicates ....
        /// </summary>
        public string FocusedElement
        {
            get { return (string)GetValue(FocusedElementProperty); }
            set { SetValue(FocusedElementProperty, value); }
        }

        #endregion

        #region addButtons
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Settings settings = new Settings();
                settings.Experiments = MainView.Experiments;
                settings.ShowDialog();
                MainView.SetupDataModel();
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private void AddBatch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddBatchWindow batchWindow = new AddBatchWindow();
                batchWindow.Experiment = MainView.Document.Experiment();
                batchWindow.ShowDialog();
                MainView.SetupDataModel();
                RefreshData();
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private void AddChip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddChipWindow chipWindow = new AddChipWindow();
                chipWindow.Experiment = MainView.Document.Experiment();
                chipWindow.Batch = MainView.Document.Batch();
                chipWindow.ShowDialog();
                MainView.SetupDataModel();
                RefreshData();
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private void AddJunction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Controls.InputBox ib = new Controls.InputBox("Please enter junction name:", "New Junction", "");
                string jName = ib.ShowDialog();
                if (jName != "")
                {
                    MainView.Document.Chip().AddJunction(jName);
                    MainView.SetupDataModel();
                    RefreshData();
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        Brush b;
        private void addFile_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                b = addFileButton.Background;
                addFileButton.Background = Brushes.DarkSlateGray;
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private void addFile_DragLeave(object sender, DragEventArgs e)
        {
            try
            {
                addFileButton.Background = b;
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private void addFile_Drop(object sender, DragEventArgs e)
        {
            try
            {
                addFileButton.Background = b;
                string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                if (s.Length > 0)
                {
                    string dropped = s[0];
                    if (File.Exists(dropped))
                    {
                        Controls.AddFileDialog adf = new Controls.AddFileDialog();
                        string experimentName = "";
                        string batchName = "";
                        string chipName = "";
                        string junctionName = "";
                        try
                        {
                            experimentName = MainView.Document.Experiment().ExperimentName;
                            batchName = MainView.Document.Batch().BatchName;
                            chipName = MainView.Document.Chip().ChipName;
                            junctionName = MainView.Document.Junction().JunctionName;
                        }
                        catch { }
                        adf.ShowFilenames(experimentName, batchName, chipName, junctionName, s);
                        adf.ShowDialog();
                        MainView.SetupDataModel();
                        RefreshData();
                    }
                    else if (Directory.Exists(dropped))
                    {
                        DataModel.ExistingParse.ParseExperimentFolder(MainView.Document.Experiment(), dropped,false);
                        MainView.SetupDataModel();
                        RefreshData();
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        System.Windows.Threading.DispatcherTimer fileWatcherTimer = new System.Windows.Threading.DispatcherTimer();
        List<string> ExistingFiles = new List<string>();
        string _WatchFolder = "";
        void fileWatcherTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                string[] files = Directory.GetFiles(_WatchFolder, "*.*", SearchOption.AllDirectories);

                var result = files.Where(p => !ExistingFiles.Any(p2 => p2 == p)).ToList();
                if (result != null && result.Count > 0)
                {
                    fileWatcherTimer.IsEnabled = false;
                    ExistingFiles.AddRange(result);
                    AddFiles(result.ToArray());
                    fileWatcherTimer_Tick(sender, e);
                    fileWatcherTimer.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private void AddFiles(string[] files)
        {
            try
            {
                Controls.AddFileDialog adf = new Controls.AddFileDialog();
                string experimentName = "";
                string batchName = "";
                string chipName = "";
                string junctionName = "";
                try
                {
                    experimentName = MainView.Document.Experiment().ExperimentName;
                    batchName = MainView.Document.Batch().BatchName;
                    chipName = MainView.Document.Chip().ChipName;
                    junctionName = MainView.Document.Junction().JunctionName;
                }
                catch { }

                string[] _files = (from x in files where x.ToLower().Contains("abf") select x).ToArray();
                if (_files != null && _files.Length > 0)
                {
                    adf.ShowFilenames(experimentName, batchName, chipName, junctionName, _files);
                    adf.ShowDialog();
                }
                _files = (from x in files where System.IO.Path.GetExtension(x) == "" select x).ToArray();
                if (_files != null && _files.Length > 0)
                {
                    adf.ShowFilenames(experimentName, batchName, chipName, junctionName, _files);
                    adf.ShowDialog();
                }
                MainView.SetupDataModel();
                RefreshData();
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private void addFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
                ofd.Multiselect = true;
                var ret = ofd.ShowDialog();
                if (ret == System.Windows.Forms.DialogResult.OK)
                {
                    AddFiles(ofd.FileNames);
                }
                RefreshData();
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }



        private void dataTraceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Controls.TreeViewModel.NewDataTraceVM dt = new Controls.TreeViewModel.NewDataTraceVM(Controls.TreeViewModel.NewDataTraceVM.TraceTypeEnum.Constant, new DataModel.DataTrace
                {
                    TraceName = "New Trace_0000",
                    Chip = MainView.Document.Chip(),
                    Junction = MainView.Document.Junction(),
                    Concentration_mM = 100,
                    BufferConcentration_mM=100,
                    BottomReference_mV = 0,
                    TopReference_mV = 450,
                    Buffer = "PB",
                    Analyte = "PB",
                    DateAcquired = DateTime.Now,
                    Folder = MainView.Document.Junction().Folder,
                    Note = new eNote(),
                    QCStep = QualityControlStep.RT,
                    RunTime = 120,
                    Tunnel_mV_Top = 300

                });
                var lastTrace = Controls.TreeViewModel.NewDataTraceVM.LastTrace;
                if (lastTrace!= null)
                {
                    dt.Concentration = lastTrace.Concentration;
                    dt.BufferConcentration = lastTrace.BufferConcentration;
                    dt.BottomReference = lastTrace.BottomReference;
                    dt.TopReference = lastTrace.TopReference;
                    dt.Buffer = lastTrace.Buffer;
                    dt.Analyte = lastTrace.Analyte;
                    dt.TunnelVoltage = lastTrace.TunnelVoltage;
                    dt.TunnelVoltageBot = lastTrace.TunnelVoltageBot;
                }
                DataViewer_ItemSelected(null, dt);
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private void ivTraceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Controls.TreeViewModel.NewDataTraceVM dt = new Controls.TreeViewModel.NewDataTraceVM(Controls.TreeViewModel.NewDataTraceVM.TraceTypeEnum.Tunneling, new DataModel.DataTrace
                {
                    TraceName = "tunnelIV_0000",
                    Chip = MainView.Document.Chip(),
                    Junction = MainView.Document.Junction(),
                    Concentration_mM = 100,
                    BufferConcentration_mM = 100,
                    BottomReference_mV = 0,
                    TopReference_mV = 0,
                    Buffer = "PB",
                    Analyte = "PB",
                    DateAcquired = DateTime.Now,
                    Folder = MainView.Document.Junction().Folder,
                    Note = new eNote(),
                    QCStep = QualityControlStep.RT,
                    RunTime = 120,
                    Tunnel_mV_Top = 200

                });
                DataViewer_ItemSelected(null, dt);
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private void ionicivTraceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var junction = MainView.Document.Junction();
                Controls.TreeViewModel.NewDataTraceVM dt;
                if (junction != null)
                {
                    dt = new Controls.TreeViewModel.NewDataTraceVM(Controls.TreeViewModel.NewDataTraceVM.TraceTypeEnum.Ionic, new DataModel.DataTrace
                    {
                        TraceName = "ionicIV_0000",
                        Chip = MainView.Document.Chip(),
                        Junction = MainView.Document.Junction(),
                        Analyte = "PB",
                        Concentration_mM = 100,
                        BufferConcentration_mM = 100,
                        BottomReference_mV = 0,
                        TopReference_mV = 450,
                        Buffer = "PB",
                        DateAcquired = DateTime.Now,
                        Folder = MainView.Document.Junction().Folder,
                        Note = new eNote(),
                        QCStep = QualityControlStep.RT,
                        RunTime = 120,
                        Tunnel_mV_Top = 0,
                    });
                }
                else
                {
                    dt = new Controls.TreeViewModel.NewDataTraceVM(Controls.TreeViewModel.NewDataTraceVM.TraceTypeEnum.Ionic, new DataModel.DataTrace
                    {
                        TraceName = "ionicIV_0000",
                        Chip = MainView.Document.Chip(),
                        Junction = null,
                        BottomReference_mV = 0,
                        Concentration_mM = 100,
                        Analyte = "PB",
                        TopReference_mV = 450,
                        Buffer = "PB",
                        DateAcquired = DateTime.Now,
                        Folder = MainView.Document.Junction().Folder,
                        Note = new eNote(),
                        QCStep = QualityControlStep.RT,
                        RunTime = 120,
                        Tunnel_mV_Top = 0

                    });
                }
                DataViewer_ItemSelected(null, dt);
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }
        #endregion

        #region Ratings
        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog ofd = new System.Windows.Forms.SaveFileDialog();
            ofd.FileName = "Folder";
            var ret = ofd.ShowDialog();
            if (ret == System.Windows.Forms.DialogResult.OK)
            {
                MainView.ExportToRoche(System.IO.Path.GetDirectoryName( ofd.FileName) );
            }
        }

        private void btnBroken_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //    currentContent.RateItem(UserRating.Bad);
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private void btnBad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //   currentContent.RateItem(UserRating.Bad);
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        private void btnGood_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //currentContent.RateItem(UserRating.Good);
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }
        #endregion

        #region Watcher
        private void watchFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
                folderDialog.SelectedPath = (string)Properties.Settings.Default["WatchFolder"];

                System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
                if (result.ToString() == "OK")
                {
                    Properties.Settings.Default["WatchFolder"] = folderDialog.SelectedPath;
                    _WatchFolder = folderDialog.SelectedPath;
                    fileWatcherTimer.Interval = new TimeSpan(0, 1, 0);
                    fileWatcherTimer.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }

        
        private void miWatch_Click(object sender, RoutedEventArgs e)
        {
            MainView.Document = new Controls.TreeViewModel.DBWatcherVM();
        }

        #endregion

        #region FileViewer
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.OpenFileDialog folderDialog = new System.Windows.Forms.OpenFileDialog();
                System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
                if (result.ToString() == "OK")
                {
                    var fakeItem = new TunnelVision.Controls.TreeViewModel.DataTraceVM(new DataModel.DataTrace(folderDialog.FileName));
                    this.DataViewer_ItemSelected(null, fakeItem);
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
        }
        #endregion


        #region RibbonHeaders
        private void HomeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            HardwareButtons.Visibility = Visibility.Collapsed;
            HomeButtons.Visibility = Visibility.Visible;
            DataContext = null;
            DataContext = MainView;
        }

        private void HardwareMenuItem_Click(object sender, RoutedEventArgs e)
        {
            HardwareButtons.Visibility = Visibility.Visible;
            HomeButtons.Visibility = Visibility.Collapsed;
            DataContext = null;
            DataContext = DeviceView;
        }
        #endregion

        #region Hardware
        private void HardSettingsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void bDAQButton_Click(object sender, RoutedEventArgs e)
        {
            DeviceView.Document = new DeviceControls.bDAQ.bDAQVM(); 
        }

        private void keithleyButton_Click(object sender, RoutedEventArgs e)
        {
            DeviceView.Document = new DeviceControls.Keithley.KeithleyVM();
        }

        private void axonButton_Click(object sender, RoutedEventArgs e)
        {
            DeviceView.Document = new DeviceControls.Axon.AxonVM();
        }

        private void NiDAQButton_Click(object sender, RoutedEventArgs e)
        {
            DeviceView.Document = new DeviceControls.NIDAQ.NIDAQDeviceVM();
        }
        #endregion
    }
}