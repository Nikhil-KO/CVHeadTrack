using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
// SimConnect broken, review readme file
// Flight sim connection 
using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using System.ComponentModel;
// Face NN
using DlibDotNet;
using DlibDotNet.Extensions;
using Dlib = DlibDotNet.Dlib;
// Read from url
using OpenCvSharp;
using OpenCvSharp.Extensions;
// UDP socker
using System.Net;
using System.Net.Sockets;

namespace CVHeadTrack {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window {

        // SimConnect broken, review readme file
        private IntPtr simConnectHandle;
        private SimConnect simconnect;
        private const int WM_USER_SIMCONNECT = 0x0402;
        // Wpf stuff
        private TextBlock debugTextBlock;
        private TextBox camUrl;
        private Image imageItem;
        // Neural net stuff
        private FrontalFaceDetector faceDetector;
        private ShapePredictor shapePredictor;
        // Opentrack upd socket objects
        private Socket updSocket;
        private IPAddress openTrackAddr;
        private IPEndPoint openTrackEndPoint;


        public MainWindow() {
            InitializeComponent();
            // Set up image viewer
            this.debugTextBlock = (TextBlock)this.FindName("DebugPanel");
            this.imageItem = (Image)this.FindName("ImageItem");
            this.camUrl = (TextBox)this.FindName("CameraUrl");
            // Set up face nn
            this.faceDetector = Dlib.GetFrontalFaceDetector();
            this.shapePredictor = ShapePredictor.Deserialize("assets/shape_predictor_68_face_landmarks.dat");
            // Set up UDP socket
            this.updSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.openTrackAddr = IPAddress.Parse("127.0.0.1");
            this.openTrackEndPoint = new IPEndPoint(this.openTrackAddr, 4242);
        }

        private void CleanSimConnectHandle() {
            if (simconnect != null) {
                simconnect.Dispose();
                simconnect = null;
            }
        }

        void WindowClose(object sender, CancelEventArgs e) {
            this.faceDetector.Dispose();
            this.shapePredictor.Dispose();
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

        private void ConnectCamera(object sender, RoutedEventArgs e) {
            DebugDialog("Connecting to camera...");
            // get input
            String url = this.camUrl.Text;
            // If empty use default else attach to input camera
            VideoCapture cap = url.Equals("") ? new VideoCapture() : new VideoCapture(url);
            if (!cap.IsOpened()) {
                DebugDialog("Failed to connect to camera");
                return;
            }
            DebugDialog("Conencted!");
            Mat temp = new Mat();
            ImageWindow imgWin = new ImageWindow();
            // Keep processing until window closed
            while (!imgWin.IsClosed()) {
                Console.WriteLine("reading");
                if (!cap.Read(temp)) {
                    break;
                }
                // Display in window
                System.Drawing.Bitmap t = temp.ToBitmap();
                BitmapImage b = BitmapToImageSource(t);
                this.imageItem.Source = b;
                // Copy from the cv2 image to dlib format then process and output
                var array = new byte[temp.Width * temp.Height * temp.ElemSize()];
                Marshal.Copy(temp.Data, array, 0, array.Length);
                using (var cimg = Dlib.LoadImageData<BgrPixel>(array, (uint)temp.Height, (uint)temp.Width, (uint)(temp.Width * temp.ElemSize()))) {
                    Console.WriteLine("Processing face nn");
                    var faces = this.faceDetector.Operator(cimg);
                    // Find the pose of each face.
                    var shapes = new List<FullObjectDetection>();
                    for (var i = 0; i < faces.Length; ++i) {
                        var det = this.shapePredictor.Detect(cimg, faces[i]);
                        shapes.Add(det);
                    }

                    // Display it all on the screen
                    imgWin.ClearOverlay();
                    imgWin.SetImage(cimg);
                    var lines = Dlib.RenderFaceDetections(shapes);
                    imgWin.AddOverlay(lines);
                    // some clean up required
                    foreach (var line in lines)
                        line.Dispose();
                }
                Cv2.WaitKey();
            }
            imgWin.Dispose();
            temp.Dispose();
            cap.Dispose();
            DebugDialog("Successfull detach");
        }

        // Util method to draw System.Drawing.Bitmap to wpf image panel
        public BitmapImage BitmapToImageSource(System.Drawing.Bitmap bitmap) {
            using (MemoryStream memory = new MemoryStream()) {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }

        private void DebugImageTest(object sender, RoutedEventArgs e) {
            DebugDialog("Starting face ML");
            using (var fd = Dlib.GetFrontalFaceDetector())
            using (var sp = ShapePredictor.Deserialize("assets/shape_predictor_68_face_landmarks.dat")) {
                DebugDialog("Model loaded");
                var img = Dlib.LoadImage<RgbPixel>("assets/image.jpeg");              
                var faces = fd.Operator(img);
                foreach (var face in faces) {
                    // find the landmark points for this face
                    var shape = sp.Detect(img, face);
                    // draw the landmark points on the image
                    for (var i = 0; i < shape.Parts; i++) {
                        DlibDotNet.Point point = shape.GetPart((uint)i);
                        var rect = new DlibDotNet.Rectangle(point);
                        Dlib.DrawRectangle(img, rect, color: new RgbPixel(255, 255, 0), thickness: 4);
                    }
                    Dlib.SaveJpeg(img, "output.jpg");
                    var bi = BitmapExtensions.ToBitmap(img);
                    this.imageItem.Source = BitmapToImageSource(bi);
                }
            }
            DebugDialog("Finished");
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
            sendOpenTrackUDP(test);
        }
    }
}
