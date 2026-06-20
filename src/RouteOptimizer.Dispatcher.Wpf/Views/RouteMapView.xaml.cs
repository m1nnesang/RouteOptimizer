using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services;

namespace RouteOptimizer.Dispatcher.Wpf.Views;

public partial class RouteMapView : UserControl
{
    private bool _isReady;

    public RouteMapView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public static readonly DependencyProperty StopsProperty = DependencyProperty.Register(
        nameof(Stops),
        typeof(IEnumerable),
        typeof(RouteMapView),
        new PropertyMetadata(null, OnStopsChanged));

    public IEnumerable? Stops
    {
        get => (IEnumerable?)GetValue(StopsProperty);
        set => SetValue(StopsProperty, value);
    }

    private static void OnStopsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((RouteMapView)d).RenderStops();

    public static readonly DependencyProperty DepotLatProperty = DependencyProperty.Register(
        nameof(DepotLat), typeof(double), typeof(RouteMapView),
        new PropertyMetadata(double.NaN, OnStopsChanged));

    public static readonly DependencyProperty DepotLngProperty = DependencyProperty.Register(
        nameof(DepotLng), typeof(double), typeof(RouteMapView),
        new PropertyMetadata(double.NaN, OnStopsChanged));

    public double DepotLat
    {
        get => (double)GetValue(DepotLatProperty);
        set => SetValue(DepotLatProperty, value);
    }

    public double DepotLng
    {
        get => (double)GetValue(DepotLngProperty);
        set => SetValue(DepotLngProperty, value);
    }

    public static readonly DependencyProperty DriverLatProperty = DependencyProperty.Register(
        nameof(DriverLat), typeof(double), typeof(RouteMapView),
        new PropertyMetadata(double.NaN, OnDriverChanged));

    public static readonly DependencyProperty DriverLngProperty = DependencyProperty.Register(
        nameof(DriverLng), typeof(double), typeof(RouteMapView),
        new PropertyMetadata(double.NaN, OnDriverChanged));

    public double DriverLat
    {
        get => (double)GetValue(DriverLatProperty);
        set => SetValue(DriverLatProperty, value);
    }

    public double DriverLng
    {
        get => (double)GetValue(DriverLngProperty);
        set => SetValue(DriverLngProperty, value);
    }

