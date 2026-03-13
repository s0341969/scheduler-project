const state = {
    chart: {
        displayMode: "person",
        nodes: []
    },
    selectedId: null
};

const refs = {
    chartContainer: document.getElementById("chartContainer"),
    statusText: document.getElementById("statusText"),
    displayModeSelect: document.getElementById("displayModeSelect"),
    nodeForm: document.getElementById("nodeForm"),
    departmentNameInput: document.getElementById("departmentNameInput"),
    personNameInput: document.getElementById("personNameInput"),
    titleInput: document.getElementById("titleInput"),
    codeInput: document.getElementById("codeInput"),
    emailInput: document.getElementById("emailInput"),
    phoneInput: document.getElementById("phoneInput"),
    parentIdSelect: document.getElementById("parentIdSelect"),
    importFile: document.getElementById("importFile")
};

init();

async function init() {
    bindEvents();
    await loadChart();
}

function bindEvents() {
    document.getElementById("refreshBtn").addEventListener("click", () => loadChart());
    document.getElementById("addRootBtn").addEventListener("click", () => createNode(null));
    document.getElementById("addChildBtn").addEventListener("click", addChildNode);
    document.getElementById("moveUpBtn").addEventListener("click", () => moveSelected("up"));
    document.getElementById("moveDownBtn").addEventListener("click", () => moveSelected("down"));
    document.getElementById("deleteBtn").addEventListener("click", deleteSelected);
    document.getElementById("exportBtn").addEventListener("click", exportChart);
    document.getElementById("importBtn").addEventListener("click", () => refs.importFile.click());
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
    if (node.id === state.selectedId) {
        card.classList.add("selected");
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

        await apiRequest("/api/orgchart/import", {
            method: "POST",
            body: JSON.stringify(data)
        });

        state.selectedId = null;
        await loadChart();
        setStatus("匯入完成。", false);
    } catch (error) {
        setStatus(`匯入失敗：${error.message}`, true);
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

async function apiRequest(url, options = {}) {
    const headers = {
        "Content-Type": "application/json",
        ...(options.headers || {})
    };

    const response = await fetch(url, {
        ...options,
        headers
    });

    const hasBody = response.status !== 204;
    let payload = null;

    if (hasBody) {
        const text = await response.text();
        payload = text ? JSON.parse(text) : null;
    }

    if (!response.ok) {
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
