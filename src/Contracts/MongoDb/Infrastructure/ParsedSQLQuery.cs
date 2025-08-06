using Selise.Ecap.SC.Wopi.Contracts.MongoDb.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace Selise.Ecap.SC.Wopi.Contracts.MongoDb.Infrastructure
{
    public class ParsedSQLQuery
    {
        public ParsedSQLFilter[] DataFilters { get; set; }
        public DataFilter[] RowLevelSecurityDataFilters { get; set; }
        public bool OnlyCount { get; set; }
        public string EntityName { get; set; }
        public string OrderBy { get; set; }
        public SortObjects[] SortBy { get; set; }
        public int? PageLimit { get; set; }
        public int? PageNumber { get; set; }
        public SortOrder? SortOrder { get; set; }
        public string[] Fields { get; set; }
        public bool IsRoot { get; set; }
        public string Shuffler { get; set; }
        public int? Skip { get; set; }
        public ParsedSQLQuery()
        {
            DataFilters = new ParsedSQLFilter[0];
            PageLimit = 20;
            PageNumber = 0;
            OnlyCount = false;
        }
        public void AddRowLevelSecurityParameters(string userId, string[] roles)
        {
            RowLevelSecurityDataFilters = AddRowLevelSecurityDataFilter(userId, roles).ToArray();
        }
        private DataFilter[] AddRowLevelSecurityDataFilter(string userId, string[] roles)
        {
            List<DataFilter> list = new List<DataFilter>
            {
                new DataFilter
                {
                    PropertyName = "IdsAllowedToRead",
                    Value = userId
                }
            };
            list.AddRange(roles.Select((string role) => new DataFilter
            {
                PropertyName = "RolesAllowedToRead",
                Value = role
            }));
            return list.ToArray();
        }
    }
}
