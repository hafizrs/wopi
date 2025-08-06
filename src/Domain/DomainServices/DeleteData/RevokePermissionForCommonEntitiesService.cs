using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class RevokePermissionForCommonEntitiesService : IRevokePermissionForCommonEntities
    {
        private readonly IRepository _repository;
        private readonly ILogger<RevokePermissionForCommonEntitiesService> _logger;

        public RevokePermissionForCommonEntitiesService(
            IRepository repository,
            ILogger<RevokePermissionForCommonEntitiesService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task UpdateOpenItemConfigPermissionForMPAGroupUser(string personId, string userId)
        {
            _logger.LogInformation($"Going to update executing group permission for all {nameof(PraxisOpenItemConfig)} entity data for user: {userId}.");
            try
            {
                var existingOpenItemConfigs = _repository.GetItems<PraxisOpenItemConfig>(o => o.SpecificControlledMembers.Contains(personId) && !o.IsMarkedToDelete).ToList();
                foreach (var openItemConfig in existingOpenItemConfigs)
                {
                    var specificControlledMembers = openItemConfig.SpecificControlledMembers?.Where(r => !r.Contains(personId));
                    openItemConfig.SpecificControlledMembers = specificControlledMembers.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        { "specificControlledMembers", openItemConfig.SpecificControlledMembers}
                    };

                    await _repository.UpdateAsync<PraxisOpenItemConfig>(o => o.ItemId == openItemConfig.ItemId, updates);
                    _logger.LogInformation($"Data has been successfully updated for {nameof(PraxisOpenItemConfig)} entity with ItemId: {openItemConfig.ItemId}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update for executing group permission for all {EntityName} data for user: {UserId}. Exception Message: {ExceptionMessage}. Exception Details: {ExceptionStackTrace}",
                nameof(PraxisOpenItemConfig),
                userId,
                ex.Message,
                ex.StackTrace);

            }
        }

        public async Task UpdateOpenItemPermissionForMPAGroupUser(string personId, string userId)
        {
            _logger.LogInformation("Going to update executing group permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisOpenItem), userId);


            try
            {
                var existingOpenItems = _repository.GetItems<PraxisOpenItem>(o => o.ControlledMembers.Contains(personId) && !o.IsMarkedToDelete).ToList();
                foreach (var openItem in existingOpenItems)
                {
                    var controlledMembers = openItem.ControlledMembers?.Where(r => !r.Contains(personId));
                    openItem.ControlledMembers = controlledMembers.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"ControlledMembers", openItem.ControlledMembers}
                    };

                    await _repository.UpdateAsync<PraxisOpenItem>(o => o.ItemId == openItem.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisOpenItem), openItem.ItemId);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update for executing group permission for all {EntityName} data for user: {UserId}. Exception Message: {ExceptionMessage}. Exception Details: {ExceptionStackTrace}",
                 nameof(PraxisOpenItem),
                 userId,
                 ex.Message,
                 ex.StackTrace);

            }
        }

        public async Task UpdatePermissionForCategoryData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisClientCategory), userId);

            try
            {
                var existingCategories = _repository.GetItems<PraxisClientCategory>(c => c.IdsAllowedToRead.Contains(userId) || c.IdsAllowedToUpdate.Contains(userId) && !c.IsMarkedToDelete).ToList();

                foreach (var category in existingCategories)
                {
                    var updatedIdsAllowToRead = category.IdsAllowedToRead?.Where(i => !i.Contains(userId));
                    var updatedIdsAllowToUpdate = category.IdsAllowedToUpdate?.Where(i => !i.Contains(userId));
                    var updatedIdsAllowToDelete = category.IdsAllowedToDelete?.Where(i => !i.Contains(userId));

                    category.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    category.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    category.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", category.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", category.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", category.IdsAllowedToDelete},
                    };

                    await _repository.UpdateAsync<PraxisClientCategory>(c => c.ItemId == category.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisClientCategory), category.ItemId);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update {EntityName} data for user: {UserId}. Exception Message: {ExceptionMessage}. Exception Details: {ExceptionStackTrace}",
                nameof(PraxisClientCategory),
                userId,
                ex.Message,
                ex.StackTrace);

            }
        }

        public async Task UpdatePermissionForEquipmentData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisEquipment), userId);

            try
            {
                var existingEquipments = _repository.GetItems<PraxisEquipment>(f => f.IdsAllowedToRead.Contains(userId) || f.IdsAllowedToUpdate.Contains(userId) && !f.IsMarkedToDelete).ToList();
                foreach (var equipment in existingEquipments)
                {
                    var updatedIdsAllowToRead = equipment.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = equipment.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = equipment.IdsAllowedToDelete?.Where(i => !i.Contains(userId));

                    equipment.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    equipment.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    equipment.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", equipment.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", equipment.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", equipment.IdsAllowedToDelete},
                    };

                    await _repository.UpdateAsync<PraxisEquipment>(f => f.ItemId == equipment.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisEquipment), equipment.ItemId);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update {EntityName} data for user: {UserId}. Exception Message: {ExceptionMessage}. Exception Details: {ExceptionStackTrace}",
                nameof(PraxisEquipment),
                userId,
                ex.Message,
                ex.StackTrace);

            }
        }

        public async Task UpdatePermissionForEquipmentMaintenanceData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisEquipmentMaintenance), userId);

            try
            {
                var existingMaintananceList = _repository.GetItems<PraxisEquipmentMaintenance>(m => m.IdsAllowedToRead.Contains(userId) || m.IdsAllowedToUpdate.Contains(userId) && !m.IsMarkedToDelete).ToList();
                foreach (var maintainance in existingMaintananceList)
                {
                    var updatedIdsAllowToRead = maintainance.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = maintainance.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = maintainance.IdsAllowedToDelete?.Where(i => !i.Contains(userId));

                    maintainance.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    maintainance.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    maintainance.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", maintainance.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", maintainance.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", maintainance.IdsAllowedToDelete},
                    };

                    await _repository.UpdateAsync<PraxisEquipmentMaintenance>(f => f.ItemId == maintainance.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisEquipmentMaintenance), maintainance.ItemId);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update {EntityName} data for user: {UserId}. Exception Message: {ExceptionMessage}. Exception Details: {ExceptionStackTrace}",
                nameof(PraxisEquipmentMaintenance),
                userId,
                ex.Message,
                ex.StackTrace);

            }
        }

        public async Task UpdatePermissionForFormData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisForm), userId);
            try
            {
                var existingForms = _repository.GetItems<PraxisForm>(f => f.IdsAllowedToRead.Contains(userId) || f.IdsAllowedToUpdate.Contains(userId) && !f.IsMarkedToDelete).ToList();
                foreach (var form in existingForms)
                {
                    var updatedIdsAllowToRead = form.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = form.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = form.IdsAllowedToDelete?.Where(i => !i.Contains(userId));

                    form.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    form.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    form.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", form.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", form.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", form.IdsAllowedToDelete},
                    };

                    await _repository.UpdateAsync<PraxisForm>(f => f.ItemId == form.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisForm), form.ItemId);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of {EntityName} data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PraxisForm), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdatePermissionForOpenItemCompletionInfoData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisOpenItemCompletionInfo), userId);
            try
            {
                var existingAnswerList = _repository.GetItems<PraxisOpenItemCompletionInfo>(a => a.IdsAllowedToRead.Contains(userId) && !a.IsMarkedToDelete).ToList();
                foreach (var answer in existingAnswerList)
                {
                    var updatedIdsAllowToRead = answer.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = answer.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = answer.IdsAllowedToDelete?.Where(r => !r.Contains(userId));

                    answer.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    answer.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    answer.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", answer.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", answer.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", answer.IdsAllowedToDelete}
                    };

                    await _repository.UpdateAsync<PraxisOpenItemCompletionInfo>(a => a.ItemId == answer.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisOpenItemCompletionInfo), answer.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of {EntityName} entity data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PraxisOpenItemCompletionInfo), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdatePermissionForOpenItemConfigData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisOpenItemConfig), userId);
            try
            {
                var existingOpenItemConfigs = _repository.GetItems<PraxisOpenItemConfig>(o => o.IdsAllowedToRead.Contains(userId) || o.IdsAllowedToUpdate.Contains(userId) || o.SpecificControllingMembers.Contains(userId) && !o.IsMarkedToDelete).ToList();
                foreach (var openItemConfig in existingOpenItemConfigs)
                {
                    var updatedIdsAllowToRead = openItemConfig.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = openItemConfig.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = openItemConfig.IdsAllowedToDelete?.Where(r => !r.Contains(userId));
                    var controllingMembers = openItemConfig.SpecificControllingMembers?.Where(r => !r.Contains(userId));

                    openItemConfig.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    openItemConfig.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    openItemConfig.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();
                    openItemConfig.SpecificControllingMembers = controllingMembers.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", openItemConfig.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", openItemConfig.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", openItemConfig.IdsAllowedToDelete},
                        { "SpecificControllingMembers", openItemConfig.SpecificControllingMembers}
                    };

                    await _repository.UpdateAsync<PraxisOpenItemConfig>(o => o.ItemId == openItemConfig.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisOpenItemConfig), openItemConfig.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of {EntityName} entity data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PraxisOpenItemConfig), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdatePermissionForOpenItemData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisOpenItem), userId);
            try
            {
                var existingOpenItems = _repository.GetItems<PraxisOpenItem>(o => o.IdsAllowedToRead.Contains(userId) || o.IdsAllowedToUpdate.Contains(userId) || o.ControllingMembers.Contains(userId) && !o.IsMarkedToDelete).ToList();
                foreach (var openItem in existingOpenItems)
                {
                    var updatedIdsAllowToRead = openItem.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = openItem.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = openItem.IdsAllowedToDelete?.Where(r => !r.Contains(userId));
                    var controllingMembers = openItem.ControllingMembers?.Where(r => !r.Contains(userId));

                    openItem.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    openItem.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    openItem.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();
                    openItem.ControllingMembers = controllingMembers.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", openItem.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", openItem.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", openItem.IdsAllowedToDelete},
                        {"ControllingMembers", openItem.ControllingMembers}
                    };

                    await _repository.UpdateAsync<PraxisOpenItem>(o => o.ItemId == openItem.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisOpenItem), openItem.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of {EntityName} entity data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PraxisOpenItem), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdatePermissionForPraxisTaskData(string userId, string personId, string role)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisTask), userId);
            try
            {
                var taskList = _repository.GetItems<PraxisTask>(t => t.IdsAllowedToRead.Contains(userId) || t.IdsAllowedToUpdate.Contains(userId) && !t.IsMarkedToDelete).ToList();
                foreach (var task in taskList)
                {
                    var updatedIdsAllowToRead = task.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = task.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = task.IdsAllowedToDelete?.Where(r => !r.Contains(userId));

                    task.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    task.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    task.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();

                    if (role == RoleNames.Leitung)
                    {
                        var updatedControllingMembers = task.ControllingMembers.Where(c => !c.Contains(personId));
                        task.ControllingMembers = updatedControllingMembers.ToArray();
                    }

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", task.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", task.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", task.IdsAllowedToDelete},
                        {"SpecificControllingMembers", task.ControllingMembers}
                    };

                    await _repository.UpdateAsync<PraxisTask>(c => c.ItemId == task.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisTask), task.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of {EntityName} entity data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PraxisTask), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdatePermissionForRiskAssessmentData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisAssessment), userId);
            try
            {
                var existingAssessments = _repository.GetItems<PraxisAssessment>(a => a.IdsAllowedToRead.Contains(userId) || a.IdsAllowedToUpdate.Contains(userId) && !a.IsMarkedToDelete);
                foreach (var assessment in existingAssessments)
                {
                    var updatedIdsAllowToRead = assessment.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = assessment.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = assessment.IdsAllowedToDelete?.Where(r => !r.Contains(userId));

                    assessment.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    assessment.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    assessment.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", assessment.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", assessment.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", assessment.IdsAllowedToDelete}
                    };

                    await _repository.UpdateAsync<PraxisAssessment>(r => r.ItemId == assessment.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisAssessment), assessment.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of {EntityName} entity data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PraxisAssessment), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdatePermissionForRiskData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisRisk), userId);
            try
            {
                var existingRisks = _repository.GetItems<PraxisRisk>(r => r.IdsAllowedToRead.Contains(userId) || r.IdsAllowedToUpdate.Contains(userId) || r.RiskOwners.Contains(userId) || r.RiskProfessionals.Contains(userId) && !r.IsMarkedToDelete).ToList();
                foreach (var risk in existingRisks)
                {
                    var updatedIdsAllowToRead = risk.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = risk.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = risk.IdsAllowedToDelete?.Where(r => !r.Contains(userId));
                    var riskOwners = risk.RiskOwners?.Where(r => !r.Contains(userId));
                    var riskProfessionals = risk.RiskProfessionals?.Where(r => !r.Contains(userId));

                    risk.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    risk.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    risk.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();
                    risk.RiskOwners = riskOwners;
                    risk.RiskProfessionals = riskProfessionals;

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", risk.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", risk.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", risk.IdsAllowedToDelete},
                        {"RiskOwners", risk.RiskOwners},
                        { "RiskProfessionals", risk.RiskProfessionals}
                    };

                    await _repository.UpdateAsync<PraxisRisk>(r => r.ItemId == risk.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisRisk), risk.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of {EntityName} entity data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PraxisRisk), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdatePermissionForRoomData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisRoom), userId);
            try
            {
                var existingRoomList = _repository.GetItems<PraxisRoom>(c => c.IdsAllowedToRead.Contains(userId) && !c.IsMarkedToDelete).ToList();
                foreach (var room in existingRoomList)
                {
                    var updatedIdsAllowToRead = room.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = room.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = room.IdsAllowedToDelete?.Where(r => !r.Contains(userId));

                    room.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    room.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    room.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", room.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", room.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", room.IdsAllowedToDelete},
                    };

                    await _repository.UpdateAsync<PraxisRoom>(c => c.ItemId == room.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisRoom), room.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of {EntityName} entity data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PraxisRoom), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdatePermissionForTaskConfigData(string userId, string personId, string role)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisTaskConfig), userId);
            try
            {
                var existingConfigList = _repository.GetItems<PraxisTaskConfig>(c => c.IdsAllowedToRead.Contains(userId) || c.IdsAllowedToUpdate.Contains(userId) && !c.IsMarkedToDelete).ToList();
                foreach (var taskConfig in existingConfigList)
                {
                    var updatedIdsAllowToRead = taskConfig.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = taskConfig.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = taskConfig.IdsAllowedToDelete?.Where(r => !r.Contains(userId));

                    taskConfig.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    taskConfig.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    taskConfig.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();

                    if (role == RoleNames.Leitung)
                    {
                        var updatedControllingMembers = taskConfig.SpecificControllingMembers.Where(c => !c.Contains(personId));
                        taskConfig.SpecificControllingMembers = updatedControllingMembers.ToArray();
                    }

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", taskConfig.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", taskConfig.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", taskConfig.IdsAllowedToDelete},
                        {"SpecificControllingMembers", taskConfig.SpecificControllingMembers}
                    };

                    await _repository.UpdateAsync<PraxisTaskConfig>(c => c.ItemId == taskConfig.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisTaskConfig), taskConfig.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of {EntityName} entity data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PraxisTaskConfig), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdatePermissionForTaskScheduleData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(TaskSchedule), userId);
            try
            {
                var existingSchedules = _repository.GetItems<TaskSchedule>(s => s.IdsAllowedToRead.Contains(userId) || s.IdsAllowedToUpdate.Contains(userId) && !s.IsMarkedToDelete).ToList();

                foreach (var schedule in existingSchedules)
                {
                    var updatedIdsAllowToRead = schedule.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = schedule.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = schedule.IdsAllowedToDelete?.Where(r => !r.Contains(userId));

                    schedule.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    schedule.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    schedule.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", schedule.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", schedule.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", schedule.IdsAllowedToDelete}
                    };

                    await _repository.UpdateAsync<TaskSchedule>(r => r.ItemId == schedule.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(TaskSchedule), schedule.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of {EntityName} entity data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(TaskSchedule), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdatePermissionForTaskSummaryData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(TaskSummary), userId);
            try
            {
                var existingSummarys = _repository.GetItems<TaskSummary>(s => s.IdsAllowedToRead.Contains(userId) || s.IdsAllowedToUpdate.Contains(userId) && !s.IsMarkedToDelete).ToList();

                foreach (var summary in existingSummarys)
                {
                    var updatedIdsAllowToRead = summary.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = summary.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = summary.IdsAllowedToDelete?.Where(r => !r.Contains(userId));

                    summary.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    summary.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    summary.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", summary.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", summary.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", summary.IdsAllowedToDelete}
                    };

                    await _repository.UpdateAsync<TaskSummary>(r => r.ItemId == summary.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(TaskSummary), summary.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of {EntityName} entity data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(TaskSummary), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdatePermissionForTrainingAnswerData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisTrainingAnswer), userId);
            try
            {
                var existingAnswerList = _repository.GetItems<PraxisTrainingAnswer>(a => a.IdsAllowedToRead.Contains(userId) && !a.IsMarkedToDelete).ToList();
                foreach (var answer in existingAnswerList)
                {
                    var updatedIdsAllowToRead = answer.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = answer.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = answer.IdsAllowedToDelete?.Where(r => !r.Contains(userId));

                    answer.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    answer.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    answer.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", answer.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", answer.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", answer.IdsAllowedToDelete}
                    };

                    await _repository.UpdateAsync<PraxisTrainingAnswer>(a => a.ItemId == answer.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisTrainingAnswer), answer.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of {EntityName} entity data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PraxisTrainingAnswer), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdatePraxisTaskPermissionForMPAGroupUser(string personId, string userId)
        {
            _logger.LogInformation("Going to update all Executing Group permission for {EntityName} entity data for user: {UserId}", nameof(PraxisTask), userId);
            try
            {
                var taskList = _repository.GetItems<PraxisTask>(t => t.ControlledMembers.Contains(personId) && !t.IsMarkedToDelete).ToList();
                foreach (var task in taskList)
                {
                    var controlledMembers = task.ControlledMembers?.Where(r => !r.Contains(personId));

                    task.ControlledMembers = controlledMembers.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"ControlledMembers", task.ControlledMembers}
                    };

                    await _repository.UpdateAsync<PraxisTask>(c => c.ItemId == task.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisTask), task.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of all Executing Group permission for {EntityName} entity data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PraxisTask), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdateTaskConfigPermissionForMPAGroupUser(string personId, string userId)
        {
            _logger.LogInformation("Going to update all Executing Group permission for {EntityName} entity data for user: {UserId}", nameof(PraxisTaskConfig), userId);
            try
            {
                var existingConfigList = _repository.GetItems<PraxisTaskConfig>(c => c.SpecificControlledMembers.Contains(personId) && !c.IsMarkedToDelete).ToList();
                foreach (var taskConfig in existingConfigList)
                {
                    var specificControlledMembers = taskConfig.SpecificControlledMembers?.Where(r => !r.Contains(userId));

                    taskConfig.SpecificControlledMembers = specificControlledMembers?.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"SpecificControlledMembers", taskConfig.SpecificControlledMembers}
                    };

                    await _repository.UpdateAsync<PraxisTaskConfig>(c => c.ItemId == taskConfig.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisTaskConfig), taskConfig.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of all Executing Group permission for {EntityName} entity data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PraxisTaskConfig), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdateTrainingPermissionForMPAGroupUser(string personId, string userId)
        {
            _logger.LogInformation("Going to update Training Participants permission for all {EntityName} entity data for user: {UserId}", nameof(PraxisTraining), userId);
            try
            {
                var existingTrainingList = _repository.GetItems<PraxisTraining>(t => t.SpecificControlledMembers.Contains(personId) && !t.IsMarkedToDelete).ToList();
                foreach (var training in existingTrainingList)
                {
                    var specificControlledMembers = training.SpecificControlledMembers?.Where(r => !r.Contains(personId));
                    training.SpecificControlledMembers = specificControlledMembers;

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"SpecificControlledMembers", training.SpecificControlledMembers}
                    };

                    await _repository.UpdateAsync<PraxisTraining>(a => a.ItemId == training.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}", nameof(PraxisTraining), training.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update of Training Participants permission for all {EntityName} entity data for user: {UserId}. Exception Message: {Message}. Exception Details: {StackTrace}", nameof(PraxisTraining), userId, ex.Message, ex.StackTrace);
            }
        }
    }
}
