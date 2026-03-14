const state = {
    chart: {
        displayMode: "person",
        nodes: []
    },
    selectedId: null,
    draggingId: null,
    dragOver: null,
    searchMatchedIds: new Set(),
    apiBase: null,
    apiBaseCandidates: [],
    auth: {
        role: "viewer",
        key: "",
        actor: "anonymous"
    }
};

const refs = {
    chartContainer: document.getElementById("chartContainer"),
    statusText: document.getElementById("statusText"),
    apiInfo: document.getElementById("apiInfo"),
    displayModeSelect: document.getElementById("displayModeSelect"),
    nodeForm: document.getElementById("nodeForm"),
    departmentNameInput: document.getElementById("departmentNameInput"),
    personNameInput: document.getElementById("personNameInput"),
    titleInput: document.getElementById("titleInput"),
    codeInput: document.getElementById("codeInput"),
    emailInput: document.getElementById("emailInput"),
    phoneInput: document.getElementById("phoneInput"),
    parentIdSelect: document.getElementById("parentIdSelect"),
    importFile: document.getElementById("importFile"),
    searchInput: document.getElementById("searchInput"),
    snapshotSelect: document.getElementById("snapshotSelect"),
    backupSelect: document.getElementById("backupSelect"),
    roleSelect: document.getElementById("roleSelect"),
    accessKeyInput: document.getElementById("accessKeyInput"),
    actorInput: document.getElementById("actorInput")
};

init();

async function init() {
    state.apiBaseCandidates = buildApiBaseCandidates();
    loadAuthSettings();
    bindEvents();

    await ensureApiBase();
    await Promise.all([loadChart(), loadSnapshots(), loadBackups()]);
}

function bindEvents() {
    document.getElementById("refreshBtn").addEventListener("click", async () => {
        await Promise.all([loadChart(), loadSnapshots(), loadBackups()]);
    });

    document.getElementById("addRootBtn").addEventListener("click", () => createNode(null));
    document.getElementById("addChildBtn").addEventListener("click", addChildNode);
    document.getElementById("moveUpBtn").addEventListener("click", () => moveSelected("up"));
    document.getElementById("moveDownBtn").addEventListener("click", () => moveSelected("down"));
    document.getElementById("deleteBtn").addEventListener("click", deleteSelected);
    document.getElementById("exportBtn").addEventListener("click", exportChart);
    document.getElementById("importBtn").addEventListener("click", () => refs.importFile.click());
    document.getElementById("searchBtn").addEventListener("click", runSearch);
    document.getElementById("clearSearchBtn").addEventListener("click", clearSearch);
    document.getElementById("createSnapshotBtn").addEventListener("click", createSnapshot);
    document.getElementById("restoreSnapshotBtn").addEventListener("click", restoreSnapshot);
    document.getElementById("createBackupBtn").addEventListener("click", createBackup);
    document.getElementById("restoreBackupBtn").addEventListener("click", restoreBackup);
    document.getElementById("saveAuthBtn").addEventListener("click", saveAuthSettings);

    document.getElementById("clearSelectionBtn").addEventListener("click", () => {
        state.selectedId = null;
        renderAll();
        setStatus("已取消選取節點。");
    });

    refs.displayModeSelect.addEventListener("change", updateDisplayMode);
    refs.nodeForm.addEventListener("submit", saveSelectedNode);

    refs.importFile.addEventListener("change", async (event) => {
        const file = event.target.files?.[0];
        if (!file) {
            return;
        }

        try {
            await importChart(file);
        } finally {
            refs.importFile.value = "";
        }
    });

    refs.searchInput.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            event.preventDefault();
            runSearch();
        }
    });

    refs.chartContainer.addEventListener("click", (event) => {
        const target = event.target;
        if (!(target instanceof HTMLElement)) {
            return;
        }

        const card = target.closest(".node-card");
        if (!card) {
            return;
        }

        const id = Number(card.dataset.nodeId);
        if (!Number.isFinite(id)) {
            return;
        }

        state.selectedId = id;
        renderAll();
        setStatus(`已選取節點 #${id}`);
    });

    refs.chartContainer.addEventListener("dragstart", handleDragStart);
    refs.chartContainer.addEventListener("dragover", handleDragOver);
    refs.chartContainer.addEventListener("dragleave", handleDragLeave);
    refs.chartContainer.addEventListener("drop", handleDrop);
    refs.chartContainer.addEventListener("dragend", clearDragState);
}

