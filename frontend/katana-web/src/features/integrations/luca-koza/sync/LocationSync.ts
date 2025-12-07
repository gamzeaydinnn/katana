// features/integrations/luca-koza/sync/LocationSync.ts

import {
    filterActiveLocations,
    KatanaLocation,
    mapKatanaLocationToKozaDepo,
} from "../cards/DepoMapper";
import { depoService } from "../cards/DepoService";
import { locationSyncLogger, LogContext } from "./LocationSyncLogger";

/**
 * Location senkronizasyon sonucu
 */
export interface LocationSyncResult {
  katanaId: number | string;
  katanaName: string;
  kozaKod: string;
  kozaDepoId?: number;
  status: "created" | "existing" | "error";
  error?: string;
}

/**
 * Location senkronizasyon konfigürasyonu
 */
export interface LocationSyncConfig {
  defaultKategoriKod: string;  // Koza depo kategori kodu
  skipInactive?: boolean;       // Silinen location'ları atla (default: true)
}

/**
 * Katana Location → Koza Depo senkronizasyonu
 */
export class LocationSyncService {
  private config: LocationSyncConfig;

  constructor(config: LocationSyncConfig) {
    this.config = {
      skipInactive: true,
      ...config,
    };
  }

  /**
   * Tek bir location'ı senkronize et
   */
  async syncLocation(location: KatanaLocation, context?: LogContext): Promise<LocationSyncResult> {
    const syncContext = context || locationSyncLogger.createContext('syncLocation');
    const startTime = Date.now();

    const kozaDepo = mapKatanaLocationToKozaDepo(
      location,
      this.config.defaultKategoriKod
    );

    // Log location attempt
    locationSyncLogger.logLocationAttempt(syncContext, location, kozaDepo);

    try {
      const result = await depoService.getirVeyaOlustur(kozaDepo);
      const duration = Date.now() - startTime;

      // Log success
      locationSyncLogger.logLocationSuccess(syncContext, location, result, duration);

      return {
        katanaId: location.id,
        katanaName: location.name,
        kozaKod: kozaDepo.kod,
        kozaDepoId: result.depoId,
        status: result.depoId ? "existing" : "created",
      };
    } catch (error) {
      const duration = Date.now() - startTime;

      // Log error
      locationSyncLogger.logLocationError(syncContext, location, error, duration);

      return {
        katanaId: location.id,
        katanaName: location.name,
        kozaKod: kozaDepo.kod,
        status: "error",
        error: error instanceof Error ? error.message : "Bilinmeyen hata",
      };
    }
  }

  /**
   * Birden fazla location'ı senkronize et
   */
  async syncLocations(locations: KatanaLocation[]): Promise<LocationSyncResult[]> {
    const batchContext = locationSyncLogger.createContext('syncLocations');

    // Aktif olmayanları filtrele (opsiyonel)
    const toSync = this.config.skipInactive
      ? filterActiveLocations(locations)
      : locations;

    // Log batch start
    locationSyncLogger.logBatchStart(batchContext, toSync.length);

    const results: LocationSyncResult[] = [];
    const batchStartTime = Date.now();

    for (let index = 0; index < toSync.length; index++) {
      const location = toSync[index];
      console.log(`[Depot Sync] Progress: ${index + 1}/${toSync.length} - Processing location: ${location.code}`);
      
      const result = await this.syncLocation(location, batchContext);
      results.push(result);

      // Rate limiting - Koza API'ye fazla yük bindirmemek için
      await this.delay(100);
    }

    // Sonuç özeti
    const created = results.filter((r) => r.status === "created").length;
    const existing = results.filter((r) => r.status === "existing").length;
    const errors = results.filter((r) => r.status === "error").length;
    const totalTime = Date.now() - batchStartTime;

    // Log batch completion
    locationSyncLogger.logBatchComplete(batchContext, {
      successful: created + existing,
      failed: errors,
      skipped: locations.length - toSync.length,
      totalTime,
    });

    return results;
  }

  /**
   * Depo mapping'i oluştur (Katana ID → Koza depoId)
   * Eldeki miktar endpoint'i için depoId gerekli
   */
  buildDepoIdMapping(results: LocationSyncResult[]): Map<string | number, number> {
    const mapping = new Map<string | number, number>();

    for (const result of results) {
      if (result.kozaDepoId) {
        mapping.set(result.katanaId, result.kozaDepoId);
      }
    }

    return mapping;
  }

  /**
   * Depo kod mapping'i oluştur (Katana ID → Koza kod)
   * Depo transferi için girisDepoKodu/cikisDepoKodu gerekli
   */
  buildDepoKodMapping(results: LocationSyncResult[]): Map<string | number, string> {
    const mapping = new Map<string | number, string>();

    for (const result of results) {
      mapping.set(result.katanaId, result.kozaKod);
    }

    return mapping;
  }

  private delay(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
}

/**
 * Kullanım örneği:
 * 
 * const syncService = new LocationSyncService({
 *   defaultKategoriKod: "GENEL", // Koza'daki depo kategorisi
 * });
 * 
 * // Katana'dan location'ları çek
 * const katanaLocations = await fetchKatanaLocations();
 * 
 * // Senkronize et
 * const results = await syncService.syncLocations(katanaLocations);
 * 
 * // Mapping'leri oluştur
 * const depoIdMap = syncService.buildDepoIdMapping(results);
 * const depoKodMap = syncService.buildDepoKodMapping(results);
 * 
 * // Eldeki miktar için depoId kullan
 * const depoId = depoIdMap.get(katanaLocationId);
 * 
 * // Depo transferi için depoKodu kullan
 * const girisDepoKodu = depoKodMap.get(targetLocationId);
 * const cikisDepoKodu = depoKodMap.get(sourceLocationId);
 */
