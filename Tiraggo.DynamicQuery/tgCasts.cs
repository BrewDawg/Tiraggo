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

namespace Tiraggo.DynamicQuery
{
    /// <summary>
    /// The base class for all of the explicit casting operators such as 
    /// <see cref="tgByte"/>, <see cref="tgChar"/>, <see cref="tgDateTime"/>.
    /// </summary>
    public class tgCast
    {
        internal tgCast() { }

        internal tgQueryItem item;
    }

    #region tgBoolean
    /// <summary>
    /// Used in the DynamicQuery syntax to cast like this: (tgBoolean)query.SomeProperty
    /// </summary>
    /// <remarks>
    /// VB.NET users will need to use tgQueryItem.Cast.
    /// </remarks>
    /// <example>
    /// Here we are building a special column like this "Smith, John [24]" where 24 is
    /// his age. We use the (tgString) cast operator so that the database system will
    /// convert it to a string.
    /// <code>
    /// EmployeeCollection coll = new EmployeeCollection(); 
    /// EmployeeQuery q = coll.Query; 
    /// 
    /// q.Select
    /// (
    ///     (
    ///         (q.LastName + ", " + q.FirstName).Trim() + " [" + (tgString)q.Age + "]"
    ///     )
    ///     .ToUpper().As("FullName")
    /// ); 
    /// 
    /// if (coll.Query.Load())
    /// {
    ///     foreach (Employee emp in coll)
    ///     {
    ///         string fn = emp.GetColumn("FullName");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class tgBoolean : tgCast
    {
        private tgBoolean() { }

        internal tgBoolean(tgQueryItem item)
        {
            base.item = item;
            item.Cast(tgCastType.Boolean);
        }

        /// <summary>
        /// This is called automatically for you when necessary.
        /// </summary>
        public static implicit operator tgQueryItem(tgBoolean cast)
        {
            return cast.item;
        }
    }
    #endregion

    #region tgByte
    /// <summary>
    /// Used in the DynamicQuery syntax to cast like this: (tgByte)query.SomeProperty
    /// </summary>
    /// <remarks>
    /// VB.NET users will need to use tgQueryItem.Cast.
    /// </remarks>
    /// <example>
    /// Here we are building a special column like this "Smith, John [24]" where 24 is
    /// his age. We use the (tgString) cast operator so that the database system will
    /// convert it to a string.
    /// <code>
    /// EmployeeCollection coll = new EmployeeCollection(); 
    /// EmployeeQuery q = coll.Query; 
    /// 
    /// q.Select
    /// (
    ///     (
    ///         (q.LastName + ", " + q.FirstName).Trim() + " [" + (tgString)q.Age + "]"
    ///     )
    ///     .ToUpper().As("FullName")
    /// ); 
    /// 
    /// if (coll.Query.Load())
    /// {
    ///     foreach (Employee emp in coll)
    ///     {
    ///         string fn = emp.GetColumn("FullName");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class tgByte : tgCast
    {
        private tgByte() { }

        internal tgByte(tgQueryItem item)
        {
            base.item = item;
            item.Cast(tgCastType.Byte);
        }

        /// <summary>
        /// This is called automatically for you when necessary.
        /// </summary>
        public static implicit operator tgQueryItem(tgByte cast)
        {
            return cast.item;
        }
    }
    #endregion

    #region tgChar
    /// <summary>
    /// Used in the DynamicQuery syntax to cast like this: (tgChar)query.SomeProperty
    /// </summary>
    /// <remarks>
    /// VB.NET users will need to use tgQueryItem.Cast.
    /// </remarks>
    /// <example>
    /// Here we are building a special column like this "Smith, John [24]" where 24 is
    /// his age. We use the (tgString) cast operator so that the database system will
    /// convert it to a string.
    /// <code>
    /// EmployeeCollection coll = new EmployeeCollection(); 
    /// EmployeeQuery q = coll.Query; 
    /// 
    /// q.Select
    /// (
    ///     (
    ///         (q.LastName + ", " + q.FirstName).Trim() + " [" + (tgString)q.Age + "]"
    ///     )
    ///     .ToUpper().As("FullName")
    /// ); 
    /// 
    /// if (coll.Query.Load())
    /// {
    ///     foreach (Employee emp in coll)
    ///     {
    ///         string fn = emp.GetColumn("FullName");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class tgChar : tgCast
    {
        private tgChar() { }

        internal tgChar(tgQueryItem item)
        {
            base.item = item;
            item.Cast(tgCastType.Char);
        }

        /// <summary>
        /// This is called automatically for you when necessary.
        /// </summary>
        public static implicit operator tgQueryItem(tgChar cast)
        {
            return cast.item;
        }
    }
    #endregion

