namespace EventHandlers.Models
{
   public class GetFilteredSqlQueryData
    {
        public string Text { get; set; }
        public bool? IsRoot { get; set; }
        public string EntityName { get; set; }
        public string OrderBy { get; set; }
        public int? PageLimit { get; set; }
        public int? PageNumber { get; set; }
        public SortOrder? SortOrder { get; set; }
        public bool FromGetFilteredComplex { get; set; }
        public bool FetchAllMatchedItem { get; set; }
        public string Key { get; set; }
        public string[] ConnectionTags { get; set; }
        public string ConnectionsAccessKey { get; set; }

        public bool SolveConnectionForEntity { get; set; } = false;
        public bool IsParentEntityOfConnection { get; set; } = false;
        public bool ExpandParent { get; set; } = false;
        public bool ExpandChild { get; set; } = false;
    }
    public enum SortOrder : byte
    {
        Ascending = 0,
        Descending = 1
    }
}
