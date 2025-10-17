namespace Katana.Business.Interfaces;
//Amaç: Servisler veya işlemler, doğrudan Serilog ya da ILogger kullanmasın; onun yerine bir arayüzden geçsin.
public interface ILoggingService
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? ex = null);
}
