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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TunnelVision.Controls
{
    /// <summary>
    /// Interaction logic for ChipContents.xaml
    /// </summary>
    public partial class JunctionContents : UserControl
    {
        public JunctionContents()
        {
            InitializeComponent();
        }
    }

    public class IVFolder
    {
        public IVItem[] IonicCurves { get; set; }
        public IVItem[] TunnelingCurves { get; set; }
    }

    public class IVItem
    {
        public string IVName { get; set; }
        public override string ToString()
        {
            return IVName;
        }
    }

    public class TimeFolder
    {

        public OrderedAnalyte[] Analytes { get; set; }
    }

    public class OrderedAnalyte
    {
        public int AnalyteOrder { get; set; }
        public string Analyte { get; set; }
        public TraceItem[] Traces { get; set; }
        public string FolderName { get; set; }
        public override string ToString()
        {
            return AnalyteOrder + " " + Analyte;
        }

    }

    public class TraceItem
    {
        public string TraceName { get; set; }
        public override string ToString()
        {
            return TraceName;
        }
    }
}
