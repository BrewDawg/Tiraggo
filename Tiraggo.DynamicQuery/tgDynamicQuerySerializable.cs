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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;


namespace Tiraggo.DynamicQuery
{
    /// <summary>
    /// This provides the Dynamic Query mechanism used by your Business object (Employees),
    /// collection (EmployeesCollection), and query caching (EmployeesQuery).
    /// </summary>
    /// <example>
    /// DynamicQuery allows you to (without writing any stored procedures)
    /// query your database on the fly. All selection criteria are passed in
    /// via Parameters (SAParameter, OleDbParameter) in order to prevent
    /// sql injection techniques often attempted by hackers.  
    /// Additional examples are provided here:
    /// <code>
    /// http://www.entityspaces.net/portal/QueryAPISamples/tabid/80/Default.aspx
    /// </code>
    /// <code>
    /// EmployeesCollection emps = new EmployeesCollection;
    /// 
    /// emps.Query.es.CountAll = true;
    /// emps.Query.Select
    /// (
    ///		emps.Query.LastName,
    ///		emps.Query.FirstName
    /// )
    /// .Where
    /// (
    ///		emps.Query.Or
    ///		(
    ///			emps.Query.LastName.Like("%A%"),
    ///			emps.Query.LastName.Like("%O%")
    ///		),
    ///		emps.Query.BirthDate.Between("1940-01-01", "2006-12-31")
    /// )
    /// .GroupBy
    /// (
    ///		emps.Query.LastName,
    ///		emps.Query.FirstName
    /// )
    /// .OrderBy
    /// (
    ///		emps.Query.LastName.Descending,
    ///		emps.Query.FirstName.Ascending
    /// );
    /// 
    /// emps.Query.Load();
    /// </code>
    /// </example>
    [Serializable]
    [DataContract(Namespace = "tg", IsReference = true)]
    public class tgDynamicQuerySerializable : IDynamicQuerySerializableInternal
    {
        /// <summary>
        /// The Constructor
        /// </summary>
        public tgDynamicQuerySerializable()
        {

        }

        /// <summary>
        /// The Constructor used when using this query in a "Join"
        /// </summary>
        /// <param name="joinAlias">The alias of the associated Table to be used in the "Join"</param>
        public tgDynamicQuerySerializable(string joinAlias)
        {
            this.joinAlias = joinAlias;
        }

        /// <summary>
        /// Called whenever the Entity needs a connection. This can be used to override the default connection 
        /// per object manually, or automatically by filling in the "Connection Name" on the "Generated Master"
        /// template. 
        /// </summary>
        /// <returns></returns>
        virtual protected string GetConnectionName()
        {
            return null;
        }

        /// <summary>
        /// Used during DataContract Serializaton
        /// </summary>
        /// <returns>The Name of the Query such as EmployeesQuery and not EmployeesQueryProxyStub</returns>
        virtual protected string GetQueryName()
        {
            return null;
        }

        virtual protected tgQueryItem QueryItemFromName(string name)
        {
            return null;
        }

        /// <summary>
        /// Used by EntitySpaces internally
        /// </summary>
        /// <param name="query">The SubQuery</param>
        internal void AddQueryToList(tgDynamicQuerySerializable query)
        {
            if (!queries.ContainsKey(query.joinAlias) && query != this)
            {
                if (subQueryNames == null) subQueryNames += string.Empty;
                if (subQueryNames.Length > 0) subQueryNames += ";";
                subQueryNames += query.GetQueryName();

                queries[query.joinAlias] = query;
            }
        }

        /// <summary>
        /// The query's SELECT clause. SELECT * by default (all columns).
        /// </summary>
        /// <example>
        /// <code>
        /// emps.Query.Select();
        /// </code>
        /// </example>
        /// <returns>An esDynamicQueryTransport containing a select clause.</returns>
        public tgDynamicQuerySerializable Select()
        {
            return this;
        }

        /// <summary>
        /// A query SELECT clause for the specified column.
        /// </summary>
        /// <example>
        /// <code>
        /// emps.Query.Select(emps.Query.LastName);
        /// </code>
        /// </example>
        /// <param name="column">The column to place in the select statement.</param>
        /// <returns>An esDynamicQueryTransport containing a select clause.</returns>
        public tgDynamicQuerySerializable Select(object obj)
        {
            if (this.selectColumns == null)
            {
                this.selectColumns = new List<tgExpression>();
            }

            tgExpression sItem = obj as tgExpression;
            if (sItem == null)
            {
                tgQueryItem queryItem = null;

                tgCast cast = obj as tgCast;
                if (cast != null)
                {
                    queryItem = cast.item;
                }
                else
                {
                    queryItem = obj as tgQueryItem;
                }

                // This if conditional is this way intentionally
                if(queryItem is object)
                {
                    sItem = queryItem;
                }
                else
                {
                    sItem = new tgExpression();
                    tgDynamicQuerySerializable query = obj as tgDynamicQuerySerializable;
                    if (query != null)
                    {
                        AddQueryToList(query);
                        sItem.Query = query;
                    }
                    else
                    {
                        sItem.Column.Name = obj as string;
                    }
                }
            }

            this.selectColumns.Add(sItem);

            return this;
        }

        /// <summary>
        /// A query SELECT clause for columns or aggregates.
        /// </summary>
        /// <example>
        /// See <see cref="tgQuerySubOperatorType"/> Enumeration.
        /// <code>
        /// emps.Query.Select
        /// (
        ///		emps.Query.LastName,
        ///		emps.Query.Age.Avg("Average Age")
        /// );
        /// </code>
        /// </example>
        /// <param name="columns">The list of columns or aggregates to place in the select statement.</param>
        /// <returns>An esDynamicQueryTransport containing a select clause.</returns>
        public tgDynamicQuerySerializable Select(params object[] columns)
        {
            if (this.selectColumns == null)
            {
                this.selectColumns = new List<tgExpression>();
            }

            foreach (object obj in columns)
            {
                tgQueryItem queryItem = null;
                tgExpression sItem;

                tgCast cast = obj as tgCast;
                if (cast != null)
                {
                    queryItem = cast.item;
                }
                else
                {
                    queryItem = obj as tgQueryItem;
                }

                // This if conditional is this way intentionally
                if(queryItem is object)
                {
                    sItem = queryItem;
                }
                else
                {
                    sItem = new tgExpression();
                    tgDynamicQuerySerializable query = obj as tgDynamicQuerySerializable;
                    if (query != null)
                    {
                        AddQueryToList(query);
                        sItem.Query = query;
                    }
                    else
                    {
                        sItem.Column.Name = obj as string;
                    }
                }

                this.selectColumns.Add(sItem);
            }

            return this;
        }

