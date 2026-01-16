#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Blazor.WebAssembly.DynamicCulture.Loader.Interop
{
    internal partial class SatelliteAssemblies
    {
        public virtual async ValueTask LoadCurrentCultureResourcesAsync(IEnumerable<CultureInfo> cultureInfos)
        {
            // TODO: In .NET11 there is already another change to WebAssemblyCultureProvider, so expecting another break in the future.
#if NET10_0_OR_GREATER
            // Technically, Microsoft already should have loaded those cultures via WebAssemblyCultureProvider https://github.com/dariatiurina/aspnetcore/blob/3801203e0cd7ba5fcb4c7bd3ea9b99b1590e9b2d/src/Components/WebAssembly/WebAssembly/src/Hosting/WebAssemblyCultureProvider.cs#L34
            string[] excludeCultures = GetExcludedCultures();
#else
            string[] excludeCultures = Array.Empty<string>();
#endif
            List<string> culturesToLoad = GetCultures(cultureInfos, excludeCultures);
            if (culturesToLoad.Count == 0)
            {
                return;
            }

            await LoadSatelliteAssemblies(culturesToLoad.ToArray());
        }

        private static string[] GetExcludedCultures()
        {
            var excludedCultures = new HashSet<string>();
            
            // Add CurrentUICulture and all its parents
            AddCultureWithParents(excludedCultures, CultureInfo.CurrentUICulture);
            
            // Add CurrentCulture and all its parents (if different from CurrentUICulture)
            AddCultureWithParents(excludedCultures, CultureInfo.CurrentCulture);
            
            return excludedCultures.ToArray();
        }

        private static void AddCultureWithParents(HashSet<string> cultures, CultureInfo culture)
        {
            while (culture != null && !ReferenceEquals(culture, CultureInfo.InvariantCulture))
            {
                cultures.Add(culture.Name);

                if (ReferenceEquals(culture.Parent, culture))
                {
                    break;
                }

                culture = culture.Parent;
            }
        }

        [JSImport("INTERNAL.loadSatelliteAssemblies")]
        public static partial Task LoadSatelliteAssemblies(string[] culturesToLoad);
    }
}
#endif
