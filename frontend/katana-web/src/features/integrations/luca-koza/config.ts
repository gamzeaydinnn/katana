// features/integrations/luca-koza/config.ts

/**
 * Luca-Koza Entegrasyon Konfigürasyonu
 */
export const KOZA_CONFIG = {
  /**
   * Depo Kartı varsayılan kategori kodu
   */
  DEFAULT_DEPO_KATEGORI_KOD: "GENEL",

  /**
   * Stok Kartı varsayılan değerleri
   */
  STOCK_DEFAULTS: {
    KATEGORI_AGAC_KOD: "001",      // Varsayılan kategori kodu
    OLCUM_BIRIMI_ID: 1,            // 1: Adet, 2: Kg, 3: Litre, vb.
    KART_TURU: 1,                  // 1: Ürün, 2: Hizmet
    KART_TIPI: 1,                  // 1: Normal, 2: Set
    KDV_ORAN: 0.18,                // %18 KDV
  },

  /**
   * API Endpoint'leri
   */
  ENDPOINTS: {
    // Depo
    DEPO_LISTELE: "/ListeleStkDepo.do",
    DEPO_EKLE: "/EkleStkWsDepo.do",
    
    // Stok Kartı
    STOK_LISTELE: "/ListeleStkKart.do",
    STOK_EKLE: "/EkleStkWsKart.do",
    
    // Eldeki Miktar
    ELDEKI_MIKTAR: "/ListeleStkElkiMiktar.do",
    
    // Depo Transferi
    DEPO_TRANSFER: "/EkleStokFisiWs.do",
  },

  /**
   * Sync ayarları
   */
  SYNC: {
    /**
     * API çağrıları arası bekleme (ms)
     * Rate limiting için
     */
    DELAY_BETWEEN_CALLS: 100,

    /**
     * Silinen kayıtları atla
     */
    SKIP_DELETED: true,
  },
};
