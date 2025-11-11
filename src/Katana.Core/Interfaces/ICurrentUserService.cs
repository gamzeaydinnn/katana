using System;

namespace Katana.Core.Interfaces;

/// <summary>
/// Provides information about the current request user and client.
/// This abstraction avoids coupling data layer to ASP.NET HttpContext.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Authenticated username (JWT name claim) or null when unauthenticated.
    /// </summary>
    string? Username { get; }

    /// <summary>
    /// Client IP address if available.
    /// </summary>
    string? IpAddress { get; }

    /// <summary>
    /// Client user-agent header if available.
    /// </summary>
    string? UserAgent { get; }
}

