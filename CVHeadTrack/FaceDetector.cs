using System;
using System.IO;
using System.Runtime.InteropServices;
// Face NN
using DlibDotNet;
using DlibDotNet.Extensions;
using Dlib = DlibDotNet.Dlib;
// Read from url
using OpenCvSharp;
using OpenCvSharp.Extensions;
// Set image
using System.Windows.Media.Imaging;
using System.Windows;

namespace CVHeadTrack {

    class FaceDetector : IDisposable {

        // Static finals
        // Key landmarks for 68 model
        private static readonly int[] keyIndices = { 30, 8, 36, 45, 48, 54 };
        // 3d model points for landmarks
        private static readonly double[] objectPoints = {
                0, 0, 0,
                0.0, -330.0, -65.0,
                -225.0, 170.0, -135.0,
                225.0, 170.0, -135.0,
                -150.0, -150.0, -125.0,
                150.0, -150.0, -125.0
            };
        private static readonly Mat objectPointsIn = new Mat(6, 3, MatType.CV_64F, objectPoints);
        // Distortion assumed to be 0
        private static readonly double[,] distortionCoefficients = {
                { 0 },
                { 0 },
                { 0 },
                { 0 }
            };
        private static readonly Mat distortionCoefficientsIn = new Mat(4, 1, MatType.CV_64F, distortionCoefficients);

        private bool disposedValue;
        // Video capture object
        private VideoCapture cap;
        private Mat cameraMatrix;
        // Neural net stuff
        private FrontalFaceDetector faceDetector;
        private ShapePredictor shapePredictor;
        private MovingAverage movingAverage;

        public FaceDetector(String predictorUrl) {
            this.faceDetector = Dlib.GetFrontalFaceDetector();
            this.shapePredictor = ShapePredictor.Deserialize(predictorUrl);
            this.movingAverage = new MovingAverage(25);
        }

        public bool ConnectCamera(String url) {
            this.cap = url.Equals("") ? new VideoCapture() : new VideoCapture(url);
            if (this.cap.IsOpened()) {
                Mat frame = new Mat();
                if (!this.cap.Read(frame)) {
                    frame.Dispose();
                    this.cap.Dispose();
                    return false;
                }
                double focalLength = frame.Cols;
                double[] center = { frame.Cols / 2, frame.Rows / 2 };
                double[,] cameraMatrixTemp = {
                    { focalLength, 0, center[0]},
                    { 0, focalLength, center[1]},
                    { 0, 0, 1}
                };
                this.cameraMatrix = new Mat(3, 3, MatType.CV_64F, cameraMatrixTemp);
                return true;
            }
            this.cap.Dispose();
            return false;
        }

        public Vec3d ProcessFace(DlibDotNet.Point[] points) {
            double[,] imagePoints = {
                { points[0].X, points[0].Y },
                { points[1].X, points[1].Y },
                { points[2].X, points[2].Y },
                { points[3].X, points[3].Y },
                { points[4].X, points[4].Y },
                { points[5].X, points[5].Y },
            };
            Mat imagePointsIn = new Mat(6, 2, MatType.CV_64F, imagePoints);
            Mat rvec = new Mat(3, 1, MatType.CV_64F);
            Mat tvec = new Mat(3, 1, MatType.CV_64F);
            // Solve init PnP for the rotational vector
            Cv2.SolvePnP(objectPointsIn, imagePointsIn, cameraMatrix, distortionCoefficientsIn, rvec, tvec);
            // Rodrigues to create 3d
            Mat rvec3d = new Mat(3, 3, MatType.CV_64F);
            Cv2.Rodrigues(rvec, rvec3d);
            // Decompose 3x3 into single vector
            Mat mtxR = new Mat();
            Mat mtxQ = new Mat();
            Vec3d angles = Cv2.RQDecomp3x3(rvec3d, mtxR, mtxQ);
            mtxQ.Dispose();
            mtxR.Dispose();
            rvec3d.Dispose();
            tvec.Dispose();
            rvec.Dispose();
            imagePointsIn.Dispose();
            return angles;
        }

        /**
         * Gets pose from captured frame, null if no pose/frame
         * Param : output array of pose data
         * {x, y, z, yaw, pitch, roll}
         * Return: success bool
         */
        public bool GetPose(double[] outputPose) {
            Mat frame = new Mat();
            if (!this.cap.Read(frame))
                return false;
            Cv2.CvtColor(frame, frame, ColorConversionCodes.BGR2RGB);
            byte[] frameBytearray = new byte[frame.Width * frame.Height * frame.ElemSize()];
            Marshal.Copy(frame.Data, frameBytearray, 0, frameBytearray.Length);
            using (Array2D<RgbPixel> img = Dlib.LoadImageData<RgbPixel>(frameBytearray, (uint)frame.Height, (uint)frame.Width, (uint)(frame.Width * frame.ElemSize()))) {
                // Get face from image
                Rectangle[] faces = this.faceDetector.Operator(img);
                // No face return null
                if (faces.Length == 0) {
                    frame.Dispose();
                    return false;
                }
                if (faces.Length > 1) {
                    Console.WriteLine("Not coded for multi face tracking, using first face detected");
                }
                // Get keypoints of main face
                Rectangle face = faces[0];
                FullObjectDetection shape = this.shapePredictor.Detect(img, face);
                DlibDotNet.Point[] keyPoints = new DlibDotNet.Point[6];
                for (int i = 0; i < keyIndices.Length; i++) {
                    DlibDotNet.Point point = shape.GetPart((uint)keyIndices[i]);
                    keyPoints[i] = point;
                }
                Vec3d pose = ProcessFace(keyPoints);
                this.movingAverage.Add(pose.Item1);
                outputPose[3] = this.movingAverage.Average();
                shape.Dispose();
            }
            frame.Dispose();
            Cv2.WaitKey();
            return true;
        }

