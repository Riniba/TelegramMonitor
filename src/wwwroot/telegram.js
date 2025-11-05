// Telegram Monitor 前端脚本
// 反混淆版本 - 恢复可读性

// API端点
const API = '/api/telegram';

// DOM元素引用
const toastBox = document.getElementById('toast');
toastBox.style.zIndex = '9999';

/**
 * 显示提示消息
 * @param {string} message - 消息内容
 * @param {boolean} isSuccess - 是否为成功消息
 */
function toast(message, isSuccess = true) {
    const toastElement = document.createElement('div');
    toastElement.role = 'alert';
    toastElement.className = 'alert alert-' + (isSuccess ? 'success' : 'error') + ' alert-horizontal shadow-lg';
    toastElement.innerHTML = '<span>' + message + '</span>';
    toastBox.appendChild(toastElement);
    setTimeout(() => toastElement.remove(), 4000);
}

/**
 * API请求封装
 * @param {string} url - 请求URL
 * @param {Object} options - 请求选项
 * @returns {Promise} API响应
 */
async function api(url, options) {
    const response = await fetch(url, {
        headers: { 'Content-Type': 'application/json' },
        ...options
    });
    
    const responseText = await response.text();
    let data = {};
    
    try {
        data = responseText ? JSON.parse(responseText) : {};
    } catch {
        // JSON解析失败时忽略
    }
    
    if (!response.ok || data.succeeded === false) {
        const errorMessage = data.errors && 
            (typeof data.errors === 'string' ? 
                data.errors : 
                Object.values(data.errors)[0][0]) || 
            data.message || 
            responseText || 
            '失败';
        
        toast(errorMessage, false);
        throw new Error(errorMessage);
    }
    
    return data;
}

// 输出元素
const out = document.getElementById('out');

/**
 * 控制台日志输出
 * @param {*} message - 日志内容
 */
const log = (message) => {
    console.log(typeof message === 'string' ? message : JSON.stringify(message, null, 2));
};

// 代理相关元素
const proxyType = document.getElementById('proxyType');
const proxyUrl = document.getElementById('proxyUrl');

// 对话框列表
const dialogList = document.getElementById('dialogList');

// 按钮引用
const btns = {
    load: document.getElementById('btnLoadDialogs'),
    target: document.getElementById('btnSetTarget'),
    start: document.getElementById('btnStart'),
    stop: document.getElementById('btnStop')
};

// 应用状态
let state = {
    logged: false,
    mon: false
};

/**
 * 应用状态到UI
 */
function applyState() {
    btns.load.disabled = !state.logged;
    dialogList.disabled = !state.logged;
    btns.target.disabled = !state.logged;
    btns.start.disabled = !state.logged || state.mon;
    btns.stop.disabled = !state.logged || !state.mon;
}

/**
 * 获取服务器状态
 */
async function fetchState() {
    const { data: statusData } = await api(API + '/status');
    
    state = {
        logged: statusData.loggedIn,
        mon: statusData.monitoring
    };
    
    applyState();
    
    if (state.logged) {
        document.getElementById('btnLogin').innerText = '已登录 (点击重新登录)';
    } else {
        document.getElementById('btnLogin').innerText = '登录';
    }
}

/**
 * 代理类型改变事件处理
 */
function proxyTypeChanged() {
    if (proxyUrl && proxyType) {
        proxyUrl.disabled = proxyType.value === '0';
        if (proxyType.value === '0') {
            proxyUrl.placeholder = "选择代理类型后输入...";
        } else if (proxyType.value === '1') {
            proxyUrl.disabled = false;
            proxyUrl.placeholder = "格式: host:port 或 host:port:username:password";
        } else if (proxyType.value === '2') {
            proxyUrl.disabled = false;
            proxyUrl.placeholder = "格式: http://t.me/proxy...";
        }
    }
}

/**
 * 设置代理
 */
async function setProxy() {
    const proxyUrlValue = proxyType.value === '0' ? '' : proxyUrl.value.trim();
    const wasMonitoring = state.mon;
    
    try {
        if (wasMonitoring) {
            toast('正在停止监控以应用新代理...');
        }
        
        const response = await api(API + '/proxy', {
            method: 'POST',
            body: JSON.stringify({
                type: +proxyType.value,
                url: proxyUrlValue
            })
        });
        
        const proxyResponse = response.data;
        log('代理设置响应:', proxyResponse);
        
        switch (proxyResponse) {
            case 'LoggedIn':
                toast('代理已设置，登录状态已保持');
                if (wasMonitoring) {
                    toast('监控已恢复');
                }
                break;
            case 'NotLoggedIn':
                toast('代理已设置，但需要重新登录', false);
                document.getElementById('btnLogin').innerText = '登录';
                break;
            case 'WaitingForVerificationCode':
            case 'WaitingForPassword':
                toast('代理已设置，需要额外验证', false);
                openLoginWithState(proxyResponse);
                break;
            default:
                toast('代理已设置');
                break;
        }
        
        await fetchState();
    } catch (error) {
        toast('设置代理失败: ' + error.message, false);
    }
}

// 登录相关变量
let step = 0;
let currentPhone = '';

// 登录相关DOM元素
const loginModal = document.getElementById('loginModal');
const title = document.getElementById('loginTitle');
const inp = document.getElementById('stepInput');

