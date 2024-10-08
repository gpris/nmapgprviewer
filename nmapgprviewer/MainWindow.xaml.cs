﻿using Microsoft.Win32;
using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System.Windows.Input;
using System.Windows.Media.Media3D;


namespace nmapgprviewer
{
    class insGeoLocation
    {
        //Distance[m],Speed[m/s],Latitude[��],Longitude[��],Altitude[m],Roll[��],Pitch[��],Yaw[��],Gyro_X[dps],Gyro_Y[dps],Gyro_Z[dps],Acc_X[m/s2],Acc_Y[m/s2],Acc_Z[m/s2],Mag_X[mG],Mag_Y[mG],Mag_Z[mG],TriggerInfo,TriggerPulseCount
        [Name("Distance[m]")]
        public double distance { get; set; }

        [Name("Speed[m/s]")]
        public double speed { get; set; }

        [Name("Latitude[deg]")]
        public double latitude { get; set; }

        [Name("Longitude[deg]")]
        public double longitude { get; set; }

        [Name("Altitude[m]")]
        public double altitude { get; set; }

        [Name("Roll[deg]")]
        public double roll { get; set; }

        [Name("Pitch[deg]")]
        public double pitch { get; set; }

        [Name("Yaw[deg]")]
        public double yaw { get; set; }

        [Name("Gyro_X[dps]")]
        public double Gyro_X { get; set; }

        [Name("Gyro_Y[dps]")]
        public double Gyro_Y { get; set; }

        [Name("Gyro_Z[dps]")]
        public double Gyro_Z { get; set; }

        [Name("Acc_X[m/s2]")]
        public double Accel_X { get; set; }

        [Name("Acc_Y[m/s2]")]
        public double Accel_Y { get; set; }

        [Name("Acc_Z[m/s2]")]
        public double Accel_Z { get; set; }

        [Name("Mag_X[mG]")]
        public double Mag_X { get; set; }

        [Name("Mag_Y[mG]")]
        public double Mag_Y { get; set; }

        [Name("Mag_Z[mG]")]
        public double Mag_Z { get; set; }

        public int TriggerInfo { get; set; }

        public int TriggerPulseCount { get; set; }
    }

    public partial class MainWindow : Window
    {
        private static readonly string clientId = "sm8oqxebg0"; // 네이버 클라이언트 ID
        private static readonly string clientSecret = "IP2QQ91DvwVN1VjSq282eYzIh5LBNI7Ps1Yua65D"; // 네이버 클라이언트 시크릿
        public static double EARTH_CIR_METERS = 40075016.686; // 지구 둘레
        public static double degreePerMeter = 360.0 / EARTH_CIR_METERS; // 1미터당 몇 도인지 계산

        public double latMin = 0.0; // 최소 위도
        public double latMax = 0.0; // 최대 위도
        public double lngMin = 0.0; // 최소 경도
        public double lngMax = 0.0; // 최대 경도
        public double shiftDegreesEW = 0.0;
        public double shiftDegreesNS = 0.0;

        public String projectPath = "";
        public String projectFolder = "";
        public String projectName = "";

        public String insPath = "";
        public String roadFolder = "";
        public String[] roadImagePaths;
        public double[,] insData = new double[10000, 2];
        public int insDataIndex = 0;
        public double[] centerLatLng = { 37.331788, 126.713350 };
        public int zoom = 18;
        public double pathlatMin = 90.0; // 최소 위도
        public double pathlatMax = 0.0; // 최대 위도
        public double pathlngMin = 180.0; // 최소 경도
        public double pathlngMax = 0.0; // 최대 경도

        private Point _start;
        private Point _origin;

        private int width = 1024 * 2;
        private int height = 768 * 2;

        public WriteableBitmap mapBitmap;
        public WriteableBitmap roadsBitmap = new WriteableBitmap(1024*2, 768*2, 96,96, PixelFormats.Bgr32, null);

        public double toRadians(double degree) => degree * Math.PI / 180.0; // degree를 radian으로 변환

        public double toDegrees(double radian) => radian * 180.0 / Math.PI; // radian을 degree로 변환

