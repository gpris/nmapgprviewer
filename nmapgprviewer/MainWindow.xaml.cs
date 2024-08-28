using Microsoft.Win32;
using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace nmapgprviewer
{
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

        public double toRadians(double degree) => degree * Math.PI / 180.0; // degree를 radian으로 변환

        public double toDegrees(double radian) => radian * 180.0 / Math.PI; // radian을 degree로 변환

        public double [] latLngToBounds(double lat, double lng, int zoom, int width, int height) 
        {
            double metersPerPixelEW = EARTH_CIR_METERS / Math.Pow(2, zoom+9); // 가로 미터당 픽셀 since zoom level is 0 to 20 21 levels
            double metersPerPixelNS = EARTH_CIR_METERS / Math.Pow(2, zoom+9) * Math.Cos(toRadians(lat)); ; // 세로 미터당 픽셀

            double shiftMetersEW = width/ 2.0 * metersPerPixelEW; // 가로로 이동한 거리
            double shiftMetersNS = height / 2.0 * metersPerPixelNS; // 세로로 이동한 거리

            shiftDegreesEW = shiftMetersEW * degreePerMeter; // 가로로 이동한 각도
            shiftDegreesNS = shiftMetersNS * degreePerMeter; // 세로로 이동한 각도

            latMin = lat - shiftDegreesNS;
            lngMin = lng - shiftDegreesEW;
            latMax = lat + shiftDegreesNS;
            lngMax = lng + shiftDegreesEW;

            double [] bounds = {latMin, lngMin, latMax, lngMax}; // NOT USED
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
            LoadMapAsync();
        }

        private async Task LoadMapAsync()
        {
            double latitude = 37.5665; // 예시 위도
            double longitude = 126.9780; // 예시 경도
            // (37.5599687961511, 126.967013671875), (37.5730312038489, 126.988986328125) dlat:0.0065, dlng:0.0109
            // (37.5632343980755, 126.972506835937), (37.5697656019245, 126.983493164062) dlat:0.0032, dlng:0.0054
            //latitude +=  (1.0 * 0.0065); // was latMin 
            //longitude -= (1.0 * 0.01098); // was lngMin 
            double testLat = 37.564885;
            double testLng = 126.978398;

            int zoom = 16;
            int width = 1024;
            int height = 768;

            // 마커 파라미터 추가
            //string markers = $"type:t|size:mid|pos:{longitude} {latitude}";
            string markers = $"type:t|size:mid|pos:{testLng} {testLat}";

            string url = $"https://naveropenapi.apigw.ntruss.com/map-static/v2/raster?center={longitude},{latitude}&level={zoom}&w={width}&h={height}&markers={markers}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-NCP-APIGW-API-KEY-ID", clientId);
                client.DefaultRequestHeaders.Add("X-NCP-APIGW-API-KEY", clientSecret);
                double[] boundBox = latLngToBounds(latitude, longitude, zoom, width, height);
                int[] pos = latLngToPos(testLat, testLng, width, height);
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

                        // 선의 시작점과 끝점 설정
                        int x1 = 50, y1 = 50;
                        int x2 = 200, y2 = 200;
                        int[] n1pos  = latLngToPos(latitude, longitude, width, height);
                        int[] n2pos = latLngToPos(testLat, testLng, width, height);

                        // 선 색상 설정
                        Color lineColor = Colors.Red;
                        // 선 두께 설정
                        int thickness = 4;

                        // 선 그리기
                        //DrawThickLine(writeableBitmap, n1pos[0], n1pos[1], n2pos[0], n2pos[1], lineColor, thickness);
                        // 선 그리기
                        //DrawLine(writeableBitmap, x1, y1, x2, y2, lineColor);
                        //DrawLineLatLng(writeableBitmap, lat0, lng0, lat1, lng1, lineColor);
                        DrawRoadBitmap(writeableBitmap, n1pos[0], n1pos[1], n2pos[0], n2pos[1]);


                        //MapImage.Source = bitmap;
                        MapImage.Source = writeableBitmap;
                    }
                    double dlat = latMax - latitude;
                    double dlng = lngMax - longitude;
                    BoundLatLng.Text = $"({latMin}, {lngMin}), ({latMax}, {lngMax}) dlat:{dlat}, dlng:{dlng}, posx:{pos[0]}, posy={pos[1]}";
                    //BoundLatLng.Text = $"({boundBox[0]}, {boundBox[1]}), ({boundBox[2]}, {boundBox[3]}) dlat:{dlat}, dlng:{dlng}";

                }
                else
                {
                    MessageBox.Show($"Error: {response.StatusCode}");
                }
            }
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

        private void DrawRoadBitmap(WriteableBitmap bitmap, int x1, int y1, int x2, int y2)
        {
            BitmapImage bitmapImage = new BitmapImage(new Uri("..\\..\\pmsdata\\s000010000s.jpg", UriKind.RelativeOrAbsolute));

            int width = Math.Abs(x2 - x1);
            int height = Math.Abs(y2 - y1);

            CopyBitmapImageToWriteableBitmap(bitmapImage, bitmap, x1, y1, width, height);
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
            //int width = int( bitmapImage.PixelWidth / 10.);
            //int height = int( bitmapImage.PixelHeight / 10.);
            //int stride = width * ((bitmapImage.Format.BitsPerPixel + 7) / 8);
            //byte[] pixelData = new byte[height * stride];
            int width = (int)(bitmapImage.PixelWidth);
            int height = (int)(bitmapImage.PixelHeight);
            int stride = width * ((bitmapImage.Format.BitsPerPixel + 7) / 8);
            byte[] pixelData = new byte[height * stride];

            bitmapImage.CopyPixels(pixelData, stride, 0);
            writeableBitmap.WritePixels(new Int32Rect(x, y, width, height), pixelData, stride, 0); // 
        }

        //--------------------------------------------------------------------------------

        //public static WritableBitmap RotateBitmap(BitmapSource source, double angle)
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
        //    WritableBitmap writableBitmap = new WritableBitmap(renderBitmap);
        //    return writableBitmap;
        //}

        //public static void Main()
        //{
        //    // Open a file dialog to select an image file
        //    OpenFileDialog openFileDialog = new OpenFileDialog();
        //    openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";
        //    if (openFileDialog.ShowDialog() == true)
        //    {
        //        // Load the selected image file
        //        BitmapImage bitmapImage = new BitmapImage(new Uri(openFileDialog.FileName));

        //        // Rotate the image by 45 degrees
        //        WritableBitmap rotatedBitmap = RotateBitmap(bitmapImage, 45);

        //        // Save the rotated image to a file (optional)
        //        SaveRotatedImage(rotatedBitmap, "rotated_image.png");
        //    }
        //}

        //private static void SaveRotatedImage(WritableBitmap bitmap, string filePath)
        //{
        //    using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
        //    {
        //        PngBitmapEncoder encoder = new PngBitmapEncoder();
        //        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        //        encoder.Save(fileStream);
        //    }
        //}
    }
}