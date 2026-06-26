window.driverMap = (function () {
    let map = null;
    let stopLayer = null;
    let routeLine = null;
    let routeLineUpcoming = null;
    let driverMarker = null;
    let driverHalo = null;

    function nearestIndex(path, target) {
        let best = 0;
        let bestD = Infinity;
        for (let i = 0; i < path.length; i++) {
            const dLat = path[i][0] - target[0];
            const dLng = path[i][1] - target[1];
            const d = dLat * dLat + dLng * dLng;
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    function statusColor(status, isCurrent) {
        if (isCurrent) return { fill: "#4c84ff", text: "#fff" };
        switch (status) {
            case "Completed": return { fill: "#25b366", text: "#fff" };
            case "PartiallyCompleted": return { fill: "#d99a2b", text: "#1a1206" };
            case "Failed": return { fill: "#f0564b", text: "#fff" };
            case "Skipped": return { fill: "#d99a2b", text: "#1a1206" };
            default: return { fill: "#1e2740", text: "#8893ad" };
        }
    }

    function warehouseIcon() {
        const html =
            '<div style="width:30px;height:30px;border-radius:8px;background:#1a2238;' +
            'color:#9db2ff;display:flex;align-items:center;justify-content:center;' +
            'border:2px solid #5b8cff;box-shadow:0 1px 4px rgba(0,0,0,.4);">' +
            '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" ' +
            'stroke-width="2" stroke-linecap="round" stroke-linejoin="round">' +
            '<path d="M3 21V9l9-6 9 6v12"/><path d="M9 21v-6h6v6"/></svg></div>';
        return L.divIcon({
            html: html,
            className: "",
            iconSize: [30, 30],
            iconAnchor: [15, 15]
        });
    }

    function stopIcon(label, color, isCurrent) {
        const size = isCurrent ? 28 : 22;
        const html =
            '<div style="width:' + size + 'px;height:' + size + 'px;border-radius:50%;background:' + color.fill +
            ';color:' + color.text + ';display:flex;align-items:center;justify-content:center;font-weight:600;' +
            'border:2px solid #fff;box-shadow:0 1px 4px rgba(0,0,0,.3);font-size:' + (isCurrent ? 12 : 11) + 'px;">' +
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

            var isLocal = location.hostname === "localhost" || location.hostname === "127.0.0.1";
            var tileUrl = isLocal
                ? "http://localhost:8081/light_all/{z}/{x}/{y}{r}.png"
                : "https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png";

            L.tileLayer(tileUrl, {
                maxZoom: 19,
                subdomains: "abcd"
            }).addTo(map);

            stopLayer = L.layerGroup().addTo(map);

            setTimeout(function () { if (map) map.invalidateSize(); }, 100);
        },

        render: function (stops, currentStopId, routeGeometry, warehouse) {
            if (!map) return;

            stopLayer.clearLayers();

            if (routeLine) {
                map.removeLayer(routeLine);
                routeLine = null;
            }
            if (routeLineUpcoming) {
                map.removeLayer(routeLineUpcoming);
                routeLineUpcoming = null;
            }

            if (warehouse) {
                L.marker([warehouse.latitude, warehouse.longitude], {
                    icon: warehouseIcon(),
                    zIndexOffset: -100
                }).bindTooltip("Warehouse").addTo(stopLayer);
            }

            if (!stops || stops.length === 0) return;

            const ordered = stops.slice().sort(function (a, b) { return a.sequence - b.sequence; });
            const latlngs = [];
            let currentIndex = -1;

            ordered.forEach(function (s, i) {
                const isCurrent = s.id === currentStopId;
                if (isCurrent) currentIndex = i;
                const color = statusColor(s.status, isCurrent);
                const marker = L.marker([s.latitude, s.longitude], {
                    icon: stopIcon(s.sequence + 1, color, isCurrent)
                });
                marker.addTo(stopLayer);
                latlngs.push([s.latitude, s.longitude]);
            });

            const fallback = warehouse
                ? [[warehouse.latitude, warehouse.longitude]].concat(latlngs)
                : latlngs;

            const path = (routeGeometry && routeGeometry.length > 1)
                ? routeGeometry.map(function (p) { return [p.latitude, p.longitude]; })
                : fallback;

            if (path.length > 1) {
                const split = currentIndex >= 0
                    ? nearestIndex(path, latlngs[currentIndex])
                    : path.length - 1;
                const traveled = path.slice(0, split + 1);
                const upcoming = path.slice(split);

                if (traveled.length > 1) {
                    routeLine = L.polyline(traveled, { color: "#5b8cff", weight: 6, opacity: 1, lineCap: "round", lineJoin: "round" }).addTo(map);
                }
                if (upcoming.length > 1) {
                    routeLineUpcoming = L.polyline(upcoming, { color: "#5b8cff", weight: 6, opacity: 1, lineCap: "round", lineJoin: "round" }).addTo(map);
                }
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
                driverHalo = L.circleMarker([lat, lng], {
                    radius: 16, stroke: false, fillColor: "#4c84ff", fillOpacity: 0.22
                }).addTo(map);
                driverMarker = L.circleMarker([lat, lng], {
                    radius: 7, color: "#0f1626", weight: 2.5, fillColor: "#4c84ff", fillOpacity: 1
                }).addTo(map);
            } else {
                driverMarker.setLatLng([lat, lng]);
                if (driverHalo) driverHalo.setLatLng([lat, lng]);
            }
        },

        dispose: function () {
            if (map) {
                map.remove();
                map = null;
            }
            stopLayer = null;
            routeLine = null;
            routeLineUpcoming = null;
            driverMarker = null;
            driverHalo = null;
        }
    };
})();
