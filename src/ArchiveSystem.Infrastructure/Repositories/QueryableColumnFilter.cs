using System.Linq.Expressions;

namespace ArchiveSystem.Infrastructure.Repositories
{
    internal static class QueryableColumnFilter
    {
        public static IQueryable<T> ApplyString<T>(
            IQueryable<T> query,
            Expression<Func<T, string?>> selector,
            string? filterOperator,
            string value)
        {
            var parameter = selector.Parameters[0];
            var text = Expression.Coalesce(selector.Body, Expression.Constant(string.Empty));
            var constant = Expression.Constant(value);
            Expression body = filterOperator switch
            {
                "equals" => Expression.Equal(text, constant),
                "doesNotEqual" => Expression.NotEqual(text, constant),
                "startsWith" => Expression.Call(text, nameof(string.StartsWith), Type.EmptyTypes, constant),
                "endsWith" => Expression.Call(text, nameof(string.EndsWith), Type.EmptyTypes, constant),
                "doesNotContain" => Expression.Not(Expression.Call(text, nameof(string.Contains), Type.EmptyTypes, constant)),
                _ => Expression.Call(text, nameof(string.Contains), Type.EmptyTypes, constant)
            };

            return query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
        }
    }
}
