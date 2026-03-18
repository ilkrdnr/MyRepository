namespace FrpHelper.Web.Models;

public sealed class SupabaseOperationResult
{
    public bool IsSuccess { get; init; }

    public string Message { get; init; } = string.Empty;

    public static SupabaseOperationResult Success(string message) =>
        new() { IsSuccess = true, Message = message };

    public static SupabaseOperationResult Failure(string message) =>
        new() { IsSuccess = false, Message = message };
}
