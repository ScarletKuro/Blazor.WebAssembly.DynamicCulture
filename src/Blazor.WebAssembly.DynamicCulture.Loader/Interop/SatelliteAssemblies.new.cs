#if NET8_0_OR_GREATER
using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Blazor.WebAssembly.DynamicCulture.Loader.Interop
{
    internal partial class SatelliteAssemblies
    {
        private const string ModuleName = "Blazor.WebAssembly.DynamicCulture.Loader";

        private static async Task LoadCultureResourcesAsync(string[] culturesToLoad)
        {
            // Resolve against the application's base URI, including applications in a subdirectory.
            if (!OperatingSystem.IsBrowser())
            {
                throw new PlatformNotSupportedException("Satellite assemblies can only be loaded in the browser.");
            }

            using var document = JSHost.GlobalThis.GetPropertyAsJSObject("document")!;
            var moduleUrl = new Uri(new Uri(document.GetPropertyAsString("baseURI")!),
                "_content/Blazor.WebAssembly.DynamicCulture.Loader/SatelliteAssemblies.js");
            using var module = await JSHost.ImportAsync(ModuleName, moduleUrl.AbsoluteUri);
            using var runtime = JSHost.DotnetInstance;
            await LoadSatelliteAssemblies(runtime, culturesToLoad);
        }

        [JSImport("loadSatelliteAssemblies", ModuleName)]
        private static partial Task LoadSatelliteAssemblies(JSObject runtime, string[] culturesToLoad);
    }
}
#endif
