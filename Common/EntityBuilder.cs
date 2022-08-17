﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using Dos.Tools.Model;
using System.Xml;
using System.Windows.Forms;
using Dos.Common;
using Dos.ORM;
using Dos.ORM.Common;
using RazorEngine;
using RazorEngine.Templating;
using System.Text.RegularExpressions;

namespace Dos.Tools
{
    public class EntityBuilder
    {
        private List<Model.ColumnInfo> _columns = new List<Dos.Tools.Model.ColumnInfo>();

        private string _tableName;
        private string _dbType;

        private string _nameSpace = "Dos.Model";

        private string _className;

        private bool _isView = false;

        private bool _isSZMDX = false;

        public EntityBuilder(string tableName, string nameSpace, string className, List<Model.ColumnInfo> columns, bool isView)
            : this(tableName, nameSpace, className, columns, isView, false)
        {
            
        }

        public EntityBuilder(string tableName, string nameSpace, string className, List<Model.ColumnInfo> columns, bool isView, bool isSZMDX, string dbType = null)
        {
            _isSZMDX = isSZMDX;
            _className = Utils.ReplaceSpace(className);
            _nameSpace = Utils.ReplaceSpace(nameSpace);
            _tableName = tableName;
            _dbType = dbType;
            if (_isSZMDX)
            {
                _className = Utils.ToUpperFirstword(_className);
            }
            _isView = isView;



            foreach (Model.ColumnInfo col in columns)
            {
                col.ColumnName = Utils.ReplaceSpace(col.ColumnName);
                if (_isSZMDX)
                {
                    col.ColumnName = Utils.ToUpperFirstword(col.ColumnName);
                }

                col.DeText = Utils.ReplaceSpace(col.DeText);
                _columns.Add(col);
            }

        }

        public List<Model.ColumnInfo> Columns
        {
            get { return _columns; }
            set { _columns = value; }
        }
        public string TableName
        {
            get { return _tableName; }
            set { _tableName = value; }
        }
        public string NameSpace
        {
            get { return _nameSpace; }
            set { _nameSpace = value; }
        }
        public string ClassName
        {
            get { return _className; }
            set { _className = value; }
        }
        public string DbType
        {
            get { return _dbType; }
            set { _dbType = value; }
        }
        public bool IsView
        {
            get { return _isView; }
            set { _isView = value; }
        }
        public abstract class MyCustomTemplateBase<T> : TemplateBase<T>
        {
            public bool IsLast(object item, List<object> list)
            {
                return list.FindIndex(d => d == item) == (list.Count - 1);
            }
        }

        /// <summary>
        /// 使用模板生成代码
        /// </summary>
        /// <param name="tplContent">模板文件的内容</param>
        /// <returns></returns>
        public string Builder(string tplContent,string keyName)
        {
            Columns = DbToCS.DbtoCSColumns(Columns, DbType);
            if (!string.IsNullOrWhiteSpace(tplContent))
            {
                #region 模板生成
                //var template = FileHelper.Read(@"D:\工作\GitHub\Dos.Tools\bin\Debug\Template\实体类_最新.tpl");
                var primarykeyColumns = Columns.FindAll(col => col.IsPK);
                var identityColumn = Columns.Find(col => col.IsIdentity);
                //StringPlus plus = new StringPlus();
                //plus.AppendLine("//------------------------------------------------------------------------------");
                //plus.AppendLine("// <auto-generated>");
                //plus.AppendLine("//     此代码由工具生成。");
                //plus.AppendLine("//     运行时版本:" + Environment.Version.ToString());
                //plus.AppendLine("//     Website: http://ITdos.com/Dos/ORM/Index.html");
                //plus.AppendLine("//     对此文件的更改可能会导致不正确的行为，并且如果");
                //plus.AppendLine("//     重新生成代码，这些更改将会丢失。");
                //plus.AppendLine("// </auto-generated>");
                //plus.AppendLine("//------------------------------------------------------------------------------");
                //plus.AppendLine();
                Columns.ForEach(v => {
                    var coll = Regex.Matches(v.ColumnName, "[A-Z]{1,}[0-9a-z]*");
                    foreach (Match item in coll)
                    {
                        v.ColumnName2 +=(item.Index > 0?"_":"") + item.Value;
                        v.ColumnName3 +=(item.Index > 0?"-":"") + item.Value;
                    }
                });
                var result = Engine.Razor.RunCompile(tplContent,DateTime.Now.ToString("yyMMddHHmmss"),null, new
                {
                    ClassName = ClassName,
                    TableName = TableName,
                    Columns = Columns,
                    NameSpace = NameSpace,
                    PrimaryKeyColumns = primarykeyColumns,
                    IdentityColumn = identityColumn,
                    i1 = 1,
                    i2 = 1,
                    i3 = 1
                });
                return result;
                #endregion
            }
            return "请使用模板生成方式";
        }

