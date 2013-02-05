using System;
using System.Windows.Forms;

using BusinessObjects;

namespace WindowsForms
{
    public partial class Form1 : Form
    {

        EmployeesCollection coll = new EmployeesCollection();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Employees emp = new Employees();
            emp.EmployeeID = 44;

            string ln = emp.LastName;

            emp.LastName = "Griffin";
            emp.FirstName = "Mike";

            emp.Save();
            LoadGrid();
        }

        private void LoadGrid()
        {
            try
            {
                coll = new EmployeesCollection();
                if (coll.LoadAll())
                {
                    grid.DataSource = coll;
                }
            }
            catch (Exception ex)
            {
                throw;
                //string error = ex.Message;
            }
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            LoadGrid();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            coll.Save();
        }
    }
}
