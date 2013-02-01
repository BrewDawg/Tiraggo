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
using System.Data.SqlServerCe;

using Tiraggo.DynamicQuery;
using Tiraggo.Interfaces;

namespace Tiraggo.SqlServerCeProvider
{
    class QueryBuilder
    {
        public static SqlCeCommand PrepareCommand(tgDataRequest request)
        {
            StandardProviderParameters std = new StandardProviderParameters();
            std.cmd = new SqlCeCommand();
            std.pindex = NextParamIndex(std.cmd);
            std.request = request;

            string sql = BuildQuery(std, request.DynamicQuery);

            std.cmd.CommandText = sql;
            return (SqlCeCommand)std.cmd;
        }

        protected static string BuildQuery(StandardProviderParameters std, tgDynamicQuerySerializable query)
        {
            bool paging = false;

            if (query.tg.PageNumber.HasValue && query.tg.PageSize.HasValue)
                paging = true;

            IDynamicQuerySerializableInternal iQuery = query as IDynamicQuerySerializableInternal;

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
                int begRow = ((query.tg.PageNumber.Value - 1) * query.tg.PageSize.Value) + 1;
                int endRow = begRow + (query.tg.PageSize.Value - 1);

                // The WITH statement
                sql += "WITH [withStatement] AS (";
                sql += "SELECT " + select + ", ROW_NUMBER() OVER(" + orderBy + ") AS ESRN ";
                sql += "FROM " + from + join + where + groupBy + ") ";

                // The actual select
                if (join.Length > 0)
                    sql += "SELECT * FROM [withStatement] ";
                else
                    sql += "SELECT " + select + " FROM [withStatement] ";

                sql += "WHERE ESRN BETWEEN " + begRow + " AND " + endRow;
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
                sql = Shared.CreateFullName(query);

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

            if (query.tg.Distinct) sql += " DISTINCT ";
            if (query.tg.Top >= 0) sql += " TOP (" + query.tg.Top.ToString() + ") ";

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

            if (query.tg.CountAll)
            {
                selectAll = false;

                sql += comma;
                sql += "COUNT(*)";

                if (query.tg.CountAllAlias != null)
                {
                    // Need DBMS string delimiter here
                    sql += " AS " + Delimiters.ColumnOpen + query.tg.CountAllAlias + Delimiters.ColumnClose;
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
                    tgJoinItem.tgJoinItemData joinData = (tgJoinItem.tgJoinItemData)joinItem;

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

                    sql += Shared.CreateFullName((tgProviderSpecificMetadata)iSubQuery.ProviderMetadata);

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
                    tgComparison.tgComparisonData comparisonData = (tgComparison.tgComparisonData)comparisonItem;
                    tgDynamicQuerySerializable subQuery = null;

                    bool requiresParam = true;
                    bool needsStringParameter = false;
                    std.needsIntegerParameter = false;

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

                    Dictionary<string, SqlCeParameter> types = null;
                    if (comparisonData.Column.Query != null)
                    {
                        IDynamicQuerySerializableInternal iLocalQuery = comparisonData.Column.Query as IDynamicQuerySerializableInternal;
                        types = Cache.GetParameters(iLocalQuery.DataID, (tgProviderSpecificMetadata)iLocalQuery.ProviderMetadata, (tgColumnMetadataCollection)iLocalQuery.Columns);
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
                                tgColumnMetadataCollection columns = (tgColumnMetadataCollection)iColQuery.Columns;
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
                        case tgComparisonOperand.NotLike:
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

                        case tgComparisonOperand.Between:

                            SqlCeCommand sqlCommand = std.cmd as SqlCeCommand;

                            sql += ApplyWhereSubOperations(std, query, comparisonData) + " BETWEEN ";
                            sql += compareTo;
                            if (comparisonData.ComparisonColumn.Name == null)
                            {
                                sqlCommand.Parameters.AddWithValue(compareTo, comparisonData.BetweenBegin);
                            }

                            if (comparisonData.ComparisonColumn2.Name == null)
                            {
                                IDynamicQuerySerializableInternal iColQuery = comparisonData.Column.Query as IDynamicQuerySerializableInternal;
                                tgColumnMetadataCollection columns = (tgColumnMetadataCollection)iColQuery.Columns;
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
                        SqlCeParameter p;

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
                            p = new SqlCeParameter(compareTo, comparisonData.Value);
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
                        sql += GetExpressionColumn(std, query, orderByItem.Expression, false, false);
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

                if (query.tg.WithRollup)
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

            Tiraggo.DynamicQuery.tgCase.tgSimpleCaseData caseStatement = caseWhenThen;

            tgColumnItem column = caseStatement.QueryItem;

            sql += "CASE ";

            List<tgComparison> list = new List<tgComparison>();

            foreach (Tiraggo.DynamicQuery.tgCase.tgSimpleCaseData.tgCaseClause caseClause in caseStatement.Cases)
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
            sql += " AS " + Delimiters.ColumnOpen + column.Alias + Delimiters.ColumnClose;

            return sql;
        }

        protected static string GetMathmaticalExpressionColumn(StandardProviderParameters std, tgDynamicQuerySerializable query, tgMathmaticalExpression mathmaticalExpression)
        {
            string sql = "(";

            if (mathmaticalExpression.ItemFirst)
            {
                sql += GetExpressionColumn(std, query, mathmaticalExpression.SelectItem1, true, true);

                switch (mathmaticalExpression.Operator)
                {
                    case tgArithmeticOperator.Add: sql += "+"; break;
                    case tgArithmeticOperator.Subtract: sql += "-"; break;
                    case tgArithmeticOperator.Multiply: sql += "*"; break;
                    case tgArithmeticOperator.Divide: sql += "/"; break;
                    case tgArithmeticOperator.Modulo: sql += "%"; break;
                }

                if (mathmaticalExpression.SelectItem2 != null)
                {
                    sql += GetExpressionColumn(std, query, mathmaticalExpression.SelectItem2, true, true);
                }
                else
                {
                    switch (mathmaticalExpression.LiteralType)
                    {
                        case tgSystemType.String:
                            sql += Delimiters.StringOpen + (string)mathmaticalExpression.Literal + Delimiters.StringClose;
                            break;

                        case tgSystemType.DateTime:
                            sql += Delimiters.StringOpen + ((DateTime)(mathmaticalExpression.Literal)).ToShortDateString() + Delimiters.StringClose;
                            break;

                        default:
                            sql += Convert.ToString(mathmaticalExpression.Literal);
                            break;
                    }
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
                    switch (mathmaticalExpression.LiteralType)
                    {
                        case tgSystemType.String:
                            sql += Delimiters.StringOpen + (string)mathmaticalExpression.Literal + Delimiters.StringClose;
                            break;

                        case tgSystemType.DateTime:
                            sql += Delimiters.StringOpen + ((DateTime)(mathmaticalExpression.Literal)).ToShortDateString() + Delimiters.StringClose;
                            break;

                        default:
                            sql += Convert.ToString(mathmaticalExpression.Literal);
                            break;
                    }
                }

                switch (mathmaticalExpression.Operator)
                {
                    case tgArithmeticOperator.Add: sql += "+"; break;
                    case tgArithmeticOperator.Subtract: sql += "-"; break;
                    case tgArithmeticOperator.Multiply: sql += "*"; break;
                    case tgArithmeticOperator.Divide: sql += "/"; break;
                    case tgArithmeticOperator.Modulo: sql += "%"; break;
                }

                sql += GetExpressionColumn(std, query, mathmaticalExpression.SelectItem1, true, true);
            }

            sql += ")";

            return sql;
        }

        protected static string ApplyWhereSubOperations(StandardProviderParameters std, tgDynamicQuerySerializable query, tgComparison.tgComparisonData comparisonData)
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
                        case tgQuerySubOperatorType.ToLower:
                            sql += "LOWER(";
                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.ToUpper:
                            sql += "UPPER(";
                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.LTrim:
                            sql += "LTRIM(";
                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.RTrim:
                            sql += "RTRIM(";
                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Trim:
                            sql += "LTRIM(RTRIM(";
                            stack.Push("))");
                            break;

                        case tgQuerySubOperatorType.SubString:

                            sql += "SUBSTRING(";

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
                            sql += "COALESCE(";

                            stack.Push(")");
                            stack.Push(op.Parameters["expressions"]);
                            stack.Push(",");
                            break;

                        case tgQuerySubOperatorType.Date:
                            sql += "DATEADD(dd, 0, DATEDIFF(dd, 0,";
                            stack.Push("))");
                            break;

                        case tgQuerySubOperatorType.Length:
                            sql += "LEN(";
                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Round:
                            sql += "ROUND(";

                            stack.Push(")");
                            stack.Push(op.Parameters["SignificantDigits"]);
                            stack.Push(",");
                            break;

                        case tgQuerySubOperatorType.DatePart:
                           std.needsStringParameter = true;
                            sql += "DATEPART(";
                            sql += op.Parameters["DatePart"];
                            sql += ",";

                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Avg:
                            sql += "AVG(";

                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Count:
                            sql += "COUNT(";

                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Max:
                            sql += "MAX(";

                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Min:
                            sql += "MIN(";

                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.StdDev:
                            sql += "STDEV(";

                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Sum:
                            sql += "SUM(";

                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Var:
                            sql += "VAR(";

                            stack.Push(")");
                            break;

                        case tgQuerySubOperatorType.Cast:
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
            if (column.Query == null || column.Query.tg.JoinAlias == " ")
            {
                return Delimiters.ColumnOpen + column.Name + Delimiters.ColumnClose;
            }
            else
            {
                IDynamicQuerySerializableInternal iQuery = column.Query as IDynamicQuerySerializableInternal;

                if (iQuery.IsInSubQuery)
                {
                    return column.Query.tg.JoinAlias + "." + Delimiters.ColumnOpen + column.Name + Delimiters.ColumnClose;
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

