let adminHeatmapInstance = null;
let adminHeatLayer = null;

window.renderHeatmap = (containerId, dataPoints) => {
    const container = document.getElementById(containerId);
    if (!container) return;

    // Default center to Vinh Khanh Street
    let center = [10.7601, 106.7025];
    if (dataPoints && dataPoints.length > 0) {
        center = [dataPoints[0][0], dataPoints[0][1]];
    }

    // If map already exists, just update the heat layer data — no flicker!
    if (adminHeatmapInstance !== null) {
        if (adminHeatLayer) {
            adminHeatLayer.setLatLngs(dataPoints || []);
        }
        return;
    }

    // First-time initialization
    adminHeatmapInstance = L.map(containerId).setView(center, 16);

    L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap &copy; CARTO',
        subdomains: 'abcd',
        maxZoom: 20
    }).addTo(adminHeatmapInstance);

    // Create Heatmap Layer once
    if (dataPoints && dataPoints.length > 0) {
        adminHeatLayer = L.heatLayer(dataPoints, {
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
        }).addTo(adminHeatmapInstance);
    }
};
