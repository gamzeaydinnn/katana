Luca Koza API - Entegrasyon Dokümantasyonu

Not: Bu doküman, Katana ile Luca (Koza) arasındaki entegrasyonda kullanılacak REST servislerinin açıklamasını içerir.

1. Genel Bilgiler

LucaKOZA API, veri alım-satım işlemlerini JSON ile sağlamaktadır. Oturum yönetimi cookie/oturum nesneleri ile sağlanmaktadır. İstemci tarafı entegrasyon için cookie yönetimini gerçekleştirmelidir. LucaKOZA sisteminde oluşturulmuş olan organizasyona ait Admin kullanıcısı ile REST API üzerinde işlem sağlanacaktır.

DTO referansı (src/Katana.Core/DTOs/LucaDtos.cs):
- 3.2.1: LucaListTaxOfficesRequest
- 3.2.2: LucaListMeasurementUnitsRequest
- 3.2.3: LucaListCustomersRequest
- 3.2.4: LucaListSuppliersRequest
- 3.2.5: LucaListStockCardsRequest
- 3.2.7: LucaStockCardSuppliersRequest (stkSkart.skartId)
- 3.2.8: LucaListCustomerContactsRequest (finansalNesneId)
- 3.2.9: LucaListStockCardPriceListsRequest (stkSkart.skartId + tip)
- 3.2.10/11: LucaStockCardByIdRequest (alternatif OB / alternatif stok)
- 3.2.12: LucaListBanksRequest
- 3.2.13/14/15: LucaListCustomerAddressesRequest, LucaGetCustomerWorkingConditionsRequest, LucaListCustomerAuthorizedPersonsRequest
- 3.2.16: LucaListWarehousesRequest
- 3.2.17: LucaGetWarehouseStockRequest (cagirilanKart=depo, stkDepo.depoId)
- 3.2.18/20: LucaStockCardByIdRequest (maliyet, alım şartları)
- 3.2.19: LucaGetCustomerRiskRequest (gnlFinansalNesne.finansalNesneId) – servis varsayılanı query string
- 3.2.21: LucaListIrsaliyeRequest
- 3.2.22: Boş body + detayliListe query param (isteğe bağlı LucaListSalesOrdersRequest)
- 3.2.23: LucaListStockCategoriesRequest
- 3.2.24: LucaCreateInvoiceHeaderRequest + LucaCreateInvoiceDetailRequest
- 3.2.25/26: LucaCloseInvoiceRequest, LucaDeleteInvoiceRequest
- 3.2.27/28: LucaCreateIrsaliyeBaslikRequest, LucaDeleteIrsaliyeRequest
- 3.2.29/30: LucaCreateCustomerRequest, LucaCreateSupplierRequest
- 3.2.31: LucaCreateCariHareketRequest
- 3.2.32: LucaCreateCreditCardEntryRequest
- 3.2.33: LucaCreateStokKartiRequest
- 3.2.40: LucaCreateDshBaslikRequest

3.1 Authentication

Method: POST
URL: API_URL/Giris.do
Headers: Content-Type: application/json
Body (JSON):
{
  "orgCode": "ÜyeNumarası",
  "userName": "KullancıAdı (Admin olmalı)",
  "userPassword": "Admin şifresi"
}

Response (JSON):
{
  "code": 0, // 0: Başarılı, 99: Kullanıcı-Parola Bilgisi Yanlış!
  "message": "Dönüş mesajı"
}

Not: Rest servis üzerinde çalışılabilmesi için aynı zamanda Şirket/Şube seçimi yapılması zorunludur. Bu seçimden sonra yapılan çağrılar otomatik olarak Şirket/Şube üzerinden yapılacaktır.

Şirket/Şube listesi için:
Method: POST
URL: API_URL/YdlUserResponsibilityOrgSs.do
Headers: Content-Type: application/json
Body: boş JSON ({} veya body boş gönderilir)

Response: Liste şeklinde `ack` (Şirket/Şube Adı) ve `id` (Şirket/Şube Id) döner.

