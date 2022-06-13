using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Blazor.WebAssembly.DynamicCulture.LocalizationManager
{
    public abstract class BaseLocalizationManager : IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly string _modulePath;

        protected IJSObjectReference? JsModule;

        public bool IsInitialized { get; private set; }

        protected BaseLocalizationManager(IJSRuntime jsRuntime, string modulePath)
        {
            _jsRuntime = jsRuntime;
            _modulePath = modulePath;
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            JsModule ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", _modulePath);

            IsInitialized = true;
        }

        public ValueTask DisposeAsync()
        {
            return DisposeAsyncCore();
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (JsModule is not null)
            {
                await JsModule.DisposeAsync();
            }
        }
    }
}
