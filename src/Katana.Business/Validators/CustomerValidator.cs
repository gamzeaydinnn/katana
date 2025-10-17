using System.Text.RegularExpressions;
using Katana.Core.DTOs;

namespace Katana.Business.Validators;

public static class CustomerValidator
{
    public static List<string> ValidateCreate(CreateCustomerDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.TaxNo))
            errors.Add("Vergi numarası gereklidir");
        else if (dto.TaxNo.Length > 11)
            errors.Add("Vergi numarası 11 karakterden uzun olamaz");
        else if (!Regex.IsMatch(dto.TaxNo, @"^\d{10,11}$"))
            errors.Add("Geçerli bir vergi numarası giriniz");

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
            errors.Add("Vergi numarası gereklidir");
        else if (dto.TaxNo.Length > 11)
            errors.Add("Vergi numarası 11 karakterden uzun olamaz");
        else if (!Regex.IsMatch(dto.TaxNo, @"^\d{10,11}$"))
            errors.Add("Geçerli bir vergi numarası giriniz");

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
}
