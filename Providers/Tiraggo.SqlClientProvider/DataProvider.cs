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
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

using Tiraggo.DynamicQuery;
using Tiraggo.Interfaces;


namespace Tiraggo.SqlClientProvider
{
    public class DataProvider : IDataProvider
    {
        public DataProvider()
        {
        }

        #region esTraceArguments

        private sealed class esTraceArguments : Tiraggo.Interfaces.ITraceArguments, IDisposable
        {
            static private long packetOrder = 0;

            private sealed class esTraceParameter : ITraceParameter
            {
                public string Name { get; set; }

                public string Direction { get; set; }

                public string ParamType { get; set; }

                public string BeforeValue { get; set; }

                public string AfterValue { get; set; }
            }

            public esTraceArguments()
            {
            }

            public esTraceArguments(tgDataRequest request, IDbCommand cmd, string action, string callStack)
            {
                PacketOrder = Interlocked.Increment(ref esTraceArguments.packetOrder);

                this.command = cmd;

                TraceChannel = DataProvider.sTraceChannel;
                Syntax = "MSSQL";
                Request = request;
                ThreadId = Thread.CurrentThread.ManagedThreadId;
                Action = action;
                CallStack = callStack;
                SqlCommand = cmd;
                ApplicationName = System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                IDataParameterCollection parameters = cmd.Parameters;

                if (parameters.Count > 0)
                {
                    Parameters = new List<ITraceParameter>(parameters.Count);

                    for (int i = 0; i < parameters.Count; i++)
                    {
                        SqlParameter param = parameters[i] as SqlParameter;

                        esTraceParameter p = new esTraceParameter()
                        {
                            Name = param.ParameterName,
                            Direction = param.Direction.ToString(),
                            ParamType = param.SqlDbType.ToString().ToUpper(),
                            BeforeValue = param.Value != null ? Convert.ToString(param.Value) : "null"
                        };

                        this.Parameters.Add(p);
                    }
                }

                stopwatch = Stopwatch.StartNew();
            }

            // Temporary variable
            private IDbCommand command;

            public long PacketOrder { get; set; }

            public string Syntax { get; set; }

            public tgDataRequest Request { get; set; }

            public int ThreadId { get; set; }

            public string Action { get; set; }

            public string CallStack { get; set; }

            public IDbCommand SqlCommand { get; set; }

            public string ApplicationName { get; set; }

            public string TraceChannel { get; set; }

            public long Duration { get; set; }

            public long Ticks { get; set; }

            public string Exception { get; set; }

            public List<ITraceParameter> Parameters { get; set; }

            private Stopwatch stopwatch;

            void IDisposable.Dispose()
            {
                stopwatch.Stop();
                Duration = stopwatch.ElapsedMilliseconds;
                Ticks = stopwatch.ElapsedTicks;

                // Gather Output Parameters
                if (this.Parameters != null && this.Parameters.Count > 0)
                {
                    IDataParameterCollection parameters = command.Parameters;

                    for (int i = 0; i < this.Parameters.Count; i++)
                    {
                        ITraceParameter esParam = this.Parameters[i];
                        IDbDataParameter param = parameters[esParam.Name] as IDbDataParameter;

                        if (param.Direction == ParameterDirection.InputOutput || param.Direction == ParameterDirection.Output)
                        {
                            esParam.AfterValue = param.Value != null ? Convert.ToString(param.Value) : "null";
                        }
                    }
                }

                DataProvider.sTraceHandler(this);
            }
        }

        #endregion esTraceArguments

        #region Profiling Logic

        /// <summary>
        /// The EventHandler used to decouple the profiling code from the core assemblies
        /// </summary>
        event TraceEventHandler IDataProvider.TraceHandler
        {
            add { DataProvider.sTraceHandler += value; }
            remove { DataProvider.sTraceHandler -= value; }
        }

        static private event TraceEventHandler sTraceHandler;

        /// <summary>
        /// Returns true if this Provider is current being profiled
        /// </summary>
        bool IDataProvider.IsTracing
        {
            get
            {
                return sTraceHandler != null ? true : false;
            }
        }

        /// <summary>
        /// Used to set the Channel this provider is to use during Profiling
        /// </summary>
        string IDataProvider.TraceChannel
        {
            get { return DataProvider.sTraceChannel; }
            set { DataProvider.sTraceChannel = value; }
        }

        static private string sTraceChannel = "Channel1";

