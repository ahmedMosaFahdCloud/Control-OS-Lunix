using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace ControlOS.Api.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public sealed class CredentialProtector
{
    public string Protect(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        byte[] encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(value),
            optionalEntropy: null,
            scope: DataProtectionScope.CurrentUser);

        return Convert.ToBase64String(encrypted);
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return string.Empty;
        }

        try
        {
            byte[] decrypted = ProtectedData.Unprotect(
                Convert.FromBase64String(protectedValue),
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return string.Empty;
        }
    }
}
