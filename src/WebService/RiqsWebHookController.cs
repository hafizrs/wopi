using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Threading.Tasks;


namespace Selise.Ecap.SC.PraxisMonitor.WebService
{
    public class RiqsWebHookController : ControllerBase
    {
        private readonly CommandHandler _commandService;
        private readonly ILogger<RiqsWebHookController> _logger;
        private readonly IServiceClient _serviceClient;

        public RiqsWebHookController(
            CommandHandler commandService,
            ILogger<RiqsWebHookController> logger,
            IServiceClient serviceClient
        )
        {
            _commandService = commandService;
            _logger = logger;
            _serviceClient = serviceClient;
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<CommandResponse> UpdateManualFileUploadStatus([FromBody] UpdateManualFileUploadStatusCommand command)
        {
            _logger.LogInformation("Webhook received: {Command}", command);

            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return await _commandService.SubmitAsync<UpdateManualFileUploadStatusCommand, CommandResponse>(command);
            //return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisQueueName(), command);
        }
    }
}

