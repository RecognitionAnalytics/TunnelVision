using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace TunnelVision.Controls.TreeViewModel
{
    public class TreeNodeVM : INotifyPropertyChanged
    {
        public event RefreshDataTreeEvent RefreshDataTree;

        public void RequestDataTreeRefresh()
        {
            if (RefreshDataTree != null)
                RefreshDataTree();
        }

        protected void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TreeNodeVM(object selectedItem)
        {
            this.DataModelItem = selectedItem;
            _Children = new ObservableCollection<object>();
            _Column1 = new ObservableCollection<object>();
            _Column2 = new ObservableCollection<object>();
            _Column3 = new ObservableCollection<object>();
            _Column4 = new ObservableCollection<object>();
        }
       

        public Visibility ListOpen
        {
            get { return _OpenList; }
            set
            {
                _OpenList = value;
                NotifyPropertyChanged("ListOpen");
            }
        }

        protected virtual void DoListOpen(bool value)
        {

        }

        Visibility _OpenList = Visibility.Collapsed;
        public bool OpenList
        {
            get { return (_OpenList == Visibility.Visible); }
            set
            {
                if (value == true)
                {
                    DoListOpen(value);
                    ListOpen = Visibility.Visible;
                }
                else
                    ListOpen = Visibility.Collapsed;
            }
        }

        protected virtual void DoClick()
        {
        }

        bool AlreadyClicked = false;
        public void Clicked()
        {
            if (AlreadyClicked == false)
            {
                AlreadyClicked = true;
                DoClick();
            }
        }

        public void DoubleClicked()
        {
            OpenList = !OpenList;
        }

        public object DataModelItem
        {
            get;
            set;
        }
        public override string ToString()
        {
            return DataModelItem.ToString();
        }

        #region Columns
        private ObservableCollection<object> _Column1;
        private ObservableCollection<object> _Column2;
        private ObservableCollection<object> _Column3;
        private ObservableCollection<object> _Column4;
        public ObservableCollection<object> Column1
        {
            get
            {
                return _Column1;
            }
            set
            {
                _Column1 = value;
                NotifyPropertyChanged("Column1");
            }
        }
        public ObservableCollection<object> Column2
        {
            get
            {
                return _Column2;
            }
            set
            {
                _Column2 = value;
                NotifyPropertyChanged("Column2");
            }
        }
        public ObservableCollection<object> Column3
        {
            get
            {
                return _Column3;
            }
            set
            {
                _Column3 = value;
                NotifyPropertyChanged("Column3");
            }
        }
        public ObservableCollection<object> Column4
        {
            get
            {
                return _Column4;
            }
            set
            {
                _Column4 = value;
                NotifyPropertyChanged("Column4");
            }
        }
        #endregion

        private ObservableCollection<object> _Children;
        public ObservableCollection<object> Children
        {
            get
            {
                return _Children;
            }
            set
            {
                _Children = value;
                NotifyPropertyChanged("Children");
            }
        }


        private Brush _GoodColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Black"));
        public Brush GoodColor
        {
            get { return _GoodColor; }
            set
            {
                _GoodColor = value;
                NotifyPropertyChanged("GoodColor");
            }
        }
        public void RateItem(DataModel.UserRating rating)
        {
            DataModel.iRateable myTest = DataModelItem as DataModel.iRateable;

            if (myTest != null)
            {
                myTest.RateItem(rating);
            }
            GoodColor = ConvertRatingToColor.Convert(rating);
        }

        public virtual void ExportToRoche(string FolderName)
        {

        }

        #region Buttons
        private ButtonPanel _MenuButtons = new ButtonPanel();
        public ButtonPanel MenuButtons
        {
            get { return _MenuButtons; }
            set
            {
                _MenuButtons = value;
                NotifyPropertyChanged("MenuButtons");
            }
        }

        public class ButtonPanel : INotifyPropertyChanged
        {
            protected void NotifyPropertyChanged(string property)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            public Visibility _AddExperimentButton = Visibility.Visible;
            public Visibility AddExperimentButton
            {
                get
                {
                    return _AddExperimentButton;
                }
                set
                {
                    _AddExperimentButton = value;
                    NotifyPropertyChanged("AddExperimentButton");
                }
            }

            public Visibility _AddBatchButton = Visibility.Visible;
            public Visibility AddBatchButton
            {
                get
                {
                    return _AddBatchButton;
                }
                set
                {
                    _AddBatchButton = value;
                    NotifyPropertyChanged("AddBatchButton");
                }
            }

            public Visibility _AddChipButton = Visibility.Visible;
            public Visibility AddChipButton
            {
                get
                {
                    return _AddChipButton;
                }
                set
                {
                    _AddChipButton = value;
                    NotifyPropertyChanged("AddChipButton");
                }
            }

            public Visibility _StatsButton = Visibility.Visible;
            public Visibility StatsButton
            {
                get
                {
                    return _StatsButton;
                }
                set
                {
                    _StatsButton = value;
                    NotifyPropertyChanged("StatsButton");
                }
            }

            public Visibility _AddJunctionButton = Visibility.Visible;
            public Visibility AddJunctionButton
            {
                get
                {
                    return _AddJunctionButton;
                }
                set
                {
                    _AddJunctionButton = value;
                    NotifyPropertyChanged("AddJunctionButton");
                }
            }

            public Visibility _AddDataTraceButton = Visibility.Visible;
            public Visibility AddDataTraceButton
            {
                get
                {
                    return _AddDataTraceButton;
                }
                set
                {
                    _AddDataTraceButton = value;
                    NotifyPropertyChanged("AddDataTraceButton");
                }
            }

            public Visibility _AddIonicButton = Visibility.Visible;
            public Visibility AddIonicButton
            {
                get
                {
                    return _AddIonicButton;
                }
                set
                {
                    _AddIonicButton = value;
                    NotifyPropertyChanged("AddIonicButton");
                }
            }

            public Visibility _AddTunnelButton = Visibility.Visible;
            public Visibility AddTunnelButton
            {
                get
                {
                    return _AddTunnelButton;
                }
                set
                {
                    _AddTunnelButton = value;
                    NotifyPropertyChanged("AddTunnelButton");
                }
            }

            public Visibility _UploadButton = Visibility.Visible;
            public Visibility UploadButton
            {
                get
                {
                    return _UploadButton;
                }
                set
                {
                    _UploadButton = value;
                    NotifyPropertyChanged("UploadButton");
                }
            }

            public Visibility _BrokenButton = Visibility.Visible;
            public Visibility BrokenButton
            {
                get
                {
                    return _BrokenButton;
                }
                set
                {
                    _BrokenButton = value;
                    NotifyPropertyChanged("BrokenButton");
                }
            }

            public Visibility _BadButton = Visibility.Visible;
            public Visibility BadButton
            {
                get
                {
                    return _BadButton;
                }
                set
                {
                    _BadButton = value;
                    NotifyPropertyChanged("BadButton");
                }
            }

            public Visibility _GoodButton = Visibility.Visible;
            public Visibility GoodButton
            {
                get
                {
                    return _GoodButton;
                }
                set
                {
                    _GoodButton = value;
                    NotifyPropertyChanged("GoodButton");
                }
            }
        }
        #endregion

        #region DataTree
        public virtual void Delete()
        {

        }

        public virtual DataModel.Experiment Experiment() { return null; }
        public virtual DataModel.Batch Batch() { return null; }
        public virtual DataModel.Chip Chip() { return null; }
        public virtual DataModel.Junction Junction() { return null; }
        #endregion

#if Travel
        public static DataTable selectQuery(System.Data.SQLite.SQLiteConnection sqlite, string query)
        {
            System.Data.SQLite.SQLiteDataAdapter ad;
            DataTable dt = new DataTable();

            try
            {
                System.Data.SQLite.SQLiteCommand cmd;
                sqlite.Open();  //Initiate connection to the db
                cmd = sqlite.CreateCommand();
                cmd.CommandText = query;  //set the passed query
                ad = new System.Data.SQLite.SQLiteDataAdapter(cmd);
                ad.Fill(dt); //fill the datasource
            }
            catch (System.Data.SQLite.SQLiteException ex)
            {
                //Add your exception code here.
            }
            sqlite.Close();
            return dt;
        }
#else
        //public  static DataTable selectQuery(MySql.Data.MySqlClient.MySqlConnection mySQL, string query)
        //{
        //    MySql.Data.MySqlClient.MySqlDataAdapter ad;
          
        //    DataTable dt = new DataTable();

        //    try
        //    {
        //        MySql.Data.MySqlClient.MySqlCommand cmd;
        //        mySQL.Open();  //Initiate connection to the db
        //        cmd = mySQL.CreateCommand();
        //        cmd.CommandText = query;  //set the passed query
        //        ad = new MySql.Data.MySqlClient.MySqlDataAdapter(cmd);
        //        ad.Fill(dt); //fill the datasource
        //    }
        //    catch (System.Data.SQLite.SQLiteException ex)
        //    {
        //        //Add your exception code here.
        //    }
        //    mySQL.Close();
        //    return dt;
        //}

#endif
    }

    public class ConvertRatingToColor
    {
        public static System.Windows.Media.Brush Convert(DataModel.UserRating rating)
        {
            switch (rating)
            {
                case DataModel.UserRating.Bad:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("DarkRed"));
                case DataModel.UserRating.Good:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("DarkGreen"));
                case DataModel.UserRating.Broken:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("DarkSlateGray"));
                case DataModel.UserRating.Unrated:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("Black"));
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("Black"));
        }
    }
}
