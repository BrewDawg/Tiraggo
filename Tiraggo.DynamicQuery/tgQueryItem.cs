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
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Tiraggo.DynamicQuery
{
    /// <summary>
    /// The tgQueryItem class is dynamically created by your BusinessEntity's
    /// DynamicQuery mechanism.
    /// This class is mostly used by the EntitySpaces architecture, not the programmer.
    /// </summary>
    /// <example>
    /// You will not call tgQueryItem directly, but will be limited to use as
    /// in the example below, or to the many uses posted here:
    /// <code>
    /// http://www.entityspaces.net/portal/QueryAPISamples/tabid/80/Default.aspx
    /// </code>
    /// This will be the extent of your use of the tgQueryItem class:
    /// <code>
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
    ///		emps.Query.LastName
    /// )
    /// .OrderBy
    /// (
    ///		emps.Query.LastName.Descending,
    ///		emps.Query.FirstName.Ascending
    /// );
    /// </code>
    /// </example>
    [Serializable]
    [DataContract(Namespace = "tg", IsReference = true)]
    public class tgQueryItem
    {
        private tgQueryItem()
        {
            this.Expression = new tgMathmaticalExpression();
        }

        /// <summary>
        /// The tgQueryItem class is dynamically created by your
        /// BusinessEntity's DynamicQuery mechanism.
        /// </summary>
        /// <param name="query">The esDynamicQueryTransport passed in via DynamicQuery</param>
        /// <param name="columnName">The columnName passed in via DynamicQuery</param>
        /// <param name="datatype">The tgSystemType</param>
        public tgQueryItem(tgDynamicQuerySerializable query, string columnName, tgSystemType datatype)
        {
            this.query = query;
            this.Column.Query = query;
            this.Column.Name = columnName;
            this.Column.Query.tg.JoinAlias = query.tg.JoinAlias;
            this.Column.Datatype = datatype;
        }

        #region operators applied to other QueryItems (LiteralExpression)

        #region > operator literal overloads

        /// <summary>
        /// Greater Than &gt; (to use in Where clauses).
        /// </summary>
        /// <example>
        /// The operators provide an alternative, natural syntax for DynamicQueries.
        /// <code>
        ///	emps.Query.Where(emps.Query.BirthDate &gt; "2000-01-01");
        /// </code>
        /// </example>
        /// <param name="item">Passed in via DynamicQuery</param>
        /// <param name="value">Passed in via DynamicQuery</param>
        /// <returns>The tgComparison returned to DynamicQuery</returns>
        public static tgComparison operator >(tgQueryItem item, tgQueryItem value)
        {
            tgComparison wi = new tgComparison(item.query);
            wi.Operand = tgComparisonOperand.GreaterThan;

            wi.data.Column = item.Column;
            wi.data.ComparisonColumn = value.Column;

            wi.SubOperators = item.SubOperators;
            return wi;
        }

        private static tgComparison GreaterThan(tgQueryItem queryItem, object literal, tgSystemType literalType, bool itemFirst)
        {
            tgComparison wi = new tgComparison(queryItem.query);
            wi.Operand = tgComparisonOperand.GreaterThan;

            wi.data.Column = queryItem.Column;
            wi.data.Value = literal;
            wi.data.Expression = queryItem.Expression;
            wi.data.ItemFirst = itemFirst;

            wi.SubOperators = queryItem.SubOperators;
            return wi;
        }

        // tgSystemType.Boolean
        public static tgComparison operator >(tgQueryItem item1, bool literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgComparison operator >(bool literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgComparison operator >(tgQueryItem item1, byte literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Byte, true);
        }

        public static tgComparison operator >(byte literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgComparison operator >(tgQueryItem item1, char literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Char, true);
        }

        public static tgComparison operator >(char literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgComparison operator >(tgQueryItem item1, DateTime literal)
        {
            return GreaterThan(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgComparison operator >(DateTime literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgComparison operator >(tgQueryItem item1, double literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Double, true);
        }

        public static tgComparison operator >(double literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgComparison operator >(tgQueryItem item1, decimal literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgComparison operator >(decimal literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgComparison operator >(tgQueryItem item1, Guid literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Guid, true);
        }

        public static tgComparison operator >(Guid literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgComparison operator >(tgQueryItem item1, short literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Int16, true);
        }

        public static tgComparison operator >(short literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgComparison operator >(tgQueryItem item1, int literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Int32, true);
        }

        public static tgComparison operator >(int literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgComparison operator >(tgQueryItem item1, long literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Int64, true);
        }

        public static tgComparison operator >(long literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.Object
        public static tgComparison operator >(tgQueryItem item1, object literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Object, true);
        }

        public static tgComparison operator >(object literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Object, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgComparison operator >(tgQueryItem item1, sbyte literal)
        {
            return GreaterThan(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >(sbyte literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgComparison operator >(tgQueryItem item1, float literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Single, true);
        }

        public static tgComparison operator >(float literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.String
        public static tgComparison operator >(tgQueryItem item1, string literal)
        {
            return GreaterThan(item1, literal, tgSystemType.String, true);
        }

        public static tgComparison operator >(string literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.String, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgComparison operator >(tgQueryItem item1, ushort literal)
        {
            return GreaterThan(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >(ushort literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgComparison operator >(tgQueryItem item1, uint literal)
        {
            return GreaterThan(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >(uint literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgComparison operator >(tgQueryItem item1, ulong literal)
        {
            return GreaterThan(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >(ulong literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region < operator literal overloads

        /// <summary>
        /// Less Than &lt; (to use in Where clauses).
        /// </summary>
        /// <example>
        /// The operators provide an alternative, natural syntax for DynamicQueries.
        /// <code>
        ///	emps.Query.Where(emps.Query.BirthDate &lt; "2000-01-01");
        /// </code>
        /// </example>
        /// <param name="item">Passed in via DynamicQuery</param>
        /// <param name="value">Passed in via DynamicQuery</param>
        /// <returns>The tgComparison returned to DynamicQuery</returns>
        public static tgComparison operator <(tgQueryItem item, tgQueryItem value)
        {
            tgComparison wi = new tgComparison(item.query);
            wi.Operand = tgComparisonOperand.LessThan;

            wi.data.Column = item.Column;
            wi.data.ComparisonColumn = value.Column;

            wi.SubOperators = item.SubOperators;
            return wi;
        }

        private static tgComparison LessThan(tgQueryItem queryItem, object literal, tgSystemType literalType, bool itemFirst)
        {
            tgComparison wi = new tgComparison(queryItem.query);
            wi.Operand = tgComparisonOperand.LessThan;

            wi.data.Column = queryItem.Column;
            wi.data.Value = literal;
            wi.data.Expression = queryItem.Expression;
            wi.data.ItemFirst = itemFirst;

            wi.SubOperators = queryItem.SubOperators;
            return wi;
        }

        // tgSystemType.Boolean
        public static tgComparison operator <(tgQueryItem item1, bool literal)
        {
            return LessThan(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgComparison operator <(bool literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgComparison operator <(tgQueryItem item1, byte literal)
        {
            return LessThan(item1, literal, tgSystemType.Byte, true);
        }

        public static tgComparison operator <(byte literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgComparison operator <(tgQueryItem item1, char literal)
        {
            return LessThan(item1, literal, tgSystemType.Char, true);
        }

        public static tgComparison operator <(char literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgComparison operator <(tgQueryItem item1, DateTime literal)
        {
            return LessThan(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgComparison operator <(DateTime literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgComparison operator <(tgQueryItem item1, double literal)
        {
            return LessThan(item1, literal, tgSystemType.Double, true);
        }

        public static tgComparison operator <(double literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgComparison operator <(tgQueryItem item1, decimal literal)
        {
            return LessThan(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgComparison operator <(decimal literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgComparison operator <(tgQueryItem item1, Guid literal)
        {
            return LessThan(item1, literal, tgSystemType.Guid, true);
        }

        public static tgComparison operator <(Guid literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgComparison operator <(tgQueryItem item1, short literal)
        {
            return LessThan(item1, literal, tgSystemType.Int16, true);
        }

        public static tgComparison operator <(short literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgComparison operator <(tgQueryItem item1, int literal)
        {
            return LessThan(item1, literal, tgSystemType.Int32, true);
        }

        public static tgComparison operator <(int literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgComparison operator <(tgQueryItem item1, long literal)
        {
            return LessThan(item1, literal, tgSystemType.Int64, true);
        }

        public static tgComparison operator <(long literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.Object
        public static tgComparison operator <(tgQueryItem item1, object literal)
        {
            return LessThan(item1, literal, tgSystemType.Object, true);
        }

        public static tgComparison operator <(object literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Object, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgComparison operator <(tgQueryItem item1, sbyte literal)
        {
            return LessThan(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <(sbyte literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgComparison operator <(tgQueryItem item1, float literal)
        {
            return LessThan(item1, literal, tgSystemType.Single, true);
        }

        public static tgComparison operator <(float literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.String
        public static tgComparison operator <(tgQueryItem item1, string literal)
        {
            return LessThan(item1, literal, tgSystemType.String, true);
        }

        public static tgComparison operator <(string literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.String, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgComparison operator <(tgQueryItem item1, ushort literal)
        {
            return LessThan(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <(ushort literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgComparison operator <(tgQueryItem item1, uint literal)
        {
            return LessThan(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <(uint literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgComparison operator <(tgQueryItem item1, ulong literal)
        {
            return LessThan(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <(ulong literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region <= operator literal overloads

        /// <summary>
        /// Less Than or Equal &lt;= (to use in Where clauses).
        /// </summary>
        /// <example>
        /// The operators provide an alternative, natural syntax for DynamicQueries.
        /// <code>
        ///	emps.Query.Where(emps.Query.BirthDate &lt;= "2000-01-01");
        /// </code>
        /// </example>
        /// <param name="item">Passed in via DynamicQuery</param>
        /// <param name="value">Passed in via DynamicQuery</param>
        /// <returns>The tgComparison returned to DynamicQuery</returns>
        public static tgComparison operator <=(tgQueryItem item, tgQueryItem value)
        {
            tgComparison wi = new tgComparison(item.query);
            wi.Operand = tgComparisonOperand.LessThanOrEqual;

            wi.data.Column = item.Column;
            wi.data.ComparisonColumn = value.Column;

            wi.SubOperators = item.SubOperators;
            return wi;
        }

        private static tgComparison LessThanOrEqual(tgQueryItem queryItem, object literal, tgSystemType literalType, bool itemFirst)
        {
            tgComparison wi = new tgComparison(queryItem.query);
            wi.Operand = tgComparisonOperand.LessThanOrEqual;

            wi.data.Column = queryItem.Column;
            wi.data.Value = literal;
            wi.data.Expression = queryItem.Expression;
            wi.data.ItemFirst = itemFirst;

            wi.SubOperators = queryItem.SubOperators;
            return wi;
        }

        // tgSystemType.Boolean
        public static tgComparison operator <=(tgQueryItem item1, bool literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgComparison operator <=(bool literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgComparison operator <=(tgQueryItem item1, byte literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Byte, true);
        }

        public static tgComparison operator <=(byte literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgComparison operator <=(tgQueryItem item1, char literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Char, true);
        }

        public static tgComparison operator <=(char literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgComparison operator <=(tgQueryItem item1, DateTime literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgComparison operator <=(DateTime literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgComparison operator <=(tgQueryItem item1, double literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Double, true);
        }

        public static tgComparison operator <=(double literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgComparison operator <=(tgQueryItem item1, decimal literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgComparison operator <=(decimal literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgComparison operator <=(tgQueryItem item1, Guid literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Guid, true);
        }

        public static tgComparison operator <=(Guid literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgComparison operator <=(tgQueryItem item1, short literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Int16, true);
        }

        public static tgComparison operator <=(short literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgComparison operator <=(tgQueryItem item1, int literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Int32, true);
        }

        public static tgComparison operator <=(int literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgComparison operator <=(tgQueryItem item1, long literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Int64, true);
        }

        public static tgComparison operator <=(long literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.Object
        public static tgComparison operator <=(tgQueryItem item1, object literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Object, true);
        }

        public static tgComparison operator <=(object literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Object, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgComparison operator <=(tgQueryItem item1, sbyte literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <=(sbyte literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgComparison operator <=(tgQueryItem item1, float literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Single, true);
        }

        public static tgComparison operator <=(float literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.String
        public static tgComparison operator <=(tgQueryItem item1, string literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.String, true);
        }

        public static tgComparison operator <=(string literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.String, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgComparison operator <=(tgQueryItem item1, ushort literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <=(ushort literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgComparison operator <=(tgQueryItem item1, uint literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <=(uint literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgComparison operator <=(tgQueryItem item1, ulong literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <=(ulong literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region >= operator literal overloads

        /// <summary>
        /// Greater Than or Equal &gt;= (to use in Where clauses).
        /// </summary>
        /// <example>
        /// The operators provide an alternative, natural syntax for DynamicQueries.
        /// <code>
        ///	emps.Query.Where(emps.Query.BirthDate &gt;= "2000-01-01");
        /// </code>
        /// </example>
        /// <param name="item">Passed in via DynamicQuery</param>
        /// <param name="value">Passed in via DynamicQuery</param>
        /// <returns>The tgComparison returned to DynamicQuery</returns>
        public static tgComparison operator >=(tgQueryItem item, tgQueryItem value)
        {
            tgComparison wi = new tgComparison(item.query);
            wi.Operand = tgComparisonOperand.GreaterThanOrEqual;

            wi.data.Column = item.Column;
            wi.data.ComparisonColumn = value.Column;

            wi.SubOperators = item.SubOperators;
            return wi;
        }

        private static tgComparison GreaterThanOrEqual(tgQueryItem queryItem, object literal, tgSystemType literalType, bool itemFirst)
        {
            tgComparison wi = new tgComparison(queryItem.query);
            wi.Operand = tgComparisonOperand.GreaterThanOrEqual;

            wi.data.Column = queryItem.Column;
            wi.data.Value = literal;
            wi.data.Expression = queryItem.Expression;
            wi.data.ItemFirst = itemFirst;

            wi.SubOperators = queryItem.SubOperators;
            return wi;
        }

        // tgSystemType.Boolean
        public static tgComparison operator >=(tgQueryItem item1, bool literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgComparison operator >=(bool literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgComparison operator >=(tgQueryItem item1, byte literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Byte, true);
        }

        public static tgComparison operator >=(byte literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgComparison operator >=(tgQueryItem item1, char literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Char, true);
        }

        public static tgComparison operator >=(char literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgComparison operator >=(tgQueryItem item1, DateTime literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgComparison operator >=(DateTime literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgComparison operator >=(tgQueryItem item1, double literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Double, true);
        }

        public static tgComparison operator >=(double literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgComparison operator >=(tgQueryItem item1, decimal literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgComparison operator >=(decimal literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgComparison operator >=(tgQueryItem item1, Guid literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Guid, true);
        }

        public static tgComparison operator >=(Guid literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgComparison operator >=(tgQueryItem item1, short literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Int16, true);
        }

        public static tgComparison operator >=(short literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgComparison operator >=(tgQueryItem item1, int literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Int32, true);
        }

        public static tgComparison operator >=(int literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgComparison operator >=(tgQueryItem item1, long literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Int64, true);
        }

        public static tgComparison operator >=(long literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.Object
        public static tgComparison operator >=(tgQueryItem item1, object literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Object, true);
        }

        public static tgComparison operator >=(object literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Object, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgComparison operator >=(tgQueryItem item1, sbyte literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >=(sbyte literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgComparison operator >=(tgQueryItem item1, float literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Single, true);
        }

        public static tgComparison operator >=(float literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.String
        public static tgComparison operator >=(tgQueryItem item1, string literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.String, true);
        }

        public static tgComparison operator >=(string literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.String, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgComparison operator >=(tgQueryItem item1, ushort literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >=(ushort literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgComparison operator >=(tgQueryItem item1, uint literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >=(uint literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgComparison operator >=(tgQueryItem item1, ulong literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >=(ulong literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region == operator literal overloads

        public static tgComparison operator ==(tgQueryItem item, tgQueryItem value)
        {
            tgComparison wi = new tgComparison(item.query);
            wi.Operand = tgComparisonOperand.Equal;

            wi.data.Column = item.Column;
            wi.data.ComparisonColumn = value.Column;

            wi.SubOperators = item.SubOperators;
            return wi;
        }

        private static tgComparison EqualOperator(tgQueryItem queryItem, object literal, tgSystemType literalType, bool itemFirst)
        {
            tgComparison wi = new tgComparison(queryItem.query);
            wi.Operand = tgComparisonOperand.Equal;

            wi.data.Column = queryItem.Column;
            wi.data.Value = literal;
            wi.data.Expression = queryItem.Expression;
            wi.data.ItemFirst = itemFirst;

            wi.SubOperators = queryItem.SubOperators;
            return wi;
        }

        // tgSystemType.Boolean
        public static tgComparison operator ==(tgQueryItem item1, bool literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgComparison operator ==(bool literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgComparison operator ==(tgQueryItem item1, byte literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Byte, true);
        }

        public static tgComparison operator ==(byte literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgComparison operator ==(tgQueryItem item1, char literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Char, true);
        }

        public static tgComparison operator ==(char literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgComparison operator ==(tgQueryItem item1, DateTime literal)
        {
            return EqualOperator(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgComparison operator ==(DateTime literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgComparison operator ==(tgQueryItem item1, double literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Double, true);
        }

        public static tgComparison operator ==(double literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgComparison operator ==(tgQueryItem item1, decimal literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgComparison operator ==(decimal literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgComparison operator ==(tgQueryItem item1, Guid literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Guid, true);
        }

        public static tgComparison operator ==(Guid literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgComparison operator ==(tgQueryItem item1, short literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Int16, true);
        }

        public static tgComparison operator ==(short literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgComparison operator ==(tgQueryItem item1, int literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Int32, true);
        }

        public static tgComparison operator ==(int literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgComparison operator ==(tgQueryItem item1, long literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Int64, true);
        }

        public static tgComparison operator ==(long literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.Object
        public static tgComparison operator ==(tgQueryItem item1, object literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Object, true);
        }

        public static tgComparison operator ==(object literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Object, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgComparison operator ==(tgQueryItem item1, sbyte literal)
        {
            return EqualOperator(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator ==(sbyte literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgComparison operator ==(tgQueryItem item1, float literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Single, true);
        }

        public static tgComparison operator ==(float literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.String
        public static tgComparison operator ==(tgQueryItem item1, string literal)
        {
            return EqualOperator(item1, literal, tgSystemType.String, true);
        }

        public static tgComparison operator ==(string literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.String, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgComparison operator ==(tgQueryItem item1, ushort literal)
        {
            return EqualOperator(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator ==(ushort literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgComparison operator ==(tgQueryItem item1, uint literal)
        {
            return EqualOperator(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator ==(uint literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgComparison operator ==(tgQueryItem item1, ulong literal)
        {
            return EqualOperator(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator ==(ulong literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region != operator literal overloads

        public static tgComparison operator !=(tgQueryItem item, tgQueryItem value)
        {
            tgComparison wi = new tgComparison(item.query);
            wi.Operand = tgComparisonOperand.NotEqual;

            wi.data.Column = item.Column;
            wi.data.ComparisonColumn = value.Column;

            wi.SubOperators = item.SubOperators;

            return wi;
        }

        private static tgComparison NotEqualOperator(tgQueryItem queryItem, object literal, tgSystemType literalType, bool itemFirst)
        {
            tgComparison wi = new tgComparison(queryItem.query);
            wi.Operand = tgComparisonOperand.NotEqual;

            wi.data.Column = queryItem.Column;
            wi.data.Value = literal;
            wi.data.Expression = queryItem.Expression;
            wi.data.ItemFirst = itemFirst;

            wi.SubOperators = queryItem.SubOperators;
            return wi;
        }

        // tgSystemType.Boolean
        public static tgComparison operator !=(tgQueryItem item1, bool literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgComparison operator !=(bool literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgComparison operator !=(tgQueryItem item1, byte literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Byte, true);
        }

        public static tgComparison operator !=(byte literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgComparison operator !=(tgQueryItem item1, char literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Char, true);
        }

        public static tgComparison operator !=(char literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgComparison operator !=(tgQueryItem item1, DateTime literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgComparison operator !=(DateTime literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgComparison operator !=(tgQueryItem item1, double literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Double, true);
        }

        public static tgComparison operator !=(double literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgComparison operator !=(tgQueryItem item1, decimal literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgComparison operator !=(decimal literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgComparison operator !=(tgQueryItem item1, Guid literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Guid, true);
        }

        public static tgComparison operator !=(Guid literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgComparison operator !=(tgQueryItem item1, short literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Int16, true);
        }

        public static tgComparison operator !=(short literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgComparison operator !=(tgQueryItem item1, int literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Int32, true);
        }

        public static tgComparison operator !=(int literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgComparison operator !=(tgQueryItem item1, long literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Int64, true);
        }

        public static tgComparison operator !=(long literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.Object
        public static tgComparison operator !=(tgQueryItem item1, object literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Object, true);
        }

        public static tgComparison operator !=(object literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Object, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgComparison operator !=(tgQueryItem item1, sbyte literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator !=(sbyte literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgComparison operator !=(tgQueryItem item1, float literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Single, true);
        }

        public static tgComparison operator !=(float literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.String
        public static tgComparison operator !=(tgQueryItem item1, string literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.String, true);
        }

        public static tgComparison operator !=(string literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.String, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgComparison operator !=(tgQueryItem item1, ushort literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.UInt16, true);
        }
        
        [CLSCompliant(false)]
        public static tgComparison operator !=(ushort literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgComparison operator !=(tgQueryItem item1, uint literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator !=(uint literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgComparison operator !=(tgQueryItem item1, ulong literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator !=(ulong literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region Arithmetic Expressions

        public static tgQueryItem operator +(tgQueryItem item1, tgQueryItem item2)
        {
            tgQueryItem qi = new tgQueryItem();
            qi.Expression.SelectItem1 = item1;
            qi.Expression.Operator = tgArithmeticOperator.Add;
            qi.Expression.SelectItem2 = item2;
            return qi;
        }

        #region + operator literal overloads

        private static tgQueryItem AddOperator(tgQueryItem queryItem, object literal, tgSystemType literalType, bool itemFirst)
        {
            tgQueryItem qi = new tgQueryItem();
            qi.Expression.SelectItem1 = queryItem;
            qi.Expression.Operator = tgArithmeticOperator.Add;
            qi.Expression.Literal = literal;
            qi.Expression.LiteralType = literalType;
            qi.Expression.ItemFirst = itemFirst;
            return qi;
        }

        // tgSystemType.Boolean
        public static tgQueryItem operator +(tgQueryItem item1, bool literal)
        {
            return AddOperator(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgQueryItem operator +(bool literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgQueryItem operator +(tgQueryItem item1, byte literal)
        {
            return AddOperator(item1, literal, tgSystemType.Byte, true);
        }

        public static tgQueryItem operator +(byte literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgQueryItem operator +(tgQueryItem item1, char literal)
        {
            return AddOperator(item1, literal, tgSystemType.Char, true);
        }

        public static tgQueryItem operator +(char literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgQueryItem operator +(tgQueryItem item1, DateTime literal)
        {
            return AddOperator(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgQueryItem operator +(DateTime literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgQueryItem operator +(tgQueryItem item1, double literal)
        {
            return AddOperator(item1, literal, tgSystemType.Double, true);
        }

        public static tgQueryItem operator +(double literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgQueryItem operator +(tgQueryItem item1, decimal literal)
        {
            return AddOperator(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgQueryItem operator +(decimal literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgQueryItem operator +(tgQueryItem item1, Guid literal)
        {
            return AddOperator(item1, literal, tgSystemType.Guid, true);
        }

        public static tgQueryItem operator +(Guid literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgQueryItem operator +(tgQueryItem item1, short literal)
        {
            return AddOperator(item1, literal, tgSystemType.Int16, true);
        }

        public static tgQueryItem operator +(short literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgQueryItem operator +(tgQueryItem item1, int literal)
        {
            return AddOperator(item1, literal, tgSystemType.Int32, true);
        }

        public static tgQueryItem operator +(int literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgQueryItem operator +(tgQueryItem item1, long literal)
        {
            return AddOperator(item1, literal, tgSystemType.Int64, true);
        }

        public static tgQueryItem operator +(long literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.Object
        public static tgQueryItem operator +(tgQueryItem item1, object literal)
        {
            return AddOperator(item1, literal, tgSystemType.Object, true);
        }

        public static tgQueryItem operator +(object literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Object, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgQueryItem operator +(tgQueryItem item1, sbyte literal)
        {
            return AddOperator(item1, literal, tgSystemType.SByte, true);
        }
        
        [CLSCompliant(false)]
        public static tgQueryItem operator +(sbyte literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgQueryItem operator +(tgQueryItem item1, float literal)
        {
            return AddOperator(item1, literal, tgSystemType.Single, true);
        }

        public static tgQueryItem operator +(float literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.String
        public static tgQueryItem operator +(tgQueryItem item1, string literal)
        {
            return AddOperator(item1, literal, tgSystemType.String, true);
        }

        public static tgQueryItem operator +(string literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.String, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgQueryItem operator +(tgQueryItem item1, ushort literal)
        {
            return AddOperator(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator +(ushort literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgQueryItem operator +(tgQueryItem item1, uint literal)
        {
            return AddOperator(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator +(uint literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgQueryItem operator +(tgQueryItem item1, ulong literal)
        {
            return AddOperator(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator +(ulong literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        public static tgQueryItem operator -(tgQueryItem item1, tgQueryItem item2)
        {
            tgQueryItem qi = new tgQueryItem();
            qi.Expression.SelectItem1 = item1;
            qi.Expression.Operator = tgArithmeticOperator.Subtract;
            qi.Expression.SelectItem2 = item2;
            return qi;
        }

        #region - operator literal overloads

        private static tgQueryItem SubtractOperator(tgQueryItem queryItem, object literal, tgSystemType literalType, bool itemFirst)
        {
            tgQueryItem qi = new tgQueryItem();
            qi.Expression.SelectItem1 = queryItem;
            qi.Expression.Operator = tgArithmeticOperator.Subtract;
            qi.Expression.Literal = literal;
            qi.Expression.LiteralType = literalType;
            qi.Expression.ItemFirst = itemFirst;
            return qi;
        }

        // tgSystemType.Boolean
        public static tgQueryItem operator -(tgQueryItem item1, bool literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgQueryItem operator -(bool literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgQueryItem operator -(tgQueryItem item1, byte literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Byte, true);
        }

        public static tgQueryItem operator -(byte literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgQueryItem operator -(tgQueryItem item1, char literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Char, true);
        }

        public static tgQueryItem operator -(char literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgQueryItem operator -(tgQueryItem item1, DateTime literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgQueryItem operator -(DateTime literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgQueryItem operator -(tgQueryItem item1, double literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Double, true);
        }

        public static tgQueryItem operator -(double literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgQueryItem operator -(tgQueryItem item1, decimal literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgQueryItem operator -(decimal literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgQueryItem operator -(tgQueryItem item1, Guid literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Guid, true);
        }

        public static tgQueryItem operator -(Guid literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgQueryItem operator -(tgQueryItem item1, short literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Int16, true);
        }

        public static tgQueryItem operator -(short literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgQueryItem operator -(tgQueryItem item1, int literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Int32, true);
        }

        public static tgQueryItem operator -(int literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgQueryItem operator -(tgQueryItem item1, long literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Int64, true);
        }

        public static tgQueryItem operator -(long literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.Object
        public static tgQueryItem operator -(tgQueryItem item1, object literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Object, true);
        }

        public static tgQueryItem operator -(object literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Object, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgQueryItem operator -(tgQueryItem item1, sbyte literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator -(sbyte literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgQueryItem operator -(tgQueryItem item1, float literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Single, true);
        }

        public static tgQueryItem operator -(float literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.String
        public static tgQueryItem operator -(tgQueryItem item1, string literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.String, true);
        }

        public static tgQueryItem operator -(string literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.String, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgQueryItem operator -(tgQueryItem item1, ushort literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator -(ushort literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgQueryItem operator -(tgQueryItem item1, uint literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator -(uint literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgQueryItem operator -(tgQueryItem item1, ulong literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator -(ulong literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        public static tgQueryItem operator *(tgQueryItem item1, tgQueryItem item2)
        {
            tgQueryItem qi = new tgQueryItem();
            qi.Expression.SelectItem1 = item1;
            qi.Expression.Operator = tgArithmeticOperator.Multiply;
            qi.Expression.SelectItem2 = item2;
            return qi;
        }

        #region * operator literal overloads

        private static tgQueryItem MultiplyOperator(tgQueryItem queryItem, object literal, tgSystemType literalType, bool itemFirst)
        {
            tgQueryItem qi = new tgQueryItem();
            qi.Expression.SelectItem1 = queryItem;
            qi.Expression.Operator = tgArithmeticOperator.Multiply;
            qi.Expression.Literal = literal;
            qi.Expression.LiteralType = literalType;
            qi.Expression.ItemFirst = itemFirst;
            return qi;
        }

        // tgSystemType.Boolean
        public static tgQueryItem operator *(tgQueryItem item1, bool literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgQueryItem operator *(bool literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgQueryItem operator *(tgQueryItem item1, byte literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Byte, true);
        }

        public static tgQueryItem operator *(byte literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgQueryItem operator *(tgQueryItem item1, char literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Char, true);
        }

        public static tgQueryItem operator *(char literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgQueryItem operator *(tgQueryItem item1, DateTime literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgQueryItem operator *(DateTime literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgQueryItem operator *(tgQueryItem item1, double literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Double, true);
        }

        public static tgQueryItem operator *(double literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgQueryItem operator *(tgQueryItem item1, decimal literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgQueryItem operator *(decimal literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgQueryItem operator *(tgQueryItem item1, Guid literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Guid, true);
        }

        public static tgQueryItem operator *(Guid literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgQueryItem operator *(tgQueryItem item1, short literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Int16, true);
        }

        public static tgQueryItem operator *(short literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgQueryItem operator *(tgQueryItem item1, int literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Int32, true);
        }

        public static tgQueryItem operator *(int literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgQueryItem operator *(tgQueryItem item1, long literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Int64, true);
        }

        public static tgQueryItem operator *(long literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.Object
        public static tgQueryItem operator *(tgQueryItem item1, object literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Object, true);
        }

        public static tgQueryItem operator *(object literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Object, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgQueryItem operator *(tgQueryItem item1, sbyte literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator *(sbyte literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgQueryItem operator *(tgQueryItem item1, float literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Single, true);
        }

        public static tgQueryItem operator *(float literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.String
        public static tgQueryItem operator *(tgQueryItem item1, string literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.String, true);
        }

        public static tgQueryItem operator *(string literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.String, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgQueryItem operator *(tgQueryItem item1, ushort literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator *(ushort literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgQueryItem operator *(tgQueryItem item1, uint literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator *(uint literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgQueryItem operator *(tgQueryItem item1, ulong literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator *(ulong literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        public static tgQueryItem operator /(tgQueryItem item1, tgQueryItem item2)
        {
            tgQueryItem qi = new tgQueryItem();
            qi.Expression.SelectItem1 = item1;
            qi.Expression.Operator = tgArithmeticOperator.Divide;
            qi.Expression.SelectItem2 = item2;
            return qi;
        }

        #region / operator literal overloads

        private static tgQueryItem DivideOperator(tgQueryItem queryItem, object literal, tgSystemType literalType, bool itemFirst)
        {
            tgQueryItem qi = new tgQueryItem();
            qi.Expression.SelectItem1 = queryItem;
            qi.Expression.Operator = tgArithmeticOperator.Divide;
            qi.Expression.Literal = literal;
            qi.Expression.LiteralType = literalType;
            qi.Expression.ItemFirst = itemFirst;
            return qi;
        }

        // tgSystemType.Boolean
        public static tgQueryItem operator /(tgQueryItem item1, bool literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgQueryItem operator /(bool literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgQueryItem operator /(tgQueryItem item1, byte literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Byte, true);
        }

        public static tgQueryItem operator /(byte literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgQueryItem operator /(tgQueryItem item1, char literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Char, true);
        }

        public static tgQueryItem operator /(char literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgQueryItem operator /(tgQueryItem item1, DateTime literal)
        {
            return DivideOperator(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgQueryItem operator /(DateTime literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgQueryItem operator /(tgQueryItem item1, double literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Double, true);
        }

        public static tgQueryItem operator /(double literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgQueryItem operator /(tgQueryItem item1, decimal literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgQueryItem operator /(decimal literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgQueryItem operator /(tgQueryItem item1, Guid literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Guid, true);
        }

        public static tgQueryItem operator /(Guid literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgQueryItem operator /(tgQueryItem item1, short literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Int16, true);
        }

        public static tgQueryItem operator /(short literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgQueryItem operator /(tgQueryItem item1, int literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Int32, true);
        }

        public static tgQueryItem operator /(int literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgQueryItem operator /(tgQueryItem item1, long literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Int64, true);
        }

        public static tgQueryItem operator /(long literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.Object
        public static tgQueryItem operator /(tgQueryItem item1, object literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Object, true);
        }

        public static tgQueryItem operator /(object literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Object, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgQueryItem operator /(tgQueryItem item1, sbyte literal)
        {
            return DivideOperator(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator /(sbyte literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgQueryItem operator /(tgQueryItem item1, float literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Single, true);
        }

        public static tgQueryItem operator /(float literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.String
        public static tgQueryItem operator /(tgQueryItem item1, string literal)
        {
            return DivideOperator(item1, literal, tgSystemType.String, true);
        }

        public static tgQueryItem operator /(string literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.String, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgQueryItem operator /(tgQueryItem item1, ushort literal)
        {
            return DivideOperator(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator /(ushort literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgQueryItem operator /(tgQueryItem item1, uint literal)
        {
            return DivideOperator(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator /(uint literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgQueryItem operator /(tgQueryItem item1, ulong literal)
        {
            return DivideOperator(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator /(ulong literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        public static tgQueryItem operator %(tgQueryItem item1, tgQueryItem item2)
        {
            tgQueryItem qi = new tgQueryItem();
            qi.Expression.SelectItem1 = item1;
            qi.Expression.Operator = tgArithmeticOperator.Modulo;
            qi.Expression.SelectItem2 = item2;
            return qi;
        }

        #region % operator literal overloads

        private static tgQueryItem ModuloOperator(tgQueryItem queryItem, object literal, tgSystemType literalType, bool itemFirst)
        {
            tgQueryItem qi = new tgQueryItem();
            qi.Expression.SelectItem1 = queryItem;
            qi.Expression.Operator = tgArithmeticOperator.Modulo;
            qi.Expression.Literal = literal;
            qi.Expression.LiteralType = literalType;
            qi.Expression.ItemFirst = itemFirst;
            return qi;
        }

        // tgSystemType.Boolean
        public static tgQueryItem operator %(tgQueryItem item1, bool literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgQueryItem operator %(bool literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgQueryItem operator %(tgQueryItem item1, byte literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Byte, true);
        }

        public static tgQueryItem operator %(byte literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgQueryItem operator %(tgQueryItem item1, char literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Char, true);
        }

        public static tgQueryItem operator %(char literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgQueryItem operator %(tgQueryItem item1, DateTime literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgQueryItem operator %(DateTime literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgQueryItem operator %(tgQueryItem item1, double literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Double, true);
        }

        public static tgQueryItem operator %(double literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgQueryItem operator %(tgQueryItem item1, decimal literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgQueryItem operator %(decimal literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgQueryItem operator %(tgQueryItem item1, Guid literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Guid, true);
        }

        public static tgQueryItem operator %(Guid literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgQueryItem operator %(tgQueryItem item1, short literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Int16, true);
        }

        public static tgQueryItem operator %(short literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgQueryItem operator %(tgQueryItem item1, int literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Int32, true);
        }

        public static tgQueryItem operator %(int literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgQueryItem operator %(tgQueryItem item1, long literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Int64, true);
        }

        public static tgQueryItem operator %(long literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.Object
        public static tgQueryItem operator %(tgQueryItem item1, object literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Object, true);
        }

        public static tgQueryItem operator %(object literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Object, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgQueryItem operator %(tgQueryItem item1, sbyte literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator %(sbyte literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgQueryItem operator %(tgQueryItem item1, float literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Single, true);
        }

        public static tgQueryItem operator %(float literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.String
        public static tgQueryItem operator %(tgQueryItem item1, string literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.String, true);
        }

        public static tgQueryItem operator %(string literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.String, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgQueryItem operator %(tgQueryItem item1, ushort literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator %(ushort literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgQueryItem operator %(tgQueryItem item1, uint literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator %(uint literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgQueryItem operator %(tgQueryItem item1, ulong literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator %(ulong literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #endregion

        #endregion

        #region operators applied to other QueryItems (LiteralExpression) with Nullable overloads

        #region > operator literal overloads

        // tgSystemType.Boolean
        public static tgComparison operator >(tgQueryItem item1, bool? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgComparison operator >(bool? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgComparison operator >(tgQueryItem item1, byte? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Byte, true);
        }

        public static tgComparison operator >(byte? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgComparison operator >(tgQueryItem item1, char? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Char, true);
        }

        public static tgComparison operator >(char? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgComparison operator >(tgQueryItem item1, DateTime? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgComparison operator >(DateTime? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgComparison operator >(tgQueryItem item1, double? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Double, true);
        }

        public static tgComparison operator >(double? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgComparison operator >(tgQueryItem item1, decimal? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgComparison operator >(decimal? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgComparison operator >(tgQueryItem item1, Guid? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Guid, true);
        }

        public static tgComparison operator >(Guid? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgComparison operator >(tgQueryItem item1, short? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Int16, true);
        }

        public static tgComparison operator >(short? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgComparison operator >(tgQueryItem item1, int? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Int32, true);
        }

        public static tgComparison operator >(int? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgComparison operator >(tgQueryItem item1, long? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Int64, true);
        }

        public static tgComparison operator >(long? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgComparison operator >(tgQueryItem item1, sbyte? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >(sbyte? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgComparison operator >(tgQueryItem item1, float? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.Single, true);
        }

        public static tgComparison operator >(float? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgComparison operator >(tgQueryItem item1, ushort? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >(ushort? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgComparison operator >(tgQueryItem item1, uint? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >(uint? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgComparison operator >(tgQueryItem item1, ulong? literal)
        {
            return GreaterThan(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >(ulong? literal, tgQueryItem item1)
        {
            return GreaterThan(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region < operator literal overloads

        // tgSystemType.Boolean
        public static tgComparison operator <(tgQueryItem item1, bool? literal)
        {
            return LessThan(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgComparison operator <(bool? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgComparison operator <(tgQueryItem item1, byte? literal)
        {
            return LessThan(item1, literal, tgSystemType.Byte, true);
        }

        public static tgComparison operator <(byte? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgComparison operator <(tgQueryItem item1, char? literal)
        {
            return LessThan(item1, literal, tgSystemType.Char, true);
        }

        public static tgComparison operator <(char? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgComparison operator <(tgQueryItem item1, DateTime? literal)
        {
            return LessThan(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgComparison operator <(DateTime? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgComparison operator <(tgQueryItem item1, double? literal)
        {
            return LessThan(item1, literal, tgSystemType.Double, true);
        }

        public static tgComparison operator <(double? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgComparison operator <(tgQueryItem item1, decimal? literal)
        {
            return LessThan(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgComparison operator <(decimal? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgComparison operator <(tgQueryItem item1, Guid? literal)
        {
            return LessThan(item1, literal, tgSystemType.Guid, true);
        }

        public static tgComparison operator <(Guid? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgComparison operator <(tgQueryItem item1, short? literal)
        {
            return LessThan(item1, literal, tgSystemType.Int16, true);
        }

        public static tgComparison operator <(short? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgComparison operator <(tgQueryItem item1, int? literal)
        {
            return LessThan(item1, literal, tgSystemType.Int32, true);
        }

        public static tgComparison operator <(int? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgComparison operator <(tgQueryItem item1, long? literal)
        {
            return LessThan(item1, literal, tgSystemType.Int64, true);
        }

        public static tgComparison operator <(long? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgComparison operator <(tgQueryItem item1, sbyte? literal)
        {
            return LessThan(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <(sbyte? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgComparison operator <(tgQueryItem item1, float? literal)
        {
            return LessThan(item1, literal, tgSystemType.Single, true);
        }

        public static tgComparison operator <(float? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgComparison operator <(tgQueryItem item1, ushort? literal)
        {
            return LessThan(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <(ushort? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgComparison operator <(tgQueryItem item1, uint? literal)
        {
            return LessThan(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <(uint? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgComparison operator <(tgQueryItem item1, ulong? literal)
        {
            return LessThan(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <(ulong? literal, tgQueryItem item1)
        {
            return LessThan(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region <= operator literal overloads

        // tgSystemType.Boolean
        public static tgComparison operator <=(tgQueryItem item1, bool? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgComparison operator <=(bool? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgComparison operator <=(tgQueryItem item1, byte? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Byte, true);
        }

        public static tgComparison operator <=(byte? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgComparison operator <=(tgQueryItem item1, char? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Char, true);
        }

        public static tgComparison operator <=(char? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgComparison operator <=(tgQueryItem item1, DateTime? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgComparison operator <=(DateTime? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgComparison operator <=(tgQueryItem item1, double? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Double, true);
        }

        public static tgComparison operator <=(double? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgComparison operator <=(tgQueryItem item1, decimal? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgComparison operator <=(decimal? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgComparison operator <=(tgQueryItem item1, Guid? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Guid, true);
        }

        public static tgComparison operator <=(Guid? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgComparison operator <=(tgQueryItem item1, short? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Int16, true);
        }

        public static tgComparison operator <=(short? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgComparison operator <=(tgQueryItem item1, int? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Int32, true);
        }

        public static tgComparison operator <=(int? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgComparison operator <=(tgQueryItem item1, long? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Int64, true);
        }

        public static tgComparison operator <=(long? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgComparison operator <=(tgQueryItem item1, sbyte? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <=(sbyte? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgComparison operator <=(tgQueryItem item1, float? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Single, true);
        }

        public static tgComparison operator <=(float? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgComparison operator <=(tgQueryItem item1, ushort? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <=(ushort? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgComparison operator <=(tgQueryItem item1, uint? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <=(uint? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgComparison operator <=(tgQueryItem item1, ulong? literal)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator <=(ulong? literal, tgQueryItem item1)
        {
            return LessThanOrEqual(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region >= operator literal overloads

        // tgSystemType.Boolean
        public static tgComparison operator >=(tgQueryItem item1, bool? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgComparison operator >=(bool? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgComparison operator >=(tgQueryItem item1, byte? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Byte, true);
        }

        public static tgComparison operator >=(byte? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgComparison operator >=(tgQueryItem item1, char? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Char, true);
        }

        public static tgComparison operator >=(char? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgComparison operator >=(tgQueryItem item1, DateTime? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgComparison operator >=(DateTime? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgComparison operator >=(tgQueryItem item1, double? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Double, true);
        }

        public static tgComparison operator >=(double? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgComparison operator >=(tgQueryItem item1, decimal? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgComparison operator >=(decimal? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgComparison operator >=(tgQueryItem item1, Guid? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Guid, true);
        }

        public static tgComparison operator >=(Guid? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgComparison operator >=(tgQueryItem item1, short? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Int16, true);
        }

        public static tgComparison operator >=(short? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgComparison operator >=(tgQueryItem item1, int? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Int32, true);
        }

        public static tgComparison operator >=(int? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgComparison operator >=(tgQueryItem item1, long? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Int64, true);
        }

        public static tgComparison operator >=(long? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgComparison operator >=(tgQueryItem item1, sbyte? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >=(sbyte? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgComparison operator >=(tgQueryItem item1, float? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Single, true);
        }

        public static tgComparison operator >=(float? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgComparison operator >=(tgQueryItem item1, ushort? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >=(ushort? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgComparison operator >=(tgQueryItem item1, uint? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >=(uint? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgComparison operator >=(tgQueryItem item1, ulong? literal)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator >=(ulong? literal, tgQueryItem item1)
        {
            return GreaterThanOrEqual(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region == operator literal overloads

        // tgSystemType.Boolean
        public static tgComparison operator ==(tgQueryItem item1, bool? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgComparison operator ==(bool? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgComparison operator ==(tgQueryItem item1, byte? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Byte, true);
        }

        public static tgComparison operator ==(byte? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgComparison operator ==(tgQueryItem item1, char? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Char, true);
        }

        public static tgComparison operator ==(char? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgComparison operator ==(tgQueryItem item1, DateTime? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgComparison operator ==(DateTime? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgComparison operator ==(tgQueryItem item1, double? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Double, true);
        }

        public static tgComparison operator ==(double? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgComparison operator ==(tgQueryItem item1, decimal? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgComparison operator ==(decimal? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgComparison operator ==(tgQueryItem item1, Guid? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Guid, true);
        }

        public static tgComparison operator ==(Guid? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgComparison operator ==(tgQueryItem item1, short? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Int16, true);
        }

        public static tgComparison operator ==(short? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgComparison operator ==(tgQueryItem item1, int? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Int32, true);
        }

        public static tgComparison operator ==(int? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgComparison operator ==(tgQueryItem item1, long? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Int64, true);
        }

        public static tgComparison operator ==(long? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgComparison operator ==(tgQueryItem item1, sbyte? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator ==(sbyte? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgComparison operator ==(tgQueryItem item1, float? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.Single, true);
        }

        public static tgComparison operator ==(float? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgComparison operator ==(tgQueryItem item1, ushort? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator ==(ushort? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgComparison operator ==(tgQueryItem item1, uint? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator ==(uint? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgComparison operator ==(tgQueryItem item1, ulong? literal)
        {
            return EqualOperator(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator ==(ulong? literal, tgQueryItem item1)
        {
            return EqualOperator(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region != operator literal overloads

        // tgSystemType.Boolean
        public static tgComparison operator !=(tgQueryItem item1, bool? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgComparison operator !=(bool? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgComparison operator !=(tgQueryItem item1, byte? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Byte, true);
        }

        public static tgComparison operator !=(byte? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgComparison operator !=(tgQueryItem item1, char? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Char, true);
        }

        public static tgComparison operator !=(char? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgComparison operator !=(tgQueryItem item1, DateTime? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgComparison operator !=(DateTime? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgComparison operator !=(tgQueryItem item1, double? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Double, true);
        }

        public static tgComparison operator !=(double? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgComparison operator !=(tgQueryItem item1, decimal? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgComparison operator !=(decimal? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgComparison operator !=(tgQueryItem item1, Guid? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Guid, true);
        }

        public static tgComparison operator !=(Guid? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgComparison operator !=(tgQueryItem item1, short? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Int16, true);
        }

        public static tgComparison operator !=(short? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgComparison operator !=(tgQueryItem item1, int? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Int32, true);
        }

        public static tgComparison operator !=(int? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgComparison operator !=(tgQueryItem item1, long? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Int64, true);
        }

        public static tgComparison operator !=(long? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgComparison operator !=(tgQueryItem item1, sbyte? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator !=(sbyte? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgComparison operator !=(tgQueryItem item1, float? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Single, true);
        }

        public static tgComparison operator !=(float? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgComparison operator !=(tgQueryItem item1, ushort? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator !=(ushort? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgComparison operator !=(tgQueryItem item1, uint? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator !=(uint? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgComparison operator !=(tgQueryItem item1, ulong? literal)
        {
            return NotEqualOperator(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgComparison operator !=(ulong? literal, tgQueryItem item1)
        {
            return NotEqualOperator(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region Arithmetic Expressions

        #region + operator literal overloads

        // tgSystemType.Boolean
        public static tgQueryItem operator +(tgQueryItem item1, bool? literal)
        {
            return AddOperator(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgQueryItem operator +(bool? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgQueryItem operator +(tgQueryItem item1, byte? literal)
        {
            return AddOperator(item1, literal, tgSystemType.Byte, true);
        }

        public static tgQueryItem operator +(byte? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgQueryItem operator +(tgQueryItem item1, char? literal)
        {
            return AddOperator(item1, literal, tgSystemType.Char, true);
        }

        public static tgQueryItem operator +(char? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgQueryItem operator +(tgQueryItem item1, DateTime? literal)
        {
            return AddOperator(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgQueryItem operator +(DateTime? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgQueryItem operator +(tgQueryItem item1, double? literal)
        {
            return AddOperator(item1, literal, tgSystemType.Double, true);
        }

        public static tgQueryItem operator +(double? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgQueryItem operator +(tgQueryItem item1, decimal? literal)
        {
            return AddOperator(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgQueryItem operator +(decimal? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgQueryItem operator +(tgQueryItem item1, Guid? literal)
        {
            return AddOperator(item1, literal, tgSystemType.Guid, true);
        }

        public static tgQueryItem operator +(Guid? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgQueryItem operator +(tgQueryItem item1, short? literal)
        {
            return AddOperator(item1, literal, tgSystemType.Int16, true);
        }

        public static tgQueryItem operator +(short? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgQueryItem operator +(tgQueryItem item1, int? literal)
        {
            return AddOperator(item1, literal, tgSystemType.Int32, true);
        }

        public static tgQueryItem operator +(int? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgQueryItem operator +(tgQueryItem item1, long? literal)
        {
            return AddOperator(item1, literal, tgSystemType.Int64, true);
        }

        public static tgQueryItem operator +(long? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgQueryItem operator +(tgQueryItem item1, sbyte? literal)
        {
            return AddOperator(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator +(sbyte? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgQueryItem operator +(tgQueryItem item1, float? literal)
        {
            return AddOperator(item1, literal, tgSystemType.Single, true);
        }

        public static tgQueryItem operator +(float? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgQueryItem operator +(tgQueryItem item1, ushort? literal)
        {
            return AddOperator(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator +(ushort? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgQueryItem operator +(tgQueryItem item1, uint? literal)
        {
            return AddOperator(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator +(uint? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgQueryItem operator +(tgQueryItem item1, ulong? literal)
        {
            return AddOperator(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator +(ulong? literal, tgQueryItem item1)
        {
            return AddOperator(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region - operator literal overloads

        // tgSystemType.Boolean
        public static tgQueryItem operator -(tgQueryItem item1, bool? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgQueryItem operator -(bool? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgQueryItem operator -(tgQueryItem item1, byte? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Byte, true);
        }

        public static tgQueryItem operator -(byte? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgQueryItem operator -(tgQueryItem item1, char? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Char, true);
        }

        public static tgQueryItem operator -(char? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgQueryItem operator -(tgQueryItem item1, DateTime? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgQueryItem operator -(DateTime? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgQueryItem operator -(tgQueryItem item1, double? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Double, true);
        }

        public static tgQueryItem operator -(double? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgQueryItem operator -(tgQueryItem item1, decimal? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgQueryItem operator -(decimal? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgQueryItem operator -(tgQueryItem item1, Guid? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Guid, true);
        }

        public static tgQueryItem operator -(Guid? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgQueryItem operator -(tgQueryItem item1, short? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Int16, true);
        }

        public static tgQueryItem operator -(short? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgQueryItem operator -(tgQueryItem item1, int? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Int32, true);
        }

        public static tgQueryItem operator -(int? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgQueryItem operator -(tgQueryItem item1, long? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Int64, true);
        }

        public static tgQueryItem operator -(long? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgQueryItem operator -(tgQueryItem item1, sbyte? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator -(sbyte? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgQueryItem operator -(tgQueryItem item1, float? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.Single, true);
        }

        public static tgQueryItem operator -(float? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgQueryItem operator -(tgQueryItem item1, ushort? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator -(ushort? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgQueryItem operator -(tgQueryItem item1, uint? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator -(uint? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgQueryItem operator -(tgQueryItem item1, ulong? literal)
        {
            return SubtractOperator(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator -(ulong? literal, tgQueryItem item1)
        {
            return SubtractOperator(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region * operator literal overloads

        // tgSystemType.Boolean
        public static tgQueryItem operator *(tgQueryItem item1, bool? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgQueryItem operator *(bool? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgQueryItem operator *(tgQueryItem item1, byte? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Byte, true);
        }

        public static tgQueryItem operator *(byte? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgQueryItem operator *(tgQueryItem item1, char? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Char, true);
        }

        public static tgQueryItem operator *(char? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgQueryItem operator *(tgQueryItem item1, DateTime? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgQueryItem operator *(DateTime? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgQueryItem operator *(tgQueryItem item1, double? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Double, true);
        }

        public static tgQueryItem operator *(double? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgQueryItem operator *(tgQueryItem item1, decimal? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgQueryItem operator *(decimal? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgQueryItem operator *(tgQueryItem item1, Guid? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Guid, true);
        }

        public static tgQueryItem operator *(Guid? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgQueryItem operator *(tgQueryItem item1, short? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Int16, true);
        }

        public static tgQueryItem operator *(short? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgQueryItem operator *(tgQueryItem item1, int? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Int32, true);
        }

        public static tgQueryItem operator *(int? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgQueryItem operator *(tgQueryItem item1, long? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Int64, true);
        }

        public static tgQueryItem operator *(long? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgQueryItem operator *(tgQueryItem item1, sbyte? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator *(sbyte? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgQueryItem operator *(tgQueryItem item1, float? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Single, true);
        }

        public static tgQueryItem operator *(float? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgQueryItem operator *(tgQueryItem item1, ushort? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator *(ushort? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgQueryItem operator *(tgQueryItem item1, uint? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator *(uint? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgQueryItem operator *(tgQueryItem item1, ulong? literal)
        {
            return MultiplyOperator(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator *(ulong? literal, tgQueryItem item1)
        {
            return MultiplyOperator(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region / operator literal overloads

        // tgSystemType.Boolean
        public static tgQueryItem operator /(tgQueryItem item1, bool? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgQueryItem operator /(bool? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgQueryItem operator /(tgQueryItem item1, byte? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Byte, true);
        }

        public static tgQueryItem operator /(byte? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgQueryItem operator /(tgQueryItem item1, char? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Char, true);
        }

        public static tgQueryItem operator /(char? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgQueryItem operator /(tgQueryItem item1, DateTime? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgQueryItem operator /(DateTime? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgQueryItem operator /(tgQueryItem item1, double? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Double, true);
        }

        public static tgQueryItem operator /(double? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgQueryItem operator /(tgQueryItem item1, decimal? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgQueryItem operator /(decimal? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgQueryItem operator /(tgQueryItem item1, Guid? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Guid, true);
        }

        public static tgQueryItem operator /(Guid? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgQueryItem operator /(tgQueryItem item1, short? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Int16, true);
        }

        public static tgQueryItem operator /(short? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgQueryItem operator /(tgQueryItem item1, int? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Int32, true);
        }

        public static tgQueryItem operator /(int? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgQueryItem operator /(tgQueryItem item1, long? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Int64, true);
        }

        public static tgQueryItem operator /(long? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgQueryItem operator /(tgQueryItem item1, sbyte? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator /(sbyte? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgQueryItem operator /(tgQueryItem item1, float? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.Single, true);
        }

        public static tgQueryItem operator /(float? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgQueryItem operator /(tgQueryItem item1, ushort? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator /(ushort? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgQueryItem operator /(tgQueryItem item1, uint? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator /(uint? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgQueryItem operator /(tgQueryItem item1, ulong? literal)
        {
            return DivideOperator(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator /(ulong? literal, tgQueryItem item1)
        {
            return DivideOperator(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #region % operator literal overloads

        // tgSystemType.Boolean
        public static tgQueryItem operator %(tgQueryItem item1, bool? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Boolean, true);
        }

        public static tgQueryItem operator %(bool? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Boolean, false);
        }

        // tgSystemType.Byte
        public static tgQueryItem operator %(tgQueryItem item1, byte? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Byte, true);
        }

        public static tgQueryItem operator %(byte? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Byte, false);
        }

        // tgSystemType.Char
        public static tgQueryItem operator %(tgQueryItem item1, char? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Char, true);
        }

        public static tgQueryItem operator %(char? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Char, false);
        }

        // tgSystemType.DateTime
        public static tgQueryItem operator %(tgQueryItem item1, DateTime? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.DateTime, true);
        }

        public static tgQueryItem operator %(DateTime? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.DateTime, false);
        }

        // tgSystemType.Double
        public static tgQueryItem operator %(tgQueryItem item1, double? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Double, true);
        }

        public static tgQueryItem operator %(double? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Double, false);
        }

        // tgSystemType.Decimal
        public static tgQueryItem operator %(tgQueryItem item1, decimal? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Decimal, true);
        }

        public static tgQueryItem operator %(decimal? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Decimal, false);
        }

        // tgSystemType.Guid
        public static tgQueryItem operator %(tgQueryItem item1, Guid? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Guid, true);
        }

        public static tgQueryItem operator %(Guid? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Guid, false);
        }

        // tgSystemType.Int16
        public static tgQueryItem operator %(tgQueryItem item1, short? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Int16, true);
        }

        public static tgQueryItem operator %(short? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Int16, false);
        }

        // tgSystemType.Int32
        public static tgQueryItem operator %(tgQueryItem item1, int? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Int32, true);
        }

        public static tgQueryItem operator %(int? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Int32, false);
        }

        // tgSystemType.Int64
        public static tgQueryItem operator %(tgQueryItem item1, long? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Int64, true);
        }

        public static tgQueryItem operator %(long? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Int64, false);
        }

        // tgSystemType.SByte
        [CLSCompliant(false)]
        public static tgQueryItem operator %(tgQueryItem item1, sbyte? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.SByte, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator %(sbyte? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.SByte, false);
        }

        // tgSystemType.Single
        public static tgQueryItem operator %(tgQueryItem item1, float? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.Single, true);
        }

        public static tgQueryItem operator %(float? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.Single, false);
        }

        // tgSystemType.UInt16
        [CLSCompliant(false)]
        public static tgQueryItem operator %(tgQueryItem item1, ushort? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.UInt16, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator %(ushort? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.UInt16, false);
        }

        // tgSystemType.UInt32
        [CLSCompliant(false)]
        public static tgQueryItem operator %(tgQueryItem item1, uint? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.UInt32, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator %(uint? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.UInt32, false);
        }

        // tgSystemType.UInt64
        [CLSCompliant(false)]
        public static tgQueryItem operator %(tgQueryItem item1, ulong? literal)
        {
            return ModuloOperator(item1, literal, tgSystemType.UInt64, true);
        }

        [CLSCompliant(false)]
        public static tgQueryItem operator %(ulong? literal, tgQueryItem item1)
        {
            return ModuloOperator(item1, literal, tgSystemType.UInt64, false);
        }
        #endregion

        #endregion

        #endregion

        #region Where Clause

        private tgComparison CreateComparisonParameter(tgComparisonOperand operand, object value)
        {
            tgComparison comparison = null;

            tgQueryItem qi = value as tgQueryItem;
            if (Object.Equals(qi, null))
            {
                comparison = new tgComparison(this.query);
                comparison.Operand = operand;

                if (this.HasExpression)
                {
                    comparison.data.Expression = this.Expression;
                }

                comparison.data.Column = this.Column;
                comparison.data.Value = value;
                comparison.SubOperators = this.SubOperators;
            }
            else
            {
                comparison = new tgComparison(this.query);
                comparison.Operand = operand;
                comparison.data.Column = this.Column;
                comparison.data.ComparisonColumn = qi.Column;
                comparison.SubOperators = qi.SubOperators;
            }

            return comparison;
        }

        private tgComparison CreateComparisonParameter(tgComparisonOperand operand)
        {
            tgComparison comparison = new tgComparison(this.query);

            comparison.Operand = operand;

            if (this.HasExpression)
            {
                comparison.data.Expression = this.Expression;
            }

            comparison.data.Column = this.Column;
            comparison.SubOperators = this.SubOperators;

            return comparison;
        }

        /// <summary>
        /// Where parameter operand creation is called by DynamicQuery.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <param name="op">E.g., tgComparisonOperand.IsNotNull</param>
        /// <returns>The tgComparison returned to DynamicQuery</returns>
        public tgComparison OP(tgComparisonOperand op)
        {
            switch (op)
            {
                case tgComparisonOperand.IsNotNull:
                case tgComparisonOperand.IsNull:
                    return CreateComparisonParameter(op);

                default:
                    throw new InvalidOperationException("Operand requires at least one value");
            }
        }

        /// <summary>
        /// Where parameter operand creation is called by DynamicQuery.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <param name="op">E.g., tgComparisonOperand.IsNotNull</param>
        /// <param name="value">The value for this comparison</param>
        /// <returns>The tgComparison returned to DynamicQuery</returns>
        public tgComparison OP(tgComparisonOperand op, object value)
        {
            switch (op)
            {
                case tgComparisonOperand.IsNull:
                case tgComparisonOperand.IsNotNull:
                    return CreateComparisonParameter(op);

                case tgComparisonOperand.Equal:
                case tgComparisonOperand.NotEqual:
                case tgComparisonOperand.GreaterThan:
                case tgComparisonOperand.GreaterThanOrEqual:
                case tgComparisonOperand.LessThan:
                case tgComparisonOperand.LessThanOrEqual:
                case tgComparisonOperand.Like:
                case tgComparisonOperand.In:
                case tgComparisonOperand.NotIn:
                case tgComparisonOperand.NotLike:
                case tgComparisonOperand.Contains:
                    return CreateComparisonParameter(op, value);

                case tgComparisonOperand.Between:
                    throw new InvalidOperationException("Between requires two parameters");

                default:
                    throw new InvalidOperationException("Invalid Operand");
            }
        }

        /// <summary>
        /// Where parameter operand creation is called by DynamicQuery.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <param name="op">E.g., tgComparisonOperand.Between</param>
        /// <param name="value1">The first value for this comparison</param>
        /// <param name="value2">The second value for this comparison</param>
        /// <returns>The tgComparison returned to DynamicQuery</returns>
        public tgComparison OP(tgComparisonOperand op, object value1, object value2)
        {
            switch (op)
            {
                case tgComparisonOperand.IsNull:
                case tgComparisonOperand.IsNotNull:
                    return CreateComparisonParameter(op);

                case tgComparisonOperand.Equal:
                case tgComparisonOperand.NotEqual:
                case tgComparisonOperand.GreaterThan:
                case tgComparisonOperand.GreaterThanOrEqual:
                case tgComparisonOperand.LessThan:
                case tgComparisonOperand.LessThanOrEqual:
                case tgComparisonOperand.In:
                case tgComparisonOperand.NotIn:
                case tgComparisonOperand.Contains:
                    return CreateComparisonParameter(op, value1);

                case tgComparisonOperand.Like:
                    return this.Like(value1, Convert.ToChar(value2));

                case tgComparisonOperand.NotLike:
                    return this.NotLike(value1, Convert.ToChar(value2));

                case tgComparisonOperand.Between:
                    return this.Between(value1, value2);

                default:
                    throw new InvalidOperationException("Invalid Operand");
            }
        }

        /// <summary>
        /// Comparison ensuring that the value passed in is EQUAL to this column.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// Where(Age.Equal(24))
        /// </code>
        /// or try the natural syntax
        /// <code>
        /// Where(Age == 24)
        /// </code>
        /// </example>
        /// <param name="value">The value for comparison.</param>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        [Obsolete("For more readable code use '==' in C# or '=' in VB.NET rather than this method")]
        public tgComparison Equal(object value)
        {
            return CreateComparisonParameter(tgComparisonOperand.Equal, value);
        }

        /// <summary>
        /// Comparison ensuring that the value passed in is NOT EQUAL to this column.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// Where(Age.NotEqual(24))
        /// </code>
        /// or try the natural syntax
        /// <code>
        /// Where(Age != 24)
        /// </code>
        /// </example>
        /// <param name="value">The value for comparison.</param>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        [Obsolete("For more readable code use '!=' in C# or '<>' in VB.NET rather than this method")] 
        public tgComparison NotEqual(object value)
        {
            return CreateComparisonParameter(tgComparisonOperand.NotEqual, value);
        }

        /// <summary>
        /// Comparison ensuring that this column is GREATER THAN the value passed in.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// Where(Age.GreaterThan(24))
        /// </code>
        /// or try the natural syntax
        /// <code>
        /// Where(Age &gt; 24)
        /// </code>
        /// </example>
        /// <param name="value">The value for comparison.</param>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        [Obsolete("For more readable code use '>' rather than this method")]
        public tgComparison GreaterThan(object value)
        {
            return CreateComparisonParameter(tgComparisonOperand.GreaterThan, value);
        }

        /// <summary>
        /// Comparison ensuring that this column is GREATER THAN OR EQUAL
        /// to the value passed in.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// Where(Age.GreaterThanOrEqual(24))
        /// </code>
        /// or try the natural syntax
        /// <code>
        /// Where(Age &gt;= 24)
        /// </code>
        /// </example>
        /// <param name="value">The value for comparison.</param>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        [Obsolete("For more readable code use '>=' rather than this method")]
        public tgComparison GreaterThanOrEqual(object value)
        {
            return CreateComparisonParameter(tgComparisonOperand.GreaterThanOrEqual, value);
        }

        /// <summary>
        /// Comparison ensuring that this column is LESS THAN the value passed in.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// Where(Age.LessThan(24))
        /// </code>
        /// or try the natural syntax
        /// <code>
        /// Where(Age &lt; 24)
        /// </code>
        /// </example>
        /// <param name="value">The value for comparison.</param>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        [Obsolete("For more readable code use '<' rather than this method")]
        public tgComparison LessThan(object value)
        {
            return CreateComparisonParameter(tgComparisonOperand.LessThan, value);
        }

        /// <summary>
        /// Comparison ensuring that this column is LESS THAN OR EQUAL
        /// to the value passed in.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// Where(Age.LessThanOrEqual(24))
        /// </code>
        /// or try the natural syntax
        /// <code>
        /// Where(Age &lt;= 24)
        /// </code>
        /// </example>
        /// <param name="value">The value for comparison.</param>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        [Obsolete("For more readable code use '<=' rather than this method")]
        public tgComparison LessThanOrEqual(object value)
        {
            return CreateComparisonParameter(tgComparisonOperand.LessThanOrEqual, value);
        }

        /// <summary>
        /// Comparison ensuring that the value passed in is LIKE this column.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// Where(LastName.Like("D%"))
        /// </code>
        /// </example>
        /// <param name="value">The value for comparison.</param>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>        
        public tgComparison Like(object value)
        {
            return CreateComparisonParameter(tgComparisonOperand.Like, value);
        }

        /// <summary>
        /// Comparison ensuring that the value passed in is LIKE this column.
        /// This overload takes a single escape character.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// The optional escape character is used if the search phrase
        /// contains one of the LIKE wildcards. For example,
        /// the following will match "30%" anywhere in the column.
        /// <code>
        /// Where(Discount.Like("%30!//", '!'))
        /// </code>
        /// </example>
        /// <param name="value">The value for comparison.</param>
        /// <param name="escapeCharacter">The single escape character.</param>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        public tgComparison Like(object value, char escapeCharacter)
        {
            tgComparison comparison = new tgComparison(this.query);
            comparison.data.Column = this.Column;
            comparison.data.LikeEscape = escapeCharacter;
            comparison.Operand = tgComparisonOperand.Like;
            comparison.SubOperators = this.SubOperators;

            tgQueryItem qi = value as tgQueryItem;
            if (Object.Equals(qi, null))
            {
                comparison.data.Value = value;
            }
            else
            {
                comparison.data.ComparisonColumn = qi.Column;
            }

            return comparison;
        }

        /// <summary>
        /// Comparison ensuring that the value passed in is NOT LIKE this column.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// Where(LastName.NotLike("D%"))
        /// </code>
        /// </example>
        /// <param name="value">The value for comparison.</param>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        public tgComparison NotLike(object value)
        {
            return CreateComparisonParameter(tgComparisonOperand.NotLike, value);
        }

        /// <summary>
        /// Comparison ensuring that the value passed in is NOT LIKE this column.
        /// This overload takes a single escape character.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// The optional escape character is used if the search phrase
        /// contains one of the LIKE wildcards. For example,
        /// the following will exclude columns with "30%" anywhere in them.
        /// <code>
        /// Where(Discount.NotLike("%30!//", '!'))
        /// </code>
        /// </example>
        /// <param name="value">The value for comparison.</param>
        /// <param name="escapeCharacter">The single escape character.</param>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        public tgComparison NotLike(object value, char escapeCharacter)
        {
            tgComparison comparison = new tgComparison(this.query);
            comparison.data.Column = this.Column;
            comparison.data.LikeEscape = escapeCharacter;
            comparison.Operand = tgComparisonOperand.NotLike;
            comparison.SubOperators = this.SubOperators;

            tgQueryItem qi = value as tgQueryItem;
            if (Object.Equals(qi, null))
            {
                comparison.data.Value = value;
            }
            else
            {
                comparison.data.ComparisonColumn = qi.Column;
            }

            return comparison;
        }

        /// <summary>
        /// Contains is used to search columns containing
        /// character-based data types for precise or
        /// fuzzy (less precise) matches to single words and phrases.
        /// The column must have a Full Text index.
        /// </summary>
        /// <remarks>
        /// See CONTAINS in the SQL server MSDN docs
        /// for a full explanation of supported search terms
        /// and how to enable Full Text searching at the
        /// database, table, and column levels.
        /// </remarks>
        /// <example>
        /// The company name contains both "Acme" and
        /// "Company" in close approximation, AND
        /// the address contains "Road", but no form of
        /// "St", "Street", "Ave", or "Avenue".
        /// <code>
        /// string nameTerm =
        ///     "Acme NEAR Company";
        /// string addressTerm =
        ///     "Road AND NOT (\"St*\" OR \"Ave*\")";
        /// 
        /// cust.Query.Where(
        ///     cust.Query.CompanyName.Contains(nameTerm));
        /// cust.Query.Where(
        ///     cust.Query.Address.Contains(addressTerm));
        /// cust.Query.Load();
        /// </code>
        /// </example>
        /// <param name="value">The value for comparison.</param>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        public tgComparison Contains(object value)
        {
            return CreateComparisonParameter(tgComparisonOperand.Contains, value);
        }

        /// <summary>
        /// Comparison ensuring that this column is NULL.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// Where(LastName.IsNull())
        /// </code>
        /// </example>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        public tgComparison IsNull()
        {
            return CreateComparisonParameter(tgComparisonOperand.IsNull);
        }

        /// <summary>
        /// Comparison ensuring that this column is NOT NULL.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// Where(LastName.IsNotNull())
        /// </code>
        /// </example>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        public tgComparison IsNotNull()
        {
            return CreateComparisonParameter(tgComparisonOperand.IsNotNull);
        }

        /// <summary>
        /// Comparison ensuring that this column is BETWEEN two values.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// Where(BirthDate.Between("2000-01-01", "2000-12-31"))
        /// </code>
        /// </example>
        /// <param name="start">The starting value for comparison.</param>
        /// <param name="end">The ending value for comparison.</param>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        public tgComparison Between(object start, object end)
        {
            tgComparison comparison = new tgComparison(this.query);
            comparison.Operand = tgComparisonOperand.Between;
            comparison.SubOperators = this.SubOperators;

            comparison.data.Column = this.Column;

            tgQueryItem qi = start as tgQueryItem;
            if (Object.Equals(qi, null))
            {
                comparison.BetweenBegin = start;
            }
            else
            {
                comparison.data.ComparisonColumn = qi.Column;
            }

            qi = end as tgQueryItem;
            if (Object.Equals(qi, null))
            {
                comparison.BetweenEnd = end;
            }
            else
            {
                comparison.data.ComparisonColumn2 = qi.Column;
            }

            comparison.SubOperators = this.SubOperators;

            return comparison;
        }

        /// <summary>
        /// Comparison ensuring that this column is IN a list of values.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// Where(LastName.In("Doe", "Smith", "Johnson"))
        /// </code>
        /// </example>
        /// <param name="value">The list of values for comparison.</param>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        public tgComparison In(params object[] value)
        {
            List<object> values = new List<object>();

            #region Convert object[] into a List<object>

            object[] oValues = (object[])value;

            foreach (object o in oValues)
            {
                string str = o as string;
                if (str != null)
                {
                    // String supports IEnumerable and we don't want to break
                    // up each individual character
                    values.Add(o);
                }
                else
                {
                    IEnumerable enumer = o as IEnumerable;
                    if (enumer != null)
                    {
                        foreach (object oo in enumer)
                        {
                            values.Add(oo);
                        }
                    }
                    else
                    {
                        values.Add(o);
                    }
                }
            }
            #endregion

            tgComparison comparison = new tgComparison(this.query);
            comparison.Operand = tgComparisonOperand.In;
            comparison.data.Column = this.Column;
            comparison.SubOperators = this.SubOperators;
            comparison.Values = values;
            return comparison;
        }


        /// <summary>
        /// Comparison ensuring that this column is IN a list of values.
        /// </summary>
        /// <param name="value">The Query to provide the matching values for the IN comparison</param>
        /// <returns></returns>
        public tgComparison In(tgDynamicQuerySerializable subQuery)
        {
            tgComparison comparison = new tgComparison(this.query);
            comparison.Operand = tgComparisonOperand.In;
            comparison.data.Column = this.Column;
            comparison.SubOperators = this.SubOperators;
            comparison.Value = subQuery;

            this.query.AddQueryToList(subQuery);

            return comparison;
        }

        /// <summary>
        /// Comparison ensuring that this column is NOT IN a list of values.
        /// See <see cref="tgComparisonOperand"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// Where(LastName.NotIn("Doe", "Smith", "Johnson"))
        /// </code>
        /// </example>
        /// <param name="value">The list of values for comparison.</param>
        /// <returns>The tgComparison returned to DynamicQuery.</returns>
        public tgComparison NotIn(params object[] value)
        {
            List<object> values = new List<object>();

            #region Convert object[] into a List<object>

            object[] oValues = (object[])value;

            foreach (object o in oValues)
            {
                string str = o as string;
                if (str != null)
                {
                    // String supports IEnumerable and we don't want to break
                    // up each individual character
                    values.Add(o);
                }
                else
                {
                    IEnumerable enumer = o as IEnumerable;
                    if (enumer != null)
                    {
                        foreach (object oo in enumer)
                        {
                            values.Add(oo);
                        }
                    }
                    else
                    {
                        values.Add(o);
                    }
                }
            }
            #endregion

            tgComparison comparison = new tgComparison(this.query);
            comparison.Operand = tgComparisonOperand.NotIn;
            comparison.data.Column = this.Column;
            comparison.SubOperators = this.SubOperators;
            comparison.Values = values;
            return comparison;
        }

        /// <summary>
        /// Comparison ensuring that this column is NOT IN a list of values.
        /// </summary>
        /// <param name="value">The Query to provide the matching values for the NOT IN comparison</param>
        /// <returns></returns>
        public tgComparison NotIn(tgDynamicQuerySerializable subQuery)
        {
            tgComparison comparison = new tgComparison(this.query);
            comparison.Operand = tgComparisonOperand.NotIn;
            comparison.data.Column = this.Column;
            comparison.SubOperators = this.SubOperators;
            comparison.Value = subQuery;

            this.query.AddQueryToList(subQuery);

            return comparison;
        }
        #endregion

        //public tgCase Case(tgQueryItem column)
        //{
        //    this.CaseWhen = new tgCase(this.query, this, column);
        //    return this.CaseWhen;
        //}

        public tgCase Case()
        {
            this.CaseWhen = new tgCase(this.query, this);
            return this.CaseWhen;
        }

        #region OrderBy

        /// <summary>
        /// Sort in descending order.
        /// See <see cref="tgOrderByDirection"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// emps.Query.OrderBy(emps.Query.LastName.Descending);
        /// </code>
        /// </example>
        /// <returns>The tgOrderByItem returned to DynamicQuery.</returns>
        public tgOrderByItem Descending
        {
            get 
            {
                tgOrderByItem item = new tgOrderByItem();
                item.Direction = tgOrderByDirection.Descending;
                item.Expression = this;
                item.Expression.Query = this.query;
                return item;
            }
        }

        /// <summary>
        /// Sort in ascending order.
        /// See <see cref="tgOrderByDirection"/> Enumeration.
        /// </summary>
        /// <example>
        /// <code>
        /// emps.Query.OrderBy(emps.Query.LastName.Ascending);
        /// </code>
        /// </example>
        /// <returns>The tgOrderByItem returned to DynamicQuery.</returns>
        public tgOrderByItem Ascending
        {
            get
            {
                tgOrderByItem item = new tgOrderByItem();
                item.Direction = tgOrderByDirection.Ascending;
                item.Expression = this;
                item.Expression.Query = this.query;
                return item;
            }
        }

        #endregion

        #region Cast

        /// <summary>
        /// Cast informs the DataProviders that a SQL CAST operation is needed.
        /// </summary>
        /// <remarks>
        /// In C# you can cast with the overloaded cast operators, like this: (tgString)query.Age
        /// </remarks>
        /// <param name="castType">The type of cast needed</param>
        /// <returns>The very same tgQueryItem now with Cast instructions</returns>
        public tgQueryItem Cast(tgCastType castType)
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.Cast;
            subOp.Parameters["tgCastType"] = castType;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Cast informs the DataProviders that a SQL CAST operation is needed. This overloaded version
        /// of Cast is useful for Casting variable length character columns
        /// </summary>
        /// <remarks>
        /// In C# you can cast with the overloaded cast operators, like this: (tgString)query.Age
        /// </remarks>
        /// <param name="castType">The type of cast needed</param>
        /// <returns>The very same tgQueryItem now with Cast instructions</returns>
        public tgQueryItem Cast(tgCastType castType, int length)
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.Cast;
            subOp.Parameters["tgCastType"] = castType;
            subOp.Parameters["length"] = length;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Cast informs the DataProviders that a SQL CAST operation is needed. This overloaded version
        /// of Cast is useful for Casting decimal types
        /// </summary>
        /// <remarks>
        /// In C# you can cast with the overloaded cast operators, like this: (tgString)query.Age
        /// </remarks>
        /// <param name="castType">The type of cast needed</param>
        /// <returns>The very same tgQueryItem now with Cast instructions</returns>
        public tgQueryItem Cast(tgCastType castType, int precision, int scale)
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.Cast;
            subOp.Parameters["tgCastType"] = castType;
            subOp.Parameters["precision"] = precision;
            subOp.Parameters["scale"] = scale;
            this.AddSubOperator(subOp);

            return this;
        }

        #endregion

        /// <summary>
        /// Privides the Ability to Alias a column name
        /// </summary>
        /// <param name="alias">The Alias Name</param>
        /// <returns>tgQueryItem</returns>
        public tgQueryItem As(string alias)
        {
            this.Column.Alias = alias;
            return this;
        }

        #region Sub Operators

        /// <summary>
        /// Returns the column in UPPER CASE
        /// </summary>
         public tgQueryItem ToUpper()
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.ToUpper;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Returns the column in LOWER CASE
        /// </summary>
        public tgQueryItem ToLower()
        {    
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.ToLower;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Performs a Left Trim (remove blanks) on the column
        /// </summary>
        public tgQueryItem LTrim()
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.LTrim;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Performs a Right Trim (remove blanks) on the column
        /// </summary>
        public  tgQueryItem RTrim()
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.RTrim;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Removes blanks from the beginning and end of the column
        /// </summary>
        public tgQueryItem Trim()
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.Trim;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Returns a portion of the string column
        /// </summary>
        /// <param name="start">The starting character</param>
        /// <param name="length">How many characters to return</param>
        public tgQueryItem Substring(System.Int64 start, System.Int64 length)
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.SubString;
            subOp.Parameters["start"] = start;
            subOp.Parameters["length"] = length;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Returns a portion of the string column
        /// </summary>
        /// <param name="length">How many characters to return</param>
        public tgQueryItem Substring(System.Int64 length)
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.SubString;
            subOp.Parameters["length"] = length;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Returns a portion of the string column
        /// </summary>
        /// <param name="start">The starting character</param>
        /// <param name="length">How many characters to return</param>
        public tgQueryItem Substring(int start, int length)
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.SubString;
            subOp.Parameters["start"] = start;
            subOp.Parameters["length"] = length;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Returns a portion of the string column
        /// </summary>
        /// <param name="length">How many characters to return</param>
        public tgQueryItem Substring(int length)
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.SubString;
            subOp.Parameters["length"] = length;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// This can be used to return the first non null parameter.
        /// </summary>
        /// <remarks>
        /// The code below will return "Smith" is the LastName column in the database is null.
        /// <code>
        /// MyCollection coll = new MyCollection();
        /// coll.Query.Select(coll.Query.LastName.Coalesce("'Smith'"));
        /// coll.Query.Load();
        /// </code>
        /// </remarks>
        /// <param name="expresssions">The value to return if null</param>
        public tgQueryItem Coalesce(string expresssions)
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.Coalesce;
            subOp.Parameters["expressions"] = expresssions;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Returns the Date only portion of a date columm
        /// </summary>
        /// <returns></returns>
        public tgQueryItem Date()
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.Date;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Returns the length of a character based column
        /// </summary>
        public tgQueryItem Length()
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.Length;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Performs a round on the column
        /// </summary>
        /// <param name="significantDigits">Round to the number of significant digits</param>
        public tgQueryItem Round(int significantDigits)
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.Round;
            subOp.Parameters["SignificantDigits"] = significantDigits;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Returns a particular date part of a date column
        /// </summary>
        /// <param name="datePart"></param>
        public tgQueryItem DatePart(string datePart)
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.DatePart;
            subOp.Parameters["DatePart"] = datePart;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Aggregate Sum.
        /// See <see cref="tgQuerySubOperatorType"/> Enumeration.
        /// </summary>
        /// <example>
        /// Aggregate Sum with the column name as the default Alias.
        /// <code>
        /// emps.Query.Select(emps.Query.Age.Sum());
        /// </code>
        /// </example>
        /// <returns>The esAggregateItem returned to DynamicQuery.</returns>
        public tgQueryItem Sum()
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.Sum;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Aggregate Avg.
        /// See <see cref="tgQuerySubOperatorType"/> Enumeration.
        /// </summary>
        /// <example>
        /// Aggregate Avg with the column name as the default Alias.
        /// <code>
        /// emps.Query.Select(emps.Query.Age.Avg());
        /// </code>
        /// </example>
        /// <returns>The esAggregateItem returned to DynamicQuery.</returns>
        public tgQueryItem Avg()
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.Avg;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Aggregate Max.
        /// See <see cref="tgQuerySubOperatorType"/> Enumeration.
        /// </summary>
        /// <example>
        /// Aggregate Max with the column name as the default Alias.
        /// <code>
        /// emps.Query.Select(emps.Query.Age.Max());
        /// </code>
        /// </example>
        /// <returns>The esAggregateItem returned to DynamicQuery.</returns>
        public tgQueryItem Max()
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.Max;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Aggregate Min.
        /// See <see cref="tgQuerySubOperatorType"/> Enumeration.
        /// </summary>
        /// <example>
        /// Aggregate Min with the column name as the default Alias.
        /// <code>
        /// emps.Query.Select(emps.Query.Age.Min());
        /// </code>
        /// </example>
        /// <returns>The esAggregateItem returned to DynamicQuery.</returns>
        public tgQueryItem Min()
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.Min;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Aggregate StdDev.
        /// See <see cref="tgQuerySubOperatorType"/> Enumeration.
        /// </summary>
        /// <example>
        /// Aggregate StdDev with the column name as the default Alias.
        /// <code>
        /// emps.Query.Select(emps.Query.Age.StdDev());
        /// </code>
        /// </example>
        /// <returns>The esAggregateItem returned to DynamicQuery.</returns>
        public tgQueryItem StdDev()
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.StdDev;
            this.AddSubOperator(subOp);

            return this;
        }

        /// <summary>
        /// Aggregate Var.
        /// See <see cref="tgQuerySubOperatorType"/> Enumeration.
        /// </summary>
        /// <example>
        /// Aggregate Var with the column name as the default Alias.
        /// <code>
        /// emps.Query.Select(emps.Query.Age.Var());
        /// </code>
        /// </example>
        /// <returns>The esAggregateItem returned to DynamicQuery.</returns>
        public tgQueryItem Var()
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.Var;
            this.AddSubOperator(subOp);
           
            return this;
        }

        /// <summary>
        /// Aggregate Count.
        /// See <see cref="tgQuerySubOperatorType"/> Enumeration.
        /// </summary>
        /// <example>
        /// Aggregate Count with the column name as the default Alias.
        /// <code>
        /// emps.Query.Select(emps.Query.Age.Count());
        /// </code>
        /// </example>
        /// <returns>The esAggregateItem returned to DynamicQuery.</returns>
        public tgQueryItem Count()
        {
            tgQuerySubOperator subOp = new tgQuerySubOperator();
            subOp.SubOperator = tgQuerySubOperatorType.Count;
            this.AddSubOperator(subOp);

            return this;
        }

        #endregion

        /// <summary>
        /// Set to true for (DISTINCT columnName).
        /// </summary>
        public tgQueryItem Distinct()
        {
            this.Column.Distinct = true;
            return this;
        }

        /// <summary>
        /// Required due to operator overloading. Use 'Equal' not 'Equals'
        /// for DynamicQueries.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object o)
        {
            throw new NotImplementedException("Use 'Equal' not 'Equals'");
        }

        /// <summary>
        /// Required due to operator overloading.
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// ToString() (to use in GroupBy/OrderBy and such ....)
        /// </summary>
        public override string ToString()
        {
            return this.Column.Name;
        }

        #region Cast Operators

        public static implicit operator tgBoolean(tgQueryItem item)
        {
            return new tgBoolean(item);
        }

        public static implicit operator tgByte(tgQueryItem item)
        {
            return new tgByte(item);
        }

        public static implicit operator tgChar(tgQueryItem item)
        {
            return new tgChar(item);
        }

        public static implicit operator tgDateTime(tgQueryItem item)
        {
            return new tgDateTime(item);
        }

        public static implicit operator tgDouble(tgQueryItem item)
        {
            return new tgDouble(item);
        }

        public static implicit operator tgDecimal(tgQueryItem item)
        {
            return new tgDecimal(item);
        }

        public static implicit operator tgGuid(tgQueryItem item)
        {
            return new tgGuid(item);
        }

        public static implicit operator tgInt16(tgQueryItem item)
        {
            return new tgInt16(item);
        }

        public static implicit operator tgInt32(tgQueryItem item)
        {
            return new tgInt32(item);
        }

        public static implicit operator tgInt64(tgQueryItem item)
        {
            return new tgInt64(item);
        }

        public static implicit operator tgSingle(tgQueryItem item)
        {
            return new tgSingle(item);
        }

        public static implicit operator tgString(tgQueryItem item)
        {
            return new tgString(item);
        }

        #endregion

        /// <summary>
        /// ToString() (to use in GroupBy/OrderBy and such ....)
        /// </summary>
        public static implicit operator string(tgQueryItem item)
        {
            return item.Column.Name;
        }

        /// <summary>
        /// ToString() (to use in GroupBy/OrderBy and such ....)
        /// </summary>
        public static implicit operator tgColumnItem(tgQueryItem item)
        {
            return item.Column;
        }

        /// <summary>
        /// ToString() (to use in GroupBy/OrderBy and such ....)
        /// </summary>
        public static implicit operator tgExpression(tgQueryItem item)
        {
            tgExpression sItem = new tgExpression();
            sItem.Column = item.Column;
            sItem.CaseWhen = item.CaseWhen;
            sItem.SubOperators = item.SubOperators;
            sItem.MathmaticalExpression = item.Expression;
            return sItem;
        }

        private void AddSubOperator(tgQuerySubOperator subOperator)
        {
            if (this.SubOperators == null)
            {
                this.SubOperators = new List<tgQuerySubOperator>();
            }

            this.SubOperators.Add(subOperator);
        }

        //-----------------------------
        // Data
        //-----------------------------
        internal tgColumnItem Column;
        internal List<tgQuerySubOperator> SubOperators;
        internal tgCase CaseWhen;
        private tgDynamicQuerySerializable query;

        /// <summary>
        /// Used Internally by EntitySpaces
        /// </summary>
        public bool HasExpression
        {
            get
            {
                return this.Expression != null;
            }
        }

        internal tgMathmaticalExpression Expression;
    }
}
