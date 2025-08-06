using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.WopiMonitor.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.WopiMonitor.Contracts.Queries.WopiModule;
using SeliseBlocks.Genesis.Framework;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.WopiMonitor.WebService
{
    public class WopiController : ControllerBase
    {
        private readonly CommandHandler _commandService;
        private readonly ValidationHandler _validationHandler;
        private readonly QueryHandler _queryHandler;
        private readonly IServiceClient _serviceClient;
        private readonly ILogger<WopiController> _logger;

        public WopiController(
            CommandHandler commandService,
            ValidationHandler validationHandler,
            QueryHandler queryHandler,
            IServiceClient serviceClient,
            ILogger<WopiController> logger)
        {
            _commandService = commandService;
            _validationHandler = validationHandler;
            _queryHandler = queryHandler;
            _serviceClient = serviceClient;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        public async Task<CreateWopiSessionResponse> CreateSession([FromBody] CreateWopiSessionCommand command)
        {
            if (command == null) return null;

            var result = await _validationHandler.SubmitAsync<CreateWopiSessionCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<CreateWopiSessionCommand, CreateWopiSessionResponse>(command);
            }

            return null;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> DeleteSession([FromBody] DeleteWopiSessionCommand command)
        {
            if (command == null) return ErrorResponse();
            var result = await _validationHandler.SubmitAsync<DeleteWopiSessionCommand, CommandResponse>(command);

            if (result.StatusCode.Equals(0))
            {
                return await _commandService.SubmitAsync<DeleteWopiSessionCommand, CommandResponse>(command);
            }

            return result;
        }

        [HttpPost]
        [Authorize]
        public QueryHandlerResponse GetAllSessions([FromBody] GetWopiSessionsQuery query)
        {
            return _queryHandler.Submit<GetWopiSessionsQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public QueryHandlerResponse GetSessionInfo([FromBody] GetWopiSessionQuery query)
        {
            return _queryHandler.Submit<GetWopiSessionQuery, QueryHandlerResponse>(query);
        }

        // WOPI Protocol Endpoints - following JavaScript endpoint names exactly
        [HttpPost]
        [AllowAnonymous]
        public QueryHandlerResponse CheckFileInfo([FromBody] GetWopiFileInfoQuery query)
        {
            return _queryHandler.Submit<GetWopiFileInfoQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [AllowAnonymous]
        public QueryHandlerResponse GetFile([FromBody] GetWopiFileContentQuery query)
        {
            return _queryHandler.Submit<GetWopiFileContentQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<CommandResponse> PutFile([FromBody] UpdateWopiFileCommand command)
        {
            if (command == null) return ErrorResponse();
            return await _commandService.SubmitAsync<UpdateWopiFileCommand, CommandResponse>(command);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<CommandResponse> Lock([FromBody] LockWopiFileCommand command)
        {
            if (command == null) return ErrorResponse();

            // Set the WopiOverride from header
            command.WopiOverride = Request.Headers["x-wopi-override"].ToString();

            return await _commandService.SubmitAsync<LockWopiFileCommand, CommandResponse>(command);
        }



        [HttpPost]
        [AllowAnonymous]
        public async Task<CommandResponse> CleanupSession([FromBody] DeleteWopiSessionCommand command)
        {
            if (command == null) return ErrorResponse();
            return await _commandService.SubmitAsync<DeleteWopiSessionCommand, CommandResponse>(command);
        }

        private CommandResponse ErrorResponse()
        {
            var response = new CommandResponse();
            response.SetError("Command", "Invalid value");
            return response;
        }
    }
}