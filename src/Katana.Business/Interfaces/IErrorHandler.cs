using System;
using System.Threading.Tasks;

namespace Katana.Business.Interfaces;

/// <summary>
/// Servis katmanında meydana gelen hataları merkezi olarak ele almak ve loglamak için kullanılır.
/// </summary>
public interface IErrorHandler
{
    Task<T?> ExecuteWithHandlingAsync<T>(Func<Task<T>> operation, string operationName);

    /// <summary>
    /// Yalnızca void (geri dönüşsüz) işlemler için hata yönetimi uygular.
    /// </summary>
    Task ExecuteWithHandlingAsync(Func<Task> operation, string operationName);
}
