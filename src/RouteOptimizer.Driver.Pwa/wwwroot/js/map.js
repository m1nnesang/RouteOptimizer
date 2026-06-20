window.driverMap = (function () {
    let map = null;
    let stopLayer = null;
    let routeLine = null;
    let driverMarker = null;

    function statusColor(status, isCurrent) {
        if (isCurrent) return "#ffb300";
        switch (status) {
            case "Completed": return "#15803d";
            case "PartiallyCompleted": return "#b45309";
            case "Failed": return "#b91c1c";
            case "Skipped": return "#6b7280";
            default: return "#1d4ed8";
        }
    }

    function stopIcon(label, color, isCurrent) {
        const size = isCurrent ? 38 : 30;
        const html =
            '<div style="width:' + size + 'px;height:' + size + 'px;border-radius:50%;background:' + color +
            ';color:#fff;display:flex;align-items:center;justify-content:center;font-weight:800;' +
            'border:2px solid #fff;box-shadow:0 1px 4px rgba(0,0,0,.5);font-size:' + (isCurrent ? 15 : 13) + 'px;">' +
            label + '</div>';
        return L.divIcon({
            html: html,
            className: "",
            iconSize: [size, size],
            iconAnchor: [size / 2, size / 2]
        });
    }

    return {
        init: function (elementId) {
            if (map) {
                map.remove();
                map = null;
            }

            map = L.map(elementId, { zoomControl: false, attributionControl: false })
                .setView([52.2297, 21.0122], 12);

            L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
                maxZoom: 19
            }).addTo(map);

            stopLayer = L.layerGroup().addTo(map);

            setTimeout(function () { if (map) map.invalidateSize(); }, 100);
        },

        render: function (stops, currentStopId) {
            if (!map) return;

            stopLayer.clearLayers();

            if (routeLine) {
                map.removeLayer(routeLine);
                routeLine = null;
            }

            if (!stops || stops.length === 0) return;

            const ordered = stops.slice().sort(function (a, b) { return a.sequence - b.sequence; });
            const latlngs = [];

            ordered.forEach(function (s) {
                const isCurrent = s.id === currentStopId;
                const color = statusColor(s.status, isCurrent);
                const marker = L.marker([s.latitude, s.longitude], {
                    icon: stopIcon(s.sequence + 1, color, isCurrent)
                });
                marker.addTo(stopLayer);
                latlngs.push([s.latitude, s.longitude]);
            });

            if (latlngs.length > 1) {
                routeLine = L.polyline(latlngs, { color: "#a9bce0", weight: 3, opacity: 0.6, dashArray: "6 8" }).addTo(map);
            }
        },

        focus: function (lat, lng) {
            if (!map) return;
            map.setView([lat, lng], 15, { animate: true });
        },

        fitAll: function (stops) {
            if (!map || !stops || stops.length === 0) return;
            const latlngs = stops.map(function (s) { return [s.latitude, s.longitude]; });
            map.fitBounds(latlngs, { padding: [60, 60] });
        },

        setDriver: function (lat, lng) {
            if (!map) return;
            if (!driverMarker) {
                driverMarker = L.circleMarker([lat, lng], {
                    radius: 8, color: "#fff", weight: 2, fillColor: "#2563eb", fillOpacity: 1
                }).addTo(map);
            } else {
                driverMarker.setLatLng([lat, lng]);
            }
        },

        dispose: function () {
            if (map) {
                map.remove();
                map = null;
            }
            stopLayer = null;
            routeLine = null;
            driverMarker = null;
        }
    };
})();
