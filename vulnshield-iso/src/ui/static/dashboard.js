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
const loadAssetsButton = document.getElementById('load-assets');
const logoutButton = document.getElementById('logout');
const assetList = document.getElementById('asset-list');
const assetDetail = document.getElementById('asset-detail');
const authStatus = document.getElementById('auth-status');
const selectedAssetStatus = document.getElementById('selected-asset-status');
const assetSearch = document.getElementById('asset-search');
const assetTypeFilter = document.getElementById('asset-type-filter');

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
    assetDetail.innerHTML = '選一台設備後，這裡會顯示設備資料、掃描歷史與弱點清單。';
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
    return `
        <article class="scan-card">
            <div class="scan-head">
                <h3>掃描任務 #${scan.id}</h3>
                ${statusChip(scan.status)}
            </div>
            <div class="pill-row">
                <span class="pill">Profile ${scan.scan_profile}</span>
                <span class="pill">開始 ${formatDate(scan.started_at)}</span>
                <span class="pill">結束 ${formatDate(scan.finished_at)}</span>
            </div>
            <p class="scan-meta">${scan.error_message || '目前無錯誤訊息。'}</p>
        </article>
    `;
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
        await loadAssets();
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
        await loadAssets();
        await loadAssetDetail(asset.id);
        showMessage('設備建立成功。');
    } catch (error) {
        showMessage(error.message);
    }
});

loadAssetsButton.addEventListener('click', () => {
    loadAssets().catch((error) => showMessage(error.message));
});

logoutButton.addEventListener('click', logout);
assetSearch.addEventListener('input', renderAssets);
assetTypeFilter.addEventListener('change', renderAssets);

(async function bootstrap() {
    if (!state.token) {
        return;
    }
    try {
        await fetchCurrentUser();
        await loadAssets();
    } catch (error) {
        logout();
    }
})();
