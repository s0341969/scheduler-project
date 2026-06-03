const state = {
    token: localStorage.getItem('vulnshield_token') || '',
    currentUser: null,
    assets: [],
    selectedAssetId: null,
    editingAssetId: null,
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
const assetFormTitle = document.getElementById('asset-form-title');
const assetFormStatus = document.getElementById('asset-form-status');
const assetFormSubmit = document.getElementById('asset-form-submit');
const assetFormCancel = document.getElementById('asset-form-cancel');
const credentialForm = document.getElementById('credential-form');
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
const deviceTypeSelect = document.getElementById('device-type-select');
const assetProfileSelect = document.getElementById('asset-profile-select');
const assetTemplateSelect = document.getElementById('asset-template-select');
const assetCredentialSelect = document.getElementById('asset-credential-select');
const credentialKindSelect = document.getElementById('credential-kind-select');
const credentialPortInput = document.getElementById('credential-port-input');
const credentialUsernameInput = document.getElementById('credential-username-input');
const credentialDomainInput = document.getElementById('credential-domain-input');
const credentialPrimaryInput = document.getElementById('credential-primary-input');
const credentialSecondaryInput = document.getElementById('credential-secondary-input');
const credentialPrimaryLabel = document.getElementById('credential-primary-label');
const credentialSecondaryLabel = document.getElementById('credential-secondary-label');
const credentialKindHint = document.getElementById('credential-kind-hint');
const credentialList = document.getElementById('credential-list');
const reportSummary = document.getElementById('report-summary');
const reportAssets = document.getElementById('report-assets');
const reportSignals = document.getElementById('report-signals');
const reportStatus = document.getElementById('report-status');
const navTabs = Array.from(document.querySelectorAll('.nav-tab'));
const refreshAllButton = document.getElementById('refresh-all');

state.scans = [];
state.report = null;
state.activeTab = 'devices';
state.catalog = {
    profiles: [],
    templates: [],
    credentialKinds: [],
};
state.credentials = [];

const deviceTypeToTemplate = {
    Computer: 'generic',
    Server: 'generic',
    Firewall: 'firewall',
    Router: 'switch',
    Switch: 'switch',
    NAS: 'nas',
    NetworkDevice: 'switch',
    Other: 'generic',
};

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

function populateDeviceSelectors() {
    deviceTypeSelect.innerHTML = Object.entries(deviceTypeLabels)
        .map(([value, label]) => `<option value="${value}">${label}</option>`)
        .join('');

    assetTypeFilter.innerHTML = [
        '<option value="">全部類型</option>',
        ...Object.entries(deviceTypeLabels).map(([value, label]) => `<option value="${value}">${label}</option>`),
    ].join('');
}

function profileLabel(profileKey) {
    const profile = state.catalog.profiles.find((item) => item.key === profileKey);
    return profile ? profile.label : profileKey;
}

function templateLabel(templateKey) {
    const template = state.catalog.templates.find((item) => item.key === templateKey);
    return template ? template.label : templateKey;
}

function populateProfileSelectors() {
    assetProfileSelect.innerHTML = state.catalog.profiles
        .map((profile) => `<option value="${profile.key}">${profile.label}</option>`)
        .join('');
    assetTemplateSelect.innerHTML = state.catalog.templates
        .map((template) => `<option value="${template.key}">${template.label}</option>`)
        .join('');
}

function populateCredentialSelectors() {
    const activeCredentials = state.credentials.filter((credential) => credential.is_active);
    assetCredentialSelect.innerHTML = [
        '<option value="">不綁定 credential</option>',
        ...activeCredentials.map((credential) => `<option value="${credential.id}">${credential.name} / ${credential.kind_label}</option>`),
    ].join('');

    credentialKindSelect.innerHTML = state.catalog.credentialKinds
        .map((kind) => `<option value="${kind.key}">${kind.label}</option>`)
        .join('');
    updateCredentialKindForm();
}

function updateCredentialKindForm() {
    const kind = state.catalog.credentialKinds.find((item) => item.key === credentialKindSelect.value);
    if (!kind) {
        return;
    }
    credentialPortInput.value = kind.default_port || '';
    credentialUsernameInput.disabled = !kind.requires_username;
    credentialDomainInput.disabled = !kind.supports_domain;
    credentialSecondaryInput.disabled = !kind.requires_secondary_secret;
    credentialPrimaryLabel.textContent = kind.key === 'SNMPv2c' ? 'Community' : kind.key === 'LinuxSSHKey' ? 'SSH 私鑰' : '主要密碼';
    credentialSecondaryLabel.textContent = kind.key === 'LinuxSSHKey' ? 'Passphrase' : '第二密鑰 / passphrase';
    credentialKindHint.textContent = kind.description;
}

function renderCredentials() {
    if (!state.credentials.length) {
        credentialList.innerHTML = '<div class="empty-state">目前沒有可用 credential。</div>';
        return;
    }
    credentialList.innerHTML = state.credentials.map((credential) => `
        <article class="credential-card ${credential.is_active ? '' : 'inactive'}">
            <strong>${credential.name}</strong>
            <p>${credential.kind_label}</p>
            <div class="pill-row">
                ${credential.username ? `<span class="pill">${credential.username}</span>` : ''}
                ${credential.domain ? `<span class="pill">${credential.domain}</span>` : ''}
                ${credential.port ? `<span class="pill">Port ${credential.port}</span>` : ''}
                <span class="pill">${credential.is_active ? '啟用中' : '已停用'}</span>
                <span class="pill">${credential.has_primary_secret ? '已保存主密鑰' : '未保存主密鑰'}</span>
                ${credential.has_secondary_secret ? '<span class="pill">含第二密鑰</span>' : ''}
            </div>
            <p>${credential.notes || '未提供備註'}</p>
            <p>最後使用：${formatDate(credential.last_used_at)} ・ 建立時間：${formatDate(credential.created_at)}</p>
            <div class="action-row">
                <button class="secondary-button credential-toggle" type="button" data-credential-id="${credential.id}" data-active="${credential.is_active ? 'true' : 'false'}">
                    ${credential.is_active ? '停用 credential' : '重新啟用'}
                </button>
                <button class="ghost-button credential-delete" type="button" data-credential-id="${credential.id}">
                    刪除 credential
                </button>
            </div>
        </article>
    `).join('');

    document.querySelectorAll('.credential-toggle').forEach((button) => {
        button.addEventListener('click', async () => {
            const credentialId = Number(button.dataset.credentialId);
            const nextState = button.dataset.active !== 'true';
            try {
                await apiRequest(`/credentials/${credentialId}`, {
                    method: 'PATCH',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ is_active: nextState }),
                });
                await Promise.all([loadCatalog(), loadCredentials(), loadAssets()]);
                if (state.selectedAssetId) {
                    await loadAssetDetail(state.selectedAssetId);
                }
                showMessage(nextState ? 'Credential 已重新啟用。' : 'Credential 已停用。');
            } catch (error) {
                showMessage(error.message);
            }
        });
    });

    document.querySelectorAll('.credential-delete').forEach((button) => {
        button.addEventListener('click', async () => {
            const credentialId = Number(button.dataset.credentialId);
            if (!window.confirm('刪除後無法復原，且必須先解除設備綁定。確定要刪除這筆 credential 嗎？')) {
                return;
            }
            try {
                await apiRequest(`/credentials/${credentialId}`, { method: 'DELETE' });
                await Promise.all([loadCatalog(), loadCredentials(), loadAssets()]);
                if (state.selectedAssetId) {
                    await loadAssetDetail(state.selectedAssetId);
                }
                showMessage('Credential 已刪除。');
            } catch (error) {
                showMessage(error.message);
            }
        });
    });
}

