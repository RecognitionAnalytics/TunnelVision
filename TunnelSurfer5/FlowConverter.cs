 using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Xaml;

namespace TunnelVision
{
   public  class FlowConverter : IValueConverter
    {


       public FlowConverter()
        {

        }


        #region IValueConverter Members

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is FlowDocument))
            {
                return null;
            }
            if (targetType == typeof(FlowDocument))
            {
                return value;
            }

            if (targetType != typeof(byte[]))
            {
                throw new InvalidOperationException(
                    "FlowDocumentToBinaryConverter can only convert back from a FlowDocument to a string.");
            }

            FlowDocument d = (FlowDocument)value;

            using (MemoryStream ms = new MemoryStream())
            {
                // write XAML out to a MemoryStream
                TextRange tr = new TextRange(
                    d.ContentStart,
                    d.ContentEnd);
                // tr.Save(ms, DataFormats.Xaml);
                // ms.Seek(0, SeekOrigin.Begin);

                // using (MemoryStream ms = new MemoryStream())
                {
                    tr.Save(ms, DataFormats.XamlPackage, true);
                    return ms.ToArray();
                }
                // return sb.ToString();
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return new FlowDocument();
            }
            if (value is FlowDocument)
            {
                return value;
            }
            if (targetType != typeof(FlowDocument))
            {
                throw new InvalidOperationException(
                    "FlowDocumentToBinaryConverter can only convert to a FlowDocument.");
            }
            if (!(value is byte[]))
            {
                throw new InvalidOperationException(
                    "FlowDocumentToBinaryConverter can only convert from a string or FlowDocument.");
            }

            byte[] s = (byte[])value;

            FlowDocument d = new FlowDocument();

            var content = new TextRange(d.ContentStart, d.ContentEnd);
            MemoryStream ms = new MemoryStream(s);
            content.Load(ms, System.Windows.DataFormats.XamlPackage);
            return d;
        }

        #endregion
    }
}


   
