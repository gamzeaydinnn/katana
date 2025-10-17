using System.Text.RegularExpressions;
using Katana.Core.DTOs;

namespace Katana.Business.Validators;

public static class ProductValidator
{
    public static List<string> ValidateCreate(CreateProductDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.Add("Ürün adı gereklidir");
        else if (dto.Name.Length > 200)
            errors.Add("Ürün adı 200 karakterden uzun olamaz");

        if (string.IsNullOrWhiteSpace(dto.SKU))
            errors.Add("SKU gereklidir");
        else if (dto.SKU.Length > 50)
            errors.Add("SKU 50 karakterden uzun olamaz");

        if (dto.Price < 0)
            errors.Add("Fiyat 0'dan küçük olamaz");

        if (dto.Stock < 0)
            errors.Add("Stok 0'dan küçük olamaz");

        if (dto.CategoryId <= 0)
            errors.Add("Geçerli bir kategori seçiniz");

        if (!string.IsNullOrEmpty(dto.MainImageUrl) && dto.MainImageUrl.Length > 500)
            errors.Add("Görsel URL 500 karakterden uzun olamaz");

        if (!string.IsNullOrEmpty(dto.Description) && dto.Description.Length > 1000)
            errors.Add("Açıklama 1000 karakterden uzun olamaz");

        return errors;
    }

    public static List<string> ValidateUpdate(UpdateProductDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.Add("Ürün adı gereklidir");
        else if (dto.Name.Length > 200)
            errors.Add("Ürün adı 200 karakterden uzun olamaz");

        if (string.IsNullOrWhiteSpace(dto.SKU))
            errors.Add("SKU gereklidir");
        else if (dto.SKU.Length > 50)
            errors.Add("SKU 50 karakterden uzun olamaz");

        if (dto.Price < 0)
            errors.Add("Fiyat 0'dan küçük olamaz");

        if (dto.Stock < 0)
            errors.Add("Stok 0'dan küçük olamaz");

        if (dto.CategoryId <= 0)
            errors.Add("Geçerli bir kategori seçiniz");

        if (!string.IsNullOrEmpty(dto.MainImageUrl) && dto.MainImageUrl.Length > 500)
            errors.Add("Görsel URL 500 karakterden uzun olamaz");

        if (!string.IsNullOrEmpty(dto.Description) && dto.Description.Length > 1000)
            errors.Add("Açıklama 1000 karakterden uzun olamaz");

        return errors;
    }

    public static List<string> ValidateStock(int quantity)
    {
        var errors = new List<string>();

        if (quantity < 0)
            errors.Add("Stok miktarı 0'dan küçük olamaz");

        return errors;
    }
}
