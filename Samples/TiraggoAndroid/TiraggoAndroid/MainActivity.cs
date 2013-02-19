using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using WcfService.TiraggoService;
using System.ServiceModel;
using System.Text;

using BusinessObjects;

namespace TiraggoAndroid
{
	[Activity (Label = "TiraggoAndroid", MainLauncher = true)]
	public class Activity1 : Activity
	{
		TiraggoXmlClassClient client = null;
		private Employees emp;
		int count = 1;

		public void Test(int id)
		{
			emp = client.Employees_GetByPrimaryKey (id);
		}

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			InitService ();

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			// Get our button from the layout resource,
			// and attach an event to it
			Button button = FindViewById<Button> (Resource.Id.myButton);

			button.Click += delegate {
				Test (count);
				string name = "null";
				if(emp != null && emp.LastName != null)
				{
					name = emp.LastName;
				}
				button.Text = string.Format ("{0} {1} CLICKS!", count++, name);
			};
		}

		protected void InitService()
		{
			var binding = new BasicHttpBinding () {
				Name = "basicHttpBinding",
				MaxReceivedMessageSize = 2147483647,
				MaxBufferSize = 2147483647,
				TransferMode = System.ServiceModel.TransferMode.Buffered,
				MessageEncoding = WSMessageEncoding.Text,
				TextEncoding = Encoding.UTF8,
				UseDefaultWebProxy = true
			};
			
			binding.ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas() {
				MaxArrayLength = 2147483646,
				MaxStringContentLength = 2147483647,
				MaxBytesPerRead = 2147483647,
				MaxNameTableCharCount = 2147483647,
				MaxDepth = 2147483647
			};
			
			var timeout = new TimeSpan(0,1,0);
			binding.SendTimeout= timeout;
			binding.OpenTimeout = timeout;
			binding.ReceiveTimeout = timeout;
			
			client = new TiraggoXmlClassClient (binding, new EndpointAddress ("http://www.tiraggo.com/tiraggo/wcfservice/Tiraggoxmlclass.svc"));
		}
	}
}