    #region tgDateTime
    /// <summary>
    /// Used in the DynamicQuery syntax to cast like this: (tgDateTime)query.SomeProperty
    /// </summary>
    /// <remarks>
    /// VB.NET users will need to use tgQueryItem.Cast.
    /// </remarks>
    /// <example>
    /// Here we are building a special column like this "Smith, John [24]" where 24 is
    /// his age. We use the (tgString) cast operator so that the database system will
    /// convert it to a string.
    /// <code>
    /// EmployeeCollection coll = new EmployeeCollection(); 
    /// EmployeeQuery q = coll.Query; 
    /// 
    /// q.Select
    /// (
    ///     (
    ///         (q.LastName + ", " + q.FirstName).Trim() + " [" + (tgString)q.Age + "]"
    ///     )
    ///     .ToUpper().As("FullName")
    /// ); 
    /// 
    /// if (coll.Query.Load())
    /// {
    ///     foreach (Employee emp in coll)
    ///     {
    ///         string fn = emp.GetColumn("FullName");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class tgDateTime : tgCast
    {
        private tgDateTime() { }

        internal tgDateTime(tgQueryItem item)
        {
            base.item = item;
            item.Cast(tgCastType.DateTime);
        }

        /// <summary>
        /// This is called automatically for you when necessary.
        /// </summary>
        public static implicit operator tgQueryItem(tgDateTime cast)
        {
            return cast.item;
        }
    }
    #endregion

    #region tgDecimal
    /// <summary>
    /// Used in the DynamicQuery syntax to cast like this: (tgDecimal)query.SomeProperty
    /// </summary>
    /// <remarks>
    /// VB.NET users will need to use tgQueryItem.Cast
    /// </remarks>
    /// <example>
    /// Here we are building a special column like this "Smith, John [24]" where 24 is
    /// his age. We use the (tgString) cast operator so that the database system will
    /// convert it to a string.
    /// <code>
    /// EmployeeCollection coll = new EmployeeCollection(); 
    /// EmployeeQuery q = coll.Query; 
    /// 
    /// q.Select
    /// (
    ///     (
    ///         (q.LastName + ", " + q.FirstName).Trim() + " [" + (tgString)q.Age + "]"
    ///     )
    ///     .ToUpper().As("FullName")
    /// ); 
    /// 
    /// if (coll.Query.Load())
    /// {
    ///     foreach (Employee emp in coll)
    ///     {
    ///         string fn = emp.GetColumn("FullName");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class tgDecimal : tgCast
    {
        private tgDecimal() { }

        internal tgDecimal(tgQueryItem item)
        {
            base.item = item;
            item.Cast(tgCastType.Decimal);
        }

        /// <summary>
        /// This is called automatically for you when necessary.
        /// </summary>
        public static implicit operator tgQueryItem(tgDecimal cast)
        {
            return cast.item;
        }
    }
    #endregion

    #region tgDouble
    /// <summary>
    /// Used in the DynamicQuery syntax to cast like this: (tgDouble)query.SomeProperty
    /// </summary>
    /// <remarks>
    /// VB.NET users will need to use tgQueryItem.Cast.
    /// </remarks>
    /// <example>
    /// Here we are building a special column like this "Smith, John [24]" where 24 is
    /// his age. We use the (tgString) cast operator so that the database system will
    /// convert it to a string.
    /// <code>
    /// EmployeeCollection coll = new EmployeeCollection(); 
    /// EmployeeQuery q = coll.Query; 
    /// 
    /// q.Select
    /// (
    ///     (
    ///         (q.LastName + ", " + q.FirstName).Trim() + " [" + (tgString)q.Age + "]"
    ///     )
    ///     .ToUpper().As("FullName")
    /// ); 
    /// 
    /// if (coll.Query.Load())
    /// {
    ///     foreach (Employee emp in coll)
    ///     {
    ///         string fn = emp.GetColumn("FullName");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class tgDouble : tgCast
    {
        private tgDouble() { }

        internal tgDouble(tgQueryItem item)
        {
            base.item = item;
            item.Cast(tgCastType.Double);
        }

        /// <summary>
        /// This is called automatically for you when necessary.
        /// </summary>
        public static implicit operator tgQueryItem(tgDouble cast)
        {
            return cast.item;
        }
    }
    #endregion

