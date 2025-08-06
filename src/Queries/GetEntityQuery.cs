namespace Selise.Ecap.SC.PraxisMonitor.Queries
{
    public class GetEntityQuery
    {
        public string EntityName {get; set;} 
        public string Filter {get; set;} 
        public string Sort  {get; set;} 
        public int? PageNumber {get; set;} 
        public int? PageSize {get; set;}
        public bool? ShowDeleted { get; set; }
    }
}