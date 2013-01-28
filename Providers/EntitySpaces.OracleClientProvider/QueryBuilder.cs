/*  New BSD License
-------------------------------------------------------------------------------
Copyright (c) 2006-2012, EntitySpaces, LLC
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the EntitySpaces, LLC nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL EntitySpaces, LLC BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
-------------------------------------------------------------------------------
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;

using Tiraggo.DynamicQuery;
using Tiraggo.Interfaces;

namespace Tiraggo.OracleClientProvider
{
    class QueryBuilder
    {
        public static OracleCommand PrepareCommand(esDataRequest request)
        {
            StandardProviderParameters std = new StandardProviderParameters();
            std.cmd = new OracleCommand();
            std.pindex = NextParamIndex(std.cmd);
            std.request = request;

            string sql = BuildQuery(std, request.DynamicQuery);

            std.cmd.CommandText = sql;
            return (OracleCommand)std.cmd;
        }

        protected static string BuildQuery(StandardProviderParameters std, tgDynamicQuerySerializable query)
        {
            IDynamicQuerySerializableInternal iQuery = query as IDynamicQuerySerializableInternal;

            bool selectAll = (iQuery.InternalSelectColumns == null && !query.es.CountAll);

            bool paging = false;

            if (query.es.PageNumber.HasValue && query.es.PageSize.HasValue)
                paging = true;

            string select = GetSelectStatement(std, query);
            string from = GetFromStatement(std, query);
            string join = GetJoinStatement(std, query);
            string where = GetComparisonStatement(std, query, iQuery.InternalWhereItems, " WHERE ");
            string groupBy = GetGroupByStatement(std, query);
            string having = GetComparisonStatement(std, query, iQuery.InternalHavingItems, " HAVING ");
            string orderBy = GetOrderByStatement(std, query);
            string setOperation = GetSetOperationStatement(std, query);

            string sql = String.Empty;

            if (paging)
            {
                int begRow = ((query.es.PageNumber.Value - 1) * query.es.PageSize.Value) + 1;
                int endRow = begRow + (query.es.PageSize.Value - 1);
                // The WITH statement
                sql += "WITH \"withStatement\" AS (";
                if (selectAll)
                {
                    sql += "SELECT " +
                        Delimiters.TableOpen + query.es.QuerySource + Delimiters.TableClose
                        + ".*, ROW_NUMBER() OVER(" + orderBy + ") AS ESRN ";
                }
                else
                {
                    sql += "SELECT " + select + ", ROW_NUMBER() OVER(" + orderBy + ") AS ESRN ";
                }
                sql += "FROM " + from + join + where + groupBy + ") ";
                // The actual select
                if (selectAll || join.Length > 0 || groupBy.Length > 0 || query.es.Distinct)
                {
                    sql += "SELECT " +
                        Delimiters.TableOpen + "withStatement" + Delimiters.TableClose
                        + ".* FROM \"withStatement\" ";
                }
                else
                {
                    sql += "SELECT " + select + " FROM \"withStatement\" ";
                }
                sql += "WHERE ESRN BETWEEN " + begRow + " AND " + endRow;
                sql += " ORDER BY ESRN ASC";
            }
            else
            {
                sql += "SELECT " + select + " FROM " + from + join + where + setOperation + groupBy + having + orderBy;
            }

            return sql;
        }

        protected static string GetFromStatement(StandardProviderParameters std, tgDynamicQuerySerializable query)
        {
            IDynamicQuerySerializableInternal iQuery = query as IDynamicQuerySerializableInternal;

            string sql = String.Empty;

            if (iQuery.InternalFromQuery == null)
            {
                sql = Shared.CreateFullName(std.request, query);

                if (iQuery.JoinAlias != " ")
                {
                    sql += " " + iQuery.JoinAlias;
                }
            }
            else
            {
                IDynamicQuerySerializableInternal iSubQuery = iQuery.InternalFromQuery as IDynamicQuerySerializableInternal;

                iSubQuery.IsInSubQuery = true;

                sql += "(";
                sql += BuildQuery(std, iQuery.InternalFromQuery);
                sql += ")";

                if (iSubQuery.SubQueryAlias != " ")
                {
                    sql += " " + iSubQuery.SubQueryAlias;
                }

                iSubQuery.IsInSubQuery = false;
            }

            return sql;
        }

        protected static string GetSelectStatement(StandardProviderParameters std, tgDynamicQuerySerializable query)
        {
            string sql = String.Empty;
            string comma = String.Empty;
            bool selectAll = true;

            IDynamicQuerySerializableInternal iQuery = query as IDynamicQuerySerializableInternal;

            if (query.es.Distinct) sql += " DISTINCT ";

            if (iQuery.InternalSelectColumns != null)
            {
                selectAll = false;

                foreach (tgExpression expressionItem in iQuery.InternalSelectColumns)
                {
                    if (expressionItem.Query != null)
                    {
                        IDynamicQuerySerializableInternal iSubQuery = expressionItem.Query as IDynamicQuerySerializableInternal;

                        sql += comma;

                        if (iSubQuery.SubQueryAlias == string.Empty)
                        {
                            sql += iSubQuery.JoinAlias + ".*";
                        }
                        else
                        {
                            iSubQuery.IsInSubQuery = true;
                            sql += " (" + BuildQuery(std, expressionItem.Query as tgDynamicQuerySerializable) + ") AS " + Delimiters.StringOpen + iSubQuery.SubQueryAlias + Delimiters.StringClose;
                            iSubQuery.IsInSubQuery = false;
                        }

                        comma = ",";
                    }
                    else
                    {
                        sql += comma;

                        string columnName = expressionItem.Column.Name;

                        if (columnName != null && columnName[0] == '<')
                            sql += columnName.Substring(1, columnName.Length - 2);
                        else
                            sql += GetExpressionColumn(std, query, expressionItem, false, true);

                        comma = ",";
                    }
                }
                sql += " ";
            }

            if (query.es.CountAll)
            {
                selectAll = false;

                sql += comma;
                sql += "COUNT(*)";

                if (query.es.CountAllAlias != null)
                {
                    // Need DBMS string delimiter here
                    sql += " AS " + Delimiters.StringOpen + query.es.CountAllAlias + Delimiters.StringClose;
                }
            }

            if (selectAll)
            {
                sql += "*";
            }

            return sql;
        }

        protected static string GetJoinStatement(StandardProviderParameters std, tgDynamicQuerySerializable query)
        {
            string sql = String.Empty;

            IDynamicQuerySerializableInternal iQuery = query as IDynamicQuerySerializableInternal;

            if (iQuery.InternalJoinItems != null)
            {
                foreach (tgJoinItem joinItem in iQuery.InternalJoinItems)
                {
                    tgJoinItem.esJoinItemData joinData = (tgJoinItem.esJoinItemData)joinItem;

                    switch (joinData.JoinType)
                    {
                        case esJoinType.InnerJoin:
                            sql += " INNER JOIN ";
                            break;
                        case esJoinType.LeftJoin:
                            sql += " LEFT JOIN ";
                            break;
                        case esJoinType.RightJoin:
                            sql += " RIGHT JOIN ";
                            break;
                        case esJoinType.FullJoin:
                            sql += " FULL JOIN ";
                            break;
                    }

                    IDynamicQuerySerializableInternal iSubQuery = joinData.Query as IDynamicQuerySerializableInternal;

                    sql += Shared.CreateFullName(std.request, joinData.Query);

                    sql += " " + iSubQuery.JoinAlias + " ON ";

                    sql += GetComparisonStatement(std, query, joinData.WhereItems, String.Empty);
                }
            }

            return sql;
        }

        protected static string GetComparisonStatement(StandardProviderParameters std, tgDynamicQuerySerializable query, List<tgComparison> items, string prefix)
        {
            string sql = String.Empty;
            string comma = String.Empty;
            bool first = true;

            IDynamicQuerySerializableInternal iQuery = query as IDynamicQuerySerializableInternal;

            //=======================================
            // WHERE
            //=======================================
            if (items != null)
            {
                sql += prefix;

                string compareTo = String.Empty;
                foreach (tgComparison comparisonItem in items)
                {
                    tgComparison.esComparisonData comparisonData = (tgComparison.esComparisonData)comparisonItem;
                    tgDynamicQuerySerializable subQuery = null;

                    bool requiresParam = true;
                    bool needsStringParameter = false;
                   std.needsStringParameter = false;

                    if (comparisonData.IsParenthesis)
                    {
                        if (comparisonData.Parenthesis == esParenthesis.Open)
                            sql += "(";
                        else
                            sql += ")";

                        continue;
                    }

                    if (comparisonData.IsConjunction)
                    {
                        switch (comparisonData.Conjunction)
                        {
                            case esConjunction.And: sql += " AND "; break;
                            case esConjunction.Or: sql += " OR "; break;
                            case esConjunction.AndNot: sql += " AND NOT "; break;
                            case esConjunction.OrNot: sql += " OR NOT "; break;
                        }
                        continue;
                    }

                    Dictionary<string, OracleParameter> types = null;
                    if (comparisonData.Column.Query != null)
                    {
                        IDynamicQuerySerializableInternal iLocalQuery = comparisonData.Column.Query as IDynamicQuerySerializableInternal;
                        types = Cache.GetParameters(iLocalQuery.DataID, (esProviderSpecificMetadata)iLocalQuery.ProviderMetadata, (esColumnMetadataCollection)iLocalQuery.Columns);
                    }

                    if (comparisonData.IsLiteral)
                    {
                        if (comparisonData.Column.Name[0] == '<')
                        {
                            sql += comparisonData.Column.Name.Substring(1, comparisonData.Column.Name.Length - 2);
                        }
                        else
                        {
                            sql += comparisonData.Column.Name;
                        }
                        continue;
                    }

                    if (comparisonData.ComparisonColumn.Name == null)
                    {
                        subQuery = comparisonData.Value as tgDynamicQuerySerializable;

                        if (subQuery == null)
                        {
                            if (comparisonData.Column.Name != null)
                            {
                                IDynamicQuerySerializableInternal iColQuery = comparisonData.Column.Query as IDynamicQuerySerializableInternal;
                                esColumnMetadataCollection columns = (esColumnMetadataCollection)iColQuery.Columns;
                                compareTo = Delimiters.Param + columns[comparisonData.Column.Name].PropertyName + (++std.pindex).ToString();
                            }
                            else
                            {
                                compareTo = Delimiters.Param + "Expr" + (++std.pindex).ToString();
                            }
                        }
                        else
                        {
                            // It's a sub query
                            compareTo = GetSubquerySearchCondition(subQuery) + " (" + BuildQuery(std, subQuery) + ") ";
                            requiresParam = false;
                        }
                    }
                    else
                    {
                        compareTo = GetColumnName(comparisonData.ComparisonColumn);
                        requiresParam = false;
                    }

                    switch (comparisonData.Operand)
                    {
                        case esComparisonOperand.Exists:
                            sql += " EXISTS" + compareTo;
                            break;
                        case esComparisonOperand.NotExists:
                            sql += " NOT EXISTS" + compareTo;
                            break;

                        //-----------------------------------------------------------
                        // Comparison operators, left side vs right side
                        //-----------------------------------------------------------
                        case esComparisonOperand.Equal:
                            if (comparisonData.ItemFirst)
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " = " + compareTo;
                            else
                                sql += compareTo + " = " + ApplyWhereSubOperations(std, query, comparisonData);
                            break;
                        case esComparisonOperand.NotEqual:
                            if (comparisonData.ItemFirst)
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " <> " + compareTo;
                            else
                                sql += compareTo + " <> " + ApplyWhereSubOperations(std, query, comparisonData);
                            break;
                        case esComparisonOperand.GreaterThan:
                            if (comparisonData.ItemFirst)
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " > " + compareTo;
                            else
                                sql += compareTo + " > " + ApplyWhereSubOperations(std, query, comparisonData);
                            break;
                        case esComparisonOperand.LessThan:
                            if (comparisonData.ItemFirst)
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " < " + compareTo;
                            else
                                sql += compareTo + " < " + ApplyWhereSubOperations(std, query, comparisonData);
                            break;
                        case esComparisonOperand.LessThanOrEqual:
                            if (comparisonData.ItemFirst)
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " <= " + compareTo;
                            else
                                sql += compareTo + " <= " + ApplyWhereSubOperations(std, query, comparisonData);
                            break;
                        case esComparisonOperand.GreaterThanOrEqual:
                            if (comparisonData.ItemFirst)
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " >= " + compareTo;
                            else
                                sql += compareTo + " >= " + ApplyWhereSubOperations(std, query, comparisonData);
                            break;

                        case esComparisonOperand.Like:
                            string esc = comparisonData.LikeEscape.ToString();
                            if (String.IsNullOrEmpty(esc) || esc == "\0")
                            {
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " LIKE " + compareTo;
                                needsStringParameter = true;
                            }
                            else
                            {
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " LIKE " + compareTo;
                                sql += " ESCAPE '" + esc + "'";
                                needsStringParameter = true;
                            }
                            break;
                        case esComparisonOperand.NotLike:
                            esc = comparisonData.LikeEscape.ToString();
                            if (String.IsNullOrEmpty(esc) || esc == "\0")
                            {
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " NOT LIKE " + compareTo;
                                needsStringParameter = true;
                            }
                            else
                            {
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " NOT LIKE " + compareTo;
                                sql += " ESCAPE '" + esc + "'";
                                needsStringParameter = true;
                            }
                            break;
                        case esComparisonOperand.Contains:
                            sql += " CONTAINS(" + GetColumnName(comparisonData.Column) +
                                ", " + compareTo + ")";
                            needsStringParameter = true;
                            break;
                        case esComparisonOperand.IsNull:
                            sql += ApplyWhereSubOperations(std, query, comparisonData) + " IS NULL";
                            requiresParam = false;
                            break;
                        case esComparisonOperand.IsNotNull:
                            sql += ApplyWhereSubOperations(std, query, comparisonData) + " IS NOT NULL";
                            requiresParam = false;
                            break;
                        case esComparisonOperand.In:
                        case esComparisonOperand.NotIn:
                            {
                                if (subQuery != null)
                                {
                                    // They used a subquery for In or Not 
                                    sql += ApplyWhereSubOperations(std, query, comparisonData);
                                    sql += (comparisonData.Operand == esComparisonOperand.In) ? " IN" : " NOT IN";
                                    sql += compareTo;
                                }
                                else
                                {
                                    comma = String.Empty;
                                    if (comparisonData.Operand == esComparisonOperand.In)
                                    {
                                        sql += ApplyWhereSubOperations(std, query, comparisonData) + " IN (";
                                    }
                                    else
                                    {
                                        sql += ApplyWhereSubOperations(std, query, comparisonData) + " NOT IN (";
                                    }

                                    foreach (object oin in comparisonData.Values)
                                    {
                                        string str = oin as string;
                                        if (str != null)
                                        {
                                            // STRING
                                            sql += comma + "'" + str + "'";
                                            comma = ",";
                                        }
                                        else if (null != oin as System.Collections.IEnumerable)
                                        {
                                            // LIST OR COLLECTION OF SOME SORT
                                            System.Collections.IEnumerable enumer = oin as System.Collections.IEnumerable;
                                            if (enumer != null)
                                            {
                                                System.Collections.IEnumerator iter = enumer.GetEnumerator();

                                                while (iter.MoveNext())
                                                {
                                                    object o = iter.Current;

                                                    string soin = o as string;

                                                    if (soin != null)
                                                        sql += comma + "'" + soin + "'";
                                                    else
                                                        sql += comma + Convert.ToString(o);

                                                    comma = ",";
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // NON STRING OR LIST
                                            sql += comma + Convert.ToString(oin);
                                            comma = ",";
                                        }
                                    }
                                    sql += ") ";
                                    requiresParam = false;
                                }
                            }
                            break;

                        case esComparisonOperand.Between:

                            OracleCommand sqlCommand = std.cmd as OracleCommand;

                            sql += ApplyWhereSubOperations(std, query, comparisonData) + " BETWEEN ";
                            sql += compareTo;
                            if (comparisonData.ComparisonColumn.Name == null)
                            {
                                OracleParameter p = new OracleParameter(compareTo, comparisonData.BetweenBegin);
                                sqlCommand.Parameters.Add(p);
                            }

                            if (comparisonData.ComparisonColumn2.Name == null)
                            {
                                IDynamicQuerySerializableInternal iColQuery = comparisonData.Column.Query as IDynamicQuerySerializableInternal;
                                esColumnMetadataCollection columns = (esColumnMetadataCollection)iColQuery.Columns;
                                compareTo = Delimiters.Param + columns[comparisonData.Column.Name].PropertyName + (++std.pindex).ToString();

                                sql += " AND " + compareTo;
                                OracleParameter p = new OracleParameter(compareTo, comparisonData.BetweenEnd);
                                sqlCommand.Parameters.Add(p);
                            }
                            else
                            {
                                sql += " AND " + Delimiters.ColumnOpen + comparisonData.ComparisonColumn2 + Delimiters.ColumnClose;
                            }

                            requiresParam = false;
                            break;
                    }

                    if (requiresParam)
                    {
                        OracleParameter p;

                        if (comparisonData.Column.Name != null)
                        {
                            p = types[comparisonData.Column.Name];

                            p = Cache.CloneParameter(p);
                            p.ParameterName = compareTo;
                            p.Value = comparisonData.Value;
                            if (needsStringParameter)
                            {
                                p.DbType = DbType.String;
                            }
                            else if (std.needsIntegerParameter)
                            {
                                p.DbType = DbType.Int32;
                            }
                        }
                        else
                        {
                            p = new OracleParameter(compareTo, comparisonData.Value);
                        }

                        std.cmd.Parameters.Add(p);
                    }

                    first = false;
                }
            }

            // Kind of a hack here, I should probably pull this out and put it in build query ... and do it only for the WHERE 
            if (query.es.Top >= 0 && prefix == " WHERE ")
            {
                if (!first)
                {
                    sql += " AND ";
                }
                else
                {
                    sql += " WHERE ";
                }
                sql += "ROWNUM <= " + query.es.Top.ToString();
            }

            return sql;
        }

        protected static string GetOrderByStatement(StandardProviderParameters std, tgDynamicQuerySerializable query)
        {
            string sql = String.Empty;
            string comma = String.Empty;

            IDynamicQuerySerializableInternal iQuery = query as IDynamicQuerySerializableInternal;

            if (iQuery.InternalOrderByItems != null)
            {
                sql += " ORDER BY ";

                foreach (tgOrderByItem orderByItem in iQuery.InternalOrderByItems)
                {
                    bool literal = false;

                    sql += comma;

                    string columnName = orderByItem.Expression.Column.Name;

                    if (columnName != null && columnName[0] == '<')
                    {
                        sql += columnName.Substring(1, columnName.Length - 2);

                        if (orderByItem.Direction == esOrderByDirection.Unassigned)
                        {
                            literal = true; // They must provide the DESC/ASC in the literal string
                        }
                    }
                    else
                    {
                        // Is in Set Operation (kind of a tricky workaround)
                        if (iQuery.HasSetOperation)
                        {
                            string joinAlias = iQuery.JoinAlias;
                            iQuery.JoinAlias = " ";
                            sql += GetExpressionColumn(std, query, orderByItem.Expression, false, false);
                            iQuery.JoinAlias = joinAlias;
                        }
                        else
                        {
                            sql += GetExpressionColumn(std, query, orderByItem.Expression, false, false);
                        }
                    }

                    if (!literal)
                    {
                        if (orderByItem.Direction == esOrderByDirection.Ascending)
                            sql += " ASC";
                        else
                            sql += " DESC";
                    }

                    comma = ",";
                }
            }

            return sql;
        }

        protected static string GetGroupByStatement(StandardProviderParameters std, tgDynamicQuerySerializable query)
        {
            string sql = String.Empty;
            string comma = String.Empty;

            IDynamicQuerySerializableInternal iQuery = query as IDynamicQuerySerializableInternal;

            if (iQuery.InternalGroupByItems != null)
            {
                sql += " GROUP BY ";

                if (query.es.WithRollup)
                {
                    sql += " ROLLUP(";
                }

                foreach (tgGroupByItem groupBy in iQuery.InternalGroupByItems)
                {
                    sql += comma;

                    string columnName = groupBy.Expression.Column.Name;

                    if (columnName != null && columnName[0] == '<')
                        sql += columnName.Substring(1, columnName.Length - 2);
                    else
                        sql += GetExpressionColumn(std, query, groupBy.Expression, false, false);

                    comma = ",";
                }

                if (query.es.WithRollup)
                {
                    sql += ")";
                }
            }

            return sql;
        }

        protected static string GetSetOperationStatement(StandardProviderParameters std, tgDynamicQuerySerializable query)
        {
            string sql = String.Empty;

            IDynamicQuerySerializableInternal iQuery = query as IDynamicQuerySerializableInternal;

            if (iQuery.InternalSetOperations != null)
            {
                foreach (tgSetOperation setOperation in iQuery.InternalSetOperations)
                {
                    switch (setOperation.SetOperationType)
                    {
                        case esSetOperationType.Union: sql += " UNION "; break;
                        case esSetOperationType.UnionAll: sql += " UNION ALL "; break;
                        case esSetOperationType.Intersect: sql += " INTERSECT "; break;
                        case esSetOperationType.Except: sql += " MINUS "; break;
                    }

                    sql += BuildQuery(std, setOperation.Query);
                }
            }

            return sql;
        }

        protected static string GetExpressionColumn(StandardProviderParameters std, tgDynamicQuerySerializable query, tgExpression expression, bool inExpression, bool useAlias)
        {
            string sql = String.Empty;

            if (expression.CaseWhen != null)
            {
                return GetCaseWhenThenEnd(std, query, expression.CaseWhen);
            }

            if (expression.HasMathmaticalExpression)
            {
                sql += GetMathmaticalExpressionColumn(std, query, expression.MathmaticalExpression);
            }
            else
            {
                sql += GetColumnName(expression.Column);
            }

            if (expression.SubOperators != null)
            {
                if (expression.Column.Distinct)
                {
                    sql = BuildSubOperationsSql(std, "DISTINCT " + sql, expression.SubOperators);
                }
                else
                {
                    sql = BuildSubOperationsSql(std, sql, expression.SubOperators);
                }
            }

            if (!inExpression && useAlias)
            {
                if (expression.SubOperators != null || expression.Column.HasAlias)
                {
                    sql += " AS " + Delimiters.ColumnOpen + expression.Column.Alias + Delimiters.ColumnClose;
                }
            }

            return sql;
        }

        protected static string GetCaseWhenThenEnd(StandardProviderParameters std, tgDynamicQuerySerializable query, tgCase caseWhenThen)
        {
            string sql = string.Empty;

            Tiraggo.DynamicQuery.tgCase.esSimpleCaseData caseStatement = caseWhenThen;

            tgColumnItem column = caseStatement.QueryItem;

            sql += "CASE ";

            List<tgComparison> list = new List<tgComparison>();

            foreach (Tiraggo.DynamicQuery.tgCase.esSimpleCaseData.esCaseClause caseClause in caseStatement.Cases)
            {
                sql += " WHEN ";
                if (!caseClause.When.IsExpression)
                {
                    sql += GetComparisonStatement(std, query, caseClause.When.Comparisons, string.Empty);
                }
                else
                {
                    if (!caseClause.When.Expression.IsLiteralValue)
                    {
                        sql += GetExpressionColumn(std, query, caseClause.When.Expression, false, true);
                    }
                    else
                    {
                        if (caseClause.When.Expression.LiteralValue is string)
                        {
                            sql += Delimiters.StringOpen + caseClause.When.Expression.LiteralValue + Delimiters.StringClose;
                        }
                        else
                        {
                            sql += Convert.ToString(caseClause.When.Expression.LiteralValue);
                        }
                    }
                }

                sql += " THEN ";

                if (!caseClause.Then.IsLiteralValue)
                {
                    sql += GetExpressionColumn(std, query, caseClause.Then, false, true);
                }
                else
                {
                    if (caseClause.Then.LiteralValue is string)
                    {
                        sql += Delimiters.AliasOpen + caseClause.Then.LiteralValue + Delimiters.AliasClose;
                    }
                    else
                    {
                        sql += Convert.ToString(caseClause.Then.LiteralValue);
                    }
                }
            }

            if (caseStatement.Else != null)
            {
                sql += " ELSE ";

                if (!caseStatement.Else.IsLiteralValue)
                {
                    sql += GetExpressionColumn(std, query, caseStatement.Else, false, true);
                }
                else
                {
                    if (caseStatement.Else.LiteralValue is string)
                    {
                        sql += Delimiters.AliasOpen + caseStatement.Else.LiteralValue + Delimiters.AliasClose;
                    }
                    else
                    {
                        sql += Convert.ToString(caseStatement.Else.LiteralValue);
                    }
                }
            }

            sql += " END ";
            sql += " AS " + Delimiters.ColumnOpen + column.Alias + Delimiters.ColumnClose;

            return sql;
        }

        protected static string GetMathmaticalExpressionColumn(StandardProviderParameters std, tgDynamicQuerySerializable query, tgMathmaticalExpression mathmaticalExpression)
        {
            bool isMod = false;
            bool needsRounding = false;

            string sql = "(";

            if (mathmaticalExpression.ItemFirst)
            {
                sql += GetExpressionColumn(std, query, mathmaticalExpression.SelectItem1, true, false);
                sql += esArithmeticOperatorToString(mathmaticalExpression, out isMod, out needsRounding);

                if (mathmaticalExpression.SelectItem2 != null)
                {
                    sql += GetExpressionColumn(std, query, mathmaticalExpression.SelectItem2, true, false);
                }
                else
                {
                    sql += GetMathmaticalExpressionLiteralType(std, mathmaticalExpression);
                }
            }
            else
            {
                if (mathmaticalExpression.SelectItem2 != null)
                {
                    sql += GetExpressionColumn(std, query, mathmaticalExpression.SelectItem2, true, true);
                }
                else
                {
                    sql += GetMathmaticalExpressionLiteralType(std, mathmaticalExpression);
                }

                sql += esArithmeticOperatorToString(mathmaticalExpression, out isMod, out needsRounding);
                sql += GetExpressionColumn(std, query, mathmaticalExpression.SelectItem1, true, false);
            }

            sql += ")";

            if (isMod)
            {
                sql = "MOD(" + sql.Replace("(", String.Empty).Replace(")", String.Empty) + ")";
            }
            if (needsRounding)
            {
                sql = "ROUND(" + sql + ", 10)";
            }

            return sql;
        }

        protected static string esArithmeticOperatorToString(tgMathmaticalExpression mathmaticalExpression, out bool isMod, out bool needsRounding)
        {
            isMod = false;
            needsRounding = false;

            switch (mathmaticalExpression.Operator)
            {
                case esArithmeticOperator.Add:

                    // MEG - 4/26/08, I'm not thrilled with this check here, will revist on future release
                    if (mathmaticalExpression.SelectItem1.Column.Datatype == esSystemType.String ||
                       (mathmaticalExpression.SelectItem1.HasMathmaticalExpression && mathmaticalExpression.SelectItem1.MathmaticalExpression.LiteralType == esSystemType.String) ||
                       (mathmaticalExpression.SelectItem1.HasMathmaticalExpression && mathmaticalExpression.SelectItem1.MathmaticalExpression.SelectItem1.Column.Datatype == esSystemType.String) ||
                       (mathmaticalExpression.LiteralType == esSystemType.String))
                        return " || ";
                    else
                        return " + ";

                case esArithmeticOperator.Subtract: return " - ";
                case esArithmeticOperator.Multiply: return " * ";

                case esArithmeticOperator.Divide:
                    needsRounding = true;
                    return "/";

                case esArithmeticOperator.Modulo:
                    isMod = true;
                    return ",";

                default: return "";
            }
        }

        protected static string GetMathmaticalExpressionLiteralType(StandardProviderParameters std, tgMathmaticalExpression mathmaticalExpression)
        {
            switch (mathmaticalExpression.LiteralType)
            {
                case esSystemType.String:
                    return "'" + (string)mathmaticalExpression.Literal + "'";

                case esSystemType.DateTime:
                    return Delimiters.StringOpen + ((DateTime)(mathmaticalExpression.Literal)).ToShortDateString() + Delimiters.StringClose;

                default:
                    return Convert.ToString(mathmaticalExpression.Literal);
            }
        }

        protected static string ApplyWhereSubOperations(StandardProviderParameters std, tgDynamicQuerySerializable query, tgComparison.esComparisonData comparisonData)
        {
            string sql = string.Empty;

            if (comparisonData.HasExpression)
            {
                sql += GetMathmaticalExpressionColumn(std, query, comparisonData.Expression);

                if (comparisonData.SubOperators != null && comparisonData.SubOperators.Count > 0)
                {
                    sql = BuildSubOperationsSql(std, sql, comparisonData.SubOperators);
                }

                return sql;
            }

            string delimitedColumnName = GetColumnName(comparisonData.Column);

            if (comparisonData.SubOperators != null)
            {
                sql = BuildSubOperationsSql(std, delimitedColumnName, comparisonData.SubOperators);
            }
            else
            {
                sql = delimitedColumnName;
            }

            return sql;
        }

        protected static string BuildSubOperationsSql(StandardProviderParameters std, string columnName, List<tgQuerySubOperator> subOperators)
        {
            string sql = string.Empty;

            subOperators.Reverse();

            Stack<object> stack = new Stack<object>();

            if (subOperators != null)
            {
                foreach (tgQuerySubOperator op in subOperators)
                {
                    switch (op.SubOperator)
                    {
                        case esQuerySubOperatorType.ToLower:
                            sql += "LOWER(";
                            stack.Push(")");
                            break;

                        case esQuerySubOperatorType.ToUpper:
                            sql += "UPPER(";
                            stack.Push(")");
                            break;

                        case esQuerySubOperatorType.LTrim:
                            sql += "LTRIM(";
                            stack.Push(")");
                            break;

                        case esQuerySubOperatorType.RTrim:
                            sql += "RTRIM(";
                            stack.Push(")");
                            break;

                        case esQuerySubOperatorType.Trim:
                            sql += "LTRIM(RTRIM(";
                            stack.Push("))");
                            break;

                        case esQuerySubOperatorType.SubString:

                            sql += "SUBSTR(";

                            stack.Push(")");
                            stack.Push(op.Parameters["length"]);
                            stack.Push(",");

                            if (op.Parameters.ContainsKey("start"))
                            {
                                stack.Push(op.Parameters["start"]);
                                stack.Push(",");
                            }
                            else
                            {
                                // They didn't pass in start so we start
                                // at the beginning
                                stack.Push(1);
                                stack.Push(",");
                            }
                            break;

                        case esQuerySubOperatorType.Coalesce:
                            sql += "COALESCE(";

                            stack.Push(")");
                            stack.Push(op.Parameters["expressions"]);
                            stack.Push(",");
                            break;

                        case esQuerySubOperatorType.Date:
                            sql += "TRUNC(";

                            stack.Push(")");
                            stack.Push("'DD'");
                            stack.Push(",");
                            break;
                        case esQuerySubOperatorType.Length:
                            sql += "LENGTH(";
                            stack.Push(")");
                            break;

                        case esQuerySubOperatorType.Round:
                            sql += "ROUND(";

                            stack.Push(")");
                            stack.Push(op.Parameters["SignificantDigits"]);
                            stack.Push(",");
                            break;

                        case esQuerySubOperatorType.DatePart:
                           std.needsStringParameter = true;
                            sql += "EXTRACT(";
                            sql += op.Parameters["DatePart"];
                            sql += " FROM ";

                            stack.Push(")");
                            break;

                        case esQuerySubOperatorType.Avg:
                            sql += "ROUND(AVG(";

                            stack.Push("), 10)");
                            break;

                        case esQuerySubOperatorType.Count:
                            sql += "COUNT(";

                            stack.Push(")");
                            break;

                        case esQuerySubOperatorType.Max:
                            sql += "MAX(";

                            stack.Push(")");
                            break;

                        case esQuerySubOperatorType.Min:
                            sql += "MIN(";

                            stack.Push(")");
                            break;

                        case esQuerySubOperatorType.StdDev:
                            sql += "ROUND(STDDEV(";

                            stack.Push("), 10)");
                            break;

                        case esQuerySubOperatorType.Sum:
                            sql += "SUM(";

                            stack.Push(")");
                            break;

                        case esQuerySubOperatorType.Var:
                            sql += "ROUND(VARIANCE(";

                            stack.Push("), 10)");
                            break;

                        case esQuerySubOperatorType.Cast:
                            sql += "CAST(";
                            stack.Push(")");

                            if (op.Parameters.Count > 1)
                            {
                                stack.Push(")");

                                if (op.Parameters.Count == 2)
                                {
                                    stack.Push(op.Parameters["length"].ToString());
                                }
                                else
                                {
                                    stack.Push(op.Parameters["scale"].ToString());
                                    stack.Push(",");
                                    stack.Push(op.Parameters["precision"].ToString());
                                }

                                stack.Push("(");
                            }


                            stack.Push(GetCastSql((esCastType)op.Parameters["esCastType"]));
                            stack.Push(" AS ");
                            break;
                    }
                }

                sql += columnName;

                while (stack.Count > 0)
                {
                    sql += stack.Pop().ToString();
                }
            }
            return sql;
        }

        protected static string GetCastSql(esCastType castType)
        {
            switch (castType)
            {
                case esCastType.Boolean: return "NUMBER";
                case esCastType.Byte: return "RAW";
                case esCastType.Char: return "CHAR";
                case esCastType.DateTime: return "TIMESTAMP";
                case esCastType.Double: return "NUMBER";
                case esCastType.Decimal: return "NUMBER";
                case esCastType.Guid: return "RAW";
                case esCastType.Int16: return "INTEGER";
                case esCastType.Int32: return "INTEGER";
                case esCastType.Int64: return "INTEGER";
                case esCastType.Single: return "NUMBER";
                case esCastType.String: return "VARCHAR2";

                default: return "error";
            }
        }


        protected static string GetColumnName(tgColumnItem column)
        {
            if (column.Query == null || column.Query.es.JoinAlias == " ")
            {
                return Delimiters.ColumnOpen + column.Name + Delimiters.ColumnClose;
            }
            else
            {
                IDynamicQuerySerializableInternal iQuery = column.Query as IDynamicQuerySerializableInternal;

                if (iQuery.IsInSubQuery)
                {
                    return column.Query.es.JoinAlias + "." + Delimiters.ColumnOpen + column.Name + Delimiters.ColumnClose;
                }
                else
                {
                    string alias = iQuery.SubQueryAlias == string.Empty ? iQuery.JoinAlias : iQuery.SubQueryAlias;
                    return alias + "." + Delimiters.ColumnOpen + column.Name + Delimiters.ColumnClose;
                }
            }
        }

        private static int NextParamIndex(IDbCommand cmd)
        {
            return cmd.Parameters.Count;
        }

        private static string GetSubquerySearchCondition(tgDynamicQuerySerializable query)
        {
            string searchCondition = String.Empty;

            IDynamicQuerySerializableInternal iQuery = query as IDynamicQuerySerializableInternal;

            switch (iQuery.SubquerySearchCondition)
            {
                case esSubquerySearchCondition.All: searchCondition = "ALL"; break;
                case esSubquerySearchCondition.Any: searchCondition = "ANY"; break;
                case esSubquerySearchCondition.Some: searchCondition = "SOME"; break;
            }

            return searchCondition;
        }
    }
}
