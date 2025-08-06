using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.Wopi.Contracts.Commands;
using Selise.Ecap.SC.Wopi.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.DomainServices.WopiModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.Wopi.CommandHandlers.WopiModule
{
    public class LockWopiFileCommandHandler : ICommandHandler<LockWopiFileCommand, CommandResponse>
    {
        private readonly IWopiService _service;
        private readonly ILogger<LockWopiFileCommandHandler> _logger;
        
        public LockWopiFileCommandHandler(IWopiService service, ILogger<LockWopiFileCommandHandler> logger)
        {
            _service = service;
            _logger = logger;
        }
        
        public CommandResponse Handle(LockWopiFileCommand command)
        {
            throw new NotImplementedException();
        }
        
        public async Task<CommandResponse> HandleAsync(LockWopiFileCommand command)
        {
            var response = new CommandResponse();
            _logger.LogInformation("Enter {HandlerName} with payload:{Payload}.", nameof(LockWopiFileCommandHandler), JsonConvert.SerializeObject(command));
            
            try
            {
                await _service.LockWopiFile(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in {HandlerName}", nameof(LockWopiFileCommandHandler));
                _logger.LogError(ex, "Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
                response.SetError("WopiFile", ex.Message);
            }
            
            _logger.LogInformation("Handled By {HandlerName} with payload:{Payload}.", nameof(LockWopiFileCommandHandler), JsonConvert.SerializeObject(command));
            return response;
        }
    }
} 