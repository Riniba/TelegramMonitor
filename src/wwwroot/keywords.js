// API路径和映射常量
const API = '/api/keyword';

// 关键词类型映射
const typeMap = {
    FullWord: '全字',
    Contains: '包含', 
    Regex: '正则',
    Fuzzy: '模糊',
    User: '用户'
};

// 关键词动作映射
const actionMap = {
    Exclude: '排除',
    Monitor: '监控'
};

// 样式映射
const styleMap = {
    isCaseSensitive: '大小写',
    isBold: '粗体',
    isItalic: '斜体',
    isUnderline: '下划线',
    isStrikeThrough: '删除线',
    isQuote: '引用',
    isMonospace: '等宽',
    isSpoiler: '剧透'
};// Toast 通知功能
const toastBox = document.getElementById('toast');
toastBox.style.zIndex = '9999';

/**
 * 显示Toast通知
 * @param {string} message - 通知消息
 * @param {boolean} isSuccess - 是否为成功消息 (默认true)
 */
function toast(message, isSuccess = true) {
    const toastElement = document.createElement('div');
    toastElement.setAttribute('role', 'alert');
    toastElement.className = `alert alert-${isSuccess ? 'success' : 'error'} alert-horizontal shadow-lg`;
    toastElement.innerHTML = `<span>${message}</span>`;
    
    toastBox.appendChild(toastElement);
    
    // 4秒后自动移除
    setTimeout(() => toastElement.remove(), 4000);
}/**
 * API请求函数
 * @param {string} url - API端点URL
 * @param {Object} options - 请求选项
 * @returns {Promise<Object>} API响应数据
 */
async function api(url, options) {
    const response = await fetch(url, Object.assign({
        headers: { 'Content-Type': 'application/json' }
    }, options));
    
    const responseText = await response.text();
    let data = {};
    
    try {
        data = responseText ? JSON.parse(responseText) : {};
    } catch (e) {
        // JSON解析失败时保持data为空对象
    }
    
    // 检查响应是否成功
    if (!response.ok || data.succeeded === false) {
        const errorMessage = data.errors && 
            (typeof data.errors === 'string' ? data.errors : Object.values(data.errors)[0][0]) ||
            data.message ||
            responseText ||
            '操作失败';
        
        toast(errorMessage, false);
        throw new Error(errorMessage);
    }
    
    return data;
}// DOM元素引用
const tbody = document.getElementById('kwBody');

/**
 * 生成样式字符串
 * @param {Object} keyword - 关键词对象
 * @returns {string} 样式描述字符串
 */
function styleString(keyword) {
    return Object.entries(styleMap)
        .filter(([key]) => keyword[key])
        .map(([, value]) => value)
        .join(' ');
}/**
 * 渲染表格行
 * @param {Object} keyword - 关键词对象
 * @returns {HTMLTableRowElement} 表格行元素
 */
function renderRow(keyword) {
    const row = document.createElement('tr');
    row.innerHTML = `
    <td><input type="checkbox" class="row-check" value="${keyword.id}"></td>
    <td>${keyword.id}</td>
    <td>${keyword.keywordContent}</td>
    <td>${typeMap[keyword.keywordType] ?? keyword.keywordType}</td>
    <td>${actionMap[keyword.keywordAction] ?? keyword.keywordAction}</td>
    <td>${styleString(keyword)}</td>
    <td>
      <button class="btn" onclick='openEdit(${JSON.stringify(keyword)})'>编辑</button>
      <button class="btn btn-error ml-1" onclick="del(${keyword.id})">删</button>
    </td>`;
    return row;
}/**
 * 刷新关键词列表
 */
async function refresh() {
    const { data: keywords } = await api(`${API}/list`);
    tbody.innerHTML = '';
    keywords.forEach(keyword => tbody.appendChild(renderRow(keyword)));
}

/**
 * 删除关键词
 * @param {number} id - 关键词ID
 */
async function del(id) {
    await api(`${API}/delete/${id}`, { method: 'DELETE' });
    toast('删除成功');
    refresh();
}/**
 * 切换所有行的选中状态
 * @param {HTMLInputElement} checkbox - 全选复选框
 */
function toggleAll(checkbox) {
    document.querySelectorAll('.row-check').forEach(check => 
        check.checked = checkbox.checked
    );
}

/**
 * 删除选中的关键词
 */
async function deleteSelected() {
    const selectedIds = [...document.querySelectorAll('.row-check')]
        .filter(check => check.checked)
        .map(check => +check.value);
    
    if (!selectedIds.length) {
        toast('未选中', false);
        return;
    }
    
    await api(`${API}/batchdelete`, {
        method: 'DELETE',
        body: JSON.stringify(selectedIds)
    });
    
    toast('批量删除成功');
    refresh();
}/**
 * 添加单个关键词
 */
