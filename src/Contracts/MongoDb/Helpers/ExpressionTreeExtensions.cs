using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Helpers
{
    public static class ExpressionTreeExtensions
    {
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var invokedExpr2 = Expression.Invoke(expr2, expr1.Parameters);
            var body = Expression.AndAlso(expr1.Body, invokedExpr2);
            return Expression.Lambda<Func<T, bool>>(body, expr1.Parameters);
        }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var invokedExpr2 = Expression.Invoke(expr2, expr1.Parameters);
            var body = Expression.OrElse(expr1.Body, invokedExpr2);
            return Expression.Lambda<Func<T, bool>>(body, expr1.Parameters);
        }

        public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expr)
        {
            var body = Expression.Not(expr.Body);
            return Expression.Lambda<Func<T, bool>>(body, expr.Parameters);
        }
    }
}
