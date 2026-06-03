const state = {
    token: localStorage.getItem('vulnshield_token') || '',
    currentUser: null,
    assets: [],
    selectedAssetId: null,
};

const deviceTypeLabels = {
    Computer: '電腦',
    Server: '伺服器',
    Firewall: '防火牆',
    Router: '路由器',
    Switch: '交換器',
    NAS: 'NAS',
    NetworkDevice: '其他網通設備',
    Other: '其他',
};

const envLabels = {
    Production: '正式環境',
    Staging: '測試環境',
    Development: '開發環境',
};

const loginForm = document.getElementById('login-form');
const assetForm = document.getElementById('asset-form');
const logoutButton = document.getElementById('logout');
const assetList = document.getElementById('asset-list');
const assetDetail = document.getElementById('asset-detail');
const authStatus = document.getElementById('auth-status');
const selectedAssetStatus = document.getElementById('selected-asset-status');
const assetSearch = document.getElementById('asset-search');
const assetTypeFilter = document.getElementById('asset-type-filter');
const scanSearch = document.getElementById('scan-search');
const scanStatusFilter = document.getElementById('scan-status-filter');
const scanList = document.getElementById('scan-list');
const reportSummary = document.getElementById('report-summary');
const reportAssets = document.getElementById('report-assets');
const reportSignals = document.getElementById('report-signals');
const reportStatus = document.getElementById('report-status');
const navTabs = Array.from(document.querySelectorAll('.nav-tab'));
const refreshAllButton = document.getElementById('refresh-all');

state.scans = [];
state.report = null;
state.activeTab = 'devices';

function showMessage(message) {
    window.alert(message);
}

function statusChip(status) {
    if (!status) {
        return '<span class="status-chip muted">尚無掃描</span>';
    }

    const normalized = String(status);
    const className = normalized === 'Completed'
        ? 'safe'
        : normalized === 'Failed'
            ? 'danger'
            : normalized === 'Running'
                ? 'warning'
                : 'muted';
    return `<span class="status-chip ${className}">${normalized}</span>`;
}

function formatDate(value) {
    if (!value) {
        return '尚無資料';
    }
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleString('zh-TW');
}

function buildHeaders(extraHeaders = {}) {
    const headers = { ...extraHeaders };
    if (state.token) {
        headers.Authorization = `Bearer ${state.token}`;
    }
    return headers;
}

async function apiRequest(url, options = {}) {
    const response = await fetch(url, {
        ...options,
        headers: buildHeaders(options.headers),
    });

    if (!response.ok) {
        const payload = await response.text();
        throw new Error(payload || `HTTP ${response.status}`);
    }

    const contentType = response.headers.get('content-type') || '';
    if (contentType.includes('application/json')) {
        return response.json();
    }
    return response.text();
}

async function login(username, password) {
    const body = new URLSearchParams();
    body.set('username', username);
    body.set('password', password);

    const response = await fetch('/token', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body,
    });

    if (!response.ok) {
        throw new Error('登入失敗，請確認帳號密碼。');
    }

    const payload = await response.json();
    state.token = payload.access_token;
    localStorage.setItem('vulnshield_token', state.token);
}

async function fetchCurrentUser() {
    state.currentUser = await apiRequest('/users/me');
    authStatus.textContent = `${state.currentUser.username} / ${state.currentUser.role}`;
    authStatus.className = 'status-chip safe';
}

function logout() {
    state.token = '';
    state.currentUser = null;
    state.assets = [];
    state.selectedAssetId = null;
    localStorage.removeItem('vulnshield_token');
    authStatus.textContent = '尚未登入';
    authStatus.className = 'status-chip muted';
    selectedAssetStatus.textContent = '尚未選取';
    selectedAssetStatus.className = 'status-chip muted';
    assetList.innerHTML = '<div class="empty-state">請先登入，再載入設備清單。</div>';
    assetDetail.innerHTML = '選一台設備後，這裡會顯示設備資料、掃描歷史、掃描內容與弱點清單。';
    scanList.innerHTML = '<div class="empty-state">請先登入，再載入掃描任務。</div>';
    reportSummary.innerHTML = '<div class="empty-state">請先登入，再載入報告資料。</div>';
    reportAssets.innerHTML = '<div class="empty-state">尚未有報告內容。</div>';
    reportSignals.innerHTML = '<div class="empty-state">尚未有掃描彙總資料。</div>';
    reportStatus.textContent = '尚未載入';
    reportStatus.className = 'status-chip muted';
    updateMetrics([]);
}

