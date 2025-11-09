
using System;

namespace Katana.Business.DTOs
{
    /// <summary>
    /// Kullanıcı giriş isteği (login request) için gerekli bilgileri temsil eder.
    /// </summary>
    public record LoginRequest(string Username, string Password);

    /// <summary>
    /// Başarılı bir oturum açma işlemi sonrasında dönen yanıtı temsil eder.
    /// JWT (JSON Web Token) bilgisini içerir.
    /// </summary>
    public record LoginResponse(string Token);

    /// <summary>
    /// Şifre değiştirme isteği için gerekli bilgileri temsil eder.
    /// </summary>
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
