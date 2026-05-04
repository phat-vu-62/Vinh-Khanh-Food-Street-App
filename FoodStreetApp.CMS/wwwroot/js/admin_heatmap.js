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

// Google Maps style red pin SVG
const RED_PIN_SVG = `<svg xmlns="http://www.w3.org/2000/svg" width="28" height="40" viewBox="0 0 28 40">
  <path d="M14 0C6.27 0 0 6.27 0 14c0 10.5 14 26 14 26s14-15.5 14-26C28 6.27 21.73 0 14 0z" fill="#EA4335"/>
  <circle cx="14" cy="14" r="6" fill="#B31412"/>
  <circle cx="14" cy="14" r="4" fill="white"/>
</svg>`;

const redPinIcon = L.divIcon({
    className: 'heatmap-red-pin',
    html: RED_PIN_SVG,
    iconSize: [28, 40],
    iconAnchor: [14, 40],
    popupAnchor: [0, -36]
});

window.renderHeatmap = (containerId, dataPoints, poiLabels) => {
    // Always destroy old map completely
    if (adminHeatmapInstance !== null) {
        try { adminHeatmapInstance.remove(); } catch (e) { }
        adminHeatmapInstance = null;
        adminHeatLayer = null;
    }

    const container = document.getElementById(containerId);
    if (!container) return;

    container.innerHTML = '';

    // Default center to Vinh Khanh Street
    let center = [10.7601, 106.7025];
    if (poiLabels && poiLabels.length > 0) {
        center = [poiLabels[0].lat, poiLabels[0].lng];
    } else if (dataPoints && dataPoints.length > 0) {
        center = [dataPoints[0][0], dataPoints[0][1]];
    }

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

    // Add red pin markers for ALL POIs
    if (poiLabels && poiLabels.length > 0) {
        poiLabels.forEach(poi => {
            const countText = poi.count > 0
                ? `<div style="margin-top:8px;padding:6px 10px;background:#f0fdf4;border-radius:8px;font-size:0.8rem;"><b style="color:#059669;">${poi.count}</b> lượt tương tác</div>`
                : `<div style="margin-top:8px;padding:6px 10px;background:#fef2f2;border-radius:8px;font-size:0.8rem;color:#dc2626;">Chưa có tương tác</div>`;

            L.marker([poi.lat, poi.lng], { icon: redPinIcon })
                .addTo(adminHeatmapInstance)
                .bindPopup(`<div style="font-family:Inter,sans-serif;min-width:140px;"><b style="font-size:0.95rem;">${poi.name}</b>${countText}</div>`)
                .bindTooltip(poi.name, {
                    permanent: true,
                    direction: 'top',
                    offset: [0, -42],
                    className: 'heatmap-poi-tooltip'
                });
        });
    }

    // Force size recalculation after Blazor render completes
    setTimeout(() => {
        if (adminHeatmapInstance) adminHeatmapInstance.invalidateSize();
    }, 300);
};