function updateMetrics(assets) {
    document.getElementById('metric-assets').textContent = String(assets.length);
    document.getElementById('metric-high-risk').textContent = String(
        assets.reduce((sum, asset) => sum + (asset.high_risk_findings || 0), 0)
    );
    document.getElementById('metric-open-findings').textContent = String(
        assets.reduce((sum, asset) => sum + (asset.open_findings || 0), 0)
    );
}

function filteredAssets() {
    const search = assetSearch.value.trim().toLowerCase();
    const typeFilter = assetTypeFilter.value;

    return state.assets.filter((asset) => {
        const matchesType = !typeFilter || asset.device_type === typeFilter;
        if (!matchesType) {
            return false;
        }

        if (!search) {
            return true;
        }

        const haystack = [
            asset.name,
            asset.target,
            asset.location || '',
            ...(asset.tags || []),
        ]
            .join(' ')
            .toLowerCase();
        return haystack.includes(search);
    });
}

function renderAssets() {
    const assets = filteredAssets();
    updateMetrics(state.assets);

    if (!assets.length) {
        assetList.innerHTML = '<div class="empty-state">目前沒有符合條件的設備。</div>';
        return;
    }

    assetList.innerHTML = assets.map((asset) => `
        <article class="asset-card ${asset.id === state.selectedAssetId ? 'active' : ''}" data-asset-id="${asset.id}">
            <div class="asset-card-head">
                <div>
                    <h3>${asset.name}</h3>
                    <p>${asset.target}</p>
                </div>
                ${statusChip(asset.last_scan_status)}
            </div>
            <div class="pill-row">
                <span class="pill">${deviceTypeLabels[asset.device_type] || asset.device_type}</span>
                <span class="pill">${envLabels[asset.env] || asset.env}</span>
                <span class="pill">重要度 ${asset.criticality}</span>
            </div>
            <div class="asset-meta">
                <span class="pill">未關閉 ${asset.open_findings}</span>
                <span class="pill">高風險 ${asset.high_risk_findings}</span>
                <span class="pill">最後掃描 ${formatDate(asset.last_scan_at)}</span>
            </div>
        </article>
    `).join('');

    document.querySelectorAll('.asset-card').forEach((card) => {
        card.addEventListener('click', () => {
            const assetId = Number(card.dataset.assetId);
            state.selectedAssetId = assetId;
            renderAssets();
            loadAssetDetail(assetId).catch((error) => showMessage(error.message));
        });
    });
}

function filteredScans() {
    const search = scanSearch.value.trim().toLowerCase();
    const statusFilter = scanStatusFilter.value;

    return state.scans.filter((scan) => {
        if (statusFilter && scan.status !== statusFilter) {
            return false;
        }
        if (!search) {
            return true;
        }
        const haystack = [
            String(scan.id),
            scan.asset_name || '',
            scan.asset_target || '',
        ].join(' ').toLowerCase();
        return haystack.includes(search);
    });
}

function findingCard(finding) {
    return `
        <article class="finding-card">
            <div class="finding-head">
                <div>
                    <h3>${finding.vulnerability_title}</h3>
                    <p>${finding.vulnerability_description || '未提供描述'}</p>
                </div>
                ${statusChip(finding.status)}
            </div>
            <div class="pill-row">
                <span class="pill">風險分數 ${finding.risk_score ?? 'N/A'}</span>
                <span class="pill">CVSS ${finding.vulnerability_severity}</span>
                <span class="pill">最後發現 ${formatDate(finding.last_seen)}</span>
            </div>
            <p class="finding-meta">修補建議：${finding.remediation || '請依設備更新公告與組態標準進行修補。'}</p>
        </article>
    `;
}

