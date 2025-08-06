using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Linq;
using System.Reflection;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices
{
    public class PraxisAssignedTaskFormService : IPraxisAssignedTaskFormService
    {
        private readonly ILogger<PraxisEquipmentMaintenanceService> _logger;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IBlocksMongoDbDataContextProvider _ecapRepository;
        private readonly IRepository _repository;

        public PraxisAssignedTaskFormService(
           ILogger<PraxisEquipmentMaintenanceService> logger,
           ISecurityContextProvider securityContextProvider,
           IBlocksMongoDbDataContextProvider ecapRepository,
           IMongoSecurityService mongoSecurityService,
           IRepository repository

       )
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _ecapRepository = ecapRepository;
            _mongoSecurityService = mongoSecurityService;
            _repository = repository;
        }
        public void CreateAssignedForm(string formId,
            string assignedEntityName,
            string assignedEntityId)
        {
            _logger.LogInformation("Enter Create AssignedForm for PG.");

            if (!string.IsNullOrEmpty(formId) && !string.IsNullOrEmpty(assignedEntityName) && !string.IsNullOrEmpty(assignedEntityId))
            {
                try
                {

                    var praxisForm = _repository.GetItem<PraxisForm>
                                          (p => p.ItemId == formId);

                    var createdAssignedTaskForm = _repository.GetItem<AssignedTaskForm>
                                          (p => p.AssignedEntityId == assignedEntityId &&
                                           p.AssignedEntityName == assignedEntityName
                                          );
                    if (praxisForm != null && createdAssignedTaskForm == null)
                    {
                        var assignedForm = new AssignedTaskForm();
                        CopyProperties(praxisForm, assignedForm);
                        assignedForm.AssignedEntityId = assignedEntityId;
                        assignedForm.AssignedEntityName = assignedEntityName;
                        assignedForm.ClonedFormId = praxisForm.ItemId;
                        assignedForm.ItemId = Guid.NewGuid().ToString();
                        _repository.Save(assignedForm);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        "Error occurred in CreateAssignedForm: {ExceptionMessage}. AssignedEntityName: {AssignedEntityName}, AssignedEntityId: {AssignedEntityId}.",
                        ex.Message, assignedEntityName, assignedEntityId);
                }
            }


        }

        public AssignedTaskForm GetAssignedForm(string assignedEntityId, string assignedEntityName)
        {
            var praxisForm = _repository.GetItem<AssignedTaskForm>
                                         (p => p.AssignedEntityId == assignedEntityId
                                         && p.AssignedEntityName == assignedEntityName
                                         && !p.IsMarkedToDelete
                                         );
            return praxisForm;

        }

        private void CopyProperties(PraxisForm source, AssignedTaskForm destination)
        {
            PropertyInfo[] sourceProperties = typeof(PraxisForm).GetProperties();
            PropertyInfo[] destinationProperties = typeof(AssignedTaskForm).GetProperties();

            foreach (var sourceProperty in sourceProperties)
            {
                var destinationProperty = destinationProperties.FirstOrDefault(p =>
                    p.Name == sourceProperty.Name &&
                    p.PropertyType == sourceProperty.PropertyType &&
                    p.CanWrite
                );

                if (destinationProperty != null)
                {
                    try
                    {
                        var value = sourceProperty.GetValue(source);
                        destinationProperty.SetValue(destination, value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error copying property '{PropertyName}': {ErrorMessage}", sourceProperty.Name, ex.Message);
                    }
                }
            }
        }

    }
}