        /**
         * Gets pose from captured frame, null if no pose/frame
         * This overload also set wpf image to output
         * Param : output array of pose data
         * {x, y, z, yaw, pitch, roll}
         * Return: success bool
         */
        public bool GetPose(double[] outputPose, System.Windows.Controls.Image imageSource) {
            Mat frame = new Mat();
            if (!this.cap.Read(frame))
                return false;
            Cv2.CvtColor(frame, frame, ColorConversionCodes.BGR2RGB);
            byte[] frameBytearray = new byte[frame.Width * frame.Height * frame.ElemSize()];
            Marshal.Copy(frame.Data, frameBytearray, 0, frameBytearray.Length);
            using (Array2D<RgbPixel> img = Dlib.LoadImageData<RgbPixel>(frameBytearray, (uint)frame.Height, (uint)frame.Width, (uint)(frame.Width * frame.ElemSize()))) {
                // Get face from image
                Rectangle[] faces = this.faceDetector.Operator(img);
                // No face return null
                if (faces.Length == 0) {
                    imageSource.Source = BitmapToImageSource(BitmapExtensions.ToBitmap(img));
                    frame.Dispose();
                    return false;
                }
                if (faces.Length > 1) {
                    Console.WriteLine("Not coded for multi face tracking, using first face detected");
                }
                // Get keypoints of main face
                Rectangle face = faces[0];
                FullObjectDetection shape = this.shapePredictor.Detect(img, face);
                DlibDotNet.Point[] keyPoints = new DlibDotNet.Point[6];
                for (int i = 0; i < keyIndices.Length; i++) {
                    DlibDotNet.Point point = shape.GetPart((uint)keyIndices[i]);
                    var rect = new Rectangle(point);
                    Dlib.DrawRectangle(img, rect, color: new RgbPixel(255, 255, 0), thickness: 4);
                    keyPoints[i] = point;
                }
                Vec3d pose = ProcessFace(keyPoints);
                this.movingAverage.Add(pose.Item1);
                outputPose[3] = this.movingAverage.Average();
                // Set image
                System.Drawing.Bitmap bi = BitmapExtensions.ToBitmap(img);
                imageSource.Source = BitmapToImageSource(bi);
                shape.Dispose();
            }
            frame.Dispose();
            Cv2.WaitKey();
            return true;
        }

        public System.Drawing.Bitmap TestImage(String src) {
            Mat frame = Cv2.ImRead(src);
            Cv2.CvtColor(frame, frame, ColorConversionCodes.BGR2RGB);
            byte[] frameBytearray = new byte[frame.Width * frame.Height * frame.ElemSize()];
            Marshal.Copy(frame.Data, frameBytearray, 0, frameBytearray.Length);
            using (Array2D<RgbPixel> img = Dlib.LoadImageData<RgbPixel>(frameBytearray, (uint)frame.Height, (uint)frame.Width, (uint)(frame.Width * frame.ElemSize()))) {
                // Get face from image
                Rectangle[] faces = this.faceDetector.Operator(img);
                // No face return null
                if (faces.Length == 0) {
                    frame.Dispose();
                    Console.WriteLine("No face detected");
                    frame.Dispose();
                    return BitmapExtensions.ToBitmap(img);
                }
                if (faces.Length > 1) {
                    Console.WriteLine("Not coded for multi face tracking, using first face detected");
                }
                // Get keypoints of main face
                Rectangle face = faces[0];
                FullObjectDetection shape = this.shapePredictor.Detect(img, face);
                DlibDotNet.Point[] keyPoints = new DlibDotNet.Point[6];
                for (int i = 0; i < keyIndices.Length; i++) {
                    DlibDotNet.Point point = shape.GetPart((uint)keyIndices[i]);
                    var rect = new Rectangle(point);
                    Dlib.DrawRectangle(img, rect, color: new RgbPixel(255, 255, 0), thickness: 4);
                    keyPoints[i] = point;
                }
                // return 
                frame.Dispose();
                return BitmapExtensions.ToBitmap(img);
            }
            
        }

        // Util method to draw System.Drawing.Bitmap to wpf image panel
        public static BitmapImage BitmapToImageSource(System.Drawing.Bitmap bitmap) {
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

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    if (!(this.cap is null))
                        this.cap.Dispose();
                    this.cameraMatrix.Dispose();
                    this.faceDetector.Dispose();
                    this.shapePredictor.Dispose();
                }
                disposedValue = true;
            }
        }

        ~FaceDetector() {
            Dispose(disposing: false);
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
