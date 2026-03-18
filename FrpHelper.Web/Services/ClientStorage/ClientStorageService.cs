using Microsoft.JSInterop;

namespace FrpHelper.Web.Services.ClientStorage;

public sealed class ClientStorageService(IJSRuntime jsRuntime) : IClientStorageService
{
    public ValueTask SetItemAsync(string key, string value, CancellationToken cancellationToken = default) =>
        jsRuntime.InvokeVoidAsync("frpStorage.set", cancellationToken, key, value);

    public ValueTask<string?> GetItemAsync(string key, CancellationToken cancellationToken = default) =>
        jsRuntime.InvokeAsync<string?>("frpStorage.get", cancellationToken, key);

    public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default) =>
        jsRuntime.InvokeVoidAsync("frpStorage.remove", cancellationToken, key);
}
