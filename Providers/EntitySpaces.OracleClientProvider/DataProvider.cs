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
using System.Data.OracleClient;

using Tiraggo.DynamicQuery;
using Tiraggo.Interfaces;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace Tiraggo.OracleClientProvider
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

            public esTraceArguments(esDataRequest request, IDbCommand cmd, string action, string callStack)
            {
                PacketOrder = Interlocked.Increment(ref esTraceArguments.packetOrder);

                this.command = cmd;

                TraceChannel = DataProvider.sTraceChannel;
                Syntax = "ORACLE";
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
                        OracleParameter param = parameters[i] as OracleParameter;

                        esTraceParameter p = new esTraceParameter()
                        {
                            Name = param.ParameterName,
                            Direction = param.Direction.ToString(),
                            ParamType = param.OracleType.ToString().ToUpper(),
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
            public esDataRequest Request { get; set; }
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

        #endregion

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

        #endregion

        /// <summary>
        /// This method acts as a delegate for esTransactionScope
        /// </summary>
        /// <returns></returns>
        static private IDbConnection CreateIDbConnectionDelegate()
        {
            return new OracleConnection();
        }

        static private void CleanupCommand(OracleCommand cmd)
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

        esDataResponse IDataProvider.esLoadDataTable(esDataRequest request)
        {
            esDataResponse response = new esDataResponse();

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

                        response = new esDataResponse();
                        OracleCommand cmd = QueryBuilder.PrepareCommand(request);
                        LoadDataTableFromDynamicQuery(request, response, cmd);
                        break;

                    case tgQueryType.DynamicQueryParseOnly:

                        response = new esDataResponse();
                        OracleCommand cmd1 = QueryBuilder.PrepareCommand(request);
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

        esDataResponse IDataProvider.esSaveDataTable(esDataRequest request)
        {
            esDataResponse response = new esDataResponse();

            try
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
            catch (OracleException ex)
            {
                tgException es = Shared.CheckForConcurrencyException(ex);
                if (es != null)
                    response.Exception = es;
                else
                    response.Exception = ex;
            }
            catch (DBConcurrencyException dbex)
            {
                response.Exception = new tgConcurrencyException("Error in OracleClientProvider.esSaveDataTable", dbex);
            }

            response.Table = request.Table;
            return response;
        }

        esDataResponse IDataProvider.ExecuteNonQuery(esDataRequest request)
        {
            esDataResponse response = new esDataResponse();
            OracleCommand cmd = null;

            try
            {
                cmd = new OracleCommand();
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
                    esTransactionScope.Enlist(cmd, request.ConnectionString, CreateIDbConnectionDelegate);

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
                    #endregion
                    {
                        response.RowsEffected = cmd.ExecuteNonQuery();
                    }
                }
                finally
                {
                    esTransactionScope.DeEnlist(cmd);
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

        esDataResponse IDataProvider.ExecuteReader(esDataRequest request)
        {
            esDataResponse response = new esDataResponse();
            OracleCommand cmd = null;

            try
            {
                cmd = new OracleCommand();
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

                        OracleParameter p = new OracleParameter("outCursor", OracleType.Cursor);
                        p.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(p);
                        break;

                    case tgQueryType.Text:
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = request.QueryText;
                        break;

                    case tgQueryType.DynamicQuery:
                        cmd = QueryBuilder.PrepareCommand(request);
                        break;
                }

                cmd.Connection = new OracleConnection(request.ConnectionString);
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
                #endregion
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

        esDataResponse IDataProvider.ExecuteScalar(esDataRequest request)
        {
            esDataResponse response = new esDataResponse();
            OracleCommand cmd = null;

            try
            {
                cmd = new OracleCommand();
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
                    esTransactionScope.Enlist(cmd, request.ConnectionString, CreateIDbConnectionDelegate);

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
                    #endregion
                    {
                        response.Scalar = cmd.ExecuteScalar();
                    }
                }
                finally
                {
                    esTransactionScope.DeEnlist(cmd);
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

        esDataResponse IDataProvider.FillDataSet(esDataRequest request)
        {
            esDataResponse response = new esDataResponse();

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

        esDataResponse IDataProvider.FillDataTable(esDataRequest request)
        {
            esDataResponse response = new esDataResponse();

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

        #endregion

        static private esDataResponse LoadDataSetFromStoredProcedure(esDataRequest request)
        {
            esDataResponse response = new esDataResponse();
            OracleCommand cmd = null;

            try
            {
                DataSet dataSet = new DataSet();

                cmd = new OracleCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = Shared.CreateFullName(request);

                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if (request.Parameters != null) Shared.AddParameters(cmd, request);

                OracleParameter p = new OracleParameter("outCursor", OracleType.Cursor);
                p.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(p);

                OracleDataAdapter da = new OracleDataAdapter();
                da.SelectCommand = cmd;

                try
                {
                    esTransactionScope.Enlist(da.SelectCommand, request.ConnectionString, CreateIDbConnectionDelegate);

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
                    #endregion
                    {
                        da.Fill(dataSet);
                    }
                }
                finally
                {
                    esTransactionScope.DeEnlist(da.SelectCommand);
                }

                response.DataSet = dataSet;

                if (request.Parameters != null)
                {
                    Shared.GatherReturnParameters(cmd, request, response);
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

            return response;
        }

        static private esDataResponse LoadDataSetFromText(esDataRequest request)
        {
            esDataResponse response = new esDataResponse();
            OracleCommand cmd = null;

            try
            {
                DataSet dataSet = new DataSet();

                cmd = new OracleCommand();
                cmd.CommandType = CommandType.Text;
                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if (request.Parameters != null) Shared.AddParameters(cmd, request);

                OracleDataAdapter da = new OracleDataAdapter();
                cmd.CommandText = request.QueryText;
                da.SelectCommand = cmd;

                try
                {
                    esTransactionScope.Enlist(da.SelectCommand, request.ConnectionString, CreateIDbConnectionDelegate);

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
                    #endregion
                    {
                        da.Fill(dataSet);
                    }
                }
                finally
                {
                    esTransactionScope.DeEnlist(da.SelectCommand);
                }

                response.DataSet = dataSet;

                if (request.Parameters != null)
                {
                    Shared.GatherReturnParameters(cmd, request, response);
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

            return response;
        }

        static private esDataResponse LoadDataTableFromStoredProcedure(esDataRequest request)
        {
            esDataResponse response = new esDataResponse();
            OracleCommand cmd = null;

            try
            {
                DataTable dataTable = new DataTable(request.ProviderMetadata.Destination);

                cmd = new OracleCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = Shared.CreateFullName(request);
                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if (request.Parameters != null) Shared.AddParameters(cmd, request);

                OracleParameter p = new OracleParameter("outCursor", OracleType.Cursor);
                p.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(p);

                OracleDataAdapter da = new OracleDataAdapter();
                da.SelectCommand = cmd;

                try
                {
                    esTransactionScope.Enlist(da.SelectCommand, request.ConnectionString, CreateIDbConnectionDelegate);

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
                    #endregion
                    {
                        da.Fill(dataTable);
                    }
                }
                finally
                {
                    esTransactionScope.DeEnlist(da.SelectCommand);
                }

                response.Table = dataTable;

                if (request.Parameters != null)
                {
                    Shared.GatherReturnParameters(cmd, request, response);
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

            return response;
        }

        static private esDataResponse LoadDataTableFromText(esDataRequest request)
        {
            esDataResponse response = new esDataResponse();
            OracleCommand cmd = null;

            try
            {
                DataTable dataTable = new DataTable(request.ProviderMetadata.Destination);

                cmd = new OracleCommand();
                cmd.CommandType = CommandType.Text;
                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if (request.Parameters != null) Shared.AddParameters(cmd, request);

                OracleDataAdapter da = new OracleDataAdapter();
                cmd.CommandText = request.QueryText;
                da.SelectCommand = cmd;

                try
                {
                    esTransactionScope.Enlist(da.SelectCommand, request.ConnectionString, CreateIDbConnectionDelegate);

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
                    #endregion
                    {
                        da.Fill(dataTable);
                    }
                }
                finally
                {
                    esTransactionScope.DeEnlist(da.SelectCommand);
                }

                response.Table = dataTable;

                if (request.Parameters != null)
                {
                    Shared.GatherReturnParameters(cmd, request, response);
                }
            }
            catch (Exception ex)
            {
                CleanupCommand(cmd);
                throw;
            }
            finally
            {

            }

            return response;
        }

        static private esDataResponse LoadManyToMany(esDataRequest request)
        {
            esDataResponse response = new esDataResponse();
            OracleCommand cmd = null;

            try
            {
                DataTable dataTable = new DataTable(request.ProviderMetadata.Destination);

                cmd = new OracleCommand();
                cmd.CommandType = CommandType.Text;
                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;

                string mmQuery = request.QueryText;

                string[] sections = mmQuery.Split('|');
                string[] tables = sections[0].Split(',');
                string[] columns = sections[1].Split(',');

                // We build the query, we don't use Delimiters to avoid tons of extra concatentation
                string sql = "SELECT * FROM \"" + tables[0] + "\" JOIN \"" + tables[1] + "\" ON \"" + tables[0] + "\".\"" + columns[0] + "\" = \"";
                sql += tables[1] + "\".\"" + columns[1] + "\" WHERE \"" + tables[1] + "\".\"" + sections[2] + "\" = :";

                if (request.Parameters != null)
                {
                    foreach (esParameter esParam in request.Parameters)
                    {
                        sql += esParam.Name;
                    }

                    Shared.AddParameters(cmd, request);
                }

                OracleDataAdapter da = new OracleDataAdapter();
                cmd.CommandText = sql;

                da.SelectCommand = cmd;

                try
                {
                    esTransactionScope.Enlist(da.SelectCommand, request.ConnectionString, CreateIDbConnectionDelegate);

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
                    #endregion
                    {
                        da.Fill(dataTable);
                    }
                }
                finally
                {
                    esTransactionScope.DeEnlist(da.SelectCommand);
                }

                response.Table = dataTable;
            }
            catch (Exception)
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
        static private void LoadDataTableFromDynamicQuery(esDataRequest request, esDataResponse response, OracleCommand cmd)
        {
            try
            {
                response.LastQuery = cmd.CommandText;

                if (request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;

                DataTable dataTable = new DataTable(request.ProviderMetadata.Destination);

                OracleDataAdapter da = new OracleDataAdapter();
                da.SelectCommand = cmd;

                try
                {
                    esTransactionScope.Enlist(da.SelectCommand, request.ConnectionString, CreateIDbConnectionDelegate);

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
                    #endregion
                    {
                        da.Fill(dataTable);
                    }
                }
                finally
                {
                    esTransactionScope.DeEnlist(da.SelectCommand);
                }

                response.Table = dataTable;
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

        static private DataTable SaveStoredProcCollection(esDataRequest request)
        {
            if (request.CollectionSavePacket == null) return null;

            OracleCommand cmdInsert = null;
            OracleCommand cmdUpdate = null;
            OracleCommand cmdDelete = null;

            try
            {
                using (esTransactionScope scope = new esTransactionScope())
                {
                    OracleCommand cmd = null;
                    bool exception = false;

                    foreach (esEntitySavePacket packet in request.CollectionSavePacket)
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
                                    esTransactionScope.Enlist(cmdInsert, request.ConnectionString, CreateIDbConnectionDelegate);
                                }
                                cmd = cmdInsert;
                                break;
                            case tgDataRowState.Modified:
                                if (cmdUpdate == null)
                                {
                                    cmdUpdate = Shared.BuildStoredProcUpdateCommand(request, packet);
                                    esTransactionScope.Enlist(cmdUpdate, request.ConnectionString, CreateIDbConnectionDelegate);
                                }
                                cmd = cmdUpdate;
                                break;
                            case tgDataRowState.Deleted:
                                if (cmdDelete == null)
                                {
                                    cmdDelete = Shared.BuildStoredProcDeleteCommand(request, packet);
                                    esTransactionScope.Enlist(cmdDelete, request.ConnectionString, CreateIDbConnectionDelegate);
                                }
                                cmd = cmdDelete;
                                break;

                            case tgDataRowState.Unchanged:
                                continue;
                        }
                        #endregion

                        #region Preprocess Parameters
                        if (cmd.Parameters != null)
                        {
                            foreach (OracleParameter param in cmd.Parameters)
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
                        #endregion

                        #region Execute Command
                        try
                        {
                            int count = 0;

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
                            #endregion
                            {
                                count = cmd.ExecuteNonQuery();
                            }

                            if (count < 1)
                            {
                                throw new tgConcurrencyException("Update failed to update any records");
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
                        #endregion

                        #region Postprocess Commands
                        if (packet.RowState != tgDataRowState.Deleted && cmd.Parameters != null)
                        {
                            foreach (OracleParameter param in cmd.Parameters)
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
                        #endregion
                    }

                    scope.Complete();
                }
            }
            finally
            {
                if (cmdInsert != null) esTransactionScope.DeEnlist(cmdInsert);
                if (cmdUpdate != null) esTransactionScope.DeEnlist(cmdUpdate);
                if (cmdDelete != null) esTransactionScope.DeEnlist(cmdDelete);
            }

            return null;
        }

        static private DataTable SaveStoredProcEntity(esDataRequest request)
        {
            OracleCommand cmd = null;

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
                esTransactionScope.Enlist(cmd, request.ConnectionString, CreateIDbConnectionDelegate);
                int count = 0;

                #region Profiling
                if (sTraceHandler != null)
                {
                    using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "SaveEntityStoredProcedure", System.Environment.StackTrace))
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
                #endregion
                {
                    count = cmd.ExecuteNonQuery();
                }

                if (count < 1)
                {
                    throw new tgConcurrencyException("Update failed to update any records");
                }
            }
            finally
            {
                esTransactionScope.DeEnlist(cmd);
                cmd.Dispose();
            }

            if (request.EntitySavePacket.RowState != tgDataRowState.Deleted && cmd.Parameters != null)
            {
                foreach (OracleParameter param in cmd.Parameters)
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

        static private DataTable SaveDynamicCollection(esDataRequest request)
        {
            if (request.CollectionSavePacket == null) return null;

            using (esTransactionScope scope = new esTransactionScope())
            {
                OracleCommand cmd = null;
                bool exception = false;

                foreach (esEntitySavePacket packet in request.CollectionSavePacket)
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
                        esTransactionScope.Enlist(cmd, request.ConnectionString, CreateIDbConnectionDelegate);
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
                        #endregion
                        {
                            count = cmd.ExecuteNonQuery();
                        }

                        if (count < 1)
                        {
                            throw new tgConcurrencyException("Update failed to update any records");
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
                        esTransactionScope.DeEnlist(cmd);
                        cmd.Dispose();
                    }

                    if (!exception && packet.RowState != tgDataRowState.Deleted && cmd.Parameters != null)
                    {
                        foreach (OracleParameter param in cmd.Parameters)
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

        static private DataTable SaveDynamicEntity(esDataRequest request)
        {
            OracleCommand cmd = null;

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
                esTransactionScope.Enlist(cmd, request.ConnectionString, CreateIDbConnectionDelegate);
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
                #endregion
                {
                    count = cmd.ExecuteNonQuery();
                }

                if (count < 1)
                {
                    throw new tgConcurrencyException("Update failed to update any records");
                }
            }
            finally
            {
                esTransactionScope.DeEnlist(cmd);
                cmd.Dispose();
            }

            if (request.EntitySavePacket.RowState != tgDataRowState.Deleted && cmd.Parameters != null)
            {
                foreach (OracleParameter param in cmd.Parameters)
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
    }
}
