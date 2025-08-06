using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.Wopi.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.DomainServices.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.EntityResponse;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.Wopi.CommandHandlers.WopiModule
{
    public class CreateWopiSessionCommandHandler : ICommandHandler<CreateWopiSessionCommand, CreateWopiSessionResponse>
    {
        private readonly IWopiService _service;
        private readonly ILogger<CreateWopiSessionCommandHandler> _logger;
        
        public CreateWopiSessionCommandHandler(IWopiService service, ILogger<CreateWopiSessionCommandHandler> logger)
        {
            _service = service;
            _logger = logger;
        }
        
        public CreateWopiSessionResponse Handle(CreateWopiSessionCommand command)
        {
            throw new NotImplementedException();
        }
        
        public async Task<CreateWopiSessionResponse> HandleAsync(CreateWopiSessionCommand command)
        {
            var response = new CommandResponse();
            _logger.LogInformation("Enter {HandlerName} with payload:{Payload}.", nameof(CreateWopiSessionCommandHandler), JsonConvert.SerializeObject(command));
            
            try
            {
                var result = await _service.CreateWopiSession(command);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in {HandlerName}", nameof(CreateWopiSessionCommandHandler));
                _logger.LogError(ex, "Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
                response.SetError("WopiSession", ex.Message);
            }
            
            _logger.LogInformation("Handled By {HandlerName} with payload:{Payload}.", nameof(CreateWopiSessionCommandHandler), JsonConvert.SerializeObject(command));
            return null;
        }
    }
} 