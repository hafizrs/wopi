using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using Selise.Ecap.SC.PraxisMonitor.ValidationHandlers;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.WebService
{
    public class RiqsQuickTaskController : ControllerBase
    {
        private readonly CommandHandler _commandService;
        private readonly ValidationHandler _validationHandler;
        private readonly QueryHandler _queryHandler;
        private readonly IServiceClient _serviceClient;
        private readonly ILogger<RiqsQuickTaskController> _logger;

        public RiqsQuickTaskController(
            CommandHandler commandService,
            ValidationHandler validationHandler,
            QueryHandler queryHandler,
            IServiceClient serviceClient,
            ILogger<RiqsQuickTaskController> logger)
        {
            _commandService = commandService;
            _validationHandler = validationHandler;
            _queryHandler = queryHandler;
            _serviceClient = serviceClient;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> GenerateQuickTaskPlanReport([FromBody] GenerateQuickTaskPlanReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> GenerateQuickTaskReport([FromBody] GenerateQuickTaskReportCommand command)
        {
            return await SendToReportQueueAfterValidation(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CreateQuickTask([FromBody] CreateQuickTaskCommand command)
        {
            if (command == null) return ErrorResponse();

            var result =
                await _validationHandler.SubmitAsync<CreateQuickTaskCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<CreateQuickTaskCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteQuickTask([FromBody] DeleteQuickTaskCommand command)
        {
            if (command == null) return ErrorResponse();
            var result = await _validationHandler.SubmitAsync<DeleteQuickTaskCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<DeleteQuickTaskCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> EditQuickTask([FromBody] EditQuickTaskCommand command)
        {
            if (command == null) return ErrorResponse();
            var result = await _validationHandler.SubmitAsync<EditQuickTaskCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<EditQuickTaskCommand, CommandResponse>(command);
            }
            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CreateQuickTaskPlan([FromBody] CreateQuickTaskPlanCommand command)
        {
            if (command == null) return ErrorResponse();
            var result = await _validationHandler.SubmitAsync<CreateQuickTaskPlanCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<CreateQuickTaskPlanCommand, CommandResponse>(command);
            }
            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateQuickTaskPlan([FromBody] UpdateQuickTaskPlanCommand command)
        {
            if (command == null) return ErrorResponse();
            var result = await _validationHandler.SubmitAsync<UpdateQuickTaskPlanCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<UpdateQuickTaskPlanCommand, CommandResponse>(command);
            }
            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteQuickTaskPlan([FromBody] DeleteQuickTaskPlanCommand command)
        {
            if (command == null) return ErrorResponse();
            var result = await _validationHandler.SubmitAsync<DeleteQuickTaskPlanCommand, CommandResponse>(command);
            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<DeleteQuickTaskPlanCommand, CommandResponse>(command);
            }
            return result;
        }

        [HttpPost]
        [Authorize]
        public CommandResponse UpdateQuickTaskSequence([FromBody] UpdateQuickTaskSequenceCommand command)
        {
            if (command == null) return ErrorResponse();

            return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
        }

        [HttpPost]
        [Authorize]
        public CommandResponse CloneQuickTaskPlans([FromBody] CloneQuickTaskPlansCommand command)
        {
            if (command != null)
                return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
            return ErrorResponse();
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CloneQuickTaskPlan([FromBody] CloneQuickTaskPlanCommand command)
        {
            if (command != null)
                return await _commandService.SubmitAsync<CloneQuickTaskPlanCommand, CommandResponse>(command);
            return ErrorResponse();
        }

        [HttpPost]
        [Authorize]
        public QueryHandlerResponse ValidateQuickTaskInfo([FromBody] ValidateQuickTaskInfo query)
        {
            return _queryHandler.Submit<ValidateQuickTaskInfo, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public QueryHandlerResponse ValidateQuickTaskPlanInfo([FromBody] ValidateQuickTaskPlanInfoQuery query)
        {
            return _queryHandler.Submit<ValidateQuickTaskPlanInfoQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public QueryHandlerResponse GetQuickTasks([FromBody] GetQuickTasksQuery query)
        {
            return _queryHandler.Submit<GetQuickTasksQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public QueryHandlerResponse GetQuickTasksDropdown([FromBody] GetQuickTasksDropdownQuery query)
        {
            return _queryHandler.Submit<GetQuickTasksDropdownQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public QueryHandlerResponse GetQuickTaskPlans([FromBody] GetQuickTaskPlanQuery query)
        {
            return _queryHandler.Submit<GetQuickTaskPlanQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public QueryHandlerResponse GetQuickTaskPlanById([FromBody] GetQuickTaskPlanByIdQuery query)
        {
            return _queryHandler.Submit<GetQuickTaskPlanByIdQuery, QueryHandlerResponse>(query);
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

            _logger.LogError("Validation Failed. Validation {Result} : ", result);
            return result;
        }

        private CommandResponse ErrorResponse()
        {
            var response = new CommandResponse();
            response.SetError("Command", "Invalid value");
            return response;
        }
    }
} 