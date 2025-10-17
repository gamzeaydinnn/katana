using System;


namespace Katana.Data.Configuration;

    public class KatanaApiSettings
    {
        // Ana API adresi (değiştirme, Katana'nın resmi API URL'si)
        public string BaseUrl { get; set; } = "https://api.katanamrp.com/v1/";

        // Katana hesabından aldığın API anahtarı
        public string ApiKey { get; set; } = "ed8c38d1-4015-45e5-9c28-381d3fe148b6";

        // Eğer Katana kullanıcı adı/şifre ile doğrulama kullanıyorsan (çoğu durumda gerekmez)
        public bool UseBasicAuth { get; set; } = false;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int MaxRetryAttempts { get; set; } = 3;

        // Zaman aşımı (isteklerin 30 saniyede yanıt dönmezse iptal olur)
        public int TimeoutSeconds { get; set; } = 30;

        // Katana API içindeki alt endpoint adresleri
        public KatanaApiEndpoints Endpoints { get; set; } = new();
    }

    public class KatanaApiEndpoints
    {
        public string Products { get; set; } = "products";
        public string Stock { get; set; } = "stock-movements";
        public string Invoices { get; set; } = "sales-orders";
        public string Health { get; set; } = "health";
          public string Customers { get; set; } = "/api/customers";
    }


