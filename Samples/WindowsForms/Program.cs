using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;


namespace WindowsForms
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            Tiraggo.Interfaces.tgProviderFactory.Factory = new Tiraggo.Loader.tgDataProviderFactory();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