Şirket/Şube seçimi yapmak için:
Method: POST
URL: API_URL/GuncelleYtkSirketSubeDegistir.do
Headers: Content-Type: application/json
Body (JSON):
{
  "orgSirketSubeId": 12345 // ŞirketŞubeId (Long)
}

Response:
{
  "message": "Oturumda Çalıştığınız Şirket Şube Başarıyla Değiştirildi.",
  "sirketSubeAdi": "Şirket Şube Adı"
}

Bu aşama başarı ile geçildiği takdirde, bütün istekler bu şirket şubeye yapılacaktır.

3.2 Metodlar (Özet)

3.2.1 Vergi Dairesi Listesi
- Method: POST
- URL: API_URL/ListeleGnlVergiDairesi.do
- Headers: Content-Type: application/json
- No-Paging: true
- Body: boş gönderilirse sistemdeki bütün Vergi Dairelerini döner. Filtreleme için aşağıdaki gibi kullanılabilir:
  {
    "gnlVergiDairesi": {
      "tanim": "SİLİFKE",
      "kod": "33202"
    }
  }

3.2.2 Ölçü Birimi Listesi
- Method: POST
- URL: API_URL/ListeleGnlOlcumBirimi.do
- Headers: Content-Type: application/json
- No-Paging: true
- Body: boş gönderilirse sistemdeki tüm Ölçü Birimlerini döner. Filtreleme örneği:
  {
    "gnlOlcumBirimi": { "tanim": "gram" }
  }

3.2.3 Müşteri Kartları Listesi
- Method: POST
- URL: API_URL/ListeleFinMusteri.do
- Headers: Content-Type: application/json
- No-Paging: true
- Body: boş gönderilirse tüm müşteriler döner. Filtreleme (koda göre) örneği:
  {
    "finMusteri": {
      "gnlFinansalNesne": {
        "kodBas": "000.00000001",
        "kodBit": "000.00000005",
        "kodOp": "between"
      }
    }
  }

3.2.4 Tedarikçi Kartları Listesi
- Method: POST
- URL: API_URL/ListeleFinTedarikci.do
- Headers: Content-Type: application/json
- No-Paging: true
- Body: boş gönderilirse tüm tedarikçiler döner. Filtreleme örneği:
  {
    "finTedarikci": {
      "gnlFinansalNesne": {
        "kodBas": "0002",
        "kodBit": "0007",
        "kodOp": "between"
      }
    }
  }

3.2.5 Stok Kartları Listesi
- Method: POST
- URL: API_URL/ListeleStkSkart.do
- Headers: Content-Type: application/json
- No-Paging: true
- Body örneği (kod aralığıyla filtre):
  {
    "stkSkart": {
      "kodBas": "00004",
      "kodBit": "00004",
      "kodOp": "between"
    }
  }

3.2.6 Fatura Listesi
- Method: POST
- URL: API_URL/ListeleFtrSsFaturaBaslik.do
  veya detaylı için: API_URL/ListeleFtrSsFaturaBaslik.do?detayliListe=true
- Headers: Content-Type: application/json
- No-Paging: true
- Filtreleme parametreleri (örnek):
  {
    "ftrSsFaturaBaslik": {
      "gnlOrgSsBelge": {
        "belgeNoBas": 201800000047,
        "belgeNoBit": 201800000048,
        "belgeNoOp": "between",
        "belgeTarihiBas": "18/02/2017",
        "belgeTarihiBit": "18/02/2019",
        "belgeTarihiOp": "between"
      }
    },
    "parUstHareketTuru": 18,
    "parAltHareketTuru": 76
  }

Fatura response alanları (özet): ssFaturaBaslikId, belgeTarihi, vadeTarihi, belgeSeriNo, yuklemeTarihi, belgeTurTanim, belgeTurDetayTanim, cariKozaId, cariKartTip, kategoriliKod, cariTanim, cariAktif, vergiDairesi, vergiKimlikNo, serbestAdres, ilKodu, ilTanim, ilceKodu, ilceTanim, satisPersonel, skartId, stokKartTuru, stokKartKategoriliKod, stokKartAdi, miktar, olcumBirim, birimFiyat, hareketDovizCinsi, tutar, kdvOran, kdvTutar, tevkifatTutar, otvTutar, stopajTutar, netTutar, depoKodu, depoAdi