        /// <summary>
        /// This method will create a Select statement for all of the columns in the entity except for the ones passed in.
        /// This is very useful when you want to eliminate blobs and other fields for performance.
        /// </summary>
        /// <param name="columns">The columns which you wish to exclude from the Select statement</param>
        /// <returns></returns>
        virtual public tgDynamicQuerySerializable SelectAllExcept(params tgQueryItem[] columns)
        {
            if (m_selectAllExcept == null)
            {
                m_selectAllExcept = new List<tgQueryItem>();
            }

            foreach (tgQueryItem item in columns)
            {
                m_selectAllExcept.Add(item);
            }
            return this;
        }

        /// <summary>
        /// This method will select all of the columns explicity by name that were present when you generated your
        /// classes as opposed to doing a SELECT *
        /// </summary>
        /// <returns></returns>
        virtual public tgDynamicQuerySerializable SelectAll()
        {
            m_selectAll = true;
            return this;
        }

        /// <summary>
        /// Use this method in conjuction with Take()
        /// </summary>
        /// <param name="count">The number of rows to skip</param>
        /// <returns></returns>
        public tgDynamicQuerySerializable Skip(int count)
        {
            this.skip = count;
            return this;
        }

        /// <summary>
        /// Use this method in conjuction with Skip()
        /// </summary>
        /// <param name="count">The number of rows to return</param>
        /// <returns></returns>
        public tgDynamicQuerySerializable Take(int count)
        {
            this.take = count;
            return this;
        }

        /// <summary>
        /// Creates an INNER JOIN 
        /// </summary>
        /// <param name="joinQuery">This query represents the table you are joining to</param>
        /// <returns>An tgJoinItem, which you then call the On() method.</returns>
        public tgJoinItem InnerJoin(tgDynamicQuerySerializable joinQuery)
        {
            return JoinCommon(joinQuery, tgJoinType.InnerJoin);
        }

        /// <summary>
        /// Creates a LEFT JOIN 
        /// </summary>
        /// <param name="joinQuery">This query represents the table you are joining to</param>
        /// <returns>An tgJoinItem, which you then call the On() method.</returns>
        public tgJoinItem LeftJoin(tgDynamicQuerySerializable joinQuery)
        {
            return JoinCommon(joinQuery, tgJoinType.LeftJoin);
        }

        /// <summary>
        /// Creates a RIGHT JOIN 
        /// </summary>
        /// <param name="joinQuery">This query represents the table you are joining to</param>
        /// <returns>An tgJoinItem, which you then call the On() method.</returns>
        public tgJoinItem RightJoin(tgDynamicQuerySerializable joinQuery)
        {
            return JoinCommon(joinQuery, tgJoinType.RightJoin);
        }

        /// <summary>
        /// Creates a FULL JOIN 
        /// </summary>
        /// <param name="joinQuery">This query represents the table you are joining to</param>
        /// <returns>An tgJoinItem, which you then call the On() method.</returns>
        public tgJoinItem FullJoin(tgDynamicQuerySerializable joinQuery)
        {
            return JoinCommon(joinQuery, tgJoinType.FullJoin);
        }

        /// <summary>
        /// Creates an INNER JOIN 
        /// </summary>
        /// <param name="joinQuery">This query represents the table you are joining to</param>
        /// <returns>An tgJoinItem, which you then call the On() method.</returns>
        public tgJoinItem CrossJoin(tgDynamicQuerySerializable joinQuery)
        {
            return JoinCommon(joinQuery, tgJoinType.CrossJoin);
        }

        /// <summary>
        /// Used by all of the join functions as all of the logic is the same except for 
        /// the type of join
        /// </summary>
        /// <param name="joinQuery">This query represents the table you are joining to</param>
        /// <param name="joinType">The type of join, ie, Inner, Left, Right, Full</param>
        /// <returns></returns>
        private tgJoinItem JoinCommon(tgDynamicQuerySerializable joinQuery, tgJoinType joinType)
        {
            if (joinQuery.joinAlias == " ")
            {
                throw new Exception("Your DynamicQuery must have an Alias, use the alternate constructor");
            }

            if (this.joinItems == null)
            {
                this.joinItems = new List<tgJoinItem>();
            }

            tgJoinItem joinItem = new tgJoinItem(this);
            joinItem.data.Query = joinQuery;
            joinItem.data.JoinType = joinType;

            this.joinItems.Add(joinItem);

            AddQueryToList(joinQuery);

            return joinItem;
        }

        /// <summary>
        /// Allows you to pass in a SubQuery for your FROM statement
        /// </summary>
        /// <param name="fromQuery">The subquery to use as your FROM statement</param>
        /// <returns></returns>
        public tgDynamicQuerySerializable From(tgDynamicQuerySerializable fromQuery)
        {
            this.fromQuery = fromQuery;
            AddQueryToList(fromQuery);
            return this;
        }

        /// <summary>
        /// Performs a UNION between two statements
        /// </summary>
        /// <returns></returns>
        public tgDynamicQuerySerializable Union(tgDynamicQuerySerializable query)
        {
            tgSetOperation setOperation = new tgSetOperation(query);
            setOperation.SetOperationType = tgSetOperationType.Union;

            if(setOperations == null)
            {
                setOperations = new List<tgSetOperation>();
            }

            setOperations.Add(setOperation);

            return this;
        }

        /// <summary>
        /// Performs a UNION ALL between two statements
        /// </summary>
        /// <returns></returns>
        public tgDynamicQuerySerializable UnionAll(tgDynamicQuerySerializable query)
        {
            tgSetOperation setOperation = new tgSetOperation(query);
            setOperation.SetOperationType = tgSetOperationType.UnionAll;

            if (setOperations == null)
            {
                setOperations = new List<tgSetOperation>();
            }

            setOperations.Add(setOperation);

            return this;
        }

        /// <summary>
        /// Peforms an INTERSECT between two statements
        /// </summary>
        /// <returns></returns>
        public tgDynamicQuerySerializable Intersect(tgDynamicQuerySerializable query)
        {
            tgSetOperation setOperation = new tgSetOperation(query);
            setOperation.SetOperationType = tgSetOperationType.Intersect;

            if (setOperations == null)
            {
                setOperations = new List<tgSetOperation>();
            }

            setOperations.Add(setOperation);

            return this;
        }

