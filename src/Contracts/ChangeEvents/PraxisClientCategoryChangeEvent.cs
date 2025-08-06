using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.ChangeEvents
{
    public class PraxisClientCategoryChangeEvent
    {
        public string ItemId { get; set; }
        public string Name { get; set; }
        public List<SubCategoryModel> Subcategories { get; set; }
    }

    public class SubCategoryModel
    {
        public string ItemId { get; set; }
        public string Name { get; set; }
    }
}
