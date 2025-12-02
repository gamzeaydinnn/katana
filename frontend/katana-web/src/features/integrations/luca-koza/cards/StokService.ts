// features/integrations/luca-koza/cards/StokService.ts

import { kozaAPI } from "../../../../services/api";
import {
    KozaStokKarti,
    StokKartiEkleRequest,
    StokKartiEkleResponse
} from "./StokKarti";

/**
 * Koza Stok Kartı Servisi
 * api.ts üzerinden backend'e bağlanır
 * Backend proxy ile Koza API'ye güvenli erişim sağlanır
 */
export class StokService {
  /**
   * Stok kartlarını listele
   * Backend: GET /api/admin/koza/stocks
   */
  async listele(): Promise<KozaStokKarti[]> {
    try {
      const response = await kozaAPI.stockCards.list();
      return Array.isArray(response) ? response : [];
    } catch (error) {
      console.error("Stok kartı listeleme hatası:", error);
      return [];
    }
  }

  /**
   * Stok koduna göre tek stok kartı getir
   */
  async getirByKod(stokKodu: string): Promise<KozaStokKarti | null> {
    const all = await this.listele();
    return all.find((s) => s.kartKodu === stokKodu) ?? null;
  }

  /**
   * Stok ID'sine göre tek stok kartı getir
   */
  async getirById(stokKartId: number): Promise<KozaStokKarti | null> {
    const all = await this.listele();
    return all.find((s) => s.stokKartId === stokKartId) ?? null;
  }

  /**
   * Stok kartı var mı kontrol et
   */
  async varMi(stokKodu: string): Promise<boolean> {
    const stok = await this.getirByKod(stokKodu);
    return stok !== null;
  }

  /**
   * Yeni stok kartı ekle
   * Backend: POST /api/admin/koza/stocks/create
   */
  async ekle(req: StokKartiEkleRequest): Promise<StokKartiEkleResponse> {
    try {
      const response = await kozaAPI.stockCards.create(req);
      return response ?? { error: true, message: "Bilinmeyen hata" };
    } catch (error) {
      console.error("Stok kartı ekleme hatası:", error);
      return {
        error: true,
        message: error instanceof Error ? error.message : "Stok kartı ekleme hatası",
      };
    }
  }

  /**
   * Stok kartı yoksa oluştur, varsa mevcut olanı döndür
   * @returns Mevcut veya yeni oluşturulan stok kartı
   */
  async getirVeyaOlustur(stokKarti: KozaStokKarti): Promise<KozaStokKarti> {
    // Önce var mı kontrol et
    const mevcut = await this.getirByKod(stokKarti.kartKodu);
    if (mevcut) {
      console.log(
        `Stok kartı zaten mevcut: ${stokKarti.kartKodu} (ID: ${mevcut.stokKartId})`
      );
      return mevcut;
    }

    // Yoksa oluştur
    console.log(`Yeni stok kartı oluşturuluyor: ${stokKarti.kartKodu}`);
    const createRes = await this.ekle({ stkKart: stokKarti });

    if (createRes?.error) {
      throw new Error(createRes.message || "Koza stok kartı oluşturma hatası");
    }

    // Tekrar listeleyip ID yakala
    const yeni = await this.getirByKod(stokKarti.kartKodu);
    if (!yeni) {
      console.warn(
        `Stok kartı oluşturuldu ama listede bulunamadı: ${stokKarti.kartKodu}`
      );
      return stokKarti;
    }

    console.log(
      `Stok kartı başarıyla oluşturuldu: ${stokKarti.kartKodu} (ID: ${yeni.stokKartId})`
    );
    return yeni;
  }

  /**
   * Birden fazla stok kartını senkronize et
   */
  async topluSenkronize(
    stokKartlari: KozaStokKarti[]
  ): Promise<Map<string, KozaStokKarti>> {
    const sonuclar = new Map<string, KozaStokKarti>();

    for (const stok of stokKartlari) {
      try {
        const sonuc = await this.getirVeyaOlustur(stok);
        sonuclar.set(stok.kartKodu, sonuc);
      } catch (error) {
        console.error(`Stok kartı senkronizasyon hatası (${stok.kartKodu}):`, error);
      }
    }

    return sonuclar;
  }
}

// Singleton instance
export const stokService = new StokService();
