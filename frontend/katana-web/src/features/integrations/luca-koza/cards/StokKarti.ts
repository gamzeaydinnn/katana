// features/integrations/luca-koza/cards/StokKarti.ts

/**
 * Koza Stok Kartı
 * Katana Product → Koza Stok Kartı eşleşmesi
 * Endpoint: ListeleStkKart.do, EkleStkWsKart.do
 */

export interface KozaStokKarti {
  // Zorunlu alanlar
  kartAdi: string;              // Stok kartı adı
  kartKodu: string;             // Stok kodu (SKU)
  kartTuru: number;             // Kart türü (1: Ürün, 2: Hizmet, vb.)
  kartTipi: number;             // Kart tipi (1: Normal, 2: Set, vb.)
  olcumBirimiId: number;        // Ölçüm birimi ID
  
  // KDV oranları
  kartAlisKdvOran: number;      // Alış KDV oranı (0.18 = %18)
  kartSatisKdvOran: number;     // Satış KDV oranı
  
  // Kategori
  kategoriAgacKod: string;      // Kategori ağaç kodu
  
  // Opsiyonel alanlar
  uzunAdi?: string;             // Uzun açıklama
  barkod?: string;              // Barkod
  baslangicTarihi?: string;     // Başlangıç tarihi (ISO format)
  bitisTarihi?: string;         // Bitiş tarihi
  
  // Stok kontrol
  minStokKontrol?: number;      // Min stok kontrolü (0: Yok, 1: Var)
  minStokMiktari?: number;      // Min stok miktarı
  maxStokKontrol?: number;      // Max stok kontrolü
  maxStokMiktari?: number;      // Max stok miktarı
  
  // Bayraklar
  satilabilirFlag?: number;     // Satılabilir mi? (0: Hayır, 1: Evet)
  satinAlinabilirFlag?: number; // Satın alınabilir mi?
  maliyetHesaplanacakFlag?: number; // Maliyet hesaplansın mı?
  
  // Diğer
  rafOmru?: number;             // Raf ömrü (gün)
  garantiSuresi?: number;       // Garanti süresi (gün)
  gtipKodu?: string;            // GTIP kodu
  
  // Listele'den gelen ID (varsa)
  stokKartId?: number;          // Koza stok kart ID
}

/**
 * Stok Kartı Listeleme Request
 */
export type StokKartiListeRequest =
  | Record<string, never>       // Boş object {} → tüm stok kartları
  | {
      stkKart: {
        kodOp?: "between" | "eq";
        kodBas?: string;
        kodBit?: string;
        kartKod?: string;       // Tek kart için
      };
    };

/**
 * Stok Kartı Listeleme Response
 */
export interface StokKartiListeResponse {
  stokKartlari?: KozaStokKarti[];
  stkKartListesi?: KozaStokKarti[];
  error?: boolean;
  message?: string;
}

/**
 * Stok Kartı Ekleme Request
 * Koza'nın beklediği format: { stkKart: { ... } }
 */
export interface StokKartiEkleRequest {
  stkKart: KozaStokKarti;
}

/**
 * Stok Kartı Ekleme Response
 */
export interface StokKartiEkleResponse {
  error?: boolean;
  message?: string;
  stokKartId?: number;
}
