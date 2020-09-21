// SimConnect broken, review readme file
// Flight sim connection 
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.ComponentModel;
// UDP socket
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

// Import Pose object

namespace CVHeadTrack {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        // SimConnect broken, review readme file
        private IntPtr simConnectHandle;
        private SimConnect simconnect;
        private const int WM_USER_SIMCONNECT = 0x0402;
        // Wpf stuff
        private TextBlock debugTextBlock;
        private TextBox camUrl;
        private Image imageItem;
        // Neural net stuff
        private FaceDetector faceDetector;
        private bool faceMutexLock;
        // Opentrack upd socket objects
        private Socket updSocket;
        private IPAddress openTrackAddr;
        private IPEndPoint openTrackEndPoint;
        // Tracker feedback textblocks
        private TextBlock[] userTrackFeedback;

        public MainWindow() {
            InitializeComponent();
            // Set up image viewer
            this.debugTextBlock = (TextBlock)this.FindName("DebugPanel");
            this.imageItem = (Image)this.FindName("ImageItem");
            this.camUrl = (TextBox)this.FindName("CameraUrl");
            // Set up face nn
            this.faceDetector = new FaceDetector("assets/shape_predictor_68_face_landmarks.dat");
            this.faceMutexLock = true;
            // Set up UDP socket
            this.updSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.openTrackAddr = IPAddress.Parse("127.0.0.1");
            this.openTrackEndPoint = new IPEndPoint(this.openTrackAddr, 4242);
            // Set up pointers to feedback textblocks
            this.userTrackFeedback = new TextBlock[6];
            this.userTrackFeedback[0] = (TextBlock)this.FindName("UserTrackX");
            this.userTrackFeedback[1] = (TextBlock)this.FindName("UserTrackY");
            this.userTrackFeedback[2] = (TextBlock)this.FindName("UserTrackZ");
            this.userTrackFeedback[3] = (TextBlock)this.FindName("UserTrackYaw");
            this.userTrackFeedback[4] = (TextBlock)this.FindName("UserTrackPitch");
            this.userTrackFeedback[5] = (TextBlock)this.FindName("UserTrackRoll");
        }

        private void CleanSimConnectHandle() {
            if (simconnect != null) {
                simconnect.Dispose();
                simconnect = null;
            }
        }

        void WindowClose(object sender, CancelEventArgs e) {
            this.faceDetector.Dispose();
            this.updSocket.Close();
            CleanSimConnectHandle();
        }

        private void DebugDialog(String msg) {
            this.debugTextBlock.Text = msg;
        }

        // SimConnect broken, review readme file
        private void LinkFlightSim(object sender, RoutedEventArgs e) {
            DebugDialog("Starting to link");
            // Open
            // Declare a SimConnect object
            // User-defined win32 event
            bool success = true;
            try {
                this.simconnect = new SimConnect("Managed Data Request", this.simConnectHandle, WM_USER_SIMCONNECT, null, 0);
            } catch (COMException ex) {
                success = false;
                DebugDialog("Failed to open connection " + ex.Message);
                CleanSimConnectHandle();
            }
            if (success) {
                DebugDialog("Attached to game!");
            }
        }

        private void StopCamera(object sender, RoutedEventArgs e) {
            this.faceMutexLock = true;
            this.imageItem.Source = null;
        }

        private void ConnectCamera(object sender, RoutedEventArgs e) {
            // Lock
            ((Button)this.FindName("ConnectCameraButton")).IsEnabled = false;
            this.faceMutexLock = false;
            DebugDialog("Connecting to camera...");
            // get input
            String url = this.camUrl.Text;
            if (!this.faceDetector.ConnectCamera(url)) {
                this.faceMutexLock = true;
                ((Button)this.FindName("ConnectCameraButton")).IsEnabled = true;
                return;
            }
            DebugDialog("Conencted!");
            while (!this.faceMutexLock) {
                double[] pose = new double[6];
                bool success = faceDetector.GetPose(pose, this.imageItem);
                if (success) {
                    sendOpenTrackUDP(pose);
                    updateUserTrackFeedback(pose);
                }
            }
            ((Button)this.FindName("ConnectCameraButton")).IsEnabled = true;
        }

        // Updates head position values on GUI
        private void updateUserTrackFeedback(double[] data) {
            for (int i = 0; i < this.userTrackFeedback.Length; i++) {
                this.userTrackFeedback[i].Text = ((int)data[i]).ToString();
            }
        }

        // Send array of data to Opentrack socket
        // Each double is formated to a byte array and but in little endian
        private void sendOpenTrackUDP(double[] data) {
            byte[] sendBuffer = new byte[data.Length * sizeof(double)];
            for (int i = 0; i < data.Length; i++) {
                byte[] converted = BitConverter.GetBytes(data[i]);
                if (!BitConverter.IsLittleEndian) { // OpenTrack uses LittleEndian
                    Array.Reverse(converted);
                }
                for (int j = 0; j < sizeof(double); j++) {
                    sendBuffer[i * sizeof(double) + j] = converted[j];
                }
            }
            this.updSocket.SendTo(sendBuffer, this.openTrackEndPoint);
        }

        // Send some fixed and random test data to the OpenTrack socket
        private void TestUDP(object sender, RoutedEventArgs e) {
            DebugDialog("Sending test UDP signal");
            Random rnd = new Random();
            double[] test = new double[6];
            test[0] = 5;
            test[1] = 3;
            test[2] = 2;
            test[3] = rnd.Next(-50, 50);
            test[4] = rnd.Next(-50, 50);
            test[5] = rnd.Next(-50, 50);
            updateUserTrackFeedback(test);
            sendOpenTrackUDP(test);
        }

        private void TestVariableImage_Click(object sender, RoutedEventArgs e) {
            var img = this.faceDetector.TestImage("assets\\testImg1.jpg");
            this.imageItem.Source = FaceDetector.BitmapToImageSource(img);
        }
    }
}
