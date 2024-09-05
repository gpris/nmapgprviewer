using Microsoft.Win32;
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
        public double[,] insData = new double[1000, 2];
        public int insDataIndex = 0;
        public double[] centerLatLng = { 37.331788, 126.713350 };
        public int zoom = 18;
        public double pathlatMin = 90.0; // 최소 위도
        public double pathlatMax = 0.0; // 최대 위도
        public double pathlngMin = 180.0; // 최소 경도
        public double pathlngMax = 0.0; // 최대 경도

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
            int x = (int)((lng - lngMin) / dw);
            int y = (int)((latMax - lat) / dh); // cordinate system is upside down comparing to lat system.


            int[] pos = { x, y }; // return x,y in array
            return pos;
        }

        public MainWindow()
        {
            InitializeComponent();
            _ = LoadMapAsync();
        }

        private async Task LoadMapAsync()
        {
            //double latitude = 37.5665; // 예시 위도
            //double longitude = 126.9780; // 예시 경도
            //double testLat = 37.564885;
            //double testLng = 126.978398;

            int width = 1024;
            int height = 768;

            // 마커 파라미터 추가
            //string markers = $"type:t|size:mid|pos:{longitude} {latitude}";
            //string markers = $"type:t|size:mid|pos:{testLng} {testLat}";

            //string url = $"https://naveropenapi.apigw.ntruss.com/map-static/v2/raster?center={longitude},{latitude}&level={zoom}&w={width}&h={height}&markers={markers}"; // with Marker
            string url = $"https://naveropenapi.apigw.ntruss.com/map-static/v2/raster?center={centerLatLng[1]},{centerLatLng[0]}&level={zoom}&w={width}&h={height}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-NCP-APIGW-API-KEY-ID", clientId);
                client.DefaultRequestHeaders.Add("X-NCP-APIGW-API-KEY", clientSecret);
                double[] boundBox = latLngToBounds(centerLatLng[0], centerLatLng[1], zoom, width, height);
                //int[] pos = latLngToPos(testLat, testLng, width, height);
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                    using (var stream = new System.IO.MemoryStream(imageBytes))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        WriteableBitmap writeableBitmap = new WriteableBitmap(bitmap);

                        //int[] n1pos  = latLngToPos(latitude, longitude, width, height); // City Hall
                        //int[] n2pos = latLngToPos(testLat, testLng, width, height); // Hotel

                        //x1 = n1pos[0];
                        //y1 = n1pos[1];
                        //x2 = n2pos[0];
                        //y2 = n2pos[1];  

                        //double radian2 = Math.Atan2(y2 - y1, x2 - x1);

                        //double angle2 = 90+(radian2 * 180 / Math.PI );

                        //WriteableBitmap loadedBitmap = DrawZoomRoadBitmap( n1pos[0], n1pos[1], zoom);
                        ////WriteableBitmap loadedBitmap = DrawRoadBitmap(n1pos[0], n1pos[1], n2pos[0], n2pos[1]);

                        //DrawRotateBitmap(writeableBitmap,loadedBitmap, angle2, x1, y1);
                        //DrawThickLine(writeableBitmap, n1pos[0], n1pos[1], n2pos[0], n2pos[1], lineColor, thickness);


                        MapImage.Source = writeableBitmap;
                    }
                    //double dlat = latMax - latitude;
                    //double dlng = lngMax - longitude;
                    //BoundLatLng.Text = $"({latMin}, {lngMin}), ({latMax}, {lngMax}) dlat:{dlat}, dlng:{dlng}, posx:{pos[0]}, posy={pos[1]}";
                    //BoundLatLng.Text = $"({boundBox[0]}, {boundBox[1]}), ({boundBox[2]}, {boundBox[3]}) dlat:
                    //{dlat}, dlng:{dlng}";

                }
                else
                {
                    System.Windows.MessageBox.Show($"Error: {response.StatusCode}");
                }
            }
        }

        private async Task LoadMapAsync2()
        {
            int width = 1024;
            int height = 768;
            // 마커 파라미터 추가
            //string markers = $"type:t|size:mid|pos:{centerLatLng[1]} {centerLatLng[0]}";
            if (pathlatMax > latMax || pathlatMin < latMin || pathlngMax > lngMax || pathlngMin < lngMin)
            {
                if(zoom -1 >= 0)
                    zoom = zoom-1;
                else zoom = 0;
            }

            //string url = $"https://naveropenapi.apigw.ntruss.com/map-static/v2/raster?center={longitude},{latitude}&level={zoom}&w={width}&h={height}&markers={markers}"; // with Marker
            string url = $"https://naveropenapi.apigw.ntruss.com/map-static/v2/raster?center={centerLatLng[1]},{centerLatLng[0]}&level={zoom}&w={width}&h={height}";

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

                        WriteableBitmap mapBitmap = new WriteableBitmap(mbitmap); // Map Image

                        WriteableBitmap roadBitmapLayer = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null); // Road Image Layer 
                        int roadimageindex = 0;
                        foreach ( string roadImgPath in roadImagePaths)
                        {
                            BitmapImage roadBitmapImage = new BitmapImage(new Uri(roadImgPath, UriKind.RelativeOrAbsolute));
                            WriteableBitmap roadBitmap = new WriteableBitmap(roadBitmapImage);
                            CopyBitmapImageToWriteableBitmap(roadBitmapImage, roadBitmap, 0, 0, roadBitmap.PixelWidth, roadBitmap.PixelHeight);
                            int zoomWidth = 1;
                            int zoomHeight = 3;

                            int roadwidth = roadBitmap.PixelWidth;
                            int roadheight = roadBitmap.PixelHeight;
                            int[] orgxy = { 0, 0 };
                            int[] destxy = { 0, 0 };
                            if (zoom > 13)
                            {
                                float ratio = (float)(Math.Pow(2, zoom - 16) / 1000.0f);

                                zoomWidth = (int)(roadwidth * ratio);
                                zoomHeight =(int)(roadheight * ratio);
                            }

                            roadBitmap.Resize(zoomWidth, zoomHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
                            if (roadBitmap != null)
                            {
                                roadBitmap = null;
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }

                            orgxy = latLngToPos(insData[roadimageindex, 0], insData[roadimageindex, 1], width, height);
                            if (insData.Length > roadimageindex + 1)
                                destxy = latLngToPos(insData[roadimageindex + 1, 0], insData[roadimageindex + 1, 1], width, height);
                            else
                                destxy = latLngToPos(insData[roadimageindex, 0], insData[roadimageindex, 1], width, height);

                            double radian2 = Math.Atan2(destxy[1] - orgxy[1], destxy[0] - orgxy[0]);

                            double angle2 = 90+(radian2 * 180 / Math.PI );
                            DrawRotateBitmap(roadBitmapLayer, roadBitmap, angle2, orgxy[0], orgxy[1]);
                            if (roadBitmap != null)
                            {
                                roadBitmap = null;
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
                            roadimageindex++;
                            MapImage.Source = roadBitmapLayer; // Road Image Layer
                            
                        }

                        MapImage.Source = roadBitmapLayer; // Road Image Layer
                        //MapImage.Source = mapBitmap; // Map Image
                    }
                    //double dlat = latMax - latitude;
                    //double dlng = lngMax - longitude;
                    //BoundLatLng.Text = $"({latMin}, {lngMin}), ({latMax}, {lngMax}) dlat:{dlat}, dlng:{dlng}, posx:{pos[0]}, posy={pos[1]}";
                    //BoundLatLng.Text = $"({boundBox[0]}, {boundBox[1]}), ({boundBox[2]}, {boundBox[3]}) dlat:
                    //{dlat}, dlng:{dlng}";

                }
                else
                {
                    System.Windows.MessageBox.Show($"Error: {response.StatusCode}");
                }
            }
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
                    //SelectedFolderPathTextBlock.Text = "Selected Folder: " + dialog.SelectedPath;
                    // Processing Project name from path ,
                    // make fill out project relative folders name ,
                    // get filelist from INS and road image 
                    //System.IO.FileInfo fileInfo = new System.IO.FileInfo(dialog.SelectedPath);
                    //string projectDirNames = fileInfo.DirectoryName;
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
                }
            }
        }

        private void ParseINSData(string insPath)
        {
            try
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

                    var records = csv.GetRecords<insGeoLocation>().ToList();
                    insDataIndex = 0;
                    List<double[]> tempLatLngs = new List<double[]>();

                    foreach (var record in records)
                    {
                        if(((int)(record.distance / 10))==0) // Distance is 0.0m to 9.9m
                        {
                            insData[0, 0] = records[0].latitude;
                            insData[0, 1] = records[0].longitude;
                            if (records[0].latitude < tminLat) tminLat = records[0].latitude;
                            if (records[0].latitude > tmaxLat) tmaxLat = records[0].latitude;
                            if (records[0].longitude < tminLng) tminLng = records[0].longitude;
                            if (records[0].longitude > tmaxLng) tmaxLng = records[0].longitude;
                        }
 
                        if (insDataIndex != ((int)(record.distance / 10))) // in Case of Distance value just changed to next value (every 10m)
                        {
                            insDataIndex = (int)(record.distance/10);
                            insData[insDataIndex , 0] = record.latitude;
                            insData[insDataIndex , 1] = record.longitude;
                            tempLatLngs.Add(new double[] { record.latitude, record.longitude });
                            subtotalLat += record.latitude;
                            subtotalLng += record.longitude;
                            if(record.latitude < tminLat) tminLat = record.latitude;
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
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void GetRoadImageList(string roadFolder)
        {
            FileInfo[] fileInfo;
            DirectoryInfo directoryInfo = new DirectoryInfo(roadFolder);
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

        private void DrawLine(WriteableBitmap bitmap, int x1, int y1, int x2, int y2, Color color)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int[] pixels = new int[width * height];
            bitmap.CopyPixels(pixels, width * 4, 0);

            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
                {
                    int pixelIndex = y1 * width + x1;
                    pixels[pixelIndex] = color.A << 24 | color.R << 16 | color.G << 8 | color.B;
                }

                if (x1 == x2 && y1 == y2) break;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
        }

        private void DrawThickLine(WriteableBitmap bitmap, int x1, int y1, int x2, int y2, Color color, int thickness)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int[] pixels = new int[width * height];
            bitmap.CopyPixels(pixels, width * 4, 0);

            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                DrawThickPixel(pixels, width, height, x1, y1, color, thickness);

                if (x1 == x2 && y1 == y2) break;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
        }

        private WriteableBitmap DrawRoadBitmap( int x1, int y1, int x2, int y2)
        {
            BitmapImage bitmapImage = new BitmapImage(new Uri("..\\..\\pmsdata\\s000010000s.jpg", UriKind.RelativeOrAbsolute));

            //int width = Math.Abs(x2 - x1);
            //int height = Math.Abs(y2 - y1);
            int width = bitmapImage.PixelWidth;
            int height = bitmapImage.PixelHeight;

            WriteableBitmap resultbitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            CopyBitmapImageToWriteableBitmap(bitmapImage, resultbitmap, 0, 0, width, height);
            return resultbitmap;
        }

        private WriteableBitmap DrawZoomRoadBitmap(int x1, int y1, int zoom)
        {
            BitmapImage bitmapImage = new BitmapImage(new Uri("..\\..\\pmsdata\\s000010000.jpg", UriKind.RelativeOrAbsolute));

            //int width = Math.Abs(x2 - x1);
            //int height = Math.Abs(y2 - y1);
            int zoomWidth = 1;
            int zoomHeight = 2 ;

            int width = bitmapImage.PixelWidth;
            int height = bitmapImage.PixelHeight;

            if (zoom > 13)
            {
                float ratio = (float)(Math.Pow(2, zoom-16) / 1000.0f);

                zoomWidth = (int)(width * ratio);
                zoomHeight = (int)(height * ratio) ;
            }

            WriteableBitmap tempbitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            CopyBitmapImageToWriteableBitmap(bitmapImage, tempbitmap, 0, 0, width, height);
            WriteableBitmap resultbitmap = tempbitmap.Resize(zoomWidth, zoomHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
            return resultbitmap;
        }

        private void DrawThickPixel(int[] pixels, int width, int height, int x, int y, Color color, int thickness)
        {
            int halfThickness = thickness / 2;
            for (int i = -halfThickness; i <= halfThickness; i++)
            {
                for (int j = -halfThickness; j <= halfThickness; j++)
                {
                    int px = x + i;
                    int py = y + j;
                    if (px >= 0 && px < width && py >= 0 && py < height)
                    {
                        int pixelIndex = py * width + px;
                        pixels[pixelIndex] = color.A << 24 | color.R << 16 | color.G << 8 | color.B;
                    }
                }
            }
        }

        private void CopyBitmapImageToWriteableBitmap(BitmapImage bitmapImage, WriteableBitmap writeableBitmap, int x, int y, int w, int h)
        {
            int width = (int)(bitmapImage.PixelWidth);
            int height = (int)(bitmapImage.PixelHeight);
            int stride = width * ((bitmapImage.Format.BitsPerPixel + 7) / 8);
            byte[] pixelData = new byte[height * stride];

            bitmapImage.CopyPixels(pixelData, stride, 0);
            writeableBitmap.WritePixels(new Int32Rect(x, y, width, height), pixelData, stride, 0); // 
        }

        public static WriteableBitmap RotateBitmap(BitmapSource source, double angle)
        {
            // Create a DrawingVisual to render the rotated image
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                // Set the rotation transform
                context.PushTransform(new RotateTransform(angle, source.Width / 2, source.Height / 2));
                // Draw the original image onto the DrawingVisual
                context.DrawImage(source, new Rect(0, 0, source.Width, source.Height));
            }

            // Render the DrawingVisual to a RenderTargetBitmap
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                (int)source.Width, (int)source.Height, source.DpiX, source.DpiY, PixelFormats.Pbgra32);
            renderBitmap.Render(visual);

            // Convert the RenderTargetBitmap to a WritableBitmap
            WriteableBitmap writableBitmap = new WriteableBitmap(renderBitmap);
            return writableBitmap;
        }

        public void DrawRotateBitmap(WriteableBitmap outputBitmap, BitmapSource source, double angle, int x, int y)
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
            using (outputBitmap.GetBitmapContext())
            {
                outputBitmap.Blit(new Rect(x, y, newWidth, newHeight), renderWriteableBitmap, new Rect(0, 0, newWidth, newHeight), WriteableBitmapExtensions.BlendMode.Alpha);
            }

        }
    }
}