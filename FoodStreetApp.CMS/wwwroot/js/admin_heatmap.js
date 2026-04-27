let adminHeatmapInstance = null;
let adminHeatLayer = null;

const HEAT_OPTIONS = {
    radius: 25,
    blur: 15,
    maxZoom: 17,
    max: 1.0,
    gradient: {
        0.2: 'blue',
        0.4: 'cyan',
        0.6: 'lime',
        0.8: 'yellow',
        1.0: 'red'
    }
};

window.renderHeatmap = (containerId, dataPoints, poiLabels) => {
    // Always destroy old map completely — simplest way to avoid stale state
    if (adminHeatmapInstance !== null) {
        try { adminHeatmapInstance.remove(); } catch (e) { }
        adminHeatmapInstance = null;
        adminHeatLayer = null;
    }

    const container = document.getElementById(containerId);
    if (!container) return;

    // Clear any leftover Leaflet artifacts in the container
    container.innerHTML = '';

    // Default center to Vinh Khanh Street
    let center = [10.7601, 106.7025];
    if (dataPoints && dataPoints.length > 0) {
        center = [dataPoints[0][0], dataPoints[0][1]];
    }

    // Create fresh map
    adminHeatmapInstance = L.map(containerId).setView(center, 16);

    L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap &copy; CARTO',
        subdomains: 'abcd',
        maxZoom: 20
    }).addTo(adminHeatmapInstance);

    // Add heat layer if data exists
    if (dataPoints && dataPoints.length > 0) {
        adminHeatLayer = L.heatLayer(dataPoints, HEAT_OPTIONS).addTo(adminHeatmapInstance);
    }

    // Add POI name labels as markers
    if (poiLabels && poiLabels.length > 0) {
        poiLabels.forEach(poi => {
            L.marker([poi.lat, poi.lng], {
                icon: L.divIcon({
                    className: 'heatmap-poi-label',
                    html: `<span>${poi.name}</span>`,
                    iconSize: [0, 0],
                    iconAnchor: [0, -8]
                })
            }).addTo(adminHeatmapInstance)
              .bindPopup(`<b>${poi.name}</b><br/>Lượt tương tác: ${poi.count}`);
        });
    }

    // Force size recalculation after Blazor render completes
    setTimeout(() => {
        if (adminHeatmapInstance) adminHeatmapInstance.invalidateSize();
    }, 300);
};
