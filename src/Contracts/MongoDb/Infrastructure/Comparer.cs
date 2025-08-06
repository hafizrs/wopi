using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.Wopi.Contracts.MongoDb.Infrastructure
{
    public class Comparer<T> : IComparer<T>
    {
        private readonly Comparison<T> comparison;

        public Comparer(Comparison<T> comparison)
        {
            if (comparison == null)
            {
                throw new ArgumentNullException("comparison");
            }

            this.comparison = comparison;
        }

        public int Compare(T x, T y)
        {
            return comparison(x, y);
        }
    }
}
