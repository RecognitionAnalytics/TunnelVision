using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.Controls.TreeViewModel
{
    public delegate void RefreshDataTreeEvent();

    public class MainViewVM : INotifyPropertyChanged
    {
        public event RefreshDataTreeEvent RefreshDataTree;

        protected void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RequestionDataTreeRefresh()
        {
            if (RefreshDataTree != null)
                RefreshDataTree();
        }

        private TreeNodeVM _Document;
        public TreeNodeVM Document
        {
            get
            {
                return _Document;
            }
            set
            {
                _Document = value;
                if (_Document != null)
                {
                    _Document.RefreshDataTree += RequestionDataTreeRefresh;
                }
                NotifyPropertyChanged("Document");
            }
        }
        public DataModel.Experiment[] _Experiments;
        public DataModel.Experiment[] Experiments
        {
            get
            {
                return _Experiments;
            }
            set
            {
                _Experiments = value;
                NotifyPropertyChanged("Experiments");
            }
        }

        public DataModel.Experiment[] SetupDataModel()
        {
            try
            {
                var labData = new DataModel.LabContext();
                Experiments = labData.Experiments.ToArray();

                return Experiments;
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
            return null;
        }

        public static  DataModel.Experiment[] GetDataModel()
        {
            DataModel.Experiment[] Experiments;
            try
            {
                var labData = new DataModel.LabContext();
                Experiments = labData.Experiments.ToArray();

                return Experiments;
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }
            return null;
        }


        public void ExportToRoche(string folderName)
        {
            Document.ExportToRoche(folderName);
        }
    }
}
