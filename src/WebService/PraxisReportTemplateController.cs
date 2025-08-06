using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Selise.Ecap.Entities.PrimaryEntities.DWT;
using Selise.Ecap.SC.PraxisMonitor.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels;
using Selise.Ecap.SC.PraxisMonitor.ValidationHandlers;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.WebService
{
    public class PraxisReportTemplateController : ControllerBase
    {
        private readonly CommandHandler _commandHandler;
        private readonly ValidationHandler _validationHandler;
        private readonly QueryHandler _queryHandler;
        private readonly IServiceClient _serviceClient;
        public PraxisReportTemplateController(
            CommandHandler commandHandler,
            ValidationHandler validationHandler,
            QueryHandler queryHandler,
            IServiceClient serviceClient)
        {
            _commandHandler = commandHandler;
            _validationHandler = validationHandler;
            _queryHandler = queryHandler;
            _serviceClient = serviceClient;
        }

        #region Command
        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CreateReportTemplate([FromBody] CreateReportTemplateCommand command)
        {
            return await SubmitAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateReportTemplate([FromBody] UpdateReportTemplateCommand command)
        {
            return await SubmitAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateReportTemplateSection([FromBody] UpdateReportTemplateSectionCommand command)
        {
            return await SubmitAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteReportTemplate([FromBody] DeleteReportTemplateCommand command)
        {
            return await SubmitAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CreateReportTemplateSection([FromBody] CreateReportTemplateSectionCommand command)
        {
            return await SubmitAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CreateGeneratedReportTemplateConfig([FromBody] CreateGeneratedReportTemplateConfigCommand command)
        {
            return await SubmitAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateGeneratedReportTemplateConfig([FromBody] UpdateGeneratedReportTemplateConfigCommand command)
        {
            return await SubmitAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteGeneratedReportTemplateConfig([FromBody] DeleteGeneratedReportTemplateConfigCommand command)
        {
            return await SubmitAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CreateGeneratedReportTemplateSection([FromBody] CreateGeneratedReportTemplateSectionCommand command)
        {
            return await SubmitAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateGeneratedReportTemplateSection([FromBody] UpdateGeneratedReportTemplateSectionCommand command)
        {
            return await SubmitAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> AssignTemplateToEquipment([FromBody] AssignTemplateToEquipmentCommand command)
        {
            return await SubmitAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> GenerateValidationReportPdf([FromBody] GenerateValidationReportTemplatePdfCommand command)
        {
            var validationResult = await _validationHandler.SubmitAsync<GenerateValidationReportTemplatePdfCommand, CommandResponse>(command);
            if (validationResult.StatusCode.Equals(0))
            {
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }
            return validationResult;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> ApproveGeneratedReport([FromBody] ApproveGeneratedReportCommand command)
        {
            return await _commandHandler.SubmitAsync<ApproveGeneratedReportCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public Task<CommandResponse> GenerateReportSignatureUrl([FromBody] GenerateReportSignatureUrlCommand command)
        {
            var response = new CommandResponse();
            if (string.IsNullOrEmpty(command?.ReportId))
            {
                response.SetError("Message", "ReportId is not valid");
                return Task.FromResult(response);
            }
            try
            {
                response = _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            }
            catch (Exception ex)
            {
                response.SetError("Message", ex.Message);
            }
            return Task.FromResult(response);
        }

        private async Task<CommandResponse> SubmitAfterValidation<TCommand>(TCommand command)
        {
            var validationResult = await _validationHandler.SubmitAsync<TCommand, CommandResponse>(command);
            if (validationResult.StatusCode.Equals(0))
            {
                return await _commandHandler.SubmitAsync<TCommand, CommandResponse>(command);
            }
            return validationResult;
        }
        #endregion

        #region Query

        [HttpPost]
        [Authorize]
        public async Task<EntityQueryResponse<ReportTemplatesResponse>> GetReportTemplate([FromBody] GetReportTemplatesQuery query)
        {
            if (string.IsNullOrEmpty(query?.FilterString) || query.PageSize <= 0 || query.PageNumber < 0)
            {
                return GetInvalidQueryResponse<ReportTemplatesResponse>();
            }
            return await _queryHandler.SubmitAsync<GetReportTemplatesQuery, EntityQueryResponse<ReportTemplatesResponse>>(query);
        }
        [HttpPost]
        [Authorize]
        public async Task<EntityQueryResponse<ReportTemplateDetailsResponse>> GetReportTemplateDetails([FromBody] GetReportTemplateDetailsQuery query)
        {
            if (string.IsNullOrEmpty(query?.FilterString))
            {
                return GetInvalidQueryResponse<ReportTemplateDetailsResponse>();
            }
            return await _queryHandler.SubmitAsync<GetReportTemplateDetailsQuery, EntityQueryResponse<ReportTemplateDetailsResponse>>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<EntityQueryResponse<ReportTemplateSectionResponse>> GetReportTemplateSections([FromBody] GetReportTemplateSectionsQuery query)
        {
            if (string.IsNullOrEmpty(query?.FilterString) || query.PageSize <= 0 || query.PageNumber < 0)
            {
                return GetInvalidQueryResponse<ReportTemplateSectionResponse>();
            }
            return await _queryHandler.SubmitAsync<GetReportTemplateSectionsQuery, EntityQueryResponse<ReportTemplateSectionResponse>>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<EntityQueryResponse<GeneratedReportTemplateResponse>> GetGeneratedReportTemplates([FromBody] GetGeneratedReportTemplateQuery query)
        {
            if (string.IsNullOrEmpty(query?.FilterString) || query.PageSize <= 0 || query.PageNumber < 0)
            {
                return GetInvalidQueryResponse<GeneratedReportTemplateResponse>();
            }
            return await _queryHandler.SubmitAsync<GetGeneratedReportTemplateQuery, EntityQueryResponse<GeneratedReportTemplateResponse>>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<EntityQueryResponse<GeneratedReportTemplateDetailsResponse>> GetGeneratedReportTemplateDetails([FromBody] GetGeneratedReportTemplateDetailsQuery query)
        {
            if (string.IsNullOrEmpty(query?.FilterString))
            {
                return GetInvalidQueryResponse<GeneratedReportTemplateDetailsResponse>();
            }
            return await _queryHandler.SubmitAsync<GetGeneratedReportTemplateDetailsQuery, EntityQueryResponse<GeneratedReportTemplateDetailsResponse>>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<EntityQueryResponse<GeneratedReportTemplateSectionResponse>> GetGeneratedReportTemplateSections([FromBody] GetGeneratedReportTemplateSectionsQuery query)
        {
            if (string.IsNullOrEmpty(query?.FilterString) || query.PageSize <= 0 || query.PageNumber < 0)
            {
                return GetInvalidQueryResponse<GeneratedReportTemplateSectionResponse>();
            }
            return await _queryHandler.SubmitAsync<GetGeneratedReportTemplateSectionsQuery, EntityQueryResponse<GeneratedReportTemplateSectionResponse>>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetPastReportSummary([FromBody]GetPastReportSummariesQuery query)
        {
            var response = new QueryHandlerResponse();
            if (string.IsNullOrEmpty(query?.EquipmentId))
            {
                response.StatusCode = 1;
                response.ErrorMessage = "EquipmentId is not valid";
                return response;
            }
            return await _queryHandler.SubmitAsync<GetPastReportSummariesQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<EntityQueryResponse<ReportTemplatePermissionRecord>> GetReportTemplatePermissions([FromBody] GetReportTemplatePermissionsQuery query)
        {
            if (string.IsNullOrEmpty(query?.ClientId))
            {
                return GetInvalidQueryResponse<ReportTemplatePermissionRecord>();
            }
            return await _queryHandler.SubmitAsync<GetReportTemplatePermissionsQuery, EntityQueryResponse<ReportTemplatePermissionRecord>>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<EntityQueryResponse<EquipmentReportPermissionRecord>> GetEquipmentReportPermissions([FromBody] GetEquipmentReportPermissionsQuery query)
        {
            if (string.IsNullOrEmpty(query?.ClientId) || string.IsNullOrEmpty(query?.OrganizationId) || string.IsNullOrEmpty(query?.EquipmentId))
            {
                return GetInvalidQueryResponse<EquipmentReportPermissionRecord>();
            }
            return await _queryHandler.SubmitAsync<GetEquipmentReportPermissionsQuery, EntityQueryResponse<EquipmentReportPermissionRecord>>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetReportSignatureUrl([FromBody] GetReportSignatureUrlQuery query)
        {
            var response = new QueryHandlerResponse();
            if (string.IsNullOrEmpty(query?.ReportId))
            {
                response.StatusCode = 1;
                response.ErrorMessage = "ReportId is not valid";
                return response;
            }
            return await _queryHandler.SubmitAsync<GetReportSignatureUrlQuery, QueryHandlerResponse>(query);
        }

        private EntityQueryResponse<TResponse> GetInvalidQueryResponse<TResponse>()
        {
            var response = new EntityQueryResponse<TResponse>
            {
                Results = null,
                StatusCode = 1,
                ErrorMessage = "Query is not valid"
            };
            return response;
        }

        [HttpPost]
        [Authorize]
        public async Task<EntityQueryResponse<EquipmentReportTemplatesResponse>> GetEquipmentReportTemplates([FromBody] GetEquipmentReportTemplatesQuery query)
        {
            if (string.IsNullOrEmpty(query?.FilterString) || query.PageSize <= 0 || query.PageNumber < 0)
            {
                return GetInvalidQueryResponse<EquipmentReportTemplatesResponse>();
            }
            return await _queryHandler.SubmitAsync<GetEquipmentReportTemplatesQuery, EntityQueryResponse<EquipmentReportTemplatesResponse>>(query);
        }

        #endregion

    }
}
