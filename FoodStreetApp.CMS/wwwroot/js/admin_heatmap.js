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

window.renderHeatmap = (containerId, dataPoints) => {
    const container = document.getElementById(containerId);
    if (!container) return;

    // Default center to Vinh Khanh Street
    let center = [10.7601, 106.7025];
    if (dataPoints && dataPoints.length > 0) {
        center = [dataPoints[0][0], dataPoints[0][1]];
    }

    // Detect stale instance (Blazor reconnect replaced DOM)
    if (adminHeatmapInstance !== null) {
        try {
            const oldContainer = adminHeatmapInstance.getContainer();
            if (!oldContainer || oldContainer !== container || !document.body.contains(oldContainer)) {
                adminHeatmapInstance.remove();
                adminHeatmapInstance = null;
                adminHeatLayer = null;
            }
        } catch (e) {
            adminHeatmapInstance = null;
            adminHeatLayer = null;
        }
    }

    // Map already exists → update heat layer data
    if (adminHeatmapInstance !== null) {
        if (dataPoints && dataPoints.length > 0) {
            if (adminHeatLayer) {
                // Update existing layer + force visual redraw
                adminHeatLayer.setLatLngs(dataPoints);
                adminHeatLayer.redraw();
            } else {
                // Layer didn't exist yet (first render had no data) → create it now
                adminHeatLayer = L.heatLayer(dataPoints, HEAT_OPTIONS).addTo(adminHeatmapInstance);
            }
        } else {
            // No data for this date → remove heat layer
            if (adminHeatLayer) {
                adminHeatmapInstance.removeLayer(adminHeatLayer);
                adminHeatLayer = null;
            }
        }
        adminHeatmapInstance.invalidateSize();
        return;
    }

    // First-time initialization
    adminHeatmapInstance = L.map(containerId).setView(center, 16);

    L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap &copy; CARTO',
        subdomains: 'abcd',
        maxZoom: 20
    }).addTo(adminHeatmapInstance);

    if (dataPoints && dataPoints.length > 0) {
        adminHeatLayer = L.heatLayer(dataPoints, HEAT_OPTIONS).addTo(adminHeatmapInstance);
    }

    setTimeout(() => {
        if (adminHeatmapInstance) adminHeatmapInstance.invalidateSize();
    }, 200);
};