        #endregion Profiling Logic

        /// <summary>
        /// This method acts as a delegate for tgTransactionScope
        /// </summary>
        /// <returns></returns>
        static private IDbConnection CreateIDbConnectionDelegate()
        {
            return new SqlConnection();
        }

        static private void CleanupCommand(SqlCommand cmd)
        {
            if (cmd != null && cmd.Connection != null)
            {
                if (cmd.Connection.State == ConnectionState.Open)
                {
                    cmd.Connection.Close();
                }
            }
        }

        #region IDataProvider Members

        tgDataResponse IDataProvider.esLoadDataTable(tgDataRequest request)
        {
            tgDataResponse response = new tgDataResponse();

            try
            {
                switch (request.QueryType)
                {
                    case tgQueryType.StoredProcedure:

                        response = LoadDataTableFromStoredProcedure(request);
                        break;

                    case tgQueryType.Text:

                        response = LoadDataTableFromText(request);
                        break;

                    case tgQueryType.DynamicQuery:

                        response = new tgDataResponse();
                        SqlCommand cmd = QueryBuilder.PrepareCommand(request);
                        LoadDataTableFromDynamicQuery(request, response, cmd);
                        break;

                    case tgQueryType.DynamicQueryParseOnly:

                        response = new tgDataResponse();
                        SqlCommand cmd1 = QueryBuilder.PrepareCommand(request);
                        response.LastQuery = cmd1.CommandText;
                        break;

                    case tgQueryType.ManyToMany:

                        response = LoadManyToMany(request);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
            }

            return response;
        }

        tgDataResponse IDataProvider.esSaveDataTable(tgDataRequest request)
        {
            tgDataResponse response = new tgDataResponse();

            try
            {
                if (request.BulkSave)
                {
                    SaveBulkInsert(request);
                }
                else
                {
                    if (request.SqlAccessType == tgSqlAccessType.StoredProcedure)
                    {
                        if (request.CollectionSavePacket != null)
                            SaveStoredProcCollection(request);
                        else
                            SaveStoredProcEntity(request);
                    }
                    else
                    {
                        if (request.EntitySavePacket.CurrentValues == null)
                            SaveDynamicCollection(request);
                        else
                            SaveDynamicEntity(request);
                    }
                }
            }
            catch (SqlException ex)
            {
                tgException es = Shared.CheckForConcurrencyException(ex);
                if (es != null)
                    response.Exception = es;
                else
                    response.Exception = ex;
            }
            catch (DBConcurrencyException dbex)
            {
                response.Exception = new tgConcurrencyException("Error in SqlClientProvider.esSaveDataTable", dbex);
            }

            response.Table = request.Table;
            return response;
        }

        tgDataResponse IDataProvider.ExecuteNonQuery(tgDataRequest request)
        {
            tgDataResponse response = new tgDataResponse();
            SqlCommand cmd = null;

            try
            {
                cmd = new SqlCommand();
                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if (request.Parameters != null) Shared.AddParameters(cmd, request);

                switch (request.QueryType)
                {
                    case tgQueryType.TableDirect:
                        cmd.CommandType = CommandType.TableDirect;
                        cmd.CommandText = request.QueryText;
                        break;

                    case tgQueryType.StoredProcedure:
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = Shared.CreateFullName(request);
                        break;

                    case tgQueryType.Text:
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = request.QueryText;
                        break;
                }

                try
                {
                    tgTransactionScope.Enlist(cmd, request.ConnectionString, CreateIDbConnectionDelegate);

                    #region Profiling

                    if (sTraceHandler != null)
                    {
                        using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "ExecuteNonQuery", System.Environment.StackTrace))
                        {
                            try
                            {
                                response.RowsEffected = cmd.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                esTrace.Exception = ex.Message;
                                throw;
                            }
                        }
                    }
                    else

                    #endregion Profiling

                    {
                        response.RowsEffected = cmd.ExecuteNonQuery();
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(cmd);
                }

                if (request.Parameters != null)
                {
                    Shared.GatherReturnParameters(cmd, request, response);
                }
            }
            catch (Exception ex)
            {
                CleanupCommand(cmd);
                response.Exception = ex;
            }

            return response;
        }

