using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
// ReSharper disable InconsistentNaming

namespace Blazor.WebAssembly.DynamicCulture;

internal static partial class CultureProviderLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "{requestCultureProvider} returned the following unsupported cultures '{cultures}'.", EventName = "UnsupportedCulture")]
    public static partial void UnsupportedCultures(this ILogger logger, string requestCultureProvider, IList<StringSegment> cultures);

    [LoggerMessage(2, LogLevel.Debug, "{requestCultureProvider} returned the following unsupported UI Cultures '{uiCultures}'.", EventName = "UnsupportedUICulture")]
    public static partial void UnsupportedUICultures(this ILogger logger, string requestCultureProvider, IList<StringSegment> uiCultures);
}