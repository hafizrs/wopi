using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.Wopi.CommandHandlers
{
    public abstract class AbstractCommandHandler<TCommand> : ICommandHandler<TCommand, CommandResponse>
    {
        public CommandResponse Handle(TCommand command)
        {
            return HandleAsync(command).Result;
        }

        public abstract Task<CommandResponse> HandleAsync(TCommand command);
    }
}
