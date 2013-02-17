using System;
using System.Collections.Generic;
using System.ServiceModel.Activation;
using System.Text.RegularExpressions;

using BusinessObjects;

using Tiraggo.Interfaces;

namespace TiraggoXmlService
{
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
	public partial class TiraggoXmlClass : ITiraggoXmlClass
	{

		#region IEmployees Members

		public EmployeesCollection.EmployeesCollectionWCFPacket Employees_LoadAll()
		{
			EmployeesCollection coll = new EmployeesCollection();
			if (coll.LoadAll())
			{
				return coll;
			}

			return null;
		}		

		public EmployeesCollection.EmployeesCollectionWCFPacket Employees_QueryForCollection(string serializedQuery)
		{
			EmployeesQuery query = EmployeesQuery.SerializeHelper.FromXml(
				serializedQuery, typeof(EmployeesQuery), AllKnownTypes) as EmployeesQuery;

			EmployeesCollection coll = new EmployeesCollection();
			if (coll.Load(query))
			{
				return coll;
			}

			return null;
		}

		public Employees Employees_QueryForEntity(string serializedQuery)
		{
			EmployeesQuery query = EmployeesQuery.SerializeHelper.FromXml(
				serializedQuery, typeof(EmployeesQuery), AllKnownTypes) as EmployeesQuery;

			Employees obj = new Employees();
			if (obj.Load(query))
			{
				return obj;
			}

			return null;
		}

		public Employees Employees_GetByPrimaryKey(System.Int32 employeeID)
		{
			Employees obj = new Employees();
			if (obj.LoadByPrimaryKey(employeeID))
			{
				return obj;
			}
			return null;
		}

		public EmployeesCollection.EmployeesCollectionWCFPacket Employees_SaveCollection(EmployeesCollection.EmployeesCollectionWCFPacket collection)
		{
			if (collection != null)
			{
				collection.Collection.Save();
				return collection;
			}

			return null;
		}

		public Employees Employees_SaveEntity(Employees entity)
		{
			if (entity != null)
			{
				entity.Save();

				if (entity.RowState != tgDataRowState.Deleted && entity.RowState != tgDataRowState.Invalid)
				{
					return entity;
				}
			}

			return null;
		}

		#endregion


		#region Tiraggo Routines

		static private List<Type> AllKnownTypes = GetAllKnownTypes();

		static List<Type> GetAllKnownTypes()
		{
			List<Type> types = new List<Type>();
			types.Add(typeof(EmployeesQuery));

			return types;
		}

		#endregion
	}
}
