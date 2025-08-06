using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.WopiMonitor.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Contracts.DomainServices.WopiModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.WopiMonitor.CommandHandlers.WopiModule
{
    public class CreateWopiSessionCommandHandler : ICommandHandler<CreateWopiSessionCommand, CommandResponse>
    {
        private readonly IWopiService _service;
        private readonly ILogger<CreateWopiSessionCommandHandler> _logger;
        
        public CreateWopiSessionCommandHandler(IWopiService service, ILogger<CreateWopiSessionCommandHandler> logger)
        {
            _service = service;
            _logger = logger;
        }
        
        public CommandResponse Handle(CreateWopiSessionCommand command)
        {
            throw new NotImplementedException();
        }
        
        public async Task<CommandResponse> HandleAsync(CreateWopiSessionCommand command)
        {
            var response = new CommandResponse();
            _logger.LogInformation("Enter {HandlerName} with payload:{Payload}.", nameof(CreateWopiSessionCommandHandler), JsonConvert.SerializeObject(command));
            
            try
            {
                var result = await _service.CreateWopiSession(command);
                response.Data = result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in {HandlerName}", nameof(CreateWopiSessionCommandHandler));
                _logger.LogError(ex, "Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
                response.SetError("WopiSession", ex.Message);
            }
            
            _logger.LogInformation("Handled By {HandlerName} with payload:{Payload}.", nameof(CreateWopiSessionCommandHandler), JsonConvert.SerializeObject(command));
            return response;
        }
    }
} 