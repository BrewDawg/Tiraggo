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
using System.Data.OleDb;

using Tiraggo.Interfaces;

namespace Tiraggo.MSAccessProvider
{
    class Cache
    {
        static public Dictionary<string, OleDbParameter> GetParameters(tgDataRequest request)
        {
            return GetParameters(request.DataID, request.ProviderMetadata, request.Columns);
        }

        static public Dictionary<string, OleDbParameter> GetParameters(Guid dataID,
            tgProviderSpecificMetadata providerMetadata, tgColumnMetadataCollection columns)
        {
            lock (parameterCache)
            {
                if (!parameterCache.ContainsKey(dataID))
                {
                    // The Parameters for this Table haven't been cached yet, this is a one time operation
                    Dictionary<string, OleDbParameter> types = new Dictionary<string, OleDbParameter>();

                    OleDbParameter param1;
                    foreach (tgColumnMetadata col in columns)
                    {
                        tgTypeMap typeMap = providerMetadata.GetTypeMap(col.PropertyName);
                        if (typeMap != null)
                        {
                            string nativeType = typeMap.NativeType;
                            OleDbType dbType = Cache.NativeTypeToDbType(nativeType);

                            param1 = new OleDbParameter(Delimiters.Param + col.PropertyName, dbType, 0, col.Name);
                            param1.SourceColumn = col.Name;

                            switch (dbType)
                            {
                                case OleDbType.Currency:
                                case OleDbType.Decimal:
                                case OleDbType.Double:
                                case OleDbType.Integer:
                                case OleDbType.Numeric:
                                case OleDbType.Single:
                                case OleDbType.UnsignedTinyInt:

                                    param1.Size = (int)col.CharacterMaxLength;
                                    param1.Precision = (byte)col.NumericPrecision;
                                    param1.Scale = (byte)col.NumericScale;
                                    break;

                                case OleDbType.VarWChar:
                                    param1.Size = (int)col.CharacterMaxLength;
                                    break;

                                case OleDbType.Date:

                                    param1.Precision = 23;
                                    param1.Scale = 3;
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

        static private OleDbType NativeTypeToDbType(string nativeType)
        {
            switch(nativeType)
            {
                case "Byte": return OleDbType.UnsignedTinyInt;
                case "Currency": return OleDbType.Currency;
                case "DateTime": return OleDbType.Date;
                case "Decimal": return OleDbType.Decimal;
                case "Double": return OleDbType.Double;
                case "Hyperlink": return OleDbType.LongVarWChar;
                case "Integer": return OleDbType.Integer;
                case "Long": return OleDbType.Numeric;
                case "Memo": return OleDbType.LongVarWChar;
                case "OLE Object": return OleDbType.LongVarBinary;
                case "Replication ID": return OleDbType.Guid;
                case "Single": return OleDbType.Single;
                case "Text": return OleDbType.VarWChar;
                case "Yes/No": return OleDbType.Boolean;

                default:
                    return OleDbType.VarWChar;
            }
        }

        static public OleDbParameter CloneParameter(OleDbParameter p)
        {
            ICloneable param = p as ICloneable;
            return param.Clone() as OleDbParameter;
        }
        
        static private Dictionary<Guid, Dictionary<string, OleDbParameter>> parameterCache
            = new Dictionary<Guid, Dictionary<string, OleDbParameter>>();
    }
}