        /// <summary>
        /// Performs an EXCEPT between two statements
        /// </summary>
        /// <returns></returns>
        public tgDynamicQuerySerializable Except(tgDynamicQuerySerializable query)
        {
            tgSetOperation setOperation = new tgSetOperation(query);
            setOperation.SetOperationType = tgSetOperationType.Except;

            if (setOperations == null)
            {
                setOperations = new List<tgSetOperation>();
            }

            setOperations.Add(setOperation);

            return this;
        }

        /// <summary>
        /// Provide an Alias for your query or subquery
        /// </summary>
        /// <param name="subQueryAlias"></param>
        /// <returns></returns>
        public tgDynamicQuerySerializable As(string subQueryAlias)
        {
            if (this.fromQuery == null)
            {
                this.subQueryAlias = subQueryAlias;
            }
            else
            {
                this.fromQuery.subQueryAlias = subQueryAlias;
            }
            return this;
        }

        /// <summary>
        /// A query WHERE clause. The default conjunction is "AND".
        /// </summary>
        /// <example>
        /// See <see cref="tgConjunction"/> Enumeration.
        /// <code>
        /// emps.Query.Where
        /// (
        ///		emps.Query.LastName == "Doe" &amp;&amp; emps.Query.FirstName.Like("J%")
        /// );
        /// </code>
        /// </example>
        /// <param name="items">The list of objects to place in the where statement.</param>
        /// <returns>An esDynamicQueryTransport containing a where clause.</returns>
        public tgDynamicQuerySerializable Where(params object[] items)
        {
            bool first = true;

            if (this.whereItems == null)
            {
                this.whereItems = new List<tgComparison>();
            }
            else
            {
                if (!isExplicitParenthesis)
                {
                    whereItems.Add(new tgComparison(this.defaultConjunction));
                }
                else
                {
                    isExplicitParenthesis = false;
                }
            }

            foreach (object item in items)
            {
                tgComparison wi = item as tgComparison;

                if (wi != null)
                {
                    if (wi.Parenthesis == tgParenthesis.Open)
                    {
                        isExplicitParenthesis = true;
                    }
                    else if (wi.Parenthesis == tgParenthesis.Close)
                    {
                        this.whereItems.RemoveAt(this.whereItems.Count - 1);
                    }

                    if (wi.data.WhereExpression != null)
                    {
                        foreach (tgComparison expItem in wi.data.WhereExpression)
                        {
                            if (expItem.Value != null)
                            {
                                tgDynamicQuerySerializable q = expItem.data.Value as tgDynamicQuerySerializable;
                                if (q != null)
                                {
                                    AddQueryToList(q);
                                }
                            }
                        }

                        this.whereItems.AddRange(wi.data.WhereExpression);
                    }
                    else
                    {
                        if (!first)
                        {
                            this.whereItems.Add(new tgComparison(this.defaultConjunction));
                        }
                        this.whereItems.Add(wi);
                    }

                    tgDynamicQuerySerializable query = wi.Value as tgDynamicQuerySerializable;

                    if (query != null)
                    {
                        AddQueryToList(query);
                    }
                }
                else
                {
                    if (!first)
                    {
                        this.whereItems.Add(new tgComparison(this.defaultConjunction));
                    }
                    this.whereItems.AddRange(ProcessWhereItems(this.defaultConjunction, item));
                }
                first = false;
            }

            return this;
        }

        /// <summary>
        /// This is not the preferred way to build a where clause. The preferred way is to use the Where() method. This is typically used when the UI allows for the user to pick any number of filters to operate on.
        /// </summary>
        /// <param name="column">This is actual the Propery name of the Entity, not the physical column name in the database table, althought they might be the same</param>
        /// <param name="operation">"EQUAL", "NOTEQUAL", "GREATERTHAN", "GREATERTHANOREQUAL", "LESSTHAN", "LESSTHANOREQUAL", "LIKE", "ISNULL", "ISNOTNULL", "BETWEEN", "IN", "NOTIN", "NOTLIKE", "CONTAINS"</param>
        /// <param name="criteria">The value the operation is to act upon</param>
        /// <param name="criteria2">the 2nd value of the "BETWEEN" operation</param>
        /// <param name="conjuction">Either "AND" or "OR"</param>
        /// <returns></returns>
        public tgComparison ManualWhere(string column, string operation, object criteria, object criteria2, string conjuction)
        {
            var v = null as tgComparison;

            switch (operation.ToUpper())
            {
                case "EQUAL":
                    v = this.QueryItemFromName(column) == criteria;
                    break;

                case "NOTEQUAL":
                    v = this.QueryItemFromName(column) != criteria;
                    break;

                case "GREATERTHAN":
                    v = this.QueryItemFromName(column) > criteria;
                    break;

                case "GREATERTHANOREQUAL":
                    v = this.QueryItemFromName(column) >= criteria;
                    break;

                case "LESSTHAN":
                    v = this.QueryItemFromName(column) < criteria;
                    break;

                case "LESSTHANOREQUAL":
                    v = this.QueryItemFromName(column) <= criteria;
                    break;

                case "LIKE":
                    v = this.QueryItemFromName(column).Like(criteria);
                    break;

                case "ISNULL":
                    v = this.QueryItemFromName(column).IsNull();
                    break;

                case "ISNOTNULL":
                    v = this.QueryItemFromName(column).IsNotNull();
                    break;

                case "BETWEEN":
                    v = this.QueryItemFromName(column).Between(criteria, criteria2);
                    break;

                case "IN":
                    v = this.QueryItemFromName(column).In(criteria);
                    break;

                case "NOTIN":
                    v = this.QueryItemFromName(column).NotIn(criteria);
                    break;

                case "NOTLIKE":
                    v = this.QueryItemFromName(column).NotLike(criteria);
                    break;

                case "CONTAINS":
                    v = this.QueryItemFromName(column).Contains(criteria);
                    break;
            }

            if (prevComp != null)
            {
                if (conjuction.ToUpper().StartsWith("A"))
                {
                    v = prevComp && v;
                }
                else
                {
                    v = prevComp || v;
                }
            }

            prevComp = v;

            return v;
        }


