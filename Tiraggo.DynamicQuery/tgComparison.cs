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
using System.Runtime.Serialization;

namespace Tiraggo.DynamicQuery
{
    /// <summary>
    /// The tgComparison class is dynamically created by your BusinessEntity's
    /// DynamicQuery mechanism.
    /// This class is mostly used by the EntitySpaces architecture, not the programmer.
    /// </summary>
    /// <example>
    /// You will not call tgComparison directly, but will be limited to use as
    /// in the example below, or to the many uses posted here:
    /// <code>
    /// http://www.entityspaces.net/portal/QueryAPISamples/tabid/80/Default.aspx
    /// </code>
    /// This will be the extent of your use of the tgComparison class:
    /// <code>
    /// .Where
    /// (
    ///		emps.Query.LastName.Like("D%"),
    ///		emps.Query.Age == 30
    /// );
    /// </code>
    /// </example>
    [Serializable]
    [DataContract(Namespace = "tg", IsReference = true)]
    public class tgComparison
    {
        /// <summary>
        /// The tgComparison class is dynamically created by your
        /// BusinessEntity's DynamicQuery mechanism.
        /// </summary>
        public tgComparison(tgDynamicQuerySerializable query) 
        {
            this.data.Query = query;
        }

        /// <summary>
        /// The tgComparison class is dynamically created by your
        /// BusinessEntity's DynamicQuery mechanism.
        /// See <see cref="tgParenthesis"/> Enumeration.
        /// </summary>
        /// <param name="paren">The tgParenthesis passed in via DynamicQuery</param>
        public tgComparison(tgParenthesis paren) 
        {
            this.data.Parenthesis = paren;
        }

        /// <summary>
        /// The tgComparison class is dynamically created by your
        /// BusinessEntity's DynamicQuery mechanism.
        /// See <see cref="tgConjunction"/> Enumeration.
        /// </summary>
        /// <param name="conj">The tgConjunction passed in via DynamicQuery</param>
        public tgComparison(tgConjunction conj)
        {
            this.data.Conjunction = conj;
        }

        /// <summary>
        /// Or | (to use in Where clauses).
        /// </summary>
        /// <example>
        /// The operators provide an alternative, natural syntax for DynamicQueries.
        /// <code>
        ///	emps.Query.Where
        /// (
        ///		emps.Query.LastName == "Doe" |
        ///		emps.Query.LastName == "Smith"
        /// );
        /// </code>
        /// </example>
        /// <param name="c1">First tgComparison passed in via DynamicQuery</param>
        /// <param name="c2">Second tgComparison passed in via DynamicQuery</param>
        /// <returns>The tgComparison returned to DynamicQuery</returns>
        public static tgComparison operator |(tgComparison c1, tgComparison c2)
        {
            return HandleOperator(c1, c2, tgConjunction.Or);
        }

        /// <summary>
        /// And &amp; (to use in Where clauses).
        /// </summary>
        /// <example>
        /// The operators provide an alternative, natural syntax for DynamicQueries.
        /// <code>
        ///	emps.Query.Where
        /// (
        ///		emps.Query.LastName == "Doe" &amp;
        ///		emps.Query.FirstName == "Jane"
        /// );
        /// </code>
        /// </example>
        /// <param name="c1">First tgComparison passed in via DynamicQuery</param>
        /// <param name="c2">Second tgComparison passed in via DynamicQuery</param>
        /// <returns>The tgComparison returned to DynamicQuery</returns>
        public static tgComparison operator &(tgComparison c1, tgComparison c2)
        {
            return HandleOperator(c1, c2, tgConjunction.And);
        }

