// Chart.js JS Interop Helper for Blazor
// Manages Chart instances to avoid memory leaks

const chartInstances = new Map();

function destroyIfExists(canvasId) {
    if (chartInstances.has(canvasId)) {
        chartInstances.get(canvasId).destroy();
        chartInstances.delete(canvasId);
    }
}

const COLORS = {
    primary: '#4f46e5',
    primaryLight: 'rgba(79, 70, 229, 0.1)',
    success: '#10b981',
    successLight: 'rgba(16, 185, 129, 0.1)',
    warning: '#f59e0b',
    warningLight: 'rgba(245, 158, 11, 0.1)',
    danger: '#ef4444',
    info: '#06b6d4',
    gray: '#94a3b8',
    palette: ['#4f46e5', '#10b981', '#f59e0b', '#ef4444', '#06b6d4', '#8b5cf6', '#ec4899', '#14b8a6']
};

const defaultFont = { family: "'Inter', sans-serif", size: 11 };

window.chartHelper = {

    renderLineChart(canvasId, labels, datasets) {
        destroyIfExists(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        const chartDatasets = datasets.map((ds, i) => ({
            label: ds.label,
            data: ds.data,
            borderColor: ds.color || COLORS.palette[i % COLORS.palette.length],
            backgroundColor: (ds.color || COLORS.palette[i % COLORS.palette.length]) + '18',
            borderWidth: 2.5,
            fill: true,
            tension: 0.4,
            pointRadius: 3,
            pointHoverRadius: 6,
            pointBackgroundColor: '#fff',
            pointBorderWidth: 2,
            pointBorderColor: ds.color || COLORS.palette[i % COLORS.palette.length]
        }));

        const chart = new Chart(ctx, {
            type: 'line',
            data: { labels, datasets: chartDatasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { intersect: false, mode: 'index' },
                plugins: {
                    legend: { display: datasets.length > 1, labels: { font: defaultFont, usePointStyle: true, padding: 16 } },
                    tooltip: { backgroundColor: '#1e293b', titleFont: defaultFont, bodyFont: defaultFont, padding: 12, cornerRadius: 8,
                        callbacks: { label: ctx => `${ctx.dataset.label}: ${ctx.parsed.y.toLocaleString('vi-VN')}` }
                    }
                },
                scales: {
                    x: { grid: { display: false }, ticks: { font: defaultFont, color: COLORS.gray } },
                    y: { beginAtZero: true, grid: { color: '#f1f5f9' }, ticks: { font: defaultFont, color: COLORS.gray } }
                }
            }
        });
        chartInstances.set(canvasId, chart);
    },

    renderBarChart(canvasId, labels, data, colors, horizontal = false) {
        destroyIfExists(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        const bgColors = colors || labels.map((_, i) => COLORS.palette[i % COLORS.palette.length]);

        const chart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    data,
                    backgroundColor: bgColors.map(c => c + 'cc'),
                    borderColor: bgColors,
                    borderWidth: 1,
                    borderRadius: 6,
                    maxBarThickness: horizontal ? 18 : 40
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                indexAxis: horizontal ? 'y' : 'x',
                plugins: {
                    legend: { display: false },
                    tooltip: { backgroundColor: '#1e293b', titleFont: defaultFont, bodyFont: defaultFont, padding: 12, cornerRadius: 8 }
                },
                scales: {
                    x: { grid: { display: horizontal }, ticks: { font: defaultFont, color: COLORS.gray } },
                    y: { beginAtZero: true, grid: { display: !horizontal, color: '#f1f5f9' }, ticks: { font: defaultFont, color: COLORS.gray } }
                }
            }
        });
        chartInstances.set(canvasId, chart);
    },

    renderDoughnutChart(canvasId, labels, data, colors) {
        destroyIfExists(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        const bgColors = colors || labels.map((_, i) => COLORS.palette[i % COLORS.palette.length]);

        const chart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels,
                datasets: [{
                    data,
                    backgroundColor: bgColors,
                    borderWidth: 2,
                    borderColor: '#fff',
                    hoverOffset: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '65%',
                plugins: {
                    legend: { position: 'bottom', labels: { font: defaultFont, usePointStyle: true, padding: 12 } },
                    tooltip: {
                        backgroundColor: '#1e293b', titleFont: defaultFont, bodyFont: defaultFont, padding: 12, cornerRadius: 8,
                        callbacks: { label: ctx => `${ctx.label}: ${ctx.parsed.toLocaleString('vi-VN')}₫` }
                    }
                }
            }
        });
        chartInstances.set(canvasId, chart);
    },

    destroyChart(canvasId) {
        destroyIfExists(canvasId);
    }
};
