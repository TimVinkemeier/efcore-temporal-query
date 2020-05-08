using System.Diagnostics.CodeAnalysis;

using EntityFrameworkCore.TemporalTables.Query;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query;

namespace Microsoft.EntityFrameworkCore
{
    public static class SqlServerEntityTypeBuilderExtensions
    {
        public static string ANNOTATION_TEMPORAL = "IS_TEMPORAL_TABLE";

        public static DbContextOptionsBuilder EnableTemporalTableQueries([NotNull] this DbContextOptionsBuilder optionsBuilder)
        {
            // If service provision is NOT being performed internally, we cannot replace services.
            var coreOptions = optionsBuilder.Options.GetExtension<CoreOptionsExtension>();
            if (coreOptions.InternalServiceProvider == null)
            {
                return optionsBuilder
                    // replace the service responsible for generating SQL strings
                    .ReplaceService<IQuerySqlGeneratorFactory, TemporalQuerySqlGeneratorFactory>()
                    // replace the service responsible for traversing the Linq AST (a.k.a Query Methods)
                    .ReplaceService<IQueryableMethodTranslatingExpressionVisitorFactory, TemporalQueryableMethodTranslatingExpressionVisitorFactory>()
                    // replace the service responsible for providing instances of SqlExpressions
                    .ReplaceService<ISqlExpressionFactory, TemporalSqlExpressionFactory>();
            }
            else
                return optionsBuilder;
        }

        /// <summary>
        /// Sets the required metadata to track that this entity type uses a temporal table.
        /// </summary>
        /// <param name="entity"></param>
        public static EntityTypeBuilder HasTemporalTable(this EntityTypeBuilder entity)
        {
            entity.Metadata.SetAnnotation(ANNOTATION_TEMPORAL, true);
            return entity;
        }
    }
}