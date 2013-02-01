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

namespace Tiraggo.Interfaces
{
    /// <summary>
    /// This class is used to pass parameters to the providers. 
    /// </summary>
    /// <example>
    /// The example below calls a stored procedure that concatenates an Employees first name and last name
    /// and returns them as an output parameter. Notice that when the FullName parameter is added it is
    /// necessary to provide the direction, type and size. This is ONLY necessary when adding output parameters
    /// and you are encouraged to only provide the name/value for all other parameters. This will make your code
    /// much more provider independent.
    /// <code>
    /// public partial class Employees : esEmployees
    /// {
    ///		public string GetFullName(int employeeID)
    ///		{
    ///			tgParameters parms = new tgParameters();
    ///
    ///			parms.Add("EmployeeID", employeeID);
    ///			parms.Add("FullName", tgParameterDirection.Output, DbType.String, 40);
    ///
    ///			this.ExecuteNonQuery(tgQueryType.StoredProcedure, "proc_GetEmployeeFullName", parms);
    ///
    ///			return parms["FullName"].Value as string;
    ///		}
    ///}
    /// </code>
    /// <seealso cref="tgParameter"/>, <seealso cref="tgDataProvider"/>, <seealso cref="IDataProvider"/>
    /// </example> 
    [Serializable] 
    public class tgParameters : IEnumerable 
    {
        public tgParameters()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameter">An already created tgParameter. This is mostly used internally.</param>
        /// <returns>The same parameter passed in</returns>
        public tgParameter Add(tgParameter parameter)
        {
            this.parameters.Add(parameter);
            this.hash[parameter.Name] = parameter;
            return parameter;
        }

        /// <summary>
        /// This is the preferred overload for adding parameters.
        /// </summary>
        /// <remarks>
        /// Do not add prefixes such as @ or ? or : as the providers append the proper prefix. This allows you 
        /// to pass parameters in a provider independent fashion. All parameters added via this method are input
        /// parameters.
        /// </remarks>
        /// <param name="name">The name of the parameter without a prefix, for example, "EmployeeID" not "@EmployeeID".</param>
        /// <param name="value">The value of the parameter</param>
        /// <returns>The newly created parameter</returns>
        public tgParameter Add(string name, object value)
        {
            return this.Add(new tgParameter(name, value));
        }

        /// <summary>
        /// If you need to add a parameter with an tgParameterDirection other than Input use this method.
        /// </summary>
        /// <param name="name">The name of the parameter without a prefix, for example, "EmployeeID" not "@EmployeeID".</param>
        /// <param name="direction">The parameter direction</param>
        /// <returns>The newly created parameter</returns>
        public tgParameter Add(string name, tgParameterDirection direction)
        {
            return this.Add(new tgParameter(name, null, direction));
        }

        /// <summary>
        /// This should ONLY be used for parameters other than input parameters.
        /// </summary>
        /// <param name="name">The name of the parameter without a prefix, for example, "EmployeeID" not "@EmployeeID".</param>
        /// <param name="value">The value of the parameter</param>
        /// <param name="direction">The parameter direction</param>
        /// <returns>The newly created parameter</returns>
        public tgParameter Add(string name, object value, tgParameterDirection direction)
        {
            return this.Add(new tgParameter(name, value, direction));
        }

        /// <summary>
        /// This should ONLY be used for parameters other than input parameters.
        /// </summary>
        /// <param name="name">The name of the parameter without a prefix, for example, "EmployeeID" not "@EmployeeID".</param>
        /// <param name="direction">The parameter direction</param>
        /// <param name="type">The System.Data.DbType of the parameter</param>
        /// <param name="size">The size. For strings the length, for most others 0 can be passed in</param>
        /// <returns>The newly created parameter</returns>
        public tgParameter Add(string name, tgParameterDirection direction, DbType type, int size)
        {
            return this.Add(new tgParameter(name, direction, type, size));
        }

        /// <summary>
        /// This should ONLY be used for parameters other than input parameters.
        /// </summary>
        /// <param name="name">The name of the parameter without a prefix, for example, "EmployeeID" not "@EmployeeID".</param>
        /// <param name="value">The value of the parameter</param>
        /// <param name="direction">The parameter direction</param>
        /// <param name="type">The System.Data.DbType of the parameter</param>
        /// <param name="size">The size. For strings the length, for most others 0 can be passed in</param>
        /// <returns>The newly created parameter</returns>
        public tgParameter Add(string name, object value, tgParameterDirection direction, DbType type, int size)
        {
            return this.Add(new tgParameter(name, value, direction, type, size));
        }

        /// <summary>
        /// The number of parameters in the collection
        /// </summary>
        public int Count
        {
            get { return parameters.Count; }
        }

        /// <summary>
        /// Use this method to fetch a given tgParameter by name. There should be no prefix such as @, ? or : in the name.
        /// </summary>
        /// <param name="name">The name of the parameter without a prefix, for example, "EmployeeID" not "@EmployeeID".</param>
        /// <returns>The desired parameter or null/Nothing if not found</returns>
        public tgParameter this[string name]
        {
            get
            {
                tgParameter param = null;

                if (this.hash.ContainsKey(name))
                {
                    param = this.hash[name];
                }

                return param;
            }
        }

        /// <summary>
        /// Used Internally to merge parameters returned by the provider back into the original collection.
        /// </summary>
        /// <param name="parms"></param>
        internal void Merge(tgParameters parms)
        {
            foreach(tgParameter esParam in parms)
            {
                tgParameter esOriginalParam = this.hash[esParam.Name];

                esOriginalParam.Value = esParam.Value;
            }
        }

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return this.parameters.GetEnumerator();
        }