function loadAuthSettings() {
    const stored = window.localStorage.getItem("orgChartAuth");
    if (stored) {
        try {
            const parsed = JSON.parse(stored);
            state.auth.role = parsed.role || "viewer";
            state.auth.key = parsed.key || "";
            state.auth.actor = parsed.actor || "anonymous";
        } catch {
        }
    }

    refs.roleSelect.value = state.auth.role;
    refs.accessKeyInput.value = state.auth.key;
    refs.actorInput.value = state.auth.actor;
}

function saveAuthSettings() {
    state.auth.role = refs.roleSelect.value || "viewer";
    state.auth.key = refs.accessKeyInput.value || "";
    state.auth.actor = refs.actorInput.value?.trim() || "anonymous";

    window.localStorage.setItem("orgChartAuth", JSON.stringify(state.auth));
    setStatus(`已套用權限：${state.auth.role} / ${state.auth.actor}`);
}

async function loadChart() {
    setStatus("載入中...");

    try {
        const data = await apiRequest("/api/orgchart");
        state.chart = data;

        const all = flattenNodes(state.chart.nodes);
        if (!all.some((x) => x.id === state.selectedId)) {
            state.selectedId = all.length > 0 ? all[0].id : null;
        }

        refs.displayModeSelect.value = state.chart.displayMode || "person";
        renderAll();

        const total = all.length;
        setStatus(`已載入 ${total} 個節點。`);
    } catch (error) {
        setStatus(`載入失敗：${error.message}`, true);
    }
}

function renderAll() {
    renderTree();
    renderEditor();
}

function renderTree() {
    refs.chartContainer.innerHTML = "";

    if (!state.chart.nodes || state.chart.nodes.length === 0) {
        const empty = document.createElement("p");
        empty.className = "tree-empty";
        empty.textContent = "目前沒有節點。請先點選「新增根節點」。";
        refs.chartContainer.appendChild(empty);
        return;
    }

    const tree = document.createElement("div");
    tree.className = "tree";

    const rootList = document.createElement("ul");
    state.chart.nodes.forEach((node) => {
        rootList.appendChild(buildNodeTree(node));
    });

    tree.appendChild(rootList);
    refs.chartContainer.appendChild(tree);
}

function buildNodeTree(node) {
    const item = document.createElement("li");

    const card = document.createElement("button");
    card.type = "button";
    card.className = "node-card";
    card.draggable = true;
    if (node.id === state.selectedId) {
        card.classList.add("selected");
    }

    if (state.searchMatchedIds.has(node.id)) {
        card.classList.add("search-hit");
    }

    card.dataset.nodeId = String(node.id);

    const mode = state.chart.displayMode || "person";
    const mainText = mode === "code"
        ? valueOrPlaceholder(node.code, "未填代碼")
        : valueOrPlaceholder(node.personName, "未填姓名");

    const subLineOne = mode === "code"
        ? valueOrPlaceholder(node.personName, "未填姓名")
        : valueOrPlaceholder(node.departmentName, "未填部門");

    const subLineTwo = valueOrPlaceholder(node.title, "未填職稱");

    card.innerHTML = `
        <div class="node-main">${escapeHtml(mainText)}</div>
        <div class="node-sub">${escapeHtml(subLineOne)}<br>${escapeHtml(subLineTwo)}</div>
    `;

    item.appendChild(card);

    if (node.children && node.children.length > 0) {
        const childrenList = document.createElement("ul");
        node.children.forEach((child) => {
            childrenList.appendChild(buildNodeTree(child));
        });
        item.appendChild(childrenList);
    }

    return item;
}