    #region tgGuid
    /// <summary>
    /// Used in the DynamicQuery syntax to cast like this: (tgGuid)query.SomeProperty
    /// </summary>
    /// <remarks>
    /// VB.NET users will need to use tgQueryItem.Cast.
    /// </remarks>
    /// <example>
    /// Here we are building a special column like this "Smith, John [24]" where 24 is
    /// his age. We use the (tgString) cast operator so that the database system will
    /// convert it to a string.
    /// <code>
    /// EmployeeCollection coll = new EmployeeCollection(); 
    /// EmployeeQuery q = coll.Query; 
    /// 
    /// q.Select
    /// (
    ///     (
    ///         (q.LastName + ", " + q.FirstName).Trim() + " [" + (tgString)q.Age + "]"
    ///     )
    ///     .ToUpper().As("FullName")
    /// ); 
    /// 
    /// if (coll.Query.Load())
    /// {
    ///     foreach (Employee emp in coll)
    ///     {
    ///         string fn = emp.GetColumn("FullName");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class tgGuid : tgCast
    {
        private tgGuid() { }

        internal tgGuid(tgQueryItem item)
        {
            base.item = item;
            item.Cast(tgCastType.Guid);
        }

        /// <summary>
        /// This is called automatically for you when necessary.
        /// </summary>
        public static implicit operator tgQueryItem(tgGuid cast)
        {
            return cast.item;
        }
    }
    #endregion

    #region tgInt16
    /// <summary>
    /// Used in the DynamicQuery syntax to cast like this: (tgInt16)query.SomeProperty
    /// </summary>
    /// <remarks>
    /// VB.NET users will need to use tgQueryItem.Cast.
    /// </remarks>
    /// <example>
    /// Here we are building a special column like this "Smith, John [24]" where 24 is
    /// his age. We use the (tgString) cast operator so that the database system will
    /// convert it to a string.
    /// <code>
    /// EmployeeCollection coll = new EmployeeCollection(); 
    /// EmployeeQuery q = coll.Query; 
    /// 
    /// q.Select
    /// (
    ///     (
    ///         (q.LastName + ", " + q.FirstName).Trim() + " [" + (tgString)q.Age + "]"
    ///     )
    ///     .ToUpper().As("FullName")
    /// ); 
    /// 
    /// if (coll.Query.Load())
    /// {
    ///     foreach (Employee emp in coll)
    ///     {
    ///         string fn = emp.GetColumn("FullName");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class tgInt16 : tgCast
    {
        private tgInt16() { }

        internal tgInt16(tgQueryItem item)
        {
            base.item = item;
            item.Cast(tgCastType.Int16);
        }

        /// <summary>
        /// This is called automatically for you when necessary.
        /// </summary>
        public static implicit operator tgQueryItem(tgInt16 cast)
        {
            return cast.item;
        }
    }
    #endregion

    #region tgInt32
    /// <summary>
    /// Used in the DynamicQuery syntax to cast like this: (tgInt32)query.SomeProperty
    /// </summary>
    /// <remarks>
    /// VB.NET users will need to use tgQueryItem.Cast.
    /// </remarks>
    /// <example>
    /// Here we are building a special column like this "Smith, John [24]" where 24 is
    /// his age. We use the (tgString) cast operator so that the database system will
    /// convert it to a string.
    /// <code>
    /// EmployeeCollection coll = new EmployeeCollection(); 
    /// EmployeeQuery q = coll.Query; 
    /// 
    /// q.Select
    /// (
    ///     (
    ///         (q.LastName + ", " + q.FirstName).Trim() + " [" + (tgString)q.Age + "]"
    ///     )
    ///     .ToUpper().As("FullName")
    /// ); 
    /// 
    /// if (coll.Query.Load())
    /// {
    ///     foreach (Employee emp in coll)
    ///     {
    ///         string fn = emp.GetColumn("FullName");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class tgInt32 : tgCast
    {
        private tgInt32() {}

        internal tgInt32(tgQueryItem item)
        {
            base.item = item;
            item.Cast(tgCastType.Int32);
        }

        /// <summary>
        /// This is called automatically for you when necessary.
        /// </summary>
        public static implicit operator tgQueryItem(tgInt32 cast)
        {
            return cast.item;
        }
    }
    #endregion

