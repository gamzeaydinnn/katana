namespace Katana.Core.Interfaces;

/// <summary>
/// Basit event publisher interface
/// Domain event'leri publish etmek için kullanılır
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Event'i asenkron olarak publish eder
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;
}
