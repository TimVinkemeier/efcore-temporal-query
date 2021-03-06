﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Microsoft.EntityFrameworkCore
{
    public static class SqlServerQueryableExtensions
    {
        public static readonly MethodInfo AllMethodInfo
          = typeof(SqlServerQueryableExtensions).GetTypeInfo().GetDeclaredMethod(nameof(ForSystemTimeAll));

        public static readonly MethodInfo AsOfMethodInfo
                  = typeof(SqlServerQueryableExtensions).GetTypeInfo().GetDeclaredMethod(nameof(AsOf));

        /// <summary>
        /// Configure a query to constrain all temporal tables to a specific time
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="source"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static IQueryable<TEntity> AsOf<TEntity>(this IQueryable<TEntity> source, DateTime date) where TEntity : class
        {
            return
              source.Provider is EntityQueryProvider
                ? source.Provider.CreateQuery<TEntity>(
                  Expression.Call(
                    instance: null,
                    method: AsOfMethodInfo.MakeGenericMethod(typeof(TEntity)),
                    arg0: source.Expression,
                    arg1: Expression.Constant(date)))
                : source;
        }

        /// <summary>
        /// Configure a query to return all temporal tables entries
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="source"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static IQueryable<TEntity> ForSystemTimeAll<TEntity>(this IQueryable<TEntity> source) where TEntity : class
        {
            return
              source.Provider is EntityQueryProvider
                ? source.Provider.CreateQuery<TEntity>(
                  Expression.Call(AllMethodInfo.MakeGenericMethod(typeof(TEntity)), source.Expression))
                : source;
        }
    }
}