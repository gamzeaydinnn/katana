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
    // Backend konfigürasyonundaki Luca cred'leri kullanılacak; body boş geçilebilir
    const response = await lucaProxyClient.post("/luca/login", {});

    const data: any = response?.data ?? null;
    console.log("Raw login response:", data);

    const raw = data?.raw ?? data;

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
      data?.code === 0 ||
      raw?.code === 0 ||
      data?.raw?.code === 0 ||
      data?.Raw?.code === 0;
    const message =
      typeof raw?.message === "string"
        ? raw.message
        : typeof data?.message === "string"
        ? data.message
        : data?.Message ?? raw?.Message ?? null;
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

    let payload: any = response.data;
    console.log("Raw branch response:", payload);
    console.log("Branch response type:", typeof payload);
    console.log("Branch response keys:", payload ? Object.keys(payload) : "null");

    if (!payload) {
      console.error("Yetkili şirket/şube bulunamadı: boş cevap.");
      return null;
    }

    // Eğer hata kodu doğrudan kökte geldiyse erken dön
    if (typeof payload === "object" && payload !== null && (payload.code ?? payload.Code)) {
      console.error(
        `Şube listesi alınamadı (code=${payload.code ?? payload.Code}): ${
          payload.message ?? payload.Message ?? "Bilinmeyen hata"
        }`
      );
      return null;
    }

    // Tüm olası veri yapılarını kontrol et
    let branches: any = null;

    // 1) Direkt array
    if (Array.isArray(payload)) {
      branches = payload;
    }
    // 2) data
    else if (payload.data && Array.isArray(payload.data)) {
      branches = payload.data;
    }
    // 3) list
    else if (Array.isArray(payload.list)) {
      branches = payload.list;
    }
    // 4) items
    else if (Array.isArray(payload.items)) {
      branches = payload.items;
    }
    // 5) branches
    else if (Array.isArray(payload.branches)) {
      branches = payload.branches;
    }
    // 6) raw içinde gömülü
    else if (payload.raw) {
      try {
        const raw =
          typeof payload.raw === "string" ? JSON.parse(payload.raw) : payload.raw;
        if (Array.isArray(raw)) branches = raw;
        else if (raw && Array.isArray(raw.data)) branches = raw.data;
        else if (raw && Array.isArray(raw.list)) branches = raw.list;

        // Eğer raw içindeki code != 0 ise erken dön ve mesajı logla
        if (raw && typeof raw === "object" && (raw.code ?? raw.Code) !== undefined) {
          const rawCode = raw.code ?? raw.Code;
          const rawMessage = raw.message ?? raw.Message ?? "";
          console.error(
            `Şube listesi alınamadı (raw code=${rawCode}): ${rawMessage}`
          );
          return null;
        }
      } catch (e) {
        console.error("Raw parse hatası:", e);
      }
    }

    if (!branches || !Array.isArray(branches)) {
      console.error(
        "Şube listesi parse edilemedi. Tam response:",
        JSON.stringify(payload, null, 2)
      );
      console.error(
        "Yetkili şirket/şube bulunamadı: beklenen biçimde dizi dönülmedi."
      );

      // raw string içinden hata mesajı varsa logla
      if (payload && typeof payload.raw === "string") {
        try {
          const parsedRaw = JSON.parse(payload.raw);
          const rawCode = parsedRaw.code ?? parsedRaw.Code;
          const rawMessage = parsedRaw.message ?? parsedRaw.Message ?? "";
          console.error(
            `Luca cevapladı fakat şube listesi yok (code=${rawCode}): ${rawMessage}`
          );
        } catch (rawEx) {
          console.error("raw payload parse başarısız:", rawEx);
        }
      }

      if (payload && typeof payload === "object" && (payload.id ?? payload.Id)) {
        console.log("Tek şube objesi tespit edildi, array'e çeviriliyor");
        branches = [payload];
      } else {
        return null;
      }
    }

    if (branches.length === 0) {
      console.error("Şube listesi boş döndü");
      return null;
    }

    console.log("Şube listesi başarıyla alındı:", branches.length, "adet şube");
    console.log("İlk şube örneği:", branches[0]);

    return branches;
  } catch (error: any) {
    if (error.response) {
      console.error("Şube listesi hata cevabı (status):", error.response.status);
      console.error("Backend error payload:", error.response.data);
      if (error.response.data && error.response.data.raw) {
        console.error("Luca raw response:", error.response.data.raw);
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