        public double[] latLngToBounds(double lat, double lng, int zoom, int width, int height)
        {
            double metersPerPixelEW = EARTH_CIR_METERS / Math.Pow(2, zoom + 9); // 가로 미터당 픽셀 since zoom level is 0 to 20 21 levels
            double metersPerPixelNS = EARTH_CIR_METERS / Math.Pow(2, zoom + 9) * Math.Cos(toRadians(lat)); ; // 세로 미터당 픽셀

            double shiftMetersEW = width / 2.0 * metersPerPixelEW; // 가로로 이동한 거리
            double shiftMetersNS = height / 2.0 * metersPerPixelNS; // 세로로 이동한 거리

            shiftDegreesEW = shiftMetersEW * degreePerMeter; // 가로로 이동한 각도
            shiftDegreesNS = shiftMetersNS * degreePerMeter; // 세로로 이동한 각도

            latMin = lat - shiftDegreesNS;
            lngMin = lng - shiftDegreesEW;
            latMax = lat + shiftDegreesNS;
            lngMax = lng + shiftDegreesEW;

            double[] bounds = { latMin, lngMin, latMax, lngMax }; // NOT USED
            return bounds;
        }

        public int[] latLngToPos(double lat, double lng, int width, int height)
        {
            double dw = 2.0 * (shiftDegreesEW) / width;
            double dh = 2.0 * (shiftDegreesNS) / height;
            int x = (int)Math.Round((lng - lngMin) / dw);
            int y = (int)Math.Round((latMax - lat) / dh); // cordinate system is upside down comparing to lat system.


            int[] pos = { x, y }; // return x,y in array
            return pos;
        }

        public MainWindow()
        {
            InitializeComponent();
            //_ = LoadMapAsync2();
            MapImage.MouseWheel += MapImage_MouseWheel;
            MapImage.MouseLeftButtonDown += MapImage_MouseLeftButtonDown;
            MapImage.MouseMove += MapImage_MouseMove;
            MapImage.MouseLeftButtonUp += MapImage_MouseLeftButtonUp;
        }