        /// <summary>
        /// A query WHERE clause. The default conjunction is "AND".
        /// </summary>
        /// <example>
        /// See <see cref="tgConjunction"/> Enumeration.
        /// <code>
        /// emps.Query.Where
        /// (
        ///		emps.Query.LastName == "Doe" &amp;&amp; emps.Query.FirstName.Like("J%")
        /// );
        /// </code>
        /// </example>
        /// <param name="items">The list of objects to place in the where statement.</param>
        /// <returns>An esDynamicQueryTransport containing a where clause.</returns>
        public tgDynamicQuerySerializable Having(params object[] items)
        {
            bool first = true;

            if (this.havingItems == null)
            {
                this.havingItems = new List<tgComparison>();
            }
            else
            {
                if (!isExplicitParenthesis)
                {
                    havingItems.Add(new tgComparison(this.defaultConjunction));
                }
                else
                {
                    isExplicitParenthesis = false;
                }
            }

            foreach (object item in items)
            {
                tgComparison wi = item as tgComparison;

                if (wi != null)
                {
                    if (wi.Parenthesis == tgParenthesis.Open)
                    {
                        isExplicitParenthesis = true;
                    }
                    else if (wi.Parenthesis == tgParenthesis.Close)
                    {
                        this.havingItems.RemoveAt(this.havingItems.Count - 1);
                    }

                    if (wi.data.WhereExpression != null)
                    {
                        foreach (tgComparison expItem in wi.data.WhereExpression)
                        {
                            if (expItem.Value != null)
                            {
                                tgDynamicQuerySerializable q = expItem.data.Value as tgDynamicQuerySerializable;
                                if (q != null)
                                {
                                    AddQueryToList(q);
                                }
                            }
                        }

                        this.havingItems.AddRange(wi.data.WhereExpression);
                    }
                    else
                    {
                        if (!first)
                        {
                            this.havingItems.Add(new tgComparison(this.defaultConjunction));
                        }
                        this.havingItems.Add(wi);
                    }

                    tgDynamicQuerySerializable query = wi.Value as tgDynamicQuerySerializable;

                    if (query != null)
                    {
                        AddQueryToList(query);
                    }
                }
                else
                {
                    if (!first)
                    {
                        this.havingItems.Add(new tgComparison(this.defaultConjunction));
                    }
                    this.havingItems.AddRange(ProcessWhereItems(this.defaultConjunction, item));
                }
                first = false;
            }

            return this;
        }

        /// <summary>
        /// Pass in a subquery to use in an EXISTS statement
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public tgComparison Exists(tgDynamicQuerySerializable query)
        {
            AddQueryToList(query);

            tgComparison where = new tgComparison(query);
            where.Operand = tgComparisonOperand.Exists;
            where.Value = query;
            return where;
        }

        /// <summary>
        /// Pass in a subquery to use in an NOT EXISTS statement
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public tgComparison NotExists(tgDynamicQuerySerializable query)
        {
            AddQueryToList(query);

            tgComparison where = new tgComparison(query);
            where.Operand = tgComparisonOperand.NotExists;
            where.Value = query;
            return where;
        }

        /// <summary>
        /// A query WHERE clause joined by "AND". Natural language alternative, C# = &amp;&amp; operator, VB.NET = AND Keyword
        /// </summary>
        /// <example>
        /// See <see cref="tgConjunction"/> Enumeration.
        /// <code>
        /// emps.Query.Where
        /// (
        ///		emps.Query.And
        ///		(
        ///			emps.Query.LastName == "Doe",
        ///			emps.Query.FirstName.Like("J%")
        ///		),
        ///		emps.Query.Age.LessThen(50)
        /// );
        /// </code>
        /// </example>
        /// <param name="items">The list of objects to place in the where statement.</param>
        /// <returns>An esDynamicQueryTransport containing a where clause.</returns>
        [Obsolete("For more readable code use '&&' in C# or 'AndAlso' in VB.NET rather than this method")]
        public List<tgComparison> And(params object[] items)
        {
            return ProcessWhereItems(tgConjunction.And, items);
        }

        /// <summary>
        /// A query WHERE clause joined by "AND NOT". Natural language alternative, C# = &amp;&amp; ! operator, VB.NET = AND NOT Keyword
        /// </summary>
        /// <example>
        /// See <see cref="tgConjunction"/> Enumeration.
        /// <code>
        /// emps.Query.Where
        /// (
        ///		emps.Query.And
        ///		(
        ///			emps.Query.LastName == "Doe",
        ///			emps.Query.FirstName.Like("J%")
        ///		),
        ///		emps.Query.Age.LessThen(50)
        /// );
        /// </code>
        /// </example>
        /// <param name="items">The list of objects to place in the where statement.</param>
        /// <returns>An esDynamicQueryTransport containing a where clause.</returns>
        public List<tgComparison> AndNot(params object[] items)
        {
            return ProcessWhereItems(tgConjunction.AndNot, items);
        }

        /// <summary>
        /// A query WHERE clause joined by "OR". Natural language alternative, C# = || operator, VB.NET = OR Keyword
        /// </summary>
        /// <example>
        /// See <see cref="tgConjunction"/> Enumeration.
        /// <code>
        /// emps.Query.Where
        /// (
        ///		emps.Query.Or
        ///		(
        ///			emps.Query.LastName == "Doe",
        ///			emps.Query.FirstName.Like("J%")
        ///		),
        ///		emps.Query.Age.LessThen(50)
        /// );
        /// </code>
        /// </example>
        /// <param name="items">The list of objects to place in the where statement.</param>
        /// <returns>An esDynamicQueryTransport containing a where clause.</returns>
        [Obsolete("For more readable code use '||' in C# or 'OrElse' in VB.NET rather than this method")]
        public List<tgComparison> Or(params object[] items)
        {
            return ProcessWhereItems(tgConjunction.Or, items);
        }

        /// <summary>
        /// A query WHERE clause joined by "OR NOT". Natural language alternative, C# = || ! operator, VB.NET = OR NOT Keyword
        /// </summary>
        /// <example>
        /// See <see cref="tgConjunction"/> Enumeration.
        /// <code>
        /// emps.Query.Where
        /// (
        ///		emps.Query.Or
        ///		(
        ///			emps.Query.LastName == "Doe",
        ///			emps.Query.FirstName.Like("J%")
        ///		),
        ///		emps.Query.Age.LessThen(50)
        /// );
        /// </code>
        /// </example>
        /// <param name="items">The list of objects to place in the where statement.</param>
        /// <returns>An esDynamicQueryTransport containing a where clause.</returns>
        public List<tgComparison> OrNot(params object[] items)
        {
            return ProcessWhereItems(tgConjunction.OrNot, items);
        }

        /// <summary>
        /// Your ORDER BY statement.
        /// </summary>
        /// <example>
        /// <code>
        /// emps.Query.OrderBy
        /// (
        ///		emps.Query.LastName.Descending
        /// );
        /// </code>
        /// </example>
        /// <param name="orderByItems">The list of objects to place in the OrderBy statement.</param>
        /// <returns>An esDynamicQueryTransport containing a OrderBy clause.</returns>
        public tgDynamicQuerySerializable OrderBy(params tgOrderByItem[] orderByItems)
        {
            if (this.orderByItems == null)
            {
                this.orderByItems = new List<tgOrderByItem>();
            }

            foreach (tgOrderByItem orderByItem in orderByItems)
            {
                this.orderByItems.Add(orderByItem);
            }
            return this;
        }

