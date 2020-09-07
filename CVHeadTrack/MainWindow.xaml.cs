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
using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;


namespace CVHeadTrack {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private IntPtr simConnectHandle;

        public MainWindow() {
            InitializeComponent();
        }

        private void DebugDialog(String msg) {
            TextBlock myTextBlock = (TextBlock)this.FindName("debugPanel");
            myTextBlock.Text = msg;

        }

        private void LinkFlightSim(object sender, RoutedEventArgs e) {
            DebugDialog("Starting to link");
            // Open
            // Declare a SimConnect object
            SimConnect simconnect = null;
            // User-defined win32 event
            const int WM_USER_SIMCONNECT = 0x0402;
            bool success = true;
            try {
                simconnect = new SimConnect("Managed Data Request", this.simConnectHandle, WM_USER_SIMCONNECT, null, 0);
            } catch (COMException ex) {
                success = false;
                DebugDialog("Failed to open connection " + ex.Message);
                if (simconnect != null) {
                    simconnect.Dispose();
                    simconnect = null;
                }
            }
            if (success) {
                DebugDialog("Attached to game!");
            }
        }
    }
}