function renderEditor() {
    const selected = getSelectedNode();

    const descendants = selected ? getDescendantIdSet(selected) : new Set();
    populateParentOptions(selected, descendants);

    if (!selected) {
        refs.departmentNameInput.value = "";
        refs.personNameInput.value = "";
        refs.titleInput.value = "";
        refs.codeInput.value = "";
        refs.emailInput.value = "";
        refs.phoneInput.value = "";
        refs.parentIdSelect.value = "";
        return;
    }

    refs.departmentNameInput.value = selected.departmentName || "";
    refs.personNameInput.value = selected.personName || "";
    refs.titleInput.value = selected.title || "";
    refs.codeInput.value = selected.code || "";
    refs.emailInput.value = selected.email || "";
    refs.phoneInput.value = selected.phone || "";
    refs.parentIdSelect.value = selected.parentId === null ? "" : String(selected.parentId);
}

function populateParentOptions(selected, descendants) {
    refs.parentIdSelect.innerHTML = "";

    const emptyOption = document.createElement("option");
    emptyOption.value = "";
    emptyOption.textContent = "(無上層節點)";
    refs.parentIdSelect.appendChild(emptyOption);

    flattenNodes(state.chart.nodes)
        .filter((node) => !selected || (node.id !== selected.id && !descendants.has(node.id)))
        .forEach((node) => {
            const option = document.createElement("option");
            option.value = String(node.id);
            option.textContent = `#${node.id} ${valueOrPlaceholder(node.departmentName, "未填部門")} / ${valueOrPlaceholder(node.personName, "未填姓名")}`;
            refs.parentIdSelect.appendChild(option);
        });
}

async function createNode(parentId) {
    try {
        const created = await apiRequest("/api/orgchart/nodes", {
            method: "POST",
            body: JSON.stringify({
                parentId,
                departmentName: "新部門",
                personName: "",
                title: "",
                code: "",
                email: "",
                phone: ""
            })
        });

        state.selectedId = created.id;
        await loadChart();
        setStatus(`已新增節點 #${created.id}`);
    } catch (error) {
        setStatus(`新增失敗：${error.message}`, true);
    }
}

function addChildNode() {
    if (!state.selectedId) {
        setStatus("請先選取父節點，再新增子節點。", true);
        return;
    }

    createNode(state.selectedId);
}

async function saveSelectedNode(event) {
    event.preventDefault();

    if (!state.selectedId) {
        setStatus("請先在左側選取要儲存的節點。", true);
        return;
    }

    const parentValue = refs.parentIdSelect.value;
    const payload = {
        parentId: parentValue ? Number(parentValue) : null,
        departmentName: refs.departmentNameInput.value,
        personName: refs.personNameInput.value,
        title: refs.titleInput.value,
        code: refs.codeInput.value,
        email: refs.emailInput.value,
        phone: refs.phoneInput.value
    };

    try {
        await apiRequest(`/api/orgchart/nodes/${state.selectedId}`, {
            method: "PUT",
            body: JSON.stringify(payload)
        });

        await loadChart();
        setStatus(`節點 #${state.selectedId} 已儲存。`);
    } catch (error) {
        setStatus(`儲存失敗：${error.message}`, true);
    }
}

async function moveSelected(direction) {
    if (!state.selectedId) {
        setStatus("請先選取節點再執行排序。", true);
        return;
    }

    try {
        await apiRequest(`/api/orgchart/nodes/${state.selectedId}/move`, {
            method: "POST",
            body: JSON.stringify({ direction })
        });

        await loadChart();
        setStatus(`節點 #${state.selectedId} 已${direction === "up" ? "上移" : "下移"}。`);
    } catch (error) {
        setStatus(`排序失敗：${error.message}`, true);
    }
}

function handleDragStart(event) {
    const target = event.target;
    if (!(target instanceof HTMLElement)) {
        return;
    }

    const card = target.closest(".node-card");
    if (!(card instanceof HTMLElement)) {
        return;
    }

    const id = Number(card.dataset.nodeId);
    if (!Number.isFinite(id)) {
        return;
    }

    state.draggingId = id;
    card.classList.add("dragging");

    if (event.dataTransfer) {
        event.dataTransfer.effectAllowed = "move";
        event.dataTransfer.setData("text/plain", String(id));
    }
}

