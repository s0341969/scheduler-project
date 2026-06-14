(function () {
    var connection = null;
    var connectionId = null;

    function initSignalR() {
        var protocol = window.location.protocol === 'https:' ? 'https:' : 'http:';
        var hubUrl = protocol + '//' + window.location.host + '/hub/notifications';

        if (typeof signalR === 'undefined') {
            loadSignalRScript(hubUrl);
            return;
        }
        connectSignalR(hubUrl);
    }

    function loadSignalRScript(hubUrl) {
        var script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.0/dist/browser/signalr.min.js';
        script.integrity = 'sha512-dU79SwyS2dR5KCT6dQkAmqT/dBYD8dNHHLjk3HLtb8uG/dUPCs5qIalKVf2Hh6Fq9Z0qIqlNS7EABnrC/4ZpNA==';
        script.crossOrigin = 'anonymous';
        script.onload = function () { connectSignalR(hubUrl); };
        script.onerror = function () {
            console.warn('SignalR SDK 載入失敗，即時通知功能無法使用。');
        };
        document.head.appendChild(script);
    }

    function connectSignalR(hubUrl) {
        if (typeof signalR === 'undefined') {
            console.warn('SignalR 尚未載入完成，跳過連線。');
            return;
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        connection.on('ScanStatusChanged', function (data) {
            updateScanNotification(data);
        });

        connection.onreconnecting(function () {
            console.log('SignalR 重新連線中...');
        });

        connection.onreconnected(function () {
            console.log('SignalR 已重新連線。');
            connectionId = connection.connectionId;
        });

        connection.onclose(function () {
            console.log('SignalR 連線已關閉。');
        });

        connection.start()
            .then(function () {
                connectionId = connection.connectionId;
                console.log('SignalR 已連線。');
            })
            .catch(function (err) {
                console.warn('SignalR 連線失敗：', err.toString());
            });
    }

    function updateScanNotification(data) {
        var indicator = document.getElementById('scan-running-indicator');
        if (!indicator) return;

        if (data.status === 'Running') {
            indicator.style.display = 'block';
            var list = indicator.querySelector('.scan-running-list');
            if (list) {
                list.innerHTML = '<div class="scan-running-item">' +
                    '<span class="scan-running-job">' + (data.jobName || '') + '</span>' +
                    '<span class="scan-running-status status-pill">' + data.status + '</span>' +
                    '<span class="scan-running-time">' + (data.startedAt || '') + '</span>' +
                    '</div>';
            }
        } else if (data.status === 'Completed' || data.status === 'Failed') {
            showToast(
                data.status === 'Completed' ? '掃描完成' : '掃描失敗',
                data.jobName + ' - ' + (data.status === 'Completed' ? '已完成' : '失敗：' + (data.errorMessage || '')),
                data.status === 'Completed' ? 'success' : 'error'
            );
            setTimeout(function () {
                var runningScans = document.querySelectorAll('.scan-running-item').length;
                if (runningScans <= 1) {
                    indicator.style.display = 'none';
                }
            }, 2000);
        }
    }

    function showToast(title, message, type) {
        var toastContainer = document.getElementById('toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toast-container';
            toastContainer.style.cssText = 'position:fixed;top:20px;right:20px;z-index:9999;display:flex;flex-direction:column;gap:8px;';
            document.body.appendChild(toastContainer);
        }

        var toast = document.createElement('div');
        toast.className = 'toast-notification toast-' + (type || 'info');
        toast.style.cssText = 'padding:12px 20px;border-radius:8px;color:#fff;font-size:14px;box-shadow:0 4px 12px rgba(0,0,0,0.15);animation:slideIn 0.3s ease;max-width:400px;';

        var bgColors = { success: '#10b981', error: '#ef4444', info: '#3b82f6', warning: '#f59e0b' };
        toast.style.backgroundColor = bgColors[type] || bgColors.info;

        toast.innerHTML = '<strong>' + title + '</strong><br><span style="font-size:13px;opacity:0.9;">' + message + '</span>';

        toastContainer.appendChild(toast);

        setTimeout(function () {
            toast.style.opacity = '0';
            toast.style.transition = 'opacity 0.3s ease';
            setTimeout(function () { toast.remove(); }, 300);
        }, 5000);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSignalR);
    } else {
        initSignalR();
    }
})();
