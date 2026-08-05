window.dashboardChart = {
    _chart: null,

    // Datasets arrive with a token name (e.g. "--rm-you") instead of a literal colour,
    // so the chart re-tints with the theme instead of hardcoding the palette in C#.
    _token: function (name, fallback) {
        const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
        return value || fallback;
    },

    render: function (canvasId, config) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        const fg = this._token('--rm-muted-fg', '#606d64');
        const border = this._token('--rm-border', '#dbe0d6');

        (config.data?.datasets ?? []).forEach((dataset) => {
            if (dataset.colorToken) {
                dataset.borderColor = this._token(dataset.colorToken, fg);
                dataset.backgroundColor = dataset.borderColor;
                dataset.pointBackgroundColor = dataset.borderColor;
            }
        });

        config.options = {
            ...config.options,
            plugins: {
                legend: { labels: { color: fg, usePointStyle: true, boxWidth: 8 } }
            },
            scales: {
                x: { ...config.options?.scales?.x, ticks: { color: fg }, grid: { color: border } },
                y: { ...config.options?.scales?.y, ticks: { color: fg }, grid: { color: border } }
            }
        };

        if (this._chart) this._chart.destroy();
        this._chart = new Chart(ctx, config);
    },

    destroy: function () {
        if (this._chart) {
            this._chart.destroy();
            this._chart = null;
        }
    }
};
