using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelVision.Controls.TreeViewModel
{
    class DBWatcherVM : TreeNodeVM
    {
        protected override void DoClick()
        {
            base.DoClick();
        }
        protected override void DoListOpen(bool value)
        {
            base.DoListOpen(value);
        }

        public DBWatcherVM() : base(null)
        {

        }
    }
}
