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

using Tiraggo.Interfaces;

namespace Tiraggo.LoaderMT
{
    public class tgDataProviderFactory : IDataProviderFactory
    {
        /// <summary>
        /// Called by the tgProviderFactory to get the proper data provider to carry
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
                case "Tiraggo.SqlClientProvider":

                    try
                    {
                        return LoadSqlClientProvider(providerClass);
                    }
                    catch
                    {
                        throw new Exception("Unable to Find " + providerName + ".dll");
                    }

                case "Tiraggo.SqlServerCeProvider":

                    try
                    {
                        return LoadSqlServerCeProvider(providerClass);
                    }
                    catch
                    {
                        throw new Exception("Unable to Find " + providerName + ".dll");
                    }

                case "Tiraggo.SqlServerCe4Provider":

                    try
                    {
                        return LoadSqlServerCe4Provider(providerClass);
                    }
                    catch
                    {
                        throw new Exception("Unable to Find " + providerName + ".dll");
                    }

                case "Tiraggo.OracleClientProvider":

                    try
                    {
                        return LoadOracleClientProvider(providerClass);
                    }
                    catch
                    {
                        throw new Exception("Unable to Find " + providerName + ".dll");
                    }

                case "Tiraggo.MSAccessProvider":

                    try
                    {
                        return LoadMSAccessProvider(providerClass);
                    }
                    catch
                    {
                        throw new Exception("Unable to Find " + providerName + ".dll");
                    }

                case "Tiraggo.MySqlClientProvider":

                    try
                    {
                        return LoadMySqlClientProvider(providerClass);
                    }
                    catch
                    {
                        throw new Exception("Unable to Find " + providerName + ".dll");
                    }

                case "Tiraggo.VistaDB4Provider":

                    try
                    {
                        return LoadVistaDB4Provider(providerClass);
                    }
                    catch
                    {
                        throw new Exception("Unable to Find " + providerName + ".dll");
                    }

                case "Tiraggo.Npgsql2Provider":

                    try
                    {
                        return LoadNpgsql2Provider(providerClass);
                    }
                    catch
                    {
                        throw new Exception("Unable to Find " + providerName + ".dll");
                    }

                case "Tiraggo.SybaseSqlAnywhereProvider":

                    try
                    {
                        return LoadSybaseSQLAnywhereProvider(providerClass);
                    }
                    catch
                    {
                        throw new Exception("Unable to Find " + providerName + ".dll");
                    }

                case "Tiraggo.SQLiteProvider":

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

        private IDataProvider LoadSqlClientProvider(string providerClass)
        {
             if(sqlClientProvider == null)
                sqlClientProvider = new Tiraggo.SqlClientProvider.DataProvider();

            return sqlClientProvider;
        }

        private IDataProvider LoadSqlServerCeProvider(string providerClass)
        {
            if (sqlCeDesktopProvider == null)
                sqlCeDesktopProvider = new Tiraggo.SqlServerCeProvider.DataProvider();

            return sqlCeDesktopProvider;
        }

        private IDataProvider LoadSqlServerCe4Provider(string providerClass)
        {
            if (sqlCe4DesktopProvider == null)
                sqlCe4DesktopProvider = new Tiraggo.SqlServerCe4Provider.DataProvider();

            return sqlCe4DesktopProvider;
        }

        private IDataProvider LoadOracleClientProvider(string providerClass)
        {
            if (oracleClientProvider == null)
                oracleClientProvider = new Tiraggo.OracleClientProvider.DataProvider();

            return oracleClientProvider;
        }

        private IDataProvider LoadMSAccessProvider(string providerClass)
        {
            if (msAccessProvider == null)
                msAccessProvider = new Tiraggo.MSAccessProvider.DataProvider();

            return msAccessProvider;
        }

        private IDataProvider LoadMySqlClientProvider(string providerClass)
        {
            if (mySqlClientProvider == null)
                mySqlClientProvider = new Tiraggo.MySqlClientProvider.DataProvider();

            return mySqlClientProvider;
        }

        private IDataProvider LoadVistaDB4Provider(string providerClass)
        {
            if (vistaDB4Provider == null)
                vistaDB4Provider = new Tiraggo.VistaDB4Provider.DataProvider();

            return vistaDB4Provider;
        }

        private IDataProvider LoadNpgsql2Provider(string providerClass)
        {
            if (npgsql2Provider == null)
                npgsql2Provider = new Tiraggo.Npgsql2Provider.DataProvider();

            return npgsql2Provider;
        }

        private IDataProvider LoadSybaseSQLAnywhereProvider(string providerClass)
        {
            if (sybaseProvider == null)
                sybaseProvider = new Tiraggo.SybaseSqlAnywhereProvider.DataProvider();

            return sybaseProvider;
        }

        private IDataProvider LoadSQLiteProvider(string providerClass)
        {
            if (sqliteProvider == null)
                sqliteProvider = new Tiraggo.SQLiteProvider.DataProvider();

            return sqliteProvider;
        }

        private IDataProvider sqlClientProvider;
        private IDataProvider sqlCeDesktopProvider;
        private IDataProvider sqlCe4DesktopProvider;
        private IDataProvider msAccessProvider;
        private IDataProvider oracleClientProvider;
        private IDataProvider mySqlClientProvider;
        private IDataProvider vistaDB4Provider;
        private IDataProvider npgsql2Provider;
        private IDataProvider sybaseProvider;
        private IDataProvider sqliteProvider;
    }
}