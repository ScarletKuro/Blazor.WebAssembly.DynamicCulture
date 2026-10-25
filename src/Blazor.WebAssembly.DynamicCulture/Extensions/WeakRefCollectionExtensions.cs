using Blazor.WebAssembly.DynamicCulture.Internals;
using Microsoft.AspNetCore.Components;

namespace Blazor.WebAssembly.DynamicCulture.Extensions;

internal static class WeakRefCollectionExtensions
{
    public static void InvokeStateHasChanged(this WeakRefCollection<ComponentBase> components)
    {
        // ComponentBase implements IHandleEvent; this re-renders without needing access to StateHasChanged.
        components.ForEach(component => ((IHandleEvent)component).HandleEventAsync(EventCallbackWorkItem.Empty, null));
    }
}