function handleDragOver(event) {
    if (!Number.isFinite(state.draggingId)) {
        return;
    }

    event.preventDefault();

    const target = event.target;
    if (!(target instanceof HTMLElement)) {
        return;
    }

    const card = target.closest(".node-card");
    if (!(card instanceof HTMLElement)) {
        setDragHint(null, null);
        refs.chartContainer.classList.add("drag-over-root");
        return;
    }

    refs.chartContainer.classList.remove("drag-over-root");

    const targetId = Number(card.dataset.nodeId);
    if (!Number.isFinite(targetId)) {
        setDragHint(null, null);
        return;
    }

    const position = getDropPosition(card, event.clientY);
    if (!isValidDrop(state.draggingId, targetId, position)) {
        setDragHint(null, null);
        return;
    }

    setDragHint(targetId, position);
}

function handleDragLeave(event) {
    const target = event.target;
    if (!(target instanceof HTMLElement)) {
        return;
    }

    if (target === refs.chartContainer) {
        refs.chartContainer.classList.remove("drag-over-root");
    }
}

async function handleDrop(event) {
    if (!Number.isFinite(state.draggingId)) {
        return;
    }

    event.preventDefault();

    const target = event.target;
    if (!(target instanceof HTMLElement)) {
        clearDragState();
        return;
    }

    const draggingId = state.draggingId;
    const card = target.closest(".node-card");

    try {
        if (!(card instanceof HTMLElement)) {
            await repositionNode(draggingId, null, state.chart.nodes.length);
            state.selectedId = draggingId;
            await loadChart();
            setStatus(`節點 #${draggingId} 已移到根節點層。`);
            return;
        }

        const targetId = Number(card.dataset.nodeId);
        if (!Number.isFinite(targetId)) {
            return;
        }

        const position = state.dragOver?.targetId === targetId
            ? state.dragOver.position
            : getDropPosition(card, event.clientY);

        if (!isValidDrop(draggingId, targetId, position)) {
            setStatus("拖放位置無效，請換一個位置。", true);
            return;
        }

        const targetNode = flattenNodes(state.chart.nodes).find((x) => x.id === targetId);
        if (!targetNode) {
            return;
        }

        let parentId = null;
        let index = 0;

        if (position === "inside") {
            parentId = targetNode.id;
            index = targetNode.children?.length || 0;
        } else if (position === "before") {
            parentId = targetNode.parentId;
            index = targetNode.sortOrder;
        } else {
            parentId = targetNode.parentId;
            index = targetNode.sortOrder + 1;
        }

        await repositionNode(draggingId, parentId, index);
        state.selectedId = draggingId;
        await loadChart();
        setStatus(`節點 #${draggingId} 已完成拖放。`);
    } catch (error) {
        setStatus(`拖放失敗：${error.message}`, true);
    } finally {
        clearDragState();
    }
}

function getDropPosition(card, clientY) {
    const rect = card.getBoundingClientRect();
    const topZone = rect.top + rect.height * 0.25;
    const bottomZone = rect.bottom - rect.height * 0.25;

    if (clientY <= topZone) {
        return "before";
    }

    if (clientY >= bottomZone) {
        return "after";
    }

    return "inside";
}

function isValidDrop(draggingId, targetId, position) {
    if (!Number.isFinite(draggingId) || !Number.isFinite(targetId)) {
        return false;
    }

    if (draggingId === targetId) {
        return false;
    }

    const flatNodes = flattenNodes(state.chart.nodes);
    const draggingNode = flatNodes.find((x) => x.id === draggingId);
    if (!draggingNode) {
        return false;
    }

    const descendants = getDescendantIdSet(draggingNode);
    if (position === "inside" && descendants.has(targetId)) {
        return false;
    }

    const targetNode = flatNodes.find((x) => x.id === targetId);
    if (!targetNode) {
        return false;
    }

    if ((position === "before" || position === "after") && targetNode.parentId !== null && descendants.has(targetNode.parentId)) {
        return false;
    }

    return true;
}

function setDragHint(targetId, position) {
    state.dragOver = targetId === null ? null : { targetId, position };

    refs.chartContainer
        .querySelectorAll(".node-card.drag-over-before, .node-card.drag-over-inside, .node-card.drag-over-after")
        .forEach((card) => card.classList.remove("drag-over-before", "drag-over-inside", "drag-over-after"));

    if (targetId === null) {
        return;
    }

    const targetCard = refs.chartContainer.querySelector(`.node-card[data-node-id="${targetId}"]`);
    if (!(targetCard instanceof HTMLElement)) {
        return;
    }

    targetCard.classList.add(`drag-over-${position}`);
}

