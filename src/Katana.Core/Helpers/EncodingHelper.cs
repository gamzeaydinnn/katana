using System.Text;

namespace Katana.Core.Helpers;

public static class EncodingHelper
{
    private static readonly object InitLock = new();
    private static bool _providerRegistered;

    private static void EnsureProvider()
    {
        if (_providerRegistered) return;
        lock (InitLock)
        {
            if (_providerRegistered) return;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _providerRegistered = true;
        }
    }

    public static string ConvertToIso88599(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
        EnsureProvider();
        try
        {
            var enc = Encoding.GetEncoding("iso-8859-9");
            var bytes = enc.GetBytes(value);
            return enc.GetString(bytes);
        }
        catch
        {
            return value;
        }
    }
}
