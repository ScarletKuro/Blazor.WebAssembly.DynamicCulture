using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Blazor.WebAssembly.DynamicCulture.Loader.Interop;
using Microsoft.JSInterop;

// ReSharper disable InconsistentNaming

namespace Blazor.WebAssembly.DynamicCulture.Loader;

[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "This type loads resx files. We don't expect it's dependencies to be trimmed in the ordinary case.")]
internal partial class WebAssemblyCultureProvider
{
#if !NET8_0_OR_GREATER
    private readonly IJSUnmarshalledRuntime _invoker;
#endif

    // For unit testing.
#if NET8_0_OR_GREATER
    internal WebAssemblyCultureProvider(CultureInfo initialCulture, CultureInfo initialUICulture)
    {
        InitialCulture = initialCulture;
        InitialUICulture = initialUICulture;
    }
#else
    internal WebAssemblyCultureProvider(IJSUnmarshalledRuntime invoker, CultureInfo initialCulture, CultureInfo initialUICulture)
    {
        _invoker = invoker;
        InitialCulture = initialCulture;
        InitialUICulture = initialUICulture;
    }
#endif

    public static WebAssemblyCultureProvider? Instance { get; private set; }

    public CultureInfo InitialCulture { get; }

    public CultureInfo InitialUICulture { get; }

    internal static void Initialize()
    {
#if NET8_0_OR_GREATER
        Instance = new WebAssemblyCultureProvider(
            initialCulture: CultureInfo.CurrentCulture,
            initialUICulture: CultureInfo.CurrentUICulture);
#else
        Instance = new WebAssemblyCultureProvider(
            DefaultWebAssemblyJSRuntime.Instance,
            initialCulture: CultureInfo.CurrentCulture,
            initialUICulture: CultureInfo.CurrentUICulture);
#endif
    }

    public void ThrowIfCultureChangeIsUnsupported()
    {
        // With ICU sharding enabled, bootstrapping WebAssembly will download a ICU shard based on the browser language.
        // If the application author was to change the culture as part of their Program.MainAsync, we might have
        // incomplete icu data for their culture. We would like to flag this as an error and notify the author to
        // use the combined icu data file instead.
        //
        // The Initialize method is invoked as one of the first steps bootstrapping the app within WebAssemblyHostBuilder.CreateDefault.
        // It allows us to capture the initial .NET culture that is configured based on the browser language.
        // The current method is invoked as part of WebAssemblyHost.RunAsync i.e. after user code in Program.MainAsync has run
        // thus allows us to detect if the culture was changed by user code.
        if (Environment.GetEnvironmentVariable("__BLAZOR_SHARDED_ICU") == "1" &&
            ((!CultureInfo.CurrentCulture.Name.Equals(InitialCulture.Name, StringComparison.Ordinal) ||
              !CultureInfo.CurrentUICulture.Name.Equals(InitialUICulture.Name, StringComparison.Ordinal))))
        {
            throw new InvalidOperationException("Blazor detected a change in the application's culture that is not supported with the current project configuration. " +
                "To change culture dynamically during startup, set <BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData> in the application's project file.");
        }
    }
    
    public virtual async ValueTask LoadCurrentCultureResourcesAsync(IEnumerable<CultureInfo> cultureInfos)
    {
        if (!OperatingSystem.IsBrowser())
        {
            throw new PlatformNotSupportedException("This method is only supported in the browser.");
        }

#if NET8_0_OR_GREATER
        var satelliteAssemblies = new SatelliteAssemblies();
#else
        var satelliteAssemblies = new SatelliteAssemblies(_invoker);
#endif

        await satelliteAssemblies.LoadCurrentCultureResourcesAsync(cultureInfos);
    }
}