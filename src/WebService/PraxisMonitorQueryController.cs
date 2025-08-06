using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Queries.OrganizationModule;

namespace Selise.Ecap.SC.PraxisMonitor.WebService
{
    public class PraxisMonitorQueryController : ControllerBase
    {
        private readonly ILogger<PraxisMonitorQueryController> _logger;
        private readonly QueryHandler queryHandler;
        private readonly ITenants _tenants;
        private readonly IConfiguration _configuration;

        public PraxisMonitorQueryController(
            QueryHandler queryHandler,
            ILogger<PraxisMonitorQueryController> logger,
            ITenants tenants,
            IConfiguration configuration)
        {
            this.queryHandler = queryHandler;
            _logger = logger;
            _tenants = tenants;
            _configuration = configuration;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public Task<QueryHandlerResponse> GetDistinctTaskList([FromBody] GetDistinctTaskListQuery query)
        {
            return queryHandler.SubmitAsync<GetDistinctTaskListQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public Task<QueryHandlerResponse> GetTrainingAnswers([FromBody] GetTrainingAnswersQuery query)
        {
            return queryHandler.SubmitAsync<GetTrainingAnswersQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public QueryHandlerResponse UpdateRiskMeasuresTakenPending(
            [FromBody] UpdateMeasuresTakenPendingCountQuery query)
        {
            var response = new QueryHandlerResponse();

            if (string.IsNullOrWhiteSpace(query.RiskId))
            {
                response.ErrorMessage = "RiskId must not be empty.";
                response.StatusCode = 400;
                return response;
            }

            if (query.OfflineMeasuresTaken < 1)
            {
                response.ErrorMessage = "OfflineMeasuresTaken must be greater than or equal to one(1).";
                response.StatusCode = 400;
                return response;
            }

            if (!string.IsNullOrEmpty(query.RiskId))
            {
                var isValidGuid = Guid.TryParse(query.RiskId, out _);

                if (!isValidGuid)
                {
                    response.ErrorMessage = "RiskId is not a valid Guid.";
                    response.StatusCode = 400;
                    return response;
                }
            }

            return queryHandler.Submit<UpdateMeasuresTakenPendingCountQuery, QueryHandlerResponse>(query);
        }

        [HttpGet]
        [Authorize]
        public CurrentStatusResponse GetCurrentStatus([FromQuery] GetCurrentStatusQuery query)
        {
            var response = new CurrentStatusResponse();
            if (string.IsNullOrWhiteSpace(query.ItemId))
            {
                response.Message = "ItemId must not be empty.";
                response.StatusCode = 400;
                return response;
            }

            if (string.IsNullOrWhiteSpace(query.EntityName))
            {
                response.Message = "Entity Name must not be empty.";
                response.StatusCode = 400;
                return response;
            }

            var entityList = new List<string>
            {
                nameof(PraxisClientCategory),
                nameof(PraxisForm),
                nameof(PraxisTraining),
                "PraxisClientSubCategory",
                nameof(PraxisClient),
                nameof(PraxisUserAdditionalInfo),
                nameof(PraxisRoom)
            };
            var isValid = entityList.Any(r => r.Contains(query.EntityName));
            if (!isValid)
            {
                response.Message = $"{query.EntityName} entity name is not valid for this end point.";
                response.StatusCode = 403;
                return response;
            }

            return queryHandler.Submit<GetCurrentStatusQuery, CurrentStatusResponse>(query);
        }

        [HttpGet]
        [ProtectedEndPoint]
        public Task<QueryHandlerResponse> GetUserActivity([FromQuery] GetUserActivityQuery query)
        {
            return queryHandler.SubmitAsync<GetUserActivityQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public Task<QueryHandlerResponse> GetCompletionDetails([FromBody] GetCompletionListQuery query)
        {
            return queryHandler.SubmitAsync<GetCompletionListQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public PersonaRoleResponse GetPersonaRoles([FromBody] GetPersonaRolesQuery query)
        {
            var response = new PersonaRoleResponse();
            if (query.UserInformations == null || query.UserInformations.Count < 1)
            {
                response.Messages = new List<string> { "UserInformations must not be empty or null." };
                response.StatusCode = 400;
                response.PersonaRoles = new List<string>();
                return response;
            }

            var message = new List<string>();
            foreach (var userInformation in query.UserInformations)
            {
                if (string.IsNullOrEmpty(userInformation.ClientId)) message.Add("ClientId must not be empty or null.");

                if (!string.IsNullOrEmpty(userInformation.ClientId))
                {
                    var isValidGuid = Guid.TryParse(userInformation.ClientId, out _);

                    if (!isValidGuid) message.Add("ClientId is not a valid Guid.");
                }

                if (userInformation.UserRoles == null || userInformation.UserRoles?.Length <= 0)
                    message.Add("UserRoles must not be empty or null.");
            }

            if (message.Any())
            {
                response.Messages = message;
                response.StatusCode = 400;
                response.PersonaRoles = new List<string>();
                return response;
            }

            return queryHandler.Submit<GetPersonaRolesQuery, PersonaRoleResponse>(query);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public ClientInformationResponse GetClientInformation([FromBody] GetClientInformationQuery query)
        {
            var response = new ClientInformationResponse();
            if (query.PersonaNames == null || !query.PersonaNames.Any())
            {
                response.StatusCode = 400;
                response.Message = "PersonaNames must not be empty or null.";
                response.Results = new List<ClientInformation>();
                return response;
            }

            return queryHandler.Submit<GetClientInformationQuery, ClientInformationResponse>(query);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public Task<UserLogOutResponse> SendNotificationForLogOutUser([FromBody] UserLogOutQuery query)
        {
            var response = new UserLogOutResponse();
            if (string.IsNullOrWhiteSpace(query.UserId))
            {
                response.StatusCode = 400;
                response.Message = "UserId must not be empty or null.";
                return Task.FromResult(response);
            }

            var isValidGuid = Guid.TryParse(query.UserId, out _);

            if (!isValidGuid)
            {
                response.StatusCode = 400;
                response.Message = "UserId is not a valid Guid.";
                return Task.FromResult(response);
            }

            if (string.IsNullOrWhiteSpace(query.ActionName))
            {
                response.StatusCode = 400;
                response.Message = "ActionName must not be empty or null.";
                return Task.FromResult(response);
            }

            if (string.IsNullOrWhiteSpace(query.Context))
            {
                response.StatusCode = 400;
                response.Message = "Context must not be empty or null.";
                return Task.FromResult(response);
            }

            return queryHandler.SubmitAsync<UserLogOutQuery, UserLogOutResponse>(query);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public OpenOrganizationResponse GetDeleteFeatureRole([FromBody] GetOpenOrganizationQuery query)
        {
            var response = new OpenOrganizationResponse();
            if (string.IsNullOrEmpty(query.ClientId))
            {
                response.StatusCode = 400;
                response.Message = "ClientId must not be empty or null.";
                response.Role = string.Empty;
                return response;
            }

            if (!string.IsNullOrEmpty(query.ClientId))
            {
                var isValidGuid = Guid.TryParse(query.ClientId, out _);
                if (!isValidGuid)
                {
                    response.StatusCode = 400;
                    response.Message = "ClientId is not a valid Guid.";
                    response.Role = string.Empty;
                    return response;
                }
            }

            return queryHandler.Submit<GetOpenOrganizationQuery, OpenOrganizationResponse>(query);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public QueryHandlerResponse GetCalculatedSubscriptionUpdatePrice([FromBody] GetCalculatedSubscriptionUpdatePriceQuery query)
        {
            if (query == null || query.NumberOfUser.Equals(null) || query.NumberOfUser < 0)
                return new QueryHandlerResponse
                {
                    StatusCode = 400,
                    ErrorMessage = "User number is not valid",
                    Results = null
                };
            if (string.IsNullOrEmpty(query.SubscriptionTypeSeedId))
                return new QueryHandlerResponse
                {
                    StatusCode = 400,
                    ErrorMessage = "SubscriptionTypeSeedId is not valid",
                    Results = null
                };
            var isValid = Guid.TryParse(query.SubscriptionTypeSeedId, out _);
            if (!isValid)
                return new QueryHandlerResponse
                {
                    StatusCode = 400,
                    ErrorMessage = "SubscriptionTypeSeedId is not valid",
                    Results = null
                };
            return queryHandler.Submit<GetCalculatedSubscriptionUpdatePriceQuery, QueryHandlerResponse>(query);
        }

        [HttpGet]
        [AnonymousEndPoint]
        public void ConsoleInBusinessPoint()
        {
            _logger.LogInformation("01/07/2021");
        }

        [HttpGet]
        public Task<bool> ValidatePayment([FromQuery] ValidatePaymentQuery query)
        {
            _logger.LogInformation("New {QueryName} arrived with payload: {Payload}.", nameof(ValidatePaymentQuery), JsonConvert.SerializeObject(query));
            return queryHandler.SubmitAsync<ValidatePaymentQuery, bool>(query);
        }

        [HttpGet]
        public Task<bool> ValidateUpdatePayment([FromQuery] ValidateUpdatePaymentQuery query)
        {
            _logger.LogInformation("New {QueryName} arrived with payload: {Payload}.", nameof(ValidateUpdatePaymentQuery), JsonConvert.SerializeObject(query));
            return queryHandler.SubmitAsync<ValidateUpdatePaymentQuery, bool>(query);
        }

        [HttpPost]
        public QueryHandlerResponse GetPaymentDetails([FromBody] GetPaymentDetailsQuery query)
        {
            _logger.LogInformation("New {QueryName} arrived with payload: {Payload}.", nameof(GetPaymentDetailsQuery), JsonConvert.SerializeObject(query));
            if (query == null)
                return new QueryHandlerResponse
                {
                    StatusCode = 1,
                    Data = null,
                    ErrorMessage = "Query not valid"
                };
            return queryHandler.Submit<GetPaymentDetailsQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public Task<QueryHandlerResponse> GetProcessGuides([FromBody] GetProcessGuideQuery query)
        {
            _logger.LogInformation("New {QueryName} arrived with payload: {Payload}.",
                nameof(GetProcessGuideQuery), JsonConvert.SerializeObject(query));
            if (query != null) return queryHandler.SubmitAsync<GetProcessGuideQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetProcessGuideIds([FromBody] GetProcessGuideIdsQuery query)
        {
            _logger.LogInformation(
                "New {QueryName} arrived with payload: {Payload}.", nameof(GetProcessGuideQuery), JsonConvert.SerializeObject(query));
            if (query != null) return queryHandler.SubmitAsync<GetProcessGuideIdsQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetProcessGuidesForReport([FromBody] GetReportQuery query)
        {
            _logger.LogInformation("New {QueryName} arrived with payload: {Payload}.", nameof(GetReportQuery), JsonConvert.SerializeObject(query));
            if (query != null) return queryHandler.SubmitAsync<GetReportQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public Task<QueryHandlerResponse> GetProcessGuideDetails([FromBody] GetProcessGuideDetailsQuery query)
        {
            _logger.LogInformation("New {QueryName} arrived with payload: {Payload}.", nameof(GetProcessGuideQuery), JsonConvert.SerializeObject(query));
            if (query != null)
                return queryHandler.SubmitAsync<GetProcessGuideDetailsQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public Task<QueryHandlerResponse> GetOwnClientList([FromBody] GetOwnClientListQuery query)
        {
            _logger.LogInformation("Received request for fetching own client List");
            return queryHandler.SubmitAsync<GetOwnClientListQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public Task<QueryHandlerResponse> GetPresignedUrls([FromBody] GetPresignedUrlQuery query)
        {
            if (query == null)
            {
                var response = new QueryHandlerResponse()
                {
                    ErrorMessage = "Command Invalid value"
                };
                return Task.FromResult(response);
            }
            return queryHandler.SubmitAsync<GetPresignedUrlQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public Task<QueryHandlerResponse> IsClientExist([FromBody] IsClientExistQuery query)
        {
            if (query == null || string.IsNullOrEmpty(query.ClientName))
            {
                var response = new QueryHandlerResponse()
                {
                    ErrorMessage = "Command Invalid value"
                };
                return Task.FromResult(response);
            }
            return queryHandler.SubmitAsync<IsClientExistQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public Task<QueryHandlerResponse> IsOrganizationExist([FromBody] IsOrganizationExistQuery query)
        {
            if (query == null || string.IsNullOrEmpty(query.OrganizationName))
            {
                var response = new QueryHandlerResponse()
                {
                    ErrorMessage = "Command Invalid value"
                };
                return Task.FromResult(response);
            }
            return queryHandler.SubmitAsync<IsOrganizationExistQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetPraxisRiskChartData([FromBody] GetPraxisRiskChartDataQuery query)
        {
            if (query?.ClientIds == null || query.ClientIds.Count == 0)
            {
                var response = new QueryHandlerResponse
                {
                    ErrorMessage = "Command Invalid value, At lease one ClientId is required"
                };
                return Task.FromResult(response);
            }
            return queryHandler.SubmitAsync<GetPraxisRiskChartDataQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetUilmResourceKeys([FromBody] GetUilmResourceKeysQuery query)
        {
            if (query == null || query.KeyNameList.Count == 0)
            {
                var response = new QueryHandlerResponse
                {
                    ErrorMessage = "Command Invalid value, At lease one UilmResourceKeyName is required",
                    StatusCode = 1
                };
                return Task.FromResult(response);
            }
            return queryHandler.SubmitAsync<GetUilmResourceKeysQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public object GetConfig()
        {
            var tenant = _tenants.GetTenantByID("82D07BF9-CC75-477D-A286-F1A19A9FA0EA");
            var globalConfig = GlobalConfig.CreateGlobalConfigFromJson(
                  _configuration["ServiceName"],
                _configuration["GlobalConfigJsonPath"]);

            return new { Tenant = tenant, globalConfig };
        }

        [HttpPost]
        [ProtectedEndPoint]
        public Task<QueryHandlerResponse> GetEntityData([FromBody] GetEntityQuery query)
        {
            return queryHandler.SubmitAsync<GetEntityQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public QueryHandlerResponse GetOrganizationBasicInfo([FromBody] GetOrganizationBasicInfoQuery query)
        {
            return queryHandler.Submit<GetOrganizationBasicInfoQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        [Obsolete]
        public Task<QueryHandlerResponse> GetIncidents([FromBody] GetIncidentQuery query)
        {
            return queryHandler.SubmitAsync<GetIncidentQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<QueryHandlerResponse> GetCalculatedSubscriptionPrice([FromBody] GetCalculatedSubscriptionPriceQuery query)
        {
            if (query == null || query.NumberOfUser.Equals(null) || query.NumberOfUser < 1)
            {
                return new QueryHandlerResponse()
                {
                    StatusCode = 400,
                    ErrorMessage = "User number is not valid",
                    Results = null
                };
            }
            if (string.IsNullOrEmpty(query.SubscriptionTypeSeedId))
            {
                return new QueryHandlerResponse
                {
                    StatusCode = 400,
                    ErrorMessage = "SubscriptionTypeSeedId is not valid",
                    Results = null
                };
            }
            if (string.IsNullOrEmpty(query.SubscriptionTypeSeedId))
            {
                return new QueryHandlerResponse
                {
                    StatusCode = 400,
                    ErrorMessage = "SubscriptionTypeSeedId is not valid",
                    Results = null
                };
            }
            return await queryHandler.SubmitAsync<GetCalculatedSubscriptionPriceQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetSubscriptionRenewalEstimatedBill([FromBody] GetSubscriptionRenewalEstimatedBillQuery query)
        {
            return queryHandler.SubmitAsync<GetSubscriptionRenewalEstimatedBillQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetSubscriptionUpdateEstimatedBill([FromBody] GetSubscriptionUpdateEstimatedBillQuery query)
        {
            return queryHandler.SubmitAsync<GetSubscriptionUpdateEstimatedBillQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetDocumentMappingDraftHtmlFileId([FromBody] GetDocumentMappingDraftHtmlFileIdQuery query)
        {
            return queryHandler.SubmitAsync<GetDocumentMappingDraftHtmlFileIdQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public QueryHandlerResponse ValidateShiftInfo([FromBody] ValidateShiftInfo query)
        {
            return queryHandler.Submit<ValidateShiftInfo, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public QueryHandlerResponse ValidateShiftPlanInfo([FromBody] ValidateShiftPlanInfoQuery query)
        {
            return queryHandler.Submit<ValidateShiftPlanInfoQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public QueryHandlerResponse GetShifts([FromBody] GetShiftQuery query)
        {
            return queryHandler.Submit<GetShiftQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        public QueryHandlerResponse GetShiftsDropdown([FromBody] GetShiftsDropdownQuery query)
        {
            return queryHandler.Submit<GetShiftsDropdownQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public QueryHandlerResponse GetShiftPlans([FromBody] GetShiftPlanQuery query)
        {
            return queryHandler.Submit<GetShiftPlanQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetLibraryGroups([FromBody] GetLibraryGroupsQuery query)
        {
            if (query == null || string.IsNullOrEmpty(query.OrganizationId))
            {
                var response = new QueryHandlerResponse()
                {
                    ErrorMessage = "Query is not valid"
                };
                return response;
            }
            return await queryHandler.SubmitAsync<GetLibraryGroupsQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public QueryHandlerResponse GetShiftPlanById([FromBody] GetShiftPlanByIdQuery query)
        {
            return queryHandler.Submit<GetShiftPlanByIdQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public QueryHandlerResponse GetDocumentEditHistory([FromBody] GetDocumentEditHistoryQuery query)
        {
            return queryHandler.Submit<GetDocumentEditHistoryQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetDocumentKeywords([FromBody] GetDocumentKeywordsQuery query)
        {
            return await queryHandler.SubmitAsync<GetDocumentKeywordsQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetLoggedInUserDraftedLibraryForm(
            [FromBody] LibraryFormCloneGetQuery query)
        {
            return queryHandler.SubmitAsync<LibraryFormCloneGetQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetSignatureUrl(
            [FromBody] SignatureUrlGetQuery query)
        {
            return queryHandler.SubmitAsync<SignatureUrlGetQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public Task<QueryHandlerResponse> VerifyTwoFactorAuthenticationCode(
            [FromBody] TwoFactorCodeVerifyQuery query)
        {
            return queryHandler.SubmitAsync<TwoFactorCodeVerifyQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetLibraryRights([FromBody] LibraryRightsGetQuery query)
        {
            if (query != null)
                return await queryHandler.SubmitAsync<LibraryRightsGetQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse()
            {
                ErrorMessage = "Query is not valid"
            };
            return response;
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetPraxisOrganizationsUsers([FromBody] GetPraxisOrganizationUserQuery query)
        {
            return queryHandler.SubmitAsync<GetPraxisOrganizationUserQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetObjectArtifactFormHistory([FromBody] ObjectArtifactFormHistoryQuery query)
        {
            if (query == null || string.IsNullOrEmpty(query.ObjectArtifactId))
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                };
            }

            return await queryHandler.SubmitAsync<ObjectArtifactFormHistoryQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetLibraryDocumentAssignees([FromBody] LibraryDocumentAssigneeQuery query)
        {
            if (query == null || string.IsNullOrEmpty(query.ObjectArtifactId))
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                };
            }

            return await queryHandler.SubmitAsync<LibraryDocumentAssigneeQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetObjectArtifacts([FromBody] ObjectArtifactQuery query)
        {
            return await queryHandler.SubmitAsync<ObjectArtifactQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetObjectArtifactFiles([FromBody] ObjectArtifactFileQuery query)
        {
            return await queryHandler.SubmitAsync<ObjectArtifactFileQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetCockpitDocumentActivityMetrics([FromBody] CockpitDocumentActivityMetricsQuery query)
        {
            if (query is { IsUserLevel: false } && (query.OrganizationIds == null || query.OrganizationIds.Length == 0))
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "OrganizationId is required",
                    Results = null
                };
            }
            return await queryHandler.SubmitAsync<CockpitDocumentActivityMetricsQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetDepartmentWiseCategories([FromBody] GetDepartmentWiseCategoriesQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                };
            }
            return await queryHandler.SubmitAsync<GetDepartmentWiseCategoriesQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetDepartmentWiseSuppliers([FromBody] GetDepartmentWiseSuppliersQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                };
            }
            return await queryHandler.SubmitAsync<GetDepartmentWiseSuppliersQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetDepartmentWiseUserAdditionalInfos([FromBody] GetDepartmentWiseUserAdditionalInfosQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                };
            }
            return await queryHandler.SubmitAsync<GetDepartmentWiseUserAdditionalInfosQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<QueryHandlerResponse> GetEquipmentMaintenanceForExternalUser([FromBody] GetEquipmentMaintenanceForExternalUserQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                };
            }
            return await queryHandler.SubmitAsync<GetEquipmentMaintenanceForExternalUserQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<QueryHandlerResponse> GetEquipmentForExternalUser([FromBody] GetEquipmentForExternalUserQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                };
            }
            return await queryHandler.SubmitAsync<GetEquipmentForExternalUserQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetEquipmentRights([FromBody] GetEquipmentRightsQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse()
                {
                    ErrorMessage = "Invalid Query"
                };
            }
            if (!string.IsNullOrEmpty(query.EquipmentId))
            {
                if (string.IsNullOrEmpty(query.DepartmentId))
                {
                    return new QueryHandlerResponse()
                    {
                        ErrorMessage = "Invalid Query: Department ID cannot be null or empty when Equipment ID is provided"
                    };
                }
            }


            return await queryHandler.SubmitAsync<GetEquipmentRightsQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetCockpitSummary([FromBody] GetCockpitSummaryQuery query)
        {
            if (query is null || query.PageNumber <= 0 || query.PageSize <= 0)
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                };
            }
            if (query.IsUserLevel == false && query.OrganizationIds?.Length == 0)
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "OrganizationId is required",
                    Results = null
                };
            }

            return await queryHandler.SubmitAsync<GetCockpitSummaryQuery, QueryHandlerResponse>(query);
        }

        [HttpGet]
        [Authorize]
        public Task<QueryHandlerResponse> GetSupplierStatus([FromQuery] GetSupplierStatusQuery query)
        {

            var response = new QueryHandlerResponse();
            if (string.IsNullOrWhiteSpace(query.ItemId))
            {
                response.ErrorMessage = "ItemId must not be empty.";
                response.StatusCode = 400;
                return Task.FromResult(response);
            }

            if (string.IsNullOrWhiteSpace(query.EntityName))
            {
                response.ErrorMessage = "Entity Name must not be empty.";
                response.StatusCode = 400;
                return Task.FromResult(response);
            }

            var entityList = new List<string>
            {
                nameof(PraxisClient)
            };
            var isValid = entityList.Any(r => r.Contains(query.EntityName));
            if (!isValid)
            {
                response.ErrorMessage = $"{query.EntityName} entity name is not valid for this end point.";
                response.StatusCode = 403;
                return Task.FromResult(response);
            }

            return queryHandler.SubmitAsync<GetSupplierStatusQuery, QueryHandlerResponse>(query);
        }


        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetSupplierGroupName([FromBody] GetSupplierGroupNameQuery query)
        {
            if (query is null || query?.PraxisClientId is null || query?.SupplierGroupName is null)
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                };
            }
            return await queryHandler.SubmitAsync<GetSupplierGroupNameQuery, QueryHandlerResponse>(query);
        }
        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetDmsArtifactUsageReference([FromBody] GetDmsArtifactUsageReferenceQuery query)
        {
            if (string.IsNullOrWhiteSpace(query?.ObjectArtifactId))
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                };
            }
            return await queryHandler.SubmitAsync<GetDmsArtifactUsageReferenceQuery, QueryHandlerResponse>(query);
        }


        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetArtifactAssignedVersion([FromBody] GetArtifactAssignedVersionQuery query)
        {
            return await queryHandler.SubmitAsync<GetArtifactAssignedVersionQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetDepartmentSubscription([FromBody] GetDepartmentSubscriptionQuery query)
        {
            if (query == null || string.IsNullOrEmpty(query.PraxisClientId))
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "DepartmentId is required",
                    Results = null
                };
            }

            return await queryHandler.SubmitAsync<GetDepartmentSubscriptionQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetOrganizationSubscription([FromBody] GetOrganizationSubscriptionQuery query)
        {
            if (query == null || string.IsNullOrEmpty(query.OrganizationId))
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "OrganizationId is required",
                    Results = null
                };
            }

            return await queryHandler.SubmitAsync<GetOrganizationSubscriptionQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetSubscriptionsInfo([FromBody] GetSubscriptionsInfoQuery query)
        {
            if (query == null || (string.IsNullOrEmpty(query.PraxisClientId) && string.IsNullOrEmpty(query.OrganizationId)))
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                };
            }

            return await queryHandler.SubmitAsync<GetSubscriptionsInfoQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetPraxisEquipments([FromBody] GetEquipementQuery query)
        {
            if (query != null) return queryHandler.SubmitAsync<GetEquipementQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetPraxisEquipmentMaintenances([FromBody] GetPraxisEquipmentMaintenancesQuery query)
        {
            if (query != null) return queryHandler.SubmitAsync<GetPraxisEquipmentMaintenancesQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetPraxisUsersForEquipement([FromBody] GetEquipementUserQuery query)
        {
            if (query != null) return queryHandler.SubmitAsync<GetEquipementUserQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetPraxisClientsForEquipement([FromBody] GetEquipementClientQuery query)
        {
            if (query != null) return queryHandler.SubmitAsync<GetEquipementClientQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetPraxisClientsCategoryForEquipement([FromBody] GetEquipementClientCategoryQuery query)
        {
            if (query != null) return queryHandler.SubmitAsync<GetEquipementClientCategoryQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetPraxisRoomForEquipement([FromBody] GetEquipementRoomQuery query)
        {
            if (query != null) return queryHandler.SubmitAsync<GetEquipementRoomQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetValidFileUploadRequest([FromBody] GetValidFileUploadRequestQuery query)
        {
            if (query != null) return queryHandler.SubmitAsync<GetValidFileUploadRequestQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }
        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetPraxisOpenItems([FromBody] GetPraxisOpenItemsQuery query)
        {
            if (string.IsNullOrWhiteSpace(query?.Filter))
            {
                return Task.FromResult(new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                });
            }
            return queryHandler.SubmitAsync<GetPraxisOpenItemsQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetSubscriptionPriceConfig([FromBody] GetSubscriptionPriceConfigQuery query)
        {
            return queryHandler.SubmitAsync<GetSubscriptionPriceConfigQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public Task<QueryHandlerResponse> GetPricingSeedData([FromBody] GetPricingSeedDataQuery query)
        {
            return queryHandler.SubmitAsync<GetPricingSeedDataQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetSubscriptionPricingCustomPackages([FromBody] GetSubscriptionPricingCustomPackagesQuery query)
        {
            return queryHandler.SubmitAsync<GetSubscriptionPricingCustomPackagesQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public Task<QueryHandlerResponse> GetSubscriptionPricingCustomPackage([FromBody] GetSubscriptionPricingCustomPackageQuery query)
        {
            if (string.IsNullOrEmpty(query?.ItemId))
            {
                return Task.FromResult(new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                });
            }
            return queryHandler.SubmitAsync<GetSubscriptionPricingCustomPackageQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public Task<QueryHandlerResponse> GetCalculatedSubscriptionInstallmentPayment([FromBody] GetCalculatedSubscriptionInstallmentPaymentQuery query)
        {
            return queryHandler.SubmitAsync<GetCalculatedSubscriptionInstallmentPaymentQuery, QueryHandlerResponse>(query);
        }

        [HttpGet]
        [AnonymousEndPoint]
        public IEnumerable<AppResponse> GetApps(GetAppsQuery query)
        {
            return queryHandler.Submit<GetAppsQuery, IEnumerable<AppResponse>>(query);
        }

        [HttpPost]
        [Authorize]

        public Task<QueryHandlerResponse> GetPraxisForm([FromBody] GetPraxisFormQuery query)
        {
            return queryHandler.SubmitAsync<GetPraxisFormQuery, QueryHandlerResponse>(query);
        }

        public Task<QueryHandlerResponse> GetAIConversation([FromBody] GetAIConversationQuery query)
        {
            if (query == null || string.IsNullOrEmpty(query.question))
            {
                return Task.FromResult(new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                });
            }
            return queryHandler.SubmitAsync<GetAIConversationQuery, QueryHandlerResponse>(query);
        }

        public Task<QueryHandlerResponse> GetPraxisFormById([FromBody] GetPraxisFormQuery query)

        {
            if (query == null || string.IsNullOrEmpty(query.ItemId))
            {
                return Task.FromResult(new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                });
            }
            return queryHandler.SubmitAsync<GetPraxisFormQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetItemsUsageInEntities([FromBody] GetItemsUsageInEntitiesQuery query)
        {
            if (query == null || string.IsNullOrEmpty(query.EntityName) || !(query.QueryItems?.Count > 0))
            {
                return new QueryHandlerResponse
                {
                    ErrorMessage = "Query is not valid",
                    Results = null
                };
            }
            return await queryHandler.SubmitAsync<GetItemsUsageInEntitiesQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetRiqsPediaViewControl([FromBody] GetRiqsPediaViewControlQuery query)
        {
            return queryHandler.SubmitAsync<GetRiqsPediaViewControlQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetPraxisUserForRiqsPedia([FromBody] GetPraxisUserForRiqsPediaQuery query)
        {
            if (query != null) return queryHandler.SubmitAsync<GetPraxisUserForRiqsPediaQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetPraxisClientsForRiqsPedia([FromBody] GetPraxisClientsForRiqsPediaQuery query)
        {
            if (query != null) return queryHandler.SubmitAsync<GetPraxisClientsForRiqsPediaQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }


        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetPraxisClientsForReporting([FromBody] GetPraxisClientsForReportingQuery query)
        {
            if (query != null) return queryHandler.SubmitAsync<GetPraxisClientsForReportingQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetPraxisFormForDepartment([FromBody] GetPraxisFormForDepartmentQuery query)
        {
            if (query != null) return queryHandler.SubmitAsync<GetPraxisFormForDepartmentQuery, QueryHandlerResponse>(query);
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetMalfunctionGroup([FromBody] GetMalfunctionGroupQuery query)
        {
            if (query != null && !string.IsNullOrEmpty(query.ClientId) && !string.IsNullOrEmpty(query.OrganizationId))
            {
                return queryHandler.SubmitAsync<GetMalfunctionGroupQuery, QueryHandlerResponse>(query);
            }
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetOrganizationLatestSubscriptionData([FromBody] GetOrganizationLatestSubscriptionDataQuery query)
        {
            if (query != null && !string.IsNullOrEmpty(query.OrganizationId))
            {
                return queryHandler.SubmitAsync<GetOrganizationLatestSubscriptionDataQuery, QueryHandlerResponse>(query);
            }
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public Task<QueryHandlerResponse> CheckUserActivated([FromBody] CheckUserActivatedQuery query)
        {
            if (query != null)
            {
                return queryHandler.SubmitAsync<CheckUserActivatedQuery, QueryHandlerResponse>(query);
            }
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetNewCirsReports([FromBody] GetNewCirsReportsQuery query)
        {
            if (query != null)
            {
                return queryHandler.SubmitAsync<GetNewCirsReportsQuery, QueryHandlerResponse>(query);
            }
            var response = new QueryHandlerResponse
            {
                StatusCode = 1,
                Data = null,
                ErrorMessage = "Query not valid"
            };
            return Task.FromResult(response);
        }
    }
}
