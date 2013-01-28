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
using System.Data.OleDb;

using Tiraggo.DynamicQuery;
using Tiraggo.Interfaces;

namespace Tiraggo.MSAccessProvider
{
    class QueryBuilder
    {
        public static OleDbCommand PrepareCommand(esDataRequest request)
        {
            StandardProviderParameters std = new StandardProviderParameters();
            std.cmd = new OleDbCommand();
            std.pindex = NextParamIndex(std.cmd);
            std.request = request;

            string sql = BuildQuery(std, request.DynamicQuery);

            std.cmd.CommandText = sql;
            return (OleDbCommand)std.cmd;
        }

        protected static string BuildQuery(StandardProviderParameters std, tgDynamicQuerySerializable query)
        {
            IDynamicQuerySerializableInternal iQuery = query as IDynamicQuerySerializableInternal;

            string select = GetSelectStatement(std, query);
            string from = GetFromStatement(std, query);
            string join = GetJoinStatement(std, query);
            string where = GetComparisonStatement(std, query, iQuery.InternalWhereItems, " WHERE ");
            string groupBy = GetGroupByStatement(std, query);
            string having = GetComparisonStatement(std, query, iQuery.InternalHavingItems, " HAVING ");
            string orderBy = GetOrderByStatement(std, query);
            string setOperation = GetSetOperationStatement(std, query);

            string sql = "SELECT " + select + " FROM " + from + join + where + setOperation + groupBy + having + orderBy;

            return sql;
        }

        protected static string GetFromStatement(StandardProviderParameters std, tgDynamicQuerySerializable query)
        {
            IDynamicQuerySerializableInternal iQuery = query as IDynamicQuerySerializableInternal;

            string sql = String.Empty;

            int joinCount = 0;
            if (iQuery.InternalJoinItems != null && iQuery.InternalJoinItems.Count > 0)
            {
                joinCount = iQuery.InternalJoinItems.Count;

                for (int i = 0; i < joinCount; i++)
                {
                    sql += "(";
                }
            }

            if (iQuery.InternalFromQuery == null)
            {
                sql += Shared.CreateFullName(query);

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
                    sql += " AS " + iSubQuery.SubQueryAlias;
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
            if (query.es.Top >= 0) sql += " TOP " + query.es.Top.ToString() + " ";

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
                            sql += " (" + BuildQuery(std, expressionItem.Query as tgDynamicQuerySerializable) + ") AS " + iSubQuery.SubQueryAlias;
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
                    sql += " AS " + Delimiters.ColumnOpen + query.es.CountAllAlias + Delimiters.ColumnClose;
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
                        case tgJoinType.InnerJoin:
                            sql += " INNER JOIN ";
                            break;
                        case tgJoinType.LeftJoin:
                            sql += " LEFT JOIN ";
                            break;
                        case tgJoinType.RightJoin:
                            sql += " RIGHT JOIN ";
                            break;
                        case tgJoinType.FullJoin:
                            sql += " FULL JOIN ";
                            break;
                    }

                    IDynamicQuerySerializableInternal iSubQuery = joinData.Query as IDynamicQuerySerializableInternal;

                    sql += Shared.CreateFullName((esProviderSpecificMetadata)iSubQuery.ProviderMetadata);
                    sql += " " + iSubQuery.JoinAlias + " ON ";

                    foreach (tgComparison comparisonItem in joinData.WhereItems)
                    {
                        tgComparison.esComparisonData comparisonData = (tgComparison.esComparisonData)comparisonItem;

                        if (comparisonData.IsParenthesis)
                        {
                            if (comparisonData.Parenthesis == tgParenthesis.Open)
                                sql += "(";
                            else
                                sql += ")";

                            continue;
                        }

                        if (comparisonData.IsConjunction)
                        {
                            if (comparisonData.Conjunction == tgConjunction.And)
                                sql += " AND ";
                            else
                                sql += " OR ";

                            continue;
                        }

                        sql += GetColumnName(comparisonData.Column);

                        switch (comparisonData.Operand)
                        {
                            case tgComparisonOperand.Equal:
                                sql += " = ";
                                break;
                            case tgComparisonOperand.NotEqual:
                                sql += " <> ";
                                break;
                            case tgComparisonOperand.GreaterThan:
                                sql += " > ";
                                break;
                            case tgComparisonOperand.LessThan:
                                sql += " < ";
                                break;
                            case tgComparisonOperand.LessThanOrEqual:
                                sql += " <= ";
                                break;
                            case tgComparisonOperand.GreaterThanOrEqual:
                                sql += " >= ";
                                break;
                            case tgComparisonOperand.IsNull:
                                sql += " IS NULL ";
                                break;
                            case tgComparisonOperand.IsNotNull:
                                sql += " IS NOT NULL ";
                                break;
                            case tgComparisonOperand.In:
                                sql += " IN ";
                                break;
                            case tgComparisonOperand.NotIn:
                                sql += " NOT IN ";
                                break;
                        }

                        if (comparisonData.ComparisonColumn.Name != null)
                        {
                            sql += GetColumnName(comparisonData.ComparisonColumn);
                        }
                        else
                        {
                            tgDynamicQuerySerializable subQuery = comparisonData.Value as tgDynamicQuerySerializable;

                            if (subQuery != null)
                            {
                                sql += GetSubquerySearchCondition(subQuery) + " (" + BuildQuery(std, subQuery) + ")";
                            }
                            else if (comparisonData.Value != null)
                            {
                                // Handle Literals
                                sql += comparisonData.Value.ToString();
                            }
                        }
                    }

                    sql += ")";
                }
            }

