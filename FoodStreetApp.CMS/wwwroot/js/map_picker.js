let map;
let marker;

window.initMapPicker = (dotNetHelper, lat, lng) => {
    // Default to District 4, Vinh Khanh if no coordinates
    const startLat = lat || 10.762622;
    const startLng = lng || 106.660172;

    if (map) {
        map.remove();
    }

    map = L.map('map-picker-container').setView([startLat, startLng], 17);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap'
    }).addTo(map);

    marker = L.marker([startLat, startLng], { draggable: true }).addTo(map);

    // Initial report
    dotNetHelper.invokeMethodAsync('UpdateCoordinates', startLat, startLng);

    // Handle map click
    map.on('click', function(e) {
        const { lat, lng } = e.latlng;
        marker.setLatLng([lat, lng]);
        dotNetHelper.invokeMethodAsync('UpdateCoordinates', lat, lng);
    });

    // Handle marker drag
    marker.on('dragend', function(e) {
        const { lat, lng } = marker.getLatLng();
        dotNetHelper.invokeMethodAsync('UpdateCoordinates', lat, lng);
    });
};

window.setMapLocation = (lat, lng) => {
    if (map && marker) {
        const newPos = [lat, lng];
        map.setView(newPos, 17);
        marker.setLatLng(newPos);
    }
};