function scanCard(scan) {
    const summary = scan.scan_summary;
    const enginesHtml = summary?.engines?.length
        ? summary.engines.map((engine) => `
            <div class="signal-item">
                <strong>${engine.name}</strong>
                <p>${engine.detail || '未提供說明'}</p>
                <div class="pill-row">
                    <span class="pill">狀態 ${engine.status}</span>
                </div>
            </div>
        `).join('')
        : '<div class="empty-state">此筆掃描尚未產生引擎摘要。</div>';

    const servicesHtml = summary?.services?.length
        ? summary.services.map((service) => `
            <div class="service-item">
                <strong>${service.port}/${service.protocol} ${service.service}</strong>
                <p>狀態：${service.state}${service.product ? ` ・ ${service.product}` : ''}</p>
            </div>
        `).join('')
        : '<div class="empty-state">本次未記錄到可辨識的服務。</div>';

    const vulnerabilitiesHtml = summary?.vulnerabilities?.length
        ? summary.vulnerabilities.map((signal) => `
            <div class="signal-item">
                <strong>${signal.title}</strong>
                <p>${signal.description || '未提供描述'}</p>
                <div class="pill-row">
                    <span class="pill">Severity ${signal.severity}</span>
                    ${signal.template_id ? `<span class="pill">${signal.template_id}</span>` : ''}
                    ${signal.matched_at ? `<span class="pill">${signal.matched_at}</span>` : ''}
                </div>
            </div>
        `).join('')
        : '<div class="empty-state">本次未命中漏洞模板。</div>';

    const infoHtml = summary?.informational?.length
        ? summary.informational.map((signal) => `
            <div class="signal-item">
                <strong>${signal.title}</strong>
                <p>${signal.description || '未提供描述'}</p>
                <div class="pill-row">
                    <span class="pill">Severity ${signal.severity}</span>
                    ${signal.template_id ? `<span class="pill">${signal.template_id}</span>` : ''}
                    ${signal.matched_at ? `<span class="pill">${signal.matched_at}</span>` : ''}
                </div>
            </div>
        `).join('')
        : '<div class="empty-state">本次沒有額外資訊或風險提示。</div>';

    return `
        <article class="scan-card">
            <div class="scan-head">
                <div>
                    <h3>掃描任務 #${scan.id}</h3>
                    <p>${scan.asset_name || '未知設備'}${scan.asset_target ? ` ・ ${scan.asset_target}` : ''}</p>
                </div>
                ${statusChip(scan.status)}
            </div>
            <div class="pill-row">
                <span class="pill">Profile ${scan.scan_profile}</span>
                ${scan.asset_device_type ? `<span class="pill">${deviceTypeLabels[scan.asset_device_type] || scan.asset_device_type}</span>` : ''}
                <span class="pill">開始 ${formatDate(scan.started_at)}</span>
                <span class="pill">結束 ${formatDate(scan.finished_at)}</span>
            </div>
            <p class="scan-meta">${scan.error_message || '目前無錯誤訊息。'}</p>
            <div class="summary-grid">
                <section class="scan-summary-block">
                    <h4>本次掃描用了哪些引擎</h4>
                    <div class="signal-list">${enginesHtml}</div>
                </section>
                <section class="scan-summary-block">
                    <h4>掃到了哪些服務</h4>
                    <div class="service-list">${servicesHtml}</div>
                </section>
                <section class="scan-summary-block">
                    <h4>哪些是漏洞</h4>
                    <div class="signal-list">${vulnerabilitiesHtml}</div>
                </section>
                <section class="scan-summary-block">
                    <h4>哪些只是資訊或風險提示</h4>
                    <div class="signal-list">${infoHtml}</div>
                </section>
            </div>
        </article>
    `;
}

function renderScans() {
    const scans = filteredScans();
    if (!scans.length) {
        scanList.innerHTML = '<div class="empty-state">目前沒有符合條件的掃描任務。</div>';
        return;
    }
    scanList.innerHTML = scans.map(scanCard).join('');
}

