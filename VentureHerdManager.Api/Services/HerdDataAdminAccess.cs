using System.Security.Cryptography;
using System.Text;

namespace VentureHerdManager.Api.Services;

public sealed class HerdDataAdminAccess(
    IConfiguration configuration,
    DemoSessionContext? demoSessionContext = null)
{
    public bool IsAuthorized(HttpRequest request)
    {
        if (demoSessionContext?.IsDemoMode == true)
        {
            return true;
        }
        var configured = configuration["HerdDataImport:AdminKey"];
        var provided = request.Headers["X-Herd-Admin-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(configured) || string.IsNullOrWhiteSpace(provided)) return false;
        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(configured)),
            SHA256.HashData(Encoding.UTF8.GetBytes(provided)));
    }
}
