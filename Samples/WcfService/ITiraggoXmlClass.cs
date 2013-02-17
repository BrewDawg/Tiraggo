using System.ServiceModel;

using BusinessObjects;

namespace TiraggoXmlService
{
	[ServiceContract]
	public interface ITiraggoXmlClass
	{

		#region IEmployees

		[OperationContract]
		EmployeesCollection.EmployeesCollectionWCFPacket Employees_LoadAll();		

		[OperationContract]
		EmployeesCollection.EmployeesCollectionWCFPacket Employees_QueryForCollection(string serializedQuery);

		[OperationContract]
		Employees Employees_QueryForEntity(string serializedQuery);

		[OperationContract]
		Employees Employees_GetByPrimaryKey(System.Int32 employeeID);

		[OperationContract]
		EmployeesCollection.EmployeesCollectionWCFPacket Employees_SaveCollection(EmployeesCollection.EmployeesCollectionWCFPacket collection);

		[OperationContract]
		Employees Employees_SaveEntity(Employees entity);		

		#endregion

	}
}
