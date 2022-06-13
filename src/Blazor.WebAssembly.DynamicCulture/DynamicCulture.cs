using System;
using System.Globalization;
// ReSharper disable InconsistentNaming

namespace Blazor.WebAssembly.DynamicCulture;

public class DynamicCulture
{
    /// <summary>
    /// Creates a new <see cref="DynamicCulture"/> object with its <see cref="Culture"/> and <see cref="UICulture"/>
    /// properties set to the same <see cref="CultureInfo"/> value.
    /// </summary>
    /// <param name="culture">The <see cref="CultureInfo"/> for the request.</param>
    public DynamicCulture(CultureInfo culture)
        : this(culture, culture)
    {
    }

    /// <summary>
    /// Creates a new <see cref="DynamicCulture"/> object with its <see cref="Culture"/> and <see cref="UICulture"/>
    /// properties set to the same <see cref="CultureInfo"/> value.
    /// </summary>
    /// <param name="culture">The culture for the request.</param>
    public DynamicCulture(string culture)
        : this(culture, culture)
    {
    }

    /// <summary>
    /// Creates a new <see cref="DynamicCulture"/> object with its <see cref="Culture"/> and <see cref="UICulture"/>
    /// properties set to the respective <see cref="CultureInfo"/> values provided.
    /// </summary>
    /// <param name="culture">The culture for the request to be used for formatting.</param>
    /// <param name="uiCulture">The culture for the request to be used for text, i.e. language.</param>
    public DynamicCulture(string culture, string uiCulture)
        : this(new CultureInfo(culture), new CultureInfo(uiCulture))
    {
    }

    /// <summary>
    /// Creates a new <see cref="DynamicCulture"/> object with its <see cref="Culture"/> and <see cref="UICulture"/>
    /// properties set to the respective <see cref="CultureInfo"/> values provided.
    /// </summary>
    /// <param name="culture">The <see cref="CultureInfo"/> for the request to be used for formatting.</param>
    /// <param name="uiCulture">The <see cref="CultureInfo"/> for the request to be used for text, i.e. language.</param>
    public DynamicCulture(CultureInfo culture, CultureInfo uiCulture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(uiCulture);

        Culture = culture;
        UICulture = uiCulture;
    }

    /// <summary>
    /// Gets the <see cref="CultureInfo"/> for the request to be used for formatting.
    /// </summary>
    public CultureInfo Culture { get; }

    /// <summary>
    /// Gets the <see cref="CultureInfo"/> for the request to be used for text, i.e. language;
    /// </summary>
    public CultureInfo UICulture { get; }
}