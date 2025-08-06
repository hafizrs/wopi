using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

public interface IDeleteTaskScheduleDataStrategy
{
    Task<bool> DeleteTask(List<string> itemIds, TaskScheduleRemoveType removeType);
}