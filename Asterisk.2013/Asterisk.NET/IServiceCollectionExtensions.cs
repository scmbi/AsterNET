using AsterNET.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsterNET
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddAsteriskManager(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var configuration = provider.GetRequiredService<IConfiguration>();
            return services.AddAsteriskManager(configuration);
        }

        public static IServiceCollection AddAsteriskManager(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ListenerOptions>(configuration.GetSection(ListenerOptions.SECTIONNAME));

            services.AddTransient<AGIServerSocketHandler>();
            return services;
        }
    }
}
