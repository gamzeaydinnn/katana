using System.Threading.Tasks;
using System;

namespace Katana.Business.Interfaces
{
    /// <summary>
    /// Thread-safe Luca session manager interface
    /// Manages JSESSIONID lifecycle, auto-refresh, and session pooling
    /// </summary>
    public interface ILucaSessionManager
    {
        /// <summary>
        /// Get current active session ID (JSESSIONID)
        /// Thread-safe, returns valid session or refreshes if expired
        /// </summary>
        Task<string> GetActiveSessionAsync();

        /// <summary>
        /// Force refresh session (login to Luca and get new JSESSIONID)
        /// </summary>
        Task<string> RefreshSessionAsync();

        /// <summary>
        /// Check if current session is valid (not expired)
        /// </summary>
        Task<bool> IsSessionValidAsync();

        /// <summary>
        /// Get session expiration time
        /// </summary>
        DateTime? GetSessionExpiry();

        /// <summary>
        /// Manually set session ID (for testing or external session injection)
        /// </summary>
        Task SetSessionAsync(string sessionId, DateTime expiry);

        /// <summary>
        /// Clear current session (logout)
        /// </summary>
        Task ClearSessionAsync();

        /// <summary>
        /// Get session statistics (for monitoring)
        /// </summary>
        Task<SessionStats> GetSessionStatsAsync();
    }

    public class SessionStats
    {
        public string? CurrentSessionId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public TimeSpan? RemainingTime { get; set; }
        public int RefreshCount { get; set; }
        public DateTime? LastRefreshAt { get; set; }
        public bool IsValid { get; set; }
    }
}
