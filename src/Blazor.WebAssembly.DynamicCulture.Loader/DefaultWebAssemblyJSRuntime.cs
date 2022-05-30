using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop.WebAssembly;

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