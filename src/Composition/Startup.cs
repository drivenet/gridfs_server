using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GridFSServer.Composition;

internal sealed class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void ConfigureServices(IServiceCollection services)
    {
#pragma warning disable CS0618 // Type or member is obsolete -- required for migration to new driver version
        MongoDB.Bson.BsonDefaults.GuidRepresentationMode = MongoDB.Bson.GuidRepresentationMode.V3;
#pragma warning restore CS0618 // Type or member is obsolete

        services.AddSingleton<Microsoft.IO.RecyclableMemoryStreamManager>();

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
        services.AddSingleton<Implementation.HttpFileServer>();
        services.AddSingleton<Components.IHttpFileServer>(
            provider => new Implementation.LoggingFileServer(
                provider.GetRequiredService<Implementation.HttpFileServer>(),
                provider.GetRequiredService<IOptionsMonitor<Components.HttpServerOptions>>(),
                provider.GetRequiredService<ILogger<Components.IHttpFileServer>>()));

        services.AddSingleton<Middleware.ReverseProxyMiddleware>();
        services.AddSingleton<Middleware.CorrelationMiddleware>();
        services.AddSingleton<Middleware.StatisticsMiddleware>();
        services.AddSingleton<Middleware.FileServerMiddleware>();
    }

    public static void Configure(IApplicationBuilder app)
    {
        app.UseMiddleware<Middleware.ReverseProxyMiddleware>();
        app.UseMiddleware<Middleware.CorrelationMiddleware>();
        app.UseMiddleware<Middleware.StatisticsMiddleware>();
        app.UseMiddleware<Middleware.FileServerMiddleware>();
    }
}
