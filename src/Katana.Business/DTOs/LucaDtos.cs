using System;

namespace Katana.Business.DTOs;

public class LucaKozaProductDto
{
    public string Code { get; set; } = "";          // kartKodu
    public string Name { get; set; } = "";          // kartAdi
    public string? Category { get; set; }             // kategoriAgacKod veya kategori adı
    public string? Uom { get; set; }                  // ölçü birimi (opsiyonel)
}
