using System;
using System.Threading.Tasks;

namespace Katana.Business.Interfaces;

/// <summary>
/// Servis katmanında meydana gelen hataları merkezi olarak ele almak ve loglamak için kullanılır.
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Belirtilen işlemi güvenli bir şekilde çalıştırır ve hata durumunda loglar.
    /// </summary>
    /// <typeparam name="T">İşlem sonucu dönecek tip.</typeparam>
    /// <param name="operation">Yürütülecek işlem.</param>
    /// <param name="operationName">İşlem adı (örneğin "GetProducts", "SyncData").</param>
    /// <returns>Başarılıysa sonucu, başarısızsa varsayılan değeri döner.</returns>
    Task<T?> ExecuteWithHandlingAsync<T>(Func<Task<T>> operation, string operationName);

    /// <summary>
    /// Yalnızca void (geri dönüşsüz) işlemler için hata yönetimi uygular.
    /// </summary>
    Task ExecuteWithHandlingAsync(Func<Task> operation, string operationName);
}
