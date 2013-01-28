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
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

using Tiraggo.Interfaces;

namespace Tiraggo.Web.Design
{
    public class esDataSourceDesigner : DataSourceDesigner
    {
        private DataSourceControl esDataSource;

        public override void Initialize(IComponent component)
        {
            base.Initialize(component);
            this.esDataSource = (DataSourceControl)component;
        }

        #region Views
        static private readonly string[] viewNames = { "DefaultView" };

        public esDataSourceDesigner()
        {
            this.dataView = new esDataSourceDesignerView(this, viewNames[0]);
		}

		public override DesignerDataSourceView GetView(string viewName)
        {
			return this.dataView;
		}

        protected DesignerDataSourceView GetView()
        {
            return this.GetView(viewNames[0]) as esDataSourceDesignerView;
        }

		public override string[] GetViewNames() 
        {
			return viewNames;
		}

        private esDataSourceDesignerView dataView;

        #endregion

        public override bool CanConfigure
        {
            get { return true;}
        }

        public override bool CanRefreshSchema
        {
            get
            {
                return false;
            }
        }

        public override void RefreshSchema(bool preferSilent)
        {
            SuppressDataSourceEvents();

            ResumeDataSourceEvents();
        }

        public override void Configure()
        {
            IServiceProvider provider = base.Component.Site;
            if (provider == null) return;

            IUIService uiService = (IUIService)provider.GetService(typeof(IUIService));
            if (uiService == null) return;

            esDataSourceDesignerView view = this.GetView() as esDataSourceDesignerView;

            esDataSourceWizard wiz = new esDataSourceWizard(provider, esDataSource);

            try
            {
                wiz.esColumnCollection = this.DesignerState["esColumnCollection"] as esColumnMetadataCollection;
                wiz.esCollectionName = this.DesignerState["esCollectionName"] as string;
                wiz.SelectedColumns = this.DesignerState["SelectedColumns"] as List<string>;
            }
            catch { }

            DialogResult result = uiService.ShowDialog(wiz);

            if (result == DialogResult.OK)
            {
                view.esColumnCollection = wiz.esColumnCollection;
                view.esCollectionName = wiz.esCollectionName;
                view.SelectedColumns = wiz.SelectedColumns;

                this.DesignerState["esColumnCollection"] = wiz.esColumnCollection;
                this.DesignerState["esCollectionName"] = wiz.esCollectionName;
                this.DesignerState["SelectedColumns"] = wiz.SelectedColumns;

                this.OnSchemaRefreshed(EventArgs.Empty);
            }
        }

        public event EventHandler DataSourceChanged
        {
            add
            {
                base.DataSourceChanged += value;
            }
            remove
            {
                base.DataSourceChanged -= value;
            }
        }
    }
}
