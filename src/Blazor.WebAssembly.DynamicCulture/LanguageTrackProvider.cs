using System;
using System.Globalization;
using Blazor.WebAssembly.DynamicCulture.Extensions;
using Blazor.WebAssembly.DynamicCulture.Internals;
using Blazor.WebAssembly.DynamicCulture.Services;
using Microsoft.AspNetCore.Components;

namespace Blazor.WebAssembly.DynamicCulture;

public class LanguageTrackProvider : ComponentBase, IDisposable
{
    private readonly WeakRefCollection<ComponentBase> _components = new();

    [Inject] 
    protected ILocalizationService LanguageService { get; set; } = null!;

    [EditorRequired]
    [Parameter]
    public ComponentBase? Component
    {
        get => this;
        set
        {
            if (value is not null)
            {
                _components.Add(value);
            }
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            LanguageService.LanguageChanged += OnLanguageChanged;
        }
    }

    public void OnLanguageChanged(object? sender, CultureInfo cultureInfo)
    {
        _components.InvokeStateHasChanged();
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            LanguageService.LanguageChanged -= OnLanguageChanged;
        }
    }
}