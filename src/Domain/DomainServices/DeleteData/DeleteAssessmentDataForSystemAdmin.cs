using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteAssessmentDataForSystemAdmin : IDeleteDataByRoleSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteAssessmentDataForSystemAdmin> _logger;

        public DeleteAssessmentDataForSystemAdmin(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteAssessmentDataForSystemAdmin> logger)
        {
            _securityContextProviderService = securityContextProviderService;
            _repository = repository;
            _logger = logger;
        }
        public bool DeleteData(string itemId)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation($"Going to delete {nameof(PraxisAssessment)} data with ItemId: {itemId} for admin.");

            try
            {
                var existingAssessment = _repository.GetItem<PraxisAssessment>(a => a.ItemId == itemId);
                if (existingAssessment != null)
                {
                    var assessmentList = _repository
                        .GetItems<PraxisAssessment>(a => a.RiskId == existingAssessment.RiskId)
                        .ToList();
                    if (assessmentList.Count > 1)
                    {
                        var existingRisk = _repository.GetItem<PraxisRisk>(r => r.ItemId == existingAssessment.RiskId);
                        if (existingRisk.RecentAssessment.ItemId == itemId)
                        {
                            var recentAssessment = assessmentList.Where(a => a.ItemId != existingAssessment.ItemId)
                                .OrderByDescending(o => o.CreateDate).FirstOrDefault();
                            if (recentAssessment != null)
                            {
                                existingRisk.RecentAssessment = recentAssessment;
                                RemoveAssessmentAndUpDateRiskData(existingRisk, existingAssessment.ItemId);
                            }
                        }
                        else
                        {
                            _repository.Delete<PraxisAssessment>(a => a.ItemId == existingAssessment.ItemId);
                            _repository.Delete<ArchivedRole>(ar =>
                                ar.EntityName == nameof(PraxisAssessment) &&
                                ar.EntityItemId == existingAssessment.ItemId);
                        }
                    }
                }
                _logger.LogInformation($"Data has been successfully deleted from {nameof(PraxisAssessment)} entity with ItemId: {itemId} for tenantId: {securityContext.TenantId}.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisAssessment)} related all data with ItemId: {itemId} and tenantId: {securityContext.TenantId} for Admin role. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                return false;
            }
        }

        private void RemoveAssessmentAndUpDateRiskData(PraxisRisk existingRisk, string assessmentItemId)
        {
            _repository.Update<PraxisRisk>(r => r.ItemId == existingRisk.ItemId, existingRisk);
            _repository.Delete<PraxisAssessment>(a => a.ItemId == assessmentItemId);
            _repository.Delete<ArchivedRole>(ar => ar.EntityName == nameof(PraxisAssessment) && ar.EntityItemId == assessmentItemId);
        }
    }
}