        delegate Regex CRegeg(string name);
        delegate List<string> CList(MatchCollection coll);

        private string BuilderModel()
        {
            StringPlus plus = new StringPlus();
            StringPlus plus2 = new StringPlus();
            StringPlus plus3 = new StringPlus();
            plus.AppendSpaceLine(2, "#region Model");
            foreach (ColumnInfo column in Columns)
            {
                //2015-09-09去除生成默认值功能
                if (false)//!string.IsNullOrWhiteSpace(column.DefaultVal.Replace("\"", "").Replace("'", "").Replace("(", "").Replace(")", "").Replace(" ", ""))
                {
                    #region 生成默认值
                    var defaultVal =
                        column.DefaultVal.Replace("\"", "")
                            .Replace("'", "")
                            .Replace("(", "")
                            .Replace(")", "")
                            .Replace(" ", "");
                    var val = "";
                    if (column.TypeName.ToLower().Contains("bool"))
                    {
                        switch (val)
                        {
                            case "b'0'":
                                val = "0";
                                break;
                            case "b'1'":
                                val = "1";
                                break;
                        }
                        val = DataUtils.ConvertValue<bool>(column.DefaultVal) ? "true" : "false";
                    }
                    else if (column.TypeName.ToLower().Contains("string"))
                    {
                        val = "\"" + column.DefaultVal + "\"";
                    }
                    else if (column.TypeName.ToLower().Contains("guid"))
                    {
                        Guid tempGuid;
                        if (Guid.TryParse(column.DefaultVal, out tempGuid))
                        {
                            val = "Guid.Parse(\"" + column.DefaultVal + "\")";
                        }
                        else
                        {
                            val = "Guid.NewGuid()";
                        }
                    }
                    else if (column.TypeName.ToLower().Contains("int"))
                    {
                        val = column.DefaultVal.Replace("\"", "").Replace("'", "").Replace("(", "").Replace(")", "").Replace(" ", "");
                    }
                    else if (column.TypeName.ToLower().Contains("decimal"))
                    {
                        val = column.DefaultVal.Replace("\"", "").Replace("'", "").Replace("(", "").Replace(")", "").Replace(" ", "") + "M";
                    }
                    else if (column.TypeName.ToLower().Contains("float"))
                    {
                        val = column.DefaultVal.Replace("\"", "").Replace("'", "").Replace("(", "").Replace(")", "").Replace(" ", "") + "F";
                    }
                    if (string.IsNullOrWhiteSpace(val))
                    {
                        plus2.AppendSpaceLine(2, "private " + column.TypeName + " _" + column.ColumnName + ";");
                    }
                    else
                    {
                        plus2.AppendSpaceLine(2, "private " + column.TypeName + " _" + column.ColumnName + " = " + val + ";");
                    }
                    #endregion
                }
                else
                {
                    plus2.AppendSpaceLine(2, "private " + column.TypeName + " _" + column.ColumnName + ";");
                }
                plus3.AppendSpaceLine(2, "/// <summary>");
                plus3.AppendSpaceLine(2, "/// " + column.DeText);
                plus3.AppendSpaceLine(2, "/// </summary>");
                plus3.AppendSpaceLine(2, "public " + column.TypeName + " " + column.ColumnName);
                plus3.AppendSpaceLine(2, "{");
                plus3.AppendSpaceLine(3, "get{ return _" + column.ColumnName + "; }");
                plus3.AppendSpaceLine(3, "set");
                plus3.AppendSpaceLine(3, "{");
                plus3.AppendSpaceLine(4, "this.OnPropertyValueChange(_." + column.ColumnName + ",_" + column.ColumnName + ",value);");
                plus3.AppendSpaceLine(4, "this._" + column.ColumnName + "=value;");
                plus3.AppendSpaceLine(3, "}");
                plus3.AppendSpaceLine(2, "}");
            }
            plus.Append(plus2.Value);
            plus.Append(plus3.Value);
            plus.AppendSpaceLine(2, "#endregion");

            return plus.ToString();


        }



