#if NET8_0_OR_GREATER
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Blazor.WebAssembly.DynamicCulture.Loader.Interop
{
    internal partial class SatelliteAssemblies
    {
        public virtual async ValueTask LoadCurrentCultureResourcesAsync(IEnumerable<CultureInfo> cultureInfos)
        {
            List<string> culturesToLoad = GetCultures(cultureInfos);
            if (culturesToLoad.Count == 0)
            {
                return;
            }

            await LoadSatelliteAssemblies(culturesToLoad.ToArray());
        }

        [JSImport("INTERNAL.loadSatelliteAssemblies")]
        public static partial Task LoadSatelliteAssemblies(string[] culturesToLoad);
    }
}
#endif
