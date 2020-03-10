using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GridFSServer.Composition
{
    internal sealed class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Configure<Components.HttpServerOptions>(_configuration.GetSection("httpServer"));
            services.AddSingleton<Implementation.IGridFSFileSourceFactory, Implementation.GridFSFileSourceFactory>();
            services.AddSingleton<Implementation.IGridFSErrorHandler, Implementation.GridFSErrorHandler>();
            services.AddSingleton<Implementation.ConfigBasedMongoUrlResolver>();
            services.AddSingleton<Implementation.IMongoUrlResolver>(provider =>
                new Implementation.TimedCachingMongoUrlResolver(provider.GetRequiredService<Implementation.ConfigBasedMongoUrlResolver>()));
            services.AddSingleton<Implementation.GridFSFileSourceResolver>();
            services.AddSingleton<Implementation.IGridFSFileSourceResolver>(provider =>
                new Implementation.CachingGridFSFileSourceResolver(provider.GetRequiredService<Implementation.GridFSFileSourceResolver>()));
            services.AddSingleton<Implementation.DefaultGridFSFileSourceResolver>();
            services.AddSingleton<Components.IFileSourceResolver>(provider =>
                provider.GetRequiredService<Implementation.DefaultGridFSFileSourceResolver>());
            services.AddSingleton<IContentTypeProvider, FileExtensionContentTypeProvider>();
            services.AddSingleton<Components.IHttpFileServer, Implementation.HttpFileServer>();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseMiddleware<Middleware.ReverseProxyMiddleware>();
            app.UseMiddleware<Middleware.CorrelationMiddleware>();

            app.UseMiddleware<Middleware.StatisticsMiddleware>();

            if (!_configuration.GetValue("disableBuffering", false))
            {
                app.UseResponseBuffering();
            }

            app.UseMiddleware<Middleware.FileServerMiddleware>();
        }
    }
}
