using System.Net;

namespace Katana.Core.Interfaces;

public interface ILucaCookieJarStore
{
    CookieContainer GetOrCreate(string sessionId);
    void Clear(string sessionId);
}

