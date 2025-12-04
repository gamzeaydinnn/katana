// features/integrations/luca-koza/cards/DepoKarti.ts

/**
 * Koza Stok Depo Kartı
 * Endpoint'ler: ListeleStkDepo.do, EkleStkWsDepo.do
 */
export interface KozaStkDepo {
  depoId?: number; // Listele'den gelebilir
  kod: string; // depoKodu (örn: "001")
  tanim: string; // depoAdi (örn: "MERKEZ DEPO")
  kategoriKod: string; // Koza depo kategori kodu
  depoKategoriAgacId?: number; // Luca depo kategori ağacı ID (11356)
  sisDepoKategoriAgacKodu?: string; // Luca sistem depo kodu (001)
  ulke?: string;
  il?: string;
  ilce?: string;
  adresSerbest?: string;
}

/** Luca MERKEZ DEPO varsayılan değerleri */
export const LUCA_MERKEZ_DEPO = {
  kod: "002",
  tanim: "MERKEZ DEPO",
  depoKategoriAgacId: 11356,
  sisDepoKategoriAgacKodu: "002",
} as const;

/**
 * Depo Listeleme Request
 * - Boş object {} → tüm depolar
 * - stkDepo filtresi → between sorgusu
 */
export type DepoListeRequest =
  | Record<string, never> // tüm depolar için boş object
  | {
      stkDepo: {
        kodOp: "between";
        kodBas: string;
        kodBit: string;
      };
    };

/**
 * Depo Listeleme Response
 * Koza response alanı değişken olabiliyor
 */
export interface DepoListeResponse {
  depolar?: KozaStkDepo[];
  stkDepoListesi?: KozaStkDepo[];
  error?: boolean;
  message?: string;
}

/**
 * Depo Ekleme Request
 * Koza'nın beklediği format: { stkDepo: { kod, tanim, kategoriKod, ... } }
 */
export interface DepoEkleRequest {
  stkDepo: KozaStkDepo;
}

/**
 * Depo Ekleme Response
 */
export interface DepoEkleResponse {
  error?: boolean;
  message?: string;
  depoId?: number;
}
