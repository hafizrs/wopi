
using SeliseBlocks.Genesis.Framework.Bus.Contracts.Command;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class CreateUserWorkspaceCommand : BlocksCommand
    {
        // Owner Id can be both UserId or ProposedUserId
        public string OwnerId { get; set; }
        public long TotalStorageSpace { get; set; }
    }
}