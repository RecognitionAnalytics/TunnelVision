using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.DeviceControls.Axon
{
   public class AxomVM : BaseDeviceVM
    {
        public void startAxon(string file, int sampleRate, double runTime)
        {
            if (_Axon == null)
                _Axon = new DeviceControls.Axon.Axon();
            _Axon.RunAxon(file, false,sampleRate, (int)Math.Floor( runTime));
            watcher = new FileSystemWatcher(file);
            watcher.Created += Watcher_Created;
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        FileSystemWatcher watcher;
        DeviceControls.Axon.Axon _Axon = null;

        public override string ToString()
        {
            return "Axon";
        }
    }
}
