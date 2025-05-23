using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace TunnelVision
{
    public class CommonExtensions
    {
        public static void DoEvents()
        {
            try
            {
                // System.Windows.Forms.Application.DoEvents();
                DispatcherFrame frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                    new DispatcherOperationCallback(ExitFrame), frame);
                Dispatcher.PushFrame(frame);
            }
            catch { }
        }

        private static object ExitFrame(object f)
        {
            try
            {
                ((DispatcherFrame)f).Continue = false;
            }
            catch { }

            return null;
        }

        public static System.Windows.Size GetElementPixelSize(UIElement element)
        {
            Matrix transformToDevice;
            var source = PresentationSource.FromVisual(element);
            if (source != null)
                transformToDevice = source.CompositionTarget.TransformToDevice;
            else
                using (var source2 = new System.Windows.Interop.HwndSource(new System.Windows.Interop.HwndSourceParameters()))
                    transformToDevice = source2.CompositionTarget.TransformToDevice;

            if (element.DesiredSize == new System.Windows.Size())
                element.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));

            return (System.Windows.Size)transformToDevice.Transform((Vector)element.DesiredSize);
        }
    }
}
