// features/integrations/luca-koza/cards/DepoService.ts

import { kozaAPI } from "../../../../services/api";
import {
  DepoEkleRequest,
  DepoEkleResponse,
  KozaStkDepo
} from "./DepoKarti";

/**
 * Koza Depo Kartı Servisi
 * api.ts üzerinden backend'e bağlanır
 * Backend proxy ile Koza API'ye güvenli erişim sağlanır
 * 
 * NOT: list() artık LOCAL DB'den pagination ile dönüyor (timeout yok)
 * Sync yapmak için kozaAPI.depots.sync() kullanın
 */
export class DepoService {
  /**
   * Depoları LOCAL DB'den listele (Koza API'ye GİTMEZ!)
   * Backend: GET /api/admin/koza/depots?page=1&pageSize=100
   */
  async listele(page = 1, pageSize = 100): Promise<KozaStkDepo[]> {
    try {
      const response = await kozaAPI.depots.list({ page, pageSize });
      
      // Response format: { data: [], pagination: {} } veya doğrudan array
      if (response && typeof response === "object" && "data" in response) {
        const data = (response as any).data;
        return Array.isArray(data) ? data : [];
      }
      
      return Array.isArray(response) ? response : [];
    } catch (error) {
      console.error("Depo listeleme hatası:", error);
      return [];
    }
  }

  /**
   * Koza'dan tüm depoları çek ve LOCAL DB'ye senkronize et
   * Backend: POST /api/admin/koza/depots/sync?batchSize=50
   */
  async sync(batchSize = 50): Promise<{ success: boolean; message: string; stats?: any }> {
    try {
      console.log(`[Depot Sync] Starting batch sync with batch size: ${batchSize}`);
      const response = await kozaAPI.depots.sync(batchSize) as { success?: boolean; message?: string; stats?: any };
      
      if (response?.success) {
        console.log('[Depot Sync] Sync completed successfully', {
          message: response.message,
          stats: response.stats
        });
      } else {
        console.warn('[Depot Sync] Sync returned with non-success status', response);
      }
      
      return {
        success: response?.success ?? false,
        message: response?.message ?? "Bilinmeyen hata",
        stats: response?.stats
      };
    } catch (error) {
      console.error('[Depot Sync] Batch sync failed:', {
        error: error instanceof Error ? error.message : String(error),
        errorType: error instanceof Error ? error.constructor.name : typeof error
      });
      return {
        success: false,
        message: error instanceof Error ? error.message : "Depo sync hatası",
      };
    }
  }

  /**
   * Depo koduna göre tek depo getir
   */
  async getirByKod(depoKodu: string): Promise<KozaStkDepo | null> {
    const all = await this.listele();
    return all.find((d) => d.kod === depoKodu) ?? null;
  }

  /**
   * Depo ID'sine göre tek depo getir
   */
  async getirById(depoId: number): Promise<KozaStkDepo | null> {
    const all = await this.listele();
    return all.find((d) => d.depoId === depoId) ?? null;
  }

  /**
   * Depo var mı kontrol et
   */
  async varMi(depoKodu: string): Promise<boolean> {
    const depo = await this.getirByKod(depoKodu);
    return depo !== null;
  }

  /**
   * Yeni depo ekle
   * Backend: POST /api/admin/koza/depots/create
   */
  async ekle(req: DepoEkleRequest): Promise<DepoEkleResponse> {
    const depoKod = req.stkDepo?.kod || 'unknown';
    
    try {
      console.log('[Depot Sync] Request:', {
        kod: depoKod,
        tanim: req.stkDepo?.tanim,
        payload: JSON.stringify(req, null, 2)
      });
      
      const response = await kozaAPI.depots.create(req) as DepoEkleResponse;
      
      if (response && !response.error) {
        console.log('[Depot Sync] Success:', {
          kod: depoKod,
          responseId: response.depoId,
          message: response.message
        });
      } else {
        console.warn('[Depot Sync] Failed:', {
          kod: depoKod,
          errorMessage: response?.message
        });
      }
      
      return response ?? { error: true, message: "Bilinmeyen hata" };
    } catch (error: any) {
      console.error('[Depot Sync] Failed:', {
        kod: depoKod,
        status: error.response?.status,
        statusText: error.response?.statusText,
        errorData: error.response?.data,
        headers: error.response?.headers,
        requestPayload: req,
        errorMessage: error instanceof Error ? error.message : String(error)
      });
      return {
        error: true,
        message: error instanceof Error ? error.message : "Depo ekleme hatası",
      };
    }
  }

  /**
   * Depo yoksa oluştur, varsa mevcut olanı döndür
   * @returns Mevcut veya yeni oluşturulan depo (depoId dahil)
   */
  async getirVeyaOlustur(depo: KozaStkDepo): Promise<KozaStkDepo> {
    // Önce var mı kontrol et
    const mevcut = await this.getirByKod(depo.kod);
    if (mevcut) {
      console.log('[Depot Sync] Depot already exists, skipping creation:', {
        kod: depo.kod,
        depoId: mevcut.depoId,
        tanim: mevcut.tanim
      });
      return mevcut;
    }

    // Yoksa oluştur
    console.log('[Depot Sync] Creating new depot:', {
      kod: depo.kod,
      tanim: depo.tanim,
      kategoriKod: depo.kategoriKod
    });
    const createRes = await this.ekle({ stkDepo: depo });

    if (createRes?.error) {
      throw new Error(createRes.message || "Koza depo oluşturma hatası");
    }

    // depoId vs. eldeki miktar için lazım → tekrar listeleyip depoId yakala
    const yeni = await this.getirByKod(depo.kod);
    if (!yeni) {
      console.warn('[Depot Sync] Depot created but not found in list:', {
        kod: depo.kod
      });
      return depo;
    }

    console.log('[Depot Sync] Depot created successfully:', {
      kod: depo.kod,
      depoId: yeni.depoId,
      tanim: yeni.tanim
    });
    return yeni;
  }

  /**
   * Birden fazla depoyu senkronize et
   */
  async topluSenkronize(depolar: KozaStkDepo[]): Promise<Map<string, KozaStkDepo>> {
    const sonuclar = new Map<string, KozaStkDepo>();
    const totalDepots = depolar.length;

    for (let index = 0; index < depolar.length; index++) {
      const depo = depolar[index];
      console.log(`[Depot Sync] Progress: ${index + 1}/${totalDepots} - Processing depot: ${depo.kod}`);
      
      try {
        const sonuc = await this.getirVeyaOlustur(depo);
        sonuclar.set(depo.kod, sonuc);
      } catch (error) {
        console.error(`[Depot Sync] Synchronization failed for depot ${depo.kod} (${index + 1}/${totalDepots}):`, error);
      }
    }

    console.log('[Depot Sync] Batch synchronization completed:', {
      totalDepots,
      successfulCount: sonuclar.size,
      failedCount: totalDepots - sonuclar.size
    });
    
    return sonuclar;
  }
}

// Singleton instance
export const depoService = new DepoService();
