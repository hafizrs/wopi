using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.DataFixServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DataFixServices
{
    public class DataDeleteService : IResolveProdDataIssuesService
    {
        private readonly ILogger<DataDeleteService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IChangeLogService _changeLogService;


        public DataDeleteService(
            ILogger<DataDeleteService> logger,
            ISecurityContextProvider securityContextProvider,
            IChangeLogService changeLogService)
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _changeLogService = changeLogService;
        }

        public async Task<bool> InitiateFix(ResolveProdDataIssuesCommand command)
        {
            _logger.LogInformation("Entered service: {ServiceName}", nameof(DataDeleteService));

            var response = await DeletePraxisTasks();
            if (response)
            {
                _logger.LogInformation("Successfully deleted PraxisTask data for {DepartmentCount} departments.", 2);
                _logger.LogInformation("Exiting service: {ServiceName}", nameof(DataDeleteService));
            }
            else 
            {
                _logger.LogError($"Error occured during PraxisTask data delete for 2 departments.");
                _logger.LogInformation("Exiting service: {ServiceName}", nameof(DataDeleteService));
            }

            return response;
        }

        private async Task<bool> DeletePraxisTasks()
        {
            var updates = PreparePraxisTaskUpdates();

            var departmentIds = new string[] { "f05e7742-b624-428d-80cb-00206f8c2302" };
            var targetDate = DateTime.Parse("2023-01-10T00:00:00.000Z");

            var builder = Builders<BsonDocument>.Filter;
            var updateFilters = builder.In("ClientId", departmentIds) &
                builder.Eq("IsMarkedToDelete", false) &
                builder.Lt("CreateDate", targetDate);

            return await _changeLogService.UpdateChange(nameof(PraxisTask), updateFilters, updates);
        }

        private Dictionary<string, object> PreparePraxisTaskUpdates()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            var updates = new Dictionary<string, object>
            {
                {"IsMarkedToDelete", true},
                {"LastUpdateDate",  DateTime.UtcNow.ToLocalTime()},
                {"LastUpdatedBy", securityContext.UserId},
            };

            return updates;
        }
    }
}