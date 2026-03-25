using Microsoft.Extensions.DependencyInjection;
using Paperwork.Services;
using Paperwork.Services.Auth;
using Paperwork.Services.Generation;

namespace Paperwork
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
