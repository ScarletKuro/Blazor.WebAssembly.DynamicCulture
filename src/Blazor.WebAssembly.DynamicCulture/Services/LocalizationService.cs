using System;
using System.Globalization;

namespace Blazor.WebAssembly.DynamicCulture.Services;

public class LocalizationService : ILocalizationService
{
    public event EventHandler<CultureInfo>? LanguageChanged;

    void ILocalizationService.InvokeLanguageChanged(CultureInfo newLanguage) => LanguageChanged?.Invoke(this, newLanguage);
}