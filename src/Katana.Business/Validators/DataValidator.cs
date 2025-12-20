using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Katana.Business.Validators
{
    
    
    
    public static class DataValidator
    {
        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        public static bool IsPositiveDecimal(decimal value) => value >= 0;

        public static bool IsPositiveInt(int value) => value >= 0;

        public static bool IsValidDateRange(DateTime start, DateTime end)
            => start <= end;

        public static bool IsNotEmpty(string? input)
            => !string.IsNullOrWhiteSpace(input);

        public static List<string> ValidateCommonFields(string? name, decimal price, DateTime? date = null)
        {
            var errors = new List<string>();

            if (!IsNotEmpty(name))
                errors.Add("İsim boş olamaz.");

            if (!IsPositiveDecimal(price))
                errors.Add("Fiyat 0'dan küçük olamaz.");

            if (date.HasValue && date.Value > DateTime.UtcNow.AddYears(5))
                errors.Add("Tarih geçerli aralıkta olmalıdır.");

            return errors;
        }
    }
}
