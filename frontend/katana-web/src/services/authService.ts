import axios from "axios";

// --- Yeni Eklenecek Kısım Başlangıcı ---

// Kendi backend'ine istek atacak olan axios instance'ı
// ÖNEMLİ: withCredentials, tarayıcının cookie'leri backend'e göndermesini ve
// backend'den gelen Set-Cookie başlıklarını almasını sağlar.
const lucaProxyClient = axios.create({
  baseURL: process.env.REACT_APP_API_URL || "/api",
  withCredentials: true,
  timeout: 30000,
  headers: {
    "Content-Type": "application/json",
  },
});

lucaProxyClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.code === "ERR_BLOCKED_BY_CLIENT") {
      console.error(
        "🚫 Browser AdBlock/Extension engelledi. Lütfen devre dışı bırakın."
      );
    }
    return Promise.reject(error);
  }
);

// --- Yeni Eklenecek Kısım Sonu ---

export const loginToLuca = async () => {
  try {
    console.log("Adım 1: Giriş yapılıyor (Backend Proxy üzerinden)...");
    // Artık 'axios' yerine 'lucaProxyClient' kullanıyoruz
    const response = await lucaProxyClient.post("/luca/login", {
      orgCode: "7374953",
      userName: "Admin",
      userPassword: "2009Bfm",
    });

    const data: any = response?.data ?? null;
    console.log("Raw login response:", data);

    // Try to pull sessionId from a few common shapes
    const sessionId =
      data?.sessionId ??
      data?.SessionId ??
      data?.session ??
      data?.data?.sessionId ??
      null;
    if (sessionId) {
      try {
        localStorage.setItem("lucaSessionId", sessionId);
      } catch (e) {
        // localStorage could fail in some environments; continue without blocking
        console.warn("Could not persist lucaSessionId to localStorage:", e);
      }
    }

    // Heuristics to decide success
    const codeOk =
      data?.code === 0 || data?.raw?.code === 0 || data?.Raw?.code === 0;
    const message =
      typeof data?.message === "string" ? data.message : data?.Message ?? null;
    const ok = Boolean(codeOk || sessionId || response.status === 200);

    if (ok) {
      console.log("Giriş Başarılı:", message, "sessionId:", sessionId);
      return true;
    }

    console.error("Giriş Başarısız:", message, data);
    return false;
  } catch (error) {
    console.error("Giriş işlemi sırasında hata:", error);
    return false;
  }
};

export const getBranchList = async () => {
  try {
    console.log("Adım 2: Şube listesi alınıyor (Backend Proxy üzerinden)...");
    // Artık 'axios' yerine 'lucaProxyClient' kullanıyoruz
    const sessionId =
      typeof window !== "undefined"
        ? localStorage.getItem("lucaSessionId")
        : null;
    const headers: any = {};
    if (sessionId) headers["X-Luca-Session"] = sessionId;
    const response = await lucaProxyClient.post(
      "/luca/branches",
      {},
      { headers }
    );
    // Defensive parsing: farklı shape'ler olabilir: array doğrudan, { data: [...] }, { branches: [...] }
    let payload: any = response.data;
    console.log("Raw branch response:", payload);

    if (!payload) {
      console.error("Yetkili şirket/şube bulunamadı: boş cevap.");
      return null;
    }

    // Drill into common wrappers
    if (payload.data && Array.isArray(payload.data)) payload = payload.data;
    else if (payload.branches && Array.isArray(payload.branches))
      payload = payload.branches;
    else if (payload.list && Array.isArray(payload.list))
      payload = payload.list; // some responses use 'list'

    // If payload is an object with items array
    if (
      !Array.isArray(payload) &&
      payload.items &&
      Array.isArray(payload.items)
    )
      payload = payload.items;

    // Return normalized array of branch objects to let caller decide (UI selection)
    if (Array.isArray(payload) && payload.length > 0) {
      console.log("Şube listesi başarıyla alındı (normalized):", payload);
      return payload;
    }

    console.error(
      "Yetkili şirket/şube bulunamadı: beklenen biçimde dizi dönülmedi."
    );
    return null;
  } catch (error: any) {
    // If the backend returned a non-2xx response, axios provides response data
    if (error.response) {
      try {
        console.error(
          "Şube listesi hata cevabı (status):",
          error.response.status
        );
        // Backend wraps remote body in `raw` when non-success; log it if available
        console.error("Backend error payload:", error.response.data);
        if (error.response.data && error.response.data.raw) {
          console.error(
            "Luca raw response (preview):",
            error.response.data.raw
          );
        }
      } catch (logEx) {
        console.error("Error while logging branch error response:", logEx);
      }
    }
    console.error("Şube listesi alınırken hata:", error?.message ?? error);
    return null;
  }
};

export const selectBranch = async (branchOrId: any) => {
  try {
    // Accept either a plain id or an object returned from getBranchList
    let branchId: any = branchOrId;
    if (branchOrId && typeof branchOrId === "object") {
      branchId =
        branchOrId?.id ??
        branchOrId?.Id ??
        branchOrId?.branchId ??
        branchOrId?.subeId ??
        branchOrId?.orgSirketSubeId ??
        branchOrId?.companyId ??
        null;
    }

    if (branchId == null) {
      console.error(
        "Şube seçimi başarısız: geçerli bir şube id'si sağlanmadı."
      );
      return false;
    }

    console.log(
      `Adım 3: ${branchId} ID'li şube seçiliyor (Backend Proxy üzerinden)...`
    );
    const sessionId =
      typeof window !== "undefined"
        ? localStorage.getItem("lucaSessionId")
        : null;
    const headers: any = {};
    if (sessionId) headers["X-Luca-Session"] = sessionId;
    const response = await lucaProxyClient.post(
      "/luca/select-branch",
      { orgSirketSubeId: branchId },
      { headers }
    );

    console.log("Raw select response:", response.data);

    const data: any = response.data;
    // Heuristics to determine success across different backend shapes
    const message =
      typeof data?.message === "string" ? data.message : data?.Message ?? null;
    const codeOk = data?.code === 0;
    const successFlag = data?.success === true || data?.isSuccess === true;
    const messageOk =
      typeof message === "string" && message.toLowerCase().includes("başar"); // başarı, başarıyla, başarılı

    const ok = codeOk || successFlag || messageOk || response.status === 200;

    console.log("Şube seçimi sonucu (ok):", ok, "message:", message);
    return !!ok;
  } catch (error) {
    console.error("Şube seçimi sırasında hata:", error);
    return false;
  }
};