        private string BuilderMethod()
        {
            StringPlus plus = new StringPlus();


            plus.AppendSpaceLine(2, "#region Method");


            //只读
            if (IsView)
            {
                plus.AppendSpaceLine(2, "/// <summary>");
                plus.AppendSpaceLine(2, "/// 是否只读");
                plus.AppendSpaceLine(2, "/// </summary>");
                plus.AppendSpaceLine(2, "public override bool IsReadOnly()");
                plus.AppendSpaceLine(2, "{");
                plus.AppendSpaceLine(3, "return true;");
                plus.AppendSpaceLine(2, "}");
            }

            Model.ColumnInfo identityColumn = Columns.Find(delegate (Model.ColumnInfo col) { return col.IsIdentity; });
            if (null != identityColumn)
            {
                plus.AppendSpaceLine(2, "/// <summary>");
                plus.AppendSpaceLine(2, "/// 获取实体中的标识列");
                plus.AppendSpaceLine(2, "/// </summary>");
                plus.AppendSpaceLine(2, "public override Field GetIdentityField()");
                plus.AppendSpaceLine(2, "{");
                plus.AppendSpaceLine(3, "return _." + identityColumn.ColumnName + ";");
                plus.AppendSpaceLine(2, "}");
            }

            List<Model.ColumnInfo> primarykeyColumns = Columns.FindAll(delegate (Model.ColumnInfo col) { return col.IsPK; });
            if (null != primarykeyColumns && primarykeyColumns.Count > 0)
            {
                plus.AppendSpaceLine(2, "/// <summary>");
                plus.AppendSpaceLine(2, "/// 获取实体中的主键列");
                plus.AppendSpaceLine(2, "/// </summary>");
                plus.AppendSpaceLine(2, "public override Field[] GetPrimaryKeyFields()");
                plus.AppendSpaceLine(2, "{");
                plus.AppendSpaceLine(3, "return new Field[] {");
                StringPlus plus2 = new StringPlus();
                foreach (Model.ColumnInfo col in primarykeyColumns)
                {
                    plus2.AppendSpaceLine(4, "_." + col.ColumnName + ",");
                }
                plus.Append(plus2.ToString().TrimEnd().Substring(0, plus2.ToString().TrimEnd().Length - 1));
                plus.AppendLine("};");
                plus.AppendSpaceLine(2, "}");
            }



            plus.AppendSpaceLine(2, "/// <summary>");
            plus.AppendSpaceLine(2, "/// 获取列信息");
            plus.AppendSpaceLine(2, "/// </summary>");
            plus.AppendSpaceLine(2, "public override Field[] GetFields()");
            plus.AppendSpaceLine(2, "{");
            plus.AppendSpaceLine(3, "return new Field[] {");
            StringPlus plus3 = new StringPlus();
            foreach (ColumnInfo col in Columns)
            {
                plus3.AppendSpaceLine(4, "_." + col.ColumnName + ",");
            }
            plus.Append(plus3.ToString().TrimEnd().Substring(0, plus3.ToString().TrimEnd().Length - 1));
            plus.AppendLine("};");
            plus.AppendSpaceLine(2, "}");


            plus.AppendSpaceLine(2, "/// <summary>");
            plus.AppendSpaceLine(2, "/// 获取值信息");
            plus.AppendSpaceLine(2, "/// </summary>");
            plus.AppendSpaceLine(2, "public override object[] GetValues()");
            plus.AppendSpaceLine(2, "{");
            plus.AppendSpaceLine(3, "return new object[] {");
            StringPlus plus4 = new StringPlus();
            foreach (ColumnInfo col in Columns)
            {
                plus4.AppendSpaceLine(4, "this._" + col.ColumnName + ",");
            }
            plus.Append(plus4.ToString().TrimEnd().Substring(0, plus4.ToString().TrimEnd().Length - 1));
            plus.AppendLine("};");
            plus.AppendSpaceLine(2, "}");

            //2015-08-10注释
            //plus.AppendSpaceLine(2, "/// <summary>");
            //plus.AppendSpaceLine(2, "/// 给当前实体赋值");
            //plus.AppendSpaceLine(2, "/// </summary>");
            //plus.AppendSpaceLine(2, "public override void SetPropertyValues(IDataReader reader)");
            //plus.AppendSpaceLine(2, "{");
            //foreach (ColumnInfo col in Columns)
            //{
            //    plus.AppendSpaceLine(3, "this._" + col.ColumnName + " = DataUtils.ConvertValue<" + col.TypeName + ">(reader[\"" + col.ColumnNameRealName + "\"]);");
            //}
            //plus.AppendSpaceLine(2, "}");


            //2015-08-10注释
            //plus.AppendSpaceLine(2, "/// <summary>");
            //plus.AppendSpaceLine(2, "/// 给当前实体赋值");
            //plus.AppendSpaceLine(2, "/// </summary>");
            //plus.AppendSpaceLine(2, "public override void SetPropertyValues(DataRow row)");
            //plus.AppendSpaceLine(2, "{");
            //foreach (ColumnInfo col in Columns)
            //{
            //    plus.AppendSpaceLine(3, "this._" + col.ColumnName + " = DataUtils.ConvertValue<" + col.TypeName + ">(row[\"" + col.ColumnNameRealName + "\"]);");
            //}
            //plus.AppendSpaceLine(2, "}");


            plus.AppendSpaceLine(2, "#endregion");
            plus.AppendLine();



            plus.AppendSpaceLine(2, "#region _Field");
            plus.AppendSpaceLine(2, "/// <summary>");
            plus.AppendSpaceLine(2, "/// 字段信息");
            plus.AppendSpaceLine(2, "/// </summary>");
            plus.AppendSpaceLine(2, "public class _");
            plus.AppendSpaceLine(2, "{");
            plus.AppendSpaceLine(3, "/// <summary>");
            plus.AppendSpaceLine(3, "/// * ");
            plus.AppendSpaceLine(3, "/// </summary>");
            plus.AppendSpaceLine(3, "public readonly static Field All = new Field(\"*\",\"" + TableName + "\");");
            foreach (ColumnInfo col in Columns)
            {
                plus.AppendSpaceLine(3, "/// <summary>");
                plus.AppendSpaceLine(3, "/// " + col.DeText);
                plus.AppendSpaceLine(3, "/// </summary>");
                plus.AppendSpaceLine(3, "public readonly static Field " + col.ColumnName + " = new Field(\"" + col.ColumnNameRealName + "\",\"" + TableName + "\",\"" + (string.IsNullOrEmpty(col.DeText) ? col.ColumnNameRealName : col.DeText) + "\");");
            }
            plus.AppendSpaceLine(2, "}");
            plus.AppendSpaceLine(2, "#endregion");
            plus.AppendLine();

            return plus.ToString();


        }




    }

