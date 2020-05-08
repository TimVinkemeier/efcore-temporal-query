using System.Linq;
using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace EntityFrameworkCore.TemporalTables.Query
{
    /// <summary>
    /// This class is responsible for traversing the Linq query extension methods, searching for any
    /// calls to the "AsOf" or "All" extensions.
    /// When it encounters them, it will process the expressions they are attached to, and find any "AsOfTableExpression" instances,
    /// then set the "AsOfDate" property of those tables.
    /// </summary>
    public class TemporalQueryableMethodTranslatingExpressionVisitor : RelationalQueryableMethodTranslatingExpressionVisitor
    {
        private readonly QueryableMethodTranslatingExpressionVisitorDependencies _dependencies;
        private readonly IModel _model;
        private readonly RelationalQueryableMethodTranslatingExpressionVisitorDependencies _relationalDependencies;
        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        public TemporalQueryableMethodTranslatingExpressionVisitor(
            QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
            RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
            IModel model
            ) : base(dependencies, relationalDependencies, model)
        {
            _dependencies = dependencies;
            _relationalDependencies = relationalDependencies;
            _model = model;

            _sqlExpressionFactory = relationalDependencies.SqlExpressionFactory;
        }

        protected override ShapedQueryExpression TranslateGroupJoin(ShapedQueryExpression outer, ShapedQueryExpression inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector)
        {
            PropagateTemporalSettings(outer, inner);
            return base.TranslateGroupJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector);
        }

        protected override ShapedQueryExpression TranslateJoin(ShapedQueryExpression outer, ShapedQueryExpression inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector)
        {
            PropagateTemporalSettings(outer, inner);
            return base.TranslateJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector);
        }

        protected override ShapedQueryExpression TranslateLeftJoin(ShapedQueryExpression outer, ShapedQueryExpression inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector)
        {
            PropagateTemporalSettings(outer, inner);
            return base.TranslateLeftJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector);
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            var methodInfo = methodCallExpression.Method;

            if (methodInfo.DeclaringType == typeof(SqlServerQueryableExtensions))
            {
                switch (methodInfo.Name)
                {
                    case nameof(SqlServerQueryableExtensions.AsOf):
                        // create an expression....
                        // store expression path
                        var asOfSelectQuery = Visit(methodCallExpression.Arguments[0]);
                        if (asOfSelectQuery is ShapedQueryExpression shapedAsOfQuery)
                        {
                            if (shapedAsOfQuery.QueryExpression is SelectExpression select)
                            {
                                var dateParameter = Visit(methodCallExpression.Arguments[1]) as ParameterExpression;
                                foreach (TemporalTableExpression temporalTable in select.Tables.OfType<TemporalTableExpression>())
                                {
                                    temporalTable.AsOfDate = dateParameter;
                                    temporalTable.TemporalExpressionType = TemporalExpressionType.AsOf;
                                }
                            }
                        }
                        return asOfSelectQuery;

                    case nameof(SqlServerQueryableExtensions.ForSystemTimeAll):
                        var allSelectQuery = Visit(methodCallExpression.Arguments[0]);
                        if (allSelectQuery is ShapedQueryExpression shapedAllQuery)
                        {
                            if (shapedAllQuery.QueryExpression is SelectExpression select)
                            {
                                foreach (TemporalTableExpression temporalTable in select.Tables.OfType<TemporalTableExpression>())
                                {
                                    temporalTable.TemporalExpressionType = TemporalExpressionType.All;
                                }
                            }
                        }
                        return allSelectQuery;
                }
            }

            return base.VisitMethodCall(methodCallExpression);
        }

        // propagates the temporal table expression settings to inner tables
        private void PropagateTemporalSettings(ShapedQueryExpression outer, ShapedQueryExpression inner)
        {
            ParameterExpression dateParameter = null;
            var temporalExpressionType = TemporalExpressionType.None;

            // take temporal settings from outer query
            if (outer.QueryExpression is SelectExpression outerSelect)
            {
                foreach (var table in outerSelect.Tables.OfType<TemporalTableExpression>())
                {
                    temporalExpressionType = table.TemporalExpressionType;
                    dateParameter = table.AsOfDate;
                }
            }

            // and propagate to inner query
            if (inner.QueryExpression is SelectExpression select)
            {
                foreach (var table in select.Tables.OfType<TemporalTableExpression>())
                {
                    table.TemporalExpressionType = temporalExpressionType;
                    if (table.AsOfDate == null)
                    {
                        table.AsOfDate = dateParameter;
                    }
                }
            }
        }
    }
}