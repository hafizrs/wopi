using System;
using Microsoft.AspNetCore.Mvc;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.ValidationHandlers;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CirsReports;

namespace Selise.Ecap.SC.PraxisMonitor.WebService
{
    public class CirsReportController : ControllerBase
    {
        private readonly CommandHandler _commandHandler;
        private readonly ValidationHandler _validationHandler;
        private readonly QueryHandler _queryHandler;

        public CirsReportController(
            CommandHandler commandService,
            ValidationHandler validationHandler,
            QueryHandler queryHandler)
        {
            _commandHandler = commandService;
            _validationHandler = validationHandler;
            _queryHandler = queryHandler;
        }

        [HttpPost]
        [Authorize]
        public Task<CommandResponse> CreateComplain([FromBody] CreateComplainReportCommand command)
            => ValidateAndSubmitCreateCommandAsync(command);

        [HttpPost]
        [Authorize]
        public Task<CommandResponse> CreateIncident([FromBody] CreateIncidentReportCommand command)
            => ValidateAndSubmitCreateCommandAsync(command);

        [HttpPost]
        [Authorize]
        public Task<CommandResponse> CreateIdea([FromBody] CreateIdeaReportCommand command)
            => ValidateAndSubmitCreateCommandAsync(command);

        [HttpPost]
        [Authorize]
        public Task<CommandResponse> CreateHint([FromBody] CreateHintReportCommand command)
            => ValidateAndSubmitCreateCommandAsync(command);

        [HttpPost]
        [Authorize]
        public Task<CommandResponse> CreateAnother([FromBody] CreateAnotherMessageCommand command)
            => ValidateAndSubmitCreateCommandAsync(command);

        [HttpPost]
        [Authorize]
        public Task<CommandResponse> CreateFault([FromBody] CreateFaultReportCommand command)
            => ValidateAndSubmitCreateCommandAsync(command);

        [HttpPost]
        [Authorize]
        public Task<CommandResponse> UpdateIncident([FromBody] UpdateIncidentReportCommand command)
            => ValidateAndSubmitUpdateCommandAsync(command);

        [HttpPost]
        [Authorize]
        public Task<CommandResponse> UpdateComplain([FromBody] UpdateComplainReportCommand command)
            => ValidateAndSubmitUpdateCommandAsync(command);

        [HttpPost]
        [Authorize]
        public Task<CommandResponse> UpdateHint([FromBody] UpdateHintReportCommand command)
            => ValidateAndSubmitUpdateCommandAsync(command);

        [HttpPost]
        [Authorize]
        public Task<CommandResponse> UpdateIdea([FromBody] UpdateIdeaReportCommand command)
            => ValidateAndSubmitUpdateCommandAsync(command);

        [HttpPost]
        [Authorize]
        public Task<CommandResponse> UpdateAnother([FromBody] UpdateAnotherMessageCommand command)
            => ValidateAndSubmitUpdateCommandAsync(command);

        [HttpPost]
        [Authorize]
        public Task<CommandResponse> UpdateFault([FromBody] UpdateFaultReportCommand command)
            => ValidateAndSubmitUpdateCommandAsync(command);

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> ActiveInactive([FromBody] ActiveInactiveCirsReportCommand  command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<ActiveInactiveCirsReportCommand, CommandResponse>(command)
                ?? throw new ArgumentNullException(nameof(ActiveInactiveCirsReportCommand));

            return result.StatusCode.Equals(0)
                ? await _commandHandler.SubmitAsync<ActiveInactiveCirsReportCommand, CommandResponse>(command)
                : result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> MoveToOtherDashboard([FromBody] MoveToOtherDashboardCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<MoveToOtherDashboardCommand, CommandResponse>(command)
                ?? throw new ArgumentNullException(nameof(MoveToOtherDashboardCommand));

            return result.StatusCode.Equals(0)
                ? await _commandHandler.SubmitAsync<MoveToOtherDashboardCommand, CommandResponse>(command)
                : result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteCirsReport([FromBody] DeleteCirsReportsCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<DeleteCirsReportsCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandHandler.SubmitAsync<DeleteCirsReportsCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> AssignCirsAdmins([FromBody] AssignCirsAdminsCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<AssignCirsAdminsCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandHandler.SubmitAsync<AssignCirsAdminsCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetReports([FromBody] GetCirsReportQuery query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            return await _queryHandler.SubmitAsync<GetCirsReportQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetCirsReportByIds([FromBody] GetCirsReportByIdsQuery query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            return await _queryHandler.SubmitAsync<GetCirsReportByIdsQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetCirsAdmins([FromBody] GetCirsAdminsQuery query)
        {
            return _queryHandler.SubmitAsync<GetCirsAdminsQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetUserCirsPermissionInfo([FromBody] GetUserCirsPermissionInfoQuery query)
        {
            return _queryHandler.SubmitAsync<GetUserCirsPermissionInfoQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public async Task<QueryHandlerResponse> GetFaultReportByEquipmentId([FromBody] GetFaultReportByEquipmentIdQuery query)
        {
            if (query == null || string.IsNullOrEmpty(query.EquipmentId) || string.IsNullOrEmpty(query.PraxisClientId) || string.IsNullOrEmpty(query.OrganizationId))
            {
                var response = new QueryHandlerResponse
                {
                    StatusCode = 1,
                    ErrorMessage = "Invalid Query"
                };
                return response;
            }
            return await _queryHandler.SubmitAsync<GetFaultReportByEquipmentIdQuery, QueryHandlerResponse>(query);
        }

        private async Task<CommandResponse> ValidateAndSubmitCreateCommandAsync<TCreateCirsReportCommand>(
            TCreateCirsReportCommand command) where TCreateCirsReportCommand : AbstractCreateCirsReportCommand
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<TCreateCirsReportCommand, CommandResponse>(command) 
                ?? throw new ArgumentNullException(nameof(TCreateCirsReportCommand));

            return result.StatusCode.Equals(0)
                ? await _commandHandler.SubmitAsync<TCreateCirsReportCommand, CommandResponse>(command)
                : result;
        }

        private async Task<CommandResponse> ValidateAndSubmitUpdateCommandAsync<TUpdateCirsReportCommand>(
            TUpdateCirsReportCommand command) where TUpdateCirsReportCommand : AbstractUpdateCirsReportCommand
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            var result =
                await _validationHandler.SubmitAsync<TUpdateCirsReportCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandHandler.SubmitAsync<TUpdateCirsReportCommand, CommandResponse>(command);
            }

            return result;
        }
    }
}