    private static void OnDriverChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((RouteMapView)d).RenderDriver();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isReady)
            return;

        try
        {
            await MapWebView.EnsureCoreWebView2Async();
            MapWebView.NavigationCompleted += (_, _) =>
            {
                _isReady = true;
                RenderStops();
                RenderDriver();
            };
            MapWebView.NavigateToString(MapHtml);
        }
        catch (Exception)
        {
        }
    }

    private async void RenderStops()
    {
        if (!_isReady || MapWebView.CoreWebView2 is null)
            return;

        try
        {
            var stops = (Stops ?? Array.Empty<RouteStop>()).OfType<RouteStop>();
            var json = RouteMapSerializer.Serialize(stops);
            var depotJson = !double.IsNaN(DepotLat) && !double.IsNaN(DepotLng)
                ? string.Format(CultureInfo.InvariantCulture, "{{\"lat\":{0},\"lng\":{1}}}", DepotLat, DepotLng)
                : "null";
            await MapWebView.CoreWebView2.ExecuteScriptAsync($"renderRoute({json}, {depotJson});");
        }
        catch (Exception)
        {
        }
    }

    private async void RenderDriver()
    {
        if (!_isReady || MapWebView.CoreWebView2 is null)
            return;

        try
        {
            var driverJson = !double.IsNaN(DriverLat) && !double.IsNaN(DriverLng)
                ? string.Format(CultureInfo.InvariantCulture, "{{\"lat\":{0},\"lng\":{1}}}", DriverLat, DriverLng)
                : "null";
            await MapWebView.CoreWebView2.ExecuteScriptAsync($"updateDriver({driverJson});");
        }
        catch (Exception)
        {
        }
    }

    private const string MapHtml = """
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
    <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
    <style>
        html, body, #map { height: 100%; margin: 0; padding: 0; }
        .stop-marker {
            background: #2980b9; color: #fff; border-radius: 50%;
            width: 26px; height: 26px; line-height: 26px; text-align: center;
            font: bold 13px sans-serif; box-shadow: 0 0 3px rgba(0,0,0,0.5);
        }
        .stop-marker.done { background: #27ae60; }
        .stop-marker.failed { background: #c0392b; }
        .stop-marker.skipped { background: #7f8c8d; }
        .depot-marker {
            background: #34495e; color: #fff; border-radius: 4px;
            width: 30px; height: 30px; line-height: 30px; text-align: center;
            font-size: 18px; box-shadow: 0 0 3px rgba(0,0,0,0.5);
        }
        .driver-marker {
            background: #8e44ad; color: #fff; border-radius: 50%;
            width: 30px; height: 30px; line-height: 30px; text-align: center;
            font-size: 17px; box-shadow: 0 0 6px rgba(142,68,173,0.9);
            border: 2px solid #fff;
        }
    </style>
</head>
<body>
    <div id="map"></div>
    <script>
        const map = L.map('map').setView([52.1, 19.0], 7);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap'
        }).addTo(map);

        let layer = L.layerGroup().addTo(map);
        let driverMarker = null;
        const OSRM_URL = 'http://localhost:5000';
        let renderToken = 0;

        function updateDriver(loc) {
            if (!loc) {
                if (driverMarker) { map.removeLayer(driverMarker); driverMarker = null; }
                return;
            }
            const latlng = [loc.lat, loc.lng];
            if (driverMarker) {
                driverMarker.setLatLng(latlng);
            } else {
                const icon = L.divIcon({
                    className: '',
                    html: '<div class="driver-marker">🚚</div>',
                    iconSize: [30, 30],
                    iconAnchor: [15, 15]
                });
                driverMarker = L.marker(latlng, { icon, zIndexOffset: 1000 })
                    .bindPopup('<b>Driver</b>')
                    .addTo(map);
            }
        }

        function statusClass(status) {
            switch ((status || '').toLowerCase()) {
                case 'completed': return 'done';
                case 'failed': return 'failed';
                case 'skipped': return 'skipped';
                default: return '';
            }
        }

        async function fetchRoadGeometry(points) {
            const coords = points.map(p => p[1] + ',' + p[0]).join(';');
            const url = OSRM_URL + '/route/v1/driving/' + coords + '?overview=full&geometries=geojson';
            const res = await fetch(url);
            if (!res.ok) throw new Error('OSRM HTTP ' + res.status);
            const data = await res.json();
            if (data.code !== 'Ok' || !data.routes || data.routes.length === 0)
                throw new Error('OSRM code ' + data.code);
            return data.routes[0].geometry.coordinates.map(c => [c[1], c[0]]);
        }

        async function renderRoute(stops, depot) {
            const token = ++renderToken;
            layer.clearLayers();
            if (!stops || stops.length === 0) {
                map.setView([52.1, 19.0], 7);
                return;
            }

            const points = [];

            if (depot) {
                const depotLatLng = [depot.lat, depot.lng];
                points.push(depotLatLng);
                const depotIcon = L.divIcon({
                    className: '',
                    html: '<div class="depot-marker">🏠</div>',
                    iconSize: [30, 30],
                    iconAnchor: [15, 15]
                });
                L.marker(depotLatLng, { icon: depotIcon }).bindPopup('<b>Warehouse</b>').addTo(layer);
            }

            stops.forEach(s => {
                const latlng = [s.lat, s.lng];
                points.push(latlng);
                const icon = L.divIcon({
                    className: '',
                    html: '<div class="stop-marker ' + statusClass(s.status) + '">' + s.seq + '</div>',
                    iconSize: [26, 26],
                    iconAnchor: [13, 13]
                });
                L.marker(latlng, { icon }).bindPopup('<b>#' + s.seq + '</b><br>' + s.label).addTo(layer);
            });

            if (points.length > 1) {
                let line = points;
                let osrmFailed = false;
                try {
                    line = await fetchRoadGeometry(points);
                } catch (e) {
                    console.warn('Falling back to straight line:', e);
                    osrmFailed = true;
                }
                if (token !== renderToken) return;
                const lineOpts = osrmFailed
                    ? { color: '#e67e22', weight: 2, opacity: 0.75, dashArray: '10 7' }
                    : { color: '#2980b9', weight: 4, opacity: 0.8 };
                L.polyline(line, lineOpts).addTo(layer);
                if (osrmFailed) {
                    L.popup({ closeButton: false, autoClose: false, closeOnClick: false })
                        .setLatLng(line[Math.floor(line.length / 2)])
                        .setContent('<span style="color:#e67e22;font-size:11px">⚠ Routing unavailable — straight line</span>')
                        .openOn(map);
                }
            }

            map.fitBounds(L.latLngBounds(points).pad(0.2));
        }
    </script>
</body>
</html>
""";
}
