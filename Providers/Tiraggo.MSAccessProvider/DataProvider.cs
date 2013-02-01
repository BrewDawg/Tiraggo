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
using System.Data.OleDb;

using Tiraggo.DynamicQuery;
using Tiraggo.Interfaces;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace Tiraggo.MSAccessProvider
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

            public esTraceArguments(tgDataRequest request, IDbCommand cmd, tgEntitySavePacket packet, string action, string callStack)
            {
                PacketOrder = Interlocked.Increment(ref esTraceArguments.packetOrder);

                this.command = cmd;

                TraceChannel = DataProvider.sTraceChannel;
                Syntax = "ACCESS";
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
                        OleDbParameter param = parameters[i] as OleDbParameter;

                        esTraceParameter p = new esTraceParameter()
                        {
                            Name = param.ParameterName,
                            Direction = param.Direction.ToString(),
                            ParamType = param.OleDbType.ToString().ToUpper(),
                            BeforeValue = param.Value != null && param.Value != DBNull.Value ? Convert.ToString(param.Value) : "null"
                        };

                        try
                        {
                            // Let's make it look like we're using parameters for the profiler
                            if (param.Value == null || param.Value == DBNull.Value)
                            {
                                if (param.SourceVersion == DataRowVersion.Current)
                                {
                                    object o = packet.CurrentValues[param.SourceColumn];
                                    if (o != null && o != DBNull.Value)
                                    {
                                        p.BeforeValue = Convert.ToString(o);
                                    }
                                }
                                else if (param.SourceVersion == DataRowVersion.Original)
                                {
                                    object o = packet.OriginalValues[param.SourceColumn];
                                    if (o != null && o != DBNull.Value)
                                    {
                                        p.BeforeValue = Convert.ToString(o);
                                    }
                                }
                            }
                        }
                        catch { }

                        this.Parameters.Add(p);
                    }
                }

                stopwatch = Stopwatch.StartNew();
            }

            public esTraceArguments(tgDataRequest request, IDbCommand cmd, string action, string callStack)
            {
                PacketOrder = Interlocked.Increment(ref esTraceArguments.packetOrder);

                this.command = cmd;

                TraceChannel = DataProvider.sTraceChannel;
                Syntax = "ACCESS";
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
                        OleDbParameter param = parameters[i] as OleDbParameter;

                        esTraceParameter p = new esTraceParameter()
                        {
                            Name = param.ParameterName,
                            Direction = param.Direction.ToString(),
                            ParamType = param.OleDbType.ToString().ToUpper(),
                            BeforeValue = param.Value != null && param.Value != DBNull.Value ? Convert.ToString(param.Value) : "null"
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
        /// This method acts as a delegate for tgTransactionScope
        /// </summary>
        /// <returns></returns>
        static private IDbConnection CreateIDbConnectionDelegate()
        {
            return new OleDbConnection();
        }

        static private void CleanupCommand(OleDbCommand cmd)
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

                        response.Table = LoadDataTableFromStoredProcedure(request);
                        break;

                    case tgQueryType.Text:

                        response.Table = LoadDataTableFromText(request);
                        break;

                    case tgQueryType.DynamicQuery:

                        response = new tgDataResponse();
                        OleDbCommand cmd = QueryBuilder.PrepareCommand(request);
                        LoadDataTableFromDynamicQuery(request, response, cmd);
                        break;

                    case tgQueryType.DynamicQueryParseOnly:

                        response = new tgDataResponse();
                        OleDbCommand cmd1 = QueryBuilder.PrepareCommand(request);
                        response.LastQuery = cmd1.CommandText;
                        break;

                    case tgQueryType.ManyToMany:

                        response.Table = LoadManyToMany(request);
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
            catch (OleDbException ex)
            {
                tgException es = Shared.CheckForConcurrencyException(ex);
                if (es != null)
                {
                    response.Exception = es;
                }
                else
                {
                    response.Exception = ex;
                }
            }
            catch (DBConcurrencyException dbex)
            {
                response.Exception = new tgConcurrencyException("Error in MSAccessProvider.esSaveDataTable", dbex);
            }

            response.Table = request.Table;
            return response;
        }

        tgDataResponse IDataProvider.ExecuteNonQuery(tgDataRequest request)
        {
            tgDataResponse response = new tgDataResponse();
            OleDbCommand cmd = null;

            try
            {
                cmd = new OleDbCommand();
                if(request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if(request.Parameters != null) AddParameters(cmd, request);

                cmd.CommandText = request.QueryText;

                switch (request.QueryType)
                {
                    case tgQueryType.TableDirect:
                        cmd.CommandType = CommandType.TableDirect;
                        break;

                    case tgQueryType.StoredProcedure:
                        cmd.CommandType = CommandType.StoredProcedure;
                        break;

                    case tgQueryType.Text:
                        cmd.CommandType = CommandType.Text;
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
                    #endregion
                    {
                        response.RowsEffected = cmd.ExecuteNonQuery();
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(cmd);
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
            OleDbCommand cmd = null;

            try
            {
                cmd = new OleDbCommand();
                if(request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if(request.Parameters != null) AddParameters(cmd, request);

                cmd.CommandText = request.QueryText;

                switch (request.QueryType)
                {
                    case tgQueryType.TableDirect:
                        cmd.CommandType = CommandType.TableDirect;
                        break;

                    case tgQueryType.StoredProcedure:
                        cmd.CommandType = CommandType.StoredProcedure;
                        break;

                    case tgQueryType.Text:
                        cmd.CommandType = CommandType.Text;
                        break;

                    case tgQueryType.DynamicQuery:
                        cmd = QueryBuilder.PrepareCommand(request);
                        break;
                }

                cmd.Connection = new OleDbConnection(request.ConnectionString);
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

        tgDataResponse IDataProvider.ExecuteScalar(tgDataRequest request)
        {
            tgDataResponse response = new tgDataResponse();
            OleDbCommand cmd = null;

            try
            {
                cmd = new OleDbCommand();
                if(request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if(request.Parameters != null) AddParameters(cmd, request);

                cmd.CommandText = request.QueryText;

                switch (request.QueryType)
                {
                    case tgQueryType.TableDirect:
                        cmd.CommandType = CommandType.TableDirect;
                        break;

                    case tgQueryType.StoredProcedure:
                        cmd.CommandType = CommandType.StoredProcedure;
                        break;

                    case tgQueryType.Text:
                        cmd.CommandType = CommandType.Text;
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
                    #endregion
                    {
                        response.Scalar = cmd.ExecuteScalar();
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(cmd);
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

                        response.DataSet = LoadDataSetFromStoredProcedure(request);
                        break;

                    case tgQueryType.Text:

                        response.DataSet = LoadDataSetFromText(request);
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

                        response.Table = LoadDataTableFromStoredProcedure(request);
                        break;

                    case tgQueryType.Text:

                        response.Table = LoadDataTableFromText(request);
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

        static private DataSet LoadDataSetFromStoredProcedure(tgDataRequest request)
        {
            DataSet dataSet = null;
            OleDbCommand cmd = null;

            try
            {
                dataSet = new DataSet();

                cmd = new OleDbCommand();
                cmd.CommandText = request.QueryText;
                cmd.CommandType = CommandType.StoredProcedure;
                if(request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;

                if (request.Parameters != null)
                {
                    foreach (tgParameter param in request.Parameters)
                    {
                        cmd.Parameters.AddWithValue(Delimiters.Param + param.Name, param.Value);
                    }
                }

                OleDbDataAdapter da = new OleDbDataAdapter();
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
                    #endregion
                    {
                        da.Fill(dataSet);
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(da.SelectCommand);
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

            return dataSet;
        }

        static private DataSet LoadDataSetFromText(tgDataRequest request)
        {
            DataSet dataSet = null;
            OleDbCommand cmd = null;

            try
            {
                dataSet = new DataSet();

                cmd = new OleDbCommand();
                cmd.CommandType = CommandType.Text;
                if(request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if(request.Parameters != null) AddParameters(cmd, request);

                OleDbDataAdapter da = new OleDbDataAdapter();
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
                    #endregion
                    {
                        da.Fill(dataSet);
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(da.SelectCommand);
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

            return dataSet;
        }

        static private DataTable LoadManyToMany(tgDataRequest request)
        {
            DataTable dataTable = null;
            OleDbCommand cmd = null;

            try
            {
                dataTable = new DataTable(request.ProviderMetadata.Destination);

                cmd = new OleDbCommand();
                cmd.CommandType = CommandType.Text;
                if(request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;

                string mmQuery = request.QueryText;

                string[] sections = mmQuery.Split('|');
                string[] tables  = sections[0].Split(',');
                string[] columns = sections[1].Split(',');

                // We build the query, we don't use Delimiters to avoid tons of extra concatentation
                string sql = "SELECT * FROM [" + tables[0] + "] LEFT JOIN [" + tables[1] + "] ON [" + tables[0] + "].[" + columns[0] + "] = [";
                sql += tables[1] + "].[" + columns[1] + "] WHERE [" + tables[1] + "].[" + sections[2] + "] = @";

                if (request.Parameters != null)
                {
                    foreach (tgParameter esParam in request.Parameters)
                    {
                        sql += esParam.Name;
                    }

                    AddParameters(cmd, request);
                }

                OleDbDataAdapter da = new OleDbDataAdapter();
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
                    #endregion
                    {
                        da.Fill(dataTable);
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(da.SelectCommand);
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

            return dataTable;
        }

        static private DataTable LoadDataTableFromStoredProcedure(tgDataRequest request)
        {
            DataTable dataTable = null;
            OleDbCommand cmd = null;

            try
            {
                dataTable = new DataTable(request.ProviderMetadata.Destination);

                cmd = new OleDbCommand();
                cmd.CommandText = request.QueryText;
                cmd.CommandType = CommandType.StoredProcedure;
                if(request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;

                if (request.Parameters != null)
                {
                    foreach (tgParameter param in request.Parameters)
                    {
                        cmd.Parameters.AddWithValue(Delimiters.Param + param.Name, param.Value);
                    }
                }

                OleDbDataAdapter da = new OleDbDataAdapter();
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
                    #endregion
                    {
                        da.Fill(dataTable);
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(da.SelectCommand);
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

            return dataTable;
        }

        static private DataTable LoadDataTableFromText(tgDataRequest request)
        {
            DataTable dataTable = null;
            OleDbCommand cmd = null;

            try
            {
                dataTable = new DataTable(request.ProviderMetadata.Destination);

                cmd = new OleDbCommand();
                cmd.CommandType = CommandType.Text;
                if(request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;
                if(request.Parameters != null) AddParameters(cmd, request);

                OleDbDataAdapter da = new OleDbDataAdapter();
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
                    #endregion
                    {
                        da.Fill(dataTable);
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(da.SelectCommand);
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

            return dataTable;
        }

        
        // This is used only to execute the Dynamic Query API
        static private void LoadDataTableFromDynamicQuery(tgDataRequest request, tgDataResponse response, OleDbCommand cmd)
        {
            DataTable dataTable = null;

            try
            {
                response.LastQuery = cmd.CommandText;

                if(request.CommandTimeout != null) cmd.CommandTimeout = request.CommandTimeout.Value;

                dataTable = new DataTable(request.ProviderMetadata.Destination);

                OleDbDataAdapter da = new OleDbDataAdapter();
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
                    #endregion
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
            throw new NotImplementedException("Stored Procedures not supported");
        }

        static private DataTable SaveStoredProcEntity(tgDataRequest request)
        {
            throw new NotImplementedException("Stored Procedures not supported");
        }

        static private DataTable SaveDynamicCollection(tgDataRequest request)
        {
            tgEntitySavePacket pkt = request.CollectionSavePacket[0];

            if (pkt.RowState == tgDataRowState.Deleted)
            {
                //============================================================================
                // We do all our deletes at once, so if the first one is a delete they all are
                //============================================================================
                return SaveDynamicCollection_Deletes(request);
            }
            else
            {
                //============================================================================
                // We do all our Inserts and Updates at once
                //============================================================================
                return SaveDynamicCollection_InsertsUpdates(request);
            }
        }

        static private DataTable SaveDynamicCollection_InsertsUpdates(tgDataRequest request)
        {
            DataTable dataTable = CreateDataTable(request);

            using (tgTransactionScope scope = new tgTransactionScope())
            {
                using (OleDbDataAdapter da = new OleDbDataAdapter())
                {
                    da.AcceptChangesDuringUpdate = false;
                    da.ContinueUpdateOnError = request.ContinueUpdateOnError;

                    OleDbCommand cmd = null;

                    if (!request.IgnoreComputedColumns)
                    {
                        da.RowUpdated += new OleDbRowUpdatedEventHandler(OnRowUpdated);
                    }

                    foreach (tgEntitySavePacket packet in request.CollectionSavePacket)
                    {
                        if (packet.RowState != tgDataRowState.Added && packet.RowState != tgDataRowState.Modified) continue;

                        DataRow row = dataTable.NewRow();
                        dataTable.Rows.Add(row);

                        switch (packet.RowState)
                        {
                            case tgDataRowState.Added:
                                cmd = da.InsertCommand = Shared.BuildDynamicInsertCommand(request, packet);
                                SetModifiedValues(request, packet, row);
                                break;

                            case tgDataRowState.Modified:
                                cmd = da.UpdateCommand = Shared.BuildDynamicUpdateCommand(request, packet);
                                SetOriginalValues(request, packet, row, false);
                                SetModifiedValues(request, packet, row);
                                row.AcceptChanges();
                                row.SetModified();
                                break;
                        }

                        request.Properties["tgDataRequest"] = request;
                        request.Properties["esEntityData"] = packet;
                        dataTable.ExtendedProperties["props"] = request.Properties;

                        DataRow[] singleRow = new DataRow[1];
                        singleRow[0] = row;

                        try
                        {
                            tgTransactionScope.Enlist(cmd, request.ConnectionString, CreateIDbConnectionDelegate);

                            #region Profiling
                            if (sTraceHandler != null)
                            {
                                using (esTraceArguments esTrace = new esTraceArguments(request, cmd, packet, "SaveCollectionDynamic", System.Environment.StackTrace))
                                {
                                    try
                                    {
                                        da.Update(singleRow);
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
                                da.Update(singleRow);
                            }

                            if (row.HasErrors)
                            {
                                request.FireOnError(packet, row.RowError);
                            }
                        }
                        finally
                        {
                            tgTransactionScope.DeEnlist(cmd);
                            dataTable.Rows.Clear();
                        }

                        if (!row.HasErrors && packet.RowState != tgDataRowState.Deleted && cmd.Parameters != null)
                        {
                            foreach (OleDbParameter param in cmd.Parameters)
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
                }

                scope.Complete();
            }

            return dataTable;
        }

        static private DataTable SaveDynamicCollection_Deletes(tgDataRequest request)
        {
            OleDbCommand cmd = null;

            DataTable dataTable = CreateDataTable(request);

            using (tgTransactionScope scope = new tgTransactionScope())
            {
                using (OleDbDataAdapter da = new OleDbDataAdapter())
                {
                    da.AcceptChangesDuringUpdate = false;
                    da.ContinueUpdateOnError = request.ContinueUpdateOnError;

                    try
                    {
                        cmd = da.DeleteCommand = Shared.BuildDynamicDeleteCommand(request, request.CollectionSavePacket[0].ModifiedColumns);
                        tgTransactionScope.Enlist(cmd, request.ConnectionString, CreateIDbConnectionDelegate);

                        DataRow[] singleRow = new DataRow[1];

                        // Delete each record
                        foreach (tgEntitySavePacket packet in request.CollectionSavePacket)
                        {
                            DataRow row = dataTable.NewRow();
                            dataTable.Rows.Add(row);

                            SetOriginalValues(request, packet, row, true);
                            row.AcceptChanges();
                            row.Delete();

                            singleRow[0] = row;

                            #region Profiling
                            if (sTraceHandler != null)
                            {
                                using (esTraceArguments esTrace = new esTraceArguments(request, cmd, packet, "SaveCollectionDynamic", System.Environment.StackTrace))
                                {
                                    try
                                    {
                                        da.Update(singleRow);
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
                                da.Update(singleRow);
                            }

                            if (row.HasErrors)
                            {
                                request.FireOnError(packet, row.RowError);
                            }

                            dataTable.Rows.Clear(); // ADO.NET won't let us reuse the same DataRow
                        }
                    }
                    finally
                    {
                        tgTransactionScope.DeEnlist(cmd);
                    }
                }
                scope.Complete();
            }

            return request.Table;
        }

        static private DataTable SaveDynamicEntity(tgDataRequest request)
        {
            bool needToDelete = request.EntitySavePacket.RowState == tgDataRowState.Deleted;

            DataTable dataTable = CreateDataTable(request);

            using (OleDbDataAdapter da = new OleDbDataAdapter())
            {
                da.AcceptChangesDuringUpdate = false;

                DataRow row = dataTable.NewRow();
                dataTable.Rows.Add(row);

                OleDbCommand cmd = null;

                switch (request.EntitySavePacket.RowState)
                {
                    case tgDataRowState.Added:
                        cmd = da.InsertCommand = Shared.BuildDynamicInsertCommand(request, request.EntitySavePacket);
                        SetModifiedValues(request, request.EntitySavePacket, row);
                        break;

                    case tgDataRowState.Modified:
                        cmd = da.UpdateCommand = Shared.BuildDynamicUpdateCommand(request, request.EntitySavePacket);
                        SetOriginalValues(request, request.EntitySavePacket, row, false);
                        SetModifiedValues(request, request.EntitySavePacket, row);
                        row.AcceptChanges();
                        row.SetModified();
                        break;

                    case tgDataRowState.Deleted:
                        cmd = da.DeleteCommand = Shared.BuildDynamicDeleteCommand(request, null);
                        SetOriginalValues(request, request.EntitySavePacket, row, true);
                        row.AcceptChanges();
                        row.Delete();
                        break;
                }

                if (!needToDelete && request.Properties != null)
                {
                    request.Properties["tgDataRequest"] = request;
                    request.Properties["esEntityData"] = request.EntitySavePacket;
                    dataTable.ExtendedProperties["props"] = request.Properties;
                }

                DataRow[] singleRow = new DataRow[1];
                singleRow[0] = row;

                if (!request.IgnoreComputedColumns)
                {
                    da.RowUpdated += new OleDbRowUpdatedEventHandler(OnRowUpdated);
                }

                try
                {
                    tgTransactionScope.Enlist(cmd, request.ConnectionString, CreateIDbConnectionDelegate);

                    #region Profiling
                    if (sTraceHandler != null)
                    {
                        using (esTraceArguments esTrace = new esTraceArguments(request, cmd, request.EntitySavePacket, "SaveEntityDynamic", System.Environment.StackTrace))
                        {
                            try
                            {
                                da.Update(singleRow);
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
                        da.Update(singleRow);
                    }
                }
                finally
                {
                    tgTransactionScope.DeEnlist(cmd);
                }

                if (request.EntitySavePacket.RowState != tgDataRowState.Deleted && cmd.Parameters != null)
                {
                    foreach (OleDbParameter param in cmd.Parameters)
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
            }

            return dataTable;
        }

        static private DataTable CreateDataTable(tgDataRequest request)
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

        static void SetOriginalValues(tgDataRequest request, tgEntitySavePacket packet, DataRow row, bool primaryKeysAndConcurrencyOnly)
        {
            foreach (tgColumnMetadata col in request.Columns)
            {
                if (primaryKeysAndConcurrencyOnly &&
                    (!col.IsInPrimaryKey && !col.IsConcurrency && !col.IsEntitySpacesConcurrency)) continue;

                string columnName = col.Name;

                if (packet.OriginalValues.ContainsKey(columnName))
                {
                    row[columnName] = packet.OriginalValues[columnName];
                }
            }
        }

        static void SetModifiedValues(tgDataRequest request, tgEntitySavePacket packet, DataRow row)
        {
            foreach (string column in packet.ModifiedColumns)
            {
                if (request.Columns.FindByColumnName(column) != null)
                {
                    row[column] = packet.CurrentValues[column];
                }
            }
        }


        protected static void OnRowUpdated(object sender, OleDbRowUpdatedEventArgs e)
        {
            try
            {
                PropertyCollection props = e.Row.Table.ExtendedProperties;
                if (props.ContainsKey("props"))
                {
                    props = (PropertyCollection)props["props"];
                }

                if (e.Status == UpdateStatus.Continue && (e.StatementType == StatementType.Insert || e.StatementType == StatementType.Update))
                {
                    tgDataRequest request = props["tgDataRequest"] as tgDataRequest;
                    tgEntitySavePacket packet = (tgEntitySavePacket)props["esEntityData"];

                    if (e.StatementType == StatementType.Insert)
                    {
                        if (props.Contains("AutoInc"))
                        {
                            string autoInc = props["AutoInc"] as string;

                            OleDbCommand cmd = new OleDbCommand();
                            cmd.Connection = e.Command.Connection;
                            cmd.Transaction = e.Command.Transaction;

                            cmd.CommandText = "SELECT @@IDENTITY";
                            cmd.CommandType = CommandType.Text;

                            object o = null;

                            #region Profiling
                            if (sTraceHandler != null)
                            {
                                using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "OnRowUpdated", System.Environment.StackTrace))
                                {
                                    try
                                    {
                                        o = cmd.ExecuteScalar();
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
                                o = cmd.ExecuteScalar();
                            }

                            if (o != null)
                            {
                                packet.CurrentValues[autoInc] = o;
                                e.Row[autoInc] = o;
                            }
                        }

                        if (props.Contains("EntitySpacesConcurrency"))
                        {
                            string esConcurrencyColumn = props["EntitySpacesConcurrency"] as string;
                            packet.CurrentValues[esConcurrencyColumn] = 1;
                        }
                    }

                    //-------------------------------------------------------------------------------------------------
                    // Fetch any defaults, SQLite doesn't support output parameters so we gotta do this the hard way
                    //-------------------------------------------------------------------------------------------------
                    if (props.Contains("Defaults"))
                    {
                        // Build the Where parameter and parameters
                        OleDbCommand cmd = new OleDbCommand();
                        cmd.Connection = e.Command.Connection;
                        cmd.Transaction = e.Command.Transaction;

                        string select = (string)props["Defaults"];

                        string[] whereParameters = ((string)props["Where"]).Split(',');

                        string comma = String.Empty;
                        string where = String.Empty;
                        int i = 1;
                        foreach (string parameter in whereParameters)
                        {
                            OleDbParameter p = new OleDbParameter("@p" + i++.ToString(), e.Row[parameter]);
                            cmd.Parameters.Add(p);
                            where += comma + "[" + parameter + "] = " + p.ParameterName;
                            comma = " AND ";
                        }

                        // Okay, now we can execute the sql and get any values that have defaults that were
                        // null at the time of the insert and/or our timestamp
                        cmd.CommandText = "SELECT " + select + " FROM [" + request.ProviderMetadata.Source + "] WHERE " + where + ";";

                        OleDbDataReader rdr = null;

                        try
                        {
                            #region Profiling
                            if (sTraceHandler != null)
                            {
                                using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "OnRowUpdated", System.Environment.StackTrace))
                                {
                                    try
                                    {
                                        rdr = cmd.ExecuteReader(CommandBehavior.SingleResult);
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
                                rdr = cmd.ExecuteReader(CommandBehavior.SingleResult);
                            }

                            if (rdr.Read())
                            {
                                select = select.Replace("[", String.Empty).Replace("]", String.Empty);
                                string[] selectCols = select.Split(',');

                                for (int k = 0; k < selectCols.Length; k++)
                                {
                                    packet.CurrentValues[selectCols[k]] = rdr.GetValue(k);
                                }
                            }
                        }
                        finally
                        {
                            // Make sure we close the reader no matter what
                            if (rdr != null) rdr.Close();
                        }
                    }

                    if (e.StatementType == StatementType.Update)
                    {
                        string colName = props["EntitySpacesConcurrency"] as string;
                        object o = e.Row[colName];

                        switch (Type.GetTypeCode(o.GetType()))
                        {
                            case TypeCode.Int16: packet.CurrentValues[colName] = ((System.Int16)o) + 1; break;
                            case TypeCode.Int32: packet.CurrentValues[colName] = ((System.Int32)o) + 1; break;
                            case TypeCode.Int64: packet.CurrentValues[colName] = ((System.Int64)o) + 1; break;
                            case TypeCode.UInt16: packet.CurrentValues[colName] = ((System.UInt16)o) + 1; break;
                            case TypeCode.UInt32: packet.CurrentValues[colName] = ((System.UInt32)o) + 1; break;
                            case TypeCode.UInt64: packet.CurrentValues[colName] = ((System.UInt64)o) + 1; break;
                        }
                    }
                }
            }
            catch { }
        }

        // If it's an Insert we fetch the @@Identity value and stuff it in the proper column
        protected static void OnRowUpdated1(object sender, OleDbRowUpdatedEventArgs e)
        {
            try
            {
                if(e.StatementType == StatementType.Delete || e.Status != UpdateStatus.Continue) return;

                PropertyCollection props = e.Row.Table.ExtendedProperties;

                if (e.StatementType == StatementType.Insert)
                {
                    tgEntitySavePacket packet = (tgEntitySavePacket)props["esEntityData"];

                    if (e.Row.Table.ExtendedProperties.Contains("AutoInc"))
                    {
                        tgDataRequest request = props["tgDataRequest"] as tgDataRequest;
                        string autoInc = props["AutoInc"] as string;

                        OleDbCommand cmd = new OleDbCommand();
                        cmd.Connection = e.Command.Connection;
                        cmd.Transaction = e.Command.Transaction;

                        cmd.CommandText = "SELECT @@IDENTITY";
                        cmd.CommandType = CommandType.Text;

                        object o = null;

                        #region Profiling
                        if (sTraceHandler != null)
                        {
                            using (esTraceArguments esTrace = new esTraceArguments(request, cmd, "OnRowUpdated", System.Environment.StackTrace))
                            {
                                try
                                {
                                    o = cmd.ExecuteScalar();
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
                            o = cmd.ExecuteScalar();
                        }

                        if (o != null)
                        {
                            packet.CurrentValues[autoInc] = o;
                        }
                    }

                    if (props.Contains("EntitySpacesConcurrency"))
                    {
                        string colName = props["EntitySpacesConcurrency"] as string;
                        packet.CurrentValues[colName] = 1;
                    }
                }
                else if (e.StatementType == StatementType.Update)
                {
                    if (props.Contains("EntitySpacesConcurrency"))
                    {
                        tgEntitySavePacket packet = (tgEntitySavePacket)props["esEntityData"];

                        string colName = props["EntitySpacesConcurrency"] as string;
                        object o = e.Row[colName];

                        switch (Type.GetTypeCode(o.GetType()))
                        {
                            case TypeCode.Int16: packet.CurrentValues[colName] = ((System.Int16)o) + 1; break;
                            case TypeCode.Int32: packet.CurrentValues[colName] = ((System.Int32)o) + 1; break;
                            case TypeCode.Int64: packet.CurrentValues[colName] = ((System.Int64)o) + 1; break;
                            case TypeCode.UInt16: packet.CurrentValues[colName] = ((System.UInt16)o) + 1; break;
                            case TypeCode.UInt32: packet.CurrentValues[colName] = ((System.UInt32)o) + 1; break;
                            case TypeCode.UInt64: packet.CurrentValues[colName] = ((System.UInt64)o) + 1; break;
                        }

                        e.Row.AcceptChanges();
                    }
                }
            }
            catch { }
        }

        static private void AddParameters(OleDbCommand cmd, tgDataRequest request)
        {
            if (request.QueryType == tgQueryType.Text && request.QueryText != null && request.QueryText.Contains("{0}"))
            {
                int i = 0;
                string token = String.Empty;
                string sIndex = String.Empty;
                string param = String.Empty;

                foreach (tgParameter esParam in request.Parameters)
                {
                    sIndex = i.ToString();
                    token = '{' + sIndex + '}';
                    param = Delimiters.Param + "p" + sIndex;
                    request.QueryText = request.QueryText.Replace(token, param);
                    i++;

                    cmd.Parameters.AddWithValue(Delimiters.Param + esParam.Name, esParam.Value);
                }
            }
            else
            {
                foreach (tgParameter esParam in request.Parameters)
                {
                    cmd.Parameters.AddWithValue(Delimiters.Param + esParam.Name, esParam.Value);
                }
            }
        }
    }
}