function clearDragState() {
    state.draggingId = null;
    state.dragOver = null;
    refs.chartContainer.classList.remove("drag-over-root");
    refs.chartContainer
        .querySelectorAll(".node-card.dragging, .node-card.drag-over-before, .node-card.drag-over-inside, .node-card.drag-over-after")
        .forEach((card) => card.classList.remove("dragging", "drag-over-before", "drag-over-inside", "drag-over-after"));
}

async function repositionNode(nodeId, parentId, index) {
    await apiRequest(`/api/orgchart/nodes/${nodeId}/reposition`, {
        method: "POST",
        body: JSON.stringify({
            parentId,
            index
        })
    });
}

async function deleteSelected() {
    if (!state.selectedId) {
        setStatus("請先選取要刪除的節點。", true);
        return;
    }

    const id = state.selectedId;
    const confirmed = window.confirm("刪除節點會連同所有子節點一起移除，確定要繼續嗎？");
    if (!confirmed) {
        return;
    }

    try {
        await apiRequest(`/api/orgchart/nodes/${id}`, {
            method: "DELETE"
        });

        state.selectedId = null;
        await loadChart();
        setStatus(`節點 #${id} 已刪除。`);
    } catch (error) {
        setStatus(`刪除失敗：${error.message}`, true);
    }
}

async function updateDisplayMode() {
    try {
        await apiRequest("/api/orgchart/settings", {
            method: "PUT",
            body: JSON.stringify({ displayMode: refs.displayModeSelect.value })
        });

        await loadChart();
        setStatus("顯示模式已更新。", false);
    } catch (error) {
        setStatus(`更新顯示模式失敗：${error.message}`, true);
    }
}

