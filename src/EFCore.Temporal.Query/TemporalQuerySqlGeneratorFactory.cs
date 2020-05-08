using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;

namespace EntityFrameworkCore.TemporalTables.Query
{
    public class TemporalQuerySqlGeneratorFactory : IQuerySqlGeneratorFactory
    {
        private readonly QuerySqlGeneratorDependencies _dependencies;
        private readonly QueryCompilationContext _queryCompilationContext;
        private readonly ISqlServerOptions _sqlServerOptions;
        private readonly IQueryableMethodTranslatingExpressionVisitorFactory _ss;

        public TemporalQuerySqlGeneratorFactory(
            [NotNull] QuerySqlGeneratorDependencies dependencies,
            [NotNull] ISqlServerOptions sqlServerOptions)
        {
            _dependencies = dependencies;
            _sqlServerOptions = sqlServerOptions;
        }

        public QuerySqlGenerator Create()
        {
            return new TemporalQuerySqlGenerator(_dependencies, null, null);
        }
    }
}