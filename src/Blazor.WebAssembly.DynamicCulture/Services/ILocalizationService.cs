using System;
using System.Globalization;

namespace Blazor.WebAssembly.DynamicCulture.Services;

public interface ILocalizationService
{
    event EventHandler<CultureInfo>? LanguageChanged;

    void InvokeLanguageChanged(CultureInfo newLanguage);
}