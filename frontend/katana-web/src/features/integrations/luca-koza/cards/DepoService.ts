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
      const response = await kozaAPI.depots.sync(batchSize);
      return response as { success: boolean; message: string; stats?: any };
    } catch (error) {
      console.error("Depo sync hatası:", error);
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
    try {
      const response = await kozaAPI.depots.create(req);
      return response ?? { error: true, message: "Bilinmeyen hata" };
    } catch (error) {
      console.error("Depo ekleme hatası:", error);
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
      console.log(`Depo zaten mevcut: ${depo.kod} (depoId: ${mevcut.depoId})`);
      return mevcut;
    }

    // Yoksa oluştur
    console.log(`Yeni depo oluşturuluyor: ${depo.kod}`);
    const createRes = await this.ekle({ stkDepo: depo });

    if (createRes?.error) {
      throw new Error(createRes.message || "Koza depo oluşturma hatası");
    }

    // depoId vs. eldeki miktar için lazım → tekrar listeleyip depoId yakala
    const yeni = await this.getirByKod(depo.kod);
    if (!yeni) {
      console.warn(`Depo oluşturuldu ama listede bulunamadı: ${depo.kod}`);
      return depo;
    }

    console.log(`Depo başarıyla oluşturuldu: ${depo.kod} (depoId: ${yeni.depoId})`);
    return yeni;
  }

  /**
   * Birden fazla depoyu senkronize et
   */
  async topluSenkronize(depolar: KozaStkDepo[]): Promise<Map<string, KozaStkDepo>> {
    const sonuclar = new Map<string, KozaStkDepo>();

    for (const depo of depolar) {
      try {
        const sonuc = await this.getirVeyaOlustur(depo);
        sonuclar.set(depo.kod, sonuc);
      } catch (error) {
        console.error(`Depo senkronizasyon hatası (${depo.kod}):`, error);
      }
    }

    return sonuclar;
  }
}

// Singleton instance
export const depoService = new DepoService();
