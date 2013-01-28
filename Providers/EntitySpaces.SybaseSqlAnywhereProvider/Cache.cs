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

using Tiraggo.Interfaces;

using iAnywhere.Data.SQLAnywhere;

namespace Tiraggo.SybaseSqlAnywhereProvider
{
    class Cache
    {
        static public Dictionary<string, SAParameter> GetParameters(tgDataRequest request)
        {
            return GetParameters(request.DataID, request.ProviderMetadata, request.Columns);
        }

        static public Dictionary<string, SAParameter> GetParameters(Guid dataID, 
            tgProviderSpecificMetadata providerMetadata, tgColumnMetadataCollection columns)
        {
            lock (parameterCache)
            {
                if (!parameterCache.ContainsKey(dataID))
                {
                    // The Parameters for this Table haven't been cached yet, this is a one time operation
                    Dictionary<string, SAParameter> types = new Dictionary<string, SAParameter>();

                    SAParameter param1;
                    foreach (tgColumnMetadata col in columns)
                    {
                        tgTypeMap typeMap = providerMetadata.GetTypeMap(col.PropertyName);
                        if (typeMap != null)
                        {
                            string nativeType = typeMap.NativeType;
                            SADbType dbType = Cache.NativeTypeToDbType(nativeType);

                            param1 = new SAParameter(Delimiters.Param + col.PropertyName, dbType, 0, col.Name);
                            param1.SourceColumn = col.Name;

                            switch (dbType)
                            {
                                case SADbType.BigInt:
                                case SADbType.Decimal:
                                case SADbType.Float:
                                case SADbType.Integer:
                                case SADbType.Money:
                                case SADbType.Real:
                                case SADbType.SmallMoney:
                                case SADbType.TinyInt:
                                case SADbType.SmallInt:

                                    param1.Size = (int)col.CharacterMaxLength;
                                    param1.Precision = (byte)col.NumericPrecision;
                                    param1.Scale = (byte)col.NumericScale;
                                    break;

                                case SADbType.VarChar:
                                    param1.Size = (int)col.CharacterMaxLength;
                                    break;

                                //case SADbType.Integer:
                                //    param1.Size = 4;
                                //    break;

                                case SADbType.Date:
                                case SADbType.DateTime:

                                    param1.Precision = 23;
                                    param1.Scale = 3;
                                    break;

                                case SADbType.SmallDateTime:

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

        static private SADbType NativeTypeToDbType(string nativeType)
        {
            switch(nativeType)
            {
                case "bigint": return SADbType.BigInt;
                case "binary": return SADbType.Binary;
                case "bit": return SADbType.Bit;
                case "char": return SADbType.Char;
                case "date": return SADbType.Date;
                case "datetime": return SADbType.DateTime;
                case "decimal": return SADbType.Decimal;
                case "float": return SADbType.Float;
                case "image": return SADbType.Image;
                case "integer": return SADbType.Integer;
                case "money": return SADbType.Money;
                case "nchar": return SADbType.NChar;
                case "ntext": return SADbType.NText;
                case "numeric": return SADbType.Decimal;
                case "nvarchar": return SADbType.NVarChar;
                case "real": return SADbType.Real;
                case "smalldatetime": return SADbType.SmallDateTime;
                case "smallint": return SADbType.SmallInt;
                case "smallmoney": return SADbType.SmallMoney;
                case "text": return SADbType.Text;
                case "time": return SADbType.Time;
                case "timestamp": return SADbType.TimeStamp;
                case "tinyint": return SADbType.TinyInt;
                case "uniqueidentifier": return SADbType.UniqueIdentifier;
                case "varbinary": return SADbType.VarBinary;
                case "varchar": return SADbType.VarChar;
                case "xml": return SADbType.Xml;

                default: 
                    return SADbType.VarChar;
            }
        }

        static public SAParameter CloneParameter(SAParameter p)
        {
            ICloneable param = p as ICloneable;
            return param.Clone() as SAParameter;
        }
        
        static private Dictionary<Guid, Dictionary<string, SAParameter>> parameterCache
            = new Dictionary<Guid, Dictionary<string, SAParameter>>();
    }
}
