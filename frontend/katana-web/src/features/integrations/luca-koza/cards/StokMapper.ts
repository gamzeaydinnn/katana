// features/integrations/luca-koza/cards/StokMapper.ts

import { KozaStokKarti } from "./StokKarti";

/**
 * Katana Product tipi
 * Backend'den gelen product yapısı
 */
export interface KatanaProduct {
  id: number | string;
  name: string;
  sku: string;
  price?: number;
  barcode?: string;
  category?: {
    id: number;
    name: string;
    code?: string;
  } | null;
  description?: string;
  unit?: string;
  minStock?: number;
  maxStock?: number;
  taxRate?: number;
  createdAt?: string;
  updatedAt?: string;
  deletedAt?: string | null;  // Aktiflik kontrolü için
}

/**
 * String normalize et
 */
function norm(v?: string | null): string {
  return (v ?? "").trim().replace(/\s+/g, " ");
}

/**
 * Katana Product → Koza Stok Kartı dönüşümü
 * @param product Katana product objesi
 * @param defaultValues Varsayılan değerler (kategori, ölçüm birimi, vb.)
 */
export function mapKatanaProductToKozaStokKarti(
  product: KatanaProduct,
  defaultValues: {
    kategoriAgacKod: string;      // Örn: "001" veya "001.002"
    olcumBirimiId: number;        // Örn: 1 (Adet), 2 (Kg), vb.
    kartTuru?: number;            // 1: Ürün (default), 2: Hizmet
    kartTipi?: number;            // 1: Normal (default)
  }
): KozaStokKarti {
  // KDV oranı: Katana'dan gelen taxRate veya varsayılan %18
  const kdvOran = product.taxRate ?? 0.18;

  return {
    // Zorunlu alanlar
    kartKodu: norm(product.sku),
    kartAdi: norm(product.name),
    kartTuru: defaultValues.kartTuru ?? 1,        // 1: Ürün
    kartTipi: defaultValues.kartTipi ?? 1,        // 1: Normal
    olcumBirimiId: defaultValues.olcumBirimiId,
    kategoriAgacKod: defaultValues.kategoriAgacKod,
    
    // KDV oranları
    kartAlisKdvOran: kdvOran,
    kartSatisKdvOran: kdvOran,
    
    // Opsiyonel alanlar
    uzunAdi: norm(product.description) || norm(product.name),
    barkod: norm(product.barcode) || undefined,
    baslangicTarihi: product.createdAt || new Date().toISOString(),
    
    // Stok kontrol
    minStokKontrol: product.minStock ? 1 : 0,
    minStokMiktari: product.minStock || 0,
    maxStokKontrol: product.maxStock ? 1 : 0,
    maxStokMiktari: product.maxStock || 0,
    
    // Bayraklar
    satilabilirFlag: 1,           // Satılabilir
    satinAlinabilirFlag: 1,       // Satın alınabilir
    maliyetHesaplanacakFlag: 1,   // Maliyet hesaplansın
  };
}

/**
 * SKU'dan Koza stok kodu üret
 * Koza bazı özel karakterleri kabul etmeyebilir
 */
export function sanitizeStokKodu(sku: string): string {
  return sku
    .replace(/[^a-zA-Z0-9._-]/g, "_")  // Özel karakterleri _ yap
    .substring(0, 50)                   // Max 50 karakter
    .toUpperCase();
}

/**
 * Kategori mapping helper
 * Katana category → Koza kategori ağaç kodu
 */
export function mapCategoryToKozaKod(
  category: KatanaProduct["category"],
  defaultKod: string = "001"
): string {
  if (!category?.code) return defaultKod;
  return category.code;
}