        /// <summary>
        /// Your ORDER BY statement
        /// </summary>
        /// <example>
        /// You can order by an aggregate by passing the aggregate alias.
        /// See <see cref="tgOrderByDirection"/> Enumeration.
        /// <code>
        /// emps.Query.OrderBy
        /// (
        ///		emps.Query.LastName.Descending
        /// );
        /// </code>
        /// </example>
        /// <param name="columnName">The column to place in the OrderBy statement.</param>
        /// <param name="direction">Sort direction.</param>
        /// <returns>An esDynamicQueryTransport containing a OrderBy clause.</returns>
        public tgDynamicQuerySerializable OrderBy(string columnName, tgOrderByDirection direction)
        {
            if (this.orderByItems == null)
            {
                this.orderByItems = new List<tgOrderByItem>();
            }

            tgOrderByItem item = new tgOrderByItem();

            item.Expression = new tgExpression();
            item.Expression.Query = this;
            item.Expression.Column.Name = columnName;
            item.Expression.Column.Alias = columnName;
            item.Direction = direction;

            this.orderByItems.Add(item);

            return this;
        }

        /// <summary>
        /// Your GROUP BY statement
        /// </summary>
        /// <example>
        /// <code>
        /// emps.Query.es.CountAll = true;
        /// emps.Query.Select
        /// (
        ///		emps.Query.LastName,
        ///		emps.Query.FirstName
        /// )
        /// .GroupBy
        /// (
        ///		emps.Query.LastName,
        ///		emps.Query.FirstName
        /// );
        /// </code>
        /// </example>
        /// <param name="columns">The columns to place in the OrderBy statement.</param>
        /// <returns>An esDynamicQueryTransport containing a GroupBy clause.</returns>
        public tgDynamicQuerySerializable GroupBy(params tgQueryItem[] columns)
        {
            if (this.groupByItems == null)
            {
                this.groupByItems = new List<tgGroupByItem>();
            }

            foreach (tgQueryItem item in columns)
            {
                tgGroupByItem groupBy = new tgGroupByItem();
                groupBy.Expression = item;
                this.groupByItems.Add(groupBy);
            }
            return this;
        }

        /// <summary>
        /// Your GROUP BY statement
        /// </summary>
        /// <example>
        /// <code>
        /// emps.Query.es.CountAll = true;
        /// emps.Query.Select
        /// (
        ///		emps.Query.LastName,
        ///		emps.Query.FirstName
        /// )
        /// .GroupBy
        /// (
        ///		emps.Query.LastName,
        ///		emps.Query.FirstName
        /// );
        /// </code>
        /// </example>
        /// <param name="columns">The columns to place in the OrderBy statement.</param>
        /// <returns>An esDynamicQueryTransport containing a GroupBy clause.</returns>
        public tgDynamicQuerySerializable GroupBy(params string[] columns)
        {
            if (this.groupByItems == null)
            {
                this.groupByItems = new List<tgGroupByItem>();
            }

            foreach (string column in columns)
            {
                tgGroupByItem item = new tgGroupByItem();
                item.Expression = new tgExpression();
                item.Expression.Column.Name = column;
                item.Expression.Column.Alias = column;
                item.Expression.Query = this;
                this.groupByItems.Add(item);
            }
            return this;
        }

        /// <summary>
        /// Allows you to pull a SubQuery out of this Query
        /// </summary>
        /// <typeparam name="T">The Type of the desired Query</typeparam>
        /// <returns>The Query if found, otherwise null</returns>
        public T GetQuery<T>() where T : tgDynamicQuerySerializable
        {
            Type type = typeof(T);
            string strType = type.ToString();

            foreach (tgJoinItem joinItem in this.joinItems)
            {
                tgJoinItem.esJoinItemData data = (tgJoinItem.esJoinItemData)joinItem;

                if (data.Query.GetType().ToString() == strType)
                {
                    return (T)data.Query;
                }
            }

            if (this.GetType().ToString() == strType)
            {
                return (T)this;
            }

            return null;
        }

        #region Helper Routine
        private List<tgComparison> ProcessWhereItems(tgConjunction conj, params object[] theItems)
        {
            List<tgComparison> items = new List<tgComparison>();

            items.Add(new tgComparison(tgParenthesis.Open));

            bool first = true;

            tgComparison whereItem;
            int count = theItems.Length;

            for (int i = 0; i < count; i++)
            {
                object o = theItems[i];

                whereItem = o as tgComparison;
                if (whereItem != null)
                {
                    if (!first)
                    {
                        items.Add(new tgComparison(conj));
                    }
                    items.Add(whereItem);
                    first = false;
                }
                else
                {
                    List<tgComparison> listItem = o as List<tgComparison>;
                    if (listItem != null)
                    {
                        if (!first)
                        {
                            items.Add(new tgComparison(conj));
                        }
                        items.AddRange(listItem);
                        first = false;
                    }
                    else
                    {
                        if (o is string)
                        {
                            whereItem = new tgComparison(this);
                            whereItem.ColumnName = o as string;
                            whereItem.IsLiteral = true;
                            items.Add(whereItem);
                        }
                    }
                }
            }

            items.Add(new tgComparison(tgParenthesis.Close));

            return items;
        }

        protected void HookupWithNoLock(tgDynamicQuerySerializable query)
        {
            if (query.tg.WithNoLock.HasValue && query.tg.WithNoLock == true)
            {
                if (query.queries != null)
                {
                    foreach (tgDynamicQuerySerializable nestedQuery in query.queries.Values)
                    {
                        nestedQuery.tg.WithNoLock = true;

                        HookupWithNoLock(nestedQuery);
                    }
                }
            }
        }
        #endregion Helper Routine

        #region es

        /// <summary>
        /// This is to help hide some details from Intellisense.
        /// </summary>
        public DynamicQueryProps tg
        {
            get
            {
                if (this.props == null)
                {
                    this.props = new DynamicQueryProps(this);
                }

                return this.props;
            }
        }

        [NonSerialized]
        private DynamicQueryProps props;

