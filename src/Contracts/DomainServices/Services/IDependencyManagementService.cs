using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

public interface IDependencyManagementService
{
    /// <summary>
    /// Handles the cascading deletion of one or more files from Riqs-Pedia and their removal from all associated entities.
    /// </summary>
    /// <param name="fileIds">A collection of unique identifiers of the files to be deleted.</param>
    Task HandleFileDeletionAsync(IEnumerable<string> fileIds);

    /// <summary>
    /// Handles the cascading deletion of one or more guides from Praxis-Process-Guide and their removal from all associated entities.
    /// </summary>
    /// <param name="guideIds">A collection of unique identifiers of the guides to be deleted.</param>
    Task HandleGuideDeletionAsync(IEnumerable<string> guideIds);

    /// <summary>
    /// Handles the cascading deletion of one or more todos from Praxis-Open-Item and their removal from all associated entities.
    /// </summary>
    /// <param name="todoIds">A collection of unique identifiers of the todos to be deleted.</param>
    Task HandleTodoDeletionAsync(IEnumerable<string> todoIds);

    /// <summary>
    /// Handles the cascading deletion of one or more praxisForms from Praxis-Form and their removal from all associated entities.
    /// </summary>
    /// <param name="praxisFormIds">A collection of unique identifiers of the praxisForms to be deleted.</param>
    Task HandlePraxisFormDeletionAsync(IEnumerable<string> praxisFormIds);

    /// <summary>
    /// Handles the inactivation of one or more files from Riqs-Pedia and updates their dependencies in all associated entities.
    /// </summary>
    /// <param name="fileIds">A collection of unique identifiers of the files to be inactivated.</param>
    Task HandleFileInactivationAsync(IEnumerable<string> fileIds);

    /// <summary>
    /// Handles the inactivation of one or more guides from Praxis-Process-Guide and updates their dependencies in all associated entities.
    /// </summary>
    /// <param name="guideIds">A collection of unique identifiers of the guides to be inactivated.</param>
    Task HandleGuideInactivationAsync(IEnumerable<string> guideIds);

    /// <summary>
    /// Handles the inactivation of one or more todos from Praxis-Open-Item and updates their dependencies in all associated entities.
    /// </summary>
    /// <param name="todoIds">A collection of unique identifiers of the todos to be inactivated.</param>
    Task HandleTodoInactivationAsync(IEnumerable<string> todoIds);
}


