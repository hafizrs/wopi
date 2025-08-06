namespace Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos
{
    public class ConnectionQuery
    {
        public string ItemId { get; set; }
        public string ParentEntityName { get; set; }
        public string ParentEntityId { get; set; }
        public string[] ParentEntityIds { get; set; }
        public string ChildEntityName { get; set; }
        public string ChildEntityId { get; set; }
        public string[] ChildEntityIds { get; set; }
        public string[] Tags { get; set; }
        public string UserId { get; set; }
        public string[] Roles { get; set; }
        public string[] EmbededInfo { get; set; }
        public bool ReadConnection { get; set; }
        public bool ReadPrant { get; set; }
        public bool ReadChild { get; set; }
    }
}
