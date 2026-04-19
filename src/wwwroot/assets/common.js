(function () {
  function prop(obj, name) {
    if (!obj || typeof obj !== "object") return undefined;
    if (Object.prototype.hasOwnProperty.call(obj, name)) return obj[name];
    const pascal = name.charAt(0).toUpperCase() + name.slice(1);
    if (Object.prototype.hasOwnProperty.call(obj, pascal)) return obj[pascal];
    return undefined;
  }

  function unwrap(payload) {
    if (!payload || typeof payload !== "object") return payload;
    const data = prop(payload, "data");
    return data !== undefined ? data : payload;
  }

  function pickError(payload, fallback) {
    if (!payload || typeof payload !== "object") return fallback;
    for (const key of ["msg", "message", "errorMessage", "errors"]) {
      const value = prop(payload, key);
      if (typeof value === "string" && value.trim()) return value;
      if (Array.isArray(value) && value.length > 0 && typeof value[0] === "string") return value[0];
    }
    return fallback;
  }

  async function request(url, options = {}) {
    const redirectOn401 = options.redirectOn401 !== false;
    const { redirectOn401: _, ...fetchOptions } = options;

    const headers = { ...(fetchOptions.headers || {}) };
    if (fetchOptions.body && !headers["Content-Type"]) headers["Content-Type"] = "application/json";

    const response = await fetch(url, {
      credentials: "include",
      ...fetchOptions,
      headers
    });

    const text = await response.text();
    let payload = null;
    try {
      payload = text ? JSON.parse(text) : null;
    } catch {
      payload = null;
    }

    if (!response.ok) {
      if (response.status === 401 && redirectOn401) {
        location.href = "/";
      }
      throw new Error(pickError(payload, "请求失败"));
    }

    if (payload && typeof payload === "object" && prop(payload, "succeeded") === false) {
      throw new Error(pickError(payload, "操作失败"));
    }

    return unwrap(payload);
  }

  async function ensureAuth() {
    try {
      return await request("/api/auth/me", { method: "GET", redirectOn401: false });
    } catch {
      location.href = "/";
      return null;
    }
  }

  async function logout() {
    await request("/api/auth/logout", { method: "POST" });
    location.href = "/";
  }

  function read(obj, name) {
    return prop(obj, name);
  }

  function toInt(value, defaultValue = 0) {
    const n = Number(value);
    return Number.isFinite(n) ? n : defaultValue;
  }

  function asText(value, empty = "-") {
    if (value === null || value === undefined || value === "") return empty;
    return String(value);
  }

  function esc(value) {
    return String(value)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll("\"", "&quot;");
  }

  function yesNo(value) {
    return value ? "是" : "否";
  }

  function shorten(value, maxLength = 80) {
    const text = asText(value, "");
    if (text.length <= maxLength) return text;
    return text.slice(0, maxLength) + "...";
  }

  function setStatus(element, message, ok) {
    if (!element) return;
    element.classList.remove("ok", "err");
    element.classList.add(ok ? "ok" : "err");
    element.textContent = message;
  }

  function formatDateTime(value) {
    if (!value) return "-";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return asText(value);
    const pad = num => String(num).padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
  }

  function setCurrentNav(pageName) {
    document.querySelectorAll(".nav-link").forEach(link => {
      if (link.dataset.page === pageName) link.classList.add("active");
      else link.classList.remove("active");
    });
  }

  function bindLogoutButton() {
    const button = document.getElementById("btnLogout");
    if (!button) return;
    button.addEventListener("click", async () => {
      await logout();
    });
  }

  function fillCurrentUser(profile) {
    const label = document.getElementById("currentUser");
    if (!label || !profile) return;
    label.textContent = `当前用户：${asText(read(profile, "username"))}`;
  }

  window.tm = {
    request,
    ensureAuth,
    logout,
    read,
    toInt,
    asText,
    esc,
    yesNo,
    shorten,
    setStatus,
    formatDateTime,
    setCurrentNav,
    bindLogoutButton,
    fillCurrentUser
  };
})();
