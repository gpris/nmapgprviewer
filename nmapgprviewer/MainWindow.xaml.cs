using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
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

            int zoom = 16;
            int width = 1024;
            int height = 768;

            // 마커 파라미터 추가
            string markers = $"type:t|size:mid|pos:{longitude} {latitude}";

            string url = $"https://naveropenapi.apigw.ntruss.com/map-static/v2/raster?center={longitude},{latitude}&level={zoom}&w={width}&h={height}&markers={markers}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-NCP-APIGW-API-KEY-ID", clientId);
                client.DefaultRequestHeaders.Add("X-NCP-APIGW-API-KEY", clientSecret);
                double[] boundBox = latLngToBounds(latitude, longitude, zoom, width, height);
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
                        MapImage.Source = bitmap;
                    }
                    double dlat = latMax - latitude;
                    double dlng = lngMax - longitude;
                    BoundLatLng.Text = $"({latMin}, {lngMin}), ({latMax}, {lngMax}) dlat:{dlat}, dlng:{dlng}";
                    //BoundLatLng.Text = $"({boundBox[0]}, {boundBox[1]}), ({boundBox[2]}, {boundBox[3]}) dlat:{dlat}, dlng:{dlng}";

                }
                else
                {
                    MessageBox.Show($"Error: {response.StatusCode}");
                }
            }
        }
    }
}