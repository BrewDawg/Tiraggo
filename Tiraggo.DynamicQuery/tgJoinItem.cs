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
    /// Created when Query.InnerJoin (LeftJoin, RightJoin, FullJoin) is called.
    /// </summary>
    [Serializable]
    [DataContract(Namespace = "tg", IsReference = true)]
    public class tgJoinItem
    {
        [NonSerialized]
        private tgDynamicQuerySerializable parentQuery;

        /// <summary>
        /// The Constructor
        /// </summary>
        public tgJoinItem()
        {

        }

        /// <summary>
        /// The Constructor
        /// </summary>
        public tgJoinItem(tgDynamicQuerySerializable parentQuery)
        {
            this.parentQuery = parentQuery;
        }

        /// <summary>
        /// Used to describe the "where" conditions of the join itself
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public tgDynamicQuerySerializable On(params object[] items)
        {
            if (this.data.WhereItems == null)
            {
                this.data.WhereItems = new List<tgComparison>();
            }

            foreach (object item in items)
            {
                tgComparison wi = item as tgComparison;

                if (wi != null)
                {
                    if (wi.data.WhereExpression != null)
                    {
                        foreach (tgComparison exp in wi.data.WhereExpression)
                        {
                            tgDynamicQuerySerializable q = exp.Value as tgDynamicQuerySerializable;

                            if (q != null)
                            {
                                IDynamicQuerySerializableInternal iQ = q as IDynamicQuerySerializableInternal;
                                iQ.HookupProviderMetadata(q);
                            }                            
                        }

                        this.data.WhereItems.AddRange(wi.data.WhereExpression);
                    }
                    else
                    {
                        this.data.WhereItems.Add(wi);
                    }

                    tgDynamicQuerySerializable query = wi.Value as tgDynamicQuerySerializable;

                    if (query != null)
                    {
                        IDynamicQuerySerializableInternal iQ = query as IDynamicQuerySerializableInternal;
                        iQ.HookupProviderMetadata(query);
                    }
                }
                else
                {
                    throw new Exception("Unsupported Join Syntax");
                }
            }

            return this.parentQuery;
        }

        #region ProcessWhereItems
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
                        throw new Exception("Unsupported Type");
                    }
                }
            }

            items.Add(new tgComparison(tgParenthesis.Close));

            return items;
        }
        #endregion

        /// <summary>
        /// Used internally by EntitySpaces to make the <see cref="tgJoinItem"/> classes data available to the
        /// EntitySpaces data providers.
        /// </summary>
  
        [Serializable]
        [DataContract(Namespace = "tg")]
        public struct tgJoinItemData
        {
            /// <summary>
            /// The Query that makes up the join
            /// </summary>
            [DataMember(Name = "Query", Order = 99, EmitDefaultValue = false)]
            public tgDynamicQuerySerializable Query;

            /// <summary>
            /// The join type, InnerJoin, LeftJoin, ...
            /// </summary>
            [DataMember(Name = "JoinType", EmitDefaultValue = false)]
            public tgJoinType JoinType;

            /// <summary>
            /// The where conditions for the subquery
            /// </summary>
            [DataMember(Name = "WhereItems", EmitDefaultValue = false)]
            public List<tgComparison> WhereItems;
        }

        /// <summary>
        /// The data is hidden from intellisense, however, the providers, can typecast
        /// the tgJoinItem and get to the real data without properties having to 
        /// be exposed thereby cluttering up the intellisense.
        /// </summary>
        /// <param name="join"></param>
        /// <returns></returns>
        public static explicit operator tgJoinItemData(tgJoinItem join)
        {
            return join.data;
        }

        [DataMember(Name = "Data", EmitDefaultValue = false)]
        internal tgJoinItemData data;
    }
}
