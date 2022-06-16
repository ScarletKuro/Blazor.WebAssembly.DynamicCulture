using Blazor.WebAssembly.DynamicCulture.Provider;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Blazor.WebAssembly.DynamicCulture.Middleware;
// ReSharper disable InconsistentNaming

namespace Blazor.WebAssembly.DynamicCulture
{
    public class LocalizationDynamicOptions
    {
        private DynamicCulture _defaultRequestCulture = new(CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);

        /// <summary>
        /// Creates a new <see cref="LocalizationDynamicOptions"/> with default values.
        /// </summary>
        public LocalizationDynamicOptions()
        {
            CultureProviders = new List<ICultureProvider>
            {
                new QueryStringCultureProvider { Options = this },
                new LocalStorageCultureProvider { Options = this },
                new AcceptLanguageHeaderCultureProvider { Options = this }
            };
        }

        /// <summary>
        /// Gets or sets the default culture to use for requests when a supported culture could not be determined by
        /// one of the configured <see cref="ICultureProvider"/>s.
        /// Defaults to <see cref="CultureInfo.CurrentCulture"/> and <see cref="CultureInfo.CurrentUICulture"/>.
        /// </summary>
        public DynamicCulture DefaultCulture
        {
            get => _defaultRequestCulture;
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                _defaultRequestCulture = value;
            }
        }

        public bool LoadAllCulturesAtOnce { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="LocalizationDynamicMiddleware"/> should set <see cref="CultureInfo.DefaultThreadCurrentCulture"/> for application automatically.
        /// Defaults to <c>false</c>;
        /// </summary>
        /// <remarks>
        /// Note that if you set both <see cref="IgnoreCulture"/> and <see cref="IgnoreUICulture"/>
        /// to <c>true</c> the <see cref="LocalizationDynamicMiddleware"/> will not set
        /// <see cref="CultureInfo.DefaultThreadCurrentCulture"/> and  <see cref="CultureInfo.DefaultThreadCurrentUICulture"/> automatically for application.
        /// </remarks>
        public bool IgnoreCulture { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="LocalizationDynamicMiddleware"/> should set <see cref="CultureInfo.DefaultThreadCurrentUICulture"/> for application automatically.
        /// Defaults to <c>false</c>;
        /// </summary>
        /// <remarks>
        /// Note that if you set both <see cref="IgnoreCulture"/> and <see cref="IgnoreUICulture"/>
        /// to <c>true</c> the <see cref="LocalizationDynamicMiddleware"/> will not set
        /// <see cref="CultureInfo.DefaultThreadCurrentCulture"/> and  <see cref="CultureInfo.DefaultThreadCurrentUICulture"/> automatically for application.
        /// </remarks>
        public bool IgnoreUICulture { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to set a request culture to an parent culture in the case the
        /// culture determined by the configured <see cref="ICultureProvider"/>s is not in the
        /// <see cref="SupportedCultures"/> list but a parent culture is.
        /// Defaults to <c>true</c>;
        /// </summary>
        /// <remarks>
        /// Note that the parent culture check is done using only the culture name.
        /// </remarks>
        /// <example>
        /// If this property is <c>true</c> and the application is configured to support the culture "fr", but not the
        /// culture "fr-FR", and a configured <see cref="ICultureProvider"/> determines a request's culture is
        /// "fr-FR", then the request's culture will be set to the culture "fr", as it is a parent of "fr-FR".
        /// </example>
        public bool FallBackToParentCultures { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to set a request UI culture to a parent culture in the case the
        /// UI culture determined by the configured <see cref="ICultureProvider"/>s is not in the
        /// <see cref="SupportedUICultures"/> list but a parent culture is.
        /// Defaults to <c>true</c>;
        /// </summary>
        /// <remarks>
        /// Note that the parent culture check is done using ony the culture name.
        /// </remarks>
        /// <example>
        /// If this property is <c>true</c> and the application is configured to support the UI culture "fr", but not
        /// the UI culture "fr-FR", and a configured <see cref="ICultureProvider"/> determines a request's UI
        /// culture is "fr-FR", then the request's UI culture will be set to the culture "fr", as it is a parent of
        /// "fr-FR".
        /// </example>
        public bool FallBackToParentUICultures { get; set; } = true;

        /// <summary>
        /// The cultures supported by the application.
        /// Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </summary>
        public IList<CultureInfo>? SupportedCultures { get; set; } = new List<CultureInfo> { CultureInfo.CurrentCulture };

        /// <summary>
        /// The UI cultures supported by the application.
        /// Defaults to <see cref="CultureInfo.CurrentUICulture"/>.
        /// </summary>
        public IList<CultureInfo>? SupportedUICultures { get; set; } = new List<CultureInfo> { CultureInfo.CurrentUICulture };

        /// <summary>
        /// An ordered list of providers used to determine a request's culture information. The first provider that
        /// returns a non-<c>null</c> result for a given request will be used.
        /// Defaults to the following:
        /// <list type="number">
        ///     <item><description><see cref="QueryStringCultureProvider"/></description></item>
        ///     <item><description><see cref="LocalStorageCultureProvider"/></description></item>
        ///     <item><description><see cref="AcceptLanguageHeaderCultureProvider"/></description></item>
        /// </list>
        /// </summary>
        public IList<ICultureProvider>? CultureProviders { get; set; }

        /// <summary>
        /// Adds the set of the supported cultures by the application.
        /// </summary>
        /// <param name="cultures">The cultures to be added.</param>
        /// <returns>The <see cref="LocalizationDynamicOptions"/>.</returns>
        public LocalizationDynamicOptions AddSupportedCultures(params string[] cultures)
        {
            var supportedCultures = new List<CultureInfo>(cultures.Length);
            supportedCultures.AddRange(cultures.Select(culture => new CultureInfo(culture)));

            SupportedCultures = supportedCultures;
            return this;
        }

        /// <summary>
        /// Adds the set of the supported UI cultures by the application.
        /// </summary>
        /// <param name="uiCultures">The UI cultures to be added.</param>
        /// <returns>The <see cref="LocalizationDynamicOptions"/>.</returns>
        public LocalizationDynamicOptions AddSupportedUICultures(params string[] uiCultures)
        {
            var supportedUICultures = new List<CultureInfo>(uiCultures.Length);
            supportedUICultures.AddRange(uiCultures.Select(culture => new CultureInfo(culture)));

            SupportedUICultures = supportedUICultures;
            return this;
        }

        /// <summary>
        /// Set the default culture which is used by the application when a supported culture could not be determined by
        /// one of the configured <see cref="ICultureProvider"/>s.
        /// </summary>
        /// <param name="defaultCulture">The default culture to be set.</param>
        /// <returns>The <see cref="LocalizationDynamicOptions"/>.</returns>
        public LocalizationDynamicOptions SetDefaultCulture(string defaultCulture)
        {
            DefaultCulture = new DynamicCulture(defaultCulture);
            return this;
        }
    }
}