        private async Task LoadMapAsync2()
        {
            //if (pathlatMax > latMax || pathlatMin < latMin || pathlngMax > lngMax || pathlngMin < lngMin)
            //{
            //    if(zoom -1 >= 0)
            //        zoom = zoom-1;
            //    else zoom = 0;
            //}
            zoom = 18; // test purpose
            
            //string url = $"https://naveropenapi.apigw.ntruss.com/map-static/v2/raster?center={longitude},{latitude}&level={zoom}&w={width}&h={height}&markers={markers}"; // with Marker
            string url = $"https://naveropenapi.apigw.ntruss.com/map-static/v2/raster?center={centerLatLng[1]},{centerLatLng[0]}&level={zoom}&w={width}&h={height}&maptype=satellite";
            try { 
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-NCP-APIGW-API-KEY-ID", clientId);
                    client.DefaultRequestHeaders.Add("X-NCP-APIGW-API-KEY", clientSecret);
                    double[] boundBox = latLngToBounds(centerLatLng[0], centerLatLng[1], zoom, width, height);

                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                        using (var stream = new System.IO.MemoryStream(imageBytes))
                        {
                            var mbitmap = new BitmapImage();
                            mbitmap.BeginInit();
                            mbitmap.StreamSource = stream;
                            mbitmap.CacheOption = BitmapCacheOption.OnLoad;
                            mbitmap.EndInit();

                            mapBitmap = new WriteableBitmap(mbitmap); // Map Image
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show($"Error: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void DrawRoadImages()
        {
            int roadimageindex = 0;
            foreach (string roadImgPath in roadImagePaths)
            {
                BitmapImage roadBitmapImage = new BitmapImage(new Uri(roadImgPath, UriKind.RelativeOrAbsolute));
                WriteableBitmap roadBitmap; 
                if (roadBitmapImage.Format != PixelFormats.Bgr32)
                    roadBitmap = new WriteableBitmap(new FormatConvertedBitmap(roadBitmapImage, PixelFormats.Bgr32, null, 0));
                else
                    roadBitmap = new WriteableBitmap(roadBitmapImage);

                int zoomWidth = 1;
                int zoomHeight = 3;

                int roadwidth = roadBitmap.PixelWidth;
                int roadheight = roadBitmap.PixelHeight;

                int[] orgxy = { 0, 0 };
                int[] destxy = { 0, 0 };
                            
                if (zoom > 13)
                {
                    float ratio = (float)(Math.Pow(2, zoom - 16) / 1000.0f);

                    zoomWidth = (int) (roadwidth* ratio);
                    zoomHeight =(int) (roadheight* ratio)+3;
                }

                //WriteableBitmap roadBitmap2 = roadBitmap.Resize(zoomWidth, zoomHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
                roadBitmap = roadBitmap.Resize(zoomWidth, zoomHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
                orgxy = latLngToPos(insData[roadimageindex, 0], insData[roadimageindex, 1], width, height);
                if (insData.Length > roadimageindex + 1)
                    destxy = latLngToPos(insData[roadimageindex + 1, 0], insData[roadimageindex + 1, 1], width, height);
                else
                    destxy = latLngToPos(insData[roadimageindex, 0], insData[roadimageindex, 1], width, height);

                double radian2 = Math.Atan2(destxy[1] - orgxy[1], destxy[0] - orgxy[0]);

                double angle2 = (radian2 * 180 / Math.PI) + 270;
                if(angle2 > 360) angle2 -= 360;
                if(angle2 < 0) angle2 += 360;
                DrawRotateBitmap(roadBitmap, angle2, orgxy[0], orgxy[1]);
                //DrawRotateBitmap(mapBitmap, angle2, orgxy[0], orgxy[1]);
                roadimageindex++;
            }
            MapImage.Source = mapBitmap; // Map Image
        }

        private void DrawRoadImages2()
        {
            int roadimageindex = 0;
            foreach (string roadImgPath in roadImagePaths)
            {
                BitmapImage roadBitmapImage = new BitmapImage(new Uri(roadImgPath, UriKind.RelativeOrAbsolute));
                WriteableBitmap roadBitmap;
                if (roadBitmapImage.Format != PixelFormats.Bgr32)
                    roadBitmap = new WriteableBitmap(new FormatConvertedBitmap(roadBitmapImage, PixelFormats.Bgr32, null, 0));
                else
                    roadBitmap = new WriteableBitmap(roadBitmapImage);


                for(int indx = 0; indx < 10; indx++)
                {
                    Int32Rect cropRect = new Int32Rect(0, indx*1000, (int)roadBitmap.Width, 1000);
                    int stride = cropRect.Width * (roadBitmap.Format.BitsPerPixel / 8);
                    byte[] pixelData = new byte[cropRect.Height * stride];

                    // 원본 이미지에서 픽셀 데이터를 읽어옵니다.
                    roadBitmap.CopyPixels(cropRect, pixelData, stride, 0);

                    // 자른 이미지를 생성합니다.
                    WriteableBitmap croppedBitmap = new WriteableBitmap(cropRect.Width, cropRect.Height, roadBitmap.DpiX, roadBitmap.DpiY, roadBitmap.Format, null);
                    croppedBitmap.WritePixels(new Int32Rect(0, 0, cropRect.Width, cropRect.Height), pixelData, stride, 0);

                    int roadwidth = croppedBitmap.PixelWidth;
                    int roadheight = croppedBitmap.PixelHeight;

                    int[] orgxy = { 0, 0 };
                    int[] destxy = { 0, 0 };

                    int zoomWidth = 1;
                    int zoomHeight = 3;

                    if (zoom > 13)
                    {
                        float ratio = (float)(Math.Pow(2, zoom - 16) / 1000.0f);

                        zoomWidth = (int)(roadwidth * ratio);
                        zoomHeight = (int)(roadheight * ratio)+2;
                    }

                    //WriteableBitmap roadBitmap2 = roadBitmap.Resize(zoomWidth, zoomHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
                    croppedBitmap = croppedBitmap.Resize(zoomWidth, zoomHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
                    orgxy = latLngToPos(insData[roadimageindex, 0], insData[roadimageindex, 1], width, height);
                    if ((int)(insData.Length/2)-1 > roadimageindex)
                        destxy = latLngToPos(insData[roadimageindex + 1, 0], insData[roadimageindex + 1, 1], width, height);
                    else
                        destxy = latLngToPos(insData[roadimageindex, 0], insData[roadimageindex, 1], width, height);

                    double radian2 = Math.Atan2(destxy[1] - orgxy[1], destxy[0] - orgxy[0]);

                    double angle2 = (radian2 * 180 / Math.PI) + 270;
                    if (angle2 > 360) angle2 -= 360;
                    if (angle2 < 0) angle2 += 360;
                    //DrawRotateBitmap(croppedBitmap, angle2, orgxy[0], orgxy[1]);
                    DrawRotateMapBitmap(croppedBitmap, angle2, orgxy[0], orgxy[1]);
                    roadimageindex++;
                    
                }
                if (roadimageindex >= 1000) break;
            }
            //MapImage.Source = roadsBitmap; // Map Image
            MapImage.Source = mapBitmap; // Map Image
        }

        private void DrawINSpoints()
        {
            int roadimageindex = 0;
            int radius = 3;
            WriteableBitmap writeableBitmap = new WriteableBitmap(2 * radius, 2 * radius, 96, 96, PixelFormats.Bgra32, null);


            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            int centerX = radius;
            int centerY = radius;
            // Calculate the circle's bounding box
            int x0 = centerX - radius;
            int x1 = centerX + radius;
            int y0 = centerY - radius;
            int y1 = centerY + radius;

            // Draw the circle
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        int index = (y * stride) + (x * 4);
                        if (index >= 0 && index < pixels.Length - 3)
                        {
                            pixels[index] = 0;     // Blue
                            pixels[index + 1] = 0; // Green
                            pixels[index + 2] = 255; // Red
                            pixels[index + 3] = 255; // Alpha
                        }
                    }
                }
            }

            // Write the pixels to the bitmap
            writeableBitmap.WritePixels(new Int32Rect(0, 0, 2* radius, 2 * radius), pixels, stride, 0);

            foreach (string roadImgPath in roadImagePaths)
            {
                int[] center = latLngToPos(insData[roadimageindex, 0], insData[roadimageindex, 1], width, height);

                int posX = center[0];
                int posY = center[1];

                //using (roadsBitmap.GetBitmapContext())
                //{
                //    roadsBitmap.Blit(new Rect(posX-radius, posY-radius, 2 * radius, 2 * radius), writeableBitmap, new Rect(0, 0, 2*radius, 2 * radius), WriteableBitmapExtensions.BlendMode.Alpha);
                //}

                using (mapBitmap.GetBitmapContext())
                {
                    mapBitmap.Blit(new Rect(posX - radius, posY - radius, 2 * radius, 2 * radius), writeableBitmap, new Rect(0, 0, 2 * radius, 2 * radius), WriteableBitmapExtensions.BlendMode.Alpha);
                }

                roadimageindex++;
            }
            //MapImage.Source = roadsBitmap; // Map Image
            MapImage.Source = mapBitmap; // Map Image
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a folder";
                dialog.ShowNewFolderButton = true;

                DialogResult result = dialog.ShowDialog();
                
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    projectPath = dialog.SelectedPath;
                    string[] arr1 = projectPath.Split('\\');
                    int arr1len = arr1.Length;
                    projectName = arr1[arr1len - 1];
                    projectFolder = projectPath.Remove(projectPath.Length - projectName.Length); // Folder should ends with '\'
                    insPath = projectPath + "\\" + projectName + "_INS\\Spatial\\" + projectName+ "_INS.csv";
                    roadFolder = projectPath + "\\" + projectName + "_표면결함\\";
                    GetRoadImageList(roadFolder);
                    ParseINSData(insPath);

                    _ = LoadMapAsync2();
                    //MapImage.Source = roadsBitmap;
                    MapImage.Source = mapBitmap;

                    if (mapBitmap != null)
                    {
                        DrawRoadImages2();
                        //DrawINSpoints();
                    }
                }
            }
        }

        private void ParseINSData(string insPath)
        {
            try
            {
                if (File.Exists(insPath))
                {

                    using (var reader = new StreamReader(insPath))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        //double[] tempLatLng = new double[2]; 
                        int fieldCount = csv.ColumnCount;
                        //string[] headers = csv.get().ToList().ToList();
                        double subtotalLat = 0.0;
                        double subtotalLng = 0.0;

                        double tminLat = 43.0;
                        double tmaxLat = 33.0;
                        double tminLng = 132.0;
                        double tmaxLng = 124.0;

                        double unitmeter = 1.0;

                        var records = csv.GetRecords<insGeoLocation>().ToList();
                        insDataIndex = 0;
                        List<double[]> tempLatLngs = new List<double[]>();

                        foreach (var record in records)
                        {
                            if (((int)(record.distance / unitmeter)) == 0) // Distance is 0.0m to unitmeter 
                            {
                                insData[0, 0] = records[0].latitude;
                                insData[0, 1] = records[0].longitude;
                                if (records[0].latitude < tminLat) tminLat = records[0].latitude;
                                if (records[0].latitude > tmaxLat) tmaxLat = records[0].latitude;
                                if (records[0].longitude < tminLng) tminLng = records[0].longitude;
                                if (records[0].longitude > tmaxLng) tmaxLng = records[0].longitude;
                            }

                            if (insDataIndex != ((int)(record.distance / unitmeter))) // in Case of Distance value just changed to next value (every 10m)
                            {
                                insDataIndex = (int)(record.distance / unitmeter);
                                insData[insDataIndex, 0] = record.latitude;
                                insData[insDataIndex, 1] = record.longitude;
                                tempLatLngs.Add(new double[] { record.latitude, record.longitude });
                                subtotalLat += record.latitude;
                                subtotalLng += record.longitude;
                                if (record.latitude < tminLat) tminLat = record.latitude;
                                if (record.latitude > tmaxLat) tmaxLat = record.latitude;
                                if (record.longitude < tminLng) tminLng = record.longitude;
                                if (record.longitude > tmaxLng) tmaxLng = record.longitude;
                            }

                        }
                        subtotalLat += records[0].latitude;
                        subtotalLng += records[0].longitude;
                        centerLatLng[0] = subtotalLat / (insDataIndex + 1);
                        centerLatLng[1] = subtotalLng / (insDataIndex + 1);
                        pathlatMax = tmaxLat;
                        pathlatMin = tminLat;
                        pathlngMax = tmaxLng;
                        pathlngMin = tminLng;
                        SelectedFolderPathTextBlock.Text = "insIndex: " + insDataIndex + "with center:(" + centerLatLng[0] + "," + centerLatLng[1] + ")";
                    }


                }
                else
                {
                    System.Windows.MessageBox.Show("Invalid INS File");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void GetRoadImageList(string roadFolder)
        {            
            FileInfo[] fileInfo;
            DirectoryInfo directoryInfo = new DirectoryInfo(roadFolder);
            if(directoryInfo.Exists)
            {
                DirectoryInfo[] subdirinfo = directoryInfo.GetDirectories();

                List<FileInfo> roadImages = new List<FileInfo>();
                foreach (DirectoryInfo subdir in subdirinfo)
                {
                    roadImages.AddRange(subdir.GetFiles("*.jpg"));
                }

                fileInfo = roadImages.ToArray();

                List<string> roadImagePathList = new List<string>();
                foreach (FileInfo file in fileInfo)
                {
                    roadImagePathList.Add(file.FullName);
                }
                roadImagePaths = roadImagePathList.ToArray();
            }
            else
            {
                System.Windows.MessageBox.Show("Invalid Project Folder");
            }
        }

        public void DrawRotateBitmap(BitmapSource source, double angle, int x, int y)
        {
            double radians = angle * Math.PI / 180;
            double cos = Math.Abs(Math.Cos(radians));
            double sin = Math.Abs(Math.Sin(radians));
            int newWidth = (int)(source.Width * cos + source.Height * sin);
            int newHeight = (int)(source.Width * sin + source.Height * cos);

            // Create a DrawingVisual to render the rotated image
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.PushTransform(new TranslateTransform(newWidth / 2, newHeight / 2));
                context.PushTransform(new RotateTransform(angle));
                // Set the rotation transform
                context.PushTransform(new TranslateTransform(-source.Width / 2, -source.Height / 2));
                // Draw the original image onto the DrawingVisual
                context.DrawImage(source, new Rect(0, 0, source.Width, source.Height));
            }

            // Render the DrawingVisual to a RenderTargetBitmap
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
               newWidth, newHeight, source.DpiX, source.DpiY, PixelFormats.Pbgra32);

            renderBitmap.Render(visual);

            WriteableBitmap renderWriteableBitmap = new WriteableBitmap(renderBitmap);
            // Convert the RenderTargetBitmap to a WritableBitmap
            using (roadsBitmap.GetBitmapContext())
            {
                // Calculate the top-left corner to draw the rotated image
                int drawX = x - newWidth / 2;
                int drawY = y - newHeight / 2;
                roadsBitmap.Blit(new Rect(drawX, drawY, newWidth, newHeight), renderWriteableBitmap, new Rect(0, 0, newWidth, newHeight), WriteableBitmapExtensions.BlendMode.Alpha);
            }
        }

        public void DrawRotateMapBitmap(BitmapSource source, double angle, int x, int y)
        {
            double radians = angle * Math.PI / 180;
            double cos = Math.Abs(Math.Cos(radians));
            double sin = Math.Abs(Math.Sin(radians));
            int newWidth = (int)(source.Width * cos + source.Height * sin);
            int newHeight = (int)(source.Width * sin + source.Height * cos);

            // Create a DrawingVisual to render the rotated image
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.PushTransform(new TranslateTransform(newWidth / 2, newHeight / 2));
                context.PushTransform(new RotateTransform(angle));
                // Set the rotation transform
                context.PushTransform(new TranslateTransform(-source.Width / 2, -source.Height / 2));
                // Draw the original image onto the DrawingVisual
                context.DrawImage(source, new Rect(0, 0, source.Width, source.Height));
            }

            // Render the DrawingVisual to a RenderTargetBitmap
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
               newWidth, newHeight, source.DpiX, source.DpiY, PixelFormats.Pbgra32);

