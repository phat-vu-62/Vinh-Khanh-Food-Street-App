let adminHeatmapInstance = null;

window.renderHeatmap = (containerId, dataPoints) => {
    // dataPoints format: [ [lat, lng, intensity], [lat, lng, intensity] ]
    const container = document.getElementById(containerId);
    if (!container) return;

    // Destroy existing instance if any
    if (adminHeatmapInstance !== null) {
        adminHeatmapInstance.remove();
    }

    // Default center to Vinh Khanh Street approx if no data, else center to the first point
    let center = [10.7601, 106.7025];
    if (dataPoints && dataPoints.length > 0) {
        // Average center or just use the first one
        center = [dataPoints[0][0], dataPoints[0][1]];
    }

    // Initialize Map
    adminHeatmapInstance = L.map(containerId).setView(center, 16);

    // Add Tile Layer (CartoDB Positron for clean look)
    L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap &copy; CARTO',
        subdomains: 'abcd',
        maxZoom: 20
    }).addTo(adminHeatmapInstance);

    // Add Heatmap Layer
    if (dataPoints && dataPoints.length > 0) {
        const heatArea = L.heatLayer(dataPoints, {
            radius: 25,
            blur: 15,
            maxZoom: 17,
            max: 1.0, // relative max intensity
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