        tgDataResponse IDataProvider.ExecuteReader(tgDataRequest request)
        {
            tgDataResponse response = new tgDataResponse();
            SqlCommand cmd = null;

            try
            {
                cmd = new SqlCommand();
                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if (request.Parameters != null) Shared.AddParameters(cmd, request);

                switch (request.QueryType)
                {
                    case tgQueryType.TableDirect:
                        cmd.CommandType = CommandType.TableDirect;
                        cmd.CommandText = request.QueryText;
                        break;

                    case tgQueryType.StoredProcedure:
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = Shared.CreateFullName(request);
                        break;

                    case tgQueryType.Text:
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = request.QueryText;
                        break;

                    case tgQueryType.DynamicQuery:
                        cmd = QueryBuilder.PrepareCommand(request);
                        break;
                }

                cmd.Connection = new SqlConnection(request.ConnectionString);
                cmd.Connection.Open();

                #region Profiling

                if (sTraceHandler != null)
                {
                    using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "ExecuteReader", System.Environment.StackTrace))
                    {
                        try
                        {
                            response.DataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                        }
                        catch (Exception ex)
                        {
                            esTrace.Exception = ex.Message;
                            throw;
                        }
                    }
                }
                else

                #endregion Profiling

                {
                    response.DataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                }
            }
            catch (Exception ex)
            {
                CleanupCommand(cmd);
                response.Exception = ex;
            }

