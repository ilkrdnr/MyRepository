using FrpHelper.Web.Models;

namespace FrpHelper.Web.Services.Auth;

public interface IAuthService
{
    AuthSession? CurrentSession { get; }

    bool IsAuthenticated { get; }

    event Action<AuthSession?>? SessionChanged;

    Task<AuthOperationResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<AuthOperationResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<AuthSession?> RestoreSessionAsync(CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);
}