        /// <summary>
        /// The Dynamic Query properties.
        /// </summary>
        public class DynamicQueryProps
        {
            /// <summary>
            /// The Dynamic Query properties.
            /// </summary>
            /// <param name="query">The esDynamicQueryTransport's properties.</param>
            public DynamicQueryProps(tgDynamicQuerySerializable query)
            {
                this.dynamicQuery = query;
            }

            /// <summary>
            /// string QuerySource.
            /// </summary>
            /// <returns>string QuerySource.</returns>
            public string QuerySource
            {
                get { return this.dynamicQuery.querySource; }
                set { this.dynamicQuery.querySource = value; }
            }

            /// <summary>
            /// string JoinAlias.
            /// </summary>
            /// <returns>string JoinAlias.</returns>
            public string JoinAlias
            {
                get { return this.dynamicQuery.joinAlias; }
                set { this.dynamicQuery.joinAlias = value; }
            }

            /// <summary>
            /// tgConjunction DefaultConjunction.
            /// </summary>
            /// <returns>tgConjunction DefaultConjunction.</returns>
            public tgConjunction DefaultConjunction
            {
                get { return this.dynamicQuery.defaultConjunction; }
                set { this.dynamicQuery.defaultConjunction = value; }
            }

            /// <summary>
            /// This will limit the number of rows returned, after sorting.
            /// Setting Top to 10 will return the top ten rows after sorting.
            /// </summary>
            public int? Top
            {
                get { return this.dynamicQuery.top; }
                set { this.dynamicQuery.top = value; }
            }

            /// <summary>
            /// This will use the WITH (NOLOCK) syntax on all tables joined in the query. Currently
            /// only implemented for Microsoft SQL Server.
            /// </summary>
            public bool? WithNoLock
            {
                get { return this.dynamicQuery.withNoLock; }
                set { this.dynamicQuery.withNoLock = value; }
            }

            /// <summary>
            /// Used in SubQueries. The Any qualifier works just like the In except it allows for >, >=, 
            /// <, <= as well as the = (In) and != (Not In) operators
            /// </summary>
            public bool Any
            {
                get { return this.dynamicQuery.subquerySearchCondition == tgSubquerySearchCondition.Any; }
                set { this.dynamicQuery.subquerySearchCondition = tgSubquerySearchCondition.Any; }
            }

            /// <summary>
            /// Used in SubQueries. Used like 'Any' except for one key exception - any operator applied must be true for
            /// ALL the values returned in our subquery.
            /// </summary>
            public bool All
            {
                get { return this.dynamicQuery.subquerySearchCondition == tgSubquerySearchCondition.All; }
                set { this.dynamicQuery.subquerySearchCondition = tgSubquerySearchCondition.All; }
            }

            /// <summary>
            /// Used in SubQueries. The word SOME is an alias for ANY, and may be used anywhere that ANY is used. 
            /// The SQL standard defines these two words with the same meaning to overcome a limitation in the English language, 
            /// particularly for inequality comparisons.
            /// </summary>
            public bool Some
            {
                get { return this.dynamicQuery.subquerySearchCondition == tgSubquerySearchCondition.Some; }
                set { this.dynamicQuery.subquerySearchCondition = tgSubquerySearchCondition.Some; }
            }

            /// <summary>
            /// This will retrieve a specific row number from a select.
            /// This is useful when paging large sets of data
            /// </summary>
            public int? PageNumber
            {
                get { return this.dynamicQuery.pageNumber; }
                set { this.dynamicQuery.pageNumber = value; }
            }

            /// <summary>
            /// This will retrieve a specific row number from a select.
            /// This is useful when paging large sets of data
            /// </summary>
            public int? PageSize
            {
                get { return this.dynamicQuery.pageSize; }
                set { this.dynamicQuery.pageSize = value; }
            }

            /// <summary>
            /// Setting Distinct = True will elimate duplicate rows from the data.
            /// </summary>
            public bool Distinct
            {
                get { return this.dynamicQuery.distinct; }
                set { this.dynamicQuery.distinct = value; }
            }

            /// <summary>
            /// If true, add a COUNT(*) Aggregate to the selected columns list.
            /// </summary>
            public bool CountAll
            {
                get { return this.dynamicQuery.countAll; }
                set 
                { 
                    this.dynamicQuery.countAll = value;
                    if (String.IsNullOrEmpty(this.dynamicQuery.countAllAlias))
                    {
                        this.dynamicQuery.countAllAlias = "Count";
                    }
                }
            }

            /// <summary>
            /// If CountAll is set to true, use this to add a user-friendly column name.
            /// </summary>
            public string CountAllAlias
            {
                get { return this.dynamicQuery.countAllAlias; }
                set { this.dynamicQuery.countAllAlias = value; }
            }

            /// <summary>
            /// If true, add WITH ROLLUP to the GROUP BY clause.
            /// </summary>
            /// <example>
            /// <code>
            /// emps.Query.es.WithRollup = true;
            /// </code>
            /// </example>
            public bool WithRollup
            {
                get { return this.dynamicQuery.withRollup; }
                set { this.dynamicQuery.withRollup = value; }
            }

            /// <summary>
            /// Returns a string containing the Sql from the last Query.Load().
            /// Useful for testing and debugging query syntax.
            /// </summary>
            public string LastQuery
            {
                get { return this.dynamicQuery.lastQuery; }
                set { this.dynamicQuery.lastQuery = value; }
            }

            /// <summary>
            /// tgConnection Connection.
            /// </summary>
            //public tgConnection Connection
            //{
            //    get
            //    {
            //        if (this.dynamicQuery.connection == null)
            //        {
            //            this.dynamicQuery.connection = new tgConnection();

            //            if (tgConnection.ConnectionService != null)
            //            {
            //                this.dynamicQuery.connection.Name = tgConnection.ConnectionService.GetName();
            //            }
            //            else
            //            {
            //                string connName = this.dynamicQuery.GetConnectionName();
            //                if (connName != null)
            //                {
            //                    this.dynamicQuery.connection.Name = connName;
            //                }
            //            }
            //        }

            //        return this.dynamicQuery.connection;
            //    }
            //    set { this.dynamicQuery.connection = value; }
            //}

            private tgDynamicQuerySerializable dynamicQuery;
        }
        #endregion

        #region Serializer


        /// <summary>
        /// This class will allow you to serialize via the DataContract manaually, but normally you won't have to use this class
        /// </summary>
        static public class SerializeHelper
        {
            static public List<System.Type> GetKnownTypes(tgDynamicQuerySerializable query)
            {
                List<System.Type> types = new List<Type>();
                GetKnownTypes(query, types);
                return types;
            }