3.2.7 Temin Yerleri Listesi
- Method: POST
- URL: API_URL/ListeleStkSkartTeminYeri.do
- Headers: Content-Type: application/json
- No-Paging: true
- Parametre: "stkSkart": { "skartId": 60382 }

3.2.8 Cari İletişim Listesi
- Method: POST
- URL: API_URL/ListeleWSGnlSsIletisim.do
- Headers: Content-Type: application/json
- No-Paging: true
- Parametre: finansalNesneId (ör: 136686)

3.2.9 Stok Kartı Alış/Satış Fiyat Listesi
- Method: POST
- URL: API_URL/ListeleStkSkartFiyatListeleri.do
- Headers: Content-Type: application/json
- No-Paging: true
- Parametre: "stkSkart": { "skartId": 66573 }, "tip": "alis" veya "satis"

3.2.10 Stok Kartı Alternatif Ölçü Birimi Listesi
- Method: POST
- URL: API_URL/ListeleStkSkartAlternatifOb.do
- Headers: Content-Type: application/json
- No-Paging: true
- Parametre: "stkSkart": { "skartId": 66573 }

3.2.11 Stok Kartı Alternatif Stoklar
- Method: POST
- URL: API_URL/ListeleStkSkartAlternatif.do
- Headers: Content-Type: application/json
- No-Paging: true
- Parametre: "stkSkart": { "skartId": 66573 }

3.2.12 Banka Kartları Listesi
- Method: POST
- URL: API_URL/ListeleFinSsBanka.do
- Headers: Content-Type: application/json
- No-Paging: true
- Body boş gönderilirse tüm banka kartları döner. Filtre örneği:
  {
    "finSsBanka": {
      "gnlFinansalNesne": {
        "kodBas": "001",
        "kodBit": "002",
        "kodOp": "between"
      }
    }
  }

3.2.13 Cari Adres Listesi
- Method: POST
- URL: API_URL/ListeleWSGnlSsAdres.do
- Headers: Content-Type: application/json
- No-Paging: true
- Parametre: finansalNesneId (ör: 136686)

3.2.14 Cari Çalışma Koşulları
- Method: POST
- URL: API_URL/GetirFinCalismaKosul.do
- Headers: Content-Type: application/json
- Parametre: calismaKosulId (Zorunlu)

3.2.15 Cari Yetkili Kişiler
- Method: POST
- URL: API_URL/ListeleFinFinansalNesneYetkili.do
- Headers: Content-Type: application/json
- No-Paging: true
- Parametre: "gnlFinansalNesne": { "finansalNesneId": 137212 }

3.2.16 Depo Kartları Listesi
- Method: POST
- URL: API_URL/ListeleStkDepo.do
- Headers: Content-Type: application/json
- No-Paging: true
- Body boş gönderilirse tüm depo kartları döner. Filtre örneği:
  {
    "stkDepo": {
      "kodOp": "between",
      "kodBas": "0002",
      "kodBit": "0004"
    }
  }

3.2.17 Depo Kartları Eldeki Miktar Listesi
- Method: POST
- URL: API_URL/ListeleStkSsEldekiMiktar.do?cagirilanKart=depo&stkDepo.depoId={depoId}
- Headers: Content-Type: application/json
- No-Paging: true
- Örnek: API_URL/ListeleStkSsEldekiMiktar.do?cagirilanKart=depo&stkDepo.depoId=1451

Body ile gönderim (LucaGetWarehouseStockRequest):
{
  "cagirilanKart": "depo",
  "stkDepo": { "depoId": 1451 }
}

3.2.18 Stok Kartları Maliyet Bilgisi
- Method: POST
- URL: API_URL/ListeleStkSkartMaliyet.do
- Headers: Content-Type: application/json
- No-Paging: true
- Parametre: "stkSkart": { "skartId": 65433 }
- DTO: LucaStockCardByIdRequest
- JSON örneği:
{
  "stkSkart": { "skartId": 65433 }
}