    #region tgInt64
    /// <summary>
    /// Used in the DynamicQuery syntax to cast like this: (tgInt64)query.SomeProperty
    /// </summary>
    /// <remarks>
    /// VB.NET users will need to use tgQueryItem.Cast.
    /// </remarks>
    /// <example>
    /// Here we are building a special column like this "Smith, John [24]" where 24 is
    /// his age. We use the (tgString) cast operator so that the database system will
    /// convert it to a string.
    /// <code>
    /// EmployeeCollection coll = new EmployeeCollection(); 
    /// EmployeeQuery q = coll.Query; 
    /// 
    /// q.Select
    /// (
    ///     (
    ///         (q.LastName + ", " + q.FirstName).Trim() + " [" + (tgString)q.Age + "]"
    ///     )
    ///     .ToUpper().As("FullName")
    /// ); 
    /// 
    /// if (coll.Query.Load())
    /// {
    ///     foreach (Employee emp in coll)
    ///     {
    ///         string fn = emp.GetColumn("FullName");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class tgInt64 : tgCast
    {
        private tgInt64() {}

        internal tgInt64(tgQueryItem item)
        {
            base.item = item;
            item.Cast(tgCastType.Int64);
        }

        /// <summary>
        /// This is called automatically for you when necessary.
        /// </summary>
        public static implicit operator tgQueryItem(tgInt64 cast)
        {
            return cast.item;
        }
    }
    #endregion

    #region tgSingle
    /// <summary>
    /// Used in the DynamicQuery syntax to cast like this: (tgSingle)query.SomeProperty
    /// </summary>
    /// <remarks>
    /// VB.NET users will need to use tgQueryItem.Cast.
    /// </remarks>
    /// <example>
    /// Here we are building a special column like this "Smith, John [24]" where 24 is
    /// his age. We use the (tgString) cast operator so that the database system will
    /// convert it to a string.
    /// <code>
    /// EmployeeCollection coll = new EmployeeCollection(); 
    /// EmployeeQuery q = coll.Query; 
    /// 
    /// q.Select
    /// (
    ///     (
    ///         (q.LastName + ", " + q.FirstName).Trim() + " [" + (tgString)q.Age + "]"
    ///     )
    ///     .ToUpper().As("FullName")
    /// ); 
    /// 
    /// if (coll.Query.Load())
    /// {
    ///     foreach (Employee emp in coll)
    ///     {
    ///         string fn = emp.GetColumn("FullName");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class tgSingle : tgCast
    {
        private tgSingle() {}

        internal tgSingle(tgQueryItem item)
        {
            base.item = item;
            item.Cast(tgCastType.Single);
        }

        /// <summary>
        /// This is called automatically for you when necessary.
        /// </summary>
        public static implicit operator tgQueryItem(tgSingle cast)
        {
            return cast.item;
        }
    }
    #endregion

    #region tgString
    /// <summary>
    /// Used in the DynamicQuery syntax to cast like this: (tgString)query.SomeProperty
    /// </summary>
    /// <remarks>
    /// VB.NET users will need to use tgQueryItem.Cast.
    /// </remarks>
    /// <example>
    /// Here we are building a special column like this "Smith, John [24]" where 24 is
    /// his age. We use the (tgString) cast operator so that the database system will
    /// convert it to a string.
    /// <code>
    /// EmployeeCollection coll = new EmployeeCollection(); 
    /// EmployeeQuery q = coll.Query; 
    /// 
    /// q.Select
    /// (
    ///     (
    ///         (q.LastName + ", " + q.FirstName).Trim() + " [" + (tgString)q.Age + "]"
    ///     )
    ///     .ToUpper().As("FullName")
    /// ); 
    /// 
    /// if (coll.Query.Load())
    /// {
    ///     foreach (Employee emp in coll)
    ///     {
    ///         string fn = emp.GetColumn("FullName");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class tgString : tgCast
    {
        internal tgString(tgQueryItem item)
        {
            base.item = item;
            item.Cast(tgCastType.String);
        }

        /// <summary>
        /// This is called automatically for you when necessary.
        /// </summary>
        public static implicit operator tgQueryItem(tgString cast)
        {
            return cast.item;
        }
    }
    #endregion
}
