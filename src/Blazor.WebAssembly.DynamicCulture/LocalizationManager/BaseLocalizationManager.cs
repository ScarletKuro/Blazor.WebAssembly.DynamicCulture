using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Blazor.WebAssembly.DynamicCulture.LocalizationManager
{
    public abstract class BaseLocalizationManager : IAsyncDisposable
    {
        private bool _disposed;
        private readonly string _modulePath;
        private readonly IJSRuntime _jsRuntime;
        private readonly SemaphoreSlim _moduleLock = new(1, 1);

        protected IJSObjectReference? JsModule;

        public bool IsInitialized { get; private set; }

        protected BaseLocalizationManager(IJSRuntime jsRuntime, string modulePath)
        {
            _jsRuntime = jsRuntime;
            _modulePath = modulePath;
        }

        public async Task InitializeAsync()
        {
            await _moduleLock.WaitAsync();
            try
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                if (!IsInitialized)
                {
                    JsModule ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", _modulePath);
                    IsInitialized = true;
                }
            }
            finally
            {
                _moduleLock.Release();
            }
        }

        public ValueTask DisposeAsync()
        {
            return DisposeAsyncCore();
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            // Wait for an in-flight import before releasing its module reference.
            await _moduleLock.WaitAsync();
            try
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                IsInitialized = false;
                var module = JsModule;
                JsModule = null;
                if (module is not null)
                {
                    await module.DisposeAsync();
                }
            }
            finally
            {
                _moduleLock.Release();
            }
        }
    }
}
