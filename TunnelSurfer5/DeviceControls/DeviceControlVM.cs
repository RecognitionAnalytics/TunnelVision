using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.DeviceControls
{

    public class DeviceControlVM : INotifyPropertyChanged
    {
        protected void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        private BaseDeviceVM _Document;
        public BaseDeviceVM Document
        {
            get
            {
                return _Document;
            }
            set
            {
                _Document = value;
                NotifyPropertyChanged("Document");
            }
        }


    }
}