function renderReports() {
    if (!state.report) {
        reportSummary.innerHTML = '<div class="empty-state">請先登入，再載入報告資料。</div>';
        reportAssets.innerHTML = '<div class="empty-state">尚未有報告內容。</div>';
        reportSignals.innerHTML = '<div class="empty-state">尚未有掃描彙總資料。</div>';
        reportStatus.textContent = '尚未載入';
        reportStatus.className = 'status-chip muted';
        return;
    }

    reportStatus.textContent = state.report.compliance_status;
    reportStatus.className = state.report.compliance_status === 'Compliant'
        ? 'status-chip safe'
        : 'status-chip warning';

    reportSummary.innerHTML = `
        <article class="report-card">
            <span>總 finding</span>
            <strong>${state.report.summary.total_findings}</strong>
        </article>
        <article class="report-card">
            <span>高風險</span>
            <strong>${state.report.summary.high_risk}</strong>
        </article>
        <article class="report-card">
            <span>中風險</span>
            <strong>${state.report.summary.medium_risk}</strong>
        </article>
        <article class="report-card">
            <span>低風險</span>
            <strong>${state.report.summary.low_risk}</strong>
        </article>
    `;

    const topAssets = [...state.assets]
        .sort((left, right) => (right.high_risk_findings - left.high_risk_findings) || (right.open_findings - left.open_findings))
        .slice(0, 6);

    reportAssets.innerHTML = topAssets.length
        ? topAssets.map((asset) => `
            <article class="scan-card">
                <div class="scan-head">
                    <div>
                        <h3>${asset.name}</h3>
                        <p>${asset.target}</p>
                    </div>
                    <span class="status-chip ${asset.high_risk_findings > 0 ? 'danger' : asset.open_findings > 0 ? 'warning' : 'safe'}">
                        ${asset.high_risk_findings > 0 ? '高風險' : asset.open_findings > 0 ? '待處理' : '穩定'}
                    </span>
                </div>
                <div class="pill-row">
                    <span class="pill">${deviceTypeLabels[asset.device_type] || asset.device_type}</span>
                    <span class="pill">未關閉 ${asset.open_findings}</span>
                    <span class="pill">高風險 ${asset.high_risk_findings}</span>
                    <span class="pill">最後掃描 ${formatDate(asset.last_scan_at)}</span>
                </div>
            </article>
        `).join('')
        : '<div class="empty-state">目前尚無可分析的設備資料。</div>';

    const scanStats = state.scans.reduce((summary, scan) => {
        const status = scan.status || 'Unknown';
        summary.byStatus[status] = (summary.byStatus[status] || 0) + 1;

        if (scan.scan_summary) {
            summary.services += scan.scan_summary.service_count || 0;
            summary.vulnerabilities += scan.scan_summary.vulnerability_count || 0;
            summary.informational += scan.scan_summary.informational_count || 0;
        }
        return summary;
    }, {
        byStatus: {},
        services: 0,
        vulnerabilities: 0,
        informational: 0,
    });

    reportSignals.innerHTML = `
        <article class="report-card">
            <span>已完成掃描</span>
            <strong>${scanStats.byStatus.Completed || 0}</strong>
        </article>
        <article class="report-card">
            <span>待處理 / 執行中</span>
            <strong>${(scanStats.byStatus.Pending || 0) + (scanStats.byStatus.Running || 0)}</strong>
        </article>
        <article class="report-card">
            <span>掃描失敗</span>
            <strong>${scanStats.byStatus.Failed || 0}</strong>
        </article>
        <article class="report-card">
            <span>已辨識服務</span>
            <strong>${scanStats.services}</strong>
        </article>
        <article class="report-card">
            <span>漏洞訊號</span>
            <strong>${scanStats.vulnerabilities}</strong>
        </article>
        <article class="report-card">
            <span>資訊 / 風險提示</span>
            <strong>${scanStats.informational}</strong>
        </article>
    `;
}

function setActiveTab(tabName) {
    state.activeTab = tabName;
    navTabs.forEach((tab) => {
        tab.classList.toggle('active', tab.dataset.tab === tabName);
    });
    document.querySelectorAll('.tab-panel').forEach((panel) => {
        panel.classList.toggle('active', panel.id === `tab-${tabName}`);
    });
}

function renderAssetDetail(asset, scans, findings) {
    selectedAssetStatus.textContent = asset.name;
    selectedAssetStatus.className = 'status-chip accent';

    assetDetail.innerHTML = `
        <div class="detail-stack">
            <div class="detail-actions">
                <div>
                    <p class="eyebrow">Selected Device</p>
                    <h2>${asset.name}</h2>
                    <p class="subtle">${asset.target} ・ ${deviceTypeLabels[asset.device_type] || asset.device_type}</p>
                </div>
                <button id="run-scan" class="primary-button" type="button">執行弱點掃描</button>
            </div>

            <div class="detail-grid">
                <article>
                    <h3>位置</h3>
                    <p>${asset.location || '未填寫'}</p>
                </article>
                <article>
                    <h3>環境 / 重要度</h3>
                    <p>${envLabels[asset.env] || asset.env} / ${asset.criticality}</p>
                </article>
                <article>
                    <h3>標籤</h3>
                    <p>${(asset.tags || []).join('、') || '未設定'}</p>
                </article>
                <article>
                    <h3>風險摘要</h3>
                    <p>總 finding ${asset.total_findings} / 未關閉 ${asset.open_findings} / 高風險 ${asset.high_risk_findings}</p>
                </article>
            </div>

            <section class="section-block">
                <h3 class="section-title">設備備註</h3>
                <div class="asset-card">
                    <p>${asset.notes || '未提供設備備註。'}</p>
                </div>
            </section>

            <section class="section-block">
                <h3 class="section-title">最近掃描歷史</h3>
                <div class="detail-sections">
                    ${scans.length ? scans.map(scanCard).join('') : '<div class="empty-state">尚未有掃描歷史。</div>'}
                </div>
            </section>

            <section class="section-block">
                <h3 class="section-title">目前弱點</h3>
                <div class="detail-sections">
                    ${findings.length ? findings.map(findingCard).join('') : '<div class="empty-state">此設備目前沒有 finding。</div>'}
                </div>
            </section>
        </div>
    `;

    document.getElementById('run-scan').addEventListener('click', async () => {
        try {
            const task = await apiRequest(`/assets/${asset.id}/scan`, { method: 'POST' });
            showMessage(`已建立掃描任務 #${task.id}`);
            await loadAssets();
            await loadAssetDetail(asset.id);
        } catch (error) {
            showMessage(error.message);
        }
    });
}

