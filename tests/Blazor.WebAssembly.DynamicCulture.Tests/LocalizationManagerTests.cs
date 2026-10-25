using Blazor.WebAssembly.DynamicCulture.LocalizationManager;
using Bunit;
using Microsoft.JSInterop;

namespace Blazor.WebAssembly.DynamicCulture.Tests;

public class LocalizationManagerTests
{
    [Fact]
    public async Task LocalStorage_WritesAndReadsCultureThroughOneModuleImport()
    {
        await using var context = new BunitContext();
        var module = context.JSInterop.SetupModule("./_content/Blazor.WebAssembly.DynamicCulture/LocalStorageManager.js");
        module.SetupVoid("setBlazorCulture", "ja-JP").SetVoidResult();
        module.Setup<string?>("getBlazorCulture").SetResult("ja-JP");
        await using var manager = new LocalizationLocalStorageManager(context.JSInterop.JSRuntime);
        Assert.False(manager.IsInitialized);

        await manager.SetBlazorCultureAsync("ja-JP");
        var culture = await manager.GetBlazorCultureAsync();
        await manager.InitializeAsync();

        Assert.True(manager.IsInitialized);
        Assert.Equal("ja-JP", culture);
        context.JSInterop.VerifyInvoke("import", 1);
        module.VerifyInvoke("setBlazorCulture", 1);
        module.VerifyInvoke("getBlazorCulture", 1);
    }

    [Theory]
    [InlineData("culture", "fr-FR")]
    [InlineData("language", null)]
    public async Task Query_ForwardsRequestedKeyAndReturnsModuleValue(string key, string? value)
    {
        await using var context = new BunitContext();
        var module = context.JSInterop.SetupModule("./_content/Blazor.WebAssembly.DynamicCulture/QueryManager.js");
        module.Setup<string?>("getQueryValue", key).SetResult(value);
        await using var manager = new LocalizationQueryManager(context.JSInterop.JSRuntime);

        Assert.Equal(value, await manager.GetValueAsync(key));
        module.VerifyInvoke("getQueryValue", 1);
    }

    [Fact]
    public async Task Navigator_ReadsPrimaryLanguageAndPreferenceListFromSameModule()
    {
        await using var context = new BunitContext();
        var module = context.JSInterop.SetupModule("./_content/Blazor.WebAssembly.DynamicCulture/NavigatorManager.js");
        module.Setup<string?>("navigatorLanguage").SetResult("fr-FR");
        module.Setup<string[]?>("navigatorLanguages").SetResult(["fr-FR", "en-US"]);
        await using var manager = new LocalizationNavigatorManager(context.JSInterop.JSRuntime);

        Assert.Equal("fr-FR", await manager.GetLanguageAsync());
        var languages = await manager.GetLanguagesAsync();
        Assert.NotNull(languages);
        Assert.Equal(["fr-FR", "en-US"], languages);
        context.JSInterop.VerifyInvoke("import", 1);
    }

    [Fact]
    public async Task FailedImport_CanBeRetried()
    {
        var runtime = new ImportRuntime(new DisposableModule()) { FailNextImport = true };
        await using var manager = new LocalizationQueryManager(runtime);

        await Assert.ThrowsAsync<JSException>(() => manager.InitializeAsync());
        Assert.False(manager.IsInitialized);
        await manager.InitializeAsync();
        Assert.True(manager.IsInitialized);
        Assert.Equal(2, runtime.ImportCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Disposal_DisposesImportedModuleAndDoesNotImportUnusedModule(bool initialize)
    {
        var module = new DisposableModule();
        var runtime = new ImportRuntime(module);
        var manager = new LocalizationQueryManager(runtime);
        if (initialize)
        {
            await manager.InitializeAsync();
        }

        await manager.DisposeAsync();

        Assert.Equal(initialize ? 1 : 0, module.DisposeCount);
        Assert.Equal(initialize ? 1 : 0, runtime.ImportCount);
    }

    private sealed class ImportRuntime(IJSObjectReference module) : IJSRuntime
    {
        public int ImportCount { get; private set; }
        public bool FailNextImport { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Assert.Equal("import", identifier);
            ImportCount++;
            if (FailNextImport)
            {
                FailNextImport = false;
                throw new JSException("Import failed");
            }
            return ValueTask.FromResult((TValue)module);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }

    private sealed class DisposableModule : IJSObjectReference
    {
        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => throw new InvalidOperationException("This module only tracks disposal.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }
}
