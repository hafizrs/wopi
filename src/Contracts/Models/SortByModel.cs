
namespace Selise.Ecap.SC.Wopi.Contracts.Models
{
    public class SortByModel
    {
        public string PropertyName { get; set; }
        public SortDirectionEnum Direction { get; set; }
    }


    public enum SortDirectionEnum
    {
        Ascending = 1,
        Descending = -1
    }
}