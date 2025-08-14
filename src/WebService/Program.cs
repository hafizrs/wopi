using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Selise.Ecap.Entities.PrimaryEntities.IDM;
using Selise.Ecap.SC.Wopi.Contracts.DomainServices.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.Infrastructure;
using Selise.Ecap.SC.Wopi.Domain.DomainServices.WopiModule;
using Selise.Ecap.SC.Wopi.ValidationHandlers;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.Wopi.WebService
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            //BlocksWebApiPipelineBuilderOptions blocksWebApiPipelineBuilderOptions = new BlocksWebApiPipelineBuilderOptions
            //{
            //    UseFileLogging = true,
            //    UseTracingLogging = true,
            //    CommandLineArguments = args,
            //    UseAuditLoggerMiddleware = true,
            //    AddApplicationServices = AddApplicationServices,
            //    UseJwtBearerAuthentication = true,
            //    AddRequiredQueues = AddRequiredQueues,
            //    ConfigureMiddlewares = ConfigureMiddlewares
            //};

            //// Build and configure the web API pipeline using EcapWebApiPipelineBuilder
            //var pipeline = await BlocksWebApiPipelineBuilder.BuildBlocksWebApiPipeline(blocksWebApiPipelineBuilderOptions);
            //pipeline.Build().Run();
            var builder = WebApplication.CreateBuilder();

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddHttpClient();
            AddApplicationServices(builder.Services);

            // Configure CORS for WOPI
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .WithExposedHeaders("Content-Disposition", "X-WOPI-ItemVersion", "X-WOPI-Lock")
                          .WithHeaders("Origin", "X-Requested-With", "Content-Type", "Accept", "Authorization",
                               "X-WOPI-Override", "X-WOPI-Lock", "X-WOPI-ItemVersion");
                });
            });

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep PascalCase
            });

            var app = builder.Build();
            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors();
            app.UseRouting();
            app.MapControllers();

            app.Run();

        }

        private static void ConfigureMiddlewares(IApplicationBuilder app, IAppSettings appSettings)
        {
            app.UseCors((corsPolicyBuilder) =>
            {
                corsPolicyBuilder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .SetIsOriginAllowed((origin) => true)
                    .AllowCredentials()
                    .WithExposedHeaders("Content-Disposition", "X-WOPI-ItemVersion", "X-WOPI-Lock")
                    .WithHeaders("Origin", "X-Requested-With", "Content-Type", "Accept", "Authorization", 
                               "X-WOPI-Override", "X-WOPI-Lock", "X-WOPI-ItemVersion")
                    .SetPreflightMaxAge(TimeSpan.FromDays(365));
            });

            app.UseAuthorization();

            app.UseAuthentication();

            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120)
            });

            app.UseRouting();

            app.UseMvc(routeBuilder =>
            {
                routeBuilder.MapRoute("Default", appSettings.ServiceName + "/{controller}/{action}/{id?}");
            });
        }

        private static IEnumerable<string> AddRequiredQueues(IAppSettings appSettings)
        {
            return new[] { appSettings.BlocksAuditLogQueueName };
        }

        private static void AddApplicationServices(IServiceCollection container)
        {


            container.AddSingleton<QueryHandler>();
            container.AddSingleton<CommandHandler>();
            container.AddSingleton<ValidationHandler>();

            #region Validator
            container.AddCommandValidator();
            #endregion
            #region Wopi Business
            container.AddWopiBusinessServices();
            #endregion

            // Add service locator in the end.
            ServiceLocator.Initialize(container.BuildServiceProvider());
        }


    }
}