    public class DbToCS
    {

        /// <summary>
        /// 类型配置文件
        /// </summary>
        public static readonly string DbTypePath = Application.StartupPath + "/Config/dbtype.xml";


        private const string cachekeystring = "_dbtype_cache_";



        static Dictionary<string, string> loadType()
        {

            Dictionary<string, string> types = Dos.ORM.Cache.Default.GetCache(cachekeystring) as Dictionary<string, string>;

            if (null == types)
            {

                types = new Dictionary<string, string>();

                XmlDocument doc = new XmlDocument();

                doc.Load(DbTypePath);

                XmlNodeList nodes = doc.SelectNodes("//type");

                if (null != nodes && nodes.Count > 0)
                {
                    foreach (XmlNode node in nodes)
                    {
                        XmlAttribute att = node.Attributes["dbtype"];
                        if (null != att)
                        {
                            string dbtypeStr = att.Value.Trim().ToLower();
                            if (!types.ContainsKey(dbtypeStr))
                            {
                                XmlAttribute attcstype = node.Attributes["cstype"];
                                if (null != attcstype)
                                {
                                    types.Add(dbtypeStr, attcstype.Value);
                                }
                            }
                        }
                    }
                }

                Dos.ORM.Cache.Default.AddCacheFilesDependency(cachekeystring, types, DbTypePath);

            }

            return types;
        }


        /// <summary>
        /// 修改TypeName
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static List<Model.ColumnInfo> DbtoCSColumns(List<Model.ColumnInfo> columns, string dbType)
        {
            Dictionary<string, string> types = loadType();

            foreach (ColumnInfo column in columns)
            {
                try
                {
                    if (column.TypeName.Trim().ToLower() == "char" && column.Length == "36"
                        && (dbType == "MySql" || dbType == "Oracle"))
                    {
                        column.TypeName = types["uniqueidentifier"];
                    }
                    else if (column.TypeName.Trim().ToLower() == "tinyint" && column.Length == "1" && dbType == "MySql")
                    {
                        column.TypeName = types["bit"];
                    }
                    else
                    {
                        column.TypeName = types[column.TypeName.Trim().ToLower()];
                    }
                }
                catch
                {
                    column.TypeName = "object";
                }
                if (!column.IsIdentity && !column.IsPK && column.cisNull)
                {
                    if (!column.TypeName.Equals("string") && !column.TypeName.Equals("object") && !column.TypeName.Equals("byte[]"))
                        column.TypeName += "?";
                }
            }

            return columns;
        }





    }
}
