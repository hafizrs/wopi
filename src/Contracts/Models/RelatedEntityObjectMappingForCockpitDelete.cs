namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

public class RelatedEntityObjectMappingForCockpitDelete
{
    public string RelatedEntityName { get; set; }
    public EntityObjectModel RelatedEntityObject { get; set; }
}

public class EntityObjectModel
{
    public string ItemId { get; set; }
}