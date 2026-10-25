using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Blazor.WebAssembly.DynamicCulture.Loader.Interop
{
    internal sealed partial class SatelliteAssemblies
    {
        private static readonly SemaphoreSlim LoadLock = new(1, 1);
        private static readonly HashSet<string> LoadedCultures = new(StringComparer.OrdinalIgnoreCase);

        internal async ValueTask LoadCurrentCultureResourcesAsync(IEnumerable<CultureInfo> cultureInfos)
        {
            var cultures = GetCultures(cultureInfos);
            await LoadLock.WaitAsync();
            try
            {
                var culturesToLoad = cultures.Where(culture => !LoadedCultures.Contains(culture)).ToArray();
                if (culturesToLoad.Length == 0)
                {
                    return;
                }

                await LoadCultureResourcesAsync(culturesToLoad);
                LoadedCultures.UnionWith(culturesToLoad);
            }
            finally
            {
                LoadLock.Release();
            }
        }

        internal static List<string> GetCultures(IEnumerable<CultureInfo> cultureInfos)
        {
            var culturesToLoad = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CultureInfo cultureInfo in cultureInfos)
            {
                foreach (var culture in GetCultures(cultureInfo))
                {
                    if (seen.Add(culture))
                    {
                        culturesToLoad.Add(culture);
                    }
                }
            }

            return culturesToLoad;
        }

        internal static List<string> GetCultures(CultureInfo? cultureInfo)
        {
            var culturesToLoad = new List<string>();

            // Once WASM is ready, we have to use .NET's assembly loading to load additional assemblies.
            // First calculate all possible cultures that the application might want to load. We do this by
            // starting from the current culture and walking up the graph of parents.
            // At the end of the walk, we'll have a list of culture names that look like
            // [ "fr-FR", "fr" ]
            while (cultureInfo != null && !Equals(cultureInfo, CultureInfo.InvariantCulture))
            {
                culturesToLoad.Add(cultureInfo.Name);

                if (Equals(cultureInfo.Parent, cultureInfo))
                {
                    break;
                }

                cultureInfo = cultureInfo.Parent;
            }

            return culturesToLoad;
        }
    }
}
