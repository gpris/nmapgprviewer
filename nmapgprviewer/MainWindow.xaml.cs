using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace nmapgprviewer
{
    public partial class MainWindow : Window
    {
        private static readonly string clientId = "sm8oqxebg0"; // 네이버 클라이언트 ID
        private static readonly string clientSecret = "IP2QQ91DvwVN1VjSq282eYzIh5LBNI7Ps1Yua65D"; // 네이버 클라이언트 시크릿

        public MainWindow()
        {
            InitializeComponent();
            LoadMapAsync();
        }

        private async Task LoadMapAsync()
        {
            double latitude = 37.5665; // 예시 위도
            double longitude = 126.9780; // 예시 경도

            // 마커 파라미터 추가
            string markers = $"type:t|size:mid|pos:{longitude} {latitude}";

            string url = $"https://naveropenapi.apigw.ntruss.com/map-static/v2/raster?center={longitude},{latitude}&level=16&w=1024&h=1024&markers={markers}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-NCP-APIGW-API-KEY-ID", clientId);
                client.DefaultRequestHeaders.Add("X-NCP-APIGW-API-KEY", clientSecret);

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
                }
                else
                {
                    MessageBox.Show($"Error: {response.StatusCode}");
                }
            }
        }
    }
}