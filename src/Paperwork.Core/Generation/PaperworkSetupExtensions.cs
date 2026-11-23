using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Paperwork.Auth;
using Paperwork.Generation;
using Paperwork.Tracing;

namespace Paperwork.Generation
{
    public static class PaperworkSetupExtensions
    {
        // Used when the application has not registered its own HttpClient
        private static readonly Lazy<HttpClient> _defaultClient = new(() => new HttpClient());

        /// <summary>
        /// Registers core Paperwork services: <see cref="IPaperworkAuthService"/> (a single
        /// <see cref="PaperworkAuthWrapperService"/>, to which auth handlers are added) and
        /// <see cref="IPaperworkFactory"/>, which fetches with that auth service. The factory uses the application's
        /// <see cref="HttpClient"/>, <see cref="IPaperworkTracingService"/> and
        /// <see cref="IPaperworkRemoteFileRequestService"/> when they are registered,
        /// and defaults otherwise.
        /// </summary>
        public static IServiceCollection AddPaperwork(this IServiceCollection services)
        {
            // Built explicitly: left to the container, it would pick the wrapper's
            // IEnumerable<IPaperworkAuthService> constructor, and that list includes the
            // wrapper itself - a circular dependency (Paperwork.Core issue #1).
            services.AddSingleton<IPaperworkAuthService>(_ => new PaperworkAuthWrapperService());
            services.AddScoped<IPaperworkFactory>(sp => new PaperworkInstanceFactory(
                new System.Text.Json.JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    PropertyNameCaseInsensitive = true
                },
                sp.GetService<HttpClient>() ?? _defaultClient.Value,
                // The registered auth service, so handlers added to it (for example
                // WithDefaultAuth()) are the ones the factory fetches with.
                sp.GetRequiredService<IPaperworkAuthService>(),
                sp.GetService<IPaperworkTracingService>() ?? new PaperworkTracingService(),
                sp.GetService<IPaperworkRemoteFileRequestService>() ?? new EmptyFileRequestService()));
            return services;
        }

        /// <summary>
        /// Registers core Paperwork services, and passes the configuration to the
        /// document engine, so the "Paperwork" (or "Scryber") section - fonts, imaging,
        /// parsing, output, tracing and so on - applies to every document.
        /// See <see cref="PaperworkConfiguration.Init"/>.
        /// </summary>
        public static IServiceCollection AddPaperwork(this IServiceCollection services, IConfiguration configuration)
        {
            if (null == configuration)
                throw new ArgumentNullException(nameof(configuration));

            PaperworkConfiguration.Init(configuration);
            return services.AddPaperwork();
        }
    }
}
