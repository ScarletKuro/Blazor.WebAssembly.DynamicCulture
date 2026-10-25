#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
using System.IO;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Blazor.WebAssembly.DynamicCulture.Loader.Interop
{
    internal partial class SatelliteAssemblies
    {
        private const string GetSatelliteAssembliesConst = "BlazorDynamicCultureLoader.getSatelliteAssemblies";
        private const string ReadSatelliteAssembliesConst = "BlazorDynamicCultureLoader.readSatelliteAssemblies";

        private readonly IJSUnmarshalledRuntime _invoker;

        internal SatelliteAssemblies(IJSUnmarshalledRuntime invoker)
        {
            _invoker = invoker;
        }

        private async Task LoadCultureResourcesAsync(string[] culturesToLoad)
        {
            await using var module = await ((IJSRuntime)_invoker).InvokeAsync<IJSObjectReference>(
                "import", "./_content/Blazor.WebAssembly.DynamicCulture.Loader/SatelliteAssemblies.js");
            await module.InvokeVoidAsync("initializeLegacy");

            // Now that we know the cultures we care about, let WebAssemblyResourceLoader (in JavaScript) load these
            // assemblies. We effectively want to resolve a Task<byte[][]> but there is no way to express this
            // using interop. We'll instead do this in two parts:
            // getSatelliteAssemblies resolves when all satellite assemblies to be loaded in .NET are fetched and available in memory.
#pragma warning disable CS0618 // Type or member is obsolete
            var count = (int)await LoadSatelliteAssemblies(culturesToLoad);

            if (count == 0)
            {
                return;
            }

            // readSatelliteAssemblies resolves the assembly bytes
            var assemblies = ReadSatelliteAssemblies();
#pragma warning restore CS0618 // Type or member is obsolete

            for (var i = 0; i < assemblies.Length; i++)
            {
                using var stream = new MemoryStream((byte[])assemblies[i]);
                AssemblyLoadContext.Default.LoadFromStream(stream);
            }
        }

        private Task<object> LoadSatelliteAssemblies(string[] cultures)
        {
            return _invoker.InvokeUnmarshalled<string[], object?, object?, Task<object>>(
                GetSatelliteAssembliesConst,
                cultures,
                null,
                null);
        }

        private object[] ReadSatelliteAssemblies()
        {
            return _invoker.InvokeUnmarshalled<object?, object?, object?, object[]>(
                ReadSatelliteAssembliesConst,
                null,
                null,
                null);
        }
    }
}
#endif
