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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Reflection;

using Tiraggo.Interfaces;
using Tiraggo.DynamicQuery;


namespace Tiraggo.Core
{
    public delegate object ModifiedByEventHandler();

    /// <summary>
    /// The base class for a single entity.
    /// </summary>
    /// <remarks>
    /// In EntitySpaces, Business Objects such as Employees (derived from <see cref="tgEntity"/>)
    /// or EmployeesCollection (derived from <see cref="tgEntityCollection"/>) map to a table or a view in a database.
    /// Views based objects are read-only though in some cases can be made to be updateable.
    /// Tables must have primary keys in order to be used with Tiraggo. An EntitySpaces solution can use 
    /// stored procedures or dynamic sql for CRUD operations (create, retrieve, update, delete). 
    /// </remarks>
    /// <example>
    /// An entity can exist alone.
    /// <code>
    /// Employees entity = new Employees();
    /// if(entity.LoadByPrimaryKey(10))
    /// {
    ///     entity.Salary = entity.Salary.Value * 1.05m;
    ///     entity.Save();
    /// }
    /// </code>
    /// Or it can exist as part of a collection.
    /// <code>
    /// EmployeesCollection collection = new EmployeesCollection();
    /// entity.LastName = "Jones";
    /// entity.FirstName = "Alice";
    /// collection.Save();
    /// </code>
    /// The esEntity's Query mechanism can be used to load the data, however the query must return only 1 row. 
    /// The boolean return of Query.Load() is set to true if only 1 row is retrieved, false if no rows are retrieved, 
    /// and an exception will be thrown if more than 1 row is returned. A generated entity also has a LoadByPrimaryKey
    /// method.
    /// <code>
    /// Employees entity = new Employees();
    /// entity.Query.es.Top = 1;
    /// entity.Query.OrderBy(entity.Query.Salary.Descending);
    /// if(entity.Query.Load())
    /// {
    ///		// record was loaded, I wish I made his salary !
    /// }
    /// </code>
    /// </example>
    [Serializable]
    [XmlType(IncludeInSchema = false)]
    [DataContract]
    public abstract class tgEntity : IVisitable, IEditableObject, IEntity, ICommittable, INotifyPropertyChanged, IDataErrorInfo
#if (WebBinding)
       , ICustomTypeDescriptor
#endif
    {
        public tgEntity()
        {
            rowState = tgDataRowState.Added;
            currentValues.FirstAccess += new tgSmartDictionary.esSmartDictionaryFirstAccessEventHandler(CurrentValues_OnFirstAccess);
        }

        /// <summary>
        /// Whenever a column that is of the string type has it's setter called this
        /// property is evaluated, if true, then an string of lenght zero is set to NULL in the database
        /// </summary>
        static public bool ConvertEmptyStringToNull = false;

        private void CurrentValues_OnFirstAccess(tgSmartDictionary smartDictionary)
        {
            smartDictionary.Allocate(tg.Meta.Columns.Count);

            foreach (tgColumnMetadata col in tg.Meta.Columns)
            {
                currentValues.SetOrdinal(col.Name, col.Ordinal);
            }
        }

        /// <summary>
        /// Invoked when a DynamicQuery has successfully executed on a single
        /// Entity. Throws an exception if more than one row is returned.
        /// </summary>
        /// <remarks>
        /// If you overload this you need to call down to this method in order
        /// to populate your entity.
        /// </remarks>
        /// <param name="query">The Query that was just loaded</param>
        /// <param name="table">The DataTable which contains what will become the Entity's data</param>
        /// <returns>True if a record was found</returns>
        protected bool OnQueryLoaded(tgDynamicQuery query, DataTable table)
        {
            if (table.Rows.Count > 1)
            {
                throw new Exception("An Entity can only hold 1 record of data");
            }

            bool dataFound = this.PopulateEntity(table);

            if (query.tg2.PrefetchMaps != null)
            {
                bool firstTime = true;

                // Add ourself into the list with a map name of string.Empty
                Dictionary<string, Dictionary<object, tgEntityCollectionBase>> collections = new Dictionary<string, Dictionary<object, tgEntityCollectionBase>>();
                Dictionary<object, tgEntityCollectionBase> me = new Dictionary<object, tgEntityCollectionBase>();
                collections[string.Empty] = me;

                this.tg.IsLazyLoadDisabled = true;

                foreach (tgPrefetchMap map in query.tg2.PrefetchMaps)
                {
                    DataTable preFetchedTable = map.Table;

                    Dictionary<object, tgEntityCollectionBase> loadedCollections = null;
                    Dictionary<object, tgEntityCollectionBase> newCollection = new Dictionary<object, tgEntityCollectionBase>();

                    if (map.Path == string.Empty)
                    {
                        loadedCollections = collections[string.Empty];

                        if (firstTime)
                        {
                            ProcessEntityForPrefetch(this, map, newCollection);
                            firstTime = true;
                        }
                    }
                    else
                    {
                        loadedCollections = collections[map.Path];
                    }

                    foreach (tgEntityCollectionBase collection in loadedCollections.Values)
                    {
                        foreach (tgEntity obj in collection)
                        {
                            ProcessEntityForPrefetch(obj, map, newCollection);
                        }
                    }

                    Dictionary<string, int> ordinals = null;
                    DataRowCollection rows = preFetchedTable.Rows;
                    int count = rows.Count;
                    for (int i = 0; i < count; i++)
                    {
                        DataRow row = rows[i];

                        object key = null;

                        // Avoid doing the Split() if we can
                        if (map.IsMultiPartKey)
                        {
                            key = string.Empty;

                            string[] columns = map.MyColumnName.Split(',');
                            foreach (string col in columns)
                            {
                                key += Convert.ToString(row[col]);
                            }
                        }
                        else
                        {
                            key = row[map.MyColumnName];
                        }

                        tgEntityCollectionBase c = newCollection[key];

                        IEntityCollection iColl = c as IEntityCollection;
                        ordinals = iColl.PopulateCollection(row, ordinals);
                    }

                    collections[map.PropertyName] = newCollection;
                }
            }

            return dataFound;
        }

        /// <summary>
        /// Used Internally by EntitySpaces to avoid duplicating some code
        /// </summary>
        static private void ProcessEntityForPrefetch(tgEntity obj, tgPrefetchMap map, Dictionary<object, tgEntityCollectionBase> newCollection)
        {
            obj.tg.IsLazyLoadDisabled = true;

            object key = null;

            // Avoid doing the Split() if we can
            if (map.IsMultiPartKey)
            {
                key = string.Empty;

                string[] columns = map.ParentColumnName.Split(',');
                foreach (string col in columns)
                {
                    key += Convert.ToString(obj.GetColumn(col));
                }
            }
            else
            {
                key = obj.GetColumn(map.ParentColumnName);
            }

            IEntity iEntity = obj as IEntity;
            newCollection[key] = iEntity.CreateCollection(map.PropertyName);
        }


        /// <summary>
        /// Creates parameters for sql passed in with the {0} {1} {2} syntax
        /// </summary>
        /// <param name="parameters">A variable length array of object values to be turned into parameters</param>
        /// <returns>An tgParameters Collection</returns>
        static private tgParameters PackageParameters(params object[] parameters)
        {
            tgParameters esParams = null;

            int i = 0;
            string sIndex = String.Empty;
            string param = String.Empty;

            if (parameters != null)
            {
                esParams = new tgParameters();

                foreach (object o in parameters)
                {
                    sIndex = i.ToString();
                    param = "p" + sIndex;
                    esParams.Add(param, o);
                    i++;
                }
            }

            return esParams;
        }

        /// <summary>
        /// Called internally anytime the esEntity class is about to make a call to
        /// one of the EntitySpaces' DataProviders. This method wraps up most of the common logic
        /// required to make the actual call. 
        /// </summary>
        /// <returns>tgDataRequest</returns>
        /// <seealso cref="tgDataRequest"/><seealso cref="tgDataProvider"/><seealso cref="IDataProvider"/>
        protected tgDataRequest CreateRequest()
        {
            tgDataRequest request = new tgDataRequest();
            request.Caller = this;

            tgConnection conn = this.tg.Connection;

            request.ConnectionString = conn.ConnectionString;
            request.CommandTimeout = conn.CommandTimeout;
            request.DataID = this.Meta.DataID;
            request.ProviderMetadata = this.Meta.GetProviderMetadata(conn.ProviderMetadataKey);
            request.SelectedColumns = selectedColumns;

            request.Catalog = conn.Catalog;
            request.Schema = conn.Schema;

            return request;
        }

        /// <summary>
        /// Returns the tgProviderSpecificMetadata for this Entity based on the 
        /// providerMetadataKey from the connections configuration values
        /// </summary>
        /// <returns></returns>
        private tgProviderSpecificMetadata GetProviderMetadata()
        {
            // We're on our own, use our own tgProviderSpecificMetadata
            string key = this.tg.Connection.ProviderMetadataKey;
            return this.Meta.GetProviderMetadata(key);
        }

        /// <summary>
        /// Performs a shallow clone of the Entity, only values are cloned, no
        /// hierarchical properties or collections are cloned
        /// </summary>
        /// <typeparam name="T">The Entity Type</typeparam>
        /// <returns>The newly cloned object</returns>
        virtual public T Clone<T>() where T : tgEntity, new()
        {
            T entity = new T();

            entity.currentValues = new tgSmartDictionary(currentValues);
            entity.rowState = rowState;

            if (originalValues != null)
            {
                entity.originalValues = new tgSmartDictionary(originalValues);
            }

            if (m_modifiedColumns != null)
            {
                entity.m_modifiedColumns = new List<string>(m_modifiedColumns);
            }

            return entity;
        }

        #region Debugger Display

        /// <summary>
        /// Used a a nice way to quickly see the values of an Entity, this exists only for
        /// debugging purposes and is called when you inspect an entity in the debugger.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden | DebuggerBrowsableState.Never)]
        virtual protected tgEntityDebuggerView[] Debug
        {
            get
            {
                tgEntityDebuggerView[] data = new tgEntityDebuggerView[currentValues.Count + 1];

                data[0].Property = "(RowState)";
                data[0].Data = rowState;

                string columnName = "";

                for(int index = 0; index < currentValues.Count; index++)
                {
                    columnName = currentValues.IndexToColumnName(index);

                    int i = index + 1;

                    data[i].Property = columnName;
                    data[i].Data = currentValues[columnName] == DBNull.Value ? null : currentValues[columnName];
                }

                return data;
            }
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Read-only metadata for the entity.
        /// </summary>
        /// <remarks>
        /// The sample below loops through the <see cref="tgColumnMetadataCollection"/> provided
        /// by the <see cref="IMetadata"/> interface. There is a lot of useful information here, in fact,
        /// there is enough information for EntitySpaces to build all of the dynamic sql required during
        /// operations that use dynamic sql.
        /// <code>
        /// public partial class Employees : esEmployees
        /// {
        /// 	public void CustomMethod()
        /// 	{
        /// 		foreach(tgColumnMetadata col in this.Meta.Columns)
        /// 		{
        /// 			if(col.IsInPrimaryKey)
        /// 			{
        /// 				// do something ...
        /// 			}
        /// 		}
        /// 	}
        /// }
        /// </code>
        /// </remarks>
        /// <seealso cref="tgColumnMetadata"/>
        /// <seealso cref="tgColumnMetadataCollection"/>
        /// <seealso cref="tgProviderSpecificMetadata"/>
        virtual protected IMetadata Meta
        {
            get { return null; }
        }

        /// <summary>
        /// Called to create an instance of this type of object, rarely called
        /// </summary>
        /// <returns></returns>
        virtual public tgEntity CreateInstance()
        {
            return null;
        }

        /// <summary>
        /// Used internally for binding.
        /// </summary>
        /// <returns>A listed of extended properties</returns>
        virtual protected internal List<tgPropertyDescriptor> GetLocalBindingProperties()
        {
            return null;
        }

        /// <summary>
        /// Used internally for binding.
        /// </summary>
        /// <returns>A listed of hierarchical properties or collections</returns>
        virtual protected internal List<tgPropertyDescriptor> GetHierarchicalProperties()
        {
            return new List<tgPropertyDescriptor>();
        }

        /// <summary>
        /// Called whenever the Entity needs a connection. This can be used to override the default connection 
        /// per object manually, or automatically by filling in the "Connection Name" on the "Generated Master"
        /// template. 
        /// </summary>
        /// <returns></returns>
        virtual protected string GetConnectionName()
        {
            return null;
        }

        /// <summary>
        /// Overridden in the generated classes.
        /// </summary>
        /// <param name="values"></param>
        virtual public void SetProperties(IDictionary values)
        {

        }

        /// <summary>
        /// Overridden in the generated classes.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        virtual public void SetProperty(string name, object value)
        {

        }

        /// <summary>
        /// Overridden by the Generated classes to support the DynamicQuery Prefetch logic
        /// </summary>
        /// <param name="collectionName">The string name of the collection, example, "OrdersCollectionByEmployeeID"</param>
        /// <returns>The collection of the right type, ie, "OrdersCollection"</returns>
        virtual protected tgEntityCollectionBase CreateCollectionForPrefetch(string collectionName)
        {
            return null;
        }

        /// <summary>
        /// Called when the first property is accessed or set when an entity has the
        /// RowState of Added. Use this method to initialize any properties of a newly
        /// created entity.
        /// </summary>
        virtual internal protected void ApplyDefaults()
        {

        }

        #endregion

        internal PropertyDescriptorCollection GetAllHierarchicalProperties(PropertyDescriptorCollection props)
        {
            ArrayList tempColl = new ArrayList();

            Type type = this.GetType();
            PropertyDescriptor prop = null;
            
            for (int i = 0; i < props.Count; i++)
            {
                prop = props[i];

                Type propType = prop.PropertyType;

                if (propType.IsClass && !propType.IsAbstract)
                {
                    if (propType.IsSubclassOf(typeof(tgEntityCollectionBase)))
                    {
                        tempColl.Add(prop);
                    }
                    else if (propType.IsSubclassOf(typeof(tgEntity)))
                    {
                        tempColl.Add(prop);
                    }
                }
            }

            PropertyDescriptorCollection theProps =
                new PropertyDescriptorCollection((PropertyDescriptor[])tempColl.ToArray(typeof(PropertyDescriptor)));

            return theProps;
        }

        /// <summary>
        /// Called by the EntitySpaces Proxies to determine of they are any
        /// extra column in the entity that need to be serialized
        /// </summary>
        /// <remarks>
        /// Extra columns are any columns that do not represent table columns,
        /// they could be brought back via a join or via adding extended
        /// properties directly to the entities.
        /// </remarks>
        /// <returns></returns>
        public Dictionary<string, object> GetExtraColumns()
        {
            Dictionary<string, object> extraColumns = new Dictionary<string, object>();

            if (this.currentValues != null)
            {
                tgColumnMetadataCollection cols = this.tg.Meta.Columns;

                foreach (string column in this.currentValues.Keys)
                {
                    if (cols.FindByColumnName(column) == null)
                    {
                        // Turn DBNull.Value into "null"
                        object o = this.currentValues[column];
                        extraColumns[column] = (o == DBNull.Value) ? null : o;
                    }
                }
            }

            return extraColumns;
        }

        /// <summary>
        /// Serializes the Dictionary return from GetExtraColumns() with the tgDataContractSerializer
        /// </summary>
        /// <returns>The serialized dictionary or null if there are no extra columns</returns>
        protected string GetExtraColumnsSerialized()
        {
            Dictionary<string, object> ext = this.GetExtraColumns();

            if (ext.Values.Count > 0)
            {
                return tgDataContractSerializer.ToXml(ext);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Called by the proxies
        /// </summary>
        /// <param name="extraColumns"></param>
        public void SetExtraColumns(Dictionary<string, object> extraColumns)
        {
            if (extraColumns != null)
            {
                foreach (string column in extraColumns.Keys)
                {
                    SetColumn(column, extraColumns[column], true);
                }
            }
        }

        #region INotifyPropertyChanged Members

        /// <summary>
        /// The PropertyChanged Event Handler
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                propertyChanged += value;
            }
            remove
            {
                propertyChanged -= value;
            }
        }

        [NonSerialized]
        [IgnoreDataMember]
        private PropertyChangedEventHandler propertyChanged;

        /// <summary>
        /// When a property is changed and INotifyPropertyChanged is supported this
        /// method is called
        /// </summary>
        /// <param name="propertyName"></param>
        virtual public void OnPropertyChanged(string propertyName)
        {
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Return the Collection for this entity (if it is contained in a collection)
        /// </summary>
        /// <returns>The collection, or null if not in a collection</returns>
        public tgEntityCollectionBase GetCollection()
        {
            return collection;
        }

        /// <summary>
        /// This entities collection, will be null if this entity is not in a collection.
        /// </summary>
        [XmlIgnore]
        internal tgEntityCollectionBase Collection
        {
            get
            {
                return collection;
            }

            set
            {
                collection = value;
            }
        }

        /// <summary>
        /// Indicates whether the entity is added, deleted, or modified
        /// </summary>
        public tgDataRowState RowState
        {
            get { return rowState; }
            set { rowState = value; }
        }

        /// <summary>
        /// This is used to keep certain EntitySpaces defined properties from conflicting with the
        /// generated properties
        /// </summary>
        [Browsable(false)]
        #if (!MonoTouch)
        [Bindable(false)]
        #endif
        [XmlIgnore]
        public IEntity tg
        {
            get { return this as IEntity; }
        }

        #endregion

        #region Sort

        /// <summary>
        /// This contains the default implementation for sorting, can be overloaded to provide sorting
        /// for individual or all properties
        /// </summary>
        /// <param name="other">The Entity to compare against</param>
        /// <param name="esColumn">The esColumn - will be null for extra properties</param>
        /// <param name="propertyName">The column name, will always be valid</param>
        /// <returns></returns>
        virtual public int OnSort(tgEntity other, tgColumnMetadata esColumn, string propertyName)
        {
            tgSystemType esType = tgSystemType.Unassigned;

            string columnName = string.Empty;

            if (esColumn == null)
            {
                // This code will only be executed for extra or extended properties
                object oThat = other.GetColumn(propertyName);
                object oThis = GetColumn(propertyName);

                if (oThat == null || oThis == null)
                {
                    return tgEntity.Compare(oThis, oThat);
                }

                columnName = propertyName;

                Type t = oThis.GetType();

                switch (t.Name)
                {
                    case "Boolean": esType = tgSystemType.Boolean; break;
                    case "Byte": esType = tgSystemType.Byte; break;
                    case "Char": esType = tgSystemType.Char; break;
                    case "DateTime": esType = tgSystemType.DateTime; break;
                    case "Decimal": esType = tgSystemType.Decimal; break;
                    case "Double": esType = tgSystemType.Double; break;
                    case "Guid": esType = tgSystemType.Guid; break;
                    case "Int16": esType = tgSystemType.Int16; break;
                    case "Int32": esType = tgSystemType.Int32; break;
                    case "Int64": esType = tgSystemType.Int64; break;
                    case "SByte": esType = tgSystemType.SByte; break;
                    case "Single": esType = tgSystemType.Single; break;
                    case "String": esType = tgSystemType.String; break;
                    case "TimeSpan": esType = tgSystemType.TimeSpan; break;
                    case "UInt16": esType = tgSystemType.UInt16; break;
                    case "UInt32": esType = tgSystemType.UInt32; break;
                    case "UInt64": esType = tgSystemType.UInt64; break;
                 }
            }
            else
            {
                esType = esColumn.esType;
                columnName = esColumn.Name;
            }

            switch (esType)
            {
                case tgSystemType.Boolean:
                    {
                        bool? oThat = other.GetSystemBoolean(columnName);
                        bool? oThis = GetSystemBoolean(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.Byte:
                    {
                        byte? oThat = other.GetSystemByte(columnName);
                        byte? oThis = GetSystemByte(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.Char:
                    {
                        char? oThat = other.GetSystemChar(columnName);
                        char? oThis = GetSystemChar(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.DateTime:
                    {
                        DateTime? oThat = other.GetSystemDateTime(columnName);
                        DateTime? oThis = GetSystemDateTime(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.Decimal:
                    {
                        decimal? oThat = other.GetSystemDecimal(columnName);
                        decimal? oThis = GetSystemDecimal(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.Double:
                    {
                        double? oThat = other.GetSystemDouble(columnName);
                        double? oThis = GetSystemDouble(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.Guid:
                    {
                        Guid? oThat = other.GetSystemGuid(columnName);
                        Guid? oThis = GetSystemGuid(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.Int16:
                    {
                        short? oThat = other.GetSystemInt16(columnName);
                        short? oThis = GetSystemInt16(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.Int32:
                    {
                        int? oThat = other.GetSystemInt32(columnName);
                        int? oThis = GetSystemInt32(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.Int64:
                    {
                        long? oThat = other.GetSystemInt64(columnName);
                        long? oThis = GetSystemInt64(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.SByte:
                    {
                        sbyte? oThat = other.GetSystemSByte(columnName);
                        sbyte? oThis = GetSystemSByte(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.Single:
                    {
                        float? oThat = other.GetSystemSingle(columnName);
                        float? oThis = GetSystemSingle(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.String:
                    {
                        string oThat = other.GetSystemString(columnName);
                        string oThis = GetSystemString(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.CompareTo(oThat);
                    }
                case tgSystemType.TimeSpan:
                    {
                        TimeSpan? oThat = other.GetSystemTimeSpan(columnName);
                        TimeSpan? oThis = GetSystemTimeSpan(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.UInt16:
                    {
                        ushort? oThat = other.GetSystemUInt16(columnName);
                        ushort? oThis = GetSystemUInt16(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.UInt32:
                    {
                        uint? oThat = other.GetSystemUInt32(columnName);
                        uint? oThis = GetSystemUInt16(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
                case tgSystemType.UInt64:
                    {
                        ulong? oThat = other.GetSystemUInt64(columnName);
                        ulong? oThis = GetSystemUInt64(columnName);
                        int result = tgEntity.Compare(oThis, oThat);
                        return (result != 2) ? result : oThis.Value.CompareTo(oThat.Value);
                    }
            }

            return 0;
        }

        /// <summary>
        /// Called by the OnSort Method to compare to objects of a given type, this metho
        /// doesn't really compare values, but rather handles null values only
        /// </summary>
        /// <param name="oThis"></param>
        /// <param name="oThat"></param>
        /// <returns>2 if either oThis and oThat aren't null, othewise 0, 1, or -1</returns>
        static protected int Compare(object oThis, object oThat)
        {
            if (oThis == null || oThat == null)
            {
                if (oThis == null && oThat == null) return 0;
                if (oThat == null) return 1;
                if (oThis == null) return -1;
            }

            return 2;
        }

        #endregion

        #region Entity Property Helpers

        //protected Dictionary<string, object> GetExtraColumnsArray()
        //{
        //    return this.GetExtraColumns();
        //}

        public List<string> GetCurrentListOfColumns()
        {
            List<string> columns = new List<string>();

            foreach (string column in currentValues.Keys)
            {
                columns.Add(column);
            }

            return columns;
        }		

        /// <summary>
        /// Returns true if this entity contains the column passed in
        /// </summary>
        /// <param name="columnName">The desired column name</param>
        /// <returns></returns>
        public bool ContainsColumn(string columnName)
        {
            return currentValues.ContainsKey(columnName);
        }

        /// <summary>
        /// This can be used to get the original value of a column before it was changed. This can also
        /// be used to get values of original columns after they have been deleted.
        /// </summary>
        /// <param name="columnName">The column name (not the property name)</param>
        /// <returns>The original value of the column</returns>
        public object GetOriginalColumnValue(string columnName)
        {
            if (this.originalValues != null && this.originalValues.ContainsKey(columnName))
            {
                return originalValues[columnName];
            }

            return null;
        }


        /// <summary>
        /// This call should never be used
        /// </summary>
        /// <param name="columnName">The column name (not the property name)</param>
        /// <param name="value">The value to set it to</param>
        protected void SetOriginalColumnValue(string columnName, object value)
        {
            if (this.originalValues == null)
            {
                this.originalValues = new tgSmartDictionary();
            }
            
            originalValues[columnName] = value;
        }

        /// <summary>
        /// This can be used for data binding to fields not in your strongly typed esEntity class.
        /// </summary>
        /// <remarks>
        /// This is especially useful when binding to comboboxes or listboxes. The technique shown
        /// below uses the &lt; &gt; syntax. Anything sent to the Query within the &lt; &gt; is 
        /// passed directly from the EntitySpaces data provider to the ADO.NET data provider unaltered.
        /// Also, anytime you use the &lt; &gt; you are writing provider specific code and losing out
        /// on portability, however, we realise not everyone is worried about supporting multi-database
        /// code. This &lt; &gt; syntax is not necessary to use the SpecialBinder property.
        /// </remarks>
        /// <example>
        /// <code>
        /// InvoicesCollection invColl = new InvoicesCollection(); 
        /// invColl.Query.Select(invColl.Query.CustomerID, "&lt;[City] + ', ' + [Country] as SpecialBinder&gt;"); 
        /// invColl.Query.Load(); 
        /// 
        /// this.comboBox1.DataSource = invColl; 
        ///	this.comboBox1.DisplayMember = "SpecialBinder"; 
        /// this.comboBox1.ValueMember = InvoicesMetadata.ColumnNames.CustomerID;
        /// </code>
        /// </example>
        [BrowsableAttribute(false)]
        [XmlIgnore]
        virtual public object SpecialBinder
        {
            get
            {
                if (currentValues.ContainsKey("SpecialBinder"))
                {
                    return currentValues["SpecialBinder"];
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (currentValues.ContainsKey("SpecialBinder"))
                {
                    currentValues["SpecialBinder"] = value;
                }
            }
        }

        /// <summary>
        /// This is the typeless version. This method should only be used for columns
        /// that do not have strongly typed properties. This is useful whenever you bring
        /// back extra columns by custom loading your entity.
        /// </summary>
        /// <param name="columnName">The name of the column such as "MyColumn".</param>
        /// <param name="Value">The desired value of the column.</param>
        /// <seealso cref="GetColumn"/>
        public void SetColumn(string columnName, object Value)
        {
            SetValue(columnName, Value);
        }

        /// <summary>
        /// This is the typeless version. This method should only be used for columns
        /// that do not have strongly typed properties. This is useful whenever you bring
        /// back extra columns by custom loading your entity.
        /// </summary>
        /// <param name="columnName">The name of the column such as "MyColumn".</param>
        /// <param name="Value">The desired value of the column.</param>
        /// <param name="isVirtualColumn">Set this to True if this is a virtual column.</param> 
        /// <seealso cref="GetColumn"/>
        public void SetColumn(string columnName, object Value, bool isVirtualColumn)
        {
            currentValues[columnName] = Value;

            if (!isVirtualColumn) //&& this.Meta.Columns[columnName] != null)
            {
                this.MarkFieldAsModified(columnName);
            }
        }

        /// <summary>
        /// This is the typeless accessor into the underlying DataTable. This method should only be
        /// used for columns that do not have strongly typed properties. This is useful whenever you bring
        /// back extra columns by custom loading your entity.
        /// </summary>
        /// <param name="columnName">The name of the column such as "MyColumn".</param>
        /// <returns>The value, you will have to typecast it to the proper type.</returns>
        /// <seealso cref="SetColumn"/>
        public object GetColumn(string columnName)
        {
            if (currentValues == null || currentValues.Count == 0 || !currentValues.ContainsKey(columnName) || rowState == tgDataRowState.Deleted)
            {
                if (rowState == tgDataRowState.Added && !applyDefaultsCalled)
                {
                    applyDefaultsCalled = true;
                    ApplyDefaults();
                }

                return null;
            }
            else
            {
                object o = currentValues[columnName];
                return (o == DBNull.Value) ? null : o;
            }
        }

        /// <summary>
        /// This is the typeless accessor into the underlying DataTable. This method should only be
        /// used for columns that do not have strongly typed properties. This is useful whenever you bring
        /// back extra columns by custom loading your entity.
        /// </summary>
        /// <param name="columnName">The name of the column such as "MyColumn".</param>
        /// <param name="defaultIfNull">The value to return if the column is null</param>
        /// <returns>The column value, or the value of defaultIfNull if the column is null</returns>
        public object GetColumn(string columnName, object defaultIfNull)
        {
            object temp = GetColumn(columnName);
            if (temp == null)
            {
                return defaultIfNull;
            }

            return temp;
        }

        #region Row Accessors

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.Object GetSystemObject(string columnName)
        {
            if (!FieldsExists(this)) return null;

            if (currentValues.ContainsKey(columnName))
            {
                object o = currentValues[columnName];
                return (o == DBNull.Value) ? null : (System.Object)o;
            }
            else return null;
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemObject(string columnName, System.Object data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.Int16? GetSystemInt16(string columnName)
        {
            if (!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == DBNull.Value)
                return null;
            else
                return (o is System.Int16) ? (System.Int16?)o : Convert.ToInt16(o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected System.Int16 GetSystemInt16Required(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.Int16) ? (System.Int16)o : Convert.ToInt16(o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemInt16(string columnName, System.Int16? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.Int32? GetSystemInt32(string columnName)
        {
            if (!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if(o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.Int32) ? (System.Int32?)o : Convert.ToInt32(o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected System.Int32 GetSystemInt32Required(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.Int32) ? (System.Int32)o : Convert.ToInt32(o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemInt32(string columnName, System.Int32? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.Int64? GetSystemInt64(string columnName)
        {
            if (!FieldsExists(this)) return null; ;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.Int64) ? (System.Int64?)o : Convert.ToInt64(o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected System.Int64 GetSystemInt64Required(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.Int64) ? (System.Int64)o : Convert.ToInt64(o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemInt64(string columnName, System.Int64? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        [CLSCompliant(false)]
        protected System.UInt16? GetSystemUInt16(string columnName)
        {
            if (!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.UInt16) ? (System.UInt16?)o : Convert.ToUInt16(o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        protected System.UInt16 GetSystemUInt16Required(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.UInt16) ? (System.UInt16)o : Convert.ToUInt16(o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        [CLSCompliant(false)]
        protected bool SetSystemUInt16(string columnName, System.UInt16? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        [CLSCompliant(false)]
        protected System.UInt32? GetSystemUInt32(string columnName)
        {
            if (!FieldsExists(this)) return null; ;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.UInt32) ? (System.UInt32?)o : Convert.ToUInt32(o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        protected System.UInt32 GetSystemUInt32Required(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.UInt32) ? (System.UInt32)o : Convert.ToUInt32(o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        [CLSCompliant(false)]
        protected bool SetSystemUInt32(string columnName, System.UInt32? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        [CLSCompliant(false)]
        protected System.UInt64? GetSystemUInt64(string columnName)
        {
            if (!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.UInt64) ? (System.UInt64?)o : Convert.ToUInt64(o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        protected System.UInt64 GetSystemUInt64Required(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.UInt64) ? (System.UInt64)o : Convert.ToUInt64(o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        [CLSCompliant(false)]
        protected bool SetSystemUInt64(string columnName, System.UInt64? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.Boolean? GetSystemBoolean(string columnName)
        {
            if (!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.Boolean) ? (System.Boolean?)o : Convert.ToBoolean(o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected System.Boolean GetSystemBooleanRequired(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.Boolean) ? (System.Boolean)o : Convert.ToBoolean(o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemBoolean(string columnName, System.Boolean? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.Char? GetSystemChar(string columnName)
        {
            if (!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.Char) ? (System.Char?)o : Convert.ToChar(o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected System.Char GetSystemCharRequired(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.Char) ? (System.Char)o : Convert.ToChar(o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemChar(string columnName, System.Char? data)
        {
            return SetValueChar(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.String GetSystemString(string columnName)
        {
            if(!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.String) ? (System.String)o : o.ToString();
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected System.String GetSystemStringRequired(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.String) ? (System.String)o : o.ToString();
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemString(string columnName, System.String data)
        {
            if (tgEntity.ConvertEmptyStringToNull == true && data != null && data.Length == 0)
            {
                data = null;
            }

            return SetValue(columnName, data);
        }

        /// <summary>
        /// Temporary Fix for ES2009. Converts String.Empty to a NULL 
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemString2(string columnName, System.String data)
        {
            if (data != null && data.Length == 0)
                data = null;

            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.Decimal? GetSystemDecimal(string columnName)
        {
            if (!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.Decimal) ? (System.Decimal?)o : Convert.ToDecimal(o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected System.Decimal GetSystemDecimalRequired(string columnName)
        {
            object o = null;
            currentValues.TryGetValue(columnName, out o);
            return (o is System.Decimal) ? (System.Decimal)o : Convert.ToDecimal(o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemDecimal(string columnName, System.Decimal? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.Double? GetSystemDouble(string columnName)
        {
            if (!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.Double) ? (System.Double?)o : Convert.ToDouble(o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected System.Double GetSystemDoubleRequired(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.Double) ? (System.Double)o : Convert.ToDouble(o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemDouble(string columnName, System.Double? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.DateTime? GetSystemDateTime(string columnName)
        {
            if (!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.DateTime) ? (System.DateTime?)o : Convert.ToDateTime(o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected System.DateTime GetSystemDateTimeRequired(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.DateTime) ? (System.DateTime)o : Convert.ToDateTime(o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemDateTime(string columnName, System.DateTime? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.DateTimeOffset? GetSystemDateTimeOffset(string columnName)
        {
            if (!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (System.DateTimeOffset?)o;
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected System.DateTimeOffset GetSystemDateTimeOffsetRequired(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (System.DateTimeOffset)o;
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemDateTimeOffset(string columnName, System.DateTimeOffset? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.TimeSpan? GetSystemTimeSpan(string columnName)
        {
            if (!FieldsExists(this)) return null; ;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (System.TimeSpan)o;
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected System.TimeSpan GetSystemTimeSpanRequired(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (System.TimeSpan)o;
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemTimeSpan(string columnName, System.TimeSpan? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.Byte? GetSystemByte(string columnName)
        {
            if (!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.Byte) ? (System.Byte?)o : Convert.ToByte(o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected System.Byte GetSystemByteRequired(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.Byte) ? (System.Byte)o : Convert.ToByte(o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemByte(string columnName, System.Byte? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        [CLSCompliant(false)]
        protected System.SByte? GetSystemSByte(string columnName)
        {
            if (!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.SByte) ? (System.SByte?)o : Convert.ToSByte(o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        protected System.SByte GetSystemSByteRequired(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.SByte) ? (System.SByte)o : Convert.ToSByte(o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        [CLSCompliant(false)]
        protected bool SetSystemSByte(string columnName, System.SByte? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.Guid? GetSystemGuid(string columnName)
        {
            if (!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.Guid) ? (System.Guid?)o : new Guid((string)o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected System.Guid GetSystemGuidRequired(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.Guid) ? (System.Guid)o : new Guid((string)o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemGuid(string columnName, System.Guid? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.Single? GetSystemSingle(string columnName)
        {
            if (!FieldsExists(this)) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return (o is System.Single) ? (System.Single?)o : Convert.ToSingle(o);
        }

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <returns></returns>
        protected System.Single GetSystemSingleRequired(string columnName)
        {
            object o = null;
            if (FieldsExistsRequired(this))
            {
                currentValues.TryGetValue(columnName, out o);
            }
            return (o is System.Single) ? (System.Single)o : Convert.ToSingle(o);
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="data">The value to set the column in the row</param>
        protected bool SetSystemSingle(string columnName, System.Single? data)
        {
            return SetValue(columnName, data);
        }

        /// <summary>
        /// Called by all "char" setters
        /// </summary>
        /// <param name="columnName">The name of the column to set</param>
        /// <param name="data">the value to set it to</param>
        /// <returns>True if the value has truly changed</returns>
        private bool SetValueChar(string columnName, object data)
        {
            bool changed = true;

            if (rowState == tgDataRowState.Deleted) throw new Exception("Cannot modifiy deleted records");

            try
            {
                if (currentValues == null)
                {
                    currentValues = new tgSmartDictionary();
                    CurrentValues_OnFirstAccess(currentValues);
                }

                if (currentValues.Count == 0)
                {
                    if (rowState == tgDataRowState.Added && !applyDefaultsCalled)
                    {
                        applyDefaultsCalled = true;
                        ApplyDefaults();
                    }
                }

                if (!currentValues.ContainsKey(columnName))
                {
                    currentValues[columnName] = data;
                    changed = true;

                    if (rowState == tgDataRowState.Unchanged)
                    {
                        rowState = tgDataRowState.Modified;
                    }
                }
                else
                {
                    object o = currentValues[columnName];
                    bool isNull = (o == DBNull.Value || o == null);

                    // Char type hack, ADO.NET often returns them as a string
                    if (!isNull && data.GetType().ToString() == typeof(char).ToString() && typeof(string).ToString() == o.GetType().ToString())
                    {
                        string str = o as string;

                        if (str != null && str.Length == 1)
                        {
                            o = str[0];
                        }
                    }

                    // Note that we grab this before we make the change
                    tgDataRowState state = rowState;

                    if (data == null && isNull)
                    {
                        // Nothing to do here
                    }
                    else
                    {
                        if (isNull && data != null)
                            currentValues[columnName] = data;
                        else if (data == null && !isNull)
                            currentValues[columnName] = DBNull.Value;
                        else if (!o.Equals(data))
                        {
                            this.currentValues[columnName] = data;

                            // Special logic to see if we have changed it back to it's original value, if 
                            // so we mark this column as no longer dirty, which if the only one could return
                            // the rowstate back to "Unchanged"
                            if (originalValues != null && originalValues.ContainsKey(columnName))
                            {
                                if (data == originalValues[columnName])
                                {
                                    MarkFieldAsUnchanged(columnName);
                                    return true; // it still changed but we don't want to mark it as dirty
                                }
                            }
                        }
                        else
                            changed = false;
                    }
                }

                if (changed)
                {
                    MarkFieldAsModified(columnName);
                }
            }
            finally
            {

            }

            return changed;
        }

        /// <summary>
        /// Called by all of the property setters
        /// </summary>
        /// <param name="columnName">The name of the column to set</param>
        /// <param name="data">the value to set it to</param>
        /// <returns>True if the value has truly changed</returns>
        private bool SetValue(string columnName, object data)
        {
            bool changed = true;

            if (rowState == tgDataRowState.Deleted) throw new Exception("Cannot modifiy deleted records");

            try
            {
                if (currentValues == null)
                {
                    currentValues = new tgSmartDictionary();
                    CurrentValues_OnFirstAccess(currentValues);
                }

                if (currentValues.Count == 0)
                {
                    if (rowState == tgDataRowState.Added && !applyDefaultsCalled)
                    {
                        applyDefaultsCalled = true;
                        ApplyDefaults();
                    }
                }

                if (!currentValues.ContainsKey(columnName))
                {
                    currentValues[columnName] = data;
                    changed = true;

                    if (rowState == tgDataRowState.Unchanged)
                    {
                        rowState = tgDataRowState.Modified;
                    }
                }
                else
                {
                    object o = currentValues[columnName];
                    bool isNull = (o == DBNull.Value || o == null);

                    // Note that we grab this before we make the change
                    tgDataRowState state = rowState;

                    if (data == null && isNull)
                    {
                        // Nothing to do here
                        changed = false;
                    }
                    else
                    {
                        if (isNull && data != null)
                            currentValues[columnName] = data;
                        else if (data == null && !isNull)
                            currentValues[columnName] = DBNull.Value;
                        else if (!o.Equals(data))
                        {
                            this.currentValues[columnName] = data;

                            // Special logic to see if we have changed it back to it's original value, if 
                            // so we mark this column as no longer dirty, which if the only one could return
                            // the rowstate back to "Unchanged"
                            if (originalValues != null && originalValues.ContainsKey(columnName))
                            {
                                if (data == originalValues[columnName])
                                {
                                    MarkFieldAsUnchanged(columnName);
                                    return true; // it still changed but we don't want to mark it as dirty
                                }
                            }
                        }
                        else
                            changed = false;
                    }
                }

                if (changed)
                {
                    MarkFieldAsModified(columnName);
                }
            }
            finally
            {

            }

            return changed;
        }

        static private bool FieldsExists(tgEntity entity)
        {
            if (entity.currentValues == null || entity.currentValues.Count == 0)
            {
                if (entity.rowState == tgDataRowState.Added && !entity.applyDefaultsCalled)
                {
                    entity.applyDefaultsCalled = true;
                    entity.ApplyDefaults();

                    if (entity.currentValues != null)
                        return true; 
                }

                return false;
            }

            return true;
        }

        static private bool FieldsExistsRequired(tgEntity entity)
        {
            if (entity.currentValues == null || entity.currentValues.Count == 0)
            {
                if (entity.rowState == tgDataRowState.Added && !entity.applyDefaultsCalled)
                {
                    entity.applyDefaultsCalled = true;
                    entity.ApplyDefaults();

                    if (entity.currentValues != null)
                        return true; 
                }

                return false;
            }

            return true;
        }

        #endregion

        #region Array Row Accessors

        /// <summary>
        /// This is used internally to get type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column in the row</param>
        protected System.Byte[] GetSystemByteArray(string columnName)
        {
            if (currentValues == null) return null;

            object o = null;
            currentValues.TryGetValue(columnName, out o);

            if (o == null || o == DBNull.Value)
                return null;
            else
                return o as byte[];
        }

        /// <summary>
        /// This is used internally to set type specific column values for a row.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="byteArray">The value to set the column in the row</param>
        protected bool SetSystemByteArray(string columnName, System.Byte[] byteArray)
        {
            MarkFieldAsModified(columnName);
            currentValues[columnName] = byteArray;
            return true;
        }
        #endregion

        #endregion

        #region MarkAs Methods ...

        /// <summary>
        /// Marks a column as dirty
        /// </summary>
        /// <param name="columnName">The column name to mark dirty</param>
        protected void MarkFieldAsModified(string columnName)
        {
            if (this.m_modifiedColumns == null)
            {
                m_modifiedColumns = new List<string>();
            }

            if (!m_modifiedColumns.Contains(columnName))
            {
                m_modifiedColumns.Add(columnName);

                if (rowState != tgDataRowState.Added)
                {
                    rowState = tgDataRowState.Modified;
                }
            }
        }

        /// <summary>
        /// Removes a columns dirty status
        /// </summary>
        /// <param name="columnName">The column name who status you wish to clear</param>
        protected void MarkFieldAsUnchanged(string columnName)
        {
            if (this.m_modifiedColumns == null) return;

            if (m_modifiedColumns.Contains(columnName))
            {
                m_modifiedColumns.Remove(columnName);

                if (m_modifiedColumns.Count == 0)
                {
                    AcceptChanges();
                }
            }
        }

        /// <summary>
        /// Marks all columns in the DataRow for the entity as dirty.
        /// </summary>
        /// <remarks>
        /// Below is a nifty way to use MarkAllColumnsAsDirty to read data from one database and
        /// and write it to another using the same esEntity object.
        /// <code>
        /// Customer c = new Customer(); 
        /// c.LoadByPrimaryKey(5);   // Read from Microsoft SQL via default connection
        /// c.MarkAllColumnsAsDirty(DataRowState.Added); 
        /// c.es.Connection.Name = "Oracle"; 
        /// c.Save();                // Save the same data to Oracle
        /// </code>
        /// Only DataRowSate.Added and DataRowState.Modifed are supported.
        /// Do not call this with DataRowState.Deleted. Use entity.MarkAsDeleted() instead.
        /// </remarks>
        /// <param name="state">The DataRowState enumeration to which the DataRow of the entity is set.</param>
        public void MarkAllColumnsAsDirty(tgDataRowState state)
        {
            (this as IEntity).RowState = state;

            foreach (tgColumnMetadata meta in this.Meta.Columns)
            {
                MarkFieldAsModified(meta.Name);
            }
        }

        /// <summary>
        /// Marks this entity as deleted, if saved, the entity will be deleted from the database.
        /// </summary>
        virtual public void MarkAsDeleted()
        {
            if (rowState == tgDataRowState.Deleted) return;

            if (collection != null)
            {
                IList list = collection as IList;
                if (list.Contains(this))
                {
                    list.Remove(this);
                }

                if (this.RowState != tgDataRowState.Added)
                {
                    collection.AddEntityToDeletedList(this);
                }
            }

            if (this.RowState == tgDataRowState.Added)
            {
                try
                {
                    foreach (tgColumnMetadata esCol in this.Meta.Columns)
                    {
                        if (!esCol.IsInPrimaryKey)
                        {
                            this.SetColumn(esCol.Name, null);
                        }
                    }
                }
                catch { }

                rowState = tgDataRowState.Unchanged;
            }
            else
            {
                rowState = tgDataRowState.Deleted;
            }
        }

        #endregion

        #region Accept / Reject Changes

        /// <summary>
        /// Called to accept all proposed changes and mark the dirty as Unchanged. See <see cref="RejectChanges"/> as well.
        /// </summary>
        virtual public void AcceptChanges()
        {
            if (rowState == tgDataRowState.Deleted)
            {
                currentValues = originalValues = null;
                m_modifiedColumns = null;
                rowState = tgDataRowState.Invalid;
            }
            else
            {
                originalValues = new tgSmartDictionary(currentValues);
                rowState = tgDataRowState.Unchanged;
                m_modifiedColumns = null;
            }
        }

        /// <summary>
        /// RejectChanges does just the opposite of <see cref="AcceptChanges"/>. That is, RejectChanges moves the
        /// original values back into the current values, it's as if nothing was ever changed.
        /// </summary>
        /// <seealso cref="AcceptChanges"/>
        virtual public void RejectChanges()
        {
            if (rowState == tgDataRowState.Added)
            {
                currentValues = new tgSmartDictionary(currentValues.Count);
                rowState = tgDataRowState.Unchanged;
                m_modifiedColumns = null;
            }
            else
            {
                currentValues = new tgSmartDictionary(originalValues);
                rowState = tgDataRowState.Unchanged;
                m_modifiedColumns = null;
            }
        }

        #endregion

        #region Save
        /// <summary>
        /// Saves the entity.
        /// </summary>
        /// <remarks>
        /// Save is always wrapped in a transaction. 
        /// You do not need to use transactions unless you need to Save
        /// two unrelated objects; e.g. Employees and Products.
        /// </remarks>
        /// <example>
        /// <code>
        /// Employees entity = new Employees();
        /// entity.LoadByPrimaryKey(32);
        /// entity.MarkAsDeleted();
        /// entity.Save();
        /// </code>
        /// </example>
        /// <seealso cref="MarkAsDeleted"/>
        virtual public void Save()
        {
            IEntity entity = this as IEntity;
            this.Save(entity.Connection.SqlAccessType);
        }

        /// <summary>
        /// Saves the entity using the specified SqlAccessType. This method is only used when you need
        /// to override the value set in your config file which would be rare.
        /// </summary>
        /// <remarks>
        /// Save is always wrapped in a transaction. 
        /// You do not need to use transactions unless you need to Save
        /// two unrelated objects; e.g. Employees and Products.
        /// </remarks>
        /// <example>
        /// <code>
        /// Employees entity = new Employees();
        /// entity.FirstName = this.txtFirstName.Text;
        /// entity.LastName = this.txtLastName.Text;
        /// entity.Save(tgSqlAccessType.StoredProcedure);
        /// </code>
        /// </example>
        /// <param name="sqlAccessType">See <see cref="tgSqlAccessType"/>.</param>
        virtual public void Save(tgSqlAccessType sqlAccessType)
        {
            if (!NeedsTransactionDuringSave())
            {
                // Save modified or added rows only
                if (rowState == tgDataRowState.Modified || rowState == tgDataRowState.Added)
                {
                    this.SaveToProvider(sqlAccessType);
                }

                // Save my deleted records on the way back up
                if (rowState == tgDataRowState.Deleted)
                {
                    this.SaveToProvider(sqlAccessType);
                }

                this.AcceptChanges();
            }
            else
            {
                tgTransactionScopeOption txOption =
                    tgTransactionScope.GetCurrentTransactionScopeOption() == tgTransactionScopeOption.Suppress ?
                    tgTransactionScopeOption.Suppress : tgTransactionScopeOption.Required;

                using (tgTransactionScope scope = new tgTransactionScope(txOption))
                {
                    // 1) Commit the PreSaves
                    this.CommitPreSaves();
                    this.ApplyPreSaveKeys();

                    // 2) Save me ...  (modified or added rows only)
                    if (rowState == tgDataRowState.Modified || rowState == tgDataRowState.Added)
                    {
                        this.SaveToProvider(sqlAccessType);
                    }

                    // 3) Commit the PostSaves
                    this.ApplyPostSaveKeys();
                    this.ApplyPostOneSaveKeys();
                    this.CommitPostSaves();
                    this.CommitPostOneSaves();

                    // 4) Save my deleted records on the way back up
                    if (rowState == tgDataRowState.Deleted)
                    {
                        this.SaveToProvider(sqlAccessType);
                    }

                    this.AcceptChanges();

                    scope.Complete();
                }
            }
        }

        /// <summary>
        /// Overridden in the generated class and used during the hierarchical save logic.
        /// </summary>
        /// <seealso cref="ApplyPostSaveKeys"/>
        virtual internal protected void ApplyPreSaveKeys()
        {

        }

        /// <summary>
        /// Overridden in the generated class and used during the hierarchical save logic.
        /// </summary>
        /// <seealso cref="ApplyPreSaveKeys"/> 
        virtual internal protected void ApplyPostSaveKeys()
        {

        }

        /// <summary>
        /// Overridden in the generated class and used during the hierarchical save logic.
        /// </summary>
        /// <seealso cref="ApplyPreSaveKeys"/> 
        virtual internal protected void ApplyPostOneSaveKeys()
        {

        }

        /// <summary>
        /// This method calls esSaveDataTable on the EntitySpaces DataProvider to physically save the data.
        /// </summary>
        /// <param name="sqlAccessType"></param>
        /// <seealso cref="Save"/>
        virtual protected void SaveToProvider(tgSqlAccessType sqlAccessType)
        {
            tgDataRequest request = CreateRequest();

            #region Auditing fields
            tgColumnMetadataCollection cols = this.Meta.Columns;

            if (rowState == tgDataRowState.Added)
            {
                if (cols.DateAdded != null && cols.DateAdded.Type == DateType.ClientSide)
                {
                    this.SetColumn(cols.DateAdded.ColumnName, cols.DateAdded.ClientType == ClientType.Now ? DateTime.Now : DateTime.UtcNow, false);
                }

                if (cols.AddedBy != null && cols.AddedBy.UseEventHandler && AddedByEventHandler != null)
                {
                    this.SetColumn(cols.AddedBy.ColumnName, AddedByEventHandler(), false);
                }

                if (cols.DateModified != null && cols.DateModified.Type == DateType.ClientSide)
                {
                    this.SetColumn(cols.DateModified.ColumnName, cols.DateModified.ClientType == ClientType.Now ? DateTime.Now : DateTime.UtcNow, false);
                }

                if (cols.ModifiedBy != null && cols.ModifiedBy.UseEventHandler && ModifiedByEventHandler != null)
                {
                    this.SetColumn(cols.ModifiedBy.ColumnName, ModifiedByEventHandler(), false);
                }
            }
            else if (rowState == tgDataRowState.Modified)
            {
                if (cols.DateModified != null && cols.DateModified.Type == DateType.ClientSide)
                {
                    this.SetColumn(cols.DateModified.ColumnName, cols.DateModified.ClientType == ClientType.Now ? DateTime.Now : DateTime.UtcNow, false);
                }

                if (cols.ModifiedBy != null && cols.ModifiedBy.UseEventHandler && ModifiedByEventHandler != null)
                {
                    this.SetColumn(cols.ModifiedBy.ColumnName, ModifiedByEventHandler(), false);
                }
            }
            #endregion

            request.EntitySavePacket.Entity = this;
            request.EntitySavePacket.OriginalValues = originalValues;
            request.EntitySavePacket.CurrentValues = currentValues;
            request.EntitySavePacket.RowState = rowState;
            request.EntitySavePacket.ModifiedColumns = tg.ModifiedColumns;

            request.Columns = Meta.Columns;
            request.SqlAccessType = sqlAccessType;

            tgDataProvider provider = new tgDataProvider();
            tgDataResponse response = provider.esSaveDataTable(request, tg.Connection.ProviderSignature);

            if (response.IsException)
            {
                rowError = response.Exception.Message;

                // Game over, we received an exception
                throw response.Exception;
            }
        }

        internal void PrepareSpecialFields()
        {
            tgColumnMetadataCollection cols = this.Meta.Columns;

            if (rowState == tgDataRowState.Added)
            {
                if (cols.DateAdded != null && cols.DateAdded.Type == DateType.ClientSide)
                {
                    this.SetColumn(cols.DateAdded.ColumnName, cols.DateAdded.ClientType == ClientType.Now ? DateTime.Now : DateTime.UtcNow, false);
                }

                if (cols.AddedBy != null && cols.AddedBy.UseEventHandler && AddedByEventHandler != null)
                {
                    this.SetColumn(cols.AddedBy.ColumnName, AddedByEventHandler(), false);
                }

                if (cols.DateModified != null && cols.DateModified.Type == DateType.ClientSide)
                {
                    this.SetColumn(cols.DateModified.ColumnName, cols.DateModified.ClientType == ClientType.Now ? DateTime.Now : DateTime.UtcNow, false);
                }

                if (cols.ModifiedBy != null && cols.ModifiedBy.UseEventHandler && ModifiedByEventHandler != null)
                {
                    this.SetColumn(cols.ModifiedBy.ColumnName, ModifiedByEventHandler(), false);
                }
            }
            else if (rowState == tgDataRowState.Modified)
            {
                if (cols.DateModified != null && cols.DateModified.Type == DateType.ClientSide)
                {
                    this.SetColumn(cols.DateModified.ColumnName, cols.DateModified.ClientType == ClientType.Now ? DateTime.Now : DateTime.UtcNow, false);
                }

                if (cols.ModifiedBy != null && cols.ModifiedBy.UseEventHandler && ModifiedByEventHandler != null)
                {
                    this.SetColumn(cols.ModifiedBy.ColumnName, ModifiedByEventHandler(), false);
                }
            }
        }

        #endregion

        #region Load

        /// <summary>
        /// This can be called to custom load your esEntity class. The <see cref="tgQueryType"/> provides
        /// a lot of flexibiliy allowing you load your esEntity in any way desired. 
        /// </summary>
        /// <remarks>
        /// This sample demonstrates how to load an esEntity via raw sql, however, this is not a
        /// recommended approach. It would be better to use a view or a stored procedure.
        /// </remarks>
        /// <example>
        /// <code>
        /// public partial class Employees : esEmployees
        /// {
        ///     public bool CustomLoad(string whereClause)
        ///     {
        ///			string sqlText = String.Empty;
        /// 
        ///         sqlText = "SELECT [LastName], [DepartmentID], [HireDate] ";
        ///         sqlText += "FROM [Employees] ";
        ///         sqlText += whereClause;
        /// 
        ///         return this.Load(tgQueryType.Text, sqlText);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="queryType">See <see cref="tgQueryType"/>.</param>
        /// <param name="query">Either the SQL for the Query or the name of a stored procedure.</param>
        /// <returns>True if a record was loaded.</returns>
        protected bool Load(tgQueryType queryType, string query)
        {
            return Load(queryType, query, null as tgParameters);
        }

        /// <summary>
        /// This can be called to custom load your esEntity class. The <see cref="tgQueryType"/> provides
        /// a lot of flexibiliy allowing you load your esEntity in any way desired. 
        /// </summary>
        /// <remarks>
        /// This sample demonstrates how to load an esEntity via a stored procedure.
        /// </remarks>
        /// <example>
        /// <code>
        /// public partial class Employees : esEmployees
        /// {
        ///     public bool CustomLoad()
        ///     {
        ///         // The stored procedures expects three parameters
        ///         return this.Load(tgQueryType.StoredProcedure, "sp_MyProc", "Joe", "Smith", 27.53);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="queryType">See <see cref="tgQueryType"/>.</param>
        /// <param name="query">Either the SQL for the Query or the name of a stored procedure.</param>
        /// <param name="parameters">A list of parameters. It is important that you do not tack
        /// leading decorators such as @ or ? or whatever your database might require. The EntitySpaces
        /// data providers will do this for you.</param>
        /// <returns>True if a record was loaded.</returns>
        /// <seealso cref="tgQueryType"/>
        /// <seealso cref="tgParameters"/>
        protected bool Load(tgQueryType queryType, string query, params object[] parameters)
        {
            return Load(queryType, query, PackageParameters(parameters));
        }

        /// <summary>
        /// This can be called to custom load your esEntity class. The <see cref="tgQueryType"/> provides
        /// a lot of flexibiliy allowing you load your esEntity in any way desired. 
        /// </summary>
        /// <remarks>
        /// This sample demonstrates how to load an esEntity via a stored procedure.
        /// </remarks>
        /// <example>
        /// <code>
        /// public partial class Employees : esEmployees
        /// {
        ///     public bool CustomLoad()
        ///     {
        ///         tgParameters esParams = new tgParameters();   
        ///         esParams.Add("FirstName", "Joe");   
        ///         esParams.Add("LastName", "Smith");   
        ///         esParams.Add("Salary", 27.53);   
        /// 
        ///			return this.Load(tgQueryType.StoredProcedure, "sp_MyProc", esParams);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="queryType">See <see cref="tgQueryType"/>.</param>
        /// <param name="query">Either the SQL for the Query or the name of a stored procedure.</param>
        /// <param name="parms">A list of parameters. See <see cref="tgParameters"/>.</param>
        /// <returns>True if a record was loaded.</returns>
        /// <seealso cref="tgQueryType"/>
        /// <seealso cref="tgParameters"/>/// 
        protected bool Load(tgQueryType queryType, string query, tgParameters parms)
        {
            tgDataRequest request = this.CreateRequest();

            request.Parameters = parms;
            request.QueryText = query;
            request.QueryType = queryType;

            tgConnection conn = this.tg.Connection;

            tgDataProvider provider = new tgDataProvider();
            tgDataResponse response = provider.esLoadDataTable(request, conn.ProviderSignature);

            return this.PopulateEntity(response.Table);
        }


        /// <summary>
        /// Called internally to bind the esEntity to its respective DataRow.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the entity contains more than 1 row.
        /// </remarks>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        /// <param name="table">The name of the table.</param>
        /// <returns>True, if the entity was loaded.</returns>
        /// <seealso cref="HasData"/>
        protected bool PopulateEntity(DataTable table)
        {
            bool found = false;

            int count = table.Rows.Count;

            if (count > 1)
            {
                throw new Exception("An Entity can only hold 1 record of data");
            }

            if (count == 1)
            {
                tgColumnMetadataCollection esCols = Meta.Columns;
                tgColumnMetadata esCol;
                string columnName;
                selectedColumns = new Dictionary<string, int>();

                // Now let's actually created an esEntity for each DataRow and thereby populate
                // the collection
                DataRow row = table.Rows[0];
                DataColumnCollection cols = table.Columns;

                Dictionary<string, int> ordinals = new Dictionary<string, int>(cols.Count);
                foreach (DataColumn col in cols)
                {
                    // Let's make sure we use the Case in the Metadata Class, if they return "employeeid" in a proc
                    // this little trick will make sure we use "EmployeeId" for our property accessors
                    esCol = esCols.FindByColumnName(col.ColumnName);
                    columnName = esCol != null ? esCol.Name : col.ColumnName;

                    ordinals[columnName] = col.Ordinal;

                    if (esCol != null)
                    {
                        selectedColumns[columnName] = 0;
                    }
                }

                object[] values = row.ItemArray;

                currentValues = new tgSmartDictionary(ordinals, values);
                originalValues = new tgSmartDictionary(ordinals, values, true);
                if (m_modifiedColumns != null) m_modifiedColumns = null;
                rowState = tgDataRowState.Unchanged;

                found = true;
            }

            return found;
        }

        #endregion

        #region ExecuteNonQuery

        /// <summary>
        /// ExecuteNonQuery does not return any data. This method can be used to execute any SQL
        /// statement that does not return any data. You cannot populate your esEntity with 
        /// this method.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteNonQuery.
        /// </remarks>
        /// <example>
        /// <code>
        /// public partial class Employees : esEmployees
        /// {
        ///     public int CustomUpdate(string newName, int empID)
        ///     {
        ///			string sqlText = String.Empty;
        /// 
        ///         sqlText = "UPDATE [Employees] ";
        ///         sqlText += "SET [LastName] = '" + newName + "' ";
        ///         sqlText += "WHERE [EmployeeID] = " + empID;
        /// 
        ///         return this.ExecuteNonQuery(tgQueryType.Text, sqlText);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="queryType">See <see cref="tgQueryType"/>.</param>
        /// <param name="query">Either the SQL for the Query or the name of a stored procedure.</param>
        /// <returns>The result of ExecuteNonQuery.</returns>
        /// <seealso cref="tgQueryType"/>
        protected int ExecuteNonQuery(tgQueryType queryType, string query)
        {
            return ExecuteNonQuery(queryType, query, null as tgParameters);
        }

        /// <summary>
        /// ExecuteNonQuery does not return any data. This method can be used to execute any SQL
        /// statement that does not return any data. You cannot populate your esEntity with 
        /// this method.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteNonQuery.
        /// </remarks>
        /// <example>
        /// <code>
        /// public partial class Employees : esEmployees
        /// {
        ///     public int CustomUpdate(string newName, int empID)
        ///     {
        ///			string sqlText = String.Empty;
        /// 
        ///         sqlText = "UPDATE [Employees] ";
        ///         sqlText += "SET [LastName] = {0} ";
        ///         sqlText += "WHERE [EmployeeID] = {1}";
        /// 
        ///         return this.ExecuteNonQuery(tgQueryType.Text, sqlText, newName, empID);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="queryType">See <see cref="tgQueryType"/>.</param>
        /// <param name="query">Either the SQL for the Query or the name of a stored procedure.</param>
        /// <param name="parameters">A list of parameters.</param>
        /// <returns>The result of ExecuteNonQuery.</returns>
        protected int ExecuteNonQuery(tgQueryType queryType, string query, params object[] parameters)
        {
            return ExecuteNonQuery(queryType, query, PackageParameters(parameters));
        }

        /// <summary>
        /// ExecuteNonQuery does not return any data. This method can be used to execute any SQL
        /// statement that does not return any data. You cannot populate your esEntity with 
        /// this method.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteNonQuery.
        /// </remarks>
        /// <example>
        /// <code>
        /// public partial class Employees : esEmployees
        /// {
        ///     public int CustomUpdate(string newName)
        ///     {
        ///			string sqlText = String.Empty;
        ///			tgParameters esParams = new tgParameters();
        ///			esParams.Add("FirstName", newName);
        ///			esParams.Add("LastName", "Doe");
        ///			esParams.Add("Salary", 27.53);
        /// 
        ///         sqlText = "UPDATE [Employees] ";
        ///			sqlText += "SET [FirstName] =  @FirstName ";
        ///			sqlText += "WHERE [LastName] = @LastName ";
        ///			sqlText += "AND [Salary] = @Salary";
        /// 
        ///         return this.ExecuteNonQuery(tgQueryType.Text, sqlText, esParams);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="queryType">See <see cref="tgQueryType"/>.</param>
        /// <param name="query">Either the SQL for the Query or the name of a stored procedure.</param>
        /// <param name="parms">A list of parameters. See <see cref="tgParameters"/>.</param>
        /// <returns>The result of ExecuteNonQuery.</returns>
        protected int ExecuteNonQuery(tgQueryType queryType, string query, tgParameters parms)
        {
            tgDataRequest request = this.CreateRequest();

            request.Parameters = parms;
            request.QueryText = query;
            request.QueryType = queryType;

            tgDataProvider provider = new tgDataProvider();
            tgDataResponse response = provider.ExecuteNonQuery(request, this.tg.Connection.ProviderSignature);

            return response.RowsEffected;
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteNonQuery.
        /// </remarks>
        /// <example>
        /// <code>
        /// public partial class Employees : esEmployees
        /// {
        ///     public int CustomUpdate()
        ///     {
        ///			return this.ExecuteNonQuery(this.es.Schema, "MyStoredProc");
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="schema">See <see cref="IEntity.Schema"/>.</param>
        /// <param name="storedProcedure">The name of a stored procedure.</param>
        /// <returns>The result of ExecuteNonQuery.</returns>
        protected int ExecuteNonQuery(string schema, string storedProcedure)
        {
            return ExecuteNonQuery(schema, storedProcedure, null as tgParameters);
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteNonQuery.
        /// </remarks>
        /// <example>
        /// <code>
        /// public partial class Employees : esEmployees
        /// {
        ///     public int CustomUpdate(string newName, int empID)
        ///     {
        ///			return this.ExecuteNonQuery(this.es.Schema, "MyStoredProc", newName, empID);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="schema">See <see cref="IEntity.Schema"/>.</param>
        /// <param name="storedProcedure">The name of a stored procedure.</param>
        /// <param name="parameters">A list of parameters.</param>
        /// <returns>The result of ExecuteNonQuery.</returns>
        protected int ExecuteNonQuery(string schema, string storedProcedure, params object[] parameters)
        {
            return ExecuteNonQuery(schema, storedProcedure, PackageParameters(parameters));
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteNonQuery.
        /// </remarks>
        /// <example>
        /// <code>
        /// public partial class Employees : esEmployees
        /// {
        ///     public int CustomUpdate(string newName)
        ///     {
        ///			tgParameters esParams = new tgParameters();
        ///			esParams.Add("FirstName", newName);
        ///			esParams.Add("LastName", "Doe");
        ///			esParams.Add("Salary", 27.53);
        /// 
        ///			return this.ExecuteNonQuery(this.es.Schema, "MyStoredProc", esParams);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="schema">See <see cref="IEntity.Schema"/>.</param>
        /// <param name="storedProcedure">The name of a stored procedure.</param>
        /// <param name="parameters">A list of parameters. See <see cref="tgParameters"/>.</param>
        /// <returns>The result of ExecuteNonQuery.</returns>
        protected int ExecuteNonQuery(string schema, string storedProcedure, tgParameters parameters)
        {
            tgDataRequest request = this.CreateRequest();

            request.Parameters = parameters;
            request.Schema = schema;
            request.QueryText = storedProcedure;
            request.QueryType = tgQueryType.StoredProcedure;

            tgDataProvider provider = new tgDataProvider();
            tgDataResponse response = provider.ExecuteNonQuery(request, this.tg.Connection.ProviderSignature);

            return response.RowsEffected;
        }

        #endregion

        #region ExecuteReader

        /// <summary>
        /// Can be called by a method in your Custom esEntity or tgEntityCollection. This does not populate
        /// your entity with data, for that see <see cref="esLoadData"/>
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for SqlCommand.ExecuteReader. 
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples, overloads, and parameters.
        /// </example>
        /// <returns>The IDataReader</returns>
        protected IDataReader ExecuteReader(tgQueryType queryType, string query)
        {
            return ExecuteReader(queryType, query, null as tgParameters);
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteReader.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteReader.</returns>
        protected IDataReader ExecuteReader(tgQueryType queryType, string query, params object[] parameters)
        {
            return ExecuteReader(queryType, query, PackageParameters(parameters));
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteReader.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// <code>
        /// public MyCollection : esMyCollection
        /// {
        /// 	public IDataReader CustomExecuteReader()
        /// 	{
        /// 		// The "proc_GetByLastName" stored procedure requires a LastName parameter. Notice we do not
        /// 		// use @, or :, or ? or any type of prefix, the EntitySpaces DataProvider
        /// 		// knows what kind of prefix to use thereby allowing you to write database
        /// 		// independent code even when accessing custom stored procedures.
        /// 		tgParameters esParams = new tgParameters();			
        /// 		esParams.Add("LastName", "Doe");
        /// 			
        /// 		return this.ExecuteReader(tgQueryType.StoredProcedure, "proc_GetByLastName", esParams);
        ///		}
        /// }
        /// </code> 
        /// </example>
        /// <returns>The result of ExecuteReader.</returns>
        protected IDataReader ExecuteReader(tgQueryType queryType, string query, tgParameters parms)
        {
            tgDataRequest request = this.CreateRequest();

            request.Parameters = parms;
            request.QueryText = query;
            request.QueryType = queryType;

            tgDataProvider provider = new tgDataProvider();
            tgDataResponse response = provider.ExecuteReader(request, this.tg.Connection.ProviderSignature);

            return response.DataReader;
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteReader.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteReader.</returns>
        protected IDataReader ExecuteReader(string schema, string storedProcedure)
        {
            return ExecuteReader(schema, storedProcedure, null as tgParameters);
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteReader.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteReader.</returns>
        protected IDataReader ExecuteReader(string schema, string storedProcedure, params object[] parameters)
        {
            return ExecuteReader(schema, storedProcedure, PackageParameters(parameters));
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteReader.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteReader.</returns>
        protected IDataReader ExecuteReader(string schema, string storedProcedure, tgParameters parameters)
        {
            tgDataRequest request = this.CreateRequest();

            request.Parameters = parameters;
            request.Schema = schema;
            request.QueryText = storedProcedure;
            request.QueryType = tgQueryType.StoredProcedure;

            tgDataProvider provider = new tgDataProvider();
            tgDataResponse response = provider.ExecuteReader(request, this.tg.Connection.ProviderSignature);

            return response.DataReader;
        }

        #endregion

        #region ExecuteScalar

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteScalar.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteScalar.</returns>
        protected object ExecuteScalar(tgQueryType queryType, string query)
        {
            return ExecuteScalar(queryType, query, null as tgParameters);
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteScalar.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteScalar.</returns>
        protected object ExecuteScalar(tgQueryType queryType, string query, params object[] parameters)
        {
            return ExecuteScalar(queryType, query, PackageParameters(parameters));
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteScalar.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteScalar.</returns>
        protected object ExecuteScalar(tgQueryType queryType, string query, tgParameters parms)
        {
            tgDataRequest request = this.CreateRequest();

            request.Parameters = parms;
            request.QueryText = query;
            request.QueryType = queryType;

            tgDataProvider provider = new tgDataProvider();
            tgDataResponse response = provider.ExecuteScalar(request, this.tg.Connection.ProviderSignature);

            return response.Scalar;
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteScalar.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteScalar.</returns>
        protected object ExecuteScalar(string schema, string storedProcedure)
        {
            return ExecuteScalar(schema, storedProcedure, null as tgParameters);
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteScalar.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteScalar.</returns>
        protected object ExecuteScalar(string schema, string storedProcedure, params object[] parameters)
        {
            return ExecuteScalar(schema, storedProcedure, PackageParameters(parameters));
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteScalar.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteScalar.</returns>
        protected object ExecuteScalar(string schema, string storedProcedure, tgParameters parameters)
        {
            tgDataRequest request = this.CreateRequest();

            request.Parameters = parameters;
            request.Schema = schema;
            request.QueryText = storedProcedure;
            request.QueryType = tgQueryType.StoredProcedure;

            tgDataProvider provider = new tgDataProvider();
            tgDataResponse response = provider.ExecuteScalar(request, this.tg.Connection.ProviderSignature);

            return response.Scalar;
        }

        #endregion

        #region ExecuteScalar<T>

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteScalar.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteScalar.</returns>
        protected T ExecuteScalar<T>(tgQueryType queryType, string query)
        {
            return (T)ExecuteScalar<T>(queryType, query, null as tgParameters);
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteScalar.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteScalar.</returns>
        protected T ExecuteScalar<T>(tgQueryType queryType, string query, params object[] parameters)
        {
            return (T)ExecuteScalar<T>(queryType, query, PackageParameters(parameters));
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteScalar.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteScalar.</returns>
        protected T ExecuteScalar<T>(tgQueryType queryType, string query, tgParameters parms)
        {
            tgDataRequest request = this.CreateRequest();

            request.Parameters = parms;
            request.QueryText = query;
            request.QueryType = queryType;

            tgDataProvider provider = new tgDataProvider();
            tgDataResponse response = provider.ExecuteScalar(request, this.tg.Connection.ProviderSignature);

            if (response.Scalar == DBNull.Value)
            {
                response.Scalar = null;
            }

            return (T)response.Scalar;
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteScalar.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteScalar.</returns>
        protected T ExecuteScalar<T>(string schema, string storedProcedure)
        {
            return (T)ExecuteScalar<T>(schema, storedProcedure, null as tgParameters);
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteScalar.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteScalar.</returns>
        protected T ExecuteScalar<T>(string schema, string storedProcedure, params object[] parameters)
        {
            return (T)ExecuteScalar<T>(schema, storedProcedure, PackageParameters(parameters));
        }

        /// <summary>
        /// Can be called by a method in your Custom entity.
        /// This does not populate your entity.
        /// </summary>
        /// <remarks>
        /// See the .NET documentation for ExecuteScalar.
        /// </remarks>
        /// <example>
        /// See <see cref="ExecuteNonQuery"/> for examples,
        /// overloads, and parameters.
        /// </example>
        /// <returns>The result of ExecuteScalar.</returns>
        protected T ExecuteScalar<T>(string schema, string storedProcedure, tgParameters parameters)
        {
            tgDataRequest request = this.CreateRequest();

            request.Parameters = parameters;
            request.Schema = schema;
            request.QueryText = storedProcedure;
            request.QueryType = tgQueryType.StoredProcedure;

            tgDataProvider provider = new tgDataProvider();
            tgDataResponse response = provider.ExecuteScalar(request, this.tg.Connection.ProviderSignature);

            if (response.Scalar == DBNull.Value)
            {
                response.Scalar = null;
            }

            return (T)response.Scalar;
        }

        #endregion

        #region Graph Operations

        #region AcceptChangesGraph()

        /// <summary>
        /// Will call AcceptChanges on the entire object graph.
        /// </summary>
        public void AcceptChangesGraph()
        {
            tgVisitor.Visit(this, AcceptChangesGraphCallback);
        }

        private bool AcceptChangesGraphCallback(tgVisitParameters p)
        {
            if (p.Node.NodeType == tgVisitableNodeType.Entity)
            {
                if (p.Node.Entity.GetCollection() == null)
                {
                    p.Node.Entity.AcceptChanges();
                }
            }
            else
            {
                p.Node.Collection.AcceptChanges();
            }

            return true;
        }

        #endregion

        #region RejectChangesGraph()

        /// <summary>
        /// Will call RejectChanges() on the entire object graph.
        /// </summary>
        public void RejectChangesGraph()
        {
            tgVisitor.Visit(this, RejectChangesGraphCallback);
        }

        private bool RejectChangesGraphCallback(tgVisitParameters p)
        {
            if (p.Node.NodeType == tgVisitableNodeType.Entity)
            {
                if (p.Node.Entity.GetCollection() == null)
                {
                    p.Node.Entity.RejectChanges();
                }
            }
            else
            {
                p.Node.Collection.RejectChanges();
            }

            return true;
        }

        #endregion

        #region PruneGraph()

        /// <summary>
        /// Will eliminate all objects that are not modified from the object graph. If an object
        /// is not dirty but has children that are then it remains in the graph.
        /// </summary>
        public void PruneGraph()
        {
            tgVisitor.Visit(this, PruneGraphEnterCallback, PruneGraphExitCallback);
        }

        /// <summary>
        /// Will eliminate all objects that have the state or state(s) that you pass in. If you want to
        /// Prune all unmodified objects then PruneGraph() with no parameters, it's faster.
        /// </summary>
        /// <param name="statesToPrune">The states you wish to prune, can be many such as PruneGraph(tgDataRowState.Modified | tgDataRowState.Deleted)</param>
        public void PruneGraph(tgDataRowState statesToPrune)
        {
            tgVisitor.Visit(this, PruneGraphWithStateEnterCallback, PruneGraphWithStatExitCallback, statesToPrune);
        }

        private bool PruneGraphEnterCallback(tgVisitParameters p)
        {
            if (p.Node.NodeType == tgVisitableNodeType.Collection)
            {
                p.Node.UserState = new List<tgEntity>();
            }

            if (p.Node.NodeType == tgVisitableNodeType.Entity)
            {
                if (p.Node.Entity.GetCollection() == null)
                {
                    if (!p.Node.Entity.tg.IsDirty && !p.Node.Entity.tg.IsGraphDirty)
                    {
                        p.Node.SetValueToNull(p.Parent.Obj);
                    }
                }
                else if (!p.Node.Entity.tg.IsDirty && !p.Node.Entity.tg.IsGraphDirty)
                {
                    List<tgEntity> list = p.Parent.UserState as List<tgEntity>;
                    list.Add(p.Node.Entity);
                }
            }
            else
            {
                p.Node.UserState = new List<tgEntity>();

                if (!p.Node.Collection.IsDirty && !p.Node.Collection.IsGraphDirty)
                {
                    p.Node.SetValueToNull(p.Parent.Obj);
                }
            }

            return true;
        }

        private bool PruneGraphExitCallback(tgVisitParameters p)
        {
            if (p.Node.NodeType == tgVisitableNodeType.Collection)
            {
                tgEntityCollectionBase coll = p.Node.Collection as tgEntityCollectionBase;

                List<tgEntity> list = p.Node.UserState as List<tgEntity>;

                foreach (tgEntity entity in list)
                {
                    coll.RemoveEntity(entity);
                }

                if (coll.Count == 0 && !coll.Count.Equals(p.Root))
                {
                    p.Node.SetValueToNull(p.Parent.Obj);
                }
            }

            return true;
        }

        private bool PruneGraphWithStateEnterCallback(tgVisitParameters p)
        {
            if (p.Node.NodeType == tgVisitableNodeType.Collection)
            {
                p.Node.UserState = new List<tgEntity>();
            }

            if (p.Node.NodeType == tgVisitableNodeType.Entity)
            {
                if (p.Node.Entity.GetCollection() == null)
                {
                    if (MatchesState(p.Node.Entity.tg.RowState, (tgDataRowState)p.UserState))
                    {
                        p.Node.SetValueToNull(p.Parent.Obj);
                    }
                }
                else if (MatchesState(p.Node.Entity.tg.RowState, (tgDataRowState)p.UserState))
                {
                    List<tgEntity> list = p.Parent.UserState as List<tgEntity>;
                    list.Add(p.Node.Entity);
                }
            }
            else
            {
                p.Node.UserState = new List<tgEntity>();

                if (MatchesState(tgDataRowState.Deleted, (tgDataRowState)p.UserState))
                {
                    p.Node.Collection.ClearDeletedEntries();
                }

                bool canSetToNull = true;
                foreach (tgEntity entity in p.Node.Collection)
                {
                    if (!MatchesState(entity.tg.RowState, (tgDataRowState)p.UserState))
                    {
                        canSetToNull = false;
                        break;
                    }
                }

                if (canSetToNull)
                {
                    p.Node.SetValueToNull(p.Parent.Obj);
                }
            }

            return true;
        }

        private bool PruneGraphWithStatExitCallback(tgVisitParameters p)
        {
            if (p.Node.NodeType == tgVisitableNodeType.Collection)
            {
                tgEntityCollectionBase coll = p.Node.Collection as tgEntityCollectionBase;

                List<tgEntity> list = p.Node.UserState as List<tgEntity>;

                foreach (tgEntity entity in list)
                {
                    coll.RemoveEntity(entity);
                }

                if (coll.Count == 0 && !coll.Count.Equals(p.Root))
                {
                    p.Node.SetValueToNull(p.Parent.Obj);
                }
            }

            return true;
        }

        static private bool MatchesState(tgDataRowState theState, tgDataRowState statesToPrune)
        {
            return (theState == (statesToPrune & theState)) ? true : false;
        }

        #endregion

        #endregion

        #region IEditableObject Members

        /// <summary>
        /// This method backs up all the values so that at anytime during edit that <see cref="CancelEdit"/> is called
        /// the values can be restored. <see cref="EndEdit"/> makes the proposed values the current values.
        /// </summary>
        void IEditableObject.BeginEdit()
        {
            if (isInEditMode) return;

            isInEditMode = true;

            backupValues = currentValues;
            currentValues = proposedValues = new tgSmartDictionary(currentValues);

            originalRowState = rowState;
            originalModifiedColumns = m_modifiedColumns;
        }

        /// <summary>
        /// Restored the original values that were captured during <see cref="BeginEdit"/> when the editing began.
        /// <see cref="EndEdit"/> makes the proposed values the current values.
        /// </summary>
        void IEditableObject.CancelEdit()
        {
            if (!isInEditMode) return;

            isInEditMode = false;

            currentValues = backupValues;
            proposedValues = backupValues = null;
            rowState = originalRowState;
            m_modifiedColumns = originalModifiedColumns;
        }

        /// <summary>
        /// When EndEdit is called the proposed values become the current values. 
        /// </summary>
        /// <seealso cref="BeginEdit"/><seealso cref="CancelEdit"/>
        void IEditableObject.EndEdit()
        {
            if (!isInEditMode) return;

            isInEditMode = false;

            currentValues = proposedValues;
            originalRowState = tgDataRowState.Invalid;
            proposedValues = backupValues = null;
            originalModifiedColumns = null;
        }

        #endregion

        #region IEntity Members

        /// <summary>
        /// Allows you access the Connection information that is contained in your config file or that was
        /// setup via the configless methods.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        tgConnection IEntity.Connection
        {
            get
            {
                if (this.connection == null)
                {
                    this.connection = new tgConnection();

                    if (tgConnection.ConnectionService != null)
                    {
                        this.connection.Name = tgConnection.ConnectionService.GetName();
                    }
                    else
                    {
                        // Make it so if they access this they get the 
                        // Collections Connection info
                        if (this.Collection != null)
                        {
                            IEntityCollection ec = this.Collection as IEntityCollection;
                            this.connection.Name = ec.Connection.Name;
                        }
                        else
                        {
                            string connName = this.GetConnectionName();
                            if (connName != null)
                            {
                                this.connection.Name = connName;
                            }
                        }
                    }
                }
                return this.connection;
            }
            set { this.connection = value; }
        }

        /// <summary>
        /// Contains a list of modified columns
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        List<string> IEntity.ModifiedColumns
        {
            get
            {
                if (this.m_modifiedColumns == null)
                {
                    this.m_modifiedColumns = new List<string>();
                }

                return m_modifiedColumns;
            }
        }

        /// <summary>
        /// The Catalog of this entity
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string IEntity.Catalog
        {
            get
            {
                if (this.tg.Connection.Catalog != null)
                    return this.tg.Connection.Catalog;
                else
                    return this.GetProviderMetadata().Catalog;
            }
        }

        /// <summary>
        /// The Schema of this entity
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string IEntity.Schema
        {
            get
            {
                if (this.tg.Connection.Schema != null)
                    return this.tg.Connection.Schema;
                else
                    return this.GetProviderMetadata().Schema;
            }
        }

        /// <summary>
        /// The Destination of this entity. This is the Table this entity is saved to
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string IEntity.Destination
        {
            get { return this.GetProviderMetadata().Destination; }
        }

        /// <summary>
        /// The source of this entity
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string IEntity.Source
        {
            get { return this.GetProviderMetadata().Source; }
        }

        /// <summary>
        /// The INSERT stored procedure name
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string IEntity.spInsert
        {
            get { return this.GetProviderMetadata().spInsert; }
        }

        /// <summary>
        /// The UPDATE stored procedure name
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string IEntity.spUpdate
        {
            get { return this.GetProviderMetadata().spUpdate; }
        }

        /// <summary>
        /// The DELETE stored procedure name
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string IEntity.spDelete
        {
            get { return this.GetProviderMetadata().spDelete; }
        }

        /// <summary>
        /// The LoadAll stored procedure name
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string IEntity.spLoadAll
        {
            get { return this.GetProviderMetadata().spLoadAll; }
        }

        /// <summary>
        /// The LoadByPrimaryKey stored procedure name
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string IEntity.spLoadByPrimaryKey
        {
            get { return this.GetProviderMetadata().spLoadByPrimaryKey; }
        }

        /// <summary>
        /// This method returns true if the esEntity has been loaded with data and the esEntity is not
        /// marked as Deleted. Remember, an esEntity can only represent a single row. However the full Query
        /// syntax is allowed to load an esEntity. See <see cref="tgDynamicQuery"/>
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool IEntity.HasData
        {
            get
            {
                if ((currentValues.Count > 0 && tg.RowState != tgDataRowState.Deleted) || collection != null)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// This is set to true if it is a new entity, MarkAsDeleted(), or if any of the esEntities table based properties 
        /// have been changed. After a successful call to <see cref="Save"/> IsDirty will report false.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool IEntity.IsDirty
        {
            get
            {
                switch (rowState)
                {
                    case tgDataRowState.Added:
                        return true;

                    case tgDataRowState.Modified:
                        return true;

                    case tgDataRowState.Deleted:
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Returns true if any entity or collection in the object graph returns true from thier 
        /// respect IsDirty property.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool IEntity.IsGraphDirty
        {
            get
            {
                if (this.tg.IsDirty) return true; 

                return !tgVisitor.Visit(this, IsGraphDirtyCallback);
            }
        }

        private bool IsGraphDirtyCallback(tgVisitParameters p)
        {
            bool isClean = true;

            if (p.Node.NodeType == tgVisitableNodeType.Entity)
            {
                if (p.Node.Entity.GetCollection() == null)
                {
                    isClean = !p.Node.Entity.tg.IsDirty;
                }
            }
            else
            {
                isClean = !p.Node.Collection.IsDirty;
            }

            if (!isClean)
            {
                p.ProcessChildren = false;
            }

            return isClean;
        }

        /// <summary>
        /// Returns true of the RowState is tgDataRowState.Modified
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool IEntity.IsModified
        {
            get
            {
                return rowState == tgDataRowState.Modified;
             }
        }

        /// <summary>
        /// Returns true of the RowState is tgDataRowState.Added
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool IEntity.IsAdded
        {
            get
            {
                return rowState == tgDataRowState.Added;
            }
        }

        /// <summary>
        /// Returns true of the RowState is tgDataRowState.Deleted
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool IEntity.IsDeleted
        {
            get
            {
                return rowState == tgDataRowState.Deleted;
            }
        }

        /// <summary>
        /// See the ADO.NET tgDataRowState enum for more information. 
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        tgDataRowState IEntity.RowState
        {
            get
            {
                return rowState;
            }

            set
            {
                switch (value)
                {
                    case tgDataRowState.Added:
                        rowState = value;
                        break;

                    case tgDataRowState.Deleted:
                        throw new Exception("Call instead esEntity.MarkAsDeleted()");

                    case tgDataRowState.Modified:
                        rowState = value;
                        break;

                    case tgDataRowState.Unchanged:
                        throw new Exception("Call instead AcceptChanges()");
                }
            }
        }

        /// <summary>
        /// See the ADO.NET DataRow.RowError for more information.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string IEntity.RowError
        {
            get
            {
                return rowError;
            }
        }

        /// <summary>
        /// Allow you to gain access to the on-board metadata
        /// </summary>
        IMetadata IEntity.Meta
        {
            get { return this.Meta; }
        }

        /// <summary>
        /// If true, disables lazy loading for all hierarchical properties
        /// </summary>
        bool IEntity.IsLazyLoadDisabled
        {
            get { return this._isLazyLoadDisabled; }
            set { this._isLazyLoadDisabled = value; }
        }

        /// <summary>
        /// Called by EntitySpaces during the DynamicQuery Prefetch logic
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        tgEntityCollectionBase IEntity.CreateCollection(string name)
        {
            return this.CreateCollectionForPrefetch(name);
        }

        #endregion

        #region ICommittable Members

        bool ICommittable.Commit()
        {
            AcceptChanges();
            return true;
        }

        #endregion

        #region ICustomTypeDescriptor Members

        // We need this in order for extra columns to bind in the web, however, if I recall correctly this breaks
        // Windows.Forms binding

        #if (WebBinding)

        System.ComponentModel.AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return new System.ComponentModel.AttributeCollection(null);
        }

        string ICustomTypeDescriptor.GetClassName()
        {
            return null;
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return null;
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return null;
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return null;
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return null;
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return null;
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return new EventDescriptorCollection(null);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return new EventDescriptorCollection(null);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            if (this.Collection == null)
            {
                return null;
            }

            ITypedList tl = this.Collection as ITypedList;
            return tl.GetItemProperties(null);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            if (this.Collection == null)
            {
                return null;
            }

            ITypedList tl = this.Collection as ITypedList;
            return tl.GetItemProperties(null);
        }

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }
#endif

        #endregion

        #region Hierarchical Support

        /// <summary>
        /// True if an tgTransactionScope is needed during Save due to hierarchical properties of this entity being dirty.
        /// EntitySpaces will automatically create the tgTransactionScope transaction if true.
        /// </summary>
        /// <returns></returns>
        private bool NeedsTransactionDuringSave()
        {
            bool needsTx = this.preSaves != null && this.preSaves.Count > 0;

            needsTx |= this.postSaves != null && this.postSaves.Count > 0;
            needsTx |= this.postOneSaves != null && this.postOneSaves.Count > 0;

            return needsTx;
        }

        #region PreSaves
        /// <summary>
        /// Called internally by EntitySpaces as part of the Hierarchical logic.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected tgEntity GetPreSave(string name)
        {
            if (preSaves != null)
            {
                if (preSaves.ContainsKey(name))
                {
                    return preSaves[name];
                }
            }

            return null;
        }

        /// <summary>
        /// Called internally by EntitySpaces as part of the Hierarchical logic.
        /// </summary>
        /// <param name="name"></param>
        protected void RemovePreSave(string name)
        {
            if (preSaves != null)
            {
                if (preSaves.ContainsKey(name))
                {
                    preSaves.Remove(name);
                }
            }
        }

        /// <summary>
        /// Called internally by EntitySpaces as part of the Hierarchical logic.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="entity"></param>
        protected void SetPreSave(string name, tgEntity entity)
        {
            if (preSaves == null)
            {
                preSaves = new Dictionary<string, tgEntity>();
            }

            preSaves[name] = entity;
        }

        /// <summary>
        /// Called internally by EntitySpaces, derived classes generated in conjuction with the Hierarchical
        /// functionality implement this to Save nested sub-objects that need to be saved before the esEntity
        /// itself is saved.
        /// </summary>
        internal protected void CommitPreSaves()
        {
            if (this.preSaves != null)
            {
                foreach (tgEntity entity in this.preSaves.Values)
                {
                    entity.Save();
                }
            }
        }
        #endregion

        #region PostSaves
        /// <summary>
        /// Called internally by EntitySpaces as part of the Hierarchical logic.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected tgEntityCollectionBase GetPostSave(string name)
        {
            if (postSaves != null)
            {
                if (postSaves.ContainsKey(name))
                {
                    return postSaves[name];
                }
            }

            return null;
        }

        /// <summary>
        /// Called internally by EntitySpaces as part of the Hierarchical logic.
        /// </summary>
        /// <param name="name"></param>
        protected void RemovePostSave(string name)
        {
            if (postSaves != null)
            {
                if (postSaves.ContainsKey(name))
                {
                    postSaves.Remove(name);
                }
            }
        }

        /// <summary>
        /// Called internally by EntitySpaces as part of the Hierarchical logic.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="coll"></param>
        protected void SetPostSave(string name, tgEntityCollectionBase coll)
        {
            if (postSaves == null)
            {
                postSaves = new Dictionary<string, tgEntityCollectionBase>();
            }

            postSaves[name] = coll;
        }

        /// <summary>
        /// Called internally by EntitySpaces, derived classes generated in conjuction with the Hierarchical
        /// functionality implement this to Save nested sub-objects that need to be saved after the esEntity
        /// itself is saved.
        /// </summary>
        internal protected void CommitPostSaves()
        {
            if (postSaves != null)
            {
                foreach (tgEntityCollectionBase coll in this.postSaves.Values)
                {
                    coll.Save();
                }
            }
        }
        #endregion

        #region PostOneSaves
        /// <summary>
        /// Called internally by EntitySpaces as part of the Hierarchical logic.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected tgEntity GetPostOneSave(string name)
        {
            if (postOneSaves != null)
            {
                if (postOneSaves.ContainsKey(name))
                {
                    return postOneSaves[name];
                }
            }

            return null;
        }

        /// <summary>
        /// Called internally by EntitySpaces as part of the Hierarchical logic.
        /// </summary>
        /// <param name="name"></param>
        protected void RemovePostOneSave(string name)
        {
            if (postOneSaves != null)
            {
                if (postOneSaves.ContainsKey(name))
                {
                    postOneSaves.Remove(name);
                }
            }
        }

        /// <summary>
        /// Called internally by EntitySpaces as part of the Hierarchical logic.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ent"></param>
        protected void SetPostOneSave(string name, tgEntity entity)
        {
            if (postOneSaves == null)
            {
                postOneSaves = new Dictionary<string, tgEntity>();
            }

            postOneSaves[name] = entity;
        }

        /// <summary>
        /// Called internally by EntitySpaces, derived classes generated in conjuction with the Hierarchical
        /// functionality implement this to Save nested sub-objects that need to be saved after the esEntity
        /// itself is saved.
        /// </summary>
        internal protected void CommitPostOneSaves()
        {
            if (postOneSaves != null)
            {
                foreach (tgEntity entity in this.postOneSaves.Values)
                {
                    entity.Save();
                }
            }
        }
        #endregion

        [NonSerialized]
        private Dictionary<string, tgEntity> preSaves;
        [NonSerialized]
        private Dictionary<string, tgEntityCollectionBase> postSaves;
        [NonSerialized]
        private Dictionary<string, tgEntity> postOneSaves;
        #endregion

        #region IDataErrorInfo Members

        string IDataErrorInfo.Error
        {
            get
            {
                return ((IDataErrorInfo)this)[string.Empty];
            }
        }

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                string result = string.Empty;

                // First let's assume this is the property name
                tgColumnMetadata columnMeta = this.Meta.Columns.FindByPropertyName(columnName);

                if (columnMeta == null)
                {
                    // nope, it must be the column name
                    columnMeta = this.Meta.Columns.FindByColumnName(columnName);
                }

                if (this.onValidateDelegate != null)
                {
                    result = onValidateDelegate(columnName, this, columnMeta);
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = this.Validate(columnName, this, columnMeta);
                }

                return result;
            }
        }

        /// <summary>
        /// Overridden in the generated classes to provide validation.
        /// </summary>
        /// <remarks>
        /// See the ES 2007 v1.1021.0 Release notes for details at
        /// http://www.entityspaces.net
        /// </remarks>
        /// <param name="columnName"></param>
        /// <param name="entity"></param>
        /// <param name="metadata"></param> 
        /// <returns></returns>
        virtual public string Validate(string columnName, tgEntity entity, tgColumnMetadata metadata)
        {
            return string.Empty;
        }

        /// <summary>
        /// Validation by delegate
        /// </summary>
        /// <remarks>
        /// See the ES 2007 v1.1021.0 Release notes for details at
        /// http://www.entityspaces.net
        /// </remarks>
        /// <param name="columnName"></param>
        /// <param name="entity"></param>
        /// <param name="metadata"></param> 
        /// <returns></returns>
        public delegate string ValidateDelegate(string columnName, tgEntity entity, tgColumnMetadata metadata);

        /// <summary>
        /// Used to provide validation.
        /// </summary> 
        /// <remarks>
        /// See the ES 2007 v1.1021.0 Release notes for details at
        /// http://www.entityspaces.net
        /// </remarks>
        public event ValidateDelegate OnValidateDelegate
        {
            add
            {
                onValidateDelegate += value;
            }
            remove
            {
                onValidateDelegate -= value;
            }
        }

        [NonSerialized]
        [IgnoreDataMember]
        private ValidateDelegate onValidateDelegate;

        #endregion

        #region IVisitable

        tgVisitableNode[] IVisitable.GetGraph(object state)
        {
            Type type = this.GetType();

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            FieldInfo field = null;

            List<tgVisitableNode> references = new List<tgVisitableNode>();

            for (int i = 0; i < fields.Length; i++)
            {
                field = fields[i];

                object o = field.GetValue(this);

                if (o != null)
                {
                    if (typeof(tgEntity).IsAssignableFrom(field.FieldType))
                    {
                        references.Add(new tgVisitableNode() { Obj = o, PropertyName = field.Name, fieldInfo = field });
                    }
                    else if (typeof(tgEntityCollectionBase).IsAssignableFrom(field.FieldType))
                    {
                        references.Add(new tgVisitableNode() { Obj = o, PropertyName = field.Name, fieldInfo = field });
                    }
                }
            }

            return references.ToArray();
        }

        #endregion

        #region Fields

        [NonSerialized]
        private tgEntityCollectionBase collection;

        [NonSerialized]
        private bool isInEditMode;

        [NonSerialized]
        private tgConnection connection;

        [NonSerialized]
        private tgDataRowState originalRowState;

        [NonSerialized]
        private List<string> originalModifiedColumns;

        [NonSerialized]
        private tgSmartDictionary proposedValues;

        [NonSerialized]
        private tgSmartDictionary backupValues;

        [NonSerialized]
        private Dictionary<string, int> selectedColumns;

        [NonSerialized]
        internal string rowError;

        [NonSerialized]
        internal bool _isLazyLoadDisabled;

        [NonSerialized]
        private bool applyDefaultsCalled;

        /// <summary>
        /// A list of column names that have been modified (or that are dirty) since this entity was
        /// retrieved from the database or since it was instantied if created new.
        /// </summary>
        internal List<string> m_modifiedColumns;

        internal tgSmartDictionary originalValues;

        internal tgSmartDictionary currentValues = new tgSmartDictionary();

        internal tgDataRowState rowState = tgDataRowState.Unchanged;

        #endregion

        #region DataContract Serialization Trickery

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            if (tempModifiedColumns != null)
            {
                this.m_modifiedColumns = tempModifiedColumns.ToList();
                tempModifiedColumns = null;
            }

            this.rowState = tempRowState;

            if (originalValues == null)
            {
                originalValues = new tgSmartDictionary(currentValues);
            }
        }

        [XmlIgnore]
        [DataMember(Name = "RowState", EmitDefaultValue = false)]
        private tgDataRowState TempRowState
        {
            get { return tg.RowState; }
            set { tempRowState = value; }
        }

        [NonSerialized]
        [IgnoreDataMember]
        private tgDataRowState tempRowState;

        [XmlIgnore]
        [DataMember(Name = "ModifiedColumns", EmitDefaultValue=false)]
        private string[] TempModifiedColumns
        {
            get { return tempModifiedColumns; }
            set { tempModifiedColumns = value; }
        }

        [NonSerialized]
        [IgnoreDataMember]
        private string[] tempModifiedColumns;

        #endregion

        #region ModifiedBy Hooks

        static public event ModifiedByEventHandler AddedByEventHandler;
        static public event ModifiedByEventHandler ModifiedByEventHandler;

        #endregion
    }

    [Serializable]
    [XmlType(IncludeInSchema = false)]
    [DataContract]
    public class tgKeyValuePair
    {
        [DataMember]
        public string Key { get; set; }
        [DataMember]
        public object Value { get; set;}
    }
}
