using System;

namespace Tiraggo_js
{

    public partial class Editing : System.Web.UI.Page
    {
        private string json = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            //Tiraggo.Interfaces.tgProviderFactory.Factory = new Tiraggo.Loader.tgDataProviderFactory();

            //EmployeesCollection coll = new EmployeesCollection();

            //EmployeesQuery q = new EmployeesQuery();
            //q.Select(q.EmployeeID, q.LastName, q.FirstName, q.FirstName.As("Extra"));

            //if (coll.Load(q))
            //{
            //    json = coll.ToJSON();

            //    EmployeesCollection newColl = new EmployeesCollection().FromJSON(json);
            //}

        }
    }
}