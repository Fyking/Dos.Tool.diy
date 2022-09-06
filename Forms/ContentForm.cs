using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using Dos.Common;
using Dos.DbObjects;

namespace Dos.Tools
{
    public partial class ContentForm : WeifenLuo.WinFormsUI.Docking.DockContent
    {
        public ContentForm()
        {
            InitializeComponent();
            #region 加载模板
            var tpls = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Template")).GetFiles("*.tpl", SearchOption.AllDirectories);
            foreach (var fileInfo in tpls)
            {
                if (fileInfo.Name.Contains("实体类_最新"))
                {
                    tplComboBox.Items.Insert(0, fileInfo.Name);
                    continue;
                }
                tplComboBox.Items.Add(fileInfo.Name);
            }
            tplComboBox.SelectedIndex = 0;
            var tpl = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Template", tplComboBox.SelectedText);
            if (File.Exists(tpl))
            {
                tplContent.Text = FileHelper.Read(tpl);
            }
            #endregion
        }

        public string Content
        {
            get { return content; }
            set { content = value; }
        }

        private string content;


        private Model.Connection connectionModel;

        public Model.Connection ConnectionModel
        {
            set { connectionModel = value; }
            get { return connectionModel; }
        }

        private string tableName;
        public string TableName
        {
            get { return tableName; }
            set { tableName = value; }
        }

        private bool isView = false;
        public bool IsView
        {
            get { return isView; }
            set { isView = value; }
        }

        private string databaseName;
        public string DatabaseName
        {
            get { return databaseName; }
            set { databaseName = value; }
        }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContentForm_Load(object sender, EventArgs e)
        {
            IDbObject dbObject = null;
            if (ConnectionModel.DbType.Equals(Dos.ORM.DatabaseType.SqlServer.ToString()))
            {
                dbObject = new Dos.DbObjects.SQL2000.DbObject(ConnectionModel.ConnectionString);
            }
            else if (ConnectionModel.DbType.Equals(Dos.ORM.DatabaseType.SqlServer9.ToString()))
            {
                dbObject = new Dos.DbObjects.SQL2005.DbObject(ConnectionModel.ConnectionString);
            }
            else if (ConnectionModel.DbType.Equals(Dos.ORM.DatabaseType.MsAccess.ToString()))
            {
                dbObject = new Dos.DbObjects.OleDb.DbObject(ConnectionModel.ConnectionString);
            }
            else if (ConnectionModel.DbType.Equals(Dos.ORM.DatabaseType.Oracle.ToString()))
            {
                dbObject = new Dos.DbObjects.Oracle.DbObject(ConnectionModel.ConnectionString);
            }
            else if (ConnectionModel.DbType.Equals(Dos.ORM.DatabaseType.Sqlite3.ToString()))
            {
                dbObject = new Dos.DbObjects.SQLite.DbObject(ConnectionModel.ConnectionString);
            }
            else if (ConnectionModel.DbType.Equals(Dos.ORM.DatabaseType.MySql.ToString()))
            {
                dbObject = new Dos.DbObjects.MySQL.DbObject(ConnectionModel.ConnectionString);
            }
            else
            {
                MessageBox.Show("未知数据库类型!");
                return;
            }
            columnsdt = dbObject.GetColumnInfoList(DatabaseName, TableName);

            gridColumns.DataSource = columnsdt;

            DataTable primarykeydt = dbObject.GetKeyName(DatabaseName, TableName);

            cbPrimarykey.Items.Clear();

            if (null != primarykeydt && primarykeydt.Rows.Count > 0)
            {
                foreach (DataRow dr in primarykeydt.Rows)
                {
                    cbPrimarykey.Items.Add(dr["ColumnName"].ToString());
                }

                cbPrimarykey.SelectedIndex = 0;
            }

            txtClassName.Text = TableName.Trim().Replace(' ', '_');
            txtnamespace.Text = Utils.ReadNamespace();

        }

        DataTable columnsdt = null;

