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

using VistaDB;
using VistaDB.Provider;

namespace Tiraggo.VistaDB4Provider
{
    class Cache
    {
        static public Dictionary<string, VistaDBParameter> GetParameters(esDataRequest request)
        {
            return GetParameters(request.DataID, request.ProviderMetadata, request.Columns);
        }

        static public Dictionary<string, VistaDBParameter> GetParameters(Guid dataID,
            esProviderSpecificMetadata providerMetadata, esColumnMetadataCollection columns)
        {
            lock (parameterCache)
            {
                if (!parameterCache.ContainsKey(dataID))
                {
                    // The Parameters for this Table haven't been cached yet, this is a one time operation
                    Dictionary<string, VistaDBParameter> types = new Dictionary<string, VistaDBParameter>();

                    VistaDBParameter param1;
                    foreach (esColumnMetadata col in columns)
                    {
                        esTypeMap typeMap = providerMetadata.GetTypeMap(col.PropertyName);
                        if (typeMap != null)
                        {
                            string nativeType = typeMap.NativeType;
                            VistaDBType dbType = Cache.NativeTypeToDbType(nativeType);

                            param1 = new VistaDBParameter(Delimiters.Param + col.PropertyName, dbType, 0, col.Name);
                            param1.SourceColumn = col.Name;

                            switch (dbType)
                            {
                                case VistaDBType.BigInt:
                                case VistaDBType.Int:
                                case VistaDBType.SmallInt:
                                case VistaDBType.Decimal:
                                case VistaDBType.Float:
                                case VistaDBType.Money:
                                case VistaDBType.SmallMoney:

                                    param1.Size = (int)col.CharacterMaxLength;
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

        static private VistaDBType NativeTypeToDbType(string nativeType)
        {
            switch(nativeType)
            {
                case "BigInt": return VistaDBType.BigInt;
                case "Bit": return VistaDBType.Bit;
                case "Char": return VistaDBType.Char;
                case "DateTime": return VistaDBType.DateTime;
                case "Decimal": return VistaDBType.Decimal;
                case "Float": return VistaDBType.Float;
                case "Image": return VistaDBType.Image;
                case "Int": return VistaDBType.Int;
                case "Money": return VistaDBType.Money;
                case "NChar": return VistaDBType.NChar;
                case "NText": return VistaDBType.NText;
                case "NVarChar": return VistaDBType.NVarChar;
                case "Real": return VistaDBType.Real;
                case "SmallDateTime": return VistaDBType.SmallDateTime;
                case "SmallInt": return VistaDBType.SmallInt;
                case "SmallMoney": return VistaDBType.SmallMoney;
                case "Text": return VistaDBType.Text;
                case "Timestamp": return VistaDBType.Timestamp;
                case "TinyInt": return VistaDBType.TinyInt;
                case "UniqueIdentifier": return VistaDBType.UniqueIdentifier;
                case "VarBinary": return VistaDBType.VarBinary;
                case "VarChar": return VistaDBType.VarChar;

                default: 
                    return VistaDBType.Unknown;
            }
        }

        static public VistaDBParameter CloneParameter(VistaDBParameter p)
        {
            ICloneable param = p as ICloneable;
            return param.Clone() as VistaDBParameter;
        }
        
        static private Dictionary<Guid, Dictionary<string, VistaDBParameter>> parameterCache
            = new Dictionary<Guid, Dictionary<string, VistaDBParameter>>();
    }
}
