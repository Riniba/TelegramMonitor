// Telegram Monitor - Web Interface Script
// Deobfuscated version

const API = '/api/telegram';

// Toast notification system
const toastBox = document.getElementById('toast');
toastBox.style.zIndex = '9999';

function toast(message, isSuccess = true) {
    const alertEl = document.createElement('div');
    alertEl.role = 'alert';
    alertEl.className = `alert alert-${isSuccess ? 'success' : 'error'} alert-horizontal shadow-lg`;
    alertEl.innerHTML = `<span>${message}</span>`;
    toastBox.appendChild(alertEl);
    setTimeout(() => alertEl.remove(), 4000);
}

// API wrapper
async function api(url, options) {
    const response = await fetch(url, {
        headers: { 'Content-Type': 'application/json' },
        ...options
    });

    const text = await response.text();
    let data = {};

    try {
        data = text ? JSON.parse(text) : {};
    } catch {}

    if (!response.ok || data.succeeded === false) {
        const errorMsg = data.errors && (typeof data.errors === 'string' ?
            data.errors : Object.values(data.errors)[0][0]) ||
            data.message || text || '失败';
        toast(errorMsg, false);
        throw new Error(errorMsg);
    }

    return data;
}

// Logging utility
const log = (data) => {
    console.log(typeof data === 'string' ? data : JSON.stringify(data, null, 2));
};

// DOM elements
const proxyType = document.getElementById('proxyType');
const proxyUrl = document.getElementById('proxyUrl');
const dialogList = document.getElementById('dialogList');
const btns = {
    load: document.getElementById('btnLoadDialogs'),
    target: document.getElementById('btnSetTarget'),
    start: document.getElementById('btnStart'),
    stop: document.getElementById('btnStop')
};

// Application state
let state = { logged: false, mon: false };

function applyState() {
    btns.load.disabled = !state.logged;
    dialogList.disabled = !state.logged;
    btns.target.disabled = !state.logged;
    btns.start.disabled = !state.logged || state.mon;
    btns.stop.disabled = !state.logged || !state.mon;
}

async function fetchState() {
    const { data: status } = await api(`${API}/status`);
    state = { logged: status.loggedIn, mon: status.monitoring };
    applyState();

    if (state.logged) {
        document.getElementById('btnLogin').innerText = '已登录 (点击重新登录)';
    } else {
        document.getElementById('btnLogin').innerText = '登录';
    }
}

function proxyTypeChanged() {
    proxyUrl.disabled = proxyType.value === '0';
}

async function setProxy() {
    const url = proxyType.value === '0' ? '' : proxyUrl.value.trim();
    const wasMonitoring = state.mon;

    try {
        if (wasMonitoring) {
            toast('正在停止监控以应用新代理...');
        }

        const result = await api(`${API}/proxy`, {
            method: 'POST',
            body: JSON.stringify({ type: +proxyType.value, url })
        });

        const status = result.data;
        log('代理设置响应:', status);

        switch (status) {
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
                openLoginWithState(status);
                break;
            default:
                toast('代理已设置');
                break;
        }

        await fetchState();
    } catch (error) {
        toast(`设置代理失败: ${error.message}`, false);
    }
}

// Login flow
let step = 0;
let currentPhone = '';
const loginModal = document.getElementById('loginModal');
const title = document.getElementById('loginTitle');
const inp = document.getElementById('stepInput');

function openLoginWithState(status) {
    step = status === 'WaitingForVerificationCode' ? 1 : 2;

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

function openLogin() {
    step = 0;
    title.textContent = '手机号登录';
    inp.placeholder = '+8613812345678';
    inp.value = '';
    loginModal.showModal();
}

async function loginStep() {
    const value = inp.value.trim();

    if (!value) {
        toast('输入不能为空', false);
        return;
    }

    try {
        if (step === 0) {
            currentPhone = value;
        }

        const payload = step === 0 ?
            { phoneNumber: value, loginInfo: '' } :
            { phoneNumber: currentPhone, loginInfo: value };

        const result = await api(`${API}/login`, {
            method: 'POST',
            body: JSON.stringify(payload)
        });

        log(result);

        if (result && typeof result === 'object') {
            if (result.data !== undefined) {
                handleLoginResponse(result.data);
            } else {
                handleLoginResponse(result);
            }
        } else {
            toast('登录响应格式错误', false);
            loginModal.close();
        }
    } catch (error) {
        toast(`登录时出错: ${error.message}`, false);
        loginModal.close();
    }
}

function handleLoginResponse(status) {
    if (typeof status !== 'string') {
        toast('登录响应格式错误', false);
        loginModal.close();
        return;
    }

    switch (status) {
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
            fetchState().catch(err => toast(`获取状态失败: ${err.message}`, false));
            break;
        case 'NotLoggedIn':
            toast('登录失败', false);
            loginModal.close();
            break;
        default:
            toast(`登录状态未知: ${status}`, false);
            loginModal.close();
    }
}

async function loadDialogs() {
    const { data: dialogs } = await api(`${API}/dialogs`);
    dialogList.innerHTML = dialogs.map(dialog =>
        `<option value="${dialog.id}">${dialog.displayTitle}</option>`
    ).join('');
    toast('会话已加载');
}

async function setTarget() {
    if (!dialogList.value) {
        toast('请选择会话', false);
        return;
    }

    await api(`${API}/target`, {
        method: 'POST',
        body: dialogList.value
    });

    toast('已设置目标');
}

async function startMonitor() {
    const { data: status } = await api(`${API}/start`, { method: 'POST' });

    let success = false;
    let message = '';

    switch (status) {
        case 'Started':
            success = true;
            message = '启动成功';
            break;
        case 'MissingTarget':
            message = '未设置目标群';
            break;
        case 'NoUserInfo':
            message = '未获取到用户信息';
            break;
        case 'AlreadyRunning':
            success = true;
            message = '已在运行';
            break;
        case 'Error':
            message = '未登录';
            break;
        default:
            message = `未知状态: ${status}`;
    }

    toast(message, success);
    state.mon = success;
    applyState();
}

async function stopMonitor() {
    await api(`${API}/stop`, { method: 'POST' });
    toast('已停止');
    state.mon = false;
    applyState();
}

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
proxyTypeChanged();
fetchState();