/**
 * 根据状态打开登录对话框
 * @param {string} loginState - 登录状态
 */
function openLoginWithState(loginState) {
    step = loginState === 'WaitingForVerificationCode' ? 1 : 2;
    
    if (step === 1) {
        title.textContent = '输入验证码';
        inp.placeholder = '短信验证码';
    } else {
        title.textContent = '输入 2FA 密码';
        inp.placeholder = '账户密码';
    }
    
    inp.value = '';
    loginModal.showModal();
}

/**
 * 打开登录对话框
 */
function openLogin() {
    step = 0;
    title.textContent = '手机号登录';
    inp.placeholder = '+8613812345678';
    inp.value = '';
    loginModal.showModal();
}

/**
 * 执行登录步骤
 */
async function loginStep() {
    const inputValue = inp.value.trim();
    
    if (!inputValue) {
        toast('输入不能为空', false);
        return;
    }
    
    try {
        if (step === 0) {
            currentPhone = inputValue;
        }
        
        const loginData = step === 0 ? {
            phoneNumber: inputValue,
            loginInfo: ''
        } : {
            phoneNumber: currentPhone,
            loginInfo: inputValue
        };
        
        const response = await api(API + '/login', {
            method: 'POST',
            body: JSON.stringify(loginData)
        });
        
        log(response);
        
        if (response && typeof response === 'object') {
            if (response.data !== undefined) {
                handleLoginResponse(response.data);
            } else {
                handleLoginResponse(response);
            }
        } else {
            toast('登录响应格式错误', false);
            loginModal.close();
        }
    } catch (error) {
        toast('登录时出错: ' + error.message, false);
        loginModal.close();
    }
}

/**
 * 处理登录响应
 * @param {string} response - 登录响应
 */
function handleLoginResponse(response) {
    if (typeof response !== 'string') {
        toast('登录响应格式错误', false);
        loginModal.close();
        return;
    }
    
    switch (response) {
        case 'WaitingForVerificationCode':
            step = 1;
            title.textContent = '输入验证码';
            inp.value = '';
            inp.placeholder = '短信验证码';
            break;
        case 'WaitingForPassword':
            step = 2;
            title.textContent = '输入 2FA 密码';
            inp.value = '';
            inp.placeholder = '账户密码';
            break;
        case 'LoggedIn':
            toast('登录成功');
            loginModal.close();
            document.getElementById('btnLogin').innerText = '已登录 (点击重新登录)';
            fetchState().catch(error => toast('获取状态失败: ' + error.message, false));
            break;
        case 'NotLoggedIn':
            toast('登录失败', false);
            loginModal.close();
            break;
        default:
            toast('登录状态未知: ' + response, false);
            loginModal.close();
    }
}

/**
 * 加载对话列表
 */
async function loadDialogs() {
    try {
        const { data: dialogs } = await api(API + '/dialogs');
        if (dialogList && dialogs) {
            dialogList.innerHTML = dialogs.map(dialog => 
                '<option value="' + dialog.id + '">' + dialog.displayTitle + '</option>'
            ).join('');
            toast('会话已加载');
        }
    } catch (error) {
        console.error('加载对话列表失败:', error);
        toast('加载会话失败', false);
    }
}

/**
 * 设置监控目标
 */
async function setTarget() {
    try {
        if (!dialogList || !dialogList.value) {
            toast('请选择会话', false);
            return;
        }
        
        await api(API + '/target', {
            method: 'POST',
            body: dialogList.value
        });
        
        toast('已设置目标');
    } catch (error) {
        console.error('设置目标失败:', error);
        toast('设置目标失败', false);
    }
}

/**
 * 启动监控
 */
async function startMonitor() {
    try {
        const { data: response } = await api(API + '/start', {
            method: 'POST'
        });
        
        let isSuccess = false;
        let message = '';
        
        switch (response) {
            case 'Started':
                isSuccess = true;
                message = '启动成功';
                break;
            case 'MissingTarget':
                message = '未设置目标群';
                break;
            case 'NoUserInfo':
                message = '未获取到用户信息';
                break;
            case 'AlreadyRunning':
                isSuccess = true;
                message = '已在运行';
                break;
            case 'Error':
                message = '未登录';
                break;
            default:
                message = '未知状态: ' + response;
        }
        
        toast(message, isSuccess);
        state.mon = isSuccess;
        applyState();
    } catch (error) {
        console.error('启动监控失败:', error);
        toast('启动监控失败', false);
    }
}

/**
 * 停止监控
 */
async function stopMonitor() {
    try {
        await api(API + '/stop', {
            method: 'POST'
        });
        
        toast('已停止');
        state.mon = false;
        applyState();
    } catch (error) {
        console.error('停止监控失败:', error);
        toast('停止监控失败', false);
    }
}


// 初始化
document.addEventListener('DOMContentLoaded', () => {
    // 设置代理类型变更事件
    const proxyTypeElement = document.getElementById('proxyType');
    if (proxyTypeElement) {
        proxyTypeElement.addEventListener('change', proxyTypeChanged);
    }
    
    // 获取初始状态
    fetchState().catch(error => {
        console.error('获取状态失败:', error);
        toast('获取状态失败', false);
    });
});