            return sql;
        }

        protected static string GetComparisonStatement(StandardProviderParameters std, tgDynamicQuerySerializable query, List<tgComparison> items, string prefix)
        {
            string sql = String.Empty;
            string comma = String.Empty;

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

                    if (comparisonData.IsParenthesis)
                    {
                        if (comparisonData.Parenthesis == tgParenthesis.Open)
                            sql += "(";
                        else
                            sql += ")";

                        continue;
                    }

                    if (comparisonData.IsConjunction)
                    {
                        switch (comparisonData.Conjunction)
                        {
                            case tgConjunction.And: sql += " AND "; break;
                            case tgConjunction.Or: sql += " OR "; break;
                            case tgConjunction.AndNot: sql += " AND NOT "; break;
                            case tgConjunction.OrNot: sql += " OR NOT "; break;
                        }
                        continue;
                    }

                    Dictionary<string, OleDbParameter> types = null;
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
                        case tgComparisonOperand.Exists:
                            sql += " EXISTS" + compareTo;
                            break;
                        case tgComparisonOperand.NotExists:
                            sql += " NOT EXISTS" + compareTo;
                            break;

                        //-----------------------------------------------------------
                        // Comparison operators, left side vs right side
                        //-----------------------------------------------------------
                        case tgComparisonOperand.Equal:
                            if (comparisonData.ItemFirst)
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " = " + compareTo;
                            else
                                sql += compareTo + " = " + ApplyWhereSubOperations(std, query, comparisonData);
                            break;
                        case tgComparisonOperand.NotEqual:
                            if (comparisonData.ItemFirst)
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " <> " + compareTo;
                            else
                                sql += compareTo + " <> " + ApplyWhereSubOperations(std, query, comparisonData);
                            break;
                        case tgComparisonOperand.GreaterThan:
                            if (comparisonData.ItemFirst)
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " > " + compareTo;
                            else
                                sql += compareTo + " > " + ApplyWhereSubOperations(std, query, comparisonData);
                            break;
                        case tgComparisonOperand.LessThan:
                            if (comparisonData.ItemFirst)
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " < " + compareTo;
                            else
                                sql += compareTo + " < " + ApplyWhereSubOperations(std, query, comparisonData);
                            break;
                        case tgComparisonOperand.LessThanOrEqual:
                            if (comparisonData.ItemFirst)
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " <= " + compareTo;
                            else
                                sql += compareTo + " <= " + ApplyWhereSubOperations(std, query, comparisonData);
                            break;
                        case tgComparisonOperand.GreaterThanOrEqual:
                            if (comparisonData.ItemFirst)
                                sql += ApplyWhereSubOperations(std, query, comparisonData) + " >= " + compareTo;
                            else
                                sql += compareTo + " >= " + ApplyWhereSubOperations(std, query, comparisonData);
                            break;

                        case tgComparisonOperand.Like:
                            sql += ApplyWhereSubOperations(std, query, comparisonData) + " LIKE " + compareTo;
                            needsStringParameter = true;
                            break;
                        case tgComparisonOperand.NotLike:
                            sql += ApplyWhereSubOperations(std, query, comparisonData) + " NOT LIKE " + compareTo;
                            needsStringParameter = true;
                            break;
                        case tgComparisonOperand.Contains:
                            sql += " CONTAINS(" + GetColumnName(comparisonData.Column) + ", " + compareTo + ")";
                            needsStringParameter = true;
                            break;
                        case tgComparisonOperand.IsNull:
                            sql += ApplyWhereSubOperations(std, query, comparisonData) + " IS NULL";
                            requiresParam = false;
                            break;
                        case tgComparisonOperand.IsNotNull:
                            sql += ApplyWhereSubOperations(std, query, comparisonData) + " IS NOT NULL";
                            requiresParam = false;
                            break;
                        case tgComparisonOperand.In:
                        case tgComparisonOperand.NotIn:
                            {
                                if (subQuery != null)
                                {
                                    // They used a subquery for In or Not 
                                    sql += ApplyWhereSubOperations(std, query, comparisonData);
                                    sql += (comparisonData.Operand == tgComparisonOperand.In) ? " IN" : " NOT IN";
                                    sql += compareTo;
                                }
                                else
                                {
                                    comma = String.Empty;
                                    if (comparisonData.Operand == tgComparisonOperand.In)
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
                                        if (str != null && !(str.StartsWith("#")))
                                        {
                                            // STRING
                                            sql += comma + Delimiters.StringOpen + str + Delimiters.StringClose;
                                            comma = ",";
                                        }
                                        else if (str != null)
                                        {
                                            // DateTime
                                            sql += comma + Convert.ToString(oin);
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

                                                    if (soin != null && !(soin.StartsWith("#")))
                                                        sql += comma + Delimiters.StringOpen + soin + Delimiters.StringClose;
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

                        case tgComparisonOperand.Between:

                            OleDbCommand sqlCommand = std.cmd as OleDbCommand;

                            sql += ApplyWhereSubOperations(std, query, comparisonData) + " BETWEEN ";
                            sql += compareTo;
                            if (comparisonData.ComparisonColumn.Name == null)
                            {
                                sqlCommand.Parameters.AddWithValue(compareTo, comparisonData.BetweenBegin);
                            }

                            if (comparisonData.ComparisonColumn2.Name == null)
                            {
                                IDynamicQuerySerializableInternal iColQuery = comparisonData.Column.Query as IDynamicQuerySerializableInternal;
                                esColumnMetadataCollection columns = (esColumnMetadataCollection)iColQuery.Columns;
                                compareTo = Delimiters.Param + columns[comparisonData.Column.Name].PropertyName + (++std.pindex).ToString();

                                sql += " AND " + compareTo;
                                sqlCommand.Parameters.AddWithValue(compareTo, comparisonData.BetweenEnd);
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
                        OleDbParameter p;

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
                        }
                        else
                        {
                            p = new OleDbParameter(compareTo, comparisonData.Value);
                        }

                        std.cmd.Parameters.Add(p);
                    }
                }
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

                        if (orderByItem.Direction == tgOrderByDirection.Unassigned)
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
                        if (orderByItem.Direction == tgOrderByDirection.Ascending)
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
                    sql += " WITH ROLLUP";
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
                        case tgSetOperationType.Union: sql += " UNION "; break;
                        case tgSetOperationType.UnionAll: sql += " UNION ALL "; break;
                        case tgSetOperationType.Intersect: sql += " INTERSECT "; break;
                        case tgSetOperationType.Except: sql += " EXCEPT "; break;
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
                sql += " AS " + Delimiters.ColumnOpen + expression.Column.Alias + Delimiters.ColumnClose;
            }

            return sql;
        }

        protected static string GetCaseWhenThenEnd(StandardProviderParameters std, tgDynamicQuerySerializable query, tgCase caseWhenThen)
        {
            string sql = string.Empty;

            Tiraggo.DynamicQuery.tgCase.esSimpleCaseData caseStatement = caseWhenThen;

            tgColumnItem column = caseStatement.QueryItem;

            sql += Delimiters.ColumnOpen + column.Alias + Delimiters.ColumnClose + " = ";
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
                        sql += Delimiters.StringOpen + caseClause.Then.LiteralValue + Delimiters.StringClose;
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
                        sql += Delimiters.StringOpen + caseStatement.Else.LiteralValue + Delimiters.StringClose;
                    }
                    else
                    {
                        sql += Convert.ToString(caseStatement.Else.LiteralValue);
                    }
                }
            }

            sql += " END ";

            return sql;
        }

        protected static string GetMathmaticalExpressionColumn(StandardProviderParameters std, tgDynamicQuerySerializable query, tgMathmaticalExpression mathmaticalExpression)
        {
            string sql = "(";

            if (mathmaticalExpression.ItemFirst)
            {
                sql += GetExpressionColumn(std, query, mathmaticalExpression.SelectItem1, true, true);
                sql += esArithmeticOperatorToString(mathmaticalExpression.Operator);

                if (mathmaticalExpression.SelectItem2 != null)
                {
                    sql += GetExpressionColumn(std, query, mathmaticalExpression.SelectItem2, true, true);
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

                sql += esArithmeticOperatorToString(mathmaticalExpression.Operator);
                sql += GetExpressionColumn(std, query, mathmaticalExpression.SelectItem1, true, true);
            }

            sql += ")";

            return sql;
        }

        protected static string esArithmeticOperatorToString(tgArithmeticOperator arithmeticOperator)
        {
            switch (arithmeticOperator)
            {
                case tgArithmeticOperator.Add: return " + ";
                case tgArithmeticOperator.Subtract: return " - ";
                case tgArithmeticOperator.Multiply: return " * ";
                case tgArithmeticOperator.Divide: return " / ";
                case tgArithmeticOperator.Modulo: return " % ";
                default: return "";
            }
        }

        protected static string GetMathmaticalExpressionLiteralType(StandardProviderParameters std, tgMathmaticalExpression mathmaticalExpression)
        {
            switch (mathmaticalExpression.LiteralType)
            {
                case tgSystemType.String:
                    return Delimiters.StringOpen + (string)mathmaticalExpression.Literal + Delimiters.StringClose;

                case tgSystemType.DateTime:
                    return Delimiters.StringOpen + ((DateTime)(mathmaticalExpression.Literal)).ToShortDateString() + Delimiters.StringClose;

                default:
                    return Convert.ToString(mathmaticalExpression.Literal);
            }
        }

        protected static string ApplyWhereSubOperations(StandardProviderParameters std, tgDynamicQuerySerializable query, tgComparison.esComparisonData comparisonData)
        {
            string sql = string.Empty;
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
                        case tgQuerySubOperatorType.ToLower:
                            sql += "LCase(";
                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.ToUpper:
                            sql += "UCase(";
                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.LTrim:
                            sql += "LTrim(";
                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.RTrim:
                            sql += "RTrim(";
                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Trim:
                            sql += "Trim(";
                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.SubString:

                            sql += "Mid(";

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

                        case tgQuerySubOperatorType.Coalesce:
                            sql += "Coalesce(";

                            stack.Push(")");
                            stack.Push(op.Parameters["expressions"]);
                            stack.Push(",");
                            break;

                        case tgQuerySubOperatorType.Date:
                            sql += "DATEADD(\"d\", 0, DATEDIFF(\"d\", 0,";

                            stack.Push("))");
                            break;
                        case tgQuerySubOperatorType.Length:
                            sql += "Len(";
                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Round:
                            sql += "Round(";

                            stack.Push(")");
                            stack.Push(op.Parameters["SignificantDigits"]);
                            stack.Push(",");
                            break;

                        case tgQuerySubOperatorType.DatePart:
                            sql += "DatePart(\"";
                            sql += op.Parameters["DatePart"];
                            sql += "\",";

                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Avg:
                            sql += "Round(Avg(";

                            stack.Push("), 10)");
                            break;

                        case tgQuerySubOperatorType.Count:
                            sql += "Count(";

                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Max:
                            sql += "Max(";

                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Min:
                            sql += "Min(";

                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.StdDev:
                            sql += "Round(StDev(";

                            stack.Push("), 10)");
                            break;
 
                        case tgQuerySubOperatorType.Sum:
                            sql += "Sum(";

                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Var:
                            sql += "Round(Var(";

                            stack.Push("), 10)");
                            break;

                        case tgQuerySubOperatorType.Cast:
                            sql += "Cast(";
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


                            stack.Push(GetCastSql((tgCastType)op.Parameters["tgCastType"]));
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

        protected static string GetCastSql(tgCastType castType)
        {
            switch (castType)
            {
                case tgCastType.Boolean: return "bit";
                case tgCastType.Byte: return "tinyint";
                case tgCastType.Char: return "char";
                case tgCastType.DateTime: return "datetime";
                case tgCastType.Double: return "float";
                case tgCastType.Decimal: return "decimal";
                case tgCastType.Guid: return "uniqueidentifier";
                case tgCastType.Int16: return "smallint";
                case tgCastType.Int32: return "int";
                case tgCastType.Int64: return "bigint";
                case tgCastType.Single: return "real";
                case tgCastType.String: return "nvarchar";

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
                case tgSubquerySearchCondition.All: searchCondition = "ALL"; break;
                case tgSubquerySearchCondition.Any: searchCondition = "ANY"; break;
                case tgSubquerySearchCondition.Some: searchCondition = "SOME"; break;
            }

            return searchCondition;
        }
    }
}