async function addSingle() {
    const keywordData = {
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
    
    if (!keywordData.keywordContent) {
        toast('内容不能为空', false);
        return;
    }
    
    await api(`${API}/add`, {
        method: 'POST',
        body: JSON.stringify(keywordData)
    });
    
    toast('添加成功');
    refresh();
}// DOM元素引用
const dynamicRows = document.getElementById('dynamicRows');

/**
 * 生成行模板
 * @param {number} id - 行ID
 * @returns {string} HTML模板字符串
 */
function rowTpl(id) {
    return `<div class="flex flex-wrap gap-2 items-center border p-2 rounded" id="row-${id}">
    <input class="input input-bordered w-40" placeholder="关键词">
    <select class="select select-bordered">
      <option value="0">全字</option><option value="1">包含</option>
      <option value="2">正则</option><option value="3">模糊</option><option value="4">用户</option>
    </select>
    <select class="select select-bordered"><option value="1">监控</option><option value="0">排除</option></select>
    ${Object.entries(styleMap).map(([key, value]) => `
      <label class="label gap-1 text-xs"><span>${value}</span>
        <input type="checkbox" data-flag="${key}" class="checkbox">
      </label>`).join('')}
    <button class="btn btn-error" onclick="this.parentNode.remove()">x</button>
  </div>`;
}

/**
 * 添加新行
 */
function addRow() {
    dynamicRows.insertAdjacentHTML('beforeend', rowTpl(Date.now()));
}/**
 * 上传动态行的关键词
 */
async function uploadRows() {
    const keywords = [...dynamicRows.children].map(row => {
        const input = row.querySelector('input.input');
        if (!input || !input.value.trim()) return null;
        
        const [typeSelect, actionSelect] = row.querySelectorAll('select');
        const keywordData = {
            keywordContent: input.value.trim(),
            keywordType: +typeSelect.value,
            keywordAction: +actionSelect.value
        };
        
        // 添加样式选项
        row.querySelectorAll('input[data-flag]').forEach(checkbox => {
            keywordData[checkbox.dataset.flag] = checkbox.checked;
        });
        
        return keywordData;
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
}/**
 * 上传文本关键词
 */
async function uploadText() {
    const keywords = txtKeywords.value
        .split('
')
        .map(line => line.trim())
        .filter(Boolean);
    
    if (!keywords.length) {
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
    
    const keywordData = keywords.map(keyword => ({
        keywordContent: keyword,
        keywordType: +tType.value,
        keywordAction: +tAction.value,
        ...styleOptions
    }));
    
    await api(`${API}/batchadd`, {
        method: 'POST',
        body: JSON.stringify(keywordData)
    });
    
    toast('批量添加成功');
    refresh();
}/**
 * 填充编辑表单
 * @param {Object} keyword - 关键词对象
 */
function fillEdit(keyword) {
    eId.value = keyword.id;
    eContent.value = keyword.keywordContent;
    
    const typeKeys = Object.keys(typeMap);
    const actionKeys = Object.keys(actionMap);
    
    eType.value = typeKeys.indexOf(keyword.keywordType);
    eAction.value = actionKeys.indexOf(keyword.keywordAction);
    
    // 设置样式选项
    eCase.checked = keyword.isCaseSensitive;
    eBold.checked = keyword.isBold;
    eItalic.checked = keyword.isItalic;
    eUnder.checked = keyword.isUnderline;
    eStrike.checked = keyword.isStrikeThrough;
    eQuote.checked = keyword.isQuote;
    eMono.checked = keyword.isMonospace;
    eSpoil.checked = keyword.isSpoiler;
}/**
 * 打开编辑模态框
 * @param {Object} keyword - 关键词对象
 */
function openEdit(keyword) {
    fillEdit(keyword);
    editModal.showModal();
}

/**
 * 保存编辑
 */
async function saveEdit() {
    const keywordData = {
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
        body: JSON.stringify(keywordData)
    });
    
    toast('修改成功');
    refresh();
}// 标签页切换功能
document.querySelectorAll('[role=tab]').forEach(tab => {
    tab.onclick = () => {
        // 移除所有标签的激活状态
        document.querySelectorAll('[role=tab]').forEach(t => 
            t.classList.remove('tab-active')
        );
        
        // 激活当前标签
        tab.classList.add('tab-active');
        
        // 切换面板显示
        ['list', 'single', 'batch', 'text'].forEach(panelName => {
            document.getElementById('panel-' + panelName)
                .classList.toggle('hidden', !tab.id.endsWith(panelName));
        });
    };
});



// 初始化数据
refresh();
