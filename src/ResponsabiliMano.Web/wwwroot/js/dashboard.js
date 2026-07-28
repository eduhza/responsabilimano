window.dashboardChart = {
    _chart: null,
    render: function (canvasId, config) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
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
