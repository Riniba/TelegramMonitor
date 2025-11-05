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

// 单个添加表单元素
const sContent = document.getElementById('sContent');
const sType = document.getElementById('sType');
const sAction = document.getElementById('sAction');
const sCase = document.getElementById('sCase');
const sBold = document.getElementById('sBold');
const sItalic = document.getElementById('sItalic');
const sUnder = document.getElementById('sUnder');
const sStrike = document.getElementById('sStrike');
const sQuote = document.getElementById('sQuote');
const sMono = document.getElementById('sMono');
const sSpoil = document.getElementById('sSpoil');

// 文本批量添加表单元素
const txtKeywords = document.getElementById('txtKeywords');
const tType = document.getElementById('tType');
const tAction = document.getElementById('tAction');
const tCase = document.getElementById('tCase');
const tBold = document.getElementById('tBold');
const tItalic = document.getElementById('tItalic');
const tUnder = document.getElementById('tUnder');
const tStrike = document.getElementById('tStrike');
const tQuote = document.getElementById('tQuote');
const tMono = document.getElementById('tMono');
const tSpoil = document.getElementById('tSpoil');

// 编辑表单元素
const eId = document.getElementById('eId');
const eContent = document.getElementById('eContent');
const eType = document.getElementById('eType');
const eAction = document.getElementById('eAction');
const eCase = document.getElementById('eCase');
const eBold = document.getElementById('eBold');
const eItalic = document.getElementById('eItalic');
const eUnder = document.getElementById('eUnder');
const eStrike = document.getElementById('eStrike');
const eQuote = document.getElementById('eQuote');
const eMono = document.getElementById('eMono');
const eSpoil = document.getElementById('eSpoil');

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
    try {
        if (!sContent || !sType || !sAction) {
            toast('表单元素未找到', false);
            return;
        }
        
        const keywordData = {
            keywordContent: sContent.value.trim(),
            keywordType: +sType.value,
            keywordAction: +sAction.value,
            isCaseSensitive: sCase ? sCase.checked : false,
            isBold: sBold ? sBold.checked : false,
            isItalic: sItalic ? sItalic.checked : false,
            isUnderline: sUnder ? sUnder.checked : false,
            isStrikeThrough: sStrike ? sStrike.checked : false,
            isQuote: sQuote ? sQuote.checked : false,
            isMonospace: sMono ? sMono.checked : false,
            isSpoiler: sSpoil ? sSpoil.checked : false
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
        // 清空表单
        if (sContent) sContent.value = '';
        refresh();
    } catch (error) {
        console.error('添加关键词失败:', error);
        toast('添加失败', false);
    }
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
    try {
        if (!txtKeywords || !tType || !tAction) {
            toast('表单元素未找到', false);
            return;
        }
        
        const keywords = txtKeywords.value
            .split('\n')
            .map(line => line.trim())
            .filter(Boolean);
        
        if (!keywords.length) {
            toast('文本为空', false);
            return;
        }
        
        const styleOptions = {
            isCaseSensitive: tCase ? tCase.checked : false,
            isBold: tBold ? tBold.checked : false,
            isItalic: tItalic ? tItalic.checked : false,
            isUnderline: tUnder ? tUnder.checked : false,
            isStrikeThrough: tStrike ? tStrike.checked : false,
            isQuote: tQuote ? tQuote.checked : false,
            isMonospace: tMono ? tMono.checked : false,
            isSpoiler: tSpoil ? tSpoil.checked : false
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
        // 清空表单
        if (txtKeywords) txtKeywords.value = '';
        refresh();
    } catch (error) {
        console.error('批量添加失败:', error);
        toast('批量添加失败', false);
    }
}/**
 * 填充编辑表单
 * @param {Object} keyword - 关键词对象
 */
function fillEdit(keyword) {
    if (!eId || !eContent || !eType || !eAction) {
        console.error('编辑表单元素未找到');
        return;
    }
    
    eId.value = keyword.id;
    eContent.value = keyword.keywordContent;
    
    // 直接使用数值，因为HTML中的option value就是数值
    eType.value = keyword.keywordType;
    eAction.value = keyword.keywordAction;
    
    // 设置样式选项
    if (eCase) eCase.checked = keyword.isCaseSensitive || false;
    if (eBold) eBold.checked = keyword.isBold || false;
    if (eItalic) eItalic.checked = keyword.isItalic || false;
    if (eUnder) eUnder.checked = keyword.isUnderline || false;
    if (eStrike) eStrike.checked = keyword.isStrikeThrough || false;
    if (eQuote) eQuote.checked = keyword.isQuote || false;
    if (eMono) eMono.checked = keyword.isMonospace || false;
    if (eSpoil) eSpoil.checked = keyword.isSpoiler || false;
}/**
 * 打开编辑模态框
 * @param {Object} keyword - 关键词对象
 */
function openEdit(keyword) {
    try {
        fillEdit(keyword);
        if (editModal) {
            editModal.showModal();
        } else {
            toast('编辑对话框未找到', false);
        }
    } catch (error) {
        console.error('打开编辑失败:', error);
        toast('打开编辑失败', false);
    }
}

/**
 * 保存编辑
 */
async function saveEdit() {
    try {
        if (!eId || !eContent || !eType || !eAction) {
            toast('编辑表单元素未找到', false);
            return;
        }
        
        const keywordData = {
            id: +eId.value,
            keywordContent: eContent.value.trim(),
            keywordType: +eType.value,
            keywordAction: +eAction.value,
            isCaseSensitive: eCase ? eCase.checked : false,
            isBold: eBold ? eBold.checked : false,
            isItalic: eItalic ? eItalic.checked : false,
            isUnderline: eUnder ? eUnder.checked : false,
            isStrikeThrough: eStrike ? eStrike.checked : false,
            isQuote: eQuote ? eQuote.checked : false,
            isMonospace: eMono ? eMono.checked : false,
            isSpoiler: eSpoil ? eSpoil.checked : false
        };
        
        if (!keywordData.keywordContent) {
            toast('关键词内容不能为空', false);
            return;
        }
        
        await api(`${API}/update`, {
            method: 'PUT',
            body: JSON.stringify(keywordData)
        });
        
        toast('修改成功');
        if (editModal) {
            editModal.close();
        }
        refresh();
    } catch (error) {
        console.error('保存编辑失败:', error);
        toast('保存失败', false);
    }
}// 标签页切换功能
document.querySelectorAll('[role=tab]').forEach(tab => {
    tab.addEventListener('click', () => {
        // 移除所有标签的激活状态
        document.querySelectorAll('[role=tab]').forEach(t => 
            t.classList.remove('tab-active')
        );
        
        // 激活当前标签
        tab.classList.add('tab-active');
        
        // 切换面板显示
        ['list', 'single', 'batch', 'text'].forEach(panelName => {
            const panel = document.getElementById('panel-' + panelName);
            if (panel) {
                panel.classList.toggle('hidden', !tab.id.endsWith(panelName));
            }
        });
    });
});

// DOM元素引用 - 确保所有元素都存在
const editModal = document.getElementById('editModal');

// 初始化数据
document.addEventListener('DOMContentLoaded', () => {
    refresh().catch(error => {
        console.error('初始化失败:', error);
        toast('初始化失败，请刷新页面', false);
    });
});