        /// <summary>
        /// NOT ! (to use in Where clauses).
        /// </summary>
        /// <example>
        /// The operators provide an alternative, natural syntax for DynamicQueries.
        /// <code>
        ///	emps.Query.Where
        /// (
        ///		emps.Query.LastName == "Doe" &amp;
        ///		emps.Query.FirstName == "Jane"
        /// );
        /// </code>
        /// </example>
        /// <param name="c1">The tgComparison to negate</param>
        /// <returns>The tgComparison returned to DynamicQuery</returns>
        public static tgComparison operator !(tgComparison comparison)
        {
            comparison.not = true;
            return comparison;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        private static tgComparison HandleOperator(tgComparison c1, tgComparison c2, tgConjunction op)
        {
            List<tgComparison> exp = null;

            if (c1.data.WhereExpression == null)
            {
                c1.data.WhereExpression = new List<tgComparison>();
                exp = c1.data.WhereExpression;

                exp.Add(new tgComparison(tgParenthesis.Open));
                exp.Add(c1);
            }
            else
            {
                exp = c1.data.WhereExpression;
                exp.Insert(0, new tgComparison(tgParenthesis.Open));
            }

            tgConjunction conj = op;

            if (c2.not)
            {
                switch (op)
                {
                    case tgConjunction.And:
                        conj = tgConjunction.AndNot;
                        break;

                    case tgConjunction.Or:
                        conj = tgConjunction.OrNot;
                        break;
                }
            }

            exp.Add(new tgComparison(conj));

            if (c2.data.WhereExpression == null)
            {
                exp.Add(c2);
            }
            else
            {
                exp.AddRange(c2.data.WhereExpression);
            }

            exp.Add(new tgComparison(tgParenthesis.Close));

            return c1;
        }

        /// <summary>
        /// Force true (to use in Where clauses).
        /// </summary>
        public static bool operator true(tgComparison c1)
        {
            return false;
        }

        ///// <summary>
        ///// Force false (to use in Where clauses).
        ///// </summary>
        public static bool operator false(tgComparison c1)
        {
            return false;
        }


        /// <summary>
        /// string ColumnName.
        /// </summary>
        internal string ColumnName
        {
            get { return data.Column.Name; }
            set { data.Column.Name = value; }
        }

        /// <summary>
        /// bool IsLiteral.
        /// </summary>
        internal bool IsLiteral
        {
            get { return data.IsLiteral; }
            set { data.IsLiteral = value; }
        }

        /// <summary>
        /// object value.
        /// </summary>
        internal object Value
        {
            get { return data.Value; }
            set { data.Value = value; }
        }

        /// <summary>
        /// Used for In() and NotIn()
        /// </summary>
        internal List<object> Values
        {
            get { return data.Values; }
            set { data.Values = value; }
        }

        /// <summary>
        /// Used whenever a value is needed on the right hand side of an operator in a Where clause.
        /// </summary>
        internal string ComparisonColumn
        {
            get { return data.ComparisonColumn.Name; }
            set { data.ComparisonColumn.Name = value; }
        }


        /// <summary>
        /// Used only when <see cref="tgComparisonOperand.Between"/> and the 2nd date is another column in the database.
        /// </summary>
        internal string ComparisonColumn2
        {
            get { return data.ComparisonColumn2.Name; }
            set { data.ComparisonColumn2.Name = value; }
        }

        /// <summary>
        /// tgComparisonOperand Operand.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        internal tgComparisonOperand Operand
        {
            get { return data.Operand; }
            set { data.Operand = value; }
        }

        /// <summary>
        /// tgQuerySubOperator
        /// See <see cref="tgQuerySubOperator"/> Enumeration.
        /// </summary>
        internal List<tgQuerySubOperator> SubOperators
        {
            get { return data.SubOperators; }
            set { data.SubOperators = value; }
        }

        /// <summary>
        /// tgConjunction Conjunction.
        /// See <see cref="tgConjunction"/> Enumeration.
        /// </summary>
        internal tgConjunction Conjunction
        {
            get { return data.Conjunction; }
            set { data.Conjunction = value; }
        }

        /// <summary>
        /// tgParenthesis Parenthesis.
        /// See <see cref="tgParenthesis"/> Enumeration.
        /// </summary>
        internal tgParenthesis Parenthesis
        {
            get { return data.Parenthesis; }
            set { data.Parenthesis = value; }
        }

        /// <summary>
        /// The first date when <see cref="tgComparisonOperand.Between"/> is used and the value is 
        /// not another column in the table but a literal value being passed in.
        /// </summary>
        internal object BetweenBegin
        {
            get { return data.BetweenBegin; }
            set { data.BetweenBegin = value; }
        }

        /// <summary>
        /// The second date when <see cref="tgComparisonOperand.Between"/> is used and the value is 
        /// not another column in the table but a literal value being passed in.
        /// </summary>
        internal object BetweenEnd
        {
            get { return data.BetweenEnd; }
            set { data.BetweenEnd = value; }
        }

        /// <summary>
        /// char LikeEscape.
        /// </summary>
        internal char LikeEscape
        {
            get { return data.LikeEscape; }
            set { data.LikeEscape = value; }
        }

        /// <summary>
        /// Whether the tgComparison goes first in the expression
        /// </summary>
        internal bool ItemFirst
        {
            get { return data.ItemFirst; }
            set { data.ItemFirst = value; }
        }

        /// <summary>
        /// Used internally by EntitySpaces to make the <see cref="tgComparison"/> classes data available to the
        /// EntitySpaces data providers.
        /// </summary>
        [Serializable]
        [DataContract(Namespace = "tg", IsReference = true)]
        public class tgComparisonData
        {
            /// <summary>
            /// bool IsConjunction.
            /// </summary>
            public bool IsConjunction
            {
                get
                {
                    return (this.Conjunction == tgConjunction.Unassigned) ? false : true;
                }
            }

            /// <summary>
            /// bool IsParenthesis.
            /// </summary>
            public bool IsParenthesis
            {
                get
                {
                    return (this.Parenthesis == tgParenthesis.Unassigned) ? false : true;
                }
            }

            /// <summary>
            /// bool HasComparisonColumn.
            /// </summary>
            public bool HasComparisonColumn
            {
                get
                {
                    return (this.ComparisonColumn.Name != null || this.ComparisonColumn2.Name != null) ? true : false;
                }
            }

            /// <summary>
            /// bool HasExpression.
            /// </summary>
            public bool HasExpression
            {
                get
                {
                    return (this.Expression != null) ? true : false;
                }
            }

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "ParentQuery", Order = 99, EmitDefaultValue = false)]
            public tgDynamicQuerySerializable Query;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "Column", EmitDefaultValue = false)]
            public tgColumnItem Column;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "IsLiteral", EmitDefaultValue = false)]
            public bool IsLiteral;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "ComparisonColumn", EmitDefaultValue = false)]
            public tgColumnItem ComparisonColumn;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "ComparisonColumn2", EmitDefaultValue = false)]
            public tgColumnItem ComparisonColumn2;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "Expression", EmitDefaultValue = false)]
            public tgMathmaticalExpression Expression;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "Value", EmitDefaultValue = false)]
            public object Value;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "Values", EmitDefaultValue = false)]
            public List<object> Values;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "Operand", EmitDefaultValue = false)]
            public tgComparisonOperand Operand;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "Conjunction", EmitDefaultValue = false)]
            public tgConjunction Conjunction;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "Parenthesis", EmitDefaultValue = false)]
            public tgParenthesis Parenthesis;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "BetweenBegin", EmitDefaultValue = false)]
            public object BetweenBegin;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "BetweenEnd", EmitDefaultValue = false)]
            public object BetweenEnd;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "LikeEscape", EmitDefaultValue = false)]
            public char LikeEscape;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "SubOperators", EmitDefaultValue = false)]
            public List<tgQuerySubOperator> SubOperators;

            /// <summary>
            /// Internal data used by <see cref="tgComparison"/> and accessed by the EntitySpaces data providers.
            /// </summary>
            [DataMember(Name = "WhereExpression", EmitDefaultValue = false)]
            public List<tgComparison> WhereExpression;

            /// <summary>
            /// Whether the tgQueryItem goes first in the expression
            /// </summary>
            [DataMember(Name = "ItemFirst", EmitDefaultValue = false)]
            public bool ItemFirst = true;
        }

        /// <summary>
        /// The data is hidden from intellisense, however, the providers, can typecast
        /// the tgComparison and get to the real data without properties having to 
        /// be exposed thereby cluttering up the intellisense
        /// </summary>
        /// <param name="where">The tgComparison to cast</param>
        /// <returns>The tgComparisonData interface</returns>
        public static explicit operator tgComparisonData(tgComparison where)
        {
            return where.data;
        }

        [DataMember(Name = "Data", EmitDefaultValue = false)]
        internal tgComparisonData data = new tgComparisonData();

        [NonSerialized]
        // this lives for just a fraction during the queries build process
        private bool not;
    }
}
