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
    line_1?: string | null;    // Katana'da line_1 (line1 değil)
    line_2?: string | null;    // Katana'da line_2 (line2 değil)
    city?: string | null;
    state?: string | null;
    zip?: string | null;       // Katana'da zip
    country?: string | null;
  } | null;
  is_primary?: boolean;
  sales_allowed?: boolean;
  manufacturing_allowed?: boolean;
  purchase_allowed?: boolean;
  deleted_at?: string | null;  // Katana'da aktiflik için deleted_at kullanılıyor
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
  const parts = [
    a.line_1,
    a.line_2,
    a.zip,
    a.city,
    a.state,
    a.country
  ].map(norm).filter(Boolean);
  return parts.join(", ");
}

/**
 * Depo kodu stratejisi:
 * - id numeric ise 4 hane pad (0002 gibi)
 * - id string ise deterministic kısa bir kod üret (Koza formatına göre)
 */
function makeDepoKodu(id: number | string): string {
  if (typeof id === "number" || /^\d+$/.test(String(id))) {
    return String(id).padStart(4, "0");
  }
  // string id fallback (ör: "loc_abc123") → "LOC_ABC123" (max 20)
  return norm(String(id))
    .toUpperCase()
    .replace(/[^A-Z0-9]/g, "_")
    .slice(0, 20);
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
  const kod = makeDepoKodu(location.id);
  const tanim = norm(location.legal_name) || norm(location.name);

  return {
    kod,
    tanim,
    kategoriKod: defaultKategoriKod,
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
export function filterActiveLocations(locations: KatanaLocation[]): KatanaLocation[] {
  return locations.filter(isKatanaLocationActive);
}
