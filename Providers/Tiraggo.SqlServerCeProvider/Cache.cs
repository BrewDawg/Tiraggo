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

using Tiraggo.Interfaces;

namespace Tiraggo.SqlServerCeProvider
{
    class Cache
    {
        static public Dictionary<string, SqlCeParameter> GetParameters(tgDataRequest request)
        {
            return GetParameters(request.DataID, request.ProviderMetadata, request.Columns);
        }

        static public Dictionary<string, SqlCeParameter> GetParameters(Guid dataID,
            tgProviderSpecificMetadata providerMetadata, tgColumnMetadataCollection columns)
        {
            lock (parameterCache)
            {
                if (!parameterCache.ContainsKey(dataID))
                {
                    // The Parameters for this Table haven't been cached yet, this is a one time operation
                    Dictionary<string, SqlCeParameter> types = new Dictionary<string, SqlCeParameter>();

                    SqlCeParameter param1;
                    foreach (tgColumnMetadata col in columns)
                    {
                        tgTypeMap typeMap = providerMetadata.GetTypeMap(col.PropertyName);
                        if (typeMap != null)
                        {
                            string nativeType = typeMap.NativeType;
                            SqlDbType dbType = Cache.NativeTypeToDbType(nativeType);

                            param1 = new SqlCeParameter(Delimiters.Param + col.PropertyName, dbType, 0, col.Name);
                            param1.SourceColumn = col.Name;

                            switch (dbType)
                            {
                                case SqlDbType.Decimal:

                                    param1.Precision = (byte)col.NumericPrecision;
                                    param1.Scale = (byte)col.NumericScale;
                                    break;

                                case SqlDbType.DateTime:

                                    param1.Precision = 23;
                                    param1.Scale = 3;
                                    break;

                                case SqlDbType.SmallDateTime:

                                    param1.Precision = 16;
                                    break;

                            }
                            types[col.Name] = param1;
                        }
                    }

                    parameterCache[dataID] = types;
                }
            }

            return parameterCache[dataID];
        }

        static private SqlDbType NativeTypeToDbType(string nativeType)
        {
            switch (nativeType)
            {
                case "bigint": return SqlDbType.BigInt;
                case "binary": return SqlDbType.Binary;
                case "bit": return SqlDbType.Bit;
                case "char": return SqlDbType.Char;
                case "datetime": return SqlDbType.DateTime;
                case "decimal": return SqlDbType.Decimal;
                case "float": return SqlDbType.Float;
                case "image": return SqlDbType.Image;
                case "int": return SqlDbType.Int;
                case "money": return SqlDbType.Money;
                case "nchar": return SqlDbType.NChar;
                case "ntext": return SqlDbType.NText;
                case "numeric": return SqlDbType.Decimal;
                case "nvarchar": return SqlDbType.NVarChar;
                case "real": return SqlDbType.Real;
                case "rowversion": return SqlDbType.Timestamp;
                case "smalldatetime": return SqlDbType.SmallDateTime;
                case "smallint": return SqlDbType.SmallInt;
                case "smallmoney": return SqlDbType.SmallMoney;
                case "text": return SqlDbType.Text;
                case "timestamp": return SqlDbType.Timestamp;
                case "tinyint": return SqlDbType.TinyInt;
                case "uniqueidentifier": return SqlDbType.UniqueIdentifier;
                case "varbinary": return SqlDbType.VarBinary;
                case "varchar": return SqlDbType.VarChar;
                case "xml": return SqlDbType.Xml;
                case "sql_variant": return SqlDbType.Variant;

                default:
                    return SqlDbType.Udt;
            }
        }

        static public SqlCeParameter CloneParameter(SqlCeParameter p)
        {
            ICloneable param = p as ICloneable;
            return param.Clone() as SqlCeParameter;
        }

        static private Dictionary<Guid, Dictionary<string, SqlCeParameter>> parameterCache
            = new Dictionary<Guid, Dictionary<string, SqlCeParameter>>();
    }
}
