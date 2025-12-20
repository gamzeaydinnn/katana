// features/integrations/luca-koza/cards/DepoMapper.ts

import { KozaStkDepo } from "./DepoKarti";

/**
 * Katana Location tipi
 * Katana API dokümanına göre doğru alan adları
 */
export interface KatanaLocation {
  id: number | string;
  name: string;
  legal_name?: string | null;
  address?: {
    line_1?: string | null; // Katana'da line_1 (line1 değil)
    line_2?: string | null; // Katana'da line_2 (line2 değil)
    city?: string | null;
    state?: string | null;
    zip?: string | null; // Katana'da zip
    country?: string | null;
  } | null;
  is_primary?: boolean;
  sales_allowed?: boolean;
  manufacturing_allowed?: boolean;
  purchase_allowed?: boolean;
  deleted_at?: string | null; // Katana'da aktiflik için deleted_at kullanılıyor
}

/**
 * String normalize et
 */
function norm(v?: string | null): string {
  return (v ?? "").trim().replace(/\s+/g, " ");
}

/**
 * Adres parçalarını birleştir
 */
function toAdresSerbest(a?: KatanaLocation["address"]): string {
  if (!a) return "";
  const parts = [a.line_1, a.line_2, a.zip, a.city, a.state, a.country]
    .map(norm)
    .filter(Boolean);
  return parts.join(", ");
}

/**
 * Koza depo kodu üretimi
 * Örnekler:
 *  - "KTN-171569"
 *  - "DEP-istanbul-merkez"
 */
export function generateKozaDepotCode(location: KatanaLocation): string {
  const rawId = location.id;
  const idStr = String(rawId ?? "").trim();

  // Opsiyon 1: Katana ID'den deterministik kod
  if (idStr) {
    return `KTN-${idStr}`;
  }

  // Opsiyon 2: Name'den slug oluştur
  const baseName = norm(location.name) || norm(location.legal_name);
  const slug =
    baseName
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-+|-+$/g, "") // baştaki/sondaki tireleri temizle
      .substring(0, 20) || "depo";

  return `DEP-${slug}`;
}

/**
 * Katana Location → Koza Depo Kartı dönüşümü
 * @param location Katana location objesi
 * @param defaultKategoriKod Koza depo kategori kodu (config'ten alınmalı)
 */
export function mapKatanaLocationToKozaDepo(
  location: KatanaLocation,
  defaultKategoriKod: string
): KozaStkDepo {
  const kod = generateKozaDepotCode(location);
  const tanim = norm(location.legal_name) || norm(location.name);

  return {
    kod,
    tanim,
    kategoriKod: defaultKategoriKod,
    // Luca MERKEZ DEPO için gerekli alanlar
    depoKategoriAgacId: 11356, // Luca'daki depo kategori ağacı ID
    sisDepoKategoriAgacKodu: "002", // MERKEZ DEPO kodu
    ulke: norm(location.address?.country) || undefined,
    il: norm(location.address?.city) || undefined,
    ilce: undefined, // Katana'da net ilçe alanı yok
    adresSerbest: toAdresSerbest(location.address) || undefined,
  };
}

/**
 * Katana aktiflik kontrolü
 * deleted_at doluysa "silinmiş" demektir
 */
export function isKatanaLocationActive(location: KatanaLocation): boolean {
  return !location.deleted_at;
}

/**
 * Aktif location'ları filtrele
 */
export function filterActiveLocations(
  locations: KatanaLocation[]
): KatanaLocation[] {
  return locations.filter(isKatanaLocationActive);
}
