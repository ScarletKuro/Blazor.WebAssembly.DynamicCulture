using System.Globalization;
using Blazor.WebAssembly.DynamicCulture.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class LanguageTrackProviderTests : BunitContext
{
    private readonly LocalizationService _service = new();

    public LanguageTrackProviderTests()
    {
        Services.AddSingleton<ILocalizationService>(_service);
    }

    [Fact]
    public void Initialization_ProvidesItsInstanceToRegistrationCallbackOnlyOnce()
    {
        var initialized = new List<LanguageTrackProvider>();
        var tracker = Render<LanguageTrackProvider>(parameters => parameters
            .Add(component => component.OnInitializeEvent, provider => initialized.Add(provider)));

        tracker.Render();

        Assert.Same(tracker.Instance, Assert.Single(initialized));
    }

    [Fact]
    public async Task LanguageChanged_RefreshesEveryRegisteredComponentOnce()
    {
        var first = Render<TrackedComponent>();
        var second = Render<TrackedComponent>();
        var unregistered = Render<TrackedComponent>();
        var tracker = Render<LanguageTrackProvider>(parameters => parameters
            .Add(component => component.OnInitializeEvent, provider =>
            {
                provider.RegisterComponent(first.Instance);
                provider.RegisterComponent(first.Instance);
                provider.RegisterComponent(second.Instance);
                provider.RegisterComponent(null);
            }));
        var firstRenderCount = first.RenderCount;
        var secondRenderCount = second.RenderCount;
        first.Instance.Text = "updated first";
        second.Instance.Text = "updated second";
        unregistered.Instance.Text = "should not render";

        await tracker.InvokeAsync(() => ((ILocalizationService)_service)
            .InvokeLanguageChanged(CultureInfo.GetCultureInfo("ja-JP")));

        Assert.Equal("updated first", first.Markup);
        Assert.Equal("updated second", second.Markup);
        Assert.Equal("initial", unregistered.Markup);
        Assert.Equal(firstRenderCount + 1, first.RenderCount);
        Assert.Equal(secondRenderCount + 1, second.RenderCount);
    }

    [Fact]
    public async Task RerenderingTracker_DoesNotSubscribeTwice()
    {
        var component = Render<TrackedComponent>();
        var tracker = Render<LanguageTrackProvider>(parameters => parameters
            .Add(provider => provider.OnInitializeEvent, provider => provider.RegisterComponent(component.Instance)));
        tracker.Render();
        tracker.Render();
        var renderCount = component.RenderCount;

        await tracker.InvokeAsync(() => ((ILocalizationService)_service)
            .InvokeLanguageChanged(CultureInfo.GetCultureInfo("fr-FR")));

        Assert.Equal(renderCount + 1, component.RenderCount);
    }

    [Fact]
    public async Task DisposingTracker_UnsubscribesFromLanguageChanges()
    {
        var component = Render<TrackedComponent>();
        var host = Render<TrackerHost>(parameters => parameters
            .Add(provider => provider.OnInitializeEvent, provider => provider.RegisterComponent(component.Instance)));
        host.Render(parameters => parameters.Add(parent => parent.ShowTracker, false));
        var renderCount = component.RenderCount;

        await component.InvokeAsync(() => ((ILocalizationService)_service)
            .InvokeLanguageChanged(CultureInfo.GetCultureInfo("fr-FR")));

        Assert.Equal(renderCount, component.RenderCount);
    }

    public sealed class TrackerHost : ComponentBase
    {
        [Parameter]
        public bool ShowTracker { get; set; } = true;

        [Parameter]
        public EventCallback<LanguageTrackProvider> OnInitializeEvent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (ShowTracker)
            {
                builder.OpenComponent<LanguageTrackProvider>(0);
                builder.AddAttribute(1, nameof(LanguageTrackProvider.OnInitializeEvent), OnInitializeEvent);
                builder.CloseComponent();
            }
        }
    }

    public sealed class TrackedComponent : ComponentBase
    {
        public string Text { get; set; } = "initial";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Text);
        }
    }
}
