using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// <summary>
    /// Interaction logic for DocumentHolder.xaml
    /// </summary>
    public partial class DocumentHolder : UserControl
    {
        public DocumentHolder()
        {
            
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex);
            }

            this.DataContextChanged += DocumentHolder_DataContextChanged;
        }

        private void DocumentHolder_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
           // throw new NotImplementedException();
        }
    }
}
