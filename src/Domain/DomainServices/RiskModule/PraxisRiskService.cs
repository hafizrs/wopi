using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.PraxisRiskConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisRiskService : IPraxisRiskService, IDeleteDataForClientInCollections
    {
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly IRepository _repository;
        private readonly IPraxisAssessmentService _praxisAssessmentService;
        private readonly ILogger<PraxisRiskService> _logger;
        private readonly ICommonUtilService _commonUtilService;
        private readonly ICirsRiskManagementAttachmentService _cirsRiskManagementAttachmentService;

        public PraxisRiskService(
            IMongoSecurityService mongoSecurityService,
            IPraxisAssessmentService praxisAssessmentService,
            IRepository repository,
            ILogger<PraxisRiskService> logger,
            ICommonUtilService commonUtilService,
            ICirsRiskManagementAttachmentService cirsRiskManagementAttachmentService
        )
        {
            _mongoSecurityService = mongoSecurityService;
            _praxisAssessmentService = praxisAssessmentService;
            _repository = repository;
            _logger = logger;
            _commonUtilService = commonUtilService;
            _cirsRiskManagementAttachmentService = cirsRiskManagementAttachmentService;
        }

        public void AddRowLevelSecurity(string itemId, string clientId)
        {
            var clientAdminAccessRole =
                _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, clientId);
            var clientReadAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, clientId);
            var clientManagerAccessRole =
                _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, clientId);

            var permission = new EntityReadWritePermission
            {
                Id = Guid.Parse(itemId)
            };

            permission.RolesAllowedToRead.Add(clientAdminAccessRole);
            permission.RolesAllowedToRead.Add(clientReadAccessRole);
            permission.RolesAllowedToRead.Add(clientManagerAccessRole);

            permission.RolesAllowedToUpdate.Add(clientManagerAccessRole);
            permission.RolesAllowedToUpdate.Add(clientAdminAccessRole);
            permission.RolesAllowedToUpdate.Add(clientReadAccessRole);

            _mongoSecurityService.UpdateEntityReadWritePermission<PraxisRisk>(permission);
        }

        public List<PraxisRisk> GetAllPraxisRisk()
        {
            throw new NotImplementedException();
        }

        public PraxisRisk GetPraxisRisk(string itemId)
        {
            return _repository.GetItem<PraxisRisk>(risk =>
                risk.ItemId.Equals(itemId) && !risk.IsMarkedToDelete);
        }

        public void RemoveRowLevelSecurity(string clientId)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteDataForClient(string clientId, string orgId = null)
        {
            _logger.LogInformation("Going to delete {PraxisRisk} and {PraxisAssessment} for client {ClientId}", nameof(PraxisRisk), nameof(PraxisAssessment), clientId);

            try
            {
                var deleteTasks = new List<Task>
                {
                    _repository.DeleteAsync<PraxisRisk>(risk => risk.ClientId.Equals(clientId)),
                    _repository.DeleteAsync<PraxisAssessment>(assessment => assessment.ClientId.Equals(clientId))
                };

                await Task.WhenAll(deleteTasks);
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while trying to delete {PraxisRisk} and {PraxisAssessment} for client {ClientId}. Error: {ErrorMessage}. Stacktrace: {StackTrace}", nameof(PraxisRisk), nameof(PraxisAssessment), clientId, e.Message, e.StackTrace);
            }
        }

        public void UpdateRecentAssessment(string riskId)
        {
            PraxisRisk risk = GetPraxisRisk(riskId);
            PraxisAssessment recentAssessment = _praxisAssessmentService.GetRecentPraxisAssessment(riskId);

            if (risk != null && recentAssessment != null)
            {
                risk.RecentAssessment = recentAssessment;
                _repository.Update<PraxisRisk>(r => r.ItemId.Equals(risk.ItemId), risk);
            }
        }

        public string GetCurrentRiskValue(PraxisAssessment assessment)
        {
            return $"{RiskAssessmentImpactKeys[assessment.Impact]}" +
                   $"/{RiskAssessmentProbabilityKeys[assessment.Probability]}";
        }

        public async Task<Dictionary<string, List<PraxisRiskChartData>>> GetPraxisRiskChartData(
            GetPraxisRiskChartDataQuery query
        )
        {
            try
            {
                var chartDataActual = new List<PraxisRiskChartData>();
                var chartDataTarget = new List<PraxisRiskChartData>();
                var filterString = query.FilterString[0..^1];

                if (!filterString.Contains("RecentAssessment: null"))
                {
                    filterString += (filterString.Length > 1 ? ", " : "") + "ClientId: {$in: [\"" +
                                    string.Join("\", \"", query.ClientIds) + "\"]}";
                    if (query.Topics != null && query.Topics?.Count != 0)
                    {
                        filterString += ", TopicValue: {$in: [\"" + string.Join("\", \"", query.Topics) + "\"]}";
                    }

                    filterString += filterString.Contains("RecentAssessment: {$ne: null}}")
                        ? ", RecentAssessment: {$ne: null}}"
                        : "}";
                    _logger.LogInformation("FilterString: {FilterString}", filterString);

                    var dictionary = (await _commonUtilService.GetEntityQueryResponse<PraxisRisk>(filterString)).Results
                        .GroupBy(risk => risk.TopicValue)
                        .AsEnumerable()
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var (key, praxisRisks) in dictionary)
                    {
                        var assessmentList = praxisRisks
                            .Where(risk =>
                                risk.RecentAssessment != null &&
                                !string.IsNullOrEmpty(risk.RecentAssessment.Impact) &&
                                !string.IsNullOrEmpty(risk.RecentAssessment.Probability)
                            )
                            .AsEnumerable()
                            .ToDictionary(risk => risk.ItemId, risk => risk.RecentAssessment);
                        // For secondary fault handling
                        var graphActual = new List<string>[5, 5];
                        var graphTarget = new List<string>[5, 5];
                        foreach (var (riskItemId, assessment) in assessmentList)
                        {
                            var riskName = praxisRisks.Find(risk => risk.ItemId.Equals(riskItemId)).Reference;
                            Console.WriteLine(Convert.ToString(assessment));
                            AddItemToNullableList(
                                ref graphActual[
                                    RiskAssessmentImpactKeys[assessment.Impact] - 1,
                                    RiskAssessmentProbabilityKeys[assessment.Probability] - 1
                                ], riskName
                            );
                            var riskValue = assessment.RiskAssessmentValue.Split("/")
                                .Select(s => Convert.ToInt16(s))
                                .ToArray();
                            AddItemToNullableList(
                                ref graphTarget[riskValue.ElementAt(0) - 1, riskValue.ElementAt(1) - 1], riskName
                            );
                        }

                        var datasetActual = new List<ChartDataSet>();
                        var datasetTarget = new List<ChartDataSet>();
                        for (int i = 0; i < 5; i++)
                        {
                            for (int j = 0; j < 5; j++)
                            {
                                if (graphActual[i, j] != null && graphActual[i, j].Count != 0)
                                {
                                    datasetActual.Add(new ChartDataSet
                                    { X = i + 1, Y = j + 1, RiskList = graphActual[i, j] });
                                }

                                if (graphTarget[i, j] != null && graphTarget[i, j].Count != 0)
                                {
                                    datasetTarget.Add(new ChartDataSet
                                    { X = i + 1, Y = j + 1, RiskList = graphTarget[i, j] });
                                }
                            }
                        }

                        chartDataActual.Add(new PraxisRiskChartData { Label = key, DataSets = datasetActual });
                        chartDataTarget.Add(new PraxisRiskChartData { Label = key, DataSets = datasetTarget });
                    }
                }

                return new Dictionary<string, List<PraxisRiskChartData>>
                {
                    { "Actual", chartDataActual },
                    { "Target", chartDataTarget }
                };
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while trying to generate chart data for clients {ClientIds}. Error: {Message}. Stacktrace: {StackTrace}", string.Join(", ", query.ClientIds), e.Message, e.StackTrace);
                throw;
            }
        }

        void AddItemToNullableList<T>(ref List<T> list, T item)
        {
            if (list == null)
            {
                list = new List<T>();
            }

            list.Add(item);
        }

        public async Task UpdateAttachmentInReporting(string riskId)
        {
            _logger.LogInformation("Updating attachment in reporting for risk: {RiskId}", riskId);
            try
            {
                var risk = GetPraxisRisk(riskId);
                const string reportingKey = "ReportingInfo";
                if (risk?.MetaDataList != null && risk.MetaDataList.Any(r => r.Key.Equals(reportingKey)))
                {
                    var reportingInfo = risk.MetaDataList
                        .FirstOrDefault(r => r.Key.Equals(reportingKey))?
                        .MetaData?
                        .Value ?? string.Empty;
                    var parsedReportingInfo = !string.IsNullOrEmpty(reportingInfo)
                        ? JsonConvert.DeserializeObject<List<ReportingInfo>>(reportingInfo)
                        : new List<ReportingInfo>();

                    await _cirsRiskManagementAttachmentService.AddRiskManagementAttachment(parsedReportingInfo, risk);

                }
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in Method: {MethodName}. Error Message: {Message} Error Details: {StackTrace}",
                    nameof(UpdateAttachmentInReporting), e.Message, e.StackTrace);
            }
        }
    }
}