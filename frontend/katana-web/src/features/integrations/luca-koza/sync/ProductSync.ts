// features/integrations/luca-koza/sync/ProductSync.ts

import {
    KatanaProduct,
    mapKatanaProductToKozaStokKarti,
} from "../cards/StokMapper";
import { stokService } from "../cards/StokService";
import { KOZA_CONFIG } from "../config";

/**
 * Product senkronizasyon sonucu
 */
export interface ProductSyncResult {
  katanaId: number | string;
  katanaName: string;
  katanaSku: string;
  kozaKod: string;
  kozaStokKartId?: number;
  status: "created" | "existing" | "error";
  error?: string;
}

/**
 * Product senkronizasyon konfigürasyonu
 */
export interface ProductSyncConfig {
  kategoriAgacKod?: string;     // Kategori kodu (default: "001")
  olcumBirimiId?: number;       // Ölçüm birimi ID (default: 1 = Adet)
  kartTuru?: number;            // Kart türü (default: 1 = Ürün)
  kartTipi?: number;            // Kart tipi (default: 1 = Normal)
  skipDeleted?: boolean;        // Silinen ürünleri atla (default: true)
}

/**
 * Katana Product → Koza Stok Kartı senkronizasyonu
 */
export class ProductSyncService {
  private config: Required<ProductSyncConfig>;

  constructor(config?: ProductSyncConfig) {
    this.config = {
      kategoriAgacKod: config?.kategoriAgacKod ?? KOZA_CONFIG.STOCK_DEFAULTS.KATEGORI_AGAC_KOD,
      olcumBirimiId: config?.olcumBirimiId ?? KOZA_CONFIG.STOCK_DEFAULTS.OLCUM_BIRIMI_ID,
      kartTuru: config?.kartTuru ?? KOZA_CONFIG.STOCK_DEFAULTS.KART_TURU,
      kartTipi: config?.kartTipi ?? KOZA_CONFIG.STOCK_DEFAULTS.KART_TIPI,
      skipDeleted: config?.skipDeleted ?? true,
    };
  }

  /**
   * Tek bir product'ı senkronize et
   */
  async syncProduct(product: KatanaProduct): Promise<ProductSyncResult> {
    const kozaStokKarti = mapKatanaProductToKozaStokKarti(product, {
      kategoriAgacKod: this.config.kategoriAgacKod,
      olcumBirimiId: this.config.olcumBirimiId,
      kartTuru: this.config.kartTuru,
      kartTipi: this.config.kartTipi,
    });

    try {
      const result = await stokService.getirVeyaOlustur(kozaStokKarti);

      return {
        katanaId: product.id,
        katanaName: product.name,
        katanaSku: product.sku,
        kozaKod: kozaStokKarti.kartKodu,
        kozaStokKartId: result.stokKartId,
        status: result.stokKartId ? "existing" : "created",
      };
    } catch (error) {
      return {
        katanaId: product.id,
        katanaName: product.name,
        katanaSku: product.sku,
        kozaKod: kozaStokKarti.kartKodu,
        status: "error",
        error: error instanceof Error ? error.message : "Bilinmeyen hata",
      };
    }
  }

  /**
   * Birden fazla product'ı senkronize et
   */
  async syncProducts(products: KatanaProduct[]): Promise<ProductSyncResult[]> {
    // Silinen ürünleri filtrele (opsiyonel)
    const toSync = this.config.skipDeleted
      ? products.filter((p) => !p.deletedAt)
      : products;

    console.log(`${toSync.length} product senkronize edilecek...`);

    const results: ProductSyncResult[] = [];

    for (const product of toSync) {
      const result = await this.syncProduct(product);
      results.push(result);

      // Rate limiting
      await this.delay(KOZA_CONFIG.SYNC.DELAY_BETWEEN_CALLS);
    }

    // Sonuç özeti
    const created = results.filter((r) => r.status === "created").length;
    const existing = results.filter((r) => r.status === "existing").length;
    const errors = results.filter((r) => r.status === "error").length;

    console.log(
      `Product senkronizasyon tamamlandı: ${created} oluşturuldu, ${existing} mevcut, ${errors} hata`
    );

    return results;
  }

  /**
   * Stok kartı ID mapping'i oluştur (Katana Product ID → Koza stokKartId)
   */
  buildStokKartIdMapping(results: ProductSyncResult[]): Map<string | number, number> {
    const mapping = new Map<string | number, number>();

    for (const result of results) {
      if (result.kozaStokKartId) {
        mapping.set(result.katanaId, result.kozaStokKartId);
      }
    }

    return mapping;
  }

  /**
   * Stok kartı kod mapping'i oluştur (Katana SKU → Koza kartKodu)
   */
  buildStokKartKodMapping(results: ProductSyncResult[]): Map<string, string> {
    const mapping = new Map<string, string>();

    for (const result of results) {
      mapping.set(result.katanaSku, result.kozaKod);
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
 * const syncService = new ProductSyncService({
 *   kategoriAgacKod: "001",
 *   olcumBirimiId: 1,  // Adet
 * });
 * 
 * // Katana'dan product'ları çek
 * const katanaProducts = await fetchKatanaProducts();
 * 
 * // Senkronize et
 * const results = await syncService.syncProducts(katanaProducts);
 * 
 * // Mapping'leri oluştur
 * const stokKartIdMap = syncService.buildStokKartIdMapping(results);
 * const stokKartKodMap = syncService.buildStokKartKodMapping(results);
 */
