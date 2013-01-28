namespace Tiraggo.DebuggerVisualizer
{
    partial class esVisualizerForm
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.CollectionDataGridView = new System.Windows.Forms.DataGridView();
            this.txtLastQuery = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.CollectionDataGridView)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // CollectionDataGridView
            // 
            this.CollectionDataGridView.AllowUserToOrderColumns = true;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.CollectionDataGridView.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.CollectionDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.CollectionDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CollectionDataGridView.Location = new System.Drawing.Point(2, 2);
            this.CollectionDataGridView.Margin = new System.Windows.Forms.Padding(2);
            this.CollectionDataGridView.Name = "CollectionDataGridView";
            this.CollectionDataGridView.RowTemplate.Height = 24;
            this.CollectionDataGridView.Size = new System.Drawing.Size(590, 346);
            this.CollectionDataGridView.TabIndex = 0;
            // 
            // txtLastQuery
            // 
            this.txtLastQuery.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLastQuery.Location = new System.Drawing.Point(3, 353);
            this.txtLastQuery.Multiline = true;
            this.txtLastQuery.Name = "txtLastQuery";
            this.txtLastQuery.ReadOnly = true;
            this.txtLastQuery.Size = new System.Drawing.Size(588, 99);
            this.txtLastQuery.TabIndex = 1;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.txtLastQuery, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.CollectionDataGridView, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 76.92308F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 23.07692F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(594, 455);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // esVisualizerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(594, 455);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "esVisualizerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "EntitySpaces esVisualizerForm";
            ((System.ComponentModel.ISupportInitialize)(this.CollectionDataGridView)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView CollectionDataGridView;
        private System.Windows.Forms.TextBox txtLastQuery;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;

    }
}