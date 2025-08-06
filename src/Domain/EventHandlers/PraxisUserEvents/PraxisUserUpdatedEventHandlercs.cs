using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisUserEvents
{
    public class PraxisUserUpdatedEventHandler : IBaseEventHandler<GqlEvent<PraxisUser>>
    {
        private readonly ILogger<PraxisUserUpdatedEventHandler> _logger;
        private readonly IUserCountMaintainService _userCountMaintainService;
        private readonly IRepository _repository;
        public PraxisUserUpdatedEventHandler(
            ILogger<PraxisUserUpdatedEventHandler> logger,
            IUserCountMaintainService userCountMaintainService,
            IRepository repository)
        {
            _logger = logger;
            _userCountMaintainService = userCountMaintainService;
            _repository = repository;
        }

        public bool Handle(GqlEvent<PraxisUser> eventPayload)
        {
            _logger.LogInformation("Enter {EventHandlerName} with praxisUserId: {PraxisUserId}",
                nameof(PraxisUserUpdatedEventHandler), eventPayload.Filter);
            try
            {
                if (eventPayload.Filter != null)
                {
                    var primaryDept = _repository.GetItem<PraxisUser>(p => !p.IsMarkedToDelete && p.ItemId == eventPayload.Filter)?.ClientList?.FirstOrDefault(c => c.IsPrimaryDepartment);
                    if (primaryDept != null)
                    {
                        _userCountMaintainService.InitiateUserCountUpdateProcessOnUserCreate(primaryDept.ClientId, primaryDept.ParentOrganizationId).GetAwaiter().GetResult();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during {handleName} with ItemId: {ItemId}. Exception message: {ExceptionMessage}. Exception Details: {ExceptionDetails}.",
                    nameof(PraxisUserUpdatedEventHandler), eventPayload.Filter, ex.Message, ex.StackTrace);
            }

            return false;
        }
    }
}
