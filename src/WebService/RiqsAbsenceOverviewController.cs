using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using Selise.Ecap.SC.PraxisMonitor.ValidationHandlers;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.WebService
{
    public class RiqsAbsenceOverviewController : ControllerBase
    {
        private readonly CommandHandler _commandService;
        private readonly ValidationHandler _validationHandler;
        private readonly QueryHandler _queryHandler;
        private readonly IServiceClient _serviceClient;

        public RiqsAbsenceOverviewController(
            CommandHandler commandService,
            ValidationHandler validationHandler,
            QueryHandler queryHandler,
            IServiceClient serviceClient)
        {
            _commandService = commandService;
            _validationHandler = validationHandler;
            _queryHandler = queryHandler;
            _serviceClient = serviceClient;
        }

        #region Absence-Type endpoints
        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CreateAbsenceType([FromBody] CreateAbsenceTypeCommand command)
        {
            if (command == null) return ErrorResponse();

            var result = await _validationHandler.SubmitAsync<CreateAbsenceTypeCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<CreateAbsenceTypeCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateAbsenceType([FromBody] UpdateAbsenceTypeCommand command)
        {
            if (command == null) return ErrorResponse();

            var result = await _validationHandler.SubmitAsync<UpdateAbsenceTypeCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<UpdateAbsenceTypeCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteAbsenceType([FromBody] DeleteAbsenceTypeCommand command)
        {
            if (command == null) return ErrorResponse();

            var result = await _validationHandler.SubmitAsync<DeleteAbsenceTypeCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<DeleteAbsenceTypeCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetAbsenceTypes([FromBody] GetAbsenceTypesQuery query)
        {
            return await _queryHandler.SubmitAsync<GetAbsenceTypesQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetAbsenceTypeById([FromBody] GetAbsenceTypeByIdQuery query)
        {
            return await _queryHandler.SubmitAsync<GetAbsenceTypeByIdQuery, QueryHandlerResponse>(query);
        }
        #endregion Absence-Type endpoints

        #region Absence-Plan endpoints
        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CreateAbsencePlan([FromBody] CreateAbsencePlanCommand command)
        {
            if (command == null) return ErrorResponse();

            var result = await _validationHandler.SubmitAsync<CreateAbsencePlanCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<CreateAbsencePlanCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateAbsencePlan([FromBody] UpdateAbsencePlanCommand command)
        {
            if (command == null) return ErrorResponse();

            var result = await _validationHandler.SubmitAsync<UpdateAbsencePlanCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<UpdateAbsencePlanCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteAbsencePlan([FromBody] DeleteAbsencePlanCommand command)
        {
            if (command == null) return ErrorResponse();

            var result = await _validationHandler.SubmitAsync<DeleteAbsencePlanCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<DeleteAbsencePlanCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateAbsencePlanStatus([FromBody] UpdateAbsencePlanStatusCommand command)
        {
            if (command == null) return ErrorResponse();

            var result = await _validationHandler.SubmitAsync<UpdateAbsencePlanStatusCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<UpdateAbsencePlanStatusCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetAbsencePlans([FromBody] GetAbsencePlansQuery query)
        {
            return await _queryHandler.SubmitAsync<GetAbsencePlansQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetAbsencePlanById([FromBody] GetAbsencePlanByIdQuery query)
        {
            return await _queryHandler.SubmitAsync<GetAbsencePlanByIdQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetAbsencePlanApprovalPermission([FromBody] GetAbsencePlanApprovalPermissionQuery query)
        {
            return await _queryHandler.SubmitAsync<GetAbsencePlanApprovalPermissionQuery, QueryHandlerResponse>(query);
        }
        #endregion Absence-Plan endpoints

        private static CommandResponse ErrorResponse()
        {
            var response = new CommandResponse();
            response.SetError("Command", "Invalid value");
            return response;
        }
    }
}