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
    [Serializable]
    [DataContract(Namespace = "tg", IsReference = true)]
    public class tgCase
    {
        [NonSerialized]
        private tgExpressionOrComparison WhenItem;

        /// <summary>
        /// The Constructor
        /// </summary>
        public tgCase(tgDynamicQuerySerializable query, tgQueryItem queryItem)
        {
            this.data.Query = query;
            this.data.QueryItem = queryItem;
        }

        /// <summary>
        /// The Constructor
        /// </summary>
        public tgCase(tgDynamicQuerySerializable query)
        {
            this.data.Query = query;
        }

        public tgCase When(object value)
        {
            this.WhenItem = new tgExpressionOrComparison();
            this.WhenItem.Expression = new tgExpression();
            this.WhenItem.Expression.LiteralValue = value;

            return this;
        }

        public tgCase When(tgQueryItem ex)
        {
            return this;
        }

        public tgCase When(tgExpression ex)
        {
            return this;
        }

        public tgCase When(tgComparison comparison)
        {
            this.WhenItem = new tgExpressionOrComparison();
            this.WhenItem.Comparisons = new List<tgComparison>();

            if (comparison != null)
            {
                if (comparison.data.WhereExpression != null)
                {
                    foreach (tgComparison exp in comparison.data.WhereExpression)
                    {
                        tgDynamicQuerySerializable q = exp.Value as tgDynamicQuerySerializable;

                        if (q != null)
                        {
                            IDynamicQuerySerializableInternal iQ = q as IDynamicQuerySerializableInternal;
                            iQ.HookupProviderMetadata(q);
                        }
                    }

                    this.WhenItem.Comparisons.AddRange(comparison.data.WhereExpression);
                }
                else
                {
                    this.WhenItem.Comparisons.Add(comparison);
                }

                tgDynamicQuerySerializable query = comparison.Value as tgDynamicQuerySerializable;

                if (query != null)
                {
                    IDynamicQuerySerializableInternal iQ = query as IDynamicQuerySerializableInternal;
                    iQ.HookupProviderMetadata(query);
                }
            }

            return this;
        }

        #region Then

        public tgCase Then(object value)
        {

            tgSimpleCaseData.tgCaseClause clause = new tgSimpleCaseData.tgCaseClause();
            clause.When = this.WhenItem;
            clause.Then = new tgExpression();
            clause.Then.LiteralValue = value;

            if (data.Cases == null)
            {
                data.Cases = new List<tgSimpleCaseData.tgCaseClause>();
            }

            this.data.Cases.Add(clause);

            return this;
        }

        public tgCase Then(tgQueryItem queryItem)
        {
            tgExpression expression = queryItem;

            tgSimpleCaseData.tgCaseClause clause = new tgSimpleCaseData.tgCaseClause();
            clause.When = this.WhenItem;
            clause.Then = expression;

            if (data.Cases == null)
            {
                data.Cases = new List<tgSimpleCaseData.tgCaseClause>();
            }

            this.data.Cases.Add(clause);

            return this;
        }

        public tgCase Then(tgExpression expression)
        {
            tgSimpleCaseData.tgCaseClause clause = new tgSimpleCaseData.tgCaseClause();
            clause.When = this.WhenItem;
            clause.Then = expression;

            if (data.Cases == null)
            {
                data.Cases = new List<tgSimpleCaseData.tgCaseClause>();
            }

            this.data.Cases.Add(clause);

            return this;
        }

        #endregion

        #region Else

        public tgCase Else(object value)
        {
            this.data.Else = new tgExpression();
            this.data.Else.LiteralValue = value;

            return this;
        }

        public tgCase Else(tgQueryItem queryItem)
        {
            tgExpression expression = queryItem;
            this.data.Else = expression;

            return this;
        }

        public tgCase Else(tgExpression expression)
        {
            this.data.Else = expression;
            return this;
        }

        #endregion

        #region End

        public tgQueryItem End()
        {
            return this.data.QueryItem;
        }

        #endregion

        /// <summary>
        /// Used internally by EntitySpaces to make the <see cref="tgJoinItem"/> classes data available to the
        /// EntitySpaces data providers.
        /// </summary>
        [Serializable]
        [DataContract(Namespace = "tg")]
        public struct tgSimpleCaseData
        {
            public struct tgCaseClause
            {
                public tgExpressionOrComparison When;
                public tgExpression Then;
            }

            public tgDynamicQuerySerializable Query;
            public tgQueryItem QueryItem;

            public List<tgCaseClause> Cases;
            public tgExpression Else;
        }

        [DataMember(Name = "Data", EmitDefaultValue = false)]
        internal tgSimpleCaseData data;

        public static implicit operator tgSimpleCaseData(tgCase caseWhen)
        {
            return caseWhen.data;
        }
    }
}
