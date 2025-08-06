using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.WopiMonitor.CommandHandlers;
using Selise.Ecap.SC.WopiMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.WopiMonitor.QueryHandlers;
using Selise.Ecap.SC.WopiMonitor.ValidationHandlers;
using Selise.Ecap.SC.WopiMonitor.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.WopiMonitor.Domain.DomainServices.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Contracts.DomainServices.WopiModule;

namespace Selise.Ecap.SC.WopiMonitor.WebService
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            BlocksWebApiPipelineBuilderOptions blocksWebApiPipelineBuilderOptions = new BlocksWebApiPipelineBuilderOptions
            {
                UseFileLogging = true,
                UseTracingLogging = true,
                CommandLineArguments = args,
                UseAuditLoggerMiddleware = true,
                AddApplicationServices = AddApplicationServices,
                UseJwtBearerAuthentication = true,
                AddRequiredQueues = AddRequiredQueues,
                ConfigureMiddlewares = ConfigureMiddlewares
            };

            // Build and configure the web API pipeline using EcapWebApiPipelineBuilder
            var pipeline = await BlocksWebApiPipelineBuilder.BuildBlocksWebApiPipeline(blocksWebApiPipelineBuilderOptions);
            pipeline.Build().Run();
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
                    .WithExposedHeaders("Content-Disposition")
                    .SetPreflightMaxAge(TimeSpan.FromDays(365));
            });

            app.UseAuthorization();
            app.UseAuthentication();

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

        private static void AddApplicationServices(IServiceCollection container, IAppSettings appSettings)
        {
            container.AddSingleton<QueryHandler>();
            container.AddSingleton<CommandHandler>();
            container.AddSingleton<ValidationHandler>();
            container.AddSingleton<IServiceClient, ServiceClient>();
            
            container.RegisterCollection(typeof(IQueryHandler<,>), new[]
            {
                typeof(GetWopiSessionsQueryHandler).Assembly
            });
            
            container.RegisterCollection(typeof(ICommandHandler<,>), new[]
            {
                typeof(CreateWopiSessionCommandHandler).Assembly
            });
            
            container.RegisterCollection(typeof(IValidationHandler<,>), new[]
            {
                typeof(CreateWopiSessionCommandValidator).Assembly
            });

            container.AddRegisterAllDerivedTypesServices();

            #region WOPI Module
            container.AddWopiModuleServices();
            container.AddWopiModuleCommandValidators();
            #endregion

            #region Validator
            container.AddCommandValidator();
            #endregion

            // Add service locator in the end.
            ServiceLocator.Initialize(container.BuildServiceProvider());
        }
    }
}
