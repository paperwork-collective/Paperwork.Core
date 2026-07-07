using Microsoft.Extensions.DependencyInjection;
using Paperwork.Auth;
using Paperwork.Generation;

namespace Paperwork.Generation
{
    public static class PaperworkSetupExtensions
    {
        /// <summary>
        /// Registers core Paperwork services: <see cref="IPaperworkAuthService"/>,
        /// <see cref="IPaperworkFactory"/>, and <see cref="IPaperworkTracingService"/>.
        /// </summary>
        public static IServiceCollection AddPaperwork(this IServiceCollection services)
        {
            services.AddSingleton<IPaperworkAuthService, PaperworkAuthWrapperService>();
            services.AddScoped<IPaperworkFactory, PaperworkInstanceFactory>();
            return services;
        }
    }
}
