using Microsoft.Extensions.DependencyInjection;
using Selise.Ecap.SC.WopiMonitor.Contracts.Infrastructure;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.WopiMonitor.ValidationHandlers
{
    public class CommandHandler : ICommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public CommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<CommandResponse> SubmitAsync<TCommand, TResponse>(TCommand command) where TCommand : class
        {
            var commandHandlerType = typeof(ICommandHandler<TCommand, TResponse>);
            var commandHandler = _serviceProvider.GetService(commandHandlerType) as ICommandHandler<TCommand, TResponse>;

            return await commandHandler?.HandleAsync(command) ?? Task.FromResult(new CommandResponse());
        }
    }
} 