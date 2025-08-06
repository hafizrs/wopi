using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.DmsMigration;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.ValidationHandlers;
using Selise.Ecap.SC.PraxisMonitor.Commands.ClientModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.Subscriptions.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ClientModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;

namespace Selise.Ecap.SC.PraxisMonitor.WebService
{
    public class PraxisMonitorCommandController : ControllerBase
    {
        private readonly CommandHandler _commandService;
        private readonly ValidationHandler _validationHandler;
        private readonly IServiceClient _serviceClient;
        private readonly ILogger<PraxisMonitorCommandController> _logger;

        public PraxisMonitorCommandController(
            CommandHandler commandService,
            ValidationHandler validationHandler,
            IServiceClient serviceClient,
            ILogger<PraxisMonitorCommandController> logger)
        {
            _commandService = commandService;
            _validationHandler = validationHandler;
            _serviceClient = serviceClient;
            _logger = logger;
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<CommandResponse> AddStatusToPraxisUser([FromBody] DataProcessCommand command)
        {
            if (command != null && command.EntityName == EntityName.PraxisUser)
            {
                return await _commandService.SubmitAsync<DataProcessCommand, CommandResponse>(command);
            }

            var response = new CommandResponse();
            return response;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> UpdatePraxisTaskStatus([FromBody] UpdatePraxisTaskStatusCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result = await _validationHandler.SubmitAsync<UpdatePraxisTaskStatusCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }


        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteData([FromBody] DeleteDataCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            _logger.LogInformation("New {CommandName} arrived with Entity Name: {EntityName} and Item Id: {ItemId}.",
                nameof(DeleteDataCommand), command.EntityName, command.ItemId);
            var result = await _validationHandler.SubmitAsync<DeleteDataCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> PrepareDynamicNavigation([FromBody] PrepareDynamicNavigationCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            _logger.LogInformation("New {CommandName} arrived with OrganizationId: {OrganizationId}, Type: {Type}.",
                nameof(PrepareDynamicNavigationCommand), command.OrganizationId, command.Type);
            var result = await _validationHandler.SubmitAsync<PrepareDynamicNavigationCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                _logger.LogInformation("Validation success. Going to post queue with {CommandName} with OrganizationId: {OrganizationId}, Type: {Type}.",
                    nameof(PrepareDynamicNavigationCommand), command.OrganizationId, command.Type);
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> UpdateDeletePermission([FromBody] UpdateOpenOrganizationCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            _logger.LogInformation("New {CommandName} arrived with ClientId: {ClientId}.",
                nameof(UpdateOpenOrganizationCommand), command.ClientId);
            var result = await _validationHandler.SubmitAsync<UpdateOpenOrganizationCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> ProcessUserCreateUpdate([FromBody] ProcessUserCreateUpdateCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("command", "Invalid Value");
                return response;
            }

            var result = await _validationHandler.SubmitAsync<ProcessUserCreateUpdateCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> UpdateProcessGuideCompletionStatus(
            [FromBody] UpdateProcessGuideCompletionStatusCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("command", "Invalid Value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<UpdateProcessGuideCompletionStatusCommand, CommandResponse>(
                    command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation Result: {ValidationResult}.", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> UpdatePraxisUserDtos([FromBody] UpdatePraxisUserDtosCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("command", "Invalid Value");
                return response;
            }

            var result = await _validationHandler.SubmitAsync<UpdatePraxisUserDtosCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> ProcessGuideDeveloperOverviewReport(
            [FromBody] ExportProcessGuideReportForDeveloperCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler
                    .SubmitAsync<ExportProcessGuideReportForDeveloperCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            return result;
        }


        [HttpPost]
        [AnonymousEndPoint]
        public async Task<CommandResponse> UpdateOrgTypeChangePermission(
            [FromBody] UpdateOrgTypeChangePermissionCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<UpdateOrgTypeChangePermissionCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            return result;
        }

        [HttpPost]
        [AnonymousEndPoint]
        public Task<CommandResponse> UploadFile([FromForm] UploadFileCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return Task.FromResult(response);
            }

            using (var ms = new MemoryStream())
            {
                Request.Form.Files[0].CopyTo(ms);
                var fileBytes = ms.ToArray();
                command.FileData = fileBytes;
            }

            return Task.FromResult(new CommandResponse());
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> SetLicensingSpecification(
            [FromBody] SetLicensingSpecificationCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<SetLicensingSpecificationCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            return result;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> PraxisClientCustomSubscription(
            [FromBody] PraxisClientCustomSubscriptionCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<PraxisClientCustomSubscriptionCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            return result;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> UpdateCustomSubscription([FromBody] UpdateCustomSubscriptionCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<UpdateCustomSubscriptionCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            return result;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> RemoveCustomSubscription([FromBody] RemoveCustomSubscriptionCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<RemoveCustomSubscriptionCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> SetReadPermissionForEntity(
            [FromBody] SetReadPermissionForEntityCommand command
        )
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<SetReadPermissionForEntityCommand, CommandResponse>(command);

            return await ReturnAfterValidation(result, command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> GeneratePdfUsingTemplateEngine(
            [FromBody] GeneratePdfUsingTemplateEngineCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<GeneratePdfUsingTemplateEngineCommand, CommandResponse>(command);

            return await ReturnAfterValidation(result, command);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<CommandResponse> GenerateDocumentFileFromHtml(
            [FromBody] GenerateDocumentUsingTemplateEngineCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler
                    .SubmitAsync<GenerateDocumentUsingTemplateEngineCommand, CommandResponse>(command);

            return await ReturnAfterValidation(result, command);
        }

        #region ReportCommands

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> EquipmentListReport([FromBody] ExportEquipmentListReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> ProcessGuideDetailReport(
            [FromBody] ExportProcessGuideDetailReportCommand command
        )
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> ProcessGuideOverviewReport(
            [FromBody] ExportProcessGuideCaseOverviewReportCommand command
        )
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> ExportCategoryReport([FromBody] ExportCategoryReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> ExportTrainingReport([FromBody] ExportTrainingReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> ExportTrainingDetailsReport(
            [FromBody] ExportTrainingDetailsReportCommand command
        )
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> ExportDeveloperReport([FromBody] ExportDeveloperReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> ExportPraxisUserListReport(
            [FromBody] ExportPraxisUserListReportCommand command
        )
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> ExportRiskOverviewReport([FromBody] ExportRiskOverviewReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }


        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> CreateTaskListReport([FromBody] ExportTaskListReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> CreateDistinctTaskListReport(
            [FromBody] ExportDistinctTaskListReportCommand command
        )
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> CreateOpenItemReport([FromBody] ExportOpenItemReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> EquipmentMaintenanceListReport(
            [FromBody] ExportEquipmentMaintenanceListReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        private async Task<CommandResponse> SendToReportQueueAfterValidation<T>(T command)
        {
            _logger.LogInformation("New {TypeName} arrived.", typeof(T).Name);
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result = await _validationHandler.SubmitAsync<T, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetReportQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        #endregion

        private Task<CommandResponse> ReturnAfterValidation(CommandResponse response, object command)
        {
            if (response.StatusCode.Equals(0))
            {
                return Task.FromResult(
                    _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command));
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(response));
            return Task.FromResult(response);
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> ResolveProdDataIssues([FromBody] ResolveProdDataIssuesCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<ResolveProdDataIssuesCommand, CommandResponse>(command);

            return await ReturnAfterValidation(result, command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> ExportCirsReport([FromBody] ExportReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> GenerateShiftPlanReport([FromBody] GenerateShiftPlanReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> GenerateShiftReport([FromBody] GenerateShiftReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> GenerateLibraryReport([FromBody] GenerateLibraryReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> ProcessOrganizationCreateUpdate(
            [FromBody] ProcessOrganizationCreateUpdateCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<ProcessOrganizationCreateUpdateCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<ProcessOrganizationCreateUpdateCommand, CommandResponse>(
                    command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> ProcessOrganizationExternalOffice(
            [FromBody] ProcessOrganizationExternalOfficeCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return await _commandService.SubmitAsync<ProcessOrganizationExternalOfficeCommand, CommandResponse>(command);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<CommandResponse> ClientPaymentSubmission([FromBody] ClientPaymentSubmissionCommand command)
        {
            _logger.LogInformation("New {CommandName} arrived.", nameof(ClientPaymentSubmissionCommand));
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result = await _validationHandler.SubmitAsync<ClientPaymentSubmissionCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<CommandResponse> ProcessClientUserData([FromBody] PrepareClientUserForPaymentCommand command)
        {
            _logger.LogInformation("New {CommandName} arrived.", nameof(PrepareClientUserForPaymentCommand));
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<PrepareClientUserForPaymentCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> MakeSubscriptionRenewalPayment([FromBody] SubscriptionRenewalCommand command)
        {
            _logger.LogInformation("New {CommandName} arrived.", nameof(SubscriptionRenewalCommand));
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result = await _validationHandler.SubmitAsync<SubscriptionRenewalCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> MakeSubscriptionRenewalPaymentForClient([FromBody] SubscriptionRenewalForClientCommand command)
        {
            _logger.LogInformation("New {CommandName} arrived.", nameof(SubscriptionRenewalForClientCommand)); 
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result = await _validationHandler.SubmitAsync<SubscriptionRenewalForClientCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<CommandResponse> MakeSubscriptionUpdatePayment([FromBody] SubscriptionUpdateCommand command)
        {
            _logger.LogInformation("New {CommandName} arrived.", nameof(SubscriptionUpdateCommand));
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result = await _validationHandler.SubmitAsync<SubscriptionUpdateCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<SubscriptionUpdateCommand, CommandResponse>(command); ;
                //return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<CommandResponse> MakeSubscriptionUpdatePaymentForClient([FromBody] SubscriptionUpdateForClientCommand command)
        {
            _logger.LogInformation("New {CommandName} arrived.", nameof(SubscriptionUpdateForClientCommand)); 
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result = await _validationHandler.SubmitAsync<SubscriptionUpdateForClientCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<CommandResponse> UpdateClientSubscriptionInformation(
            [FromBody] UpdateClientSubscriptionInformationCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("command", "Invalid Value");
                return response;
            }

            var result = await _validationHandler.SubmitAsync<UpdateClientSubscriptionInformationCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<CommandResponse> IsAValidRenewSubscriptionRequest(
            [FromBody] IsAValidRenewSubscriptionRequestCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("command", "Invalid Value");
                return response;
            }

            var result =
                await _commandService.SubmitAsync<IsAValidRenewSubscriptionRequestCommand, CommandResponse>(command);

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> UpdateObjectArtifact([FromBody] ObjectArtifactUpdateCommand command)
        {
            if (command == null)
            {
                var response = new RiqsCommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<ObjectArtifactUpdateCommand, RiqsCommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<ObjectArtifactUpdateCommand, RiqsCommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> RenameObjectArtifact([FromBody] ObjectArtifactRenameCommand command)
        {
            if (command == null)
            {
                var response = new RiqsCommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<ObjectArtifactRenameCommand, RiqsCommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<ObjectArtifactRenameCommand, RiqsCommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> ActivateDeactivateObjectArtifact(
            [FromBody] ObjectArtifactActivationDeactivationCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler
                    .SubmitAsync<ObjectArtifactActivationDeactivationCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService
                    .SubmitAsync<ObjectArtifactActivationDeactivationCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> ApproveObjectArtifact([FromBody] ObjectArtifactApprovalCommand command)
        {
            if (command == null)
            {
                var response = new RiqsCommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<ObjectArtifactApprovalCommand, RiqsCommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<ObjectArtifactApprovalCommand, RiqsCommandResponse>(command);
            }

            return result;
        }


        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> SearchObjectArtifact([FromBody] ObjectArtifactSearchCommand command)
        {
            if (command == null)
            {
                var response = new RiqsCommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<ObjectArtifactSearchCommand, RiqsCommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<ObjectArtifactSearchCommand, RiqsCommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> GetLibraryDirectories([FromBody] LibraryDirectoryGetCommand command)
        {
            if (command == null)
            {
                var response = new RiqsCommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<LibraryDirectoryGetCommand, RiqsCommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<LibraryDirectoryGetCommand, RiqsCommandResponse>(command);
            }

            return result;
        }


        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> GetLibraryFileVersionComparison([FromBody] LibraryFileVersionComparisonCommand command)
        {
           
            var response = new RiqsCommandResponse();

            if (command == null || string.IsNullOrEmpty(command.ObjectArtifactId))
            {
                response.SetError("Command", "Invalid value: ObjectArtifactId is required.");
                return response;
            }

            try
            {
                
                var result = await _validationHandler.SubmitAsync<LibraryFileVersionComparisonCommand, RiqsCommandResponse>(command);

                if (result.StatusCode.Equals(0))
                {
                    return _serviceClient.SendToQueue<RiqsCommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
                }


                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing GetLibraryFileVersionComparison. Error Message: {Message}. Error Details: {StackTrace}.", ex.Message, ex.StackTrace);

                response.SetError("Server Error", "An error occurred while processing the request.");
                return response;
            }
        }


        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> ShareObjectArtifactFile(
            [FromBody] ObjectArtifactFileShareCommand command)
        {
            if (command == null)
            {
                var response = new RiqsCommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<ObjectArtifactFileShareCommand, RiqsCommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<ObjectArtifactFileShareCommand, RiqsCommandResponse>(command);
            }

            return result;
        }


        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> ShareObjectArtifactFolder(
            [FromBody] ObjectArtifactFolderShareCommand command)
        {
            if (command == null)
            {
                var response = new RiqsCommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<ObjectArtifactFolderShareCommand, RiqsCommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService
                    .SubmitAsync<ObjectArtifactFolderShareCommand, RiqsCommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> MoveObjectArtifact([FromBody] ObjectArtifactMoveCommand command)
        {
            if (command == null)
            {
                var response = new RiqsCommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<ObjectArtifactMoveCommand, RiqsCommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<ObjectArtifactMoveCommand, RiqsCommandResponse>(command);
            }

            return result;
        }

        // depricated endpoints

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<CommandResponse> UpdateClientPayment([FromBody] UpdateClientPaymentCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("command", "Invalid Value");
                return response;
            }

            var result = await _validationHandler.SubmitAsync<UpdateClientPaymentCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> CreateShift([FromBody] CreateShiftCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<CreateShiftCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<CreateShiftCommand, CommandResponse>(command);
            }

            return result;
        }
        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> DeleteShift([FromBody] DeleteShiftCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<DeleteShiftCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<DeleteShiftCommand, CommandResponse>(command);
            }

            return result;
        }
        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> EditShift([FromBody] EditShiftCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<EditShiftCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<EditShiftCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> CreateShiftPlan([FromBody] CreateShiftPlanCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result = await _validationHandler.SubmitAsync<CreateShiftPlanCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<CreateShiftPlanCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CreateLibraryGroup([FromBody] CreateLibraryGroupCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<CreateLibraryGroupCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<CreateLibraryGroupCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> UpdateShiftPlan([FromBody] UpdateShiftPlanCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<UpdateShiftPlanCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<UpdateShiftPlanCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public async Task<CommandResponse> DeteteShiftPlan([FromBody] DeleteShiftPlanCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<DeleteShiftPlanCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<DeleteShiftPlanCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> GetHtmlFileIdFromObjectArtifactDocument(
            [FromBody] GetHtmlFileIdFromObjectArtifactDocumentCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler
                    .SubmitAsync<GetHtmlFileIdFromObjectArtifactDocumentCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisDmsConversionQueueName(), command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> ProcessDraftedObjectArtifactDocument(
            [FromBody] ProcessDraftedObjectArtifactDocumentCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler
                    .SubmitAsync<ProcessDraftedObjectArtifactDocumentCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            return result;
        }


        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> DraftDocumentEditRecord(
            [FromBody] CreateDocumentEditMappingRecordCommand command)
        {
            if (command == null)
            {
                var response = new RiqsCommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler
                    .SubmitAsync<CreateDocumentEditMappingRecordCommand, RiqsCommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<CreateDocumentEditMappingRecordCommand, RiqsCommandResponse>(
                    command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> IsAValidArtifactEditRequest(
            [FromBody] IsAValidArtifactEditRequestCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("command", "Invalid Value");
                return response;
            }

            var result =
                await _commandService.SubmitAsync<IsAValidArtifactEditRequestCommand, CommandResponse>(command);

            return result;
        }

        [HttpPost]
        [Authorize]
        public CommandResponse DeleteObjectArtifact([FromBody] DeleteObjectArtifactCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
        }


        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CloneLibraryForm
        (
            [FromBody] LibraryFormCloneCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return await _commandService
                .SubmitAsync<LibraryFormCloneCommand, CommandResponse>(command);
        }


        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateLibraryForm
        (
            [FromBody] LibraryFormUpdateCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return await _commandService
                .SubmitAsync<LibraryFormUpdateCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public CommandResponse UpdateShiftSequence([FromBody] UpdateShiftSequenceCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
        }

        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> GenerateTwoFactorAuthenticationCode(
            [FromBody] TwoFactorCodeGenerateCommand command)
        {
            return await _commandService
                .SubmitAsync<TwoFactorCodeGenerateCommand, RiqsCommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> GenerateSignatureUrl([FromBody] SignatureGenerateCommand command)
        {
            return await _commandService
                .SubmitAsync<SignatureGenerateCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> AssignLibraryRights([FromBody] LibraryRightsAssignCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return await _commandService.SubmitAsync<LibraryRightsAssignCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> SaveUserActivity([FromBody] UserActivityCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return await _commandService.SubmitAsync<UserActivityCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CloneDepartmentCategories([FromBody] CloneDepartmentCategoriesCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return await _commandService.SubmitAsync<CloneDepartmentCategoriesCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CloneDepartmentSuppliers([FromBody] CloneDepartmentSuppliersCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return await _commandService.SubmitAsync<CloneDepartmentSuppliersCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CloneDepartmentUserAdditionalInfos([FromBody] CloneDepartmentUserAdditionalInfosCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return await _commandService.SubmitAsync<CloneDepartmentUserAdditionalInfosCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public CommandResponse UpdateTasks([FromBody] TasksUpdateCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
        }

        [HttpPost]
        [Authorize]
        public CommandResponse CloneShiftPlans([FromBody] CloneShiftPlansCommand command)
        {
            if (command != null)
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);

            var response = new CommandResponse();
            response.SetError("Command", "Invalid value");
            return response;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CloneShiftPlan([FromBody] CloneShiftPlanCommand command)
        {
            if (command != null)
                return await _commandService.SubmitAsync<CloneShiftPlanCommand, CommandResponse>(command);

            var response = new CommandResponse();
            response.SetError("Command", "Invalid value");
            return response;
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<CommandResponse> ProcessScheduledMaintenance([FromBody] ProcessScheduledMaintenance command)
        {
            if (command != null)
                return await _commandService.SubmitAsync<ProcessScheduledMaintenance, CommandResponse>(command);

            var response = new CommandResponse();
            response.SetError("Command", "Invalid value");
            return response;
        }

        [HttpPost]
        [Authorize]
        public CommandResponse CreateMaintenance([FromBody] CreateMaintenanceCommand command)
        {
            var response = new CommandResponse();
            if (command == null)
            {
                response.SetError("Command", "Invalid value");
                return response;
            }

            response = _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            return response;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> AssignEquipmentAdmins([FromBody] AssignEquipmentAdminsCommand command)
        {
            if (command is null)
            {
                var response = new CommandResponse();
                response.SetError("AssignEquipmentAdminsCommand", "Invalid command");
                return response;
            }

            var result = await _validationHandler.SubmitAsync<AssignEquipmentAdminsCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<AssignEquipmentAdminsCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<RiqsCommandResponse> Generate2FaCodeForEquipment([FromBody] GenerateTwofaCodeForEquimentDetailCommand command)
        {
            if (command is null ||
                string.IsNullOrWhiteSpace(command.EquipementId) ||
                string.IsNullOrWhiteSpace(command.SupplierId) 
                )
            {
                var response = new RiqsCommandResponse();
                response.SetError("Generate2FaCodeForEquipment", "Invalid command");
                return response;
            }
            return await _commandService.SubmitAsync<GenerateTwofaCodeForEquimentDetailCommand, RiqsCommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> AssignProcessGuideForEquipment([FromBody]AssignProcessGuideForEquipmentCommand command)
        {
            var result = await _validationHandler.SubmitAsync<AssignProcessGuideForEquipmentCommand, CommandResponse>(command);
            
            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<AssignProcessGuideForEquipmentCommand, CommandResponse>(command);
            }

            return result;
        }


        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> ExportSuppliersReport([FromBody] ExportSuppliersReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> MarkAsReadDocument([FromBody] ObjectArtifactMarkAsReadCommand command)
        {
            if (command is null ||
                string.IsNullOrWhiteSpace(command.ObjectArtifactId))
            {
                var response = new CommandResponse();
                response.SetError("CommandResponse", "Invalid command or Object Artifact Not found");
                return response;
            }
            return await _commandService.SubmitAsync<ObjectArtifactMarkAsReadCommand, CommandResponse>(command);
        }
        
        public async Task<CommandResponse> UpdateSupplierGroupName([FromBody] UpdateSupplierGroupNameCommand command)
        {
            if (command is null ||
                string.IsNullOrWhiteSpace(command.PraxisClientId) ||
                 command.SupplierGroupName==null)
            {
                var response = new CommandResponse();
                response.SetError("CommandResponse", "Invalid command or Object Artifact Not found");
                return response;
            }
            return await _commandService.SubmitAsync<UpdateSupplierGroupNameCommand, CommandResponse>(command);
        }
        
        #region CockpitSummary
        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteCockpitSummary([FromBody] DeleteCockpitSummaryCommand command)
        {
            if (command?.TaskSummaryIds == null || command.TaskSummaryIds.Length == 0 || string.IsNullOrWhiteSpace(command?.RelatedEntityName))
            {
                var response = new CommandResponse();
                response.SetError("CommandResponse", "Invalid Command. TaskScheduleIds or RelatedEntityName not provided");
                return response;
            }

            return await _commandService.SubmitAsync<DeleteCockpitSummaryCommand, CommandResponse>(command);
        }
        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateCockpitSummary([FromBody] UpdateCockpitSummaryCommand command)
        {
            if (command?.TaskScheduleIds == null || command.TaskScheduleIds.Length == 0 || string.IsNullOrWhiteSpace(command?.RelatedEntityName))
            {
                var response = new CommandResponse();
                response.SetError("CommandResponse", "Invalid Command. TaskScheduleIds or RelatedEntityName not provided");
                return response;
            }

            return await _commandService.SubmitAsync<UpdateCockpitSummaryCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DiscardCockpitSummary([FromBody] DiscardCockpitSummaryCommand command)
        {
            if (command?.RelatedEntityIds == null || command.RelatedEntityIds.Length == 0)
            {
                var response = new CommandResponse();
                response.SetError("CommandResponse", "Invalid Command. RelatedEntityIds not provided");
                return response;
            }
            return await _commandService.SubmitAsync<DiscardCockpitSummaryCommand, CommandResponse>(command);
        }
        
        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> GetRiqsTranslations([FromBody] GetRiqsTranslationCommand command)
        {
            if (command is null)
            {
                var response = new RiqsCommandResponse();
                response.SetError("GetRiqsTranslationCommand", "Invalid command");   
                return response;
            }

            var result = await _validationHandler.SubmitAsync<GetRiqsTranslationCommand, RiqsCommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<GetRiqsTranslationCommand, RiqsCommandResponse>(command);
            }

            return result;
         }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteCockpitDataByContext([FromBody] DeleteCockpitDataCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("CommandResponse", "Invalid Command. Command is null");
                return response;
            }
            return await _commandService.SubmitAsync<DeleteCockpitDataCommand, CommandResponse>(command);
        }
        #endregion
        
        [HttpPost]
        [Authorize]
        public CommandResponse DeleteClonedProcessGuide([FromBody] DeleteClonedProcessGuideCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CCreateStandardLibraryForm
        (
            [FromBody] CreateStandardLibraryFormCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return await _commandService
                .SubmitAsync<CreateStandardLibraryFormCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateSubscriptionPriceConfig([FromBody] UpdateSubscriptionPriceConfigCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return await _commandService
                .SubmitAsync<UpdateSubscriptionPriceConfigCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> ExportLibraryDocumentAssigneeReport(
            [FromBody] ExportLibraryDocumentAssigneesReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> SaveOrUpdateSubscriptionCustomPricingPackage([FromBody] SubscriptionPricingCustomPackageCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            } 

            return await _commandService.SubmitAsync<SubscriptionPricingCustomPackageCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteSubscriptionCustomPricingPackage([FromBody] DeleteSubscriptionPricingCustomPackageCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value"); 
                return response;
            }

            return await _commandService.SubmitAsync<DeleteSubscriptionPricingCustomPackageCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public CommandResponse CreateSubscriptionInvoice([FromBody] SubscriptionGenerateInvoiceCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
        }
        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteMultipleData([FromBody] DeleteMultipleDataCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            _logger.LogInformation("New {CommandName} arrived with Entity Name: {EntityName} and Item Ids :{ItemIds}",
                nameof(DeleteMultipleDataCommand), command.EntityName, JsonConvert.SerializeObject(command.ItemIds));
            
            var result = await _validationHandler.SubmitAsync<DeleteMultipleDataCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }

            _logger.LogError("Validation Failed. Validation {Result} : ", JsonConvert.SerializeObject(result));
            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteProcessGuideFromEquipment(
            [FromBody] DeleteProcessGuideFromEquipmentCommand command)
        {
            if (command?.EquipmentId == null || command.FormIds == null || command.FormIds.Count == 0)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid command");
                return response;
            }
            return await _commandService.SubmitAsync<DeleteProcessGuideFromEquipmentCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public CommandResponse FileUploadToVectorDB(
            [FromBody] List<FileUploadToVectorDBCommand> command)
        {
            if (command == null)
            {
                var response = new RiqsCommandResponse();
                response.SetError("Command", "Invalid command");
                return response;
            }
            return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteLibraryFilesFromEquipment([FromBody] DeleteLibraryFilesFromEquipmentCommand command)
        {
            if (!(command?.FileIds?.Count > 0) || string.IsNullOrEmpty(command?.EquipmentId))
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid command");
                return response;
            }
            return await _commandService.SubmitAsync<DeleteLibraryFilesFromEquipmentCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CreateRiqsPediaViewControl(
           [FromBody] UpsertRiqsPediaViewControlCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid command");
                return response;
            }
            return await _commandService.SubmitAsync<UpsertRiqsPediaViewControlCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteTaskScheduleData([FromBody] DeleteTaskScheduleDataCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return await _commandService.SubmitAsync<DeleteTaskScheduleDataCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CreateMalfunctionGroup([FromBody] CreateMalfunctionGroupCommand command)
        {
            if (command == null || 
                string.IsNullOrEmpty(command.Name) || 
                string.IsNullOrEmpty(command.ClientId) || 
                string.IsNullOrEmpty(command.OrganizationId))
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return await _commandService.SubmitAsync<CreateMalfunctionGroupCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateMalfunctionGroup([FromBody] UpdateMalfunctionGroupCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.ItemId))
            {
                var response = new CommandResponse();
                response.SetError("Command", "ItemId is mandatory");
                return response;
            }
            return await _commandService.SubmitAsync<UpdateMalfunctionGroupCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateStatusMalfunctionGroup([FromBody] UpdateStatusMalfunctionGroupCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.ItemId))
            {
                var response = new CommandResponse();
                response.SetError("Command", "ItemId is mandatory");
                return response;
            }
            return await _commandService.SubmitAsync<UpdateStatusMalfunctionGroupCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteMalfunctionGroups([FromBody] DeleteMalfunctionGroupCommand command)
        {
            if (command == null || !(command.ItemIds?.Length > 0))
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return await _commandService.SubmitAsync<DeleteMalfunctionGroupCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteCockpitObjectArtifactSummary([FromBody] DeleteCockpitObjectArtifactSummaryCommand command)
        {
            var result = await _validationHandler.SubmitAsync<DeleteCockpitObjectArtifactSummaryCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<DeleteCockpitObjectArtifactSummaryCommand, CommandResponse>(command);
            }
            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> MarkAsPaidOfflineInvoice([FromBody] MarkAsPaidOfflineInvoiceComman command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return await _commandService.SubmitAsync<MarkAsPaidOfflineInvoiceComman, CommandResponse>(command);
        }
    }
}
