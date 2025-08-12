using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.Wopi.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.DomainServices.WopiModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.Wopi.CommandHandlers.WopiModule
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
                if (command.SessionIds == null || command.SessionIds.Length == 0)
                {
                    response.SetError("WopiSession", "No session IDs provided for deletion");
                    return response;
                }

                await _service.DeleteWopiSession(command);
                _logger.LogInformation("Successfully processed deletion request for {Count} sessions", command.SessionIds.Length);
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