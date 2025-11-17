// Telegram Monitor - Keywords Management Script
// Deobfuscated version

const API = '/api/keyword';

// Type and action mappings
const typeMap = {
    FullWord: '全字',
    Contains: '包含',
    Regex: '正则',
    Fuzzy: '模糊',
    User: '用户'
};

const actionMap = {
    Exclude: '排除',
    Monitor: '监控'
};

const styleMap = {
    isCaseSensitive: '大小写',
    isBold: '粗体',
    isItalic: '斜体',
    isUnderline: '下划线',
    isStrikeThrough: '删除线',
    isQuote: '引用',
    isMonospace: '等宽',
    isSpoiler: '剧透'
};

// Toast notification system
const toastBox = document.getElementById('toast');
toastBox.style.zIndex = '9999';

function toast(message, isSuccess = true) {
    const alertEl = document.createElement('div');
    alertEl.setAttribute('role', 'alert');
    alertEl.className = `alert alert-${isSuccess ? 'success' : 'error'} alert-horizontal shadow-lg`;
    alertEl.innerHTML = `<span>${message}</span>`;
    toastBox.appendChild(alertEl);
    setTimeout(() => alertEl.remove(), 4000);
}

// API wrapper
async function api(url, options) {
    const response = await fetch(url, Object.assign({
        headers: { 'Content-Type': 'application/json' }
    }, options));

    const text = await response.text();
    let data = {};

    try {
        data = text ? JSON.parse(text) : {};
    } catch {}

    if (!response.ok || data.succeeded === false) {
        const errorMsg = data.errors && (typeof data.errors === 'string' ?
            data.errors : Object.values(data.errors)[0][0]) ||
            data.message || text || '操作失败';
        toast(errorMsg, false);
        throw new Error(errorMsg);
    }

    return data;
}

// DOM elements
const tbody = document.getElementById('kwBody');

// Utility functions
function styleString(keyword) {
    return Object.entries(styleMap)
        .filter(([key]) => keyword[key])
        .map(([, value]) => value)
        .join(' ');
}

function renderRow(keyword) {
    const tr = document.createElement('tr');
    tr.innerHTML = `
        <td><input type="checkbox" class="row-check" value="${keyword.id}"></td>
        <td>${keyword.id}</td>
        <td>${keyword.keywordContent}</td>
        <td>${typeMap[keyword.keywordType] ?? keyword.keywordType}</td>
        <td>${actionMap[keyword.keywordAction] ?? keyword.keywordAction}</td>
        <td>${styleString(keyword)}</td>
        <td>
            <button class="btn" onclick='openEdit(${JSON.stringify(keyword)})'>编辑</button>
            <button class="btn btn-error ml-1" onclick="del(${keyword.id})">删</button>
        </td>
    `;
    return tr;
}

// Keyword list operations
async function refresh() {
    const { data: keywords } = await api(`${API}/list`);
    tbody.innerHTML = '';
    keywords.forEach(keyword => tbody.appendChild(renderRow(keyword)));
}

async function del(id) {
    await api(`${API}/delete/${id}`, { method: 'DELETE' });
    toast('删除成功');
    refresh();
}

function toggleAll(checkbox) {
    document.querySelectorAll('.row-check').forEach(el => el.checked = checkbox.checked);
}

async function deleteSelected() {
    const ids = [...document.querySelectorAll('.row-check')]
        .filter(el => el.checked)
        .map(el => +el.value);

    if (!ids.length) {
        toast('未选中', false);
        return;
    }

    await api(`${API}/batchdelete`, {
        method: 'DELETE',
        body: JSON.stringify(ids)
    });

    toast('批量删除成功');
    refresh();
}

// Single keyword add
async function addSingle() {
    const keyword = {
        keywordContent: sContent.value.trim(),
        keywordType: +sType.value,
        keywordAction: +sAction.value,
        isCaseSensitive: sCase.checked,
        isBold: sBold.checked,
        isItalic: sItalic.checked,
        isUnderline: sUnder.checked,
        isStrikeThrough: sStrike.checked,
        isQuote: sQuote.checked,
        isMonospace: sMono.checked,
        isSpoiler: sSpoil.checked
    };

    if (!keyword.keywordContent) {
        toast('内容不能为空', false);
        return;
    }

    await api(`${API}/add`, {
        method: 'POST',
        body: JSON.stringify(keyword)
    });

    toast('添加成功');
    refresh();
}

// Dynamic row batch add
const dynamicRows = document.getElementById('dynamicRows');

function rowTpl(id) {
    return `<div class="flex flex-wrap gap-2 items-center border p-2 rounded" id="row-${id}">
        <input class="input input-bordered w-40" placeholder="关键词">
        <select class="select select-bordered">
            <option value="0">全字</option>
            <option value="1">包含</option>
            <option value="2">正则</option>
            <option value="3">模糊</option>
            <option value="4">用户</option>
        </select>
        <select class="select select-bordered">
            <option value="1">监控</option>
            <option value="0">排除</option>
        </select>
        ${Object.entries(styleMap).map(([key, label]) => `
            <label class="label gap-1 text-xs">
                <span>${label}</span>
                <input type="checkbox" data-flag="${key}" class="checkbox">
            </label>
        `).join('')}
        <button class="btn btn-error" onclick="this.parentNode.remove()">x</button>
    </div>`;
}

