using System;
using System.Collections.Generic;
using System.Globalization;

namespace Blazor.WebAssembly.DynamicCulture.Loader.Interop
{
    internal partial class SatelliteAssemblies
    {
        internal static List<string> GetCultures(IEnumerable<CultureInfo> cultureInfos, params string[] excludedCultures)
        {
            var culturesToLoad = new List<string>();
            foreach (CultureInfo cultureInfo in cultureInfos)
            {
                var cultures = GetCultures(cultureInfo, excludedCultures);
                culturesToLoad.AddRange(cultures);
            }

            return culturesToLoad;
        }

        internal static List<string> GetCultures(CultureInfo? cultureInfo, params string[] excludedCultures)
        {
            var culturesToLoad = new List<string>();

            var excludedSet = new HashSet<string>(excludedCultures, StringComparer.OrdinalIgnoreCase);

            // Once WASM is ready, we have to use .NET's assembly loading to load additional assemblies.
            // First calculate all possible cultures that the application might want to load. We do this by
            // starting from the current culture and walking up the graph of parents.
            // At the end of the walk, we'll have a list of culture names that look like
            // [ "fr-FR", "fr" ]
            while (cultureInfo != null && !Equals(cultureInfo, CultureInfo.InvariantCulture))
            {
                // If the culture is in the excluded list, stop immediately
                if (excludedSet.Contains(cultureInfo.Name))
                {
                    break;
                }

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
