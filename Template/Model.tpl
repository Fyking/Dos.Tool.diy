using System;
using Kogel.Dapper.Extension.Attributes;
@{
    string strTemp;
    strTemp = "";
}
namespace @Model.NameSpace
{
    /// <summary>
    /// 实体类@(Model.ClassName)
    /// </summary>
    [Display(Rename = "@Model.TableName")]
    public class @Model.ClassName.Replace("Ecomm_",""):BaseModel
    {
        #region Model
    @foreach(var item in Model.Columns){
		//排除BaseModel字段
	    string[] cols = "Id,CreateUser,CreateTime,UpdateUser,UpdateTime,IsDelete".Split(',');bool flag = false;
		foreach(var col in cols){ if(item.ColumnName==col){ flag = true; } }
		if(flag){ continue; }
        @:/// <summary>
        @:/// @(item.DeText)
        @:/// </summary>
        @:public @item.TypeName @item.ColumnName { get;set; }
    }
        #endregion
    }
}