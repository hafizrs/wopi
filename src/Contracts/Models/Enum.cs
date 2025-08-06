namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class Enum
    {
        public enum RepeatTypeEnums
        {
            None = 0,
            Daily = 1,
            Weekly = 2,
            Monthly = 3,
            Yearly = 4
        }

        public enum WeekDaysEnums // or use System.DayOfWeek
        {
            Sunday = 0,
            Monday = 1,
            Tuesday = 2,
            Wednesday = 3,
            Thursday = 4,
            Friday = 5,
            Saturday = 6
        }

        public enum SortMonthEnums
        {
            NotSet = 0,
            Jan = 1,
            Feb = 2,
            Mar = 3,
            Apr = 4,
            May = 5,
            Jun = 6,
            Jul = 7,
            Aug = 8,
            Sept = 9,
            Oct = 10,
            Nov = 11,
            Dec = 12
        }

        public enum SubscriptionInvoiceType
        {
            NewOrRenew = 1,
            Update = 2,
            DuePayment = 3
        }
    }
}
