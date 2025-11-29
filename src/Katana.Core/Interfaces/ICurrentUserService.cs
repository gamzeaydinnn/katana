using System;

namespace Katana.Core.Interfaces;

public interface ICurrentUserService
{
    
    
    
    string? Username { get; }

    
    
    
    string? IpAddress { get; }

    
    
    
    string? UserAgent { get; }
}

