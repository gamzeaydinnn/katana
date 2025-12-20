using System.Text.RegularExpressions;
using Katana.Core.DTOs;

namespace Katana.Business.Validators;

public static class CustomerValidator
{
    public static List<string> ValidateCreate(CreateCustomerDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.TaxNo))
            errors.Add("Vergi/TC numarası gereklidir");
        else if (dto.TaxNo.Length > 11)
            errors.Add("Vergi/TC numarası 11 karakterden uzun olamaz");
        else
        {
            // "U" ile başlayanlar TC Kimlik No olarak kabul edilir
            if (dto.TaxNo.StartsWith("U", StringComparison.OrdinalIgnoreCase))
            {
                string tcNo = dto.TaxNo.Substring(1); // U'yu çıkar
                
                if ((tcNo.Length < 8 || tcNo.Length > 11) || !IsNumeric(tcNo))
                    errors.Add($"Geçersiz TC Kimlik No formatı: {dto.TaxNo} (U + 8-11 rakam olmalı)");
                else if (tcNo.Length == 11 && !ValidateTurkishId(tcNo))
                    errors.Add("Geçersiz TC Kimlik No");
            }
            else if (!Regex.IsMatch(dto.TaxNo, @"^\d{10,11}$"))
            {
                errors.Add("Geçerli bir vergi numarası giriniz (10-11 haneli rakam)");
            }
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
            errors.Add("Müşteri ünvanı gereklidir");
        else if (dto.Title.Length > 200)
            errors.Add("Ünvan 200 karakterden uzun olamaz");

        if (!string.IsNullOrEmpty(dto.ContactPerson) && dto.ContactPerson.Length > 100)
            errors.Add("İletişim kişisi 100 karakterden uzun olamaz");

        if (!string.IsNullOrEmpty(dto.Phone) && dto.Phone.Length > 20)
            errors.Add("Telefon 20 karakterden uzun olamaz");

        if (!string.IsNullOrEmpty(dto.Email))
        {
            if (dto.Email.Length > 100)
                errors.Add("Email 100 karakterden uzun olamaz");
            else if (!Regex.IsMatch(dto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors.Add("Geçerli bir email adresi giriniz");
        }

        if (dto.CreditLimit.HasValue && dto.CreditLimit.Value < 0)
            errors.Add("Kredi limiti 0'dan küçük olamaz");

        return errors;
    }

    public static List<string> ValidateUpdate(UpdateCustomerDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.TaxNo))
            errors.Add("Vergi/TC numarası gereklidir");
        else if (dto.TaxNo.Length > 11)
            errors.Add("Vergi/TC numarası 11 karakterden uzun olamaz");
        else
        {
            // "U" ile başlayanlar TC Kimlik No olarak kabul edilir
            if (dto.TaxNo.StartsWith("U", StringComparison.OrdinalIgnoreCase))
            {
                string tcNo = dto.TaxNo.Substring(1); // U'yu çıkar
                
                if ((tcNo.Length < 8 || tcNo.Length > 11) || !IsNumeric(tcNo))
                    errors.Add($"Geçersiz TC Kimlik No formatı: {dto.TaxNo} (U + 8-11 rakam olmalı)");
                else if (tcNo.Length == 11 && !ValidateTurkishId(tcNo))
                    errors.Add("Geçersiz TC Kimlik No");
            }
            else if (!Regex.IsMatch(dto.TaxNo, @"^\d{10,11}$"))
            {
                errors.Add("Geçerli bir vergi numarası giriniz (10-11 haneli rakam)");
            }
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
            errors.Add("Müşteri ünvanı gereklidir");
        else if (dto.Title.Length > 200)
            errors.Add("Ünvan 200 karakterden uzun olamaz");

        if (!string.IsNullOrEmpty(dto.ContactPerson) && dto.ContactPerson.Length > 100)
            errors.Add("İletişim kişisi 100 karakterden uzun olamaz");

        if (!string.IsNullOrEmpty(dto.Phone) && dto.Phone.Length > 20)
            errors.Add("Telefon 20 karakterden uzun olamaz");

        if (!string.IsNullOrEmpty(dto.Email))
        {
            if (dto.Email.Length > 100)
                errors.Add("Email 100 karakterden uzun olamaz");
            else if (!Regex.IsMatch(dto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors.Add("Geçerli bir email adresi giriniz");
        }

        if (dto.CreditLimit.HasValue && dto.CreditLimit.Value < 0)
            errors.Add("Kredi limiti 0'dan küçük olamaz");

        return errors;
    }

    /// <summary>
    /// TC Kimlik No algoritması ile doğrulama yapar
    /// </summary>
    private static bool ValidateTurkishId(string tcNo)
    {
        if (tcNo.Length != 11) return false;
        if (!long.TryParse(tcNo, out long ATCNO)) return false;

        long BTCNO = ATCNO / 100;
        long TCNOTek = 0, TCNOCift = 0;

        for (int i = 0; i < 9; i++)
        {
            if (i % 2 == 0)
                TCNOTek += BTCNO % 10;
            else
                TCNOCift += BTCNO % 10;
            BTCNO /= 10;
        }

        long onuncuHane = ((TCNOTek * 7) - TCNOCift) % 10;
        long birinciOnHane = (TCNOTek + TCNOCift + onuncuHane) % 10;

        return (ATCNO % 100 == (onuncuHane * 10 + birinciOnHane));
    }

    /// <summary>
    /// String'in sadece rakamlardan oluşup oluşmadığını kontrol eder
    /// </summary>
    private static bool IsNumeric(string value)
    {
        return !string.IsNullOrEmpty(value) && value.All(char.IsDigit);
    }
}
