using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop.WebAssembly;
// ReSharper disable InconsistentNaming

namespace Blazor.WebAssembly.DynamicCulture.Loader;

public sealed class DefaultWebAssemblyJSRuntime : WebAssemblyJSRuntime
{
    internal static readonly DefaultWebAssemblyJSRuntime Instance = new();

    public ElementReferenceContext ElementReferenceContext { get; }

    DefaultWebAssemblyJSRuntime()
    {
        ElementReferenceContext = new WebElementReferenceContext(this);
        JsonSerializerOptions.Converters.Add(new ElementReferenceJsonConverter(ElementReferenceContext));
    }
}