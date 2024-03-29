# Blazor.WebAssembly.DynamicCulture
[![Nuget](https://img.shields.io/nuget/v/Blazor.WebAssembly.DynamicCulture?color=ff4081&logo=nuget)](https://www.nuget.org/packages/Blazor.WebAssembly.DynamicCulture/)
[![Nuget](https://img.shields.io/nuget/dt/Blazor.WebAssembly.DynamicCulture?color=ff4081&label=nuget%20downloads&logo=nuget)](https://www.nuget.org/packages/Blazor.WebAssembly.DynamicCulture/)
[![GitHub](https://img.shields.io/github/license/ScarletKuro/Blazor.WebAssembly.DynamicCulture?color=594ae2&logo=github)](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/blob/master/LICENSE)

This library essentially replicates the functionality of `.UseRequestLocalization` for Blazor Server-Side(BSS), but it is specifically designed for Blazor WebAssembly (WASM). It relies on the default `IStringLocalizer` and utilizes the **Blazor.WebAssembly.DynamicCulture.Loader**, eliminating the need to refresh the page when switching languages.

## Demonstration
![gif](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/blob/master/gif/DynamicCulture.gif)

## Samples
1. [Blazor.WebAssembly.Sample.DynamicCulture](https://github.com/ScarletKuro/Blazor.WebAssembly.DynamicCulture/tree/master/samples/Blazor.WebAssembly.Sample.DynamicCulture) - Minimal getting started project. Shows basic usage of `Blazor.WebAssembly.DynamicCulture`.

## Getting Started

### Add to .csproj
```XML
<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>
```
### Register Services
Blazor WASM
```CSharp
    builder.Services.AddLocalization(); //requires Microsoft.Extensions.Localization package
    builder.Services.AddLocalizationDynamic(options =>
    {
        options.SetDefaultCulture("en-US"); //Do not forget to specify your delfault culture, usually the neutral one is en-US
        options.AddSupportedCultures("et", "ru");
        options.AddSupportedUICultures("et", "ru");
    });
    //...
    var host = builder.Build();
    await host.SetMiddlewareCulturesAsync();
    await host.RunAsync();
```
**NB!** Do not use it for Blazor ServerSide.

### Add Imports
After the package is added, you need to add the following in your **_Imports.razor**
```CSharp
@using Microsoft.Extensions.Localization
@using Blazor.WebAssembly.DynamicCulture.Services
@using Blazor.WebAssembly.DynamicCulture
```

### Add Components
Add the following for each **components** / **pages** that needs dynamic cultures. It will listen for `LocalizationService.InvokeLanguageChanged` and call `StateHasChanged` for the corresponding component.

For version **1.x.x**:
```HTML
<LanguageTrackProvider Component="this"/>
```
For version **2.x.x** and higher:
```HTML
<LanguageTrackProvider OnInitializeEvent="provider => provider.RegisterComponent(this)"/>
```
### Create your own LangugeSelector Component (optional, depending on your needs)
This can be optional in case you don't want to have a language selector and want to take the langauge from query or header for example.
In fact this library has 3 providers:
 - QueryStringCultureProvider
 - LocalStorageCultureProvider
 - AcceptLanguageHeaderCultureProvider

First it will check if there is query parameter in url with the culture, then it will check local storage if there is `getBlazorCulture` and then it will check for langauge header.

You can make own with implementing ICultureProvider and changing the LocalizationDynamicOptions.CultureProviders.

This example uses `LocalStorageCultureProvider`.
```CSharp
@inject LocalizationLocalStorageManager LocalizationLocalStorageManager;
@inject ILocalizationService LocalizationService;

<MudMenu StartIcon="@Icons.Material.Outlined.Translate" EndIcon="@Icons.Material.Filled.KeyboardArrowDown" Label="@GetAvailableLanguageInfo(Culture).Name" Color="Color.Secondary" Direction="Direction.Bottom" FullWidth="true" OffsetY="true" Dense="true">
    @foreach (var language in _supportedLanguages)
    {
        @if (Equals(Culture, language.Culture))
        {
            <MudMenuItem OnClick="() => OnLanguageClick(language.Culture)"><u>@language.Name</u></MudMenuItem>
        }
        else
        {
            <MudMenuItem OnClick="() => OnLanguageClick(language.Culture)">@language.Name</MudMenuItem>
        }
    }
</MudMenu>

@code {
    private readonly LanguageInfo[] _supportedLanguages = {
        new("English", "English", new CultureInfo("en-US")),
        new("Russian", "Русский", new CultureInfo("ru")),
        new("Estonia", "Eesti", new CultureInfo("et"))
    };

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

        throw new NotSupportedException($"Language with {culture.Name} is not supported.");
    }

    private async Task SetCulture(CultureInfo cultureInfo)
    {
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        await LocalizationLocalStorageManager.SetBlazorCultureAsync(cultureInfo.Name);
        LocalizationService.InvokeLanguageChanged(cultureInfo);
    }

    private CultureInfo Culture => CultureInfo.CurrentUICulture;
}
```

### Page example
The following demonstrates the use of the localized Greeting string with IStringLocalizer<T>. The Razor markup @Loc["Greeting"] in the following example localizes the string keyed to the Greeting value, which is set in the preceding resource files.
```HTML
@page "/culture-example-2"
@* Translation - resx class with translations *@
@inject IStringLocalizer<Translation> Loc

<LanguageTrackProvider Component="this"/>
<h2>Loc["Greeting"]</h2>
```
