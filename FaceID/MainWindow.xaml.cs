//--------------------------------------------------------------------------------------
// Copyright 2015 Intel Corporation
// All Rights Reserved
//
// Permission is granted to use, copy, distribute and prepare derivative works of this
// software for any purpose and without fee, provided, that the above copyright notice
// and this statement appear in all copies.  Intel makes no representations about the
// suitability of this software for any purpose.  THIS SOFTWARE IS PROVIDED "AS IS."
// INTEL SPECIFICALLY DISCLAIMS ALL WARRANTIES, EXPRESS OR IMPLIED, AND ALL LIABILITY,
// INCLUDING CONSEQUENTIAL AND OTHER INDIRECT DAMAGES, FOR THE USE OF THIS SOFTWARE,
// INCLUDING LIABILITY FOR INFRINGEMENT OF ANY PROPRIETARY RIGHTS, AND INCLUDING THE
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.  Intel does not
// assume any responsibility for any errors which may appear in this software nor any
// responsibility to update it.
//--------------------------------------------------------------------------------------
using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Windows.Controls;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using System.Linq;

namespace FaceID
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread processingThread;
        private PXCMSenseManager senseManager;
        private PXCMFaceConfiguration.RecognitionConfiguration recognitionConfig;
        private PXCMFaceData faceData;
        private PXCMFaceData.RecognitionData recognitionData;
        private Int32 numFacesDetected;
        private PXCMCaptureManager captureManager;
        private string userId;
        private string dbState;
        private const int DatabaseUsers = 10;
        private const string DatabaseName = "UserDB";
        private const string DatabaseFilename = "database.bin";
        private bool doRegister;
        private bool doUnregister;
        private int faceRectangleHeight;
        private int faceRectangleWidth;
        private int faceRectangleX;
        private int faceRectangleY;
        private SerialPort _serialPort;
        private List<double> s = new List<double>();
        private bool first_process = true;
        private bool person = false;
        private double avg_list = 0;

        public MainWindow()
        {
            InitializeComponent();
            rectFaceMarker.Visibility = Visibility.Hidden;
            chkShowFaceMarker.IsChecked = true;
            numFacesDetected = 0;
            userId = string.Empty;
            dbState = string.Empty;
            doRegister = false;
            doUnregister = false;
           
            // Start SenseManage and configure the face module
            ConfigureRealSense();
            // Start the worker thread
            processingThread = new Thread(new ThreadStart(ProcessingThread));
            processingThread.Start();
            /*_serialPort = new SerialPort();
            _serialPort.PortName = "COM5";
            _serialPort.BaudRate = 9600;
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;
            _serialPort.Open();
            */
        }


        private void ConfigureRealSense()
        {
            PXCMFaceModule faceModule;
            PXCMFaceConfiguration faceConfig;
            
            // Start the SenseManager and session  
            senseManager = PXCMSenseManager.CreateInstance();
            captureManager = senseManager.captureManager;
            
            // Enable the color stream
            senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 640, 480, 0);
            senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, 640, 480, 0);

            // Enable the face module
            senseManager.EnableFace();
            faceModule = senseManager.QueryFace();
            faceConfig = faceModule.CreateActiveConfiguration();

            // Configure for 3D face tracking (if camera cannot support depth it will revert to 2D tracking)
            faceConfig.SetTrackingMode(PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_PLUS_DEPTH);

            // Enable facial recognition
            recognitionConfig = faceConfig.QueryRecognition();
            recognitionConfig.Enable();

            //Enable Landmark Detection

            faceConfig.landmarks.isEnabled = true;
            // Create a recognition database
            PXCMFaceConfiguration.RecognitionConfiguration.RecognitionStorageDesc recognitionDesc = new PXCMFaceConfiguration.RecognitionConfiguration.RecognitionStorageDesc();
            recognitionDesc.maxUsers = DatabaseUsers;
            //recognitionConfig.CreateStorage(DatabaseName, out recognitionDesc);
            //recognitionConfig.UseStorage(DatabaseName);
            LoadDatabaseFromFile();
            recognitionConfig.SetRegistrationMode(PXCMFaceConfiguration.RecognitionConfiguration.RecognitionRegistrationMode.REGISTRATION_MODE_CONTINUOUS);

            // Apply changes and initialize
            faceConfig.ApplyChanges();
            senseManager.Init();
            faceData = faceModule.CreateOutput();

            // Mirror image
            senseManager.QueryCaptureManager().QueryDevice().SetMirrorMode(PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL);
            // Release resources
            faceConfig.Dispose();
            faceModule.Dispose();
         }

        private void ProcessingThread()
        {
            while (senseManager.AcquireFrame(true) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                  
                // Acquire the color image data
                PXCMCapture.Sample sample = senseManager.QuerySample();

                Bitmap colorBitmap;
                PXCMImage.ImageData colorData;
                PXCMImage depth = sample.depth;
                PXCMImage color = sample.color;
                
                PXCMProjection projection = senseManager.QueryCaptureManager().QueryDevice().CreateProjection();
                
                PXCMImage compare_depth = projection.CreateDepthImageMappedToColor(depth, color);
                
                PXCMImage.ImageInfo image_info = compare_depth.QueryInfo();
                PXCMImage.PixelFormat p =  image_info.format;
                PXCMImage.ImageInfo depth_info = depth.QueryInfo();
                sample.color.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24, out colorData);
                colorBitmap = colorData.ToBitmap(0, sample.color.info.width, sample.color.info.height);
                // Get face data
                if (faceData != null)
                {
                    //Updates face data to most recent face data
                    faceData.Update();
                    if (faceData != null)
                    {
                        //Gets the number of faces
                        numFacesDetected = faceData.QueryNumberOfDetectedFaces();
                        bool num_faces = false;
                        if(numFacesDetected ==1 )
                        {
                            num_faces = true;
                        }                
                        if (num_faces)
                        {
                            if(userId == "No Users in View")
                            {
                                userId = "Prcessing";
                            }
                            // Get the first face detected (index 0)
                            PXCMFaceData.Face face = faceData.QueryFaceByIndex(0);

                            //Get landmark points
                            PXCMFaceData.LandmarksData landmarks = face.QueryLandmarks();
                            if (landmarks != null)
                            {
                                int total_points = landmarks.QueryNumPoints();
                                PXCMFaceData.LandmarkPoint[] v = new PXCMFaceData.LandmarkPoint[total_points];
                                landmarks.QueryPoints(out v);
                                float avg = 0;
                                for (int i = 0; i < total_points; i++)
                                {
                                    avg += v[i].world.z;
                                }

                                //Found Standard Deviation
                                float k = 0;
                                avg = avg / total_points;
                                for (int i = 0; i < total_points; i++)
                                {
                                    k += (avg - v[i].world.z) * (avg - v[i].world.z);
                                }

                                double std = 0;
                                std = Math.Sqrt(k * 1 / total_points);
                                s.Add(std);
                            }
                            // Retrieve face location data
                            PXCMFaceData.DetectionData faceDetectionData = face.QueryDetection();
                                if (faceDetectionData != null)
                                {
                                    //Bounding Box for Face
                                    //X and Y are the coordinates for the top left pixel of the rectangle, 
                                    //h and w are the height and width of the rectangle
                                    PXCMRectI32 faceRectangle;
                                    faceDetectionData.QueryBoundingRect(out faceRectangle);
                                    faceRectangleHeight = faceRectangle.h;
                                    faceRectangleWidth = faceRectangle.w;
                                    faceRectangleX = faceRectangle.x;
                                    faceRectangleY = faceRectangle.y;
                                int n = faceRectangleHeight + 5;
                                }
                            if(s.Count == 14)
                            {
                                avg_list = s.Average();
                                first_process = false;
                            }
                            /*
                        PXCMPointF32 point = new PXCMPointF32();
                        List<PXCMPointF32> color_points = new List<PXCMPointF32>();
                        for(int i = faceRectangleX; i < faceRectangleX + faceRectangleWidth; i++)
                        {
                            for(int j = faceRectangleY; j < faceRectangleY + faceRectangleHeight; j++)
                            {
                                point.x = i;
                                point.y = j;
                                color_points.Add(point);
                            }
                        }

                        PXCMPointF32[] depth_points = new PXCMPointF32[color_points.Count];

                        projection.MapColorToDepth(depth, color_points.ToArray(), depth_points);
                        int size = depth_info.width * depth_info.height;
                        PXCMPoint3DF32[] vertices = new PXCMPoint3DF32[size];
                        projection.QueryVertices(depth, vertices);
                        int v_y = 0;
                        int u = 0;
                        PXCMPoint3DF32 f_point = new PXCMPoint3DF32();
                        PXCMPoint3DF32[] final_points = new PXCMPoint3DF32[depth_points.Length];
                        for(int i = 0; i < depth_points.Length/25; i++)
                        {
                            point.y = depth_points[i].y;
                            point.x = depth_points[i].x;
                            v_y = (int)point.y;
                            u = (int)point.x;
                            if (v_y*depth_info.width + u < vertices.Length && v_y*depth_info.width + u > 0) {
                                f_point = vertices[v_y * depth_info.width + u];
                                final_points[i] = f_point;
                            }
                        }
                        avg = 0;
                        int total = 0;
                        for (int i = 0; i < final_points.Length/25; i++)
                        {
                            if (final_points[i].z != 0)
                            {
                                avg += final_points[i].z;
                                total++;
                            }
                        }
                        avg = avg / total;
                        float l = 0;
                        for (int i = 0; i < final_points.Length/25; i++)
                        {
                            if (final_points[i].z != 0)
                            {
                                l += (avg - final_points[i].z) * (avg - final_points[i].z);
                            }
                        }
                        */
                            if(avg_list > .01)
                            {
                                person = true;
                            }
                            

                            if(s.Count < 14 && first_process)
                            {
                                userId = "Processing";
                                person = false;
                            }
                            if(avg_list > 0 && avg_list < .01)
                            {
                                userId = "Invalid";
                                person = false;
                            }
                            
                            if(s.Count() == 14)
                            {
                                s.Clear();

                            }
                            // Process face recognition data
                            if (face != null && person)
                                {
                                                                     
                                // Retrieve the recognition data instance
                                recognitionData = face.QueryRecognition();

                                    // Set the user ID and process register/unregister logic
                                    //Only executes when user is registered
                                    //doRegister = false
                                    if (recognitionData.IsRegistered())
                                    {
                                        userId = Convert.ToString(recognitionData.QueryUserID());
                                        System.Diagnostics.Debug.WriteLine(userId);
                                        //_serialPort.Write(1.ToString());
                                        if (doUnregister)
                                        {
                                            recognitionData.UnregisterUser();
                                            doUnregister = false;
                                        }
                                    }
                                    else
                                    {
                                        //_serialPort.Write(0.ToString());
                                        if (doRegister)
                                        {
                                            recognitionData.RegisterUser();

                                            // Capture a jpg image of registered user
                                            colorBitmap.Save("image.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                                            doRegister = false;
                                        }
                                        else
                                        {
                                            userId = "Unrecognized";

                                        }
                                    }
                                }
                            }
                        
                        else if (numFacesDetected > 1)
                        {
                            userId = "Too many faces in view";
                        }
                        else
                        {
                            //_serialPort.Write(0.ToString());
                            userId = "No users in view";
                            first_process = true;
                        }
                    }

                    // Display the color stream and other UI elements
                    UpdateUI(colorBitmap);

                    // Release resources
                    colorBitmap.Dispose();
                    sample.color.ReleaseAccess(colorData);
                    sample.color.Dispose();

                    // Release the frame
                    senseManager.ReleaseFrame();
                }
            }
        }

        private void UpdateUI(Bitmap bitmap)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                // Display  the color image
                if (bitmap != null)
                {
                    imgColorStream.Source = ConvertBitmap.BitmapToBitmapSource(bitmap);
                }

                 // Update UI elements
                lblNumFacesDetected.Content = String.Format("Faces Detected: {0}", numFacesDetected);
                lblUserId.Content = String.Format("User ID: {0}", userId);
                lblDatabaseState.Content = String.Format("Database: {0}", dbState);

                // Change picture border color depending on if user is in camera view
                if (numFacesDetected > 0)
                {
                    bdrPictureBorder.BorderBrush = System.Windows.Media.Brushes.LightGreen;
                }
                else
                {
                    bdrPictureBorder.BorderBrush = System.Windows.Media.Brushes.Red;
                }

                // Show or hide face marker
                if ((numFacesDetected > 0) && (chkShowFaceMarker.IsChecked == true))
                {
                    // Show face marker
                    rectFaceMarker.Height = faceRectangleHeight;
                    rectFaceMarker.Width = faceRectangleWidth;
                    Canvas.SetLeft(rectFaceMarker, faceRectangleX);
                    Canvas.SetTop(rectFaceMarker, faceRectangleY);
                    rectFaceMarker.Visibility = Visibility.Visible;

                    // Show floating ID label
                    lblFloatingId.Content = String.Format("User ID: {0}", userId);
                    Canvas.SetLeft(lblFloatingId, faceRectangleX);
                    Canvas.SetTop(lblFloatingId, faceRectangleY - 20);
                    lblFloatingId.Visibility = Visibility.Visible;
                }
                else
                {
                    // Hide the face marker and floating ID label
                    rectFaceMarker.Visibility = Visibility.Hidden;
                    lblFloatingId.Visibility = Visibility.Hidden;
                }
            }));

            // Release resources
            bitmap.Dispose();
        }

        private void LoadDatabaseFromFile()
        {
            if (File.Exists(DatabaseFilename))
            {
                Byte[] buffer = File.ReadAllBytes(DatabaseFilename);
                recognitionConfig.SetDatabaseBuffer(buffer);
                dbState = "Loaded";
            }
            else
            {
                dbState = "Not Found";
            }
        }

        private void SaveDatabaseToFile()
        {
            // Allocate the buffer to save the database
            PXCMFaceData.RecognitionModuleData recognitionModuleData = faceData.QueryRecognitionModule();
            Int32 nBytes = recognitionModuleData.QueryDatabaseSize();
            Byte[] buffer = new Byte[nBytes];

            // Retrieve the database buffer
            recognitionModuleData.QueryDatabaseBuffer(buffer);

            // Save the buffer to a file
            // (NOTE: production software should use file encryption for privacy protection)
            File.WriteAllBytes(DatabaseFilename, buffer);
            dbState = "Saved";
        }

        private void DeleteDatabaseFile()
        {
            if (File.Exists(DatabaseFilename))
            {
                File.Delete(DatabaseFilename);
                dbState = "Deleted";
            }
            else
            {
                dbState = "No Database Found";
            }
            PXCMFaceData.RecognitionModuleData recognitionModuleData = faceData.QueryRecognitionModule();
            for (int i = 100; i < 100 + DatabaseUsers; i++)
            {
                recognitionModuleData.UnregisterUserByID(i);
            }

        }

        private void ReleaseResources()
        {
            // Stop the worker thread
            processingThread.Abort();

            // Release resources
            faceData.Dispose();
            senseManager.Dispose();
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            doRegister = true;
        }

        private void btnUnregister_Click(object sender, RoutedEventArgs e)
        {
            doUnregister = true;
        }

        private void btnSaveDatabase_Click(object sender, RoutedEventArgs e)
        {
            SaveDatabaseToFile();
        }

        private void btnDeleteDatabase_Click(object sender, RoutedEventArgs e)
        {
            DeleteDatabaseFile();
        }
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            ReleaseResources();
            this.Close();
            _serialPort.Close();

        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ReleaseResources();
        }
    }
}