            renderBitmap.Render(visual);

            WriteableBitmap renderWriteableBitmap = new WriteableBitmap(renderBitmap);
            // Convert the RenderTargetBitmap to a WritableBitmap
            using (mapBitmap.GetBitmapContext())
            {
                // Calculate the top-left corner to draw the rotated image
                int drawX = x - newWidth / 2;
                int drawY = y - newHeight / 2;
                mapBitmap.Blit(new Rect(drawX, drawY, newWidth, newHeight), renderWriteableBitmap, new Rect(0, 0, newWidth, newHeight), WriteableBitmapExtensions.BlendMode.Alpha);
            }
        }
        private void MapImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scale = e.Delta > 0 ? 1.1 : 1 / 1.1;
            ImageScaleTransform.ScaleX *= scale;
            ImageScaleTransform.ScaleY *= scale;
        }

        private void MapImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ImageScaleTransform.ScaleX = 1.0;
                ImageScaleTransform.ScaleY = 1.0;
                ImageTranslateTransform.X = (int)(width / 2);
                ImageTranslateTransform.Y = (int)(height / 2); ;
                return;
            }

            MapImage.CaptureMouse();
            _start = e.GetPosition(this);
            _origin = new Point(ImageTranslateTransform.X, ImageTranslateTransform.Y);
        }

        private void MapImage_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!MapImage.IsMouseCaptured) return;

            var p = e.GetPosition(this);
            ImageTranslateTransform.X = _origin.X + (p.X - _start.X);
            ImageTranslateTransform.Y = _origin.Y + (p.Y - _start.Y);
        }

        private void MapImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MapImage.ReleaseMouseCapture();
        }
    }


    //private async Task LoadMapAsync()
    //{
    //    //double latitude = 37.5665; // 예시 위도
    //    //double longitude = 126.9780; // 예시 경도
    //    double testLat = 37.331974;
    //    double testLng = 126.713529;

    //    int width = 1024;
    //    int height = 768;

    //    int x1, y1, x2, y2 = 0;

    //    Color lineColor = Colors.Red;
    //    int thickness = 5;

    //    // 마커 파라미터 추가
    //    //string markers = $"type:t|size:mid|pos:{longitude} {latitude}";
    //    //string markers = $"type:t|size:mid|pos:{testLng} {testLat}";

    //    //string url = $"https://naveropenapi.apigw.ntruss.com/map-static/v2/raster?center={longitude},{latitude}&level={zoom}&w={width}&h={height}&markers={markers}"; // with Marker
    //    string url = $"https://naveropenapi.apigw.ntruss.com/map-static/v2/raster?center={centerLatLng[1]},{centerLatLng[0]}&level={zoom}&w={width}&h={height}";

    //    using (HttpClient client = new HttpClient())
    //    {
    //        client.DefaultRequestHeaders.Add("X-NCP-APIGW-API-KEY-ID", clientId);
    //        client.DefaultRequestHeaders.Add("X-NCP-APIGW-API-KEY", clientSecret);
    //        double[] boundBox = latLngToBounds(centerLatLng[0], centerLatLng[1], zoom, width, height);
    //        //int[] pos = latLngToPos(testLat, testLng, width, height);
    //        HttpResponseMessage response = await client.GetAsync(url);

    //        if (response.IsSuccessStatusCode)
    //        {
    //            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
    //            using (var stream = new System.IO.MemoryStream(imageBytes))
    //            {
    //                var bitmap = new BitmapImage();
    //                bitmap.BeginInit();
    //                bitmap.StreamSource = stream;
    //                bitmap.CacheOption = BitmapCacheOption.OnLoad;
    //                bitmap.EndInit();

    //                WriteableBitmap writeableBitmap = new WriteableBitmap(bitmap);

    //                int[] n1pos = latLngToPos(centerLatLng[0], centerLatLng[1], width, height); // City Hall
    //                int[] n2pos = latLngToPos(testLat, testLng, width, height); // Hotel

    //                x1 = n1pos[0];
    //                y1 = n1pos[1];
    //                x2 = n2pos[0];
    //                y2 = n2pos[1];

    //                double radian2 = Math.Atan2(y2 - y1, x2 - x1);

    //                double angle2 = 90 + (radian2 * 180 / Math.PI);

    //                WriteableBitmap loadedBitmap = DrawZoomRoadBitmapfilename( n1pos[0], n1pos[1], zoom, "..\\..\\pmsdata\\s000000000.jpg");
    //                //WriteableBitmap loadedBitmap2 = DrawZoomRoadBitmapfilename( n1pos[0], n1pos[1], zoom, "C:\\Users\\Sanghyun\\Downloads\\희망공원로_2(21)_상_1\\희망공원로_2(21)_상_1_표면결함\\0\\희망공원로_2(21)_상_1_s000010000.jpg");
    //                ////WriteableBitmap loadedBitmap = DrawRoadBitmap(n1pos[0], n1pos[1], n2pos[0], n2pos[1]);

    //                DrawRotateBitmap(writeableBitmap,loadedBitmap, angle2, x1, y1);
    //                //DrawRotateBitmap(writeableBitmap, loadedBitmap2, angle2, x2, y2);
    //                DrawThickLine(writeableBitmap, n1pos[0], n1pos[1], n2pos[0], n2pos[1], lineColor, thickness);


    //                MapImage.Source = writeableBitmap;
    //            }
    //        }
    //        else
    //        {
    //            System.Windows.MessageBox.Show($"Error: {response.StatusCode}");
    //        }
    //    }
    //}

    //private void DrawLine(WriteableBitmap bitmap, int x1, int y1, int x2, int y2, Color color)
    //{
    //    int width = bitmap.PixelWidth;
    //    int height = bitmap.PixelHeight;
    //    int[] pixels = new int[width * height];
    //    bitmap.CopyPixels(pixels, width * 4, 0);

    //    int dx = Math.Abs(x2 - x1);
    //    int dy = Math.Abs(y2 - y1);
    //    int sx = x1 < x2 ? 1 : -1;
    //    int sy = y1 < y2 ? 1 : -1;
    //    int err = dx - dy;

    //    while (true)
    //    {
    //        if (x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
    //        {
    //            int pixelIndex = y1 * width + x1;
    //            pixels[pixelIndex] = color.A << 24 | color.R << 16 | color.G << 8 | color.B;
    //        }

    //        if (x1 == x2 && y1 == y2) break;
    //        int e2 = 2 * err;
    //        if (e2 > -dy)
    //        {
    //            err -= dy;
    //            x1 += sx;
    //        }
    //        if (e2 < dx)
    //        {
    //            err += dx;
    //            y1 += sy;
    //        }
    //    }

    //    bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
    //}

    //private void DrawThickLine(WriteableBitmap bitmap, int x1, int y1, int x2, int y2, Color color, int thickness)
    //{
    //    int width = bitmap.PixelWidth;
    //    int height = bitmap.PixelHeight;
    //    int[] pixels = new int[width * height];
    //    bitmap.CopyPixels(pixels, width * 4, 0);

    //    int dx = Math.Abs(x2 - x1);
    //    int dy = Math.Abs(y2 - y1);
    //    int sx = x1 < x2 ? 1 : -1;
    //    int sy = y1 < y2 ? 1 : -1;
    //    int err = dx - dy;

    //    while (true)
    //    {
    //        DrawThickPixel(pixels, width, height, x1, y1, color, thickness);

    //        if (x1 == x2 && y1 == y2) break;
    //        int e2 = 2 * err;
    //        if (e2 > -dy)
    //        {
    //            err -= dy;
    //            x1 += sx;
    //        }
    //        if (e2 < dx)
    //        {
    //            err += dx;
    //            y1 += sy;
    //        }
    //    }

    //    bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
    //}

    //private WriteableBitmap DrawRoadBitmap( int x1, int y1, int x2, int y2)
    //{
    //    BitmapImage bitmapImage = new BitmapImage(new Uri("..\\..\\pmsdata\\s000010000s.jpg", UriKind.RelativeOrAbsolute));

    //    //int width = Math.Abs(x2 - x1);
    //    //int height = Math.Abs(y2 - y1);
    //    int width = bitmapImage.PixelWidth;
    //    int height = bitmapImage.PixelHeight;

    //    WriteableBitmap resultbitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
    //    CopyBitmapImageToWriteableBitmap(bitmapImage, resultbitmap, 0, 0, width, height);
    //    return resultbitmap;
    //}

    //private WriteableBitmap DrawZoomRoadBitmap(int x1, int y1, int zoom)
    //{
    //    BitmapImage bitmapImage = new BitmapImage(new Uri("..\\..\\pmsdata\\s000010000.jpg", UriKind.RelativeOrAbsolute));

    //    //int width = Math.Abs(x2 - x1);
    //    //int height = Math.Abs(y2 - y1);
    //    int zoomWidth = 1;
    //    int zoomHeight = 2 ;

    //    int width = bitmapImage.PixelWidth;
    //    int height = bitmapImage.PixelHeight;

    //    if (zoom > 13)
    //    {
    //        float ratio = (float)(Math.Pow(2, zoom-16) / 1000.0f);

    //        zoomWidth = (int)(width * ratio);
    //        zoomHeight = (int)(height * ratio) ;
    //    }

    //    WriteableBitmap tempbitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
    //    CopyBitmapImageToWriteableBitmap(bitmapImage, tempbitmap, 0, 0, width, height);
    //    WriteableBitmap resultbitmap = tempbitmap.Resize(zoomWidth, zoomHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
    //    return resultbitmap;
    //}


    //private WriteableBitmap DrawZoomRoadBitmapfilename(int x1, int y1, int zoom, string filepath)
    //{
    //    BitmapImage bitmapImage = new BitmapImage(new Uri(filepath, UriKind.RelativeOrAbsolute));

    //    //int width = Math.Abs(x2 - x1);
    //    //int height = Math.Abs(y2 - y1);
    //    int zoomWidth = 1;
    //    int zoomHeight = 2;

    //    int width = bitmapImage.PixelWidth;
    //    int height = bitmapImage.PixelHeight;

    //    if (zoom > 13)
    //    {
    //        float ratio = (float)(Math.Pow(2, zoom - 16) / 1000.0f);

    //        zoomWidth = (int)(width * ratio);
    //        zoomHeight = (int)(height * ratio);

    //        //zoomWidth = 35;
    //        //zoomHeight = 100;
    //    }
    //    //WriteableBitmap tempbitmap = new WriteableBitmap(bitmapImage);
    //    //WriteableBitmap tempbitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
    //    WriteableBitmap tempbitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
    //    CopyBitmapImageToWriteableBitmap(bitmapImage, tempbitmap, 0, 0, width, height);
    //    WriteableBitmap resultbitmap = tempbitmap.Resize(zoomWidth, zoomHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
    //    //.Resize(zoomWidth, zoomHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
    //    return resultbitmap;
    //}

    //private void DrawThickPixel(int[] pixels, int width, int height, int x, int y, Color color, int thickness)
    //{
    //    int halfThickness = thickness / 2;
    //    for (int i = -halfThickness; i <= halfThickness; i++)
    //    {
    //        for (int j = -halfThickness; j <= halfThickness; j++)
    //        {
    //            int px = x + i;
    //            int py = y + j;
    //            if (px >= 0 && px < width && py >= 0 && py < height)
    //            {
    //                int pixelIndex = py * width + px;
    //                pixels[pixelIndex] = color.A << 24 | color.R << 16 | color.G << 8 | color.B;
    //            }
    //        }
    //    }
    //}

    //private void CopyBitmapImageToWriteableBitmap(BitmapImage bitmapImage, WriteableBitmap writeableBitmap, int x, int y, int w, int h)
    //{
    //    int width = (int)(bitmapImage.PixelWidth);
    //    int height = (int)(bitmapImage.PixelHeight);
    //    int stride = width * ((bitmapImage.Format.BitsPerPixel + 7) / 8);
    //    byte[] pixelData = new byte[height * stride];

    //    bitmapImage.CopyPixels(pixelData, stride, 0);
    //    writeableBitmap.WritePixels(new Int32Rect(x, y, width, height), pixelData, stride, 0); // 
    //}

    //public static WriteableBitmap RotateBitmap(BitmapSource source, double angle)
    //{
    //    // Create a DrawingVisual to render the rotated image
    //    DrawingVisual visual = new DrawingVisual();
    //    using (DrawingContext context = visual.RenderOpen())
    //    {
    //        // Set the rotation transform
    //        context.PushTransform(new RotateTransform(angle, source.Width / 2, source.Height / 2));
    //        // Draw the original image onto the DrawingVisual
    //        context.DrawImage(source, new Rect(0, 0, source.Width, source.Height));
    //    }

    //    // Render the DrawingVisual to a RenderTargetBitmap
    //    RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
    //        (int)source.Width, (int)source.Height, source.DpiX, source.DpiY, PixelFormats.Pbgra32);
    //    renderBitmap.Render(visual);

    //    // Convert the RenderTargetBitmap to a WritableBitmap
    //    WriteableBitmap writableBitmap = new WriteableBitmap(renderBitmap);
    //    return writableBitmap;
    //}


}