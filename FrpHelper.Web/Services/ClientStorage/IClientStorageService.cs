namespace FrpHelper.Web.Services.ClientStorage;

public interface IClientStorageService
{
    ValueTask SetItemAsync(string key, string value, CancellationToken cancellationToken = default);

    ValueTask<string?> GetItemAsync(string key, CancellationToken cancellationToken = default);

    ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default);
}
