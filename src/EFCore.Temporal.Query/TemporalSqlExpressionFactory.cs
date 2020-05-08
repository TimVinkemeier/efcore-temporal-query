using System;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace EntityFrameworkCore.TemporalTables.Query
{
    public class TemporalSqlExpressionFactory : SqlExpressionFactory
    {
        public TemporalSqlExpressionFactory(SqlExpressionFactoryDependencies dependencies) : base(dependencies)
        {
        }

        public override SelectExpression Select(IEntityType entityType)
        {
            if (entityType.FindAnnotation(SqlServerEntityTypeBuilderExtensions.ANNOTATION_TEMPORAL) != null)
            {
                var temporalTableExpression = new TemporalTableExpression(
                    entityType.GetTableName(),
                    entityType.GetSchema(),
                    entityType.GetTableName().ToLower().Substring(0, 1));

                var selectContructor = typeof(SelectExpression).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(IEntityType), typeof(TableExpressionBase) }, null);
                var select = (SelectExpression)selectContructor.Invoke(new object[] { entityType, temporalTableExpression });

                var privateInitializer = typeof(SqlExpressionFactory).GetMethod("AddConditions", BindingFlags.NonPublic | BindingFlags.Instance);
                privateInitializer.Invoke(this, new object[] { select, entityType, null, null });

                return select;
            }

            return base.Select(entityType);
        }
    }
}