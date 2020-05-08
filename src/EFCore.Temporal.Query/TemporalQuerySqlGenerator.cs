using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.TemporalTables.Query
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
    public class TemporalQuerySqlGenerator : SqlServerQuerySqlGenerator
    {
        private const string TEMPORAL_PARAMETER_PREFIX = "__ef_temporal";

        private readonly RelationalQueryContext _ctx;
        private IRelationalCommandBuilder _commandbuilder;
        private ISqlGenerationHelper _sqlGenerationHelper;

        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
        public TemporalQuerySqlGenerator(
            QuerySqlGeneratorDependencies dependencies,
            QuerySqlGenerator inner,
            RelationalQueryContext ctx)
            : base(new QuerySqlGeneratorDependencies(
                new SingletonRelationalCommandBuilderFactory(dependencies.RelationalCommandBuilderFactory), dependencies.SqlGenerationHelper))
        {
            _sqlGenerationHelper = dependencies.SqlGenerationHelper;
            _ctx = ctx;
            _commandbuilder = this.Dependencies.RelationalCommandBuilderFactory.Create();
        }

        protected virtual Expression VisitAllTable(TemporalTableExpression tableExpression)
        {
            // This method was modeled on "SqlServerQuerySqlGenerator.VisitTable".
            // Where we deviate, is after printing the table name, we apply the temporal constraints
            Sql.Append(_sqlGenerationHelper.DelimitIdentifier(tableExpression.Name, tableExpression.Schema))
                .Append(" FOR SYSTEM_TIME ALL")
                .Append(AliasSeparator)
                .Append(_sqlGenerationHelper.DelimitIdentifier(tableExpression.Alias));

            return tableExpression;
        }

        protected virtual Expression VisitAsOfTable(TemporalTableExpression tableExpression)
        {
            // This method was modeled on "SqlServerQuerySqlGenerator.VisitTable".
            // Where we deviate, is after printing the table name, we check if temporal constraints
            // need to be applied.

            Sql.Append(_sqlGenerationHelper.DelimitIdentifier(tableExpression.Name, tableExpression.Schema));

            if (tableExpression.AsOfDate != null)
            {
                var name = TEMPORAL_PARAMETER_PREFIX + tableExpression.AsOfDate.Name;
                Sql.Append($" FOR SYSTEM_TIME AS OF @{name}"); //2020-02-28T11:00:00

                if (!_commandbuilder.Parameters.Any(x => x.InvariantName == tableExpression.AsOfDate.Name))
                    _commandbuilder.AddParameter(tableExpression.AsOfDate.Name, name);
            }

            Sql
                .Append(AliasSeparator)
                .Append(_sqlGenerationHelper.DelimitIdentifier(tableExpression.Alias));

            return tableExpression;
        }

        protected override Expression VisitExtension(Expression extensionExpression)
        {
            switch (extensionExpression)
            {
                case TemporalTableExpression temporalTableExpression:
                    if (temporalTableExpression.TemporalExpressionType == TemporalExpressionType.AsOf)
                    {
                        return VisitAsOfTable(temporalTableExpression);
                    }
                    else if (temporalTableExpression.TemporalExpressionType == TemporalExpressionType.All)
                    {
                        return VisitAllTable(temporalTableExpression);
                    }
                    else if (temporalTableExpression.TemporalExpressionType == TemporalExpressionType.None)
                    {
                        return base.VisitExtension(extensionExpression);
                    }
                    else
                    {
                        throw new ArgumentException($"Unsupported temporal table expression type '{temporalTableExpression.TemporalExpressionType}'.");
                    }
            }

            return base.VisitExtension(extensionExpression);
        }
    }
}