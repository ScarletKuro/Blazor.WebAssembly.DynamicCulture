using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Blazor.WebAssembly.DynamicCulture.LocalizationManager;

public sealed class LocalizationNavigatorManager : BaseLocalizationManager
{
    public LocalizationNavigatorManager(IJSRuntime jsRuntime)
        : base(jsRuntime, "./_content/Blazor.WebAssembly.DynamicCulture/NavigatorManager.js")
    {
    }

    public async Task<string?> GetLanguageAsync()
    {
        await InitializeAsync();
        if (JsModule is null)
        {
            return null;
        }

        var value = await JsModule.InvokeAsync<string?>("navigatorLanguage");

        return value;
    }

    public async Task<string[]?> GetLanguagesAsync()
    {
        await InitializeAsync();
        if (JsModule is null)
        {
            return Array.Empty<string>();
        }

        var value = await JsModule.InvokeAsync<string[]?>("navigatorLanguages");

        return value;
    }
}