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

using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Web.UI.Design;

using Tiraggo.Interfaces;

namespace Tiraggo.Web.Design
{
    internal class esDataSourceDesignerView : DesignerDataSourceView
    {
        private esDataSourceDesigner owner;

        internal tgColumnMetadataCollection esColumnCollection;
        internal string esCollectionName;
        internal List<string> SelectedColumns;

        internal esDataSourceDesignerView(esDataSourceDesigner owner, string name)
            : base(owner, name)
        {
			this.owner = owner;
		}

        public override IEnumerable GetDesignTimeData(int minimumRows, out bool isSampleData)
        {
            isSampleData = false;
            if (this.SelectedColumns != null)
            {
                DataTable table = new DataTable();
                foreach (string columnName in this.SelectedColumns)
                {
                    tgColumnMetadata col = this.esColumnCollection.FindByPropertyName(columnName);

                    if (!col.IsConcurrency)
                    {
                        DataColumn dc = table.Columns.Add(col.PropertyName, col.Type);

                        if (col.IsInPrimaryKey)
                        {
                            dc.Unique = true;
                        }
                    }
                }

                if (table != null)
                {
                    isSampleData = true;
                    return DesignTimeData.GetDesignTimeDataSource(DesignTimeData.CreateSampleDataTable(new DataView(table), true), minimumRows);
                }
                return base.GetDesignTimeData(minimumRows, out isSampleData);
            }
            else
            {
                return null;
            }
        }

        public override bool CanDelete
        {
            get { return true; }
        }

        public override bool CanPage
        {
            get { return true; }
        }

        public override bool CanSort
        {
            get { return true; }
        }

        public override bool CanInsert
        {
            get { return true; }
        }

        public override bool CanUpdate
        {
            get { return true; }
        }

        public override IDataSourceViewSchema Schema
        {
            get
            {
                if (this.SelectedColumns != null)
                {
                    DataTable table = new DataTable();

                    foreach (string columnName in this.SelectedColumns)
                    {
                        tgColumnMetadata col = this.esColumnCollection.FindByPropertyName(columnName);

                        if (!col.IsConcurrency)
                        {
                            DataColumn dc = table.Columns.Add(col.PropertyName, col.Type);

                            if (col.IsInPrimaryKey)
                            {
                                dc.Unique = true;
                            }

                            if (col.IsReadOnly || col.IsComputed || col.IsConcurrency)
                            {
                                dc.ReadOnly = true;
                            }

                            dc.AutoIncrement = col.IsAutoIncrement;
                            dc.DataType = col.Type;
                            dc.ColumnMapping = MappingType.Element;
                            dc.Unique = col.IsInPrimaryKey;
                        }
                    }

                    ArrayList list = new ArrayList();

                    tgColumnMetadataCollection esCols = this.esColumnCollection;
                    tgColumnMetadata esCol;

                    DataColumnCollection cols = table.Columns;
                    for (int i = 0; i < esCols.Count; i++)
                    {
                        esCol = esCols[i];
                        if (esCol.IsInPrimaryKey)
                        {
                            list.Add(cols[esCol.PropertyName]);
                        }
                    }

                    DataColumn temp = new DataColumn();
                    DataColumn[] pks = list.ToArray(temp.GetType()) as DataColumn[];

                    table.PrimaryKey = pks;

                    DataSetViewSchema schema = new DataSetViewSchema(table);
                    return schema;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
