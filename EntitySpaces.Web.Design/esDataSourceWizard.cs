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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Web.UI;
using System.Windows.Forms;

using Tiraggo.Core;
using Tiraggo.Interfaces;


namespace Tiraggo.Web.Design
{
    public partial class esDataSourceWizard : Form
    {
        private DataSourceControl esDataSource;
        private IServiceProvider provider;
        private esReflectionHelper helper;
        private Dictionary<string, tgEntityCollectionBase> collections = new Dictionary<string, tgEntityCollectionBase>();

        public esColumnMetadataCollection esColumnCollection;
        public string esCollectionName;
        public List<string> SelectedColumns;


        public esDataSourceWizard(IServiceProvider provider, DataSourceControl esDataSource)
        {
            InitializeComponent();

            this.esDataSource = esDataSource;
            this.provider = provider;
            this.helper = new esReflectionHelper();

            //----------------------------------------------------
            // Let's see if we can load our types right up !!
            //----------------------------------------------------
            ITypeDiscoveryService discovery = null;
            if (esDataSource.Site != null)
                discovery = (ITypeDiscoveryService)esDataSource.Site.GetService(typeof(ITypeDiscoveryService));

            ICollection types = discovery.GetTypes(typeof(tgEntityCollectionBase), true);

            foreach (Type type in types)
            {
                if (type.IsClass && !type.IsAbstract)
                {
                    if (type.IsSubclassOf(typeof(tgEntityCollectionBase)))
                    {
                        try
                        {
                            tgEntityCollectionBase coll = Activator.CreateInstance(type) as tgEntityCollectionBase;

                            if (coll != null)
                            {
                                collections[type.Name] = coll;
                            }
                        }
                        catch { }
                    }
                }
            }

            foreach (string collectionName in collections.Keys)
            {
                this.lboxCollections.Items.Add(collectionName);
            }
        }

        private void btnAssemblyFilename_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = this.dlgFileOpen.ShowDialog();
                if (result == DialogResult.OK)
                {
                    this.txtAssemblyFilename.Text = this.dlgFileOpen.FileName;

                    this.lboxCollections.Items.Clear();

                    collections = helper.GetCollections(this.txtAssemblyFilename.Text);

                    foreach (string collectionName in collections.Keys)
                    {
                        this.lboxCollections.Items.Add(collectionName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void bntOk_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.lboxCollections.SelectedIndex > -1)
                {
                    this.esCollectionName = this.lboxCollections.SelectedItem.ToString();
                    this.esColumnCollection = helper.GetColumns(this.collections[this.esCollectionName]);

                    this.SelectedColumns = new List<string>();
                    foreach (string columnName in this.chkColumns.CheckedItems)
                    {
                        this.SelectedColumns.Add(columnName);
                    }
                }
            }
            catch { }
        }

        private void lboxCollections_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                this.chkColumns.Items.Clear();

                string collectionName = this.lboxCollections.SelectedItem.ToString();
                this.esColumnCollection = helper.GetColumns(this.collections[collectionName]);

                foreach (esColumnMetadata col in this.esColumnCollection)
                {
                    this.chkColumns.Items.Add(col.PropertyName);
                }

                for (int i = 0; i < this.chkColumns.Items.Count; i++)
                {
                    this.chkColumns.SetItemChecked(i, true);
                }
            }
            catch { }
        }

        private void chkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            if (this.chkSelectAll.Checked)
            {
                this.chkDeselectAll.Checked = false;

                for (int i = 0; i < this.chkColumns.Items.Count; i++)
                {
                    this.chkColumns.SetItemChecked(i, true);
                }
            }
        }

        private void chkDeselectAll_CheckedChanged(object sender, EventArgs e)
        {
            if (this.chkDeselectAll.Checked)
            {
                this.chkSelectAll.Checked = false;

                for (int i = 0; i < this.chkColumns.Items.Count; i++)
                {
                    this.chkColumns.SetItemChecked(i, false);
                }
            }
        }

        private void esDataSourceWizard_Load(object sender, EventArgs e)
        {
            if (this.esCollectionName != null)
            {
                int index = this.lboxCollections.Items.IndexOf(this.esCollectionName);

                if (index >= 0)
                {
                    this.lboxCollections.SelectedIndex = index;

                    this.chkSelectAll.Checked = false;
                    for (int i = 0; i < this.chkColumns.Items.Count; i++)
                    {
                        this.chkColumns.SetItemChecked(i, false);
                    }

                    foreach (string columnName in this.SelectedColumns)
                    {
                        index = this.chkColumns.Items.IndexOf(columnName);

                        if (index >= 0)
                        {
                            this.chkColumns.SetItemChecked(index, true);
                        }
                    }
                }
            }
        }
    }
}