import axios from "axios";

// --- Yeni Eklenecek Kısım Başlangıcı ---

// Kendi backend'ine istek atacak olan axios instance'ı
// ÖNEMLİ: withCredentials, tarayıcının cookie'leri backend'e göndermesini ve
// backend'den gelen Set-Cookie başlıklarını almasını sağlar.
const lucaProxyClient = axios.create({
  baseURL: "http://localhost:5000", // KENDİ BACKEND'İNİN ADRESİ BU OLMALI!
  withCredentials: true,
  headers: {
    "Content-Type": "application/json",
  },
});

// --- Yeni Eklenecek Kısım Sonu ---

export const loginToLuca = async () => {
  try {
    console.log("Adım 1: Giriş yapılıyor (Backend Proxy üzerinden)...");
    // Artık 'axios' yerine 'lucaProxyClient' kullanıyoruz
    const response = await lucaProxyClient.post("/api/luca/login", {
      orgCode: "7374953",
      userName: "Admin",
      userPassword: "2009Bfm",
    });
    if (response.data.code === 0) {
      console.log("Giriş Başarılı:", response.data.message);
      return true;
    } else {
      console.error("Giriş Başarısız:", response.data.message);
      return false;
    }
  } catch (error) {
    console.error("Giriş işlemi sırasında hata:", error);
    return false;
  }
};

export const getBranchList = async () => {
  try {
    console.log("Adım 2: Şube listesi alınıyor (Backend Proxy üzerinden)...");
    // Artık 'axios' yerine 'lucaProxyClient' kullanıyoruz
    const response = await lucaProxyClient.post("/api/luca/branches", {});
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
  } catch (error) {
    console.error("Şube listesi alınırken hata:", error);
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
    const response = await lucaProxyClient.post("/api/luca/select-branch", {
      orgSirketSubeId: branchId,
    });

    console.log("Raw select response:", response.data);

    const data = response.data;
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
