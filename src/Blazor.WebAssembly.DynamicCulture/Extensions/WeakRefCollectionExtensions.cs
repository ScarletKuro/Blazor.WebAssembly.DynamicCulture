using Blazor.WebAssembly.DynamicCulture.Internals;
using Microsoft.AspNetCore.Components;

namespace Blazor.WebAssembly.DynamicCulture.Extensions;

internal static class WeakRefCollectionExtensions
{
    public static void InvokeStateHasChanged(this WeakRefCollection<ComponentBase> components)
    {
        components.ForEach(component =>
        {
            var handleEvent = component as IHandleEvent;
            handleEvent?.HandleEventAsync(EventCallbackWorkItem.Empty, null);
        });
    }
}