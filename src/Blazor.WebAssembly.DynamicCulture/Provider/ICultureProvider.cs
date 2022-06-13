using System.Threading.Tasks;
using Blazor.WebAssembly.DynamicCulture.LocalizationManager;

namespace Blazor.WebAssembly.DynamicCulture.Provider
{
    /// <summary>
    /// Represents a provider for determining the culture information.
    /// </summary>
    public interface ICultureProvider
    {
        /// <summary>
        /// Implements the provider to determine the culture.
        /// </summary>
        /// <returns>
        ///     The determined <see cref="ProviderCultureResult"/>.
        ///     Returns <c>null</c> if the provider couldn't determine a <see cref="ProviderCultureResult"/>.
        /// </returns>
        Task<ProviderCultureResult?> DetermineProviderCultureResult(LocalizationContextManager localizationContextManager);
    }
}