            static public DataContractSerializer GetSerializer(tgDynamicQuerySerializable query)
            {
                List<System.Type> types = GetKnownTypes(query);
 
                return new DataContractSerializer(query.GetType(), query.GetQueryName(),
                    "http://www.entityspaces.net", types, Int32.MaxValue, false, true, null);
            }

            static public DataContractSerializer GetSerializer(tgDynamicQuerySerializable query, Type type)
            {
                List<System.Type> types = GetKnownTypes(query);

                return new DataContractSerializer(type, query.GetQueryName(),
                    "http://www.entityspaces.net", types, Int32.MaxValue, false, true, null);
            }

            static public DataContractSerializer GetSerializer(tgDynamicQuerySerializable query, List<System.Type> knownTypes)
            {
                return new DataContractSerializer(query.GetType(), query.GetQueryName(),
                    "http://www.entityspaces.net", knownTypes, Int32.MaxValue, false, true, null);
            }

            static public DataContractSerializer GetSerializer(tgDynamicQuerySerializable query, Type type, List<System.Type> knownTypes)
            {
                return new DataContractSerializer(type, query.GetQueryName(),
                    "http://www.entityspaces.net", knownTypes, Int32.MaxValue, false, true, null);
            }

            static public string ToXml(tgDynamicQuerySerializable query)
            {
                string xml = "";

                DataContractSerializer dcs = GetSerializer(query);

                using (MemoryStream ms = new MemoryStream())
                {
                    dcs.WriteObject(ms, query);
                    ms.Seek(0, SeekOrigin.Begin);

                    using (StreamReader sr = new StreamReader(ms))
                    {
                        xml = sr.ReadToEnd();
                    }
                }

                return xml;
            }

            static public tgDynamicQuerySerializable FromXml(string xml, Type type)
            {
                tgDynamicQuerySerializable query = null;

                using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.MaxCharactersFromEntities = long.MaxValue;
                    settings.MaxCharactersInDocument = long.MaxValue;

                    using (var reader = System.Xml.XmlDictionaryReader.Create(memoryStream, settings))
                    {
                        // Deserialize
                        DataContractSerializer serializer = new DataContractSerializer(type, type.Name, "http://www.entityspaces.net");
                        query = serializer.ReadObject(reader) as tgDynamicQuerySerializable;
                    }
                }

                return query;
            }

            static public tgDynamicQuerySerializable FromXml(string xml, Type type, List<System.Type> knownTypes)
            {
                tgDynamicQuerySerializable query = null;

                using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.MaxCharactersFromEntities = long.MaxValue;
                    settings.MaxCharactersInDocument = long.MaxValue;

                    using (var reader = System.Xml.XmlDictionaryReader.Create(memoryStream, settings))
                    {
                        // Deserialize
                        DataContractSerializer serializer = new DataContractSerializer(type, type.Name, "http://www.entityspaces.net", 
                            knownTypes, Int32.MaxValue, false, true, null);
                        query = serializer.ReadObject(reader) as tgDynamicQuerySerializable;
                    }
                }

                return query;
            }

