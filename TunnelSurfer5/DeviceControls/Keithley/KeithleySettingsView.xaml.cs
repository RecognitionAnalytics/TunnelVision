﻿using System;
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

namespace TunnelVision.DeviceControls.Keithley
{
    /// <summary>
    /// Interaction logic for bDAQSettingsView.xaml
    /// </summary>
    public partial class KeithleySettingsView : UserControl
    {
        public KeithleySettingsView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            txtCodeEditor.SetHighlighting("Lua");
            string file = (string)Properties.Settings.Default["KeithleyFile"];
            try
            {
                txtCodeEditor.LoadFile(file);
            }
            catch { }
        }
    }
}
