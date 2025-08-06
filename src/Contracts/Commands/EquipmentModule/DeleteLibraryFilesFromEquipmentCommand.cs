using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;

public class DeleteLibraryFilesFromEquipmentCommand
{
    public List<string> FileIds { get; set; }
    public string EquipmentId { get; set; }
}