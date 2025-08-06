using System;
using System.Linq.Expressions;

namespace Selise.Ecap.SC.Wopi.Utils
{
    public static class ExpressionBuilder
    {
        public static Expression<Func<T, bool>> AndAlso<T>(Expression<Func<T, bool>> e1, Expression<Func<T, bool>> e2)
        {
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(
                new SwapVisitor(e1.Parameters[0], e2.Parameters[0]).Visit(e1.Body)!,
                e2.Body), e2.Parameters);
        }
        
        public static Expression<Func<T, bool>> OrElse<T>(Expression<Func<T, bool>> e1, Expression<Func<T, bool>> e2)
        {
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(
                new SwapVisitor(e1.Parameters[0], e2.Parameters[0]).Visit(e1.Body)!,
                e2.Body), e2.Parameters);
        }
        
    }
}