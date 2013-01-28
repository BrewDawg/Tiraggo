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


namespace Tiraggo.Web.Design
{
    partial class esDataSourceWizard
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.bntOk = new System.Windows.Forms.Button();
            this.dlgFileOpen = new System.Windows.Forms.OpenFileDialog();
            this.btnAssemblyFilename = new System.Windows.Forms.Button();
            this.txtAssemblyFilename = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lboxCollections = new System.Windows.Forms.ListBox();
            this.chkColumns = new System.Windows.Forms.CheckedListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.chkSelectAll = new System.Windows.Forms.CheckBox();
            this.chkDeselectAll = new System.Windows.Forms.CheckBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // bntOk
            // 
            this.bntOk.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.bntOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bntOk.Location = new System.Drawing.Point(434, 394);
            this.bntOk.Name = "bntOk";
            this.bntOk.Size = new System.Drawing.Size(75, 23);
            this.bntOk.TabIndex = 0;
            this.bntOk.Text = "Ok";
            this.bntOk.UseVisualStyleBackColor = true;
            this.bntOk.Click += new System.EventHandler(this.bntOk_Click);
            // 
            // btnAssemblyFilename
            // 
            this.btnAssemblyFilename.Location = new System.Drawing.Point(473, 113);
            this.btnAssemblyFilename.Name = "btnAssemblyFilename";
            this.btnAssemblyFilename.Size = new System.Drawing.Size(36, 23);
            this.btnAssemblyFilename.TabIndex = 23;
            this.btnAssemblyFilename.Text = ". . .";
            this.btnAssemblyFilename.UseVisualStyleBackColor = true;
            this.btnAssemblyFilename.Click += new System.EventHandler(this.btnAssemblyFilename_Click);
            // 
            // txtAssemblyFilename
            // 
            this.txtAssemblyFilename.Location = new System.Drawing.Point(25, 116);
            this.txtAssemblyFilename.Name = "txtAssemblyFilename";
            this.txtAssemblyFilename.Size = new System.Drawing.Size(433, 20);
            this.txtAssemblyFilename.TabIndex = 22;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 100);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 13);
            this.label1.TabIndex = 19;
            this.label1.Text = "Assembly:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(22, 150);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(36, 13);
            this.label3.TabIndex = 19;
            this.label3.Text = "Entity:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(353, 394);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // lboxCollections
            // 
            this.lboxCollections.FormattingEnabled = true;
            this.lboxCollections.Location = new System.Drawing.Point(25, 166);
            this.lboxCollections.Name = "lboxCollections";
            this.lboxCollections.Size = new System.Drawing.Size(484, 69);
            this.lboxCollections.Sorted = true;
            this.lboxCollections.TabIndex = 24;
            this.lboxCollections.SelectedIndexChanged += new System.EventHandler(this.lboxCollections_SelectedIndexChanged);
            // 
            // chkColumns
            // 
            this.chkColumns.CheckOnClick = true;
            this.chkColumns.FormattingEnabled = true;
            this.chkColumns.Location = new System.Drawing.Point(25, 263);
            this.chkColumns.MultiColumn = true;
            this.chkColumns.Name = "chkColumns";
            this.chkColumns.Size = new System.Drawing.Size(484, 124);
            this.chkColumns.TabIndex = 27;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 247);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 13);
            this.label2.TabIndex = 19;
            this.label2.Text = "Columns:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // chkSelectAll
            // 
            this.chkSelectAll.AutoSize = true;
            this.chkSelectAll.BackColor = System.Drawing.Color.Transparent;
            this.chkSelectAll.Checked = true;
            this.chkSelectAll.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSelectAll.Location = new System.Drawing.Point(189, 398);
            this.chkSelectAll.Name = "chkSelectAll";
            this.chkSelectAll.Size = new System.Drawing.Size(70, 17);
            this.chkSelectAll.TabIndex = 30;
            this.chkSelectAll.Text = "Select All";
            this.chkSelectAll.UseVisualStyleBackColor = false;
            this.chkSelectAll.CheckedChanged += new System.EventHandler(this.chkSelectAll_CheckedChanged);
            // 
            // chkDeselectAll
            // 
            this.chkDeselectAll.AutoSize = true;
            this.chkDeselectAll.BackColor = System.Drawing.Color.Transparent;
            this.chkDeselectAll.Location = new System.Drawing.Point(265, 398);
            this.chkDeselectAll.Name = "chkDeselectAll";
            this.chkDeselectAll.Size = new System.Drawing.Size(82, 17);
            this.chkDeselectAll.TabIndex = 30;
            this.chkDeselectAll.Text = "Unselect All";
            this.chkDeselectAll.UseVisualStyleBackColor = false;
            this.chkDeselectAll.CheckedChanged += new System.EventHandler(this.chkDeselectAll_CheckedChanged);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::Tiraggo.Web.Design.Properties.Resources.newlogo;
            this.pictureBox1.Location = new System.Drawing.Point(-2, 2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(226, 95);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 28;
            this.pictureBox1.TabStop = false;
            // 
            // esDataSourceWizard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(536, 429);
            this.Controls.Add(this.chkDeselectAll);
            this.Controls.Add(this.chkSelectAll);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.chkColumns);
            this.Controls.Add(this.lboxCollections);
            this.Controls.Add(this.btnAssemblyFilename);
            this.Controls.Add(this.txtAssemblyFilename);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.bntOk);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "esDataSourceWizard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Configure the EntitySpaces DataSource";
            this.Load += new System.EventHandler(this.esDataSourceWizard_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bntOk;
        private System.Windows.Forms.OpenFileDialog dlgFileOpen;
        private System.Windows.Forms.Button btnAssemblyFilename;
        private System.Windows.Forms.TextBox txtAssemblyFilename;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ListBox lboxCollections;
        private System.Windows.Forms.CheckedListBox chkColumns;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chkSelectAll;
        private System.Windows.Forms.CheckBox chkDeselectAll;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}