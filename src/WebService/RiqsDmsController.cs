using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.WebService
{
    public class RiqsDmsController : ControllerBase
    {
        private readonly CommandHandler _commandService;
        private readonly ILogger<RiqsDmsController> _logger;
        private readonly IServiceClient _serviceClient;

        public RiqsDmsController(
            CommandHandler commandService,
            ILogger<RiqsDmsController> logger,
            IServiceClient serviceClient
        )
        {
            _commandService = commandService;
            _logger = logger;
            _serviceClient = serviceClient;
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UploadFile([FromBody] ObjectArtifactFileUploadCommand command)
        {
            _logger.LogInformation("ObjectArtifactFileUploadCommand: {Command}", command);

            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return await _commandService.SubmitAsync<ObjectArtifactFileUploadCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CreateFolder([FromBody] ObjectArtifactFolderCreateCommand command)
        {
            _logger.LogInformation("ObjectArtifactFolderCreateCommand: {Command}", command);

            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return await _commandService.SubmitAsync<ObjectArtifactFolderCreateCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CreateUserWorkspace([FromBody] CreateUserWorkspaceCommand command)
        {
            _logger.LogInformation("CreateUserWorkspaceCommand: {Command}", command);

            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return await _commandService.SubmitAsync<CreateUserWorkspaceCommand, CommandResponse>(command);
        }
    }
}

