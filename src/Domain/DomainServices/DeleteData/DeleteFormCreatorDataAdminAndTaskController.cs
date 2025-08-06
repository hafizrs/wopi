using System;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteFormCreatorDataAdminAndTaskController : IDeleteDataByRoleSpecific
    {
        private readonly IRepository _repository;
        private readonly ILogger<DeleteFormCreatorDataAdminAndTaskController> _logger;

        public DeleteFormCreatorDataAdminAndTaskController(
            IRepository repository,
            ILogger<DeleteFormCreatorDataAdminAndTaskController> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        public bool DeleteData(string itemId)
        {
            _logger.LogInformation($"Going to delete {nameof(PraxisForm)} data with ItemId: {itemId} for admin and task controller.");

            try
            {
                _repository.Delete<PraxisForm>(f=>f.ItemId==itemId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisForm)} data for FormId:{itemId}. Exception Message: {ex.Message}. Exception Details: {ex.StackTrace}.");
                return false;
            }
        }
    }
}
