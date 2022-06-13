using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Blazor.WebAssembly.DynamicCulture.LocalizationManager;

public sealed class LocalizationLocalStorageManager : BaseLocalizationManager
{
    public LocalizationLocalStorageManager(IJSRuntime jsRuntime)
        : base(jsRuntime, "./_content/Blazor.WebAssembly.DynamicCulture/LocalStorageManager.js")
    {
    }

    public async Task SetBlazorCultureAsync(string value)
    {
        await InitializeAsync();
        if (JsModule is not null)
        {
            await JsModule.InvokeVoidAsync("setBlazorCulture", value);
        }
    }

    public async Task<string?> GetBlazorCultureAsync()
    {
        await InitializeAsync();
        if (JsModule is null)
        {
            return null;
        }

        var value = await JsModule.InvokeAsync<string?>("getBlazorCulture");

        return value;
    }
}