async function loadAssets() {
    if (!state.token) {
        showMessage('請先登入。');
        return;
    }

    state.assets = await apiRequest('/assets');
    renderAssets();

    if (state.selectedAssetId) {
        const stillExists = state.assets.some((asset) => asset.id === state.selectedAssetId);
        if (stillExists) {
            await loadAssetDetail(state.selectedAssetId);
        }
    }
}

async function loadScans() {
    if (!state.token) {
        return;
    }
    state.scans = await apiRequest('/scans');
    renderScans();
}

async function loadReports() {
    if (!state.token) {
        return;
    }
    state.report = await apiRequest('/reports/iso27001');
    renderReports();
}

async function loadAssetDetail(assetId) {
    const [asset, scans, findings] = await Promise.all([
        apiRequest(`/assets/${assetId}`),
        apiRequest(`/assets/${assetId}/scans`),
        apiRequest(`/assets/${assetId}/findings`),
    ]);
    renderAssetDetail(asset, scans, findings);
}

loginForm.addEventListener('submit', async (event) => {
    event.preventDefault();
    const formData = new FormData(loginForm);
    try {
        await login(String(formData.get('username') || ''), String(formData.get('password') || ''));
        await fetchCurrentUser();
        await Promise.all([loadAssets(), loadScans(), loadReports()]);
        showMessage('登入成功。');
    } catch (error) {
        logout();
        showMessage(error.message);
    }
});

assetForm.addEventListener('submit', async (event) => {
    event.preventDefault();
    if (!state.currentUser) {
        showMessage('請先登入後再建立設備。');
        return;
    }

    const formData = new FormData(assetForm);
    const payload = {
        name: String(formData.get('name') || ''),
        target: String(formData.get('target') || ''),
        criticality: Number(formData.get('criticality') || 3),
        env: String(formData.get('env') || 'Production'),
        device_type: String(formData.get('device_type') || 'Computer'),
        location: String(formData.get('location') || '').trim() || null,
        tags: String(formData.get('tags') || '')
            .split(',')
            .map((tag) => tag.trim())
            .filter(Boolean),
        notes: String(formData.get('notes') || '').trim() || null,
        status: 'Active',
        owner_id: state.currentUser.id,
    };

    try {
        const asset = await apiRequest('/assets', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
        });
        assetForm.reset();
        state.selectedAssetId = asset.id;
        await Promise.all([loadAssets(), loadScans(), loadReports()]);
        await loadAssetDetail(asset.id);
        showMessage('設備建立成功。');
    } catch (error) {
        showMessage(error.message);
    }
});

refreshAllButton.addEventListener('click', () => {
    Promise.all([loadAssets(), loadScans(), loadReports()]).catch((error) => showMessage(error.message));
});

logoutButton.addEventListener('click', logout);
assetSearch.addEventListener('input', renderAssets);
assetTypeFilter.addEventListener('change', renderAssets);
scanSearch.addEventListener('input', renderScans);
scanStatusFilter.addEventListener('change', renderScans);
navTabs.forEach((tab) => {
    tab.addEventListener('click', () => setActiveTab(tab.dataset.tab));
});

(async function bootstrap() {
    if (!state.token) {
        return;
    }
    try {
        await fetchCurrentUser();
        await Promise.all([loadAssets(), loadScans(), loadReports()]);
    } catch (error) {
        logout();
    }
})();