3.2.19 Cari Risk Bilgileri
- Method: POST
- URL: API_URL/GetirFinRisk.do?gnlFinansalNesne.finansalNesneId={finansalNesneId}
- Headers: Content-Type: application/json
- No-Paging: true
- Örnek: API_URL/GetirFinRisk.do?gnlFinansalNesne.finansalNesneId=137246
- DTO: LucaGetCustomerRiskRequest (isteğe bağlı POST body kullanımı)
- JSON örneği:
{
  "gnlFinansalNesne": { "finansalNesneId": 137246 }
}

3.2.20 Stok Kartı Alım Şartları
- Method: POST
- URL: API_URL/ListeleStkSkartAlimSart.do
- Headers: Content-Type: application/json
- No-Paging: true
- Parametre: "stkSkart": { "skartId": 65433 }

3.2.21 İrsaliye Listesi
- Method: POST
- URL: API_URL/ListeleStkSsIrsaliyeBaslik.do
- Detaylı: API_URL/ListeleStkSsIrsaliyeBaslik.do?detayliListe=true
- Headers: Content-Type: application/json
- No-Paging: true

3.2.22 Satış Sipariş Listesi
- Method: POST
- URL: API_URL/ListeleStsSsSiparisBaslik.do
- Detaylı: API_URL/ListeleStsSsSiparisBaslik.do?detayliListe=true
- Headers: Content-Type: application/json
- No-Paging: true

3.2.23 Stok/Hizmet Kategori Listesi
- Method: POST
- URL: API_URL/ListeleStkSkartKategoriAgac.do
- Headers: Content-Type: application/json
- No-Paging: true
- Parametre örneği: { "kartTuru": 1 }

3.2.24 Fatura Ekleme
- Method: POST
- URL: API_URL/EkleFtrWsFaturaBaslik.do
- Headers: Content-Type: application/json
- DTO: LucaCreateInvoiceHeaderRequest (başlık) + LucaCreateInvoiceDetailRequest (detay)
- Body: Fatura başlık ve detay bilgileri (çok sayıda alan; özet:
  - belgeSer, belgeNo, belgeTarihi, duzenlemeSaati, vadeTarihi, belgeTakipNo, belgeAciklama
  - belgeTurDetayId, belgeAttributeNDeger/Ack, faturaTur, paraBirimKod, kurBedeli, yuklemeTarihi
  - babsFlag, kdvFlag, referansNo, tevkifatOran, tevkifatKod, detayList (fatura detayları)
  - musteriTedarikci, cariKodu, cariTanim, cariTip, cariKisaAd, cariYasalUnvan, vergiNo, vergiDairesi
  - adres, iletişim, kargoVknTckn, odemeTipi, gonderimTipi, siparisTarihi, siparisNo, irsaliyeBilgisiList, fhAttributeNDeger/Ack, earsivNo, efaturaNo
)

Fatura detay alanları: kartTuru, kartKodu, kartAdi, kartTipi, barkod, olcuBirimi, KdvOran, kartSatisKdvOran,
depoKodu, birimFiyat, miktar, tutar, kdvOran, iskontoOran*, otvOran, stopajOran, lotNo, aciklama, garantiSuresi,
uretimTarihi, shAttributeNDeger/Ack, konaklamaVergiOran
- Not: DTO'larda ExtraFields alanları, dokümandaki belgeAttribute*/fhAttribute* gibi ek başlık/satır alanlarını doğrudan JSON ile iletmek için tanımlıdır.

3.2.25 Fatura Kapama
- Method: POST
- URL: API_URL/EkleFtrWsFaturaKapama.do
- Headers: Content-Type: application/json
- Body: faturaId, belgeTurDetayId, belgeSeri, belgeNo, belgeTarih, vadeTarih, takipNo, aciklama, tutar, cariKod, cariTur, kurBedeli

Notlar: Fatura kapama işlemlerinde farklı senaryolara göre (Virman, Tahsilat Makbuzu, Kredi Kartı, Havale vb.) kullanılacak Cari Kart Tipleri ve kurallar bulunmaktadır; dokümanda belirtildiği şekilde gönderilmelidir.

3.2.26 Fatura Silme
- Method: POST
- URL: API_URL/SilFtrWsFaturaBaslik.do
- Headers: Content-Type: application/json
- Body: { "ssFaturaBaslikId": 111193 }

