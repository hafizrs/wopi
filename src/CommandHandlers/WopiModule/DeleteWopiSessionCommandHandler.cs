using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.WopiMonitor.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Contracts.DomainServices.WopiModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.WopiMonitor.CommandHandlers.WopiModule
{
    public class DeleteWopiSessionCommandHandler : ICommandHandler<DeleteWopiSessionCommand, CommandResponse>
    {
        private readonly IWopiService _service;
        private readonly ILogger<DeleteWopiSessionCommandHandler> _logger;
        
        public DeleteWopiSessionCommandHandler(IWopiService service, ILogger<DeleteWopiSessionCommandHandler> logger)
        {
            _service = service;
            _logger = logger;
        }
        
        public CommandResponse Handle(DeleteWopiSessionCommand command)
        {
            throw new NotImplementedException();
        }
        
        public async Task<CommandResponse> HandleAsync(DeleteWopiSessionCommand command)
        {
            var response = new CommandResponse();
            _logger.LogInformation("Enter {HandlerName} with payload:{Payload}.", nameof(DeleteWopiSessionCommandHandler), JsonConvert.SerializeObject(command));
            
            try
            {
                await _service.DeleteWopiSession(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in {HandlerName}", nameof(DeleteWopiSessionCommandHandler));
                _logger.LogError(ex, "Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
                response.SetError("WopiSession", ex.Message);
            }
            
            _logger.LogInformation("Handled By {HandlerName} with payload:{Payload}.", nameof(DeleteWopiSessionCommandHandler), JsonConvert.SerializeObject(command));
            return response;
        }
    }
} 