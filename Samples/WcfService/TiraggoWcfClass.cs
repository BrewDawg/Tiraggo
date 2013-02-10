using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;

using Tiraggo.Interfaces;
using Tiraggo.js;
using Tiraggo.Loader;

using BusinessObjects;
using Tiraggo.DynamicQuery;

namespace TiraggoWcfService
{
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceContract()]
    public partial class TiraggoWcfClass
    {
        public TiraggoWcfClass()
        {
            tgProviderFactory.Factory = new tgDataProviderFactory();
        }

        #region Employees Members

        [WebInvoke(ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        public jsResponse<EmployeesCollection, Employees> EmployeesCollection_LoadAll()
        {
            jsResponse<EmployeesCollection, Employees> response = new jsResponse<EmployeesCollection, Employees>();

            try
            {
                EmployeesCollection collection = new EmployeesCollection();
                collection.LoadAll();
                response.collection = collection;
            }
            catch (Exception ex)
            {
                response.exception = ex.Message;
            }

            return response;
        }

        [WebInvoke(ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        public jsResponse<EmployeesCollection, Employees> EmployeesCollection_Save(EmployeesCollection collection)
        {
            jsResponse<EmployeesCollection, Employees> response = new jsResponse<EmployeesCollection, Employees>();

            try
            {
                collection.Save();
                response.collection = collection;
            }
            catch (Exception ex)
            {
                response.exception = ex.Message;
            }

            return response;
        }

        [WebInvoke(ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        public jsResponse<EmployeesCollection, Employees> Employees_LoadByPrimaryKey(System.Int32 employeeID)
        {
            jsResponse<EmployeesCollection, Employees> response = new jsResponse<EmployeesCollection, Employees>();

            try
            {
                Employees entity = new Employees();
                if (entity.LoadByPrimaryKey(employeeID))
                {
                    response.entity = entity;
                }
            }
            catch (Exception ex)
            {
                response.exception = ex.Message;
            }

            return response;
        }

        [WebInvoke(ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        public jsResponse<EmployeesCollection, Employees> Employees_Save(Employees entity)
        {
            jsResponse<EmployeesCollection, Employees> response = new jsResponse<EmployeesCollection, Employees>();

            try
            {
                entity.Save();
                response.entity = entity;
            }
            catch (Exception ex)
            {
                response.exception = ex.Message;
            }

            return response;
        }

        #endregion


    }
}		