3.2.27 İrsaliye Ekleme
- Method: POST
- URL: API_URL/EkleStkWsIrsaliyeBaslik.do
- Headers: Content-Type: application/json
- Body: İrsaliye başlık ve detay bilgileri (benzer alanlar fatura ekleme ile)

3.2.28 İrsaliye Silme
- Method: POST
- URL: API_URL/SilStkWsIrsaliyeBaslik.do
- Headers: Content-Type: application/json
- Body: { "ssIrsaliyeBaslikId": 111193 }

3.2.29 Müşteri Kartı Ekleme
- Method: POST
- URL: API_URL/EkleFinMusteriWS.do
- Headers: Content-Type: application/json
- Body alanları (özet): tip, cariTipId, takipNoFlag, efaturaTuru, kategoriKod, kartKod (boş bırakılabilir), tanim, mutabakatMektubuGonderilecek, paraBirimKod, vergiNo, kisaAd, yasalUnvan, tcKimlikNo, ad, soyad, dogumTarihi(dd/mm/yyyy), mustahsil, tcUyruklu, vergiDairesiId, adresTipId, ulke, il, ilce, adresSerbest, iletisimTipId, iletisimTanim

3.2.30 Tedarikçi Kartı Ekleme
- Method: POST
- URL: API_URL/EkleFinTedarikciWS.do
- Headers: Content-Type: application/json
- Body: Aynı alanlar Müşteri Kartı Ekleme ile

3.2.31 Cari Hareket Ekleme
- Method: POST
- URL: API_URL/EkleFinCariHareketBaslikWS.do
- Headers: Content-Type: application/json
- Body: Cari hareket başlık bilgileri (belgeSeri, belgeNo, belgeTarihi, duzenlemeSaati, vadeTarihi, belgeTakipNo, belgeAciklama, belgeTurDetayId), başlık alanları (cariTuru, paraBirimKod, cariKodu), detayList (kartTuru, kartKodu, avansFlag, tutar, aciklama)

3.2.32 Kredi Kartı Giriş Fişi Ekleme
- Method: POST
- URL: API_URL/EkleFinKrediKartiWS.do
- Headers: Content-Type: application/json
- Body: Kredi kartı giriş fişi bilgileri (belge başlık/detay alanları)

3.2.33 Diğer Metodlar ve Notlar
- Doküman içinde listelenen tüm metodlar POST ile JSON body alır. Bazı metodlar paging olmadan tüm kayıtları dönebilir (No-Paging: true).
- Filtreleme alanları genelde nesne içindeki `kod`, `tanim`, `bas`, `bit`, `op` gibi parametreler ile sağlanır; tarih ve aralık filtrelerinde `between` operatörü kullanılır.

3.2.24 - 3.2.33 arası fatura/irsaliye, stok, cari ve finansal işlemler için detaylı gönderim formatları dokümanın ilgili bölümlerinde örnek JSON ile verilmiştir. Entegrasyon yapacak sistemde aşağıdaki adımlar takip edilmelidir:

- 1) Oturum açma (`Giris.do`) ile token/cookie alınır.
- 2) Şirket/Şube listesi alınır (`YdlUserResponsibilityOrgSs.do`) ve uygun `id` ile şube seçimi yapılır (`GuncelleYtkSirketSubeDegistir.do`).
- 3) İstenilen veri listesini almak için ilgili `Listele*` metoduna POST ile JSON (genelde boş `{}` veya filtre nesnesi) gönderilir.
- 4) Kayıt oluşturma için `Ekle*` metodları (fatura, irsaliye, cari, tedarikçi vb.) kullanılır.
- 5) Her işlem sonrası dönen response `code` ve `message` alanları kontrol edilmelidir.

Ek: Eğer entegrasyon sırasında belirli alanların kod listesine ihtiyaç duyulursa (örneğin Belge Tür ID'leri), paket içinde referans dosyası veya ek dökümanlar bulunmalıdır (örneğin "Luca Koza Fatura Fatura Belge ve Alt Belge Tür ID.xlsx").

-- Son
