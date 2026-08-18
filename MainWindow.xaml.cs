using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Windows.Devices.Geolocation;
using Windows.Media.Control;

namespace DesktopOrganizer
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private readonly PerformanceCounter cpuCounter =
            new("Processor", "% Processor Time", "_Total");

        private readonly DispatcherTimer cpuTimer = new()
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        private readonly DispatcherTimer weatherTimer = new()
        {
            Interval = TimeSpan.FromMinutes(10)
        };

        private readonly DispatcherTimer mediaTimer = new()
        {
            Interval = TimeSpan.FromSeconds(2)
        };

        private GlobalSystemMediaTransportControlsSessionManager?
            mediaManager;

        private bool isOverlayOpen;
        private DesktopOverlay? overlay;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;

            cpuTimer.Tick += CpuTimer_Tick;
            weatherTimer.Tick += WeatherTimer_Tick;
            mediaTimer.Tick += MediaTimer_Tick;
        }

        private async void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Left =
                (SystemParameters.PrimaryScreenWidth - Width) / 2;

            Top = -40;

            cpuCounter.NextValue();
            cpuTimer.Start();

            await InitializeLocationAndWeatherAsync();
            weatherTimer.Start();

            await InitializeMediaAsync();
            mediaTimer.Start();
        }

        // =====================================================
        // CPU
        // =====================================================

        private void CpuTimer_Tick(
            object? sender,
            EventArgs e)
        {
            int cpuUsage =
                (int)Math.Round(
                    cpuCounter.NextValue()
                );

            cpuUsage =
                Math.Clamp(
                    cpuUsage,
                    0,
                    100
                );

            CpuText.Text =
                $"{cpuUsage}%";
        }

        // =====================================================
        // LOCATION + WEATHER
        // =====================================================

        private async Task InitializeLocationAndWeatherAsync()
        {
            try
            {
                GeolocationAccessStatus accessStatus =
                    await Geolocator.RequestAccessAsync();

                if (accessStatus ==
                    GeolocationAccessStatus.Allowed)
                {
                    await UpdateWeatherFromWindowsLocationAsync();
                }
                else
                {
                    await UpdateWeatherFromIpAsync();
                }
            }
            catch
            {
                await UpdateWeatherFromIpAsync();
            }
        }

        private async void WeatherTimer_Tick(
            object? sender,
            EventArgs e)
        {
            await UpdateWeatherAsync();
        }

        private async Task UpdateWeatherAsync()
        {
            try
            {
                await UpdateWeatherFromWindowsLocationAsync();
            }
            catch
            {
                await UpdateWeatherFromIpAsync();
            }
        }

        private async Task UpdateWeatherFromWindowsLocationAsync()
        {
            Geolocator geolocator = new()
            {
                DesiredAccuracy =
                    PositionAccuracy.High
            };

            Geoposition position =
                await geolocator.GetGeopositionAsync(
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromSeconds(15)
                );

            double latitude =
                position
                    .Coordinate
                    .Point
                    .Position
                    .Latitude;

            double longitude =
                position
                    .Coordinate
                    .Point
                    .Position
                    .Longitude;

            WeatherData? weather =
                await GetCurrentWeatherAsync(
                    latitude,
                    longitude
                );

            if (weather == null)
            {
                SetWeatherFallback();
                return;
            }

            string city =
                await GetCityFromCoordinatesAsync(
                    latitude,
                    longitude
                );

            WeatherText.Text =
                $"{Math.Round(weather.Temperature)}°";

            LocationText.Text =
                FormatLocation(city);
        }

        private async Task UpdateWeatherFromIpAsync()
        {
            try
            {
                LocationData? location =
                    await GetIpLocationAsync();

                if (location == null)
                {
                    SetWeatherFallback();
                    return;
                }

                WeatherData? weather =
                    await GetCurrentWeatherAsync(
                        location.Latitude,
                        location.Longitude
                    );

                if (weather == null)
                {
                    SetWeatherFallback();
                    return;
                }

                WeatherText.Text =
                    $"{Math.Round(weather.Temperature)}°";

                LocationText.Text =
                    FormatLocation(location.City);
            }
            catch
            {
                SetWeatherFallback();
            }
        }

        private static async Task<LocationData?>
            GetIpLocationAsync()
        {
            string json =
                await httpClient.GetStringAsync(
                    "https://ipapi.co/json/"
                );

            using JsonDocument document =
                JsonDocument.Parse(json);

            JsonElement root =
                document.RootElement;

            if (!root.TryGetProperty(
                    "latitude",
                    out JsonElement latitudeElement) ||
                !root.TryGetProperty(
                    "longitude",
                    out JsonElement longitudeElement))
            {
                return null;
            }

            string city =
                root.TryGetProperty(
                    "city",
                    out JsonElement cityElement)
                ? cityElement.GetString() ?? "LOCATION"
                : "LOCATION";

            return new LocationData(
                city,
                latitudeElement.GetDouble(),
                longitudeElement.GetDouble()
            );
        }

        private static async Task<string>
            GetCityFromCoordinatesAsync(
                double latitude,
                double longitude)
        {
            try
            {
                string lat =
                    latitude.ToString(
                        CultureInfo.InvariantCulture
                    );

                string lon =
                    longitude.ToString(
                        CultureInfo.InvariantCulture
                    );

                string url =
                    "https://api.bigdatacloud.net/data/" +
                    "reverse-geocode-client" +
                    $"?latitude={lat}" +
                    $"&longitude={lon}" +
                    "&localityLanguage=de";

                string json =
                    await httpClient.GetStringAsync(url);

                using JsonDocument document =
                    JsonDocument.Parse(json);

                JsonElement root =
                    document.RootElement;

                string? city =
                    GetStringProperty(
                        root,
                        "city"
                    );

                if (!string.IsNullOrWhiteSpace(city))
                {
                    return city;
                }

                string? locality =
                    GetStringProperty(
                        root,
                        "locality"
                    );

                return
                    !string.IsNullOrWhiteSpace(locality)
                        ? locality
                        : "LOCATION";
            }
            catch
            {
                return "LOCATION";
            }
        }

        private static string? GetStringProperty(
            JsonElement root,
            string propertyName)
        {
            if (!root.TryGetProperty(
                    propertyName,
                    out JsonElement element))
            {
                return null;
            }

            return element.GetString();
        }

        private static async Task<WeatherData?>
            GetCurrentWeatherAsync(
                double latitude,
                double longitude)
        {
            string lat =
                latitude.ToString(
                    CultureInfo.InvariantCulture
                );

            string lon =
                longitude.ToString(
                    CultureInfo.InvariantCulture
                );

            string url =
                "https://api.open-meteo.com/v1/forecast" +
                $"?latitude={lat}" +
                $"&longitude={lon}" +
                "&current=temperature_2m";

            string json =
                await httpClient.GetStringAsync(url);

            using JsonDocument document =
                JsonDocument.Parse(json);

            JsonElement current =
                document
                    .RootElement
                    .GetProperty("current");

            double temperature =
                current
                    .GetProperty("temperature_2m")
                    .GetDouble();

            return new WeatherData(
                temperature
            );
        }

        private void SetWeatherFallback()
        {
            WeatherText.Text =
                "--°";

            LocationText.Text =
                "L O C A T I O N";
        }

        private static string FormatLocation(
            string city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return "L O C A T I O N";
            }

            string value =
                city
                    .Trim()
                    .ToUpperInvariant();

            return string.Join(
                " ",
                value.ToCharArray()
            );
        }

        // =====================================================
        // MEDIA / RAM
        // =====================================================

        private async Task InitializeMediaAsync()
        {
            try
            {
                mediaManager =
                    await GlobalSystemMediaTransportControlsSessionManager
                        .RequestAsync();

                await UpdateMediaOrRamAsync();
            }
            catch
            {
                ShowRamStatus();
            }
        }

        private async void MediaTimer_Tick(
            object? sender,
            EventArgs e)
        {
            await UpdateMediaOrRamAsync();
        }

        private async Task UpdateMediaOrRamAsync()
        {
            try
            {
                GlobalSystemMediaTransportControlsSession?
                    session =
                        mediaManager?.GetCurrentSession();

                if (session == null)
                {
                    ShowRamStatus();
                    return;
                }

                GlobalSystemMediaTransportControlsSessionPlaybackInfo
                    playbackInfo =
                        session.GetPlaybackInfo();

                if (playbackInfo.PlaybackStatus !=
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus
                        .Playing)
                {
                    ShowRamStatus();
                    return;
                }

                GlobalSystemMediaTransportControlsSessionMediaProperties
                    media =
                        await session
                            .TryGetMediaPropertiesAsync();

                string title =
                    media.Title?.Trim()
                    ?? string.Empty;

                string artist =
                    media.Artist?.Trim()
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(title))
                {
                    ShowRamStatus();
                    return;
                }

                ShowMediaStatus(
                    artist,
                    title
                );
            }
            catch
            {
                ShowRamStatus();
            }
        }

        private void ShowMediaStatus(
            string artist,
            string title)
        {
            MediaStatus.Visibility =
                Visibility.Visible;

            SetMediaIconVisibility(
                Visibility.Visible
            );

            MediaText.Margin =
                new Thickness(
                    8,
                    0,
                    0,
                    0
                );

            MediaText.Text =
                BuildMediaText(
                    artist,
                    title
                );
        }

        private void ShowRamStatus()
        {
            MediaStatus.Visibility =
                Visibility.Visible;

            SetMediaIconVisibility(
                Visibility.Collapsed
            );

            MediaText.Margin =
                new Thickness(
                    0
                );

            int ramUsage =
                GetRamUsagePercent();

            MediaText.Text =
                $"R A M   {ramUsage}%";
        }

        private void SetMediaIconVisibility(
            Visibility visibility)
        {
            if (MediaStatus.Children.Count == 0)
            {
                return;
            }

            if (MediaStatus.Children[0] is Viewbox mediaIcon)
            {
                mediaIcon.Visibility =
                    visibility;
            }
        }

        private static string BuildMediaText(
            string artist,
            string title)
        {
            string value =
                string.IsNullOrWhiteSpace(artist)
                    ? title
                    : $"{artist} · {title}";

            const int maxLength = 24;

            if (value.Length <= maxLength)
            {
                return value;
            }

            return value[..(maxLength - 1)] + "…";
        }

        // =====================================================
        // RAM
        // =====================================================

        private static int GetRamUsagePercent()
        {
            MemoryStatus memoryStatus =
                new();

            memoryStatus.Length =
                (uint)Marshal.SizeOf<MemoryStatus>();

            bool success =
                GlobalMemoryStatusEx(
                    ref memoryStatus
                );

            if (!success)
            {
                return 0;
            }

            return (int)memoryStatus.MemoryLoad;
        }

        // =====================================================
        // OVERLAY
        // =====================================================

        private void Island_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            ToggleOverlay();
        }

        private void ToggleOverlay()
        {
            if (isOverlayOpen)
            {
                overlay?.Close();
                return;
            }

            isOverlayOpen = true;

            overlay =
                new DesktopOverlay();

            overlay.Closed +=
                Overlay_Closed;

            overlay.Show();
            overlay.Activate();
        }

        private void Overlay_Closed(
            object? sender,
            EventArgs e)
        {
            isOverlayOpen = false;
            overlay = null;
        }

        // =====================================================
        // CLEANUP
        // =====================================================

        private void MainWindow_Closed(
            object? sender,
            EventArgs e)
        {
            cpuTimer.Stop();
            weatherTimer.Stop();
            mediaTimer.Stop();

            cpuCounter.Dispose();

            if (overlay != null)
            {
                overlay.Closed -=
                    Overlay_Closed;

                overlay.Close();
                overlay = null;
            }
        }

        // =====================================================
        // WINDOWS MEMORY API
        // =====================================================

        [DllImport(
            "kernel32.dll",
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(
            ref MemoryStatus buffer
        );

        [StructLayout(
            LayoutKind.Sequential,
            CharSet = CharSet.Auto)]
        private struct MemoryStatus
        {
            public uint Length;

            public uint MemoryLoad;

            public ulong TotalPhysical;

            public ulong AvailablePhysical;

            public ulong TotalPageFile;

            public ulong AvailablePageFile;

            public ulong TotalVirtual;

            public ulong AvailableVirtual;

            public ulong AvailableExtendedVirtual;
        }

        // =====================================================
        // DATA
        // =====================================================

        private sealed record LocationData(
            string City,
            double Latitude,
            double Longitude
        );

        private sealed record WeatherData(
            double Temperature
        );
    }
}