using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Blazor.WebAssembly.DynamicCulture.LocalizationManager;

public sealed class LocalizationQueryManager : BaseLocalizationManager
{
    public LocalizationQueryManager(IJSRuntime jsRuntime)
        : base(jsRuntime, "./_content/Blazor.WebAssembly.DynamicCulture/QueryManager.js")
    {
    }

    public async Task<string?> GetValueAsync(string key)
    {
        await InitializeAsync();
        if (JsModule is null)
        {
            return null;
        }

        var value = await JsModule.InvokeAsync<string?>("getQueryValue", key);

        return value;
    }
}