document.addEventListener('DOMContentLoaded', function () {
    pollRunningScans();
    setupAutoRefresh();
});

function pollRunningScans() {
    var indicator = document.getElementById('scan-running-indicator');
    if (!indicator) return;

    fetch('/ScanJobs/RunningScans')
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (data && data.length > 0) {
                indicator.style.display = 'block';
                var list = indicator.querySelector('.scan-running-list');
                if (list) {
                    list.innerHTML = data.map(function (r) {
                        return '<div class="scan-running-item">' +
                            '<span class="scan-running-job">' + r.jobName + '</span>' +
                            '<span class="scan-running-status status-pill">' + r.status + '</span>' +
                            '<span class="scan-running-time">' + r.startedAt + '</span>' +
                            '</div>';
                    }).join('');
                }
            } else {
                indicator.style.display = 'none';
            }
        })
        .catch(function () { });

    setTimeout(pollRunningScans, 5000);
}

function setupAutoRefresh() {
    var refreshZone = document.getElementById('auto-refresh-zone');
    if (!refreshZone) return;

    var interval = parseInt(refreshZone.getAttribute('data-interval') || '10', 10) * 1000;
    setTimeout(function () {
        location.reload();
    }, interval);
}
