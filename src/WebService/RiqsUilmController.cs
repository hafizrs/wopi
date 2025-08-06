using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.WebService
{
    public class RiqsUilmController : ControllerBase
    {
        private readonly ILogger<RiqsUilmController> _logger;
        private readonly QueryHandler _queryHandler;
        private readonly CommandHandler _commandService;

        public RiqsUilmController(
            ILogger<RiqsUilmController> logger,
            QueryHandler queryHandler,
            CommandHandler commandService
        )
        {
            _logger = logger;
            _queryHandler = queryHandler;
            _commandService = commandService;
        }

        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> DownloadUilmResourceKeysAsJson(
         [FromBody] DownloadUilmResourceKeysAsJsonCommand command)
        {
            return await _commandService.SubmitAsync<DownloadUilmResourceKeysAsJsonCommand, RiqsCommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UploadUilmResourceKeysFromJson(
         [FromBody] UploadUilmResourceKeysFromJsonCommand command)
        {
            if (string.IsNullOrEmpty(command.FileId))
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid command");
                return response;
            }
            return await _commandService.SubmitAsync<UploadUilmResourceKeysFromJsonCommand, CommandResponse>(command);
        }
    }
}
