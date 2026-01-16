# Blazor.WebAssembly.DynamicCulture

[![Nuget](https://img.shields.io/nuget/v/Blazor.WebAssembly.DynamicCulture?color=ff4081&logo=nuget)](https://www.nuget.org/packages/Blazor.WebAssembly.DynamicCulture/)
[![Nuget](https://img.shields.io/nuget/dt/Blazor.WebAssembly.DynamicCulture?color=ff4081&label=nuget%20downloads&logo=nuget)](https://www.nuget.org/packages/Blazor.WebAssembly.DynamicCulture/)
[![GitHub](https://img.shields.io/github/license/ScarletKuro/Blazor.WebAssembly.DynamicCulture?color=594ae2&logo=github)](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/blob/master/LICENSE)

Dynamic localization support for Blazor WebAssembly applications. This library replicates the functionality of `.UseRequestLocalization` from Blazor Server for Blazor WebAssembly (WASM), enabling culture switching without page reloads.

## 📢 Important: Native Blazor Alternative Available

**Blazor now provides a native way to load all satellite assemblies** in WebAssembly applications. You can configure this in your `index.html`:

**Before:**
```html
<script src="_framework/blazor.webassembly.js"></script>
```

**After:**
```html
<script src="_framework/blazor.webassembly.js" autostart="false"></script>
<script>
   Blazor.start({ 
       configureRuntime: runtime => runtime.withConfig({ 
           loadAllSatelliteResources: true 
       }) 
   })
</script>
```

**This native approach may reduce or eliminate the need for this library.** However, this library still provides additional features:
- Built-in culture providers (QueryString, LocalStorage, AcceptLanguageHeader)
- Automatic component refresh on language change via `LanguageTrackProvider`
- Simplified culture management API
- No manual Blazor startup configuration needed

Evaluate which solution best fits your project requirements.

## 🎬 Demonstration

![Dynamic Culture Demo](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/blob/master/gif/DynamicCulture.gif)

## 📚 Sample Projects

- [Blazor.WebAssembly.Sample.DynamicCulture](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/tree/master/samples/Blazor.WebAssembly.Sample.DynamicCulture) - Complete sample demonstrating basic usage and features

## 🚀 Getting Started

### Prerequisites

Add the following property to your `.csproj` file to load all globalization data:

```xml
<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>
```

### Service Registration

Configure services in your Blazor WASM `Program.cs`:

```csharp
using Blazor.WebAssembly.DynamicCulture;
using Microsoft.Extensions.Localization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddLocalization(); // Requires Microsoft.Extensions.Localization package
builder.Services.AddLocalizationDynamic(options =>
{
    options.SetDefaultCulture("en-US"); // Specify your default culture
    options.AddSupportedCultures("en-US", "et", "ru");
    options.AddSupportedUICultures("en-US", "et", "ru");
});

var host = builder.Build();
await host.SetMiddlewareCulturesAsync();
await host.RunAsync();
```

> **⚠️ Note:** Do not use this library for Blazor Server applications. Use `.UseRequestLocalization` instead.

### Add Required Imports

Add the following to your `_Imports.razor`:

```csharp
@using Microsoft.Extensions.Localization
@using Blazor.WebAssembly.DynamicCulture.Services
@using Blazor.WebAssembly.DynamicCulture
```

### Component Configuration

Add the `LanguageTrackProvider` component to pages/components that need dynamic culture updates. This component listens for `LocalizationService.InvokeLanguageChanged` events and calls `StateHasChanged` on the corresponding component.

**For version 1.x.x:**
```razor
<LanguageTrackProvider Component="this"/>
```

**For version 2.x.x and higher:**
```razor
<LanguageTrackProvider OnInitializeEvent="provider => provider.RegisterComponent(this)"/>
```

## 🔧 Culture Providers

This library includes three built-in culture providers that determine the active culture:

1. **QueryStringCultureProvider** - Checks for culture in URL query parameters
2. **LocalStorageCultureProvider** - Retrieves culture from browser local storage (key: `getBlazorCulture`)
3. **AcceptLanguageHeaderCultureProvider** - Uses the browser's Accept-Language header

Providers are checked in the order listed above. The first provider that returns a culture is used.

### Custom Culture Providers

You can implement custom providers by implementing the `ICultureProvider` interface and modifying `LocalizationDynamicOptions.CultureProviders`.

## 🎨 Creating a Language Selector Component

Create a custom language selector component to allow users to switch languages. This example uses `LocalStorageCultureProvider`:

```csharp
@inject LocalizationLocalStorageManager LocalizationLocalStorageManager
@inject ILocalizationService LocalizationService

<MudMenu StartIcon="@Icons.Material.Outlined.Translate" 
         EndIcon="@Icons.Material.Filled.KeyboardArrowDown" 
         Label="@GetAvailableLanguageInfo(Culture).Name" 
         Color="Color.Secondary" 
         Direction="Direction.Bottom" 
         FullWidth="true" 
         OffsetY="true" 
         Dense="true">
    @foreach (var language in _supportedLanguages)
    {
        @if (Equals(Culture, language.Culture))
        {
            <MudMenuItem OnClick="() => OnLanguageClick(language.Culture)">
                <u>@language.Name</u>
            </MudMenuItem>
        }
        else
        {
            <MudMenuItem OnClick="() => OnLanguageClick(language.Culture)">
                @language.Name
            </MudMenuItem>
        }
    }
</MudMenu>

@code {
    private readonly LanguageInfo[] _supportedLanguages = {
        new("English", "English", new CultureInfo("en-US")),
        new("Russian", "Русский", new CultureInfo("ru")),
        new("Estonian", "Eesti", new CultureInfo("et"))
    };

    private CultureInfo Culture => CultureInfo.CurrentUICulture;

    private async Task OnLanguageClick(CultureInfo selectedCulture)
    {
        await SetCulture(selectedCulture);
    }

    private LanguageInfo GetAvailableLanguageInfo(CultureInfo culture)
    {
        foreach (var language in _supportedLanguages)
        {
            if (Equals(culture, language.Culture))
            {
                return language;
            }
        }

        throw new NotSupportedException($"Language with culture '{culture.Name}' is not supported.");
    }

    private async Task SetCulture(CultureInfo cultureInfo)
    {
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        await LocalizationLocalStorageManager.SetBlazorCultureAsync(cultureInfo.Name);
        LocalizationService.InvokeLanguageChanged(cultureInfo);
    }
}
```

## 📝 Usage Example

The following demonstrates how to use localized strings with `IStringLocalizer<T>`. The Razor markup `@Loc["Greeting"]` localizes the string keyed to the `Greeting` value defined in your resource files.

```razor
@page "/culture-example"
@inject IStringLocalizer<Translation> Loc

<LanguageTrackProvider OnInitializeEvent="provider => provider.RegisterComponent(this)"/>

<h2>@Loc["Greeting"]</h2>
<p>@Loc["WelcomeMessage"]</p>
```

## 📖 Additional Resources

- [Main Repository Documentation](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture)
- [Blazor.WebAssembly.DynamicCulture.Loader Package](https://www.nuget.org/packages/Blazor.WebAssembly.DynamicCulture.Loader/)
- [Sample Project](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/tree/master/samples/Blazor.WebAssembly.Sample.DynamicCulture)

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/blob/master/LICENSE) file for details.