        /// <summary>
        /// 添加主键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddPrimarykey_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection rows = gridColumns.SelectedRows;
            if (null != rows && rows.Count > 0)
            {
                foreach (DataGridViewRow row in rows)
                {
                    object temp = row.Cells[1].Value;

                    if (!cbPrimarykey.Items.Contains(temp))
                    {
                        cbPrimarykey.Items.Add(temp);
                    }
                }

                cbPrimarykey.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 删除主键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRemovePrimarykey_Click(object sender, EventArgs e)
        {
            if (cbPrimarykey.SelectedIndex >= 0)
            {
                cbPrimarykey.Items.RemoveAt(cbPrimarykey.SelectedIndex);
            }

            if (cbPrimarykey.Items.Count > 0)
                cbPrimarykey.SelectedIndex = 0;
        }


        /// <summary>
        /// 代码生成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtnamespace.Text))
            {
                MessageBox.Show("命名空间不能为空!");
                return;
            }
            if (string.IsNullOrEmpty(txtClassName.Text))
            {
                MessageBox.Show("类名不能为空!");
                return;
            }

            Utils.WriteNamespace(txtnamespace.Text);

            List<Model.ColumnInfo> columns = Utils.GetColumnInfos(columnsdt);

            foreach (Model.ColumnInfo col in columns)
            {

                col.IsPK = false;

                foreach (object o in cbPrimarykey.Items)
                {
                    if (col.ColumnName.Equals(o.ToString()))
                    {
                        col.IsPK = true;
                        break;
                    }
                }
            }
            Dictionary<string, string> csList = new Dictionary<string, string>(), fsList = new Dictionary<string, string>();
            EntityBuilder builder = new EntityBuilder(TableName, txtnamespace.Text, txtClassName.Text, columns, IsView, cbToupperFrstword.Checked, ConnectionModel.DbType);
            if (cbAllTemp.Checked)
            {
                foreach (var item in tplComboBox.Items)
                {
                    var tpl = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Template", item.ToString());
                    if (File.Exists(tpl)) fsList.Add(Path.GetFileNameWithoutExtension(tpl), FileHelper.Read(tpl));
                }
                foreach (var item in fsList) csList.Add(item.Key.Replace("AModel", ""), builder.Builder(item.Value, item.Key.Replace("AModel", "")));
                var filePath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Model");
                if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
                foreach (var item in csList) File.WriteAllText($"{filePath}/{TableName}{item.Key}.cs", item.Value);
                if (MessageBox.Show("导出完成，导出文件在Model文件夹。是否打开文件夹？", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes) System.Diagnostics.Process.Start("Explorer.exe", filePath);
            }
            else
            {
                txtContent.Text = builder.Builder(tplContent.Text);
                tabControl1.SelectedIndex = 1;
            }
        }


        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveEntity.FileName = txtClassName.Text;
            saveEntity.Filter = "CS 文件|*.cs";

            if (saveEntity.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(saveEntity.FileName, false, Encoding.UTF8))
                {
                    sw.Write(txtContent.Text);
                    sw.Close();
                }
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((sender as TabControl).SelectedTab.Text == "生成代码" && string.IsNullOrEmpty(txtContent.Text))
            {
                button1_Click(null, null);
            }
        }

        private void tplComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tpl = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Template", (sender as ComboBox).SelectedItem.ToString());
            tplContent.Text = FileHelper.Read(tpl);
        }

        private void btnSql_Click(object sender, EventArgs e)
        {
            if (columnsdt != null && columnsdt.Columns.Count > 0)
            {
                var cols = new List<string>();
                var key = cbPrimarykey.SelectedItem.ToString();
                foreach (DataRow item in columnsdt.Rows) cols.Add(item["ColumnName"].ToString());
                var del_cols = cols.Where(col => col.ToLower().Contains("delete") || col.ToLower().Contains("update") || col.ToLower().Contains("modify"));
                string sql_drop = $"DROP TABLE {TableName}";
                string sql_insert = $"INSERT INTO {TableName}({string.Join(",", cols)}) VALUES(@{string.Join(",@", cols)})";
                string sql_update = $"UPDATE {TableName} SET {string.Join(",", cols.Select(col => $"{col}=@{col}"))} WHERE {key}=@{key}";
                string sql_delete_fake = $"UPDATE {TableName} SET {string.Join(",", del_cols.Select(col => $"{col}=@{col}"))} IsDelete=1 WHERE {key}=@{key}";
                string sql_delete = $"DELETE FROM {TableName} WHERE {key}=@{key}";
                //生成T-SQL页面
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"--删除{TableName}表\n{sql_drop}\n");

                builder.AppendLine($"--插入{TableName}表数据\n{sql_insert}\n");
                builder.AppendLine($"--更新{TableName}表数据\n{sql_update}\n");
                builder.AppendLine($"--假删除{TableName}表数据\n{sql_delete_fake}\n");

                builder.AppendLine($"--删除{TableName}表数据\n{sql_delete}\n");

                txtContent.Text = builder.ToString();
                tabControl1.SelectedIndex = 1;
            }
        }
    }
}
