using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.CommandHandlers;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;
using Selise.Ecap.SC.PraxisMonitor.QueryHandlers;
using Selise.Ecap.SC.PraxisMonitor.ValidationHandlers;
using Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeliseBlocks.GraphQL.Extensions;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.ClientModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.AbsenceModule;

namespace Selise.Ecap.SC.PraxisMonitor.WebService
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

            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120)
            });

            app.UseRouting();

            app.Map("/conversation/chat", WebSocketCollectionExtensions.HandleAIWebSocketConnection);

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
            container.RegisterCollection(typeof(IQueryHandler<,>), new[]
            {
                typeof(GetDistinctTaskListQueryHandler).Assembly
            });
            container.RegisterCollection(typeof(ICommandHandler<,>), new[]
            {
                typeof(DataProcessCommandHandler).Assembly
            });
            container.RegisterCollection(typeof(IValidationHandler<,>), new[]
            {
                typeof(ExportTaskListReportValidationHandler).Assembly
            });

            container.AddRegisterAllDerivedTypesServices();

            #region CIRS Reports
            container.AddCirsScrumboardServices();
            container.AddCirsScrumboardCommandValidators();
            #endregion

            #region RiqsInterface Module
            container.AddRiqsInterfaceServices();
            #endregion RiqsInterface Module

            #region AI Module
            container.AddAIModuleServices();
            #endregion AI Module

            #region Subscriptions Module
            container.AddSubscriptionsServices();
            #endregion Subscriptions Module

            #region Library Module
            container.AddLibraryModuleServices();
            #endregion Library Module

            #region Cockpit Module
            container.AddCockpitModuleServices();
            #endregion

            #region Cockpit Module
            container.AddCockpitModuleServices();
            #endregion

            #region Validator
            container.AddCommandValidator();
            #endregion

            #region Praxis Business
            container.AddPraxisBusinessServices();
            #endregion

            #region Client Module
            container.AddClientModuleServices();
            #endregion

            #region Configurator Module
            container.AddConfiguratorModuleServices();
            #endregion Configuration Module
            container.AddSingleton<IReportTemplateSignatureService, ReportTemplateSignatureService>();

            #region Absence Module
            container.AddAbsenceModuleDomainServices();
            #endregion Absence Module

            // Add service locator in the end.
            ServiceLocator.Initialize(container.BuildServiceProvider());
        }


    }
}
