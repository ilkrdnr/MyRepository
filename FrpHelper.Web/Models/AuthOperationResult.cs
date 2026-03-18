namespace FrpHelper.Web.Models;

public sealed class AuthOperationResult
{
    public bool IsSuccess { get; init; }

    public string Message { get; init; } = string.Empty;

    public AuthSession? Session { get; init; }

    public static AuthOperationResult Success(string message, AuthSession session) =>
        new() { IsSuccess = true, Message = message, Session = session };

    public static AuthOperationResult Success(string message) =>
        new() { IsSuccess = true, Message = message };

    public static AuthOperationResult Failure(string message) =>
        new() { IsSuccess = false, Message = message };
}