async function exportChart() {
    try {
        const data = await apiRequest("/api/orgchart/export");
        const now = new Date();
        const stamp = `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, "0")}${String(now.getDate()).padStart(2, "0")}_${String(now.getHours()).padStart(2, "0")}${String(now.getMinutes()).padStart(2, "0")}${String(now.getSeconds()).padStart(2, "0")}`;
        const fileName = `org-chart-${stamp}.json`;

        const blob = new Blob([JSON.stringify(data, null, 2)], { type: "application/json" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(url);

        setStatus(`已匯出 ${fileName}`);
    } catch (error) {
        setStatus(`匯出失敗：${error.message}`, true);
    }
}

async function importChart(file) {
    try {
        const text = await file.text();
        const data = JSON.parse(text);

        if (!data || !Array.isArray(data.nodes)) {
            throw new Error("JSON 格式不正確，必須包含 nodes 陣列。");
        }

        const preview = await apiRequest("/api/orgchart/import/preview", {
            method: "POST",
            body: JSON.stringify(data)
        });

        const warningText = preview.warnings?.length > 0
            ? `\n\n警示：\n- ${preview.warnings.join("\n- ")}`
            : "";

        const confirmText = `預覽結果：\n目前節點：${preview.currentNodeCount}\n匯入節點：${preview.incomingNodeCount}\n差異：${preview.delta >= 0 ? "+" : ""}${preview.delta}\n根節點：${preview.rootCount}\n最大深度：${preview.maxDepth}${warningText}\n\n確定要套用匯入？`;

        if (!window.confirm(confirmText)) {
            setStatus("已取消匯入。", false);
            return;
        }

        await apiRequest("/api/orgchart/import", {
            method: "POST",
            body: JSON.stringify(data)
        });

        state.selectedId = null;
        await Promise.all([loadChart(), loadSnapshots()]);
        setStatus("匯入完成。", false);
    } catch (error) {
        setStatus(`匯入失敗：${error.message}`, true);
    }
}

async function runSearch() {
    const keyword = refs.searchInput.value?.trim() || "";
    if (!keyword) {
        setStatus("請先輸入搜尋關鍵字。", true);
        return;
    }

    try {
        const results = await apiRequest(`/api/orgchart/search?q=${encodeURIComponent(keyword)}&limit=100`);
        state.searchMatchedIds = new Set(results.map((x) => x.id));

        if (results.length > 0) {
            state.selectedId = results[0].id;
            renderAll();
            setStatus(`搜尋到 ${results.length} 筆，已定位到第一筆。`);
        } else {
            renderAll();
            setStatus("查無符合節點。", false);
        }
    } catch (error) {
        setStatus(`搜尋失敗：${error.message}`, true);
    }
}

function clearSearch() {
    state.searchMatchedIds = new Set();
    refs.searchInput.value = "";
    renderAll();
    setStatus("已清除搜尋結果。");
}

async function loadSnapshots() {
    try {
        const snapshots = await apiRequest("/api/orgchart/snapshots?take=50");
        refs.snapshotSelect.innerHTML = "";

        const empty = document.createElement("option");
        empty.value = "";
        empty.textContent = snapshots.length === 0 ? "(無快照)" : "請選擇快照";
        refs.snapshotSelect.appendChild(empty);

        snapshots.forEach((item) => {
            const option = document.createElement("option");
            option.value = String(item.id);
            const date = formatUtc(item.createdAtUtc);
            option.textContent = `#${item.id} ${date} ${item.reason}`;
            refs.snapshotSelect.appendChild(option);
        });
    } catch {
    }
}

async function createSnapshot() {
    const reason = window.prompt("請輸入快照原因", "手動快照") || "手動快照";

    try {
        await apiRequest("/api/orgchart/snapshots", {
            method: "POST",
            body: JSON.stringify({ reason })
        });

        await loadSnapshots();
        setStatus("已建立快照。", false);
    } catch (error) {
        setStatus(`建立快照失敗：${error.message}`, true);
    }
}

async function restoreSnapshot() {
    const id = Number(refs.snapshotSelect.value);
    if (!Number.isFinite(id)) {
        setStatus("請先選擇要回復的快照。", true);
        return;
    }

    if (!window.confirm(`確定要回復快照 #${id} 嗎？系統會先自動建立一份回復前快照。`)) {
        return;
    }

    try {
        await apiRequest(`/api/orgchart/snapshots/${id}/restore`, {
            method: "POST"
        });

        await Promise.all([loadChart(), loadSnapshots()]);
        setStatus(`快照 #${id} 已回復。`, false);
    } catch (error) {
        setStatus(`快照回復失敗：${error.message}`, true);
    }
}

async function loadBackups() {
    try {
        const backups = await apiRequest("/api/orgchart/backups");
        refs.backupSelect.innerHTML = "";

        const empty = document.createElement("option");
        empty.value = "";
        empty.textContent = backups.length === 0 ? "(無備份)" : "請選擇備份檔";
        refs.backupSelect.appendChild(empty);

        backups.forEach((item) => {
            const option = document.createElement("option");
            option.value = item.fileName;
            option.textContent = `${formatUtc(item.lastWriteTimeUtc)} ${item.fileName}`;
            refs.backupSelect.appendChild(option);
        });
    } catch {
    }
}

async function createBackup() {
    try {
        const backup = await apiRequest("/api/orgchart/backups", {
            method: "POST"
        });

        await loadBackups();
        setStatus(`已建立備份：${backup.fileName}`, false);
    } catch (error) {
        setStatus(`建立備份失敗：${error.message}`, true);
    }
}

async function restoreBackup() {
    const fileName = refs.backupSelect.value;
    if (!fileName) {
        setStatus("請先選擇要還原的備份檔。", true);
        return;
    }

    if (!window.confirm(`確定要還原備份 ${fileName} 嗎？系統會先做還原前備份。`)) {
        return;
    }

    try {
        await apiRequest("/api/orgchart/backups/restore", {
            method: "POST",
            body: JSON.stringify({ fileName })
        });

        await Promise.all([loadChart(), loadBackups(), loadSnapshots()]);
        setStatus("備份還原完成。", false);
    } catch (error) {
        setStatus(`備份還原失敗：${error.message}`, true);
    }
}

function getSelectedNode() {
    if (!state.selectedId) {
        return null;
    }

    return flattenNodes(state.chart.nodes).find((node) => node.id === state.selectedId) || null;
}

function flattenNodes(nodes, buffer = []) {
    nodes.forEach((node) => {
        buffer.push(node);
        if (node.children && node.children.length > 0) {
            flattenNodes(node.children, buffer);
        }
    });

    return buffer;
}

function getDescendantIdSet(rootNode) {
    const set = new Set();

    const visit = (node) => {
        if (!node.children) {
            return;
        }

        node.children.forEach((child) => {
            set.add(child.id);
            visit(child);
        });
    };

    visit(rootNode);
    return set;
}

function valueOrPlaceholder(value, fallback) {
    const normalized = typeof value === "string" ? value.trim() : "";
    return normalized || fallback;
}

function setStatus(message, isError = false) {
    refs.statusText.textContent = message;
    refs.statusText.style.color = isError ? "#b63f3f" : "#5d6d84";
}

function formatUtc(value) {
    if (!value) {
        return "";
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return String(value);
    }

    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")} ${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;
}

function buildApiBaseCandidates() {
    const params = new URLSearchParams(window.location.search);
    const fromQuery = params.get("apiBase");
    const fromStorage = window.localStorage.getItem("orgChartApiBase");

    const candidates = [];

    addCandidate(candidates, fromQuery);
    addCandidate(candidates, fromStorage);

    if (window.location.protocol !== "file:") {
        addCandidate(candidates, window.location.origin);
    }

    addCandidate(candidates, "http://localhost:5081");
    addCandidate(candidates, "https://localhost:7126");
    addCandidate(candidates, "http://localhost:5000");

    return candidates;
}

function addCandidate(target, value) {
    if (!value || typeof value !== "string") {
        return;
    }

    const normalized = value.endsWith("/")
        ? value.slice(0, -1)
        : value;

    if (!target.includes(normalized)) {
        target.push(normalized);
    }
}

async function ensureApiBase() {
    if (state.apiBase) {
        return state.apiBase;
    }

    for (const base of state.apiBaseCandidates) {
        try {
            const response = await fetch(`${base}/api/orgchart`, {
                method: "GET",
                cache: "no-store"
            });

            if (response.ok || response.status === 401) {
                state.apiBase = base;
                window.localStorage.setItem("orgChartApiBase", base);
                setApiInfo(`API 位址：${base}`);
                return base;
            }
        } catch {
        }
    }

    const fallback = state.apiBaseCandidates[0] || "http://localhost:5081";
    state.apiBase = fallback;
    setApiInfo(`API 連線失敗，嘗試位址：${fallback}`);
    return fallback;
}

function setApiInfo(message) {
    if (refs.apiInfo) {
        refs.apiInfo.textContent = message;
    }
}

async function apiRequest(path, options = {}) {
    const base = await ensureApiBase();
    const url = path.startsWith("http://") || path.startsWith("https://")
        ? path
        : `${base}${path}`;

    const headers = {
        ...(options.headers || {}),
        "X-OrgChart-Role": state.auth.role,
        "X-OrgChart-Key": state.auth.key,
        "X-OrgChart-Actor": state.auth.actor
    };

    if (options.body !== undefined && options.body !== null && !headers["Content-Type"]) {
        headers["Content-Type"] = "application/json";
    }

    let response;

    try {
        response = await fetch(url, {
            ...options,
            headers
        });
    } catch {
        throw new Error(`無法連線 API。請先執行 dotnet run，或用 ?apiBase=http://localhost:5081 指定位址（目前：${url}）`);
    }

    const hasBody = response.status !== 204;
    let payload = null;

    if (hasBody) {
        const text = await response.text();
        if (text) {
            const contentType = response.headers.get("content-type") || "";
            if (contentType.toLowerCase().includes("application/json")) {
                payload = JSON.parse(text);
            } else {
                payload = { message: text };
            }
        }
    }

    if (!response.ok) {
        if (response.status === 401) {
            throw new Error("權限不足或金鑰錯誤，請先在上方設定角色與金鑰。");
        }

        const message = payload?.message || `請求失敗（${response.status}）`;
        throw new Error(message);
    }

    return payload;
}

function escapeHtml(value) {
    return value
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
