// Chart.js helper functions for Blazor WebAssembly

window.chartHelpers = {
    charts: {},

    createLineChart: function (elementId, data, options) {
        const ctx = document.getElementById(elementId);
        if (!ctx) return null;

        // Destroy existing chart if it exists
        if (this.charts[elementId]) {
            this.charts[elementId].destroy();
        }

        const chartData = {
            labels: data.labels || [],
            datasets: [{
                label: data.label || 'Data',
                data: data.values || [],
                borderColor: 'rgb(59, 130, 246)',
                backgroundColor: 'rgba(59, 130, 246, 0.1)',
                tension: 0.4,
                fill: true
            }]
        };

        const chartOptions = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: options?.showLegend ?? true,
                    position: 'top',
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: {
                        display: true,
                        color: 'rgba(0, 0, 0, 0.05)'
                    }
                },
                x: {
                    grid: {
                        display: false
                    }
                }
            },
            interaction: {
                mode: 'nearest',
                axis: 'x',
                intersect: false
            }
        };

        this.charts[elementId] = new Chart(ctx, {
            type: 'line',
            data: chartData,
            options: chartOptions
        });

        return this.charts[elementId];
    },

    createAreaChart: function (elementId, data, options) {
        const ctx = document.getElementById(elementId);
        if (!ctx) return null;

        if (this.charts[elementId]) {
            this.charts[elementId].destroy();
        }

        const chartData = {
            labels: data.labels || [],
            datasets: [{
                label: data.label || 'Revenue',
                data: data.values || [],
                borderColor: 'rgb(34, 197, 94)',
                backgroundColor: 'rgba(34, 197, 94, 0.2)',
                fill: true,
                tension: 0.4
            }]
        };

        const chartOptions = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: options?.showLegend ?? true,
                    position: 'top',
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
                    callbacks: {
                        label: function(context) {
                            let label = context.dataset.label || '';
                            if (label) {
                                label += ': ';
                            }
                            if (context.parsed.y !== null) {
                                label += new Intl.NumberFormat('en-US', {
                                    style: 'currency',
                                    currency: 'USD'
                                }).format(context.parsed.y);
                            }
                            return label;
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function(value, index, values) {
                            return '$' + value.toLocaleString();
                        }
                    },
                    grid: {
                        display: true,
                        color: 'rgba(0, 0, 0, 0.05)'
                    }
                },
                x: {
                    grid: {
                        display: false
                    }
                }
            }
        };

        this.charts[elementId] = new Chart(ctx, {
            type: 'line',
            data: chartData,
            options: chartOptions
        });

        return this.charts[elementId];
    },

    createBarChart: function (elementId, data, options) {
        const ctx = document.getElementById(elementId);
        if (!ctx) return null;

        if (this.charts[elementId]) {
            this.charts[elementId].destroy();
        }

        const chartData = {
            labels: data.labels || [],
            datasets: [{
                label: data.label || 'Enrollments',
                data: data.values || [],
                backgroundColor: [
                    'rgba(59, 130, 246, 0.8)',
                    'rgba(34, 197, 94, 0.8)',
                    'rgba(245, 158, 11, 0.8)',
                    'rgba(239, 68, 68, 0.8)',
                    'rgba(168, 85, 247, 0.8)',
                    'rgba(14, 165, 233, 0.8)',
                    'rgba(236, 72, 153, 0.8)',
                    'rgba(99, 102, 241, 0.8)',
                    'rgba(6, 182, 212, 0.8)',
                    'rgba(251, 146, 60, 0.8)'
                ],
                borderWidth: 0
            }]
        };

        const chartOptions = {
            responsive: true,
            maintainAspectRatio: false,
            indexAxis: options?.horizontal ? 'y' : 'x',
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: {
                        display: true,
                        color: 'rgba(0, 0, 0, 0.05)'
                    }
                },
                x: {
                    grid: {
                        display: false
                    }
                }
            }
        };

        this.charts[elementId] = new Chart(ctx, {
            type: 'bar',
            data: chartData,
            options: chartOptions
        });

        return this.charts[elementId];
    },

    updateChart: function (elementId, newData) {
        const chart = this.charts[elementId];
        if (!chart) return;

        if (newData.labels) {
            chart.data.labels = newData.labels;
        }

        if (newData.values && chart.data.datasets[0]) {
            chart.data.datasets[0].data = newData.values;
        }

        chart.update();
    },

    destroyChart: function (elementId) {
        if (this.charts[elementId]) {
            this.charts[elementId].destroy();
            delete this.charts[elementId];
        }
    },

    destroyAllCharts: function () {
        Object.keys(this.charts).forEach(key => {
            this.charts[key].destroy();
        });
        this.charts = {};
    }
};