async function loadCatalog() {
    if (!state.token) {
        return;
    }
    const [profiles, templates, credentialKinds] = await Promise.all([
        apiRequest('/scans/profiles'),
        apiRequest('/scans/templates'),
        apiRequest('/credentials/kinds'),
    ]);
    state.catalog = { profiles, templates, credentialKinds };
    populateProfileSelectors();
    populateCredentialSelectors();
}

async function loadCredentials() {
    if (!state.token) {
        return;
    }
    state.credentials = await apiRequest('/credentials');
    populateCredentialSelectors();
    renderCredentials();
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
    state.credentials = [];
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
    credentialList.innerHTML = '<div class="empty-state">請先登入，再載入 credential。</div>';
    resetAssetForm();
    updateMetrics([]);
}

function resetAssetForm() {
    assetForm.reset();
    state.editingAssetId = null;
    assetFormTitle.textContent = '新增設備';
    assetFormStatus.textContent = '資產建檔';
    assetFormStatus.className = 'status-chip accent';
    assetFormSubmit.textContent = '建立設備';
    assetFormCancel.hidden = true;
    if (deviceTypeSelect.options.length) {
        deviceTypeSelect.value = 'Computer';
    }
    if (assetTemplateSelect.options.length) {
        assetTemplateSelect.value = deviceTypeToTemplate.Computer;
    }
    if (assetProfileSelect.options.length) {
        assetProfileSelect.value = 'standard';
    }
    if (assetCredentialSelect.options.length) {
        assetCredentialSelect.value = '';
    }
}

