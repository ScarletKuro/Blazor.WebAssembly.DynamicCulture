using System.Threading.Tasks;
using Blazor.WebAssembly.DynamicCulture.LocalizationManager;

namespace Blazor.WebAssembly.DynamicCulture.Provider;

/// <summary>
/// An abstract base class provider for determining the culture information of an <see cref="LocalizationContextManager"/>.
/// </summary>
public abstract class CultureProvider : ICultureProvider
{
    /// <summary>
    /// Result that indicates that this instance of <see cref="CultureProvider" /> could not determine the
    /// request culture.
    /// </summary>
    protected static readonly Task<ProviderCultureResult?> NullProviderCultureResult = Task.FromResult(default(ProviderCultureResult));

    /// <summary>
    /// The current options.
    /// </summary>
    public LocalizationDynamicOptions? Options { get; set; }

    /// <inheritdoc />
    public abstract Task<ProviderCultureResult?> DetermineProviderCultureResult(LocalizationContextManager localizationContextManager);
}