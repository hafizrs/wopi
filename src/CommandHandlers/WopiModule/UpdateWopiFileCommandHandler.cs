using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.WopiMonitor.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Contracts.DomainServices.WopiModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.WopiMonitor.CommandHandlers.WopiModule
{
    public class UpdateWopiFileCommandHandler : ICommandHandler<UpdateWopiFileCommand, CommandResponse>
    {
        private readonly IWopiService _service;
        private readonly ILogger<UpdateWopiFileCommandHandler> _logger;
        
        public UpdateWopiFileCommandHandler(IWopiService service, ILogger<UpdateWopiFileCommandHandler> logger)
        {
            _service = service;
            _logger = logger;
        }
        
        public CommandResponse Handle(UpdateWopiFileCommand command)
        {
            throw new NotImplementedException();
        }
        
        public async Task<CommandResponse> HandleAsync(UpdateWopiFileCommand command)
        {
            var response = new CommandResponse();
            _logger.LogInformation("Enter {HandlerName} with payload:{Payload}.", nameof(UpdateWopiFileCommandHandler), JsonConvert.SerializeObject(command));
            
            try
            {
                await _service.UpdateWopiFile(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in {HandlerName}", nameof(UpdateWopiFileCommandHandler));
                _logger.LogError(ex, "Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
                response.SetError("WopiFile", ex.Message);
            }
            
            _logger.LogInformation("Handled By {HandlerName} with payload:{Payload}.", nameof(UpdateWopiFileCommandHandler), JsonConvert.SerializeObject(command));
            return response;
        }
    }
} 