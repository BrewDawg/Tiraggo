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

using Tiraggo.Core;
using Tiraggo.Interfaces;

namespace Tiraggo.js
{
    public class jsColumn
    {
        public bool isVisible;
        public string displayName;
        public string dataType;
        public string columnName;
        public string propertyName;
        public int ordinal;
        public int numericPrecision;
        public int numericScale;
        public long characterMaxLength;
        public string defaultValue;
        public bool hasDefault;
        public bool isInPrimaryKey;
        public bool isAutoIncrement;
        public bool isNullable;
        public bool isConcurrency;
        public bool isSortable;
        public string footerValue;

        static public jsColumn[] PopulateColumns(tgEntity entity)
        {
            List<jsColumn> cols = new List<jsColumn>();

            // we just put some fake data in the footer section for the demo
            decimal footer = 100M;

            List<string> columns = entity.GetCurrentListOfColumns();

            foreach (string column in columns)
            {
                tgColumnMetadata esCol = entity.tg.Meta.Columns.FindByColumnName(column);

                jsColumn c = new jsColumn();

                footer += 2M;

                if (esCol != null)
                {
                    c.isVisible = true;
                    c.displayName = esCol.PropertyName;
                    c.dataType = esCol.Type.ToString();
                    c.columnName = esCol.Name;
                    c.propertyName = esCol.PropertyName;
                    c.ordinal = esCol.Ordinal;
                    c.numericPrecision = esCol.NumericPrecision;
                    c.numericScale = esCol.NumericScale;
                    c.characterMaxLength = esCol.CharacterMaxLength;
                    c.defaultValue = esCol.Default;
                    c.hasDefault = esCol.HasDefault;
                    c.isInPrimaryKey = esCol.IsInPrimaryKey;
                    c.isAutoIncrement = esCol.IsAutoIncrement;
                    c.isNullable = esCol.IsNullable;
                    c.isConcurrency = esCol.IsConcurrency || esCol.IsEntitySpacesConcurrency;
                    c.isSortable = true;
                    c.footerValue = "$" + Convert.ToString(footer);
                }
                else
                {
                    object o = entity.GetColumn(column);

                    if (o != DBNull.Value && o != null)
                    {
                        c.dataType = o.GetType().ToString();
                    }

                    c.isVisible = true;
                    c.displayName = column;
                    c.columnName = column;
                    c.propertyName = column;
                    c.isSortable = false;
                    c.footerValue = "$" + Convert.ToString(footer);
                }

                cols.Add(c);
            }

            return cols.ToArray();
        }
    }
}
