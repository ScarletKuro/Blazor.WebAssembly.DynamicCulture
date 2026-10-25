using System.Globalization;
using Blazor.WebAssembly.DynamicCulture.Services;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class LocalizationServiceTests
{
    [Fact]
    public void InvokeLanguageChanged_WithoutSubscribersDoesNotThrow()
    {
        ILocalizationService service = new LocalizationService();

        service.InvokeLanguageChanged(CultureInfo.InvariantCulture);
    }

    [Fact]
    public void InvokeLanguageChanged_NotifiesActiveSubscribersWithSenderAndExactCulture()
    {
        ILocalizationService service = new LocalizationService();
        var culture = new CultureInfo("ja-JP");
        var notifications = new List<(object? Sender, CultureInfo Culture)>();
        var removedHandlerCalled = false;
        EventHandler<CultureInfo> removed = (_, _) => removedHandlerCalled = true;
        service.LanguageChanged += removed;
        service.LanguageChanged += (sender, value) => notifications.Add((sender, value));
        service.LanguageChanged += (sender, value) => notifications.Add((sender, value));
        service.LanguageChanged -= removed;

        service.InvokeLanguageChanged(culture);

        Assert.False(removedHandlerCalled);
        Assert.Equal(2, notifications.Count);
        Assert.All(notifications, notification =>
        {
            Assert.Same(service, notification.Sender);
            Assert.Same(culture, notification.Culture);
        });
    }

    [Fact]
    public void InvokeLanguageChanged_RaisesLanguageChangedWithNewCulture()
    {
        var service = new LocalizationService();
        CultureInfo? changedCulture = null;

        service.LanguageChanged += (_, culture) => changedCulture = culture;
        ((ILocalizationService)service).InvokeLanguageChanged(new CultureInfo("ja-JP"));

        Assert.Equal(new CultureInfo("ja-JP"), changedCulture);
    }
}
