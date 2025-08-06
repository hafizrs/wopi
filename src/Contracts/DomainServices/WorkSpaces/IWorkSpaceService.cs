using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.WorkSpaces
{
    public interface IWorkSpaceService
    {
        void CreateUserWorkSpace(CreateUserWorkspaceCommand createUserWrokspaceCommand);
    }
}