            return response;
        }

        tgDataResponse IDataProvider.ExecuteScalar(tgDataRequest request)
        {
            tgDataResponse response = new tgDataResponse();
            SqlCommand cmd = null;

            try
            {
                cmd = new SqlCommand();
                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if (request.Parameters != null) Shared.AddParameters(cmd, request);

                switch (request.QueryType)
                {
                    case tgQueryType.TableDirect:
                        cmd.CommandType = CommandType.TableDirect;
                        cmd.CommandText = request.QueryText;
                        break;

                    case tgQueryType.StoredProcedure:
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = Shared.CreateFullName(request);
                        break;

                    case tgQueryType.Text:
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = request.QueryText;
                        break;

                    case tgQueryType.DynamicQuery:
                        cmd = QueryBuilder.PrepareCommand(request);
                        break;
                }

                try
                {
                    tgTransactionScope.Enlist(cmd, request.ConnectionString, CreateIDbConnectionDelegate);

                    #region Profiling

                    if (sTraceHandler != null)
                    {
                        using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "ExecuteScalar", System.Environment.StackTrace))
                        {
                            try
                            {
                                response.Scalar = cmd.ExecuteScalar();
                            }
                            catch (Exception ex)
                            {
                                esTrace.Exception = ex.Message;
                                throw;
                            }
                        }
                    }
                    else

                    #endregion Profiling

                    {
                        response.Scalar = cmd.ExecuteScalar();
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(cmd);
                }

                if (request.Parameters != null)
                {
                    Shared.GatherReturnParameters(cmd, request, response);
                }
            }
            catch (Exception ex)
            {
                CleanupCommand(cmd);
                response.Exception = ex;
            }

            return response;
        }

        tgDataResponse IDataProvider.FillDataSet(tgDataRequest request)
        {
            tgDataResponse response = new tgDataResponse();

            try
            {
                switch (request.QueryType)
                {
                    case tgQueryType.StoredProcedure:

                        response = LoadDataSetFromStoredProcedure(request);
                        break;

                    case tgQueryType.Text:

                        response = LoadDataSetFromText(request);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
            }

            return response;
        }

        tgDataResponse IDataProvider.FillDataTable(tgDataRequest request)
        {
            tgDataResponse response = new tgDataResponse();

            try
            {
                switch (request.QueryType)
                {
                    case tgQueryType.StoredProcedure:

                        response = LoadDataTableFromStoredProcedure(request);
                        break;

                    case tgQueryType.Text:

                        response = LoadDataTableFromText(request);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
            }

            return response;
        }

        #endregion IDataProvider Members

        static private tgDataResponse LoadDataSetFromStoredProcedure(tgDataRequest request)
        {
            tgDataResponse response = new tgDataResponse();
            SqlCommand cmd = null;

            try
            {
                DataSet dataSet = new DataSet();

                cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = Shared.CreateFullName(request);

                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if (request.Parameters != null) Shared.AddParameters(cmd, request);

                SqlDataAdapter da = new SqlDataAdapter();
                da.SelectCommand = cmd;

                try
                {
                    tgTransactionScope.Enlist(da.SelectCommand, request.ConnectionString, CreateIDbConnectionDelegate);

                    #region Profiling

                    if (sTraceHandler != null)
                    {
                        using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "LoadFromStoredProcedure", System.Environment.StackTrace))
                        {
                            try
                            {
                                da.Fill(dataSet);
                            }
                            catch (Exception ex)
                            {
                                esTrace.Exception = ex.Message;
                                throw;
                            }
                        }
                    }
                    else

                    #endregion Profiling

                    {
                        da.Fill(dataSet);
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(da.SelectCommand);
                }

                response.DataSet = dataSet;

                if (request.Parameters != null)
                {
                    Shared.GatherReturnParameters(cmd, request, response);
                }
            }
            catch
            {
                CleanupCommand(cmd);
                throw;
            }
            finally
            {
            }

            return response;
        }

        static private tgDataResponse LoadDataSetFromText(tgDataRequest request)
        {
            tgDataResponse response = new tgDataResponse();
            SqlCommand cmd = null;

            try
            {
                DataSet dataSet = new DataSet();

                cmd = new SqlCommand();
                cmd.CommandType = CommandType.Text;
                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if (request.Parameters != null) Shared.AddParameters(cmd, request);

                SqlDataAdapter da = new SqlDataAdapter();
                cmd.CommandText = request.QueryText;
                da.SelectCommand = cmd;

                try
                {
                    tgTransactionScope.Enlist(da.SelectCommand, request.ConnectionString, CreateIDbConnectionDelegate);

                    #region Profiling

                    if (sTraceHandler != null)
                    {
                        using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "LoadDataSetFromText", System.Environment.StackTrace))
                        {
                            try
                            {
                                da.Fill(dataSet);
                            }
                            catch (Exception ex)
                            {
                                esTrace.Exception = ex.Message;
                                throw;
                            }
                        }
                    }
                    else

                    #endregion Profiling

                    {
                        da.Fill(dataSet);
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(da.SelectCommand);
                }

                response.DataSet = dataSet;

                if (request.Parameters != null)
                {
                    Shared.GatherReturnParameters(cmd, request, response);
                }
            }
            catch
            {
                CleanupCommand(cmd);
                throw;
            }
            finally
            {
            }

            return response;
        }

        static private tgDataResponse LoadDataTableFromStoredProcedure(tgDataRequest request)
        {
            tgDataResponse response = new tgDataResponse();
            SqlCommand cmd = null;

            try
            {
                DataTable dataTable = new DataTable(request.ProviderMetadata.Destination);

                cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = Shared.CreateFullName(request);
                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if (request.Parameters != null) Shared.AddParameters(cmd, request);

                SqlDataAdapter da = new SqlDataAdapter();
                da.SelectCommand = cmd;

                try
                {
                    tgTransactionScope.Enlist(da.SelectCommand, request.ConnectionString, CreateIDbConnectionDelegate);

                    #region Profiling

                    if (sTraceHandler != null)
                    {
                        using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "LoadFromStoredProcedure", System.Environment.StackTrace))
                        {
                            try
                            {
                                da.Fill(dataTable);
                            }
                            catch (Exception ex)
                            {
                                esTrace.Exception = ex.Message;
                                throw;
                            }
                        }
                    }
                    else

                    #endregion Profiling

                    {
                        da.Fill(dataTable);
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(da.SelectCommand);
                }

                response.Table = dataTable;

                if (request.Parameters != null)
                {
                    Shared.GatherReturnParameters(cmd, request, response);
                }
            }
            catch
            {
                CleanupCommand(cmd);
                throw;
            }
            finally
            {
            }

            return response;
        }

        static private tgDataResponse LoadDataTableFromText(tgDataRequest request)
        {
            tgDataResponse response = new tgDataResponse();
            SqlCommand cmd = null;

            try
            {
                DataTable dataTable = new DataTable(request.ProviderMetadata.Destination);

                cmd = new SqlCommand();
                cmd.CommandType = CommandType.Text;
                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if (request.Parameters != null) Shared.AddParameters(cmd, request);

                SqlDataAdapter da = new SqlDataAdapter();
                cmd.CommandText = request.QueryText;
                da.SelectCommand = cmd;

                try
                {
                    tgTransactionScope.Enlist(da.SelectCommand, request.ConnectionString, CreateIDbConnectionDelegate);

                    #region Profiling

                    if (sTraceHandler != null)
                    {
                        using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "LoadFromText", System.Environment.StackTrace))
                        {
                            try
                            {
                                da.Fill(dataTable);
                            }
                            catch (Exception ex)
                            {
                                esTrace.Exception = ex.Message;
                                throw;
                            }
                        }
                    }
                    else

                    #endregion Profiling

                    {
                        da.Fill(dataTable);
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(da.SelectCommand);
                }

                response.Table = dataTable;

                if (request.Parameters != null)
                {
                    Shared.GatherReturnParameters(cmd, request, response);
                }
            }
            catch
            {
                CleanupCommand(cmd);
                throw;
            }
            finally
            {
            }

            return response;
        }

        static private tgDataResponse LoadManyToMany(tgDataRequest request)
        {
            tgDataResponse response = new tgDataResponse();
            SqlCommand cmd = null;

            try
            {
                DataTable dataTable = new DataTable(request.ProviderMetadata.Destination);

                cmd = new SqlCommand();
                cmd.CommandType = CommandType.Text;
                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;

                string mmQuery = request.QueryText;

                string[] sections = mmQuery.Split('|');
                string[] tables = sections[0].Split(',');
                string[] columns = sections[1].Split(',');

                string prefix = String.Empty;

                if (request.Catalog != null || request.ProviderMetadata.Catalog != null)
                {
                    prefix += Delimiters.TableOpen;
                    prefix += request.Catalog != null ? request.Catalog : request.ProviderMetadata.Catalog;
                    prefix += Delimiters.TableClose + ".";
                }

                if (request.Schema != null || request.ProviderMetadata.Schema != null)
                {
                    prefix += Delimiters.TableOpen;
                    prefix += request.Schema != null ? request.Schema : request.ProviderMetadata.Schema;
                    prefix += Delimiters.TableClose + ".";
                }

                string table0 = prefix + Delimiters.TableOpen + tables[0] + Delimiters.TableClose;
                string table1 = prefix + Delimiters.TableOpen + tables[1] + Delimiters.TableClose;

                string sql = "SELECT * FROM " + table0 + " JOIN " + table1 + " ON " + table0 + ".[" + columns[0] + "] = ";
                sql += table1 + ".[" + columns[1] + "] WHERE " + table1 + ".[" + sections[2] + "] = @";

                if (request.Parameters != null)
                {
                    foreach (esParameter esParam in request.Parameters)
                    {
                        sql += esParam.Name;
                    }

                    Shared.AddParameters(cmd, request);
                }

                SqlDataAdapter da = new SqlDataAdapter();
                cmd.CommandText = sql;

                da.SelectCommand = cmd;

                try
                {
                    tgTransactionScope.Enlist(da.SelectCommand, request.ConnectionString, CreateIDbConnectionDelegate);

                    #region Profiling

                    if (sTraceHandler != null)
                    {
                        using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "LoadManyToMany", System.Environment.StackTrace))
                        {
                            try
                            {
                                da.Fill(dataTable);
                            }
                            catch (Exception ex)
                            {
                                esTrace.Exception = ex.Message;
                                throw;
                            }
                        }
                    }
                    else

                    #endregion Profiling

                    {
                        da.Fill(dataTable);
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(da.SelectCommand);
                }

                response.Table = dataTable;
            }
            catch
            {
                CleanupCommand(cmd);
                throw;
            }
            finally
            {
            }

            return response;
        }

        // This is used only to execute the Dynamic Query API
        static private void LoadDataTableFromDynamicQuery(tgDataRequest request, tgDataResponse response, SqlCommand cmd)
        {
            try
            {
                response.LastQuery = cmd.CommandText;

                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;

                DataTable dataTable = new DataTable(request.ProviderMetadata.Destination);

                SqlDataAdapter da = new SqlDataAdapter();
                da.SelectCommand = cmd;

                try
                {
                    tgTransactionScope.Enlist(da.SelectCommand, request.ConnectionString, CreateIDbConnectionDelegate);

                    #region Profiling

                    if (sTraceHandler != null)
                    {
                        using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "LoadFromDynamicQuery", System.Environment.StackTrace))
                        {
                            try
                            {
                                da.Fill(dataTable);
                            }
                            catch (Exception ex)
                            {
                                esTrace.Exception = ex.Message;
                                throw;
                            }
                        }
                    }
                    else

                    #endregion Profiling

                    {
                        da.Fill(dataTable);
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(da.SelectCommand);
                }

                response.Table = dataTable;

                // Special code to remove the ESRN column if paging is going on
                tgDynamicQuerySerializable.DynamicQueryProps es = request.DynamicQuery.tg;
                if (es.PageNumber.HasValue && es.PageSize.HasValue)
                {
                    DataColumnCollection cols = response.Table.Columns;

                    if (cols.Contains("ESRN"))
                    {
                        cols.Remove("ESRN");
                    }
                }
            }
            catch (Exception)
            {
                CleanupCommand(cmd);
                throw;
            }
            finally
            {
            }
        }

        static private DataTable SaveStoredProcCollection(tgDataRequest request)
        {
            if (request.CollectionSavePacket == null) return null;

            SqlCommand cmdInsert = null;
            SqlCommand cmdUpdate = null;
            SqlCommand cmdDelete = null;

            try
            {
                using (tgTransactionScope scope = new tgTransactionScope())
                {
                    SqlCommand cmd = null;
                    bool exception = false;

                    foreach (tgEntitySavePacket packet in request.CollectionSavePacket)
                    {
                        cmd = null;
                        exception = false;

                        #region Setup Commands

                        switch (packet.RowState)
                        {
                            case tgDataRowState.Added:
                                if (cmdInsert == null)
                                {
                                    cmdInsert = Shared.BuildStoredProcInsertCommand(request, packet);
                                    tgTransactionScope.Enlist(cmdInsert, request.ConnectionString, CreateIDbConnectionDelegate);
                                }
                                cmd = cmdInsert;
                                break;
                            case tgDataRowState.Modified:
                                if (cmdUpdate == null)
                                {
                                    cmdUpdate = Shared.BuildStoredProcUpdateCommand(request, packet);
                                    tgTransactionScope.Enlist(cmdUpdate, request.ConnectionString, CreateIDbConnectionDelegate);
                                }
                                cmd = cmdUpdate;
                                break;
                            case tgDataRowState.Deleted:
                                if (cmdDelete == null)
                                {
                                    cmdDelete = Shared.BuildStoredProcDeleteCommand(request, packet);
                                    tgTransactionScope.Enlist(cmdDelete, request.ConnectionString, CreateIDbConnectionDelegate);
                                }
                                cmd = cmdDelete;
                                break;

                            case tgDataRowState.Unchanged:
                                continue;
                        }

                        #endregion Setup Commands

                        #region Preprocess Parameters

                        if (cmd.Parameters != null)
                        {
                            foreach (SqlParameter param in cmd.Parameters)
                            {
                                if (param.Direction == ParameterDirection.Output)
                                {
                                    param.Value = null;
                                }
                                else
                                {
                                    if (packet.CurrentValues.ContainsKey(param.SourceColumn))
                                    {
                                        param.Value = packet.CurrentValues[param.SourceColumn];
                                    }
                                    else
                                    {
                                        param.Value = null;
                                    }
                                }
                            }
                        }

                        #endregion Preprocess Parameters

                        #region Execute Command

                        try
                        {
                            int count;

                            #region Profiling

                            if (sTraceHandler != null)
                            {
                                using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "SaveCollectionStoredProcedure", System.Environment.StackTrace))
                                {
                                    try
                                    {
                                        count = cmd.ExecuteNonQuery();
                                    }
                                    catch (Exception ex)
                                    {
                                        esTrace.Exception = ex.Message;
                                        throw;
                                    }
                                }
                            }
                            else

                            #endregion Profiling

                            {
                                count = cmd.ExecuteNonQuery();
                            }

                            if (count < 1)
                            {
                                throw new tgConcurrencyException("Update failed to update any records @ " + cmd.CommandText);
                            }
                        }
                        catch (Exception ex)
                        {
                            exception = true;
                            request.FireOnError(packet, ex.Message);
                            if (!request.ContinueUpdateOnError)
                            {
                                throw;
                            }
                        }

                        #endregion Execute Command

                        #region Postprocess Parameters

                        if (!exception && packet.RowState != tgDataRowState.Deleted && cmd.Parameters != null)
                        {
                            foreach (SqlParameter param in cmd.Parameters)
                            {
                                switch (param.Direction)
                                {
                                    case ParameterDirection.Output:
                                    case ParameterDirection.InputOutput:

                                        packet.CurrentValues[param.SourceColumn] = param.Value;
                                        break;
                                }
                            }
                        }

                        #endregion Postprocess Parameters
                    }

                    scope.Complete();
                }
            }
            finally
            {
                if (cmdInsert != null) tgTransactionScope.DeEnlist(cmdInsert);
                if (cmdUpdate != null) tgTransactionScope.DeEnlist(cmdUpdate);
                if (cmdDelete != null) tgTransactionScope.DeEnlist(cmdDelete);
            }

            return null;
        }

        static private DataTable SaveStoredProcEntity(tgDataRequest request)
        {
            SqlCommand cmd = null;

            switch (request.EntitySavePacket.RowState)
            {
                case tgDataRowState.Added:
                    cmd = Shared.BuildStoredProcInsertCommand(request, request.EntitySavePacket);
                    break;

                case tgDataRowState.Modified:
                    cmd = Shared.BuildStoredProcUpdateCommand(request, request.EntitySavePacket);
                    break;

                case tgDataRowState.Deleted:
                    cmd = Shared.BuildStoredProcDeleteCommand(request, request.EntitySavePacket);
                    break;

                case tgDataRowState.Unchanged:
                    return null;
            }

            try
            {
                tgTransactionScope.Enlist(cmd, request.ConnectionString, CreateIDbConnectionDelegate);
                int count = 0;

                #region Profiling

                if (sTraceHandler != null)
                {
                    using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "SaveEntityStoredProcedure", System.Environment.StackTrace))
                    {
                        try
                        {
                            count = cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            esTrace.Exception = ex.Message;
                            throw;
                        }
                    }
                }
                else

                #endregion Profiling

                {
                    count = cmd.ExecuteNonQuery();
                }

                if (count < 1)
                {
                    throw new tgConcurrencyException("Update failed to update any records @ " + cmd.CommandText);
                }
            }
            finally
            {
                tgTransactionScope.DeEnlist(cmd);
                cmd.Dispose();
            }

            if (request.EntitySavePacket.RowState != tgDataRowState.Deleted && cmd.Parameters != null)
            {
                foreach (SqlParameter param in cmd.Parameters)
                {
                    switch (param.Direction)
                    {
                        case ParameterDirection.Output:
                        case ParameterDirection.InputOutput:

                            request.EntitySavePacket.CurrentValues[param.SourceColumn] = param.Value;
                            break;
                    }
                }
            }

            return null;
        }

        static private DataTable SaveDynamicCollection(tgDataRequest request)
        {
            if (request.CollectionSavePacket == null) return null;

            using (tgTransactionScope scope = new tgTransactionScope())
            {
                SqlCommand cmd = null;
                bool exception = false;

                foreach (tgEntitySavePacket packet in request.CollectionSavePacket)
                {
                    exception = false;
                    cmd = null;

                    switch (packet.RowState)
                    {
                        case tgDataRowState.Added:
                            cmd = Shared.BuildDynamicInsertCommand(request, packet);
                            break;

                        case tgDataRowState.Modified:
                            cmd = Shared.BuildDynamicUpdateCommand(request, packet);
                            break;

                        case tgDataRowState.Deleted:
                            cmd = Shared.BuildDynamicDeleteCommand(request, packet);
                            break;

                        case tgDataRowState.Unchanged:
                            continue;
                    }

                    try
                    {
                        tgTransactionScope.Enlist(cmd, request.ConnectionString, CreateIDbConnectionDelegate);
                        int count;

                        #region Profiling

                        if (sTraceHandler != null)
                        {
                            using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "SaveCollectionDynamic", System.Environment.StackTrace))
                            {
                                try
                                {
                                    count = cmd.ExecuteNonQuery(); ;
                                }
                                catch (Exception ex)
                                {
                                    esTrace.Exception = ex.Message;
                                    throw;
                                }
                            }
                        }
                        else

                        #endregion Profiling

                        {
                            count = cmd.ExecuteNonQuery();
                        }

                        if (count < 1)
                        {
                            throw new tgConcurrencyException("Update failed to update any records for Table " + Shared.CreateFullName(request));
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = true;

                        request.FireOnError(packet, ex.Message);

                        if (!request.ContinueUpdateOnError)
                        {
                            throw;
                        }
                    }
                    finally
                    {
                        tgTransactionScope.DeEnlist(cmd);
                        cmd.Dispose();
                    }

                    if (!exception && packet.RowState != tgDataRowState.Deleted && cmd.Parameters != null)
                    {
                        foreach (SqlParameter param in cmd.Parameters)
                        {
                            switch (param.Direction)
                            {
                                case ParameterDirection.Output:
                                case ParameterDirection.InputOutput:

                                    packet.CurrentValues[param.SourceColumn] = param.Value;
                                    break;
                            }
                        }
                    }
                }

                scope.Complete();
            }

            return null;
        }

        static private DataTable SaveDynamicEntity(tgDataRequest request)
        {
            SqlCommand cmd = null;

            switch (request.EntitySavePacket.RowState)
            {
                case tgDataRowState.Added:
                    cmd = Shared.BuildDynamicInsertCommand(request, request.EntitySavePacket);
                    break;

                case tgDataRowState.Modified:
                    cmd = Shared.BuildDynamicUpdateCommand(request, request.EntitySavePacket);
                    break;

                case tgDataRowState.Deleted:
                    cmd = Shared.BuildDynamicDeleteCommand(request, request.EntitySavePacket);
                    break;
            }

            try
            {
                tgTransactionScope.Enlist(cmd, request.ConnectionString, CreateIDbConnectionDelegate);

                int count = 0;

                #region Profiling

                if (sTraceHandler != null)
                {
                    using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "SaveEntityDynamic", System.Environment.StackTrace))
                    {
                        try
                        {
                            count = cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            esTrace.Exception = ex.Message;
                            throw;
                        }
                    }
                }
                else

                #endregion Profiling

                {
                    count = cmd.ExecuteNonQuery();
                }

                if (count < 1)
                {
                    throw new tgConcurrencyException("Update failed to update any records for Table " + Shared.CreateFullName(request));
                }
            }
            finally
            {
                tgTransactionScope.DeEnlist(cmd);
                cmd.Dispose();
            }

            if (request.EntitySavePacket.RowState != tgDataRowState.Deleted && cmd.Parameters != null)
            {
                foreach (SqlParameter param in cmd.Parameters)
                {
                    switch (param.Direction)
                    {
                        case ParameterDirection.Output:
                        case ParameterDirection.InputOutput:

                            request.EntitySavePacket.CurrentValues[param.SourceColumn] = param.Value;
                            break;
                    }
                }
            }

            return null;
        }

        static private DataTable SaveBulkInsert(tgDataRequest request)
        {
            if (request.CollectionSavePacket == null) return null;

            DataTable dataTable = CreateDataTableForBulkInsert(request);

            foreach (tgEntitySavePacket packet in request.CollectionSavePacket)
            {
                if (packet.RowState != tgDataRowState.Added) continue;

                DataRow row = dataTable.NewRow();
                dataTable.Rows.Add(row);

                SetModifiedValues(request, packet, row);
            }

            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default;
            bool first = true;

            if (request.BulkSaveOptions != null)
            {
                foreach (string opt in request.BulkSaveOptions)
                {
                    if (first)
                    {
                        first = false;
                        options = (SqlBulkCopyOptions)Enum.Parse(typeof(SqlBulkCopyOptions), opt);
                    }
                    else
                    {
                        options |= (SqlBulkCopyOptions)Enum.Parse(typeof(SqlBulkCopyOptions), opt);
                    }
                }
            }

            if (first)
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(request.ConnectionString))
                {
                    bulkCopy.DestinationTableName = Shared.CreateFullName(request);
                    bulkCopy.WriteToServer(dataTable);
                }
            }
            else
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(request.ConnectionString, options))
                {
                    bulkCopy.DestinationTableName = Shared.CreateFullName(request);
                    bulkCopy.WriteToServer(dataTable);
                }
            }

            return null;
        }

        static private DataTable CreateDataTableForBulkInsert(tgDataRequest request)
        {
            DataTable dataTable = new DataTable();
            DataColumnCollection dataColumns = dataTable.Columns;
            tgColumnMetadataCollection cols = request.Columns;

            if (request.SelectedColumns == null)
            {
                tgColumnMetadata col;
                for (int i = 0; i < cols.Count; i++)
                {
                    col = cols[i];
                    dataColumns.Add(new DataColumn(col.Name, col.Type));
                }
            }
            else
            {
                foreach (string col in request.SelectedColumns.Keys)
                {
                    dataColumns.Add(new DataColumn(col, cols[col].Type));
                }
            }

            return dataTable;
        }

        private static void SetModifiedValues(tgDataRequest request, tgEntitySavePacket packet, DataRow row)
        {
            foreach (string column in packet.ModifiedColumns)
            {
                if (request.Columns.FindByColumnName(column) != null)
                {
                    row[column] = packet.CurrentValues[column];
                }
            }
        }
    }
}