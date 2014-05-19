namespace SensMaster
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.pmsDataSet = new SensMaster.pmsDataSet();
            this.readerBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.readerTableAdapter = new SensMaster.pmsDataSetTableAdapters.ReaderTableAdapter();
            this.readerCodeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.currentReaderIPDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lastHealthCheckedDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.statusDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pmsDataSet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.readerBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AutoGenerateColumns = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.readerCodeDataGridViewTextBoxColumn,
            this.currentReaderIPDataGridViewTextBoxColumn,
            this.lastHealthCheckedDataGridViewTextBoxColumn,
            this.statusDataGridViewTextBoxColumn});
            this.dataGridView1.DataSource = this.readerBindingSource;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(907, 461);
            this.dataGridView1.TabIndex = 1;
            // 
            // pmsDataSet
            // 
            this.pmsDataSet.DataSetName = "pmsDataSet";
            this.pmsDataSet.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // readerBindingSource
            // 
            this.readerBindingSource.DataMember = "Reader";
            this.readerBindingSource.DataSource = this.pmsDataSet;
            // 
            // readerTableAdapter
            // 
            this.readerTableAdapter.ClearBeforeFill = true;
            // 
            // readerCodeDataGridViewTextBoxColumn
            // 
            this.readerCodeDataGridViewTextBoxColumn.DataPropertyName = "readerCode";
            this.readerCodeDataGridViewTextBoxColumn.HeaderText = "readerCode";
            this.readerCodeDataGridViewTextBoxColumn.Name = "readerCodeDataGridViewTextBoxColumn";
            // 
            // currentReaderIPDataGridViewTextBoxColumn
            // 
            this.currentReaderIPDataGridViewTextBoxColumn.DataPropertyName = "currentReaderIP";
            this.currentReaderIPDataGridViewTextBoxColumn.HeaderText = "currentReaderIP";
            this.currentReaderIPDataGridViewTextBoxColumn.Name = "currentReaderIPDataGridViewTextBoxColumn";
            // 
            // lastHealthCheckedDataGridViewTextBoxColumn
            // 
            this.lastHealthCheckedDataGridViewTextBoxColumn.DataPropertyName = "lastHealthChecked";
            this.lastHealthCheckedDataGridViewTextBoxColumn.HeaderText = "lastHealthChecked";
            this.lastHealthCheckedDataGridViewTextBoxColumn.Name = "lastHealthCheckedDataGridViewTextBoxColumn";
            // 
            // statusDataGridViewTextBoxColumn
            // 
            this.statusDataGridViewTextBoxColumn.DataPropertyName = "status";
            this.statusDataGridViewTextBoxColumn.HeaderText = "status";
            this.statusDataGridViewTextBoxColumn.Name = "statusDataGridViewTextBoxColumn";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(907, 461);
            this.Controls.Add(this.dataGridView1);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pmsDataSet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.readerBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private pmsDataSet pmsDataSet;
        private System.Windows.Forms.BindingSource readerBindingSource;
        private pmsDataSetTableAdapters.ReaderTableAdapter readerTableAdapter;
        private System.Windows.Forms.DataGridViewTextBoxColumn readerCodeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn currentReaderIPDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn lastHealthCheckedDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn statusDataGridViewTextBoxColumn;
    }
}