function addRow() {
    dynamicRows.insertAdjacentHTML('beforeend', rowTpl(Date.now()));
}

async function uploadRows() {
    const keywords = [...dynamicRows.children].map(row => {
        const input = row.querySelector('input.input');
        if (!input || !input.value.trim()) return null;

        const [typeSelect, actionSelect] = row.querySelectorAll('select');
        const keyword = {
            keywordContent: input.value.trim(),
            keywordType: +typeSelect.value,
            keywordAction: +actionSelect.value
        };

        row.querySelectorAll('input[data-flag]').forEach(el =>
            keyword[el.dataset.flag] = el.checked
        );

        return keyword;
    }).filter(Boolean);

    if (!keywords.length) {
        toast('无有效行', false);
        return;
    }

    await api(`${API}/batchadd`, {
        method: 'POST',
        body: JSON.stringify(keywords)
    });

    toast('批量添加成功');
    refresh();
}

// Text batch add
async function uploadText() {
    const lines = txtKeywords.value.split('\n')
        .map(line => line.trim())
        .filter(Boolean);

    if (!lines.length) {
        toast('文本为空', false);
        return;
    }

    const styleOptions = {
        isCaseSensitive: tCase.checked,
        isBold: tBold.checked,
        isItalic: tItalic.checked,
        isUnderline: tUnder.checked,
        isStrikeThrough: tStrike.checked,
        isQuote: tQuote.checked,
        isMonospace: tMono.checked,
        isSpoiler: tSpoil.checked
    };

    const keywords = lines.map(line => ({
        keywordContent: line,
        keywordType: +tType.value,
        keywordAction: +tAction.value,
        ...styleOptions
    }));

    await api(`${API}/batchadd`, {
        method: 'POST',
        body: JSON.stringify(keywords)
    });

    toast('批量添加成功');
    refresh();
}

// Edit functionality
function fillEdit(keyword) {
    eId.value = keyword.id;
    eContent.value = keyword.keywordContent;

    const typeKeys = Object.keys(typeMap);
    const actionKeys = Object.keys(actionMap);

    eType.value = typeKeys.indexOf(keyword.keywordType);
    eAction.value = actionKeys.indexOf(keyword.keywordAction);

    eCase.checked = keyword.isCaseSensitive;
    eBold.checked = keyword.isBold;
    eItalic.checked = keyword.isItalic;
    eUnder.checked = keyword.isUnderline;
    eStrike.checked = keyword.isStrikeThrough;
    eQuote.checked = keyword.isQuote;
    eMono.checked = keyword.isMonospace;
    eSpoil.checked = keyword.isSpoiler;
}

function openEdit(keyword) {
    fillEdit(keyword);
    editModal.showModal();
}

async function saveEdit() {
    const keyword = {
        id: +eId.value,
        keywordContent: eContent.value.trim(),
        keywordType: +eType.value,
        keywordAction: +eAction.value,
        isCaseSensitive: eCase.checked,
        isBold: eBold.checked,
        isItalic: eItalic.checked,
        isUnderline: eUnder.checked,
        isStrikeThrough: eStrike.checked,
        isQuote: eQuote.checked,
        isMonospace: eMono.checked,
        isSpoiler: eSpoil.checked
    };

    await api(`${API}/update`, {
        method: 'PUT',
        body: JSON.stringify(keyword)
    });

    toast('修改成功');
    refresh();
}

// Tab switching
document.querySelectorAll('[role=tab]').forEach(tab => {
    tab.onclick = () => {
        document.querySelectorAll('[role=tab]').forEach(t =>
            t.classList.remove('tab-active')
        );
        tab.classList.add('tab-active');

        ['list', 'single', 'batch', 'text'].forEach(panelName => {
            document.getElementById('panel-' + panelName)
                .classList.toggle('hidden', !tab.id.endsWith(panelName));
        });
    };
});

// Copyright banner
function addCopyright() {
    const banner = document.createElement('div');
    banner.style.cssText = 'position:fixed;top:0;left:0;width:100%;background-color:#f0f0f0;padding:10px;text-align:center;z-index:1000;box-shadow:0 2px 4px rgba(0,0,0,0.1);';
    banner.innerHTML = `
        <span style="margin-right:15px;">作者 <a href="https://t.me/riniba" target="_blank" style="text-decoration:none;color:#0088cc;font-weight:bold;">@riniba</a></span>
        <span style="margin-right:15px;">开源地址 <a href="https://github.com/Riniba/TelegramMonitor" target="_blank" style="text-decoration:none;color:#0088cc;font-weight:bold;">GitHub</a></span>
        <span style="margin-right:15px;">交流群 <a href="https://t.me/RinibaGroup" target="_blank" style="text-decoration:none;color:#0088cc;font-weight:bold;">Telegram</a></span>
        <span><a href="https://github.com/Riniba/TelegramMonitor/wiki/%E5%85%B3%E9%94%AE%E8%AF%8D%E4%BD%BF%E7%94%A8%E6%95%99%E7%A8%8B" target="_blank" style="text-decoration:none;color:#0088cc;font-weight:bold;">关键词配置说明</a></span>
    `;
    document.body.insertBefore(banner, document.body.firstChild);
    document.body.style.paddingTop = banner.offsetHeight + 10 + 'px';
}

// Initialize
document.addEventListener('DOMContentLoaded', addCopyright);
refresh();
