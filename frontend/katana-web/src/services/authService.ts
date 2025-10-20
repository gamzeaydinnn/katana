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
    if (response.data && response.data.length > 0) {
      console.log("Şube listesi başarıyla alındı:", response.data);
      return response.data[0].id;
    } else {
      console.error("Yetkili şirket/şube bulunamadı.");
      return null;
    }
  } catch (error) {
    console.error("Şube listesi alınırken hata:", error);
    return null;
  }
};

export const selectBranch = async (branchId: number) => {
  try {
    console.log(
      `Adım 3: ${branchId} ID'li şube seçiliyor (Backend Proxy üzerinden)...`
    );
    // Artık 'axios' yerine 'lucaProxyClient' kullanıyoruz
    const response = await lucaProxyClient.post("/api/luca/select-branch", {
      orgSirketSubeId: branchId,
    });
    console.log("Şube seçimi başarılı:", response.data.message);
    return response.data && response.data.message?.includes("Başarıyla");
  } catch (error) {
    console.error("Şube seçimi sırasında hata:", error);
    return false;
  }
};