function startAssetEdit(asset) {
    state.editingAssetId = asset.id;
    assetFormTitle.textContent = `編輯設備 #${asset.id}`;
    assetFormStatus.textContent = '設備調整';
    assetFormStatus.className = 'status-chip warning';
    assetFormSubmit.textContent = '儲存設備';
    assetFormCancel.hidden = false;
    assetForm.elements.name.value = asset.name || '';
    assetForm.elements.target.value = asset.target || '';
    assetForm.elements.device_type.value = asset.device_type || 'Computer';
    assetForm.elements.env.value = asset.env || 'Production';
    assetForm.elements.criticality.value = asset.criticality || 3;
    assetForm.elements.location.value = asset.location || '';
    assetForm.elements.default_scan_profile.value = asset.default_scan_profile || 'standard';
    assetForm.elements.template_key.value = asset.template_key || 'generic';
    assetForm.elements.default_credential_id.value = asset.default_credential_id || '';
    assetForm.elements.tags.value = (asset.tags || []).join(', ');
    assetForm.elements.notes.value = asset.notes || '';
    window.scrollTo({ top: 0, behavior: 'smooth' });
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
                <span class="pill">${profileLabel(asset.default_scan_profile)}</span>
                <span class="pill">${asset.template_label || templateLabel(asset.template_key)}</span>
                ${asset.default_credential_name ? `<span class="pill">Credential ${asset.default_credential_name}</span>` : ''}
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

    const misconfigurationsHtml = summary?.misconfigurations?.length
        ? summary.misconfigurations.map((signal) => `
            <div class="signal-item">
                <strong>${signal.title}</strong>
                <p>${signal.description || '未提供描述'}</p>
                <div class="pill-row">
                    <span class="pill">Severity ${signal.severity}</span>
                    ${signal.template_id ? `<span class="pill">${signal.template_id}</span>` : ''}
                </div>
            </div>
        `).join('')
        : '<div class="empty-state">本次未發現明確錯誤設定。</div>';

    const certificateHtml = summary?.certificate_risks?.length
        ? summary.certificate_risks.map((signal) => `
            <div class="signal-item">
                <strong>${signal.title}</strong>
                <p>${signal.description || '未提供描述'}</p>
                <div class="pill-row">
                    <span class="pill">Severity ${signal.severity}</span>
                    ${signal.template_id ? `<span class="pill">${signal.template_id}</span>` : ''}
                </div>
            </div>
        `).join('')
        : '<div class="empty-state">本次未發現憑證風險。</div>';

    const exposureHtml = summary?.exposures?.length
        ? summary.exposures.map((signal) => `
            <div class="signal-item">
                <strong>${signal.title}</strong>
                <p>${signal.description || '未提供描述'}</p>
                <div class="pill-row">
                    <span class="pill">Severity ${signal.severity}</span>
                    ${signal.matched_at ? `<span class="pill">${signal.matched_at}</span>` : ''}
                </div>
            </div>
        `).join('')
        : '<div class="empty-state">本次未發現額外曝露管理面。</div>';

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
                <span class="pill">${summary?.profile_label || profileLabel(scan.scan_profile)}</span>
                <span class="pill">${summary?.device_template_label || templateLabel(scan.device_template || 'generic')}</span>
                ${scan.asset_device_type ? `<span class="pill">${deviceTypeLabels[scan.asset_device_type] || scan.asset_device_type}</span>` : ''}
                ${scan.credential_name ? `<span class="pill">${scan.credential_name} / ${scan.credential_kind}</span>` : ''}
                <span class="pill">開始 ${formatDate(scan.started_at)}</span>
                <span class="pill">結束 ${formatDate(scan.finished_at)}</span>
            </div>
            ${scan.scan_config ? `<p class="scan-meta">範圍：${scan.scan_config.profile.label} / ${scan.scan_config.device_template.label}</p>` : ''}
            ${summary?.authentication?.credential ? `<p class="scan-meta">認證：${summary.authentication.credential.name} / ${summary.authentication.credential.kind}</p>` : ''}
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
                    <h4>哪些是錯誤設定</h4>
                    <div class="signal-list">${misconfigurationsHtml}</div>
                </section>
                <section class="scan-summary-block">
                    <h4>哪些是憑證風險</h4>
                    <div class="signal-list">${certificateHtml}</div>
                </section>
                <section class="scan-summary-block">
                    <h4>哪些是曝露管理介面</h4>
                    <div class="signal-list">${exposureHtml}</div>
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
            summary.misconfigurations += scan.scan_summary.misconfiguration_count || 0;
            summary.certificateRisks += scan.scan_summary.certificate_risk_count || 0;
            summary.exposures += scan.scan_summary.exposure_count || 0;
            summary.informational += scan.scan_summary.informational_count || 0;
        }
        return summary;
    }, {
        byStatus: {},
        services: 0,
        vulnerabilities: 0,
        misconfigurations: 0,
        certificateRisks: 0,
        exposures: 0,
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
            <span>錯誤設定</span>
            <strong>${scanStats.misconfigurations}</strong>
        </article>
        <article class="report-card">
            <span>憑證風險</span>
            <strong>${scanStats.certificateRisks}</strong>
        </article>
        <article class="report-card">
            <span>管理面曝露</span>
            <strong>${scanStats.exposures}</strong>
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
                <div class="inline-action-group">
                    <button id="edit-asset" class="secondary-button" type="button">編輯設備</button>
                    <select id="detail-scan-profile"></select>
                    <button id="run-scan" class="primary-button" type="button">執行弱點掃描</button>
                </div>
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
                    <h3>掃描策略</h3>
                    <p>${profileLabel(asset.default_scan_profile)} / ${asset.template_label || templateLabel(asset.template_key)}</p>
                </article>
                <article>
                    <h3>預設 credential</h3>
                    <p>${asset.default_credential_name ? `${asset.default_credential_name} / ${asset.default_credential_kind}` : '未綁定'}</p>
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

    const detailProfileSelect = document.getElementById('detail-scan-profile');
    detailProfileSelect.innerHTML = state.catalog.profiles
        .map((profile) => `<option value="${profile.key}" ${profile.key === asset.default_scan_profile ? 'selected' : ''}>${profile.label}</option>`)
        .join('');
    const activeCredentials = state.credentials.filter((credential) => credential.is_active || credential.id === asset.default_credential_id);
    const detailCredentialOptions = [
        '<option value="">使用設備預設 credential</option>',
        ...activeCredentials.map((credential) => `<option value="${credential.id}" ${credential.id === asset.default_credential_id ? 'selected' : ''}>${credential.name} / ${credential.kind_label}</option>`),
    ].join('');
    detailProfileSelect.insertAdjacentHTML('afterend', `<select id="detail-scan-credential">${detailCredentialOptions}</select>`);
    const detailCredentialSelect = document.getElementById('detail-scan-credential');
    document.getElementById('edit-asset').addEventListener('click', () => startAssetEdit(asset));

    document.getElementById('run-scan').addEventListener('click', async () => {
        try {
            const task = await apiRequest(`/assets/${asset.id}/scan`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    scan_profile: detailProfileSelect.value,
                    device_template: asset.template_key,
                    credential_id: detailCredentialSelect.value ? Number(detailCredentialSelect.value) : null,
                }),
            });
            showMessage(`已建立掃描任務 #${task.id}`);
            await Promise.all([loadAssets(), loadScans(), loadReports(), loadCredentials()]);
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
        await loadCatalog();
        await Promise.all([loadCredentials(), loadAssets(), loadScans(), loadReports()]);
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
        default_scan_profile: String(formData.get('default_scan_profile') || 'standard'),
        template_key: String(formData.get('template_key') || 'generic'),
        default_credential_id: formData.get('default_credential_id') ? Number(formData.get('default_credential_id')) : null,
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
        const isEditing = state.editingAssetId !== null;
        const asset = await apiRequest(isEditing ? `/assets/${state.editingAssetId}` : '/assets', {
            method: isEditing ? 'PATCH' : 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
        });
        resetAssetForm();
        state.selectedAssetId = asset.id;
        await Promise.all([loadCatalog(), loadCredentials(), loadAssets(), loadScans(), loadReports()]);
        await loadAssetDetail(asset.id);
        showMessage(isEditing ? '設備更新成功。' : '設備建立成功。');
    } catch (error) {
        showMessage(error.message);
    }
});

