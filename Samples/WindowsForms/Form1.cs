using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BusinessObjects;

namespace WindowsForms
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                Employees emp = new Employees();
                if (emp.LoadByPrimaryKey(3))
                {
                    this.textBox1.Text = emp.LastName + ", " + emp.FirstName;
                }
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
        }
    }
}
