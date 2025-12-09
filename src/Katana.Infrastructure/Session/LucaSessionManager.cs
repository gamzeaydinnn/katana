using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Katana.Business.Interfaces;
using Katana.Data.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Katana.Infrastructure.Session
{
    /// <summary>
    /// Thread-safe Luca session manager implementation
    /// Manages JSESSIONID lifecycle with automatic refresh
    /// </summary>
    public class LucaSessionManager : ILucaSessionManager
    {
        private readonly HttpClient _httpClient;
        private readonly LucaApiSettings _settings;
        private readonly ILogger<LucaSessionManager> _logger;
        private readonly SemaphoreSlim _sessionLock = new(1, 1);

        // Session state
        private string? _currentSessionId;
        private DateTime? _sessionCreatedAt;
        private DateTime? _sessionExpiresAt;
        private int _refreshCount = 0;
        private DateTime? _lastRefreshAt;

        // Session TTL: 20 minutes (Luca default)
        private static readonly TimeSpan SESSION_TTL = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan REFRESH_BUFFER = TimeSpan.FromMinutes(2);

        public LucaSessionManager(
            HttpClient httpClient,
            IOptions<LucaApiSettings> settings,
            ILogger<LucaSessionManager> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetActiveSessionAsync()
        {
            await _sessionLock.WaitAsync();
            try
            {
                // Check if current session is still valid
                if (IsSessionValidInternal())
                {
                    _logger.LogDebug("✅ Using existing session: {SessionId} (expires: {Expiry})",
                        MaskSessionId(_currentSessionId!), _sessionExpiresAt);
                    return _currentSessionId!;
                }

                // Session expired or not exists, refresh
                _logger.LogInformation("🔄 Session expired or missing, refreshing...");
                return await RefreshSessionInternalAsync();
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        public async Task<string> RefreshSessionAsync()
        {
            await _sessionLock.WaitAsync();
            try
            {
                return await RefreshSessionInternalAsync();
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        public async Task<bool> IsSessionValidAsync()
        {
            await _sessionLock.WaitAsync();
            try
            {
                return IsSessionValidInternal();
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        public DateTime? GetSessionExpiry()
        {
            return _sessionExpiresAt;
        }

        public async Task SetSessionAsync(string sessionId, DateTime expiry)
        {
            await _sessionLock.WaitAsync();
            try
            {
                _currentSessionId = sessionId;
                _sessionCreatedAt = DateTime.UtcNow;
                _sessionExpiresAt = expiry;
                _logger.LogInformation("✅ Session manually set: {SessionId} (expires: {Expiry})",
                    MaskSessionId(sessionId), expiry);
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        public async Task ClearSessionAsync()
        {
            await _sessionLock.WaitAsync();
            try
            {
                _currentSessionId = null;
                _sessionCreatedAt = null;
                _sessionExpiresAt = null;
                _logger.LogInformation("🧹 Session cleared");
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        public async Task<SessionStats> GetSessionStatsAsync()
        {
            await _sessionLock.WaitAsync();
            try
            {
                var remainingTime = _sessionExpiresAt.HasValue
                    ? _sessionExpiresAt.Value - DateTime.UtcNow
                    : (TimeSpan?)null;

                return new SessionStats
                {
                    CurrentSessionId = _currentSessionId != null ? MaskSessionId(_currentSessionId) : null,
                    CreatedAt = _sessionCreatedAt,
                    ExpiresAt = _sessionExpiresAt,
                    RemainingTime = remainingTime,
                    RefreshCount = _refreshCount,
                    LastRefreshAt = _lastRefreshAt,
                    IsValid = IsSessionValidInternal()
                };
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        // Internal methods (must be called within lock)

        private bool IsSessionValidInternal()
        {
            if (string.IsNullOrEmpty(_currentSessionId) || !_sessionExpiresAt.HasValue)
                return false;

            // Add buffer to prevent edge-case expiration during request
            var effectiveExpiry = _sessionExpiresAt.Value - REFRESH_BUFFER;
            return DateTime.UtcNow < effectiveExpiry;
        }

        private async Task<string> RefreshSessionInternalAsync()
        {
            try
            {
                _logger.LogInformation("🔐 Performing Luca login...");

                var loginPayload = new
                {
                    orgCode = _settings.MemberNumber,
                    userName = _settings.Username,
                    userPassword = _settings.Password
                };

                var json = JsonSerializer.Serialize(loginPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var loginUrl = $"{_settings.BaseUrl.TrimEnd('/')}/{_settings.Endpoints.Auth.TrimStart('/')}";
                var response = await _httpClient.PostAsync(loginUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Login failed: {response.StatusCode} - {errorBody}");
                }

                // Extract JSESSIONID from Set-Cookie header
                var sessionId = ExtractSessionIdFromResponse(response);
                if (string.IsNullOrEmpty(sessionId))
                {
                    throw new Exception("JSESSIONID not found in login response");
                }

                // Update session state
                _currentSessionId = sessionId;
                _sessionCreatedAt = DateTime.UtcNow;
                _sessionExpiresAt = DateTime.UtcNow + SESSION_TTL;
                _refreshCount++;
                _lastRefreshAt = DateTime.UtcNow;

                _logger.LogInformation("✅ Session refreshed: {SessionId} (expires: {Expiry}, refresh #{Count})",
                    MaskSessionId(sessionId), _sessionExpiresAt, _refreshCount);

                return sessionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Session refresh failed");
                throw;
            }
        }

        private string? ExtractSessionIdFromResponse(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var cookie in cookies)
                {
                    if (cookie.StartsWith("JSESSIONID=", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = cookie.Split(';');
                        var sessionPart = parts[0];
                        var sessionId = sessionPart.Substring("JSESSIONID=".Length);
                        return sessionId;
                    }
                }
            }

            return null;
        }

        private string MaskSessionId(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId) || sessionId.Length < 8)
                return "***";

            return $"{sessionId.Substring(0, 4)}...{sessionId.Substring(sessionId.Length - 4)}";
        }
    }
}
