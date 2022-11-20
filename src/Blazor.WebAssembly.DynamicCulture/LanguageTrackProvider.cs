using System;
using System.Globalization;
using System.Threading.Tasks;
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

    protected override Task OnInitializedAsync()
    {
        return OnInitializeEvent.InvokeAsync(this);
    }

    [Parameter]
    [EditorRequired]
    public EventCallback<LanguageTrackProvider> OnInitializeEvent { get; set; }

    public void RegisterComponent(ComponentBase? component)
    {
        if (component is null)
        {
            return;
        }

        _components.Add(component);
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