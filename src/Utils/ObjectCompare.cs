using System;
using System.Collections.Generic;
using ObjectsComparer;
using System.Linq;

namespace Selise.Ecap.SC.Wopi.Utils
{
    public class ObjectCompare
    {
        protected ObjectCompare() { }
        public static List<Difference> GetDifferencesByExpectedProperty<T>(T lastChange,
            T secLastChange)
        {
            try
            {
                var comparer = new ObjectsComparer.Comparer<T>();
                var isValid = comparer.Compare(lastChange, secLastChange, out IEnumerable<Difference> differences);

                if (!isValid) return differences.ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return new List<Difference>();
        }
    }
}
