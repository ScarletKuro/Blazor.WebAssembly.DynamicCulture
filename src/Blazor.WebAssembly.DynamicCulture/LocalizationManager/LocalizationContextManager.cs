namespace Blazor.WebAssembly.DynamicCulture.LocalizationManager;

public class LocalizationContextManager
{
    public LocalizationLocalStorageManager LocalStorage { get; }

    public LocalizationQueryManager Query { get; }

    public LocalizationNavigatorManager Navigator { get; }

    public LocalizationContextManager(LocalizationLocalStorageManager localStorage, LocalizationQueryManager query, LocalizationNavigatorManager navigator)
    {
        LocalStorage = localStorage;
        Query = query;
        Navigator = navigator;
    }
}