        #endregion

        private List<tgParameter> parameters = new List<tgParameter>();

        [NonSerialized]
        private Dictionary<System.String, tgParameter> hash = new Dictionary<string, tgParameter>();
    }

    /// <summary>
    /// Represents a parameter typically passed into a stored procedure.
    /// </summary>
    /// <remarks>
    /// Normally you don't directly create tgParameter objects, instead you call the Add method on the tgParameters Collection.
    /// It is important not to add prefixes to the parameter names such as @, ?, : as the providers will do this.
    /// </remarks>
    /// <seealso cref="tgParameters"/>
    [Serializable] 
    public class tgParameter
    {
        public tgParameter()
        {

        }

        /// <summary>
        /// This is the provider independent way to use parameters, you can do so with the other methods as well but 
        /// this is the preferred constructor for all input parameters.
        /// </summary>
        /// <param name="name">The name of the parameter without a prefix, for example, "EmployeeID" not "@EmployeeID".</param>
        /// <param name="value">The value of the parameter</param>
        public tgParameter(string name, object value)
        {
            this.name = name;
            this.value = value;
        }

        /// <summary>
        /// Use this constructor for non-input parameters only.
        /// </summary>
        /// <param name="name">The name of the parameter without a prefix, for example, "EmployeeID" not "@EmployeeID".</param>
        /// <param name="value">The value of the parameter</param>
        /// <param name="direction">The direction of the parameter</param>
        public tgParameter(string name, object value, tgParameterDirection direction)
        {
            this.name = name;
            this.value = value;
            this.direction = direction;
        }

        /// <summary>
        /// Use this constructor for non-input parameters only.
        /// </summary>
        /// <param name="name">The name of the parameter without a prefix, for example, "EmployeeID" not "@EmployeeID".</param>
        /// <param name="direction">The parameter direction</param>
        /// <param name="type">The System.Data.DbType of the parameter</param>
        /// <param name="size">The size. For strings the length, for most others 0 can be passed in</param>
        public tgParameter(string name, tgParameterDirection direction, DbType type, int size)
        {
            this.name = name;
            this.direction = direction;
            this.dbType = type;
            this.size = size;
        }

        /// <summary>
        /// Use this constructor for non-input parameters only.
        /// </summary>
        /// <param name="name">The name of the parameter without a prefix, for example, "EmployeeID" not "@EmployeeID".</param>
        /// <param name="value">The value of the parameter</param>
        /// <param name="direction">The parameter direction</param>
        /// <param name="type">The System.Data.DbType of the parameter</param>
        /// <param name="size">The size. For strings the length, for most others 0 can be passed in</param>
        /// <returns>The newly created parameter</returns>
        public tgParameter(string name, object value, tgParameterDirection direction, DbType type, int size)
        {
            this.name = name;
            this.value = value;
            this.direction = direction;
            this.dbType = type;
            this.size = size;
        }

        /// <summary>
        /// The Name of the parameter
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// The Value of the parameter. After the call to the EntitySpaces DataProvider is made any output or return
        /// parameters will have their "Value" set accordingly.
        /// </summary>
        public object Value
        {
            get { return this.value;  }
            set { this.value = value; }
        }

        /// <summary>
        /// The parameter direction. The default is Input.
        /// </summary>
        public tgParameterDirection Direction
        {
            get { return direction;  }
            set { direction = value; }
        }

        /// <summary>
        /// See System.Data.DbType, this should only be used for parameters other than input parameters.
        /// </summary>
        public DbType DbType
        {
            get { return dbType;  }
            set { dbType = value; }
        }

        /// <summary>
        /// Used for things such as SqlGeometry/SqlGeography. You use the SQL column type, ie, "geometry" or geography" 
        /// </summary>
        public string UdtTypeName
        {
            get { return udtTypeName; }
            set { udtTypeName = value; }
        }

        /// <summary>
        /// This should only be used for parameters other than input parameters. For strings this is the length, for most
        /// other types set to 0.
        /// </summary>
        public int Size
        {
            get { return size;}
            set { size = value; }
        }

        /// <summary>
        /// This should only be used for parameters other than input parameters.
        /// </summary>
        /// <remarks>
        /// "Obsolete" for Oracle.
        /// Not supported for Access.
        /// </remarks>
        public byte Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        /// <summary>
        /// This should only be used for parameters other than input parameters.
        /// </summary>
        /// <remarks>
        /// "Obsolete" for Oracle.
        /// Not supported for Access.
        /// </remarks>
        public byte Precision
        {
            get { return precision; }
            set { precision = value; }
        }

        private string name;
        private object value;
        private tgParameterDirection direction;
        private DbType dbType;
        private int size;
        private byte scale;
        private byte precision;
        private string udtTypeName;
    }

    /// <summary>
    /// Used to determine the direction of the tgParameter class. The default is Input
    /// </summary>
    [Serializable] 
    public enum tgParameterDirection
    {
        /// <summary>
        /// The parameter is an input parameter. 
        /// </summary>
        Input = 0,
        /// <summary>
        /// The parameter is capable of both input and output. 
        /// </summary>
        InputOutput,
        /// <summary>
        /// The parameter is an output parameter. 
        /// </summary>
        Output,
        /// <summary>
        /// The parameter represents a return value from an operation such as a stored procedure, built-in function, or user-defined function. 
        /// </summary>
        ReturnValue
    }
}