credentialForm.addEventListener('submit', async (event) => {
    event.preventDefault();
    if (!state.currentUser) {
        showMessage('請先登入後再建立 credential。');
        return;
    }

    const formData = new FormData(credentialForm);
    const payload = {
        name: String(formData.get('name') || ''),
        kind: String(formData.get('kind') || ''),
        username: String(formData.get('username') || '').trim() || null,
        domain: String(formData.get('domain') || '').trim() || null,
        port: formData.get('port') ? Number(formData.get('port')) : null,
        primary_secret: String(formData.get('primary_secret') || '').trim() || null,
        secondary_secret: String(formData.get('secondary_secret') || '').trim() || null,
        notes: String(formData.get('notes') || '').trim() || null,
        is_active: true,
    };

    try {
        await apiRequest('/credentials', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
        });
        credentialForm.reset();
        updateCredentialKindForm();
        await Promise.all([loadCatalog(), loadCredentials(), loadAssets()]);
        showMessage('Credential 建立成功。');
    } catch (error) {
        showMessage(error.message);
    }
});

refreshAllButton.addEventListener('click', () => {
    Promise.all([loadCatalog(), loadCredentials(), loadAssets(), loadScans(), loadReports()]).catch((error) => showMessage(error.message));
});

logoutButton.addEventListener('click', logout);
assetFormCancel.addEventListener('click', resetAssetForm);
assetSearch.addEventListener('input', renderAssets);
assetTypeFilter.addEventListener('change', renderAssets);
scanSearch.addEventListener('input', renderScans);
scanStatusFilter.addEventListener('change', renderScans);
navTabs.forEach((tab) => {
    tab.addEventListener('click', () => setActiveTab(tab.dataset.tab));
});

deviceTypeSelect.addEventListener('change', () => {
    const recommendedTemplate = deviceTypeToTemplate[deviceTypeSelect.value] || 'generic';
    assetTemplateSelect.value = recommendedTemplate;
    const matchedTemplate = state.catalog.templates.find((template) => template.key === recommendedTemplate);
    if (matchedTemplate) {
        assetProfileSelect.value = matchedTemplate.recommended_profile;
    }
});
credentialKindSelect.addEventListener('change', updateCredentialKindForm);

(async function bootstrap() {
    populateDeviceSelectors();
    if (!state.token) {
        return;
    }
    try {
        await fetchCurrentUser();
        await loadCatalog();
        await Promise.all([loadCredentials(), loadAssets(), loadScans(), loadReports()]);
        resetAssetForm();
    } catch (error) {
        logout();
    }
})();
