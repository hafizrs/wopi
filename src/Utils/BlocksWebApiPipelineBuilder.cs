using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.WopiMonitor.Utils
{
    public class BlocksWebApiPipelineBuilderOptions
    {
        public bool UseFileLogging { get; set; }
        public bool UseTracingLogging { get; set; }
        public string[] CommandLineArguments { get; set; }
        public bool UseAuditLoggerMiddleware { get; set; }
        public Action<IServiceCollection, IAppSettings> AddApplicationServices { get; set; }
        public bool UseJwtBearerAuthentication { get; set; }
        public Func<IAppSettings, string[]> AddRequiredQueues { get; set; }
        public Action<IApplicationBuilder, IAppSettings> ConfigureMiddlewares { get; set; }
    }

    public static class BlocksWebApiPipelineBuilder
    {
        public static async Task<IWebApplicationBuilder> BuildBlocksWebApiPipeline(BlocksWebApiPipelineBuilderOptions options)
        {
            var builder = WebApplication.CreateBuilder(options.CommandLineArguments);

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add HTTP client factory
            builder.Services.AddHttpClient();

            // Add CORS
            builder.Services.AddCors();

            // Add application services
            var appSettings = new AppSettings();
            options.AddApplicationServices?.Invoke(builder.Services, appSettings);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            // Configure middlewares
            options.ConfigureMiddlewares?.Invoke(app, appSettings);

            return builder;
        }
    }

    public class AppSettings : IAppSettings
    {
        public string ServiceName { get; set; } = "WopiMonitor";
        public string BlocksAuditLogQueueName { get; set; } = "wopi-audit-log";
        public string CollaboraBaseUrl { get; set; } = "https://colabora.rashed.app";
    }
} 