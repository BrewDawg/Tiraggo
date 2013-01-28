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
using System.Data.OracleClient;

using Tiraggo.Interfaces;

namespace Tiraggo.OracleClientProvider
{
    class Cache
    {
        static public Dictionary<string, OracleParameter> GetParameters(tgDataRequest request)
        {
            return GetParameters(request.DataID, request.ProviderMetadata, request.Columns);
        }

        static public Dictionary<string, OracleParameter> GetParameters(Guid dataID,
            tgProviderSpecificMetadata providerMetadata, tgColumnMetadataCollection columns)
        {
            lock (parameterCache)
            {
                if (!parameterCache.ContainsKey(dataID))
                {
                    // The Parameters for this Table haven't been cached yet, this is a one time operation
                    Dictionary<string, OracleParameter> types = new Dictionary<string, OracleParameter>();

                    OracleParameter param1;
                    foreach (tgColumnMetadata col in columns)
                    {
                        tgTypeMap typeMap = providerMetadata.GetTypeMap(col.PropertyName);
                        if (typeMap != null)
                        {
                            string nativeType = typeMap.NativeType;
                            OracleType dbType = Cache.NativeTypeToDbType(nativeType);

                            param1 = new OracleParameter(Delimiters.Param + col.PropertyName, dbType, 0, col.Name);
                            param1.SourceColumn = col.Name;

                            switch (dbType)
                            {
                                case OracleType.Number:

                                    //param1.Size = (int)col.CharacterMaxLength;
                                    //param1.Precision = (byte)col.NumericPrecision;
                                    //param1.Scale = (byte)col.NumericScale;
                                    break;

                                case OracleType.LongVarChar:
                                case OracleType.Char:
                                case OracleType.NChar:
                                case OracleType.VarChar:
                                case OracleType.NVarChar:

                                    param1.Size = (int)col.CharacterMaxLength;
                                    break;

                            }

                            //    case SqlDbType.DateTime:

                            //        param1.Precision = 23;
                            //        param1.Scale = 3;
                            //        break;

                            //    case SqlDbType.SmallDateTime:

                            //        param1.Precision = 16;
                            //        break;

                            //}
                            types[col.Name] = param1;
                        }
                    }

                    parameterCache[dataID] = types;
                }
            }

            return parameterCache[dataID];
        }

        static private OracleType NativeTypeToDbType(string nativeType)
        {
            switch (nativeType)
            {
                case "BFILE": return OracleType.BFile;
                case "BLOB": return OracleType.Blob;
                case "CHAR": return OracleType.Char;
                case "CLOB": return OracleType.Clob;
                case "CURSOR": return OracleType.Cursor;
                case "DATE": return OracleType.DateTime;
                case "FLOAT": return OracleType.Float;
                case "INTERVAL DAY TO SECOND": return OracleType.IntervalDayToSecond;
                case "INTERVAL YEAR TO MONTH": return OracleType.IntervalYearToMonth;
                case "LONGRAW": return OracleType.LongRaw;
                case "LONG": return OracleType.LongVarChar;
                case "NCHAR": return OracleType.NChar;
                case "NCLOB": return OracleType.NClob;
                case "NUMBER": return OracleType.Number;
                case "NVARCHAR2": return OracleType.NVarChar;
                case "RAW": return OracleType.Raw;
                case "ROWID": return OracleType.RowId;
                case "TIMESTAMP": return OracleType.Timestamp;
                case "TIMESTAMP WITH TIME ZONE": return OracleType.TimestampLocal;
                case "TIMESTAMP WITH LOCAL TIME ZONE": return OracleType.TimestampWithTZ;
                case "VARCHAR2": return OracleType.VarChar;

                default: return OracleType.VarChar;
                
            }
        }

        static public OracleParameter CloneParameter(OracleParameter p)
        {
            ICloneable param = p as ICloneable;
            return param.Clone() as OracleParameter;
        }

        static private Dictionary<Guid, Dictionary<string, OracleParameter>> parameterCache
            = new Dictionary<Guid, Dictionary<string, OracleParameter>>();
    }
}
