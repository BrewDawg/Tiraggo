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

using EntitySpaces.Interfaces;

namespace EntitySpaces.Loader
{
    public class esDataProviderFactory : IDataProviderFactory
    {
        /// <summary>
        /// Called by the esProviderFactory to get the proper data provider to carry
        /// out a particular request agains the database
        /// </summary>
        /// <param name="providerName">This is the "provider" element from an EntitySpaces connection entry</param>
        /// <param name="providerClass">This is the "providerClass" element from an EntitySpaces connection entry</param>
        /// <returns></returns>
        public IDataProvider GetDataProvider(string providerName, string providerClass)
        {
            IDataProvider provider = null;

            // This may seem like a funny way to write this routine however, by calling to these
            // sub functions this LoaderMT can run without the actual unused providers being present
            // even though we are bound to them. This is because the assemblies are loaded when they
            // are first accessed. And this first access occurs when a method is called that uses 
            // code from a given assembly. Therefore, these sub functions such as LoadSqlClientProvider()
            // make sure our GetDataProvider doesn't actually itself "new" any of the providers.
            switch (providerName)
            {
                case "EntitySpaces.SQLiteProvider":

                    try
                    {
                        return this.LoadSQLiteProvider(providerClass);
                    }
                    catch
                    {
                        throw new Exception("Unable to Find " + providerName + ".dll");
                    }

            }

            return provider;
        }


        private IDataProvider LoadSQLiteProvider(string providerClass)
        {
            if (sqliteProvider == null)
                sqliteProvider = new EntitySpaces.SQLiteProvider.DataProvider();

            return sqliteProvider;
        }

        private IDataProvider sqliteProvider;
    }
}