            static private void GetKnownTypes(tgDynamicQuerySerializable query, List<System.Type> types)
            {
                bool found = false;

                string strType = query.GetType().ToString();

                foreach (Type t in types)
                {
                    if (t.ToString() == strType)
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    types.Add(query.GetType());
                }

                foreach (tgDynamicQuerySerializable subQuery in query.queries.Values)
                {
                    GetKnownTypes(subQuery, types);
                }
            }
        }

        #endregion

        #region Internal Workings

        internal void AddSubOperator(tgExpression selectItem)
        {
            if (this.selectColumns == null)
            {
                this.selectColumns = new List<tgExpression>();
            }

            this.selectColumns.Add(selectItem);
        }

        internal void AddOrderBy(tgOrderByItem orderByItem)
        {
            if (this.orderByItems == null)
            {
                this.orderByItems = new List<tgOrderByItem>();
            }

            this.orderByItems.Add(orderByItem);
        }

        internal void AddWhere(tgComparison where)
        {
            if (this.whereItems == null)
            {
                this.whereItems = new List<tgComparison>();
            }

            this.whereItems.Add(where);
        }

        #endregion

        // Holds the name of the metadata maps that will eventually be turned into 
        // the real ones by esDynamicQuery
        [DataMember(Name = "Queries", EmitDefaultValue = false)]
        internal protected Dictionary<string, tgDynamicQuerySerializable> queries = new Dictionary<string, tgDynamicQuerySerializable>();

        // Filled in by SelectAllExcept()
        [DataMember(Name = "SelectAllExcept", EmitDefaultValue = false)]
        internal protected List<tgQueryItem> m_selectAllExcept;

        // Set to true by SelectAll()
        [DataMember(Name = "SelectAll", EmitDefaultValue = false)]
        internal protected bool m_selectAll = false;

        [DataMember(Name = "DefaultConjunction", EmitDefaultValue = false)]
        internal tgConjunction defaultConjunction = tgConjunction.And;

        [DataMember(Name = "QuerySource", EmitDefaultValue = false)]
        internal string querySource;

        [DataMember(Name = "DataID", EmitDefaultValue = false)]
        internal Guid dataID;

        [DataMember(Name = "Catalog", EmitDefaultValue = false)]
        internal string catalog;

        [DataMember(Name = "Schema", EmitDefaultValue = false)]
        internal string schema;

        [DataMember(Name = "IsInSubQuery", EmitDefaultValue = false)]
        internal bool isInSubQuery;

        [DataMember(Name = "IsExplicitParenthesis", EmitDefaultValue = false)]
        internal bool isExplicitParenthesis;

        [DataMember(Name = "SubQueryAlias", EmitDefaultValue = false, IsRequired = false)]
        internal string subQueryAlias = string.Empty;

        [DataMember(Name = "SelectColumns", Order = 99, EmitDefaultValue = false)]
        internal List<tgExpression> selectColumns;

        [DataMember(Name = "FromQuery", Order = 100, EmitDefaultValue = false)]
        internal tgDynamicQuerySerializable fromQuery;

        [DataMember(Name = "JoinItems", Order = 101, EmitDefaultValue = false)]
        internal List<tgJoinItem> joinItems;

        [DataMember(Name = "WhereItems", Order = 102, EmitDefaultValue = false)]
        internal List<tgComparison> whereItems;

        [DataMember(Name = "HavingItems", Order = 103, EmitDefaultValue = false)]
        internal List<tgComparison> havingItems;

        [DataMember(Name = "OrderByItems", Order = 104, EmitDefaultValue = false)]
        internal List<tgOrderByItem> orderByItems;

        [DataMember(Name = "GroupByItems", Order = 105, EmitDefaultValue = false)]
        internal List<tgGroupByItem> groupByItems;

        [DataMember(Name = "SetOperations", Order = 106, EmitDefaultValue = false)]
        internal List<tgSetOperation> setOperations;

        /// <summary>
        /// Used by derived classes
        /// </summary>
        [DataMember(Name = "Distinct", EmitDefaultValue = false)] 
        internal bool distinct;

        /// <summary>
        /// Used by derived classes
        /// </summary>
        [DataMember(Name = "SubquerySearchCondition", EmitDefaultValue = false)]
        internal tgSubquerySearchCondition subquerySearchCondition; 

        /// <summary>
        /// Used by derived classes
        /// </summary>
        [DataMember(Name = "Top", EmitDefaultValue = false)]
        internal int? top;

        /// <summary>
        /// Used by derived classes
        /// </summary>
        [DataMember(Name = "WithNoLock", EmitDefaultValue = false)]
        internal bool? withNoLock;

        /// <summary>
        /// Used by derived classes
        /// </summary>
        [DataMember(Name = "PageNumber", EmitDefaultValue = false)]
        internal int? pageNumber;

        /// <summary>
        /// Used by derived classes
        /// </summary>
        [DataMember(Name = "PageSize", EmitDefaultValue = false)]
        internal int? pageSize;

        /// <summary>
        /// Used by derived classes
        /// </summary>
        [DataMember(Name = "CountAll", EmitDefaultValue = false)]
        internal bool countAll;

        /// <summary>
        /// Used by derived classes
        /// </summary>
        [DataMember(Name = "CountAllAlias", EmitDefaultValue = false)]
        internal string countAllAlias;

        /// <summary>
        /// Used by derived classes
        /// </summary>
        [DataMember(Name = "WithRollup", EmitDefaultValue = false)]
        internal bool withRollup;

        /// <summary>
        /// Must be supplied when using JOIN's
        /// </summary>
        [DataMember(Name = "JoinAlias", EmitDefaultValue = false)]
        internal string joinAlias = " ";

        [DataMember(Name = "SubQueryNames", EmitDefaultValue = false)]
        internal string subQueryNames;

        [DataMember(Name = "Skip", EmitDefaultValue = false)]
        internal int? skip;

        [DataMember(Name = "Take", EmitDefaultValue = false)]
        internal int? take;

        internal string lastQuery;
        private object providerMetadata;
        private object columns;
        private tgComparison prevComp;

        #region IDynamicQueryTransportInternal Members

        Guid IDynamicQuerySerializableInternal.DataID
        {
            get { return this.dataID; }
            set { this.dataID = value; }
        }

        string IDynamicQuerySerializableInternal.Catalog
        {
            get { return this.catalog; }
            set { this.catalog = value; }
        }

        string IDynamicQuerySerializableInternal.Schema
        {
            get { return this.schema; }
            set { this.schema = value; }
        }

        bool IDynamicQuerySerializableInternal.IsInSubQuery
        {
            get { return this.isInSubQuery; }
            set { this.isInSubQuery = value; }
        }

        bool IDynamicQuerySerializableInternal.HasSetOperation
        {
            get 
            { 
                return setOperations != null && setOperations.Count > 0; 
            }
        }

        string IDynamicQuerySerializableInternal.SubQueryAlias
        {
            get { return this.subQueryAlias; }
        }

        string IDynamicQuerySerializableInternal.JoinAlias
        {
            get { return this.joinAlias; }
            set { this.joinAlias = value; }
        }

        string IDynamicQuerySerializableInternal.LastQuery
        {
            get { return this.lastQuery; }
            set { this.lastQuery = value; }
        }

        string IDynamicQuerySerializableInternal.QuerySource 
        {
            get { return this.querySource; }
            set { this.querySource = value; }
        }

        bool IDynamicQuerySerializableInternal.SelectAll 
        {
            get { return this.m_selectAll; } 
        }

        List<tgQueryItem> IDynamicQuerySerializableInternal.SelectAllExcept 
        {
            get { return this.m_selectAllExcept; } 
        }

        tgSubquerySearchCondition IDynamicQuerySerializableInternal.SubquerySearchCondition
        {
            get { return this.subquerySearchCondition; }
        }

        List<tgExpression> IDynamicQuerySerializableInternal.InternalSelectColumns
        {
            get { return this.selectColumns; }
            set { this.selectColumns = value; }
        }

        tgDynamicQuerySerializable IDynamicQuerySerializableInternal.InternalFromQuery
        {
            get { return this.fromQuery; }
        }

        List<tgJoinItem> IDynamicQuerySerializableInternal.InternalJoinItems
        {
            get { return this.joinItems; }
        }

        List<tgComparison> IDynamicQuerySerializableInternal.InternalWhereItems
        {
            get { return this.whereItems; }
        }

        List<tgOrderByItem> IDynamicQuerySerializableInternal.InternalOrderByItems
        {
            get { return this.orderByItems; }
            set { this.orderByItems = value; }
        }

        List<tgComparison> IDynamicQuerySerializableInternal.InternalHavingItems
        {
            get { return this.havingItems; }
        }

        List<tgGroupByItem> IDynamicQuerySerializableInternal.InternalGroupByItems
        {
            get { return this.groupByItems; }
            set { this.groupByItems = value; }
        }

        List<tgSetOperation> IDynamicQuerySerializableInternal.InternalSetOperations
        {
            get { return this.setOperations; }
            set { this.setOperations = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        object IDynamicQuerySerializableInternal.ProviderMetadata 
        {
            get { return this.providerMetadata; }
            set { this.providerMetadata = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        object IDynamicQuerySerializableInternal.Columns 
        {
            get { return this.columns; } 
            set { this.columns = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        void IDynamicQuerySerializableInternal.HookupProviderMetadata(tgDynamicQuerySerializable query)
        {
            AddQueryToList(query);
        }

        /// <summary>
        /// 
        /// </summary>
        Dictionary<string, tgDynamicQuerySerializable> IDynamicQuerySerializableInternal.queries 
        {
            get { return this.queries; }
        }

        /// <summary>
        /// The number of rows to skip in the result set (starting from the beginning)
        /// </summary>
        int? IDynamicQuerySerializableInternal.Skip
        {
            get { return this.skip; }
        }

        /// <summary>
        /// The number of rows to take from the result set (starting from the Skip)
        /// </summary>
        int? IDynamicQuerySerializableInternal.Take
        {
            get { return this.take; }
        }

        #endregion
